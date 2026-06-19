// using Microsoft.AspNetCore.Components;
// using Microsoft.JSInterop;
// using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
// using Smart_Campus_PUMUB.WebApi.Models;

// namespace Smart_Campus_PUMUB.Components.Admin.Pages.Tutor;

// public partial class Page_TutorList
// {
//     [Inject] public HttpClientService HttpClientService { get; set; } = null!;
//     [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

//     private List<TutorModel> TutorList = new();
//     private string _searchTerm = "";
//     private bool IsLoading = true;
//     private bool IsProcessing = false;
    
//     // Delete Modal
//     private bool ShowModal = false;
//     private TutorModel? SelectedTutor;

//     // Search Logic with Property Setter
//     public string SearchTerm 
//     { 
//         get => _searchTerm; 
//         set { _searchTerm = value; StateHasChanged(); } 
//     }

//     private string SearchInput = "";
//     private string SelectedRoleInput = "All";
//     private string SelectedRole = "All";

//     private void ApplyFilter()
//     {
//         SearchTerm = SearchInput;
//         SelectedRole = SelectedRoleInput;
//         CurrentPage = 1;
//         StateHasChanged();
//     }

//     private void ResetFilter()
//     {
//         SearchInput = "";
//         SearchTerm = "";
//         SelectedRoleInput = "All";
//         SelectedRole = "All";
//         CurrentPage = 1;
//         StateHasChanged();
//     }

//     private void HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
//     {
//         if (e.Key == "Enter")
//         {
//             ApplyFilter();
//         }
//     }

//     protected override async Task OnInitializedAsync() => await LoadTutors();

//     private async Task LoadTutors()
//     {
//         IsLoading = true;
//         TutorList = await HttpClientService.ExecuteAsync<List<TutorModel>>("tutor", EnumHttpMethod.Get) ?? new();
//         IsLoading = false;
//     }

//     // Pagination Variables
//     private int CurrentPage { get; set; } = 1;
//     private int PageSize { get; set; } = 10;
//     private int TotalPages { get; set; } = 1;

//     private IEnumerable<TutorModel> GetFilteredTutors()
//     {
//         var list = TutorList.AsEnumerable();
//         if (!string.IsNullOrWhiteSpace(SearchTerm))
//         {
//             list = list.Where(t => t.TutorName != null && t.TutorName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
//         }
//         if (SelectedRole != "All")
//         {
//             list = list.Where(t => t.RoleName == SelectedRole);
//         }
//         return list;
//     }

//     private IEnumerable<TutorModel> FilteredTutors
//     {
//         get
//         {
//             var allFiltered = GetFilteredTutors();
//             int count = allFiltered.Count();
//             int calcPages = (int)Math.Ceiling((decimal)count / PageSize);
//             TotalPages = calcPages < 1 ? 1 : calcPages;
//             if (CurrentPage > TotalPages) CurrentPage = TotalPages;
//             return allFiltered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
//         }
//     }

//     private void OnPageChanged(int newPage)
//     {
//         CurrentPage = newPage;
//         StateHasChanged();
//     }

//     private void OpenDeleteModal(TutorModel tutor)
//     {
//         SelectedTutor = tutor;
//         ShowModal = true;
//     }

//     private async Task DeleteTutor()
//     {
//         if (SelectedTutor == null) return;
//         IsProcessing = true;

//         var response = await HttpClientService.ExecuteAsync<TutorDeleteResponseModel>(
//             $"tutor/{SelectedTutor.TutorId}", EnumHttpMethod.Delete);

//         if (response?.IsSuccess == true)
//         {
//             await LoadTutors();
//             ShowModal = false;
//         }
//         else
//         {
//             await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "Delete failed.");
//         }
//         IsProcessing = false;
//     }
// }
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Tutor;

public partial class Page_TutorList
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private List<TutorModel> TutorList = new();
    private string _searchTerm = "";
    private bool IsLoading = true;
    private bool IsProcessing = false;
    
    // Delete Modal
    private bool ShowModal = false;
    private TutorModel? SelectedTutor;

    // Search & Role Filter Logic
    public string SearchTerm 
    { 
        get => _searchTerm; 
        set { _searchTerm = value; StateHasChanged(); } 
    }

    private string SearchInput = "";
    private string SelectedRoleInput = "All";
    private string SelectedRole = "All";

    // Permissions Variables
    private List<string> userPermissions = new();
    private bool canManageTutor = false;

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    protected override async Task OnInitializedAsync()
    {
        // Only load data here - auth state is NOT yet readable during SSR
        await LoadTutors();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        // Re-read auth AFTER the SignalR circuit connects (ProtectedSessionStorage is now readable)
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            userPermissions = user.Claims
                                  .Where(c => c.Type == "Permission")
                                  .Select(c => c.Value)
                                  .ToList();
                                  
            // 💡 Permission ကို စစ်ဆေးပြီး Action Buttons များကို ထိန်းချုပ်ရန်
            canManageTutor = userPermissions.Contains("Edit_Tutor") || userPermissions.Contains("Delete_Tutor");

            // Trigger re-render so the table header and action buttons update
            StateHasChanged();
        }
    }

    private async Task LoadTutors()
    {
        IsLoading = true;
        TutorList = await HttpClientService.ExecuteAsync<List<TutorModel>>("tutor", EnumHttpMethod.Get) ?? new();
        IsLoading = false;
    }

    private void ApplyFilter()
    {
        SearchTerm = SearchInput;
        SelectedRole = SelectedRoleInput;
        CurrentPage = 1;
        StateHasChanged();
    }

    private void ResetFilter()
    {
        SearchInput = "";
        SearchTerm = "";
        SelectedRoleInput = "All";
        SelectedRole = "All";
        CurrentPage = 1;
        StateHasChanged();
    }

    private void HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            ApplyFilter();
        }
    }

    // 🌟 Filter Logic နှစ်ခုစလုံးကို အချိုးကျ ပေါင်းစပ်ထားသော Method
    private IEnumerable<TutorModel> GetFilteredTutors()
    {
        var list = TutorList.AsEnumerable();
        
        // ၁။ Name Search Query စစ်ခြင်း
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            list = list.Where(t => t.TutorName != null && t.TutorName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
        }
        
        // ၂။ Selected Role Filter စစ်ခြင်း
        if (SelectedRole != "All")
        {
            list = list.Where(t => t.RoleName == SelectedRole);
        }
        
        return list;
    }

    // 🌟 Filtered ပြထားသော Data ကို Pagination ဖြတ်ထုတ်ပေးသည့် နေရာ
    private IEnumerable<TutorModel> FilteredTutors
    {
        get
        {
            var allFiltered = GetFilteredTutors();
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

    private void OpenDeleteModal(TutorModel tutor)
    {
        SelectedTutor = tutor;
        ShowModal = true;
    }

    private async Task DeleteTutor()
    {
        if (SelectedTutor == null) return;
        IsProcessing = true;

        var response = await HttpClientService.ExecuteAsync<TutorDeleteResponseModel>(
            $"tutor/{SelectedTutor.TutorId}", EnumHttpMethod.Delete);

        if (response?.IsSuccess == true)
        {
            await LoadTutors();
            ShowModal = false;
        }
        else
        {
            await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "Delete failed.");
        }
        IsProcessing = false;
    }
}