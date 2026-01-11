using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImpulseBudget.Models
{
    public class BudgetSettings
    {
        public int Id { get; set; }

        [Display(Name = "Starting Balance")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal StartingBalance { get; set; }

        [Display(Name = "Starting Balance Date")]
        [DataType(DataType.Date)]
        public DateTime StartingBalanceDate { get; set; }
    }
}
