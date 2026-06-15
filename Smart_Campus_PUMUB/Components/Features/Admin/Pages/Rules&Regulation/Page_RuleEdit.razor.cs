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

    // override စကားလုံးကို သေချာအောင် စစ်ပါ
    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
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
            StateHasChanged(); // UI ကို ပြန် refresh လုပ်ပေးရန်
        }
    }

    private async Task UpdateRule()
    {
        isProcessing = true;
        var response = await HttpClientService.ExecuteAsync<ActionResponseModel>($"rules/{Id}", EnumHttpMethod.Put, ruleModel);
        
        if (response != null && response.IsSuccess)
        {
            Nav.NavigateTo("/admin/rules");
        }
        isProcessing = false;
    }
}