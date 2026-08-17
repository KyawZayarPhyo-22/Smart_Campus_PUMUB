using Microsoft.AspNetCore.Mvc;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.WebApi.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnrollmentController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;

        public EnrollmentController(IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        public class EnrollmentRequest
        {
            public int StudentId { get; set; }
            public int SubjectId { get; set; }
            public int SemesterId { get; set; }
        }

        public class EnrollmentResponse : ActionResponseModel
        {
            public List<int>? MissingPrerequisites { get; set; }
        }

        [HttpPost("enroll")]
        public async Task<IActionResult> Enroll([FromBody] EnrollmentRequest request)
        {
            if (request.StudentId <= 0 || request.SubjectId <= 0 || request.SemesterId <= 0)
            {
                return BadRequest(new EnrollmentResponse { IsSuccess = false, Message = "Invalid input data." });
            }

            var (success, message, missing) = await _enrollmentService.EnrollStudentAsync(request.StudentId, request.SubjectId, request.SemesterId);

            var response = new EnrollmentResponse
            {
                IsSuccess = success,
                Message = message,
                MissingPrerequisites = missing
            };

            if (success)
                return Ok(response);
            else
                return BadRequest(response); // We use BadRequest to indicate business logic failure (prerequisites not met)
        }

        [HttpGet("student/{studentId}/results")]
        public async Task<IActionResult> GetStudentEnrollmentsWithResults(int studentId)
        {
            var results = await _enrollmentService.GetStudentEnrollmentsWithResultsAsync(studentId);
            return Ok(results);
        }

        [HttpGet("results")]
        public async Task<IActionResult> GetAllEnrollmentsWithResults()
        {
            var results = await _enrollmentService.GetAllEnrollmentsWithResultsAsync();
            return Ok(results);
        }

        [HttpGet("registration/{registrationId}/details")]
        public async Task<IActionResult> GetEnrollmentDetails(int registrationId)
        {
            var details = await _enrollmentService.GetEnrollmentDetailsAsync(registrationId);
            if (details == null)
                return NotFound(new { IsSuccess = false, Message = "Registration not found." });

            return Ok(details);
        }

        [HttpPost("save-student-grades")]
        public async Task<IActionResult> SaveStudentGrades([FromBody] SaveStudentGradesRequestModel request)
        {
            var response = await _enrollmentService.SaveStudentGradesAsync(request);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        [HttpGet("previous-grades")]
        public async Task<IActionResult> GetPreviousGrades([FromQuery] int? userId, [FromQuery] int? newStudentAccId, [FromQuery] string? rollNo, [FromQuery] int semesterId, [FromQuery] string? major)
        {
            var grades = await _enrollmentService.GetPreviousSemesterGradesAsync(userId, newStudentAccId, rollNo, semesterId, major);
            return Ok(grades);
        }

        [HttpGet("subjects-by-major")]
        public async Task<IActionResult> GetSubjectsByMajor([FromQuery] int semesterId, [FromQuery] string? major, [FromQuery] int? userId = null, [FromQuery] int? newStudentAccId = null, [FromQuery] string? rollNo = null)
        {
            var subjects = await _enrollmentService.GetSemesterSubjectsByMajorAsync(semesterId, major, userId, newStudentAccId, rollNo);
            return Ok(subjects);
        }
    }
}
