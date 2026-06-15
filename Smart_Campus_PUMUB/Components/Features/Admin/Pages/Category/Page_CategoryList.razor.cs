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
    private bool ShowModal { get; set; } = false;
    private CategoryModel? SelectedCategory { get; set; }

    private IEnumerable<CategoryModel> FilteredCategories => string.IsNullOrWhiteSpace(SearchTerm)
        ? CategoryList
        : CategoryList.Where(c => c.CategoryName != null && c.CategoryName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

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

    private void OpenDeleteModal(CategoryModel category) { SelectedCategory = category; ShowModal = true; }
    private void CloseDeleteModal() { SelectedCategory = null; ShowModal = false; }

    private async Task DeleteCategory()
    {
        if (SelectedCategory == null) return;
        IsProcessing = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<CategoryDeleteResponseModel>($"category/{SelectedCategory.CategoryId}", EnumHttpMethod.Delete);
            if (response != null && response.IsSuccess)
            {
                await JSRuntime.InvokeVoidAsync("alert", response.Message ?? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။");
                CloseDeleteModal();
                await LoadCategories();
            }
            else { await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "ဖျက်သိမ်း၍ မရပါ။"); }
        }
        catch (Exception ex) { await JSRuntime.InvokeVoidAsync("alert", $"Error: {ex.Message}"); }
        finally { IsProcessing = false; }
    }
}