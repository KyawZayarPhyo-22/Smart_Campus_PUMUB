using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Faculty;

public partial class Page_FacultyList
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<FacultyModel> FacultyList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private string ErrorMessage { get; set; } = "";
    private bool IsProcessing { get; set; } = false;
    private bool ShowModal { get; set; } = false;
    private FacultyModel? SelectedFaculty { get; set; }

    private IEnumerable<FacultyModel> FilteredFaculties => string.IsNullOrWhiteSpace(SearchTerm)
        ? FacultyList
        : FacultyList.Where(f => f.FacultyName != null && f.FacultyName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        await LoadFaculties();
    }

    private async Task LoadFaculties()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<FacultyModel>>("faculty", EnumHttpMethod.Get);
            if (response != null)
            {
                FacultyList = response;
                    
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"ဒေတာဆွဲယူရာတွင် အမှားရှိပါသည်။ Error: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    private void OpenDeleteModal(FacultyModel faculty) { SelectedFaculty = faculty; ShowModal = true; }
    private void CloseDeleteModal() { SelectedFaculty = null; ShowModal = false; }

    //private async Task DeleteFaculty()
    //{
    //    if (SelectedFaculty == null) return;
    //    IsProcessing = true;
    //    try
    //    {
    //        var response = await HttpClientService.ExecuteAsync<FacultyDeleteResponseModel>($"faculty/{SelectedFaculty.FacultyId}", EnumHttpMethod.Delete);
    //        if (response != null && response.IsSuccess)
    //        {
    //            await JSRuntime.InvokeVoidAsync("alert", response.Message ?? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။");
    //            CloseDeleteModal();
    //            await LoadFaculties();
    //        }
    //        else
    //        {
    //            await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "ဖျက်သိမ်း၍ မရပါ။");
    //        }
    //    }
    //    catch (Exception ex) { await JSRuntime.InvokeVoidAsync("alert", $"Error: {ex.Message}"); }
    //    finally { IsProcessing = false; }
    //}

    private async Task DeleteFaculty()
    {
        // အကယ်၍ SelectedFaculty မရှိလျှင် ဘာမှမလုပ်ပါ
        if (SelectedFaculty == null) return;

        // browser confirm box ကို ဖယ်ရှားလိုက်ပါပြီ

        IsProcessing = true;
        try
        {
            // API သို့ Delete Request ပေးပို့ခြင်း
            var response = await HttpClientService.ExecuteAsync<FacultyDeleteResponseModel>(
                $"faculty/{SelectedFaculty.FacultyId}", EnumHttpMethod.Delete);

            if (response != null && response.IsSuccess)
            {
                // ဖျက်သိမ်းမှု အောင်မြင်ပါက Modal ကို ပိတ်ပြီး Data ကို Refresh လုပ်ပါ
                CloseDeleteModal();
                await LoadFaculties();
            }
            else
            {
                // Error ဖြစ်ပါက Log ထုတ်ပြခြင်း
                // သင့် UI ထဲတွင် Error message ပြလိုလျှင် ဤနေရာတွင် State ပြောင်းလဲပေးနိုင်ပါသည်
                Console.WriteLine(response?.Message ?? "ဖျက်သိမ်း၍ မရပါ။");
            }
        }
        catch (Exception ex)
        {
            // Exception တက်ပါက Log ထုတ်ပြခြင်း
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            // Processing ဖြစ်နေခြင်းကို ရပ်တန့်ပေးခြင်း
            IsProcessing = false;
        }
    }
}