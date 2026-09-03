using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Filters;
using Smart_Campus_PUMUB.WebApi.Models;
namespace Smart_Campus_PUMUB.WebApi.Controllers;

[ApiController]
[Route("api/rules")]
public class RulesRegulationsController : ControllerBase
{
    private readonly SmartCampusDbContext _db;

    public RulesRegulationsController(SmartCampusDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetRules()
    {
         var lst = _db.RulesRegulations
                   .Where(r => r.IsDelete == false)
                   .OrderByDescending(r => r.CreatedDateTime) // အသစ်ဆုံးကို အရင်ပြရန်
                   .Select(r => new RuleModel {
                       RuleId = r.RuleId,
                       Title = r.Title,
                       Description = r.Description,
                       Penalty = r.Penalty,
                       CreatedDateTime = r.CreatedDateTime ?? DateTime.Now
                   }).ToList();
        return Ok(lst);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public IActionResult GetRule(int id)
    {
        if (id <= 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Id သည် သုညထက် ကြီးရပါမည်။" });

        var item = _db.RulesRegulations.FirstOrDefault(x => x.RuleId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "ဒေတာ ရှာမတွေ့ပါ။" });

        return Ok(item);
    }

    [HttpPost]
    [Permission("Rules.Create")]
    public IActionResult CreateRule([FromBody] RuleCreateRequestModel request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        _db.RulesRegulations.Add(new RulesRegulation
        {
            Title = request.Title,
            Description = request.Description,
            Penalty = request.Penalty,
            CreatedDateTime = DateTime.Now,
            CreatedBy = request.CreatedBy,
            IsDelete = false
        });

        int result = _db.SaveChanges();

        return StatusCode(201, new ActionResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "Saving Successful" : "Saving Failed"
        });
    }

    [HttpPut("{id}")]
    [Permission("Rules.Edit")]
    public IActionResult UpdateRule(int id,[FromBody] RuleUpdateRequestModel request)
    {
        if (id <= 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Id သည် သုညထက် ကြီးရပါမည်။" });
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var item = _db.RulesRegulations.FirstOrDefault(x => x.RuleId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "ပြင်ဆင်ရန် ဒေတာ ရှာမတွေ့ပါ။" });

        item.Title = request.Title;
        item.Description = request.Description;
        item.Penalty = request.Penalty;
        item.ModifiedDateTime = DateTime.Now;
        item.ModifiedBy = request.ModifiedBy;

        int result = _db.SaveChanges();

        return Ok(new RuleResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "Update Successful" : "Update Failed",
            Data = new RuleModel
            {
                RuleId = item.RuleId,
                Title = item.Title,
                Description = item.Description,
                Penalty = item.Penalty
            }
        });
    }

    [HttpDelete("{id}")]
    [Permission("Rules.Delete")]
    public IActionResult DeleteRule(int id)
    {
        if (id <= 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Id သည် သုညထက် ကြီးရပါမည်။" });

        var item = _db.RulesRegulations.FirstOrDefault(x => x.RuleId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "ဖျက်ရန် ဒေတာ ရှာမတွေ့ပါ။" });

        item.IsDelete = true; // Soft Delete
        int result = _db.SaveChanges();

        return Ok(new ActionResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "Delete Successfully" : "Delete Failed"
        });
    }

    [HttpGet("paginate")]
    [Permission("Rules.View")]
    public IActionResult GetRulesPaginated(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _db.RulesRegulations
            .AsNoTracking()
            .Where(x => x.IsDelete == false || x.IsDelete == null);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => (x.Title != null && x.Title.Contains(searchTerm)) || 
                                     (x.Description != null && x.Description.Contains(searchTerm)));
        }

        var totalCount = query.Count();

        var items = query
            .OrderByDescending(x => x.RuleId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RuleModel
            {
                RuleId = x.RuleId,
                Title = x.Title,
                Description = x.Description,
                Penalty = x.Penalty
            })
            .ToList();

        var result = new PagedResult<RuleModel>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Ok(result);
    }
}


