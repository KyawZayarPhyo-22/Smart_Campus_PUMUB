using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.Components.Features.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Features.Admin.Pages.Student;

public partial class Page_StudentCreate : ComponentBase, IDisposable
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public NavigationManager Nav { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public AdminLanguageService LangService { get; set; } = null!;

    private StudentCreateRequestModel createModel = new();
    private List<UserModels> UserList = new();
    private bool isProcessing = false;
    private string statusMessage = "";

    protected override async Task OnInitializedAsync()
    {
        LangService.OnLanguageChanged += HandleLanguageChanged;
        // User စာရင်းဆွဲယူရန်
        UserList = await HttpClientService.ExecuteAsync<List<UserModels>>("user", EnumHttpMethod.Get) ?? new();
    }

    private void HandleLanguageChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        LangService.OnLanguageChanged -= HandleLanguageChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                var savedLang = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "admin_dashboard_lang");
                if (!string.IsNullOrEmpty(savedLang) && savedLang != LangService.CurrentLanguage)
                {
                    LangService.SetLanguage(savedLang);
                    StateHasChanged();
                }
            }
            catch { }
        }
    }

    private async Task SaveStudent()
    {
        if (createModel.UserId == 0) 
        {
            statusMessage = "User အကောင့်ကို ရွေးချယ်ပေးပါ။";
            return;
        }

        isProcessing = true;
        statusMessage = "Saving...";

        try
        {
            // Student Creation သည် File မပါပါက JSON အနေဖြင့် တိုက်ရိုက်ပို့နိုင်သည်
            var response = await HttpClientService.ExecuteAsync<StudentResponseModel>("student", EnumHttpMethod.Post, createModel);

            if (response?.IsSuccess == true)
            {
                await JSRuntime.InvokeVoidAsync("alert", "Student Created Successfully!");
                Nav.NavigateTo("/admin/student");
            }
            else
            {
                statusMessage = response?.Message ?? "သိမ်းဆည်းမှု မအောင်မြင်ပါ။";
            }
        }
        catch (Exception ex)
        {
            statusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
        }
    }
}