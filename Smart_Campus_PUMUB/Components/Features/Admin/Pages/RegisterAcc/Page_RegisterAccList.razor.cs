using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Smart_Campus_PUMUB.Components.Features.Admin.Pages.RegisterAcc;

public partial class Page_RegisterAccList
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

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

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        CurrentAdminName = user.Identity?.Name ?? "Admin";

        await LoadData();
    }

    private async Task LoadData()
    {
        IsLoading = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<RegisterAccPagedResponse>(
                $"registeracc?pageNumber={CurrentPage}&pageSize={PageSize}" +
                $"&status={Uri.EscapeDataString(SelectedStatus)}" +
                $"&searchTerm={Uri.EscapeDataString(SearchTerm)}",
                EnumHttpMethod.Get
            );

            if (response != null && response.IsSuccess)
            {
                Items = response.Items;
                TotalCount = response.TotalCount;
                TotalPages = response.TotalPages;
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Data ဆွဲယူရာတွင် အမှားဖြစ်ပါသည်: {ex.Message}", false);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Filter Handlers ────────────────────────────────────────────────────
    private async Task ApplyFilter()
    {
        SearchTerm = SearchInput;
        SelectedStatus = SelectedStatusInput;
        CurrentPage = 1;
        await LoadData();
    }

    private async Task ResetFilter()
    {
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
                ShowToast(response.Message ?? "Approve လုပ်ပြီးပါပြီ။", true);
                CloseApproveModal();
                await LoadData();
            }
            else
            {
                ShowToast(response?.Message ?? "Approve မလုပ်နိုင်ပါ။", false);
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
                ShowToast(response.Message ?? "Reject လုပ်ပြီးပါပြီ။", true);
                CloseRejectModal();
                await LoadData();
            }
            else
            {
                ShowToast(response?.Message ?? "Reject မလုပ်နိုင်ပါ။", false);
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
}
