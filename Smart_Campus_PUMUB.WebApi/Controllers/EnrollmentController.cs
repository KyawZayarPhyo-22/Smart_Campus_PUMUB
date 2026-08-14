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
    }
}
