using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImpulseBudget.Models
{
    public class Bill
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public Frequency Frequency { get; set; }

        [Required]
        public DateTime NextDueDate { get; set; }

        public bool IsPastDue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PastDueAmount { get; set; }

        public bool IsEssential { get; set; } = true;

        public bool IsActive { get; set; } = true;
    }
}
