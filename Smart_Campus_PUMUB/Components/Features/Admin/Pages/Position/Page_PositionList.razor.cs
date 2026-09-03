using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Position;

public partial class Page_PositionList : IDisposable
{
    [Inject]
    public HttpClientService HttpClientService { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    [Inject]
    public Smart_Campus_PUMUB.Components.Features.Services.AdminLanguageService LangService { get; set; } = null!;

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
        LangService.OnLanguageChanged += StateHasChanged;
        await LoadPositions();
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

    private async Task LoadPositions()
    {
        IsLoading = true;
        ErrorMessage = "";
        StateHasChanged();
        try
        {
            var delayTask = Task.Delay(500);
            var fetchTask = HttpClientService.ExecuteAsync<PagedResult<PositionModel>>(
                $"position/paginate?pageNumber={CurrentPage}&pageSize={PageSize}&searchTerm={Uri.EscapeDataString(SearchTerm)}", 
                EnumHttpMethod.Get
            );

            await Task.WhenAll(fetchTask, delayTask);
            var response = await fetchTask;

            if (response != null)
            {
                PositionList = response.Items;
                TotalPages = response.TotalPages;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = LangService.IsMyanmar ? $"ဒေတာဆွဲယူရာတွင် အမှားအယွင်းရှိပါသည်။ Error: {ex.Message}" : $"Failed to load data. Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
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

    private async Task DeletePosition()
    {
        if (SelectedPosition == null) return;

        IsProcessing = true;
        statusMessage = LangService.IsMyanmar ? "ရာထူးကို ဖျက်သိမ်းနေပါသည်..." : "Deleting position...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<PositionDeleteResponseModel>(
                $"position/{SelectedPosition.PositionId}",
                EnumHttpMethod.Delete
            );

            if (response?.IsSuccess == true)
            {
                statusMessage = LangService.IsMyanmar ? "ရာထူး ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။" : (response.Message ?? "Deleted successfully.");
                IsSuccess = true;
                SelectedPosition = null;
                CloseDeleteModal();
                await LoadPositions();
            }
            else
            {
                IsSuccess = false;
                statusMessage = LangService.IsMyanmar ? "ဤ ရာထူး ကို အသုံးပြုထားသောကြောင့် ဖျက်၍ မရပါ။" : (response?.Message ?? "Cannot delete this position because it is in use.");
            }
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            statusMessage = LangService.IsMyanmar ? $"စနစ်ချို့ယွင်းမှု ဖြစ်ပွားနေပါသည်။ Error: {ex.Message}" : $"System error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    public void Dispose()
    {
        LangService.OnLanguageChanged -= StateHasChanged;
    }
}