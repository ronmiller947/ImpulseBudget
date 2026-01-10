using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImpulseBudget.Models
{
    public class Debt
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal AprPercent { get; set; } // 24.99

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumPayment { get; set; }

        [Required]
        public DateTime NextDueDate { get; set; }

        public bool IsEssential { get; set; } = false;
    }
}
