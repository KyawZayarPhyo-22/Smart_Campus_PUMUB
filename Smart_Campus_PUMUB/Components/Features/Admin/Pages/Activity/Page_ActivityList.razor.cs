using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Activity;

public partial class Page_ActivityList : IDisposable
{
    private bool confirmed;

    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public Smart_Campus_PUMUB.Components.Features.Services.AdminLanguageService LangService { get; set; } = null!;
    [Inject] public Microsoft.Extensions.Configuration.IConfiguration Configuration { get; set; } = null!;

    public string GetFullImageUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        if (path.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return path;
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return path;

        var baseUrl = Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5077";
        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    private List<ActivityModel> ActivityList { get; set; } = new();
    private List<string> LocationList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private string ErrorMessage { get; set; } = "";
    private bool IsProcessing { get; set; } = false;

    // Permissions Variables
    private List<string> userPermissions = new();
    private bool canManageActivity = true;

    private string SearchInput = "";
    private string SelectedLocationInput = "All";
    private string SelectedLocation = "All";

    // Custom Dropdown Open States
    private bool isLocationDropdownOpen = false;

    private void ToggleLocationDropdown()
    {
        isLocationDropdownOpen = !isLocationDropdownOpen;
    }

    private void SelectLocation(string? loc)
    {
        SelectedLocationInput = loc ?? "All";
        isLocationDropdownOpen = false;
    }

    private void CloseAllDropdowns()
    {
        isLocationDropdownOpen = false;
    }

    private async Task ApplyFilter()
    {
        CloseAllDropdowns();
        SearchTerm = SearchInput;
        SelectedLocation = SelectedLocationInput;
        CurrentPage = 1;
        await LoadActivities();
    }

    private async Task ResetFilter()
    {
        CloseAllDropdowns();
        SearchInput = "";
        SearchTerm = "";
        SelectedLocationInput = "All";
        SelectedLocation = "All";
        CurrentPage = 1;
        await LoadActivities();
    }

    private async Task HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ApplyFilter();
        }
    }

    private bool ShowModal { get; set; } = false;
    private ActivityModel? SelectedActivity { get; set; }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<ActivityModel> FilteredActivities => ActivityList;

    private async Task OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        await LoadActivities();
    }

    protected override async Task OnInitializedAsync()
    {
        LangService.OnLanguageChanged += StateHasChanged;
        await LoadLocations();
        await LoadActivities();
    }

    private async Task LoadLocations()
    {
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<string>>("activity/locations", EnumHttpMethod.Get);
            if (response != null) LocationList = response;
        }
        catch { }
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
                                  
            canManageActivity = userPermissions.Contains("Activity.Edit") || userPermissions.Contains("Activity.Delete");

            StateHasChanged();
        }
    }

    private async Task LoadActivities()
    {
        IsLoading = true;
        ErrorMessage = "";
        StateHasChanged();
        try
        {
            var delayTask = Task.Delay(500);
            var fetchTask = HttpClientService.ExecuteAsync<PagedResult<ActivityModel>>(
                $"activity/paginate?pageNumber={CurrentPage}&pageSize={PageSize}&searchTerm={Uri.EscapeDataString(SearchTerm)}&location={Uri.EscapeDataString(SelectedLocation)}", 
                EnumHttpMethod.Get
            );

            await Task.WhenAll(fetchTask, delayTask);
            var response = await fetchTask;
            if (response != null)
            {
                ActivityList = response.Items;
                TotalPages = response.TotalPages;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = LangService.IsMyanmar ? $"ဒေတာဆွဲယူရာတွင် အမှားရှိပါသည်။ Error: {ex.Message}" : $"Failed to load data. Error: {ex.Message}";
        }
        finally 
        { 
            IsLoading = false; 
            StateHasChanged();
        }
    }

    private string statusMessage = string.Empty;
    private bool IsSuccess = false;

    private void OpenDeleteModal(ActivityModel activity)
    {
        SelectedActivity = activity;
        ShowModal = true;

        statusMessage = string.Empty;
        IsSuccess = false;
    }

    private void CloseDeleteModal()
    {
        SelectedActivity = null;
        ShowModal = false;

        statusMessage = string.Empty;
        IsSuccess = false;
    }

    private async Task DeleteActivity()
    {
        if (SelectedActivity == null) return;

        IsProcessing = true;
        statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းနေပါသည်..." : "Deleting...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<ActivityDeleteResponseModel>(
                $"activity/{SelectedActivity.ActivityId}",
                EnumHttpMethod.Delete
            );

            if (response?.IsSuccess == true)
            {
                IsSuccess = true;
                statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။" : (response.Message ?? "Deleted successfully.");

                await LoadActivities();

                await Task.Delay(800);
                CloseDeleteModal();
            }
            else
            {
                IsSuccess = false;
                statusMessage = LangService.IsMyanmar ? "ဤ လှုပ်ရှားမှု ကို ဖျက်၍ မရပါ။" : (response?.Message ?? "Cannot delete this activity.");
            }
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            statusMessage = $"Error: {ex.Message}";
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