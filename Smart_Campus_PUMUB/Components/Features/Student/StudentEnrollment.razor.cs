using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Features.Student
{
    public partial class StudentEnrollment
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = null!;

        private int SelectedStudentId { get; set; }
        private int SelectedSemesterId { get; set; }
        private int SelectedSubjectId { get; set; }

        private List<StudentModel> StudentList = new();
        private List<SemesterModel> SemesterList = new();
        private List<SubjectModel> SubjectList = new();

        private bool IsProcessing = false;
        private string? ErrorMessage;
        private string? SuccessMessage;
        private List<int>? MissingPrerequisites;

        // Note: For simplicity, we create a small class here instead of a separate file
        public class StudentModel
        {
            public int StudentId { get; set; }
            public string? StudentName { get; set; }
            public string? EnrollmentNo { get; set; }
        }

        public class EnrollmentRequest
        {
            public int StudentId { get; set; }
            public int SubjectId { get; set; }
            public int SemesterId { get; set; }
        }

        public class EnrollmentResponse
        {
            public bool IsSuccess { get; set; }
            public string? Message { get; set; }
            public List<int>? MissingPrerequisites { get; set; }
        }

        protected override async Task OnInitializedAsync()
        {
            var semTask = HttpClientService.ExecuteAsync<List<SemesterModel>>("semester", EnumHttpMethod.Get);
            var subTask = HttpClientService.ExecuteAsync<List<SubjectModel>>("subject", EnumHttpMethod.Get);
            
            // Assuming there is a student endpoint, let's fetch students. If it fails, StudentList stays empty.
            var stuTask = HttpClientService.ExecuteAsync<List<StudentModel>>("student", EnumHttpMethod.Get);

            await Task.WhenAll(semTask, subTask, stuTask);

            SemesterList = semTask.Result ?? new();
            SubjectList = subTask.Result ?? new();
            StudentList = stuTask.Result ?? new();
        }

        private async Task EnrollStudent()
        {
            ErrorMessage = null;
            SuccessMessage = null;
            MissingPrerequisites = null;

            if (SelectedStudentId <= 0 || SelectedSemesterId <= 0 || SelectedSubjectId <= 0)
            {
                ErrorMessage = "ကျေးဇူးပြု၍ ကျောင်းသား၊ Semester နှင့် ဘာသာရပ်ကို ရွေးချယ်ပေးပါ။";
                return;
            }

            IsProcessing = true;

            var request = new EnrollmentRequest
            {
                StudentId = SelectedStudentId,
                SemesterId = SelectedSemesterId,
                SubjectId = SelectedSubjectId
            };

            var response = await HttpClientService.ExecuteAsync<EnrollmentResponse>("enrollment/enroll", EnumHttpMethod.Post, request);

            if (response != null && response.IsSuccess)
            {
                SuccessMessage = response.Message ?? "ဘာသာရပ် အောင်မြင်စွာ စာရင်းသွင်းပြီးပါပြီ။";
                SelectedSubjectId = 0; // Reset subject selection
            }
            else
            {
                ErrorMessage = response?.Message ?? "ဘာသာရပ် စာရင်းသွင်းခြင်း မအောင်မြင်ပါ။";
                MissingPrerequisites = response?.MissingPrerequisites;
            }

            IsProcessing = false;
        }
        
        private string GetSubjectName(int id)
        {
            var sub = SubjectList.FirstOrDefault(s => s.SubjectId == id);
            return sub != null ? $"{sub.SubjectCode} - {sub.SubjectName}" : $"Unknown Subject ID: {id}";
        }
    }
}
