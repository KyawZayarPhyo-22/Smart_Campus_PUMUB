using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Subject;

public partial class Page_SubjectCreate
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public NavigationManager Nav { get; set; } = null!;

    private SubjectCreateRequestModel subject = new();
    private List<SemesterModel> SemesterList = new();
    private List<FacultyModel> FacultyList = new();

    private bool IsProcessing = false;
    private string? ErrorMessage;

    protected override async Task OnInitializedAsync()
    {
        SemesterList = await HttpClientService.ExecuteAsync<List<SemesterModel>>("semester", EnumHttpMethod.Get) ?? new();
        FacultyList  = await HttpClientService.ExecuteAsync<List<FacultyModel>>("faculty", EnumHttpMethod.Get) ?? new();
    }

    private async Task SaveSubject()
    {
        ErrorMessage = null;

        IsProcessing = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<SubjectResponseModel>("subject", EnumHttpMethod.Post, subject);

            if (response != null && response.IsSuccess)
            {
                Nav.NavigateTo("/admin/subjects");
            }
            else
            {
                ErrorMessage = response?.Message ?? "သိမ်းဆည်း၍မရပါ။ စနစ်တွင် အမှားအယွင်းရှိနေပါသည်။";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
}