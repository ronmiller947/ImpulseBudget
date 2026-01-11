using System;

namespace ImpulseBudget.Models
{
    public class RecurringSuggestion
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int OccurrenceCount { get; set; }
        public DateTime FirstDate { get; set; }
        public DateTime LastDate { get; set; }

        // "Income", "Bill", or "Debt"
        public string SuggestedType { get; set; } = string.Empty;
    }
}
