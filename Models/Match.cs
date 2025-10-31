namespace V1_2025_07.Models
{
    using System.ComponentModel.DataAnnotations;

    public class Match
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = "";

        [Required, MaxLength(120)]
        public string Game { get; set; } = "";

        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string Status { get; set; } = "";

        // Add more fields as needed from your match tables (e.g. teams, result, etc)
    }

}
