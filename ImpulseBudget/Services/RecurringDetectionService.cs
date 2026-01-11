using ImpulseBudget.Models;
using Microsoft.EntityFrameworkCore;

namespace ImpulseBudget.Services
{
    public class RecurringDetectionService
    {
        private readonly ApplicationDbContext _db;

        public RecurringDetectionService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<RecurringSuggestion>> FindRecurringAsync()
        {
            var txs = await _db.BankTransactions
                .OrderBy(t => t.Date)
                .ToListAsync();

            // Group by normalized description and sign (income vs expense)
            var groups = txs
                .GroupBy(t => new
                {
                    Desc = NormalizeDescription(t.Description),
                    Sign = t.Amount >= 0 ? "IN" : "OUT"
                });

            var suggestions = new List<RecurringSuggestion>();

            foreach (var g in groups)
            {
                var list = g.ToList();
                if (list.Count < 3)
                    continue; // not enough occurrences to treat as recurring

                // Require amounts to be "similar" (within a few cents)
                var amountAbs = list.Select(t => Math.Abs(t.Amount)).ToList();
                var min = amountAbs.Min();
                var max = amountAbs.Max();
                if (max - min > 1.00m) // more than 1.00 spread -> probably not fixed recurring
                    continue;

                var first = list.First();
                var last = list.Last();

                // Require span of at least 30 days
                if ((last.Date - first.Date).TotalDays < 30)
                    continue;

                var avgAmount = Math.Round(amountAbs.Average(), 2);
                var sign = g.Key.Sign == "IN" ? 1 : -1;
                var finalAmount = avgAmount * sign;

                var suggestedType = SuggestTypeFromGroup(g.Key.Sign, g.Key.Desc);

                suggestions.Add(new RecurringSuggestion
                {
                    Description = g.Key.Desc,
                    Amount = finalAmount,
                    OccurrenceCount = list.Count,
                    FirstDate = first.Date,
                    LastDate = last.Date,
                    SuggestedType = suggestedType
                });
            }

            return suggestions;
        }

        private string NormalizeDescription(string desc)
        {
            return (desc ?? string.Empty).Trim().ToUpperInvariant();
        }

        private string SuggestTypeFromGroup(string sign, string normalizedDesc)
        {
            if (sign == "IN")
            {
                return "Income";
            }

            // Very crude classifier: debt-like words
            if (normalizedDesc.Contains("CARD") ||
                normalizedDesc.Contains("LOAN") ||
                normalizedDesc.Contains("CAPITAL ONE") ||
                normalizedDesc.Contains("CHASE") ||
                normalizedDesc.Contains("DISCOVER"))
            {
                return "Debt";
            }

            return "Bill";
        }
    }
}
