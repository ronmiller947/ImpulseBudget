using System;
using System.ComponentModel.DataAnnotations;

namespace ImpulseBudget.Models
{
    public class BankTransactionImportRow
    {
        public bool Import { get; set; } = true;

        public bool IsDuplicate { get; set; }

        public DuplicateSeverity DuplicateSeverity { get; set; } = DuplicateSeverity.None;

        [Display(Name = "Date")]
        public DateTime Date { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Display(Name = "Type")]
        public string? Type { get; set; }

        [Display(Name = "Status")]
        public string? Status { get; set; }
    }
}
