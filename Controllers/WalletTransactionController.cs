using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using V1_2025_07.Models;

namespace V1_2025_07.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WalletTransactionController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public WalletTransactionController(ApplicationDbContext db) => _db = db;

    // Get all transactions
    [HttpGet]
    public async Task<IEnumerable<WalletTransaction>> Get() =>
        await _db.WalletTransactions.OrderByDescending(t => t.Timestamp).ToListAsync();

    // Get transactions by player
    [HttpGet("player/{playerId}")]
    public async Task<IEnumerable<WalletTransaction>> GetByPlayer(int playerId) =>
        await _db.WalletTransactions.Where(t => t.PlayerId == playerId).OrderByDescending(t => t.Timestamp).ToListAsync();

    // Get all withdrawals
    [HttpGet("withdrawals")]
    public async Task<IEnumerable<WalletTransaction>> GetWithdrawals() =>
        await _db.WalletTransactions.Where(t => t.Type == "withdrawal").OrderByDescending(t => t.Timestamp).ToListAsync();

    // Add transaction (credit/debit/withdrawal)
    [HttpPost]
    public async Task<ActionResult<WalletTransaction>> Post(WalletTransaction transaction)
    {
        _db.WalletTransactions.Add(transaction);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = transaction.Id }, transaction);
    }
}
