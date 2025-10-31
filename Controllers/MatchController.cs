using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using V1_2025_07.Models;
using V1_2025_07;

namespace Multiplayers.Admin.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MatchController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public MatchController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IEnumerable<Match>> Get() => await _db.Matches.ToListAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<Match>> Get(int id)
    {
        var match = await _db.Matches.FindAsync(id);
        return match == null ? NotFound() : match;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> Post([FromBody] Match match)
    {
        _db.Matches.Add(match);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = match.Id }, match);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> Put(int id, [FromBody] Match match)
    {
        if (id != match.Id) return BadRequest();
        _db.Entry(match).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> Delete(int id)
    {
        var match = await _db.Matches.FindAsync(id);
        if (match == null) return NotFound();
        _db.Matches.Remove(match);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
