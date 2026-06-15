using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Rules;

public partial class Page_RuleCreate
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public NavigationManager Nav { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private RuleCreateRequestModel ruleModel = new();
    private bool isProcessing = false;
    private string ErrorMessage = "";

    private async Task SaveRule()
    {
        isProcessing = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<ActionResponseModel>("rules", EnumHttpMethod.Post, ruleModel);

            if (response != null && response.IsSuccess)
            {
                // Navigational သာ လုပ်ပါ
                Nav.NavigateTo("/admin/rules");
            }
            else
            {
                // Error Message ကို UI မှာပဲ ပြပါ (JS alert မသုံးပါနဲ့)
                ErrorMessage = response?.Message ?? "သိမ်းဆည်းရာတွင် အမှားဖြစ်ပွားပါသည်။";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
        }
    }
}