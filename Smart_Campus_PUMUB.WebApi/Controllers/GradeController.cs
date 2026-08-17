using Microsoft.AspNetCore.Mvc;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.WebApi.Services;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GradeController : ControllerBase
{
    private readonly IGradeService _gradeService;

    public GradeController(IGradeService gradeService)
    {
        _gradeService = gradeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetGrades()
    {
        var grades = await _gradeService.GetGrades();
        return Ok(grades);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGrade(int id)
    {


        var grade = await _gradeService.GetGrade(id);
        if (grade == null)
            return NotFound(new { IsSuccess = false, Message = "No data found." });
            
        return Ok(grade);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGrade([FromBody] GradeRequestModel reqModel)
    {
        var response = await _gradeService.CreateGrade(reqModel);
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGrade(int id, [FromBody] GradeRequestModel reqModel)
    {
        var response = await _gradeService.UpdateGrade(id, reqModel);
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGrade(int id)
    {
        var response = await _gradeService.DeleteGrade(id);
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }
}
