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

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<RuleModel> GetFilteredRules() => string.IsNullOrWhiteSpace(SearchTerm)
        ? RulesList
        : RulesList.Where(r => r.Title != null && r.Title.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    private IEnumerable<RuleModel> FilteredRules
    {
        get
        {
            var allFiltered = GetFilteredRules();
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

    private string statusMessage = string.Empty;
    private bool IsSuccess = false;

    private void OpenDeleteModal(RuleModel rule)
    {
        SelectedRule = rule;
        ShowModal = true;

        statusMessage = string.Empty;
        IsSuccess = false;
    }

    private void CloseDeleteModal()
    {
        SelectedRule = null;
        ShowModal = false;

        statusMessage = string.Empty;
        IsSuccess = false;
    }

    private async Task DeleteRule()
    {
        if (SelectedRule == null) return;

        IsProcessing = true;

        statusMessage = "ဖျက်သိမ်းနေပါသည်...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<ActionResponseModel>(
                $"rules/{SelectedRule.RuleId}",
                EnumHttpMethod.Delete
            );

            if (response?.IsSuccess == true)
            {
                IsSuccess = true;
                statusMessage = response.Message ?? "Rule ကို အောင်မြင်စွာ ဖျက်ပြီးပါပြီ။";

                await LoadRules();

                await Task.Delay(800);
                CloseDeleteModal();
            }
            else
            {
                IsSuccess = false;
                statusMessage = response?.Message ?? "Rule ကို ဖျက်၍ မရပါ။";
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
        }
    }

    private string ShortDescription(string? text, int limit = 40)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        text = text.Trim();

        if (text.Length <= limit)
            return text;

        return text.Substring(0, limit) + "...";
    }
}