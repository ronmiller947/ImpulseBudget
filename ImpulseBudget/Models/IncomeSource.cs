using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImpulseBudget.Models
{
    public class IncomeSource
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        [Display(Name = "Income name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount per occurrence")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Frequency")]
        public Frequency Frequency { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start date")]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "End date (optional)")]
        public DateTime? EndDate { get; set; }

        // Semi-monthly support
        [Range(1, 31, ErrorMessage = "Day must be between 1 and 31.")]
        [Display(Name = "Day of month (1st)")]
        public int? DayOfMonth1 { get; set; }

        [Range(1, 31, ErrorMessage = "Day must be between 1 and 31.")]
        [Display(Name = "Day of month (2nd)")]
        public int? DayOfMonth2 { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // If frequency is Monthly or SemiMonthly, days are relevant
            if (Frequency == Frequency.Monthly)
            {
                if (!DayOfMonth1.HasValue)
                {
                    yield return new ValidationResult(
                        "Day of month (1st) is required for monthly income.",
                        new[] { nameof(DayOfMonth1) });
                }
            }

            if (Frequency == Frequency.SemiMonthly)
            {
                if (!DayOfMonth1.HasValue)
                {
                    yield return new ValidationResult(
                        "Day of month (1st) is required for semi-monthly income.",
                        new[] { nameof(DayOfMonth1) });
                }

                if (!DayOfMonth2.HasValue)
                {
                    yield return new ValidationResult(
                        "Day of month (2nd) is required for semi-monthly income.",
                        new[] { nameof(DayOfMonth2) });
                }
            }

            // Optional: sanity check that DayOfMonth1 < DayOfMonth2 for semi-monthly
            if (Frequency == Frequency.SemiMonthly &&
                DayOfMonth1.HasValue && DayOfMonth2.HasValue &&
                DayOfMonth1.Value >= DayOfMonth2.Value)
            {
                yield return new ValidationResult(
                    "For semi-monthly income, the first day must be before the second day.",
                    new[] { nameof(DayOfMonth1), nameof(DayOfMonth2) });
            }
        }
    }
}
