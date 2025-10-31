using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using V1_2025_07.Models;

namespace V1_2025_07.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RuleController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public RuleController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IEnumerable<Rule>> Get() =>
        await _db.Rules.OrderByDescending(r => r.CreatedAt).ToListAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<Rule>> Get(int id)
    {
        var rule = await _db.Rules.FindAsync(id);
        return rule == null ? NotFound() : rule;
    }

    [HttpPost]
    public async Task<ActionResult<Rule>> Post(Rule rule)
    {
        _db.Rules.Add(rule);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = rule.Id }, rule);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, Rule rule)
    {
        if (id != rule.Id) return BadRequest();
        _db.Entry(rule).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var rule = await _db.Rules.FindAsync(id);
        if (rule == null) return NotFound();
        _db.Rules.Remove(rule);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
