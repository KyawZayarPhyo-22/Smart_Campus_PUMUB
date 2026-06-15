using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Position;

public partial class Page_PositionList
{
    [Inject]
    public HttpClientService HttpClientService { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    private List<PositionModel> PositionList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    
    private bool IsLoading { get; set; } = true;
    private string ErrorMessage { get; set; } = "";
    private bool IsProcessing { get; set; } = false;

    // Delete Modal Controls
    private bool ShowModal { get; set; } = false;
    private PositionModel? SelectedPosition { get; set; }

    // 🔍 Search Filter (Case-Insensitive)
    private IEnumerable<PositionModel> FilteredPositions => string.IsNullOrWhiteSpace(SearchTerm)
        ? PositionList
        : PositionList.Where(p => p.PositionName != null && 
                                  p.PositionName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        await LoadPositions();
    }

    // 🚀 GET Method ဖြင့် API မှ Positions စာရင်းအားလုံးအား ဆွဲယူခြင်း
    private async Task LoadPositions()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            // Backend API ၏ Route သတ်မှတ်ချက်အတိုင်း "position" သို့မဟုတ် "api/position" လိုအပ်သလို ညှိပေးပါဗျာ
            var response = await HttpClientService.ExecuteAsync<List<PositionModel>>(
                "position", 
                EnumHttpMethod.Get
            );

            if (response != null)
            {
                PositionList = response;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"ဒေတာဆွဲယူရာတွင် အမှားအယွင်းရှိပါသည်။ Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenDeleteModal(PositionModel position)
    {
        SelectedPosition = position;
        ShowModal = true;
    }

    private void CloseDeleteModal()
    {
        SelectedPosition = null;
        ShowModal = false;
    }

    // 🚀 DELETE Method ဖြင့် သက်ဆိုင်ရာ Position ID အား ဖျက်ချခြင်း
    private async Task DeletePosition()
    {
        if (SelectedPosition == null) return;

        IsProcessing = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<PositionDeleteResponseModel>(
                $"position/{SelectedPosition.PositionId}", 
                EnumHttpMethod.Delete
            );

            if (response != null && response.IsSuccess)
            {
                await JSRuntime.InvokeVoidAsync("alert", response.Message ?? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။");
                CloseDeleteModal();
                await LoadPositions(); // 🔄 စာရင်းအသစ် ချက်ချင်းပြန်တင်ခြင်း
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "ဖျက်သိမ်း၍ မရပါတကား။");
            }
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", $"Error: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }
}