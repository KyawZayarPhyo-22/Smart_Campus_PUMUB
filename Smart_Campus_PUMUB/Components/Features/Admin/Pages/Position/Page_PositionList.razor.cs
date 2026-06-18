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

    private string SearchInput = "";

    private void ApplyFilter()
    {
        SearchTerm = SearchInput;
        CurrentPage = 1;
        StateHasChanged();
    }

    private void ResetFilter()
    {
        SearchInput = "";
        SearchTerm = "";
        CurrentPage = 1;
        StateHasChanged();
    }

    private void HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            ApplyFilter();
        }
    }

    private string statusMessage;
    private bool IsSuccess;

    // Delete Modal Controls
    private bool ShowModal { get; set; } = false;
    private PositionModel? SelectedPosition { get; set; }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<PositionModel> GetFilteredPositions() => string.IsNullOrWhiteSpace(SearchTerm)
        ? PositionList
        : PositionList.Where(p => p.PositionName != null && 
                                  p.PositionName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    private IEnumerable<PositionModel> FilteredPositions
    {
        get
        {
            var allFiltered = GetFilteredPositions();
            int count = allFiltered.Count();
            int calcPages = (int)Math.Ceiling((decimal)count / PageSize);
            TotalPages = calcPages < 1 ? 1 : calcPages;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            return allFiltered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        }
    }

    private void OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        StateHasChanged();
    }

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
        // ရွေးချယ်ထားတဲ့ Position မရှိရင် အလုပ်မလုပ်အောင် တားထားပါမယ်
        if (SelectedPosition == null) return;

        IsProcessing = true;
        statusMessage = "ရာထူးကို ဖျက်သိမ်းနေပါသည်...";

        try
        {
            var response = await HttpClientService.ExecuteAsync<PositionDeleteResponseModel>(
                $"position/{SelectedPosition.PositionId}",
                EnumHttpMethod.Delete
            );

            // API ကနေ IsSuccess = true ပြန်လာတဲ့အခါ
            if (response?.IsSuccess == true)
            {
                statusMessage = response.Message ?? "ရာထူး ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။";
                SelectedPosition = null; // ဖျက်ပြီးသွားရင် ရွေးထားတာကို အလွတ်ပြန်ထားပေးတာက ပိုကောင်းပါတယ်
                CloseDeleteModal();
                await LoadPositions(); // List ကို အသစ်ပြန်ခေါ်ပါမယ်
            }
            else
            {
                // API ကနေ BadRequest နဲ့ ပို့လိုက်တဲ့ Validation Message တွေ ဒီနေရာမှာ ပေါ်လာပါလိမ့်မယ်
                statusMessage = response?.Message ?? "ရာထူး ဖျက်သိမ်း၍ မရပါ။ (အသုံးပြုနေသူများ ရှိနိုင်ပါသည်)";
            }
        }
        catch (Exception ex)
        {
            // Network အခက်အခဲ ဒါမှမဟုတ် တခြား Error တွေအတွက်
            statusMessage = $"စနစ်ချို့ယွင်းမှု ဖြစ်ပွားနေပါသည်။ Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;

            // မှတ်ချက် - အကယ်၍ Blazor ကို အသုံးပြုထားတာဆိုရင် UI ချက်ချင်း Update ဖြစ်သွားအောင် 
            // အောက်က StateHasChanged(); ကို ဖွင့်သုံးပေးဖို့ လိုနိုင်ပါတယ်နော်။
            // StateHasChanged(); 
        }
    }
}