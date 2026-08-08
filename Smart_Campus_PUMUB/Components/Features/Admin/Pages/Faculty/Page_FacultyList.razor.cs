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

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Faculty;

public partial class Page_FacultyList
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private List<FacultyModel> FacultyList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private string ErrorMessage { get; set; } = "";
    private bool IsProcessing { get; set; } = false;

    // Permissions Variables
    private List<string> userPermissions = new();
    private bool canManageFaculty = true;

    private string SearchInput = "";

    private async Task ApplyFilter()
    {
        SearchTerm = SearchInput;
        CurrentPage = 1;
        await LoadFaculties();
    }

    private async Task ResetFilter()
    {
        SearchInput = "";
        SearchTerm = "";
        CurrentPage = 1;
        await LoadFaculties();
    }

    private async Task HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ApplyFilter();
        }
    }

    private string statusMessage = "";

    public bool IsSuccess { get; private set; }
    private bool ShowModal { get; set; } = false;
    private FacultyModel? SelectedFaculty { get; set; }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<FacultyModel> FilteredFaculties => FacultyList;

    private async Task OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        await LoadFaculties();
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadFaculties();
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
                                  
            canManageFaculty = userPermissions.Contains("Faculty.Edit") || userPermissions.Contains("Faculty.Delete");

            StateHasChanged();
        }
    }

    private async Task LoadFaculties()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var response = await HttpClientService.ExecuteAsync<PagedResult<FacultyModel>>(
                $"faculty/paginate?pageNumber={CurrentPage}&pageSize={PageSize}&searchTerm={Uri.EscapeDataString(SearchTerm)}", 
                EnumHttpMethod.Get
            );
            if (response != null)
            {
                FacultyList = response.Items;
                TotalPages = response.TotalPages;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"ဒေတာဆွဲယူရာတွင် အမှားရှိပါသည်။ Error: {ex.Message}";
        }
        finally { IsLoading = false; }
    }



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
        if (SelectedFaculty == null) return;

        IsProcessing = true;
        statusMessage = "ဖျက်သိမ်းနေပါသည်...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<FacultyDeleteResponseModel>(
                $"faculty/{SelectedFaculty.FacultyId}",
                EnumHttpMethod.Delete
            );

            if (response != null && response.IsSuccess)
            {
                statusMessage = "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။";
                IsSuccess = true;

                await Task.Delay(1500); // User မြင်အောင် ခဏစောင့်ပေးခြင်း
                CloseDeleteModal();
                await LoadFaculties();
            }
            else
            {
                statusMessage = "ဤ Faculty ကို အခြားနေရာတွင် အသုံးပြုနေသောကြောင့် ဖျက်၍ မရပါ။";
                IsSuccess = false;
            }
        }
        catch (Exception)
        {
            statusMessage = "ဤ Faculty ကို အခြားနေရာတွင် အသုံးပြုနေသောကြောင့် ဖျက်၍ မရပါ။";
            IsSuccess = false;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void OpenDeleteModal(FacultyModel faculty)
    {
        SelectedFaculty = faculty;
        ShowModal = true;
        statusMessage = "";
        IsSuccess = false;
    }
    private void CloseDeleteModal()
    {
        SelectedFaculty = null;
        ShowModal = false;
        statusMessage = "";
        IsSuccess = false;
    }



}