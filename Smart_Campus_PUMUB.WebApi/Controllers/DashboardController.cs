using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;
using System.IO;

namespace NLADotNetInternshipTraining.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    private readonly SmartCampusDbContext _db;

    public HomeController(SmartCampusDbContext db)
    {
        _db = db;
    }

    // ဒီနေရာမှာ ထည့်ပါ
    [HttpGet("summary")]
    public IActionResult GetSystemSummary()
    {
        var summary = new
        {
            Faculties = _db.Faculties.Count(x => x.IsDelete == false),
            Departments = _db.Departments.Count(x => x.IsDelete == false),
            Subjects = _db.Subjects.Count(x => x.IsDelete == false),
            Semesters = _db.Semesters.Count(x => x.IsDelete == false),
            Positions = _db.Positions.Count(x => x.IsDelete == false),
            Rules = _db.RulesRegulations.Count(x => x.IsDelete == false),

        };
        return Ok(summary);
    }
}