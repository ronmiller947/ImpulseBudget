using ImpulseBudget.Models;
using Microsoft.EntityFrameworkCore;

namespace ImpulseBudget.Services
{
    public class BalanceService
    {
        private readonly ApplicationDbContext _db;

        public BalanceService(ApplicationDbContext db)
        {
            _db = db;
        }

        // Current balance "right now"
        public async Task<decimal> GetCurrentBalanceAsync()
        {
            var settings = await _db.BudgetSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                // No settings saved yet
                return 0m;
            }

            var startDate = settings.StartingBalanceDate.Date;

            var txSum = await _db.BankTransactions
                .Where(t => t.Date >= startDate)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            return settings.StartingBalance + txSum;
        }

        // Balance as of a given date (for projections)
        public async Task<decimal> GetBalanceAsOfAsync(DateTime asOf)
        {
            var settings = await _db.BudgetSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                return 0m;
            }

            var startDate = settings.StartingBalanceDate.Date;
            var endDate = asOf.Date;

            var txSum = await _db.BankTransactions
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            return settings.StartingBalance + txSum;
        }
    }
}
