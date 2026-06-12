using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Rules;

public partial class Page_RulesList
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<RuleModel> RulesList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private string ErrorMessage { get; set; } = "";
    private bool IsProcessing { get; set; } = false;
    private bool ShowModal { get; set; } = false;
    private RuleModel? SelectedRule { get; set; }

    // Search Logic: Rule Title ကို ရှာဖွေခြင်း
    private IEnumerable<RuleModel> FilteredRules => string.IsNullOrWhiteSpace(SearchTerm)
        ? RulesList
        : RulesList.Where(r => r.Title != null && r.Title.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        await LoadRules();
    }

    private async Task LoadRules()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            // API လမ်းကြောင်း "rules" ကို ခေါ်ယူခြင်း
            var response = await HttpClientService.ExecuteAsync<List<RuleModel>>("rules", EnumHttpMethod.Get);
            if (response != null) RulesList = response;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"ဒေတာဆွဲယူရာတွင် အမှားရှိပါသည်။ Error: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    private void OpenDeleteModal(RuleModel rule) { SelectedRule = rule; ShowModal = true; }
    private void CloseDeleteModal() { SelectedRule = null; ShowModal = false; }

    private async Task DeleteRule()
    {
        if (SelectedRule == null) return;
        IsProcessing = true;
        try
        {
            // Delete API Call (ID ကို ထည့်ပို့ပေးရန်)
            var response = await HttpClientService.ExecuteAsync<ActionResponseModel>($"rules/{SelectedRule.RuleId}", EnumHttpMethod.Delete);
            if (response != null && response.IsSuccess)
            {
                await JSRuntime.InvokeVoidAsync("alert", response.Message ?? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။");
                CloseDeleteModal();
                await LoadRules(); // Data ပြန်ခေါ်ခြင်း
            }
            else { await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "ဖျက်သိမ်း၍ မရပါ။"); }
        }
        catch (Exception ex) { await JSRuntime.InvokeVoidAsync("alert", $"Error: {ex.Message}"); }
        finally { IsProcessing = false; }
    }
}