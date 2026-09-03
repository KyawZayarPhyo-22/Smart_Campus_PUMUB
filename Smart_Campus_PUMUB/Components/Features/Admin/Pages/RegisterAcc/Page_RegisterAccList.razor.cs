#nullable enable
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Features.Admin.Pages.RegisterAcc;

public partial class Page_RegisterAccList : IDisposable
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] public Smart_Campus_PUMUB.Components.Features.Services.AdminLanguageService LangService { get; set; } = null!;

    // ── State ──────────────────────────────────────────────────────────────
    private List<RegisterAccListItem> Items { get; set; } = new();
    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; } = false;
    private string? ToastMessage { get; set; }
    private bool ToastIsSuccess { get; set; } = true;
    private string CurrentAdminName { get; set; } = "Admin";

    // ── Filter ─────────────────────────────────────────────────────────────
    private string SearchInput { get; set; } = "";
    private string SearchTerm { get; set; } = "";
    private string SelectedStatusInput { get; set; } = "All";
    private string SelectedStatus { get; set; } = "All";

    // ── Pagination ─────────────────────────────────────────────────────────
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;
    private int TotalCount { get; set; } = 0;

    // ── Approve Modal ──────────────────────────────────────────────────────
    private bool ShowApproveModal { get; set; } = false;
    private RegisterAccListItem? SelectedItem { get; set; }

    // ── Reject Modal ───────────────────────────────────────────────────────
    private bool ShowRejectModal { get; set; } = false;
    private string RejectReason { get; set; } = "";

    // ── Status Modal ───────────────────────────────────────────────────────
    private bool ShowStatusModal { get; set; } = false;

    protected override async Task OnInitializedAsync()
    {
        LangService.OnLanguageChanged += StateHasChanged;
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        CurrentAdminName = user.Identity?.Name ?? "Admin";

        await LoadData();
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

    private bool isStatusDropdownOpen = false;

    private void ToggleStatusDropdown()
    {
        isStatusDropdownOpen = !isStatusDropdownOpen;
    }

    private void SelectStatus(string status)
    {
        SelectedStatusInput = status;
        isStatusDropdownOpen = false;
    }

    private void CloseAllDropdowns()
    {
        isStatusDropdownOpen = false;
    }

    private async Task LoadData()
    {
        IsLoading = true;
        StateHasChanged();
        try
        {
            var delayTask = Task.Delay(500);
            var fetchTask = HttpClientService.ExecuteAsync<RegisterAccPagedResponse>(
                $"registeracc?pageNumber={CurrentPage}&pageSize={PageSize}" +
                $"&status={Uri.EscapeDataString(SelectedStatus)}" +
                $"&searchTerm={Uri.EscapeDataString(SearchTerm)}",
                EnumHttpMethod.Get
            );

            await Task.WhenAll(fetchTask, delayTask);
            var response = await fetchTask;

            if (response != null && response.IsSuccess)
            {
                Items = response.Items;
                TotalCount = response.TotalCount;
                TotalPages = response.TotalPages;
            }
        }
        catch (Exception ex)
        {
            ShowToast(LangService.IsMyanmar ? $"Data ဆွဲယူရာတွင် အမှားဖြစ်ပါသည်: {ex.Message}" : $"Failed to load data: {ex.Message}", false);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    // ── Filter Handlers ────────────────────────────────────────────────────
    private async Task ApplyFilter()
    {
        isStatusDropdownOpen = false;
        SearchTerm = SearchInput;
        SelectedStatus = SelectedStatusInput;
        CurrentPage = 1;
        await LoadData();
    }

    private async Task ResetFilter()
    {
        isStatusDropdownOpen = false;
        SearchInput = "";
        SearchTerm = "";
        SelectedStatusInput = "All";
        SelectedStatus = "All";
        CurrentPage = 1;
        await LoadData();
    }

    private async Task HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await ApplyFilter();
    }

    private async Task OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        await LoadData();
    }

    // ── Approve Flow ───────────────────────────────────────────────────────
    private void OpenApproveModal(RegisterAccListItem item)
    {
        SelectedItem = item;
        ShowApproveModal = true;
    }

    private void CloseApproveModal()
    {
        SelectedItem = null;
        ShowApproveModal = false;
    }

    private async Task ConfirmApprove()
    {
        if (SelectedItem == null) return;
        IsProcessing = true;
        try
        {
            var payload = new RegisterAccActionRequest
            {
                Status = "Approved",
                ReviewedBy = CurrentAdminName
            };

            var response = await HttpClientService.ExecuteAsync<RegisterAccActionResponse>(
                $"registeracc/{SelectedItem.RegisterAccId}/approve",
                EnumHttpMethod.Post,
                payload
            );

            if (response != null && response.IsSuccess)
            {
                ShowToast(response.Message ?? (LangService.IsMyanmar ? "အတည်ပြုပြီးပါပြီ။" : "Approved successfully."), true);
                CloseApproveModal();
                await LoadData();
            }
            else
            {
                ShowToast(response?.Message ?? (LangService.IsMyanmar ? "အတည်မပြုနိုင်ပါ။" : "Approval failed."), false);
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Error: {ex.Message}", false);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    // ── Reject Flow ────────────────────────────────────────────────────────
    private void OpenRejectModal(RegisterAccListItem item)
    {
        SelectedItem = item;
        RejectReason = "";
        ShowRejectModal = true;
    }

    private void CloseRejectModal()
    {
        SelectedItem = null;
        ShowRejectModal = false;
        RejectReason = "";
    }

    private async Task ConfirmReject()
    {
        if (SelectedItem == null) return;
        IsProcessing = true;
        try
        {
            var payload = new RegisterAccActionRequest
            {
                Status = "Rejected",
                RejectionReason = RejectReason,
                ReviewedBy = CurrentAdminName
            };

            var response = await HttpClientService.ExecuteAsync<RegisterAccActionResponse>(
                $"registeracc/{SelectedItem.RegisterAccId}/reject",
                EnumHttpMethod.Post,
                payload
            );

            if (response != null && response.IsSuccess)
            {
                ShowToast(LangService.IsMyanmar ? "ပယ်ချပြီးပါပြီ။" : (response.Message ?? "Rejected successfully."), true);
                CloseRejectModal();
                await LoadData();
            }
            else
            {
                ShowToast(LangService.IsMyanmar ? "ပယ်ချ၍ မရပါ။" : (response?.Message ?? "Rejection failed."), false);
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Error: {ex.Message}", false);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    // ── Status Flow ────────────────────────────────────────────────────────
    private void OpenStatusModal(RegisterAccListItem item)
    {
        SelectedItem = item;
        ShowStatusModal = true;
    }

    private void CloseStatusModal()
    {
        SelectedItem = null;
        ShowStatusModal = false;
    }

    private async Task ConfirmToggleStatus()
    {
        if (SelectedItem == null) return;
        var item = SelectedItem;
        CloseStatusModal();
        await ToggleStudentAccountStatus(item);
    }

    private async Task ToggleStudentAccountStatus(RegisterAccListItem item)
    {
        if (item == null) return;
        IsProcessing = true;
        try
        {
            var currentStatus = (item.AccountStatus ?? "Active").Trim();
            var nextStatus = string.Equals(currentStatus, "Active", StringComparison.OrdinalIgnoreCase) ? "Inactive" : "Active";

            var payload = new NewStudentAccUpdateStatusRequest
            {
                AccountStatus = nextStatus,
                ModifiedBy = CurrentAdminName
            };

            var response = await HttpClientService.ExecuteAsync<RegisterAccActionResponse>(
                $"registeracc/{item.RegisterAccId}/status",
                EnumHttpMethod.Put,
                payload
            );

            if (response != null)
            {
                if (response.IsSuccess)
                {
                    item.AccountStatus = nextStatus;
                    ShowToast(LangService.IsMyanmar ? $"အကောင့် အခြေအနေကို {nextStatus} သို့ ပြောင်းလဲပြီးပါပြီ။" : (response.Message ?? $"Account status updated to {nextStatus}"), true);
                    StateHasChanged();
                }
                else
                {
                    ShowToast(LangService.IsMyanmar ? "အခြေအနေ ပြောင်းလဲ၍ မရပါ။" : (response.Message ?? "Status update failed"), false);
                }
            }
            else
            {
                ShowToast("Server returned invalid response or connection failed.", false);
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Error: {ex.Message}", false);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    // ── Toast ──────────────────────────────────────────────────────────────
    private System.Threading.CancellationTokenSource? _toastCts;

    private void ShowToast(string message, bool success)
    {
        ToastMessage = message;
        ToastIsSuccess = success;
        StateHasChanged();

        _toastCts?.Cancel();
        _toastCts = new System.Threading.CancellationTokenSource();
        var token = _toastCts.Token;

        _ = Task.Delay(4000, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                InvokeAsync(() =>
                {
                    ToastMessage = null;
                    StateHasChanged();
                });
            }
        }, TaskScheduler.Default);
    }

    private static string StatusBadgeClass(string? status) => status switch
    {
        "Approved" => "badge-approved",
        "Rejected" => "badge-rejected",
        _ => "badge-pending"
    };

    public void Dispose()
    {
        LangService.OnLanguageChanged -= StateHasChanged;
    }
}
