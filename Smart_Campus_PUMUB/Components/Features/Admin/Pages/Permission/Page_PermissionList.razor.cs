using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Permission
{
    public partial class Page_PermissionList : IDisposable
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = null!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
        [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] public Smart_Campus_PUMUB.Components.Features.Services.AdminLanguageService LangService { get; set; } = null!;

        private List<PermissionModel> PermissionList { get; set; } = new();
        private string SearchTerm { get; set; } = "";
        private bool IsLoading { get; set; } = true;
        private string ErrorMessage { get; set; } = "";
        private bool IsProcessing { get; set; } = false;

        private string SearchInput = "";

        private async Task ApplyFilter()
        {
            SearchTerm = SearchInput;
            CurrentPage = 1;
            await LoadPermissions();
        }

        private async Task ResetFilter()
        {
            SearchInput = "";
            SearchTerm = "";
            CurrentPage = 1;
            await LoadPermissions();
        }

        private async Task HandleKeyUp(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await ApplyFilter();
            }
        }

        private bool ShowModal { get; set; } = false;
        private PermissionModel? SelectedPermission { get; set; }

        // Pagination Variables
        private int CurrentPage { get; set; } = 1;
        private int PageSize { get; set; } = 10;
        private int TotalPages { get; set; } = 1;

        private IEnumerable<PermissionModel> FilteredPermissions => PermissionList;

        private async Task OnPageChanged(int newPage)
        {
            CurrentPage = newPage;
            await LoadPermissions();
        }

        protected override async Task OnInitializedAsync()
        {
            LangService.OnLanguageChanged += StateHasChanged;
            await LoadPermissions();
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
        }

        private async Task LoadPermissions()
        {
            IsLoading = true;
            ErrorMessage = "";
            StateHasChanged();
            try
            {
                var delayTask = Task.Delay(500);
                var fetchTask = HttpClientService.ExecuteAsync<PagedResult<PermissionModel>>(
                    $"permission/paginate?pageNumber={CurrentPage}&pageSize={PageSize}&searchTerm={Uri.EscapeDataString(SearchTerm)}",
                    EnumHttpMethod.Get
                );

                await Task.WhenAll(fetchTask, delayTask);
                var response = await fetchTask;
                if (response != null)
                {
                    PermissionList = response.Items;
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

        private void OpenDeleteModal(PermissionModel permission)
        {
            SelectedPermission = permission;
            ShowModal = true;
            statusMessage = string.Empty;
            IsSuccess = false;
        }

        private void CloseDeleteModal()
        {
            SelectedPermission = null;
            ShowModal = false;
            statusMessage = string.Empty;
            IsSuccess = false;
        }

        private async Task DeletePermission()
        {
            if (SelectedPermission == null) return;

            IsProcessing = true;
            statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းနေပါသည်..." : "Deleting...";

            try
            {
                var response = await HttpClientService.ExecuteAsync<PermissionDeleteResponseModel>(
                    $"permission/{SelectedPermission.PermissionId}",
                    EnumHttpMethod.Delete
                );

                if (response != null && response.IsSuccess)
                {
                    IsSuccess = true;
                    statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။" : (response.Message ?? "Deleted successfully.");
                    StateHasChanged();

                    await Task.Delay(800);
                    CloseDeleteModal();
                    await LoadPermissions();
                }
                else
                {
                    IsSuccess = false;
                    statusMessage = LangService.IsMyanmar ? "ဤ လုပ်ပိုင်ခွင့် ကို ဖျက်၍ မရပါ။" : (response?.Message ?? "Cannot delete this permission.");
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
                StateHasChanged();
            }
        }

        public void Dispose()
        {
            LangService.OnLanguageChanged -= StateHasChanged;
        }
    }
}
