using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.AcademicPolicy;

public partial class Page_AcademicPolicy : IDisposable
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public Smart_Campus_PUMUB.Components.Features.Services.AdminLanguageService LangService { get; set; } = null!;

    // ==========================================
    // Dynamic Retake Limit Policy Management
    // ==========================================
    private int maxRetakeLimit = 32;
    private int newRetakeLimitInput = 32;
    private bool isSavingRetakeLimit = false;
    private string retakeSettingMessage = "";

    // ==========================================
    // Dynamic Faculty Semester Credit Management
    // ==========================================
    private List<FacultyModel> creditFacultyList = new();
    private int selectedCreditFacultyId = 1;
    private List<FacultySemesterCreditModel> facultySemesterCredits = new();
    private bool isLoadingSemesterCredits = false;
    private HashSet<int> savingCreditItemIds = new();
    private string semesterCreditAlertMessage = "";

    protected override async Task OnInitializedAsync()
    {
        LangService.OnLanguageChanged += StateHasChanged;
        await LoadRetakeLimitSetting();
        await LoadCreditFaculties();
        await LoadFacultySemesterCredits();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        try
        {
            var savedLang = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "admin_dashboard_lang");
            if (!string.IsNullOrEmpty(savedLang))
            {
                LangService.SetLanguage(savedLang);
            }
        }
        catch { }
    }

    private async Task LoadRetakeLimitSetting()
    {
        try
        {
            var res = await HttpClientService.ExecuteAsync<RetakeSettingResponse>("student/settings/max-retake-limit", EnumHttpMethod.Get);
            if (res != null && res.MaxRetakeLimit > 0)
            {
                maxRetakeLimit = res.MaxRetakeLimit;
                newRetakeLimitInput = res.MaxRetakeLimit;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading max retake limit setting: {ex.Message}");
        }
    }

    private async Task SaveRetakeLimitSetting()
    {
        if (newRetakeLimitInput <= 0 || newRetakeLimitInput > 100)
        {
            retakeSettingMessage = LangService.IsMyanmar ? "အများဆုံး Retake အကြိမ်အရေအတွက်ကို ၁ မှ ၁၀၀ ကြားသာ ထည့်သွင်းပေးပါ။" : "Please enter a max retake limit between 1 and 100.";
            return;
        }

        isSavingRetakeLimit = true;
        retakeSettingMessage = "";
        StateHasChanged();

        try
        {
            var payload = new SystemSettingModel
            {
                SettingKey = "MaxRetakeLimit",
                SettingValue = newRetakeLimitInput.ToString()
            };
            var res = await HttpClientService.ExecuteAsync<ActionResponseModel>("student/settings/max-retake-limit", EnumHttpMethod.Put, payload);
            if (res != null && res.IsSuccess)
            {
                maxRetakeLimit = newRetakeLimitInput;
                retakeSettingMessage = LangService.IsMyanmar ? $"✓ အများဆုံး Retake အကြိမ် ({newRetakeLimitInput} ကြိမ်) အား သတ်မှတ်ပြီးပါပြီ။" : $"✓ Max retake limit ({newRetakeLimitInput}) updated successfully.";
            }
            else
            {
                retakeSettingMessage = res?.Message ?? (LangService.IsMyanmar ? "Setting ပြောင်းလဲရာတွင် အမှားဖြစ်ပေါ်နေပါသည်။" : "Failed to update setting.");
            }
        }
        catch (Exception ex)
        {
            retakeSettingMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isSavingRetakeLimit = false;
            StateHasChanged();
        }
    }

    private async Task LoadCreditFaculties()
    {
        try
        {
            var faculties = await HttpClientService.ExecuteAsync<List<FacultyModel>>("faculty", EnumHttpMethod.Get);
            if (faculties != null && faculties.Any())
            {
                creditFacultyList = faculties;
                if (!creditFacultyList.Any(f => f.FacultyId == selectedCreditFacultyId))
                {
                    selectedCreditFacultyId = creditFacultyList.First().FacultyId;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading faculties for credit policy: {ex.Message}");
        }
    }

    private async Task LoadFacultySemesterCredits()
    {
        if (selectedCreditFacultyId <= 0) return;

        isLoadingSemesterCredits = true;
        semesterCreditAlertMessage = "";
        StateHasChanged();
        try
        {
            var credits = await HttpClientService.ExecuteAsync<List<FacultySemesterCreditModel>>(
                $"student/settings/semester-credits?facultyId={selectedCreditFacultyId}", 
                EnumHttpMethod.Get
            );
            if (credits != null)
            {
                facultySemesterCredits = credits.OrderBy(c => c.Sequence ?? c.SemesterId).ToList();
            }
        }
        catch (Exception ex)
        {
            semesterCreditAlertMessage = LangService.IsMyanmar ? $"Semester Credit သတ်မှတ်ချက်များ ဆွဲယူရာတွင် အမှားဖြစ်ပေါ်ပါသည်: {ex.Message}" : $"Failed to load credits: {ex.Message}";
        }
        finally
        {
            isLoadingSemesterCredits = false;
            StateHasChanged();
        }
    }

    private async Task OnCreditFacultyChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int facultyId) && facultyId > 0)
        {
            selectedCreditFacultyId = facultyId;
            await LoadFacultySemesterCredits();
        }
    }

    private async Task SaveSingleSemesterCredit(FacultySemesterCreditModel item)
    {
        int min = item.MinCredits ?? 18;
        int max = item.MaxCredits ?? 24;

        if (min <= 0 || max <= 0)
        {
            semesterCreditAlertMessage = LangService.IsMyanmar ? "Credit Point တန်ဖိုးများသည် အနည်းဆုံး ၁ မှတ်နှင့်အထက် ဖြစ်ရပါမည်။" : "Credits must be at least 1.";
            return;
        }

        if (min > max)
        {
            semesterCreditAlertMessage = LangService.IsMyanmar ? $"အနည်းဆုံး Credit ({min}) သည် အများဆုံး Credit ({max}) ထက် မကြီးရပါ။" : $"Min credits ({min}) cannot exceed max credits ({max}).";
            return;
        }

        savingCreditItemIds.Add(item.SemesterId);
        semesterCreditAlertMessage = "";
        StateHasChanged();

        try
        {
            var payload = new FacultySemesterCreditUpdateRequest
            {
                FacultyId = selectedCreditFacultyId,
                SemesterId = item.SemesterId,
                RequiredCredits = max,
                MinCredits = min,
                MaxCredits = max
            };

            var res = await HttpClientService.ExecuteAsync<ActionResponseModel>("student/settings/semester-credits", EnumHttpMethod.Put, payload);
            if (res != null && res.IsSuccess)
            {
                var facName = creditFacultyList.FirstOrDefault(f => f.FacultyId == selectedCreditFacultyId)?.FacultyName ?? "Faculty";
                semesterCreditAlertMessage = LangService.IsMyanmar ? $"✓ {facName} ၏ {item.SemesterName} အတွက် Credit Range ({min} ~ {max}) သတ်မှတ်ပြီးပါပြီ။" : $"✓ Credit range ({min} ~ {max}) updated for {facName} - {item.SemesterName}.";
            }
            else
            {
                semesterCreditAlertMessage = res?.Message ?? (LangService.IsMyanmar ? "Credit သတ်မှတ်ချက် ပြောင်းလဲရာတွင် အမှားဖြစ်ပေါ်နေပါသည်။" : "Failed to update credit policy.");
            }
        }
        catch (Exception ex)
        {
            semesterCreditAlertMessage = $"Error: {ex.Message}";
        }
        finally
        {
            savingCreditItemIds.Remove(item.SemesterId);
            StateHasChanged();
        }
    }

    public class RetakeSettingResponse
    {
        public int MaxRetakeLimit { get; set; }
    }

    public void Dispose()
    {
        LangService.OnLanguageChanged -= StateHasChanged;
    }
}
