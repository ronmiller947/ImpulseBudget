using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImpulseBudget.Models
{
    public class Bill
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        [Display(Name = "Bill name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Frequency")]
        public Frequency Frequency { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Next due date")]
        public DateTime NextDueDate { get; set; }

        [Display(Name = "Past due?")]
        public bool IsPastDue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Past due amount")]
        public decimal PastDueAmount { get; set; }

        [Display(Name = "Essential bill")]
        public bool IsEssential { get; set; } = true;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

}
