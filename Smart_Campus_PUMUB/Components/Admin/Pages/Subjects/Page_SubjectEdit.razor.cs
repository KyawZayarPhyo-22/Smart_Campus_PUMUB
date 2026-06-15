using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Subject;

public partial class Page_SubjectEdit
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public NavigationManager Nav { get; set; } = null!;
    [Parameter] public int SubjectId { get; set; }
    private string? ErrorMessage;
    private SubjectUpdateRequestModel subject = new();
    private List<SemesterModel> SemesterList = new();
    private bool IsProcessing = false;

    protected override async Task OnInitializedAsync()
    {
        // ၁။ Semester စာရင်း ဆွဲယူခြင်း
        SemesterList = await HttpClientService.ExecuteAsync<List<SemesterModel>>("semester", EnumHttpMethod.Get) ?? new();

        // ၂။ လက်ရှိ Subject အချက်အလက် ဆွဲယူခြင်း
        var data = await HttpClientService.ExecuteAsync<SubjectModel>($"subject/{SubjectId}", EnumHttpMethod.Get);
        if (data != null)
        {
            subject = new SubjectUpdateRequestModel
            {
                SemesterId = data.SemesterId,
                SubjectName = data.SubjectName,
                SubjectCode = data.SubjectCode
            };
        }
    }

    private async Task UpdateSubject()
    {
        ErrorMessage = null; // Error ကို အရင်ရှင်းပါ

        // ၁။ လက်ရှိ Semester ထဲက Subject စာရင်းကို အကုန်ဆွဲထုတ်ပါ
        var allSubjects = await HttpClientService.ExecuteAsync<List<SubjectModel>>("subject", EnumHttpMethod.Get) ?? new();

        // ၂။ ကိုယ့်ကိုယ်ကို (Current Subject) ဖယ်ပြီး တူတာရှိမရှိ စစ်ပါ
        bool isDuplicate = allSubjects.Any(s => s.SemesterId == subject.SemesterId
                                            && s.SubjectId != SubjectId // ကိုယ့်ကိုယ်ကို မစစ်ရန်
                                            && s.SubjectCode != null
                                            && subject.SubjectCode != null
                                            && s.SubjectCode.Trim().Equals(subject.SubjectCode.Trim(), StringComparison.OrdinalIgnoreCase));

        if (isDuplicate)
        {
            ErrorMessage = "ဤ Semester အတွင်းတွင် ဤ Subject Code သည် ရှိနှင့်ပြီးဖြစ်ပါသည်။";
            return;
        }

        // ၃။ Update လုပ်ခြင်း
        IsProcessing = true;
        var response = await HttpClientService.ExecuteAsync<ActionResponseModel>($"subject/{SubjectId}", EnumHttpMethod.Put, subject);

        if (response?.IsSuccess == true)
        {
            Nav.NavigateTo("/admin/subjects");
        }
        else
        {
            ErrorMessage = response?.Message ?? "Update လုပ်၍မရပါ။";
            IsProcessing = false;
        }
    }
}