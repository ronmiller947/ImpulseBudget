using System.ComponentModel.DataAnnotations;

namespace ImpulseBudget.Models
{
    public enum Frequency
    {
        [Display(Name = "One-time")]
        OneTime = 0,

        [Display(Name = "Weekly")]
        Weekly = 1,

        [Display(Name = "Bi-weekly")]
        BiWeekly = 2,

        [Display(Name = "Semi-monthly")]
        SemiMonthly = 3,

        [Display(Name = "Monthly")]
        Monthly = 4,

        [Display(Name = "Yearly")]
        Yearly = 5
    }
}
