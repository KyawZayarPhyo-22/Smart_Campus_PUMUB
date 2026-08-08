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

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Semester;

public partial class Page_SemesterList
{
    private string statusMessage;

    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private List<SemesterModel> SemesterList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private string ErrorMessage { get; set; } = "";
    private bool IsProcessing { get; set; } = false;
    private bool ShowModal { get; set; } = false;
    private SemesterModel? SelectedSemester { get; set; }

    // Permissions Variables
    private List<string> userPermissions = new();
    private bool canManageSemester = true;

    private string SearchInput = "";

    private async Task ApplyFilter()
    {
        SearchTerm = SearchInput;
        CurrentPage = 1;
        await LoadSemesters();
    }

    private async Task ResetFilter()
    {
        SearchInput = "";
        SearchTerm = "";
        CurrentPage = 1;
        await LoadSemesters();
    }

    private async Task HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ApplyFilter();
        }
    }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<SemesterModel> FilteredSemesters => SemesterList;

    private async Task OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        await LoadSemesters();
    }

    public bool IsSuccess { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadSemesters();
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
                                  
            canManageSemester = userPermissions.Contains("Semester.Edit") || userPermissions.Contains("Semester.Delete");

            StateHasChanged();
        }
    }

    private async Task LoadSemesters()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var response = await HttpClientService.ExecuteAsync<PagedResult<SemesterModel>>(
                $"semester/paginate?pageNumber={CurrentPage}&pageSize={PageSize}&searchTerm={Uri.EscapeDataString(SearchTerm)}", 
                EnumHttpMethod.Get
            );
            if (response != null)
            {
                SemesterList = response.Items;
                TotalPages = response.TotalPages;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"ဒေတာဆွဲယူရာတွင် အမှားရှိပါသည်။ Error: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    private void OpenDeleteModal(SemesterModel semester)
    {
        SelectedSemester = semester;
        ShowModal = true;
        statusMessage = string.Empty;
        IsSuccess = false;
    }

    private void CloseDeleteModal()
    {
        SelectedSemester = null;
        ShowModal = false;
        statusMessage = string.Empty;
        IsSuccess = false;
    }
    private async Task DeleteSemester()
    {
        if (SelectedSemester == null) return;

        IsProcessing = true;

        statusMessage = string.Empty;
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<SemesterDeleteResponseModel>(
                $"semester/{SelectedSemester.SemesterId}",
                EnumHttpMethod.Delete
            );

            if (response != null && response.IsSuccess)
            {
                IsSuccess = true;
                statusMessage = response.Message ?? "Deleted successfully.";

                await LoadSemesters();

                await Task.Delay(800);
                CloseDeleteModal();
            }
            else
            {
                IsSuccess = false;

                // 🔥 same as Role style (simple fallback message)
                statusMessage = response?.Message
                    ?? "ဒီ Semester ကို အသုံးပြုထားသော Data များရှိနေသောကြောင့် ဖျက်၍ မရပါ။";
            }
        }
        catch
        {
            IsSuccess = false;

            statusMessage = "Server သို့ ချိတ်ဆက်ရာတွင် အမှားဖြစ်နေပါသည်။";
        }
        finally
        {
            IsProcessing = false;
        }
    }
}