// using Microsoft.AspNetCore.Components; // ComponentBase အတွက် လိုအပ်သည်
// using Microsoft.JSInterop;
// using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
// using Smart_Campus_PUMUB.WebApi.Models;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;

// namespace Smart_Campus_PUMUB.Components.Admin.Pages.Department;

// // ComponentBase ကို အမွေဆက်ခံမှသာ OnInitializedAsync ကို override လုပ်နိုင်မည်
// public partial class Page_CategoryList : ComponentBase
// {
//     [Inject] public HttpClientService HttpClientService { get; set; } = null!;
//     [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

//     private List<CategoryModel> CategoryList { get; set; } = new();
//     private string SearchTerm { get; set; } = "";
//     private bool IsLoading { get; set; } = true;
//     private string ErrorMessage { get; set; } = "";
//     private bool IsProcessing { get; set; } = false;
//     private bool ShowModal { get; set; } = false;
//     private CategoryModel? SelectedCategory { get; set; }

//     private IEnumerable<CategoryModel> FilteredCategories => string.IsNullOrWhiteSpace(SearchTerm)
//         ? CategoryList
//         : CategoryList.Where(c => c.CategoryName != null && c.CategoryName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

//     // override keyword ကို အသုံးပြုနိုင်ပြီ
//     protected override async Task OnInitializedAsync()
//     {
//         await LoadCategories();
//     }

//     private async Task LoadCategories()
//     {
//         IsLoading = true;
//         ErrorMessage = "";
//         try
//         {
//             var response = await HttpClientService.ExecuteAsync<List<CategoryModel>>("category", EnumHttpMethod.Get);
//             if (response != null) CategoryList = response;
//         }
//         catch (Exception ex)
//         {
//             ErrorMessage = $"ဒေတာဆွဲယူရာတွင် အမှားရှိပါသည်။ Error: {ex.Message}";
//         }
//         finally { IsLoading = false; }
//     }

//     public void OpenDeleteModal(CategoryModel category) 
//     { 
//         SelectedCategory = category; 
//         ShowModal = true; 
//     }
    
//     public void CloseDeleteModal() 
//     { 
//         SelectedCategory = null; 
//         ShowModal = false; 
//     }

//     public async Task DeleteCategory()
//     {
//         if (SelectedCategory == null) return;
//         IsProcessing = true;
//         try
//         {
//             var response = await HttpClientService.ExecuteAsync<CategoryDeleteResponseModel>($"category/{SelectedCategory.CategoryId}", EnumHttpMethod.Delete);
//             if (response != null && response.IsSuccess)
//             {
//                 await JSRuntime.InvokeVoidAsync("alert", response.Message ?? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။");
//                 CloseDeleteModal();
//                 await LoadCategories();
//             }
//             else 
//             { 
//                 await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "ဖျက်သိမ်း၍ မရပါ။"); 
//             }
//         }
//         catch (Exception ex) 
//         { 
//             await JSRuntime.InvokeVoidAsync("alert", $"Error: {ex.Message}"); 
//         }
//         finally 
//         { 
//             IsProcessing = false; 
//         }
//     }
// }
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Department;

public partial class Page_DepartmentList : ComponentBase
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<DepartmentModel> DepartmentList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; } = false;
    private bool ShowModal { get; set; } = false;
    private DepartmentModel? SelectedDepartment { get; set; }

    private IEnumerable<DepartmentModel> FilteredDepartments => string.IsNullOrWhiteSpace(SearchTerm)
        ? DepartmentList
        : DepartmentList.Where(d => d.DepartmentName != null && d.DepartmentName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync() => await LoadDepartments();

    private async Task LoadDepartments()
    {
        IsLoading = true;
        try
        {
            // API Endpoint (သင့် Controller တွင် 'department' ဟု သတ်မှတ်ထားသည်ဟု ယူဆပါသည်)
            var response = await HttpClientService.ExecuteAsync<List<DepartmentModel>>("department", EnumHttpMethod.Get);
            if (response != null) DepartmentList = response;
        }
        catch { }
        finally { IsLoading = false; }
    }

    private void OpenDeleteModal(DepartmentModel dept) { SelectedDepartment = dept; ShowModal = true; }
    private void CloseDeleteModal() { SelectedDepartment = null; ShowModal = false; }

    private async Task DeleteDepartment()
    {
        if (SelectedDepartment == null) return;
        IsProcessing = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<DepartmentResponseModel>($"department/{SelectedDepartment.DepartmentId}", EnumHttpMethod.Delete);
            if (response != null && response.IsSuccess)
            {
                await JSRuntime.InvokeVoidAsync("alert", response.Message ?? "အောင်မြင်စွာ ဖျက်သိမ်းပြီးပါပြီ။");
                CloseDeleteModal();
                await LoadDepartments();
            }
            else { await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "ဖျက်သိမ်း၍ မရပါ။"); }
        }
        catch (Exception ex) { await JSRuntime.InvokeVoidAsync("alert", $"Error: {ex.Message}"); }
        finally { IsProcessing = false; }
    }
}