using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Rules;

// အရေးကြီး: : ComponentBase ကို ထည့်ပေးရပါမယ်
public partial class Page_RuleEdit : ComponentBase 
{
    [Parameter] 
    public int Id { get; set; }
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public NavigationManager Nav { get; set; } = null!;

    private RuleUpdateRequestModel ruleModel = new();
    private bool isProcessing = false;
    private bool isLoaded = false;
    private string ErrorMessage = "";

    protected override async Task OnParametersSetAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        isLoaded = false;
        ErrorMessage = "";
        try
        {
            var data = await HttpClientService.ExecuteAsync<RuleModel>($"rules/{Id}", EnumHttpMethod.Get);
            if (data != null)
            {
                ruleModel = new RuleUpdateRequestModel
                {
                    Title = data.Title,
                    Description = data.Description,
                    Penalty = data.Penalty
                };
                isLoaded = true;
            }
            else
            {
                ErrorMessage = "ပြင်ဆင်ရန် စည်းကမ်းချက် အချက်အလက်များ ရှာမတွေ့ပါ။";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            StateHasChanged();
        }
    }

    private async Task UpdateRule()
    {
        if (isProcessing) return;
        isProcessing = true;
        ErrorMessage = "";

        try
        {
            var response = await HttpClientService.ExecuteAsync<ActionResponseModel>($"rules/{Id}", EnumHttpMethod.Put, ruleModel);
            
            if (response != null && response.IsSuccess)
            {
                Nav.NavigateTo("/admin/rules");
            }
            else
            {
                ErrorMessage = response?.Message ?? "ပြင်ဆင်မှု မအောင်မြင်ပါ။";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
            StateHasChanged();
        }
    }
}