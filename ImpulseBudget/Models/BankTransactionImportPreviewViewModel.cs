using System.Collections.Generic;

namespace ImpulseBudget.Models
{
    public class BankTransactionImportPreviewViewModel
    {
        public List<BankTransactionImportRow> Rows { get; set; } = new();

        // For messages at top of preview
        public string? SourceLabel { get; set; }
    }
}
