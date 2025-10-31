using System.ComponentModel.DataAnnotations;

namespace V1_2025_07.Models;

public class WalletTransaction
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PlayerId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required, MaxLength(16)]
    public string Type { get; set; } = ""; // "credit" or "debit" or "withdrawal"

    [MaxLength(250)]
    public string? Description { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
