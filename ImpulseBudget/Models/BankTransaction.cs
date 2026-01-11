using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImpulseBudget.Models
{
    public class BankTransaction
    {
        public int Id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [MaxLength(300)]
        public string Description { get; set; } = string.Empty;

        // Positive = money in, negative = money out
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [Display(Name = "Is Imported")]
        public bool IsImported { get; set; } = true;

        [Display(Name = "Incoming transaction")]
        public bool IsIncoming { get; set; } = false; // default to outgoing
    }
}
