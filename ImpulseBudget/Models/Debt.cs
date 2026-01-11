using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImpulseBudget.Models
{
    public class Debt
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        [Display(Name = "Debt name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Current balance")]
        public decimal Balance { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "APR (%)")]
        public decimal AprPercent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Minimum payment")]
        public decimal MinimumPayment { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Next due date")]
        public DateTime NextDueDate { get; set; }

        [Display(Name = "Essential debt")]
        public bool IsEssential { get; set; } = false;
    }

}
