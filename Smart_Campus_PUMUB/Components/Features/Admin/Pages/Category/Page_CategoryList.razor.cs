using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Category;

public partial class Page_CategoryList
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<CategoryModel> CategoryList { get; set; } = new();
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
    private CategoryModel? SelectedCategory { get; set; }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<CategoryModel> GetFilteredCategories() => string.IsNullOrWhiteSpace(SearchTerm)
        ? CategoryList
        : CategoryList.Where(c => c.CategoryName != null && c.CategoryName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    private IEnumerable<CategoryModel> FilteredCategories
    {
        get
        {
            var allFiltered = GetFilteredCategories();
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
        await LoadCategories();
    }

    private async Task LoadCategories()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<CategoryModel>>("category", EnumHttpMethod.Get);
            if (response != null) CategoryList = response;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"ဒေတာဆွဲယူရာတွင် အမှားရှိပါသည်။ Error: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    private void OpenDeleteModal(CategoryModel category)
    {
        SelectedCategory = category;
        ShowModal = true;
        statusMessage = ""; // Reset message
        IsSuccess = false;
    }

    private void CloseDeleteModal()
    {
        SelectedCategory = null;
        ShowModal = false;
        statusMessage = "";
        IsSuccess = false;
    }
    private async Task DeleteCategory()
    {
        if (SelectedCategory == null) return;

        IsProcessing = true;
        statusMessage = "ဖျက်သိမ်းနေပါသည်...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<CategoryDeleteResponseModel>(
                $"category/{SelectedCategory.CategoryId}",
                EnumHttpMethod.Delete
            );

            if (response != null && response.IsSuccess)
            {
                statusMessage = "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။";
                IsSuccess = true;

                await Task.Delay(1500);
                CloseDeleteModal();
                await LoadCategories();
            }
            else
            {
                statusMessage = "ဤ Category ကို အခြားနေရာတွင် အသုံးပြုနေသောကြောင့် ဖျက်၍ မရပါ။";
                IsSuccess = false;
            }
        }
        catch (Exception)
        {
            statusMessage = "ဤ Category ကို အခြားနေရာတွင် အသုံးပြုနေသောကြောင့် ဖျက်၍ မရပါ။";
            IsSuccess = false;
        }
        finally
        {
            IsProcessing = false;
        }
    }


}