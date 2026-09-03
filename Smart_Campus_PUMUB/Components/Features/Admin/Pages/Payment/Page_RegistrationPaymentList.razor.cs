using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Payment;

public partial class Page_RegistrationPaymentList : IDisposable
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public Smart_Campus_PUMUB.Components.Features.Services.AdminLanguageService LangService { get; set; } = null!;

    private List<RegistrationPaymentModel> PaymentList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; } = false;
    private bool ShowModal { get; set; } = false;
    private RegistrationPaymentModel? SelectedPayment { get; set; }

    private string SearchInput = "";
    private string SelectedStatus = "";

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
        SelectedStatus = "";
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

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<RegistrationPaymentModel> GetFilteredPayments()
    {
        var filtered = PaymentList.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            filtered = filtered.Where(p => 
                (p.PaymentMethod != null && p.PaymentMethod.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                p.RegistrationId.ToString().Contains(SearchTerm)
            );
        }

        if (!string.IsNullOrEmpty(SelectedStatus))
        {
            filtered = filtered.Where(p => p.Status != null && p.Status.Equals(SelectedStatus, StringComparison.OrdinalIgnoreCase));
        }

        return filtered;
    }

    private IEnumerable<RegistrationPaymentModel> FilteredPayments
    {
        get
        {
            var allFiltered = GetFilteredPayments();
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
        LangService.OnLanguageChanged += StateHasChanged;
        await LoadPayments();
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

    private async Task LoadPayments()
    {
        IsLoading = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<RegistrationPaymentModel>>("registrationpayment", EnumHttpMethod.Get);
            if (response != null) PaymentList = response;
        }
        catch (Exception ex) { Console.WriteLine($"Error loading payments: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    private string statusMessage = string.Empty;
    private bool IsSuccess = false;

    private void OpenDeleteModal(RegistrationPaymentModel payment) 
    { 
        SelectedPayment = payment; 
        ShowModal = true; 
        statusMessage = string.Empty;
        IsSuccess = false;
    }
    
    private void CloseDeleteModal() 
    { 
        SelectedPayment = null; 
        ShowModal = false; 
        statusMessage = string.Empty;
        IsSuccess = false;
    }

    private async Task DeletePayment()
    {
        if (SelectedPayment == null) return;
        IsProcessing = true;
        statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းနေပါသည်..." : "Deleting...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<RegistrationPaymentResponseModel>($"registrationpayment/{SelectedPayment.PaymentId}", EnumHttpMethod.Delete);
            
            if (response != null && response.IsSuccess)
            {
                IsSuccess = true;
                statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။" : (response.Message ?? "Deleted successfully.");
                await LoadPayments();
                await Task.Delay(800);
                CloseDeleteModal();
            }
            else 
            { 
                IsSuccess = false;
                statusMessage = LangService.IsMyanmar ? "ဤ ငွေပေးချေမှုမှတ်တမ်း ကို ဖျက်၍ မရပါ။" : (response?.Message ?? "Cannot delete this payment record.");
            }
        }
        catch (Exception ex) 
        { 
            IsSuccess = false;
            statusMessage = $"Error: {ex.Message}";
        }
        finally { IsProcessing = false; }
    }

    public void Dispose()
    {
        LangService.OnLanguageChanged -= StateHasChanged;
    }
}