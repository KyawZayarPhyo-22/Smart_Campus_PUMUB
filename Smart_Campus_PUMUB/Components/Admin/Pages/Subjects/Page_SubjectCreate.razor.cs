using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Subject;

public partial class Page_SubjectCreate
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public NavigationManager Nav { get; set; } = null!;

    private SubjectCreateRequestModel subject = new();
    private List<SemesterModel> SemesterList = new();

    private bool IsProcessing = false;
    private string? ErrorMessage;

    protected override async Task OnInitializedAsync()
    {
        // Semester List ကို API ကနေ ဆွဲယူထားခြင်း
        SemesterList = await HttpClientService.ExecuteAsync<List<SemesterModel>>("semester", EnumHttpMethod.Get) ?? new();
    }

    private async Task SaveSubject()
    {
        ErrorMessage = null; // Error အဟောင်းများကို ရှင်းလင်းခြင်း

        // ၁။ Duplicate စစ်ဆေးခြင်း
        var existingSubjects = await HttpClientService.ExecuteAsync<List<SubjectModel>>("subject", EnumHttpMethod.Get) ?? new();

        bool isDuplicate = existingSubjects.Any(s => s.SemesterId == subject.SemesterId
                                                && s.SubjectCode != null
                                                && subject.SubjectCode != null
                                                && s.SubjectCode.Trim().Equals(subject.SubjectCode.Trim(), StringComparison.OrdinalIgnoreCase));

        if (isDuplicate)
        {
            ErrorMessage = "ဤ Semester အတွင်းတွင် ဤ Subject Code သည် ရှိနှင့်ပြီးဖြစ်ပါသည်။";
            return; // ဆက်မလုပ်တော့ဘဲ ရပ်လိုက်ပါ
        }

        // ၂။ Save လုပ်ခြင်း
        IsProcessing = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<SubjectResponseModel>("subject", EnumHttpMethod.Post, subject);

            if (response != null && response.IsSuccess)
            {
                Nav.NavigateTo("/admin/subjects");
            }
            else
            {
                ErrorMessage = response?.Message ?? "သိမ်းဆည်း၍မရပါ။ စနစ်တွင် အမှားအယွင်းရှိနေပါသည်။";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
}