using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Activity;

public partial class Page_ActivityList
{
    private bool confirmed;

    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<ActivityModel> ActivityList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private string ErrorMessage { get; set; } = "";
    private bool IsProcessing { get; set; } = false;

    //private string statusMessage;

    private bool ShowModal { get; set; } = false;
    private ActivityModel? SelectedActivity { get; set; }

    private IEnumerable<ActivityModel> FilteredActivities => string.IsNullOrWhiteSpace(SearchTerm)
        ? ActivityList
        : ActivityList.Where(a => a.ActivityTitle != null && a.ActivityTitle.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        await LoadActivities();
    }

    private async Task LoadActivities()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<ActivityModel>>("activity", EnumHttpMethod.Get);
            if (response != null) ActivityList = response;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"ဒေတာဆွဲယူရာတွင် အမှားရှိပါသည်။ Error: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    private string statusMessage = string.Empty;
    private bool IsSuccess = false;

    private void OpenDeleteModal(ActivityModel activity)
    {
        SelectedActivity = activity;
        ShowModal = true;

        statusMessage = string.Empty;
        IsSuccess = false;
    }

    private void CloseDeleteModal()
    {
        SelectedActivity = null;
        ShowModal = false;

        statusMessage = string.Empty;
        IsSuccess = false;
    }

    private async Task DeleteActivity()
    {
        if (SelectedActivity == null) return;

        IsProcessing = true;

        // UI reset + loading message
        statusMessage = "ဖျက်သိမ်းနေပါသည်...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<ActivityDeleteResponseModel>(
                $"activity/{SelectedActivity.ActivityId}",
                EnumHttpMethod.Delete
            );

            if (response?.IsSuccess == true)
            {
                IsSuccess = true;
                statusMessage = response.Message ?? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။";

                await LoadActivities();

                await Task.Delay(800); // show success message briefly
                CloseDeleteModal();
            }
            else
            {
                IsSuccess = false;
                statusMessage = response?.Message ?? "ဖျက်သိမ်း၍ မရပါ။";
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
}