using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using V1_2025_07.Models;

namespace V1_2025_07.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PlayerController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PlayerController(ApplicationDbContext db) => _db = db;

    // GET: api/Player
    [HttpGet]
    public async Task<IEnumerable<Player>> Get() =>
        await _db.Players.OrderByDescending(p => p.CreatedAt).ToListAsync();

    // GET: api/Player/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Player>> Get(int id)
    {
        var player = await _db.Players.FindAsync(id);
        return player == null ? NotFound() : player;
    }

    // POST: api/Player
    [HttpPost]
    public async Task<ActionResult<Player>> Post(Player player)
    {
        _db.Players.Add(player);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = player.Id }, player);
    }

    // PUT: api/Player/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, Player player)
    {
        if (id != player.Id) return BadRequest();
        _db.Entry(player).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/Player/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var player = await _db.Players.FindAsync(id);
        if (player == null) return NotFound();
        _db.Players.Remove(player);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
