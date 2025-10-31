using System.ComponentModel.DataAnnotations;

namespace V1_2025_07.Models;

public class Player
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = "";

    [MaxLength(120)]
    public string Email { get; set; } = "";

    [MaxLength(20)]
    public string Phone { get; set; } = "";

    [MaxLength(100)]
    public string Team { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
