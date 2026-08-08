using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Position;

public partial class Page_PositionList
{
    [Inject]
    public HttpClientService HttpClientService { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private List<PositionModel> PositionList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    
    private bool IsLoading { get; set; } = true;
    private string ErrorMessage { get; set; } = "";
    private bool IsProcessing { get; set; } = false;

    // Permissions Variables
    private List<string> userPermissions = new();
    private bool canManagePosition = true;

    private string SearchInput = "";

    private async Task ApplyFilter()
    {
        SearchTerm = SearchInput;
        CurrentPage = 1;
        await LoadPositions();
    }

    private async Task ResetFilter()
    {
        SearchInput = "";
        SearchTerm = "";
        CurrentPage = 1;
        await LoadPositions();
    }

    private async Task HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ApplyFilter();
        }
    }

    private string statusMessage = "";
    private bool IsSuccess;

    // Delete Modal Controls
    private bool ShowModal { get; set; } = false;
    private PositionModel? SelectedPosition { get; set; }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<PositionModel> FilteredPositions => PositionList;

    private async Task OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        await LoadPositions();
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadPositions();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            userPermissions = user.Claims
                                  .Where(c => c.Type == "Permission")
                                  .Select(c => c.Value)
                                  .ToList();
                                  
            canManagePosition = userPermissions.Contains("Position.Edit") || userPermissions.Contains("Position.Delete");

            StateHasChanged();
        }
    }

    // 🚀 GET Method ဖြင့် API မှ Positions စာရင်းအားလုံးအား ဆွဲယူခြင်း
    private async Task LoadPositions()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var response = await HttpClientService.ExecuteAsync<PagedResult<PositionModel>>(
                $"position/paginate?pageNumber={CurrentPage}&pageSize={PageSize}&searchTerm={Uri.EscapeDataString(SearchTerm)}", 
                EnumHttpMethod.Get
            );

            if (response != null)
            {
                PositionList = response.Items;
                TotalPages = response.TotalPages;
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
        statusMessage = "";
        IsSuccess = false;
        ShowModal = true;
    }

    private void CloseDeleteModal()
    {
        SelectedPosition = null;
        statusMessage = "";
        IsSuccess = false;
        ShowModal = false;
    }

    // 🚀 DELETE Method ဖြင့် သက်ဆိုင်ရာ Position ID အား ဖျက်ချခြင်း
    private async Task DeletePosition()
    {
        // ရွေးချယ်ထားတဲ့ Position မရှိရင် အလုပ်မလုပ်အောင် တားထားပါမယ်
        if (SelectedPosition == null) return;

        IsProcessing = true;
        statusMessage = "ရာထူးကို ဖျက်သိမ်းနေပါသည်...";
        IsSuccess = false;

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
                IsSuccess = true;
                SelectedPosition = null; // ဖျက်ပြီးသွားရင် ရွေးထားတာကို အလွတ်ပြန်ထားပေးတာက ပိုကောင်းပါတယ်
                CloseDeleteModal();
                await LoadPositions(); // List ကို အသစ်ပြန်ခေါ်ပါမယ်
            }
            else
            {
                IsSuccess = false;
                // API ကနေ BadRequest နဲ့ ပို့လိုက်တဲ့ Validation Message တွေ ဒီနေရာမှာ ပေါ်လာပါလိမ့်မယ်
                statusMessage = response?.Message ?? "ရာထူး ဖျက်သိမ်း၍ မရပါ။ (အသုံးပြုနေသူများ ရှိနိုင်ပါသည်)";
            }
        }
        catch (Exception ex)
        {
            IsSuccess = false;
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