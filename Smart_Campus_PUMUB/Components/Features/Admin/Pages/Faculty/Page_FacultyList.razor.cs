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

    public bool IsSuccess { get; private set; }
    private bool ShowModal { get; set; } = false;
    private FacultyModel? SelectedFaculty { get; set; }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<FacultyModel> GetFilteredFaculties() => string.IsNullOrWhiteSpace(SearchTerm)
        ? FacultyList
        : FacultyList.Where(f => f.FacultyName != null && f.FacultyName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    private IEnumerable<FacultyModel> FilteredFaculties
    {
        get
        {
            var allFiltered = GetFilteredFaculties();
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