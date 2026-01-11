using ImpulseBudget.Models;
using Microsoft.EntityFrameworkCore;

namespace ImpulseBudget.Services
{
    public class BudgetProjectionService
    {
        private readonly ApplicationDbContext _db;

        public BudgetProjectionService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<ProjectionPoint>> GetWeeklyProjectionAsync(
            decimal startingBalance,
            int weeks = 26)
        {
            // Load data from DB
            var incomes = await _db.IncomeSources
                .Where(i => i.IsActive)
                .ToListAsync();

            var bills = await _db.Bills
                .Where(b => b.IsActive)
                .ToListAsync();

            var debts = await _db.Debts       // <-- we'll fix this typo below
                .ToListAsync();

            // We'll track debt balances in memory so we don't touch DB
            var debtSimState = debts.ToDictionary(
                d => d.Id,
                d => new DebtSimState
                {
                    Balance = d.Balance,
                    NextDueDate = d.NextDueDate,
                    MinimumPayment = d.MinimumPayment
                });

            var results = new List<ProjectionPoint>();

            var today = DateTime.Today;
            var balance = startingBalance;

            // Align the first week to today (you could also snap to Sunday using StartOfWeek)
            for (int i = 0; i < weeks; i++)
            {
                var weekStart = today.AddDays(i * 7);
                var weekEnd = weekStart.AddDays(6);

                var weekIncome = GetWeekIncome(incomes, weekStart, weekEnd);
                var weekBills = GetWeekBills(bills, weekStart, weekEnd);

                var weekDebtPayments = GetWeekDebtPayments(debts, debtSimState, weekStart, weekEnd);

                var point = new ProjectionPoint
                {
                    WeekStart = weekStart,
                    WeekEnd = weekEnd,
                    StartingBalance = balance,
                    TotalIncome = weekIncome,
                    TotalBills = weekBills,
                    TotalDebtPayments = weekDebtPayments,
                    EndingBalance = balance + weekIncome - weekBills - weekDebtPayments
                };

                results.Add(point);
                balance = point.EndingBalance;
            }

            return results;
        }

        private decimal GetWeekIncome(List<IncomeSource> incomes, DateTime weekStart, DateTime weekEnd)
        {
            decimal total = 0;

            foreach (var income in incomes)
            {
                foreach (var date in GenerateOccurrences(
                             income.Frequency,
                             income.StartDate,
                             income.EndDate,
                             income.DayOfMonth1,
                             income.DayOfMonth2,
                             weekStart,
                             weekEnd))
                {
                    total += income.Amount;
                }
            }

            return total;
        }

        private decimal GetWeekBills(List<Bill> bills, DateTime weekStart, DateTime weekEnd)
        {
            decimal total = 0;

            foreach (var bill in bills)
            {
                foreach (var date in GenerateOccurrences(
                             bill.Frequency,
                             bill.NextDueDate,
                             null,
                             null,
                             null,
                             weekStart,
                             weekEnd))
                {
                    total += bill.Amount;

                    if (bill.IsPastDue && bill.PastDueAmount > 0)
                    {
                        total += bill.PastDueAmount;
                        // For now, assume past due is cleared once in the first week it's paid
                        bill.IsPastDue = false;
                        bill.PastDueAmount = 0;
                    }
                }
            }

            return total;
        }

        private decimal GetWeekDebtPayments(
            List<Debt> debts,
            Dictionary<int, DebtSimState> simState,
            DateTime weekStart,
            DateTime weekEnd)
        {
            decimal total = 0;

            foreach (var debt in debts)
            {
                var state = simState[debt.Id];

                // treat debts as monthly payments on their NextDueDate
                var nextDue = (DateTime)state.NextDueDate;
                var balance = (decimal)state.Balance;
                var minPayment = (decimal)state.MinimumPayment;

                if (nextDue >= weekStart && nextDue <= weekEnd && balance > 0)
                {
                    var payment = Math.Min(minPayment, balance);
                    total += payment;

                    // update in-memory state
                    simState[debt.Id] = new DebtSimState
                    {
                        Balance = balance - payment,
                        NextDueDate = nextDue.AddMonths(1),
                        MinimumPayment = minPayment
                    };
                }
            }

            return total;
        }

        private IEnumerable<DateTime> GenerateOccurrences(
    Frequency frequency,
    DateTime firstDate,
    DateTime? endDate,
    int? day1,
    int? day2,
    DateTime rangeStart,
    DateTime rangeEnd)
        {
            var result = new List<DateTime>();

            if (endDate.HasValue && endDate.Value < rangeStart)
                return result;

            var current = firstDate;

            // If we have day1 configured, normalize firstDate to that day in its month
            if (day1.HasValue && frequency is Frequency.Monthly or Frequency.SemiMonthly)
            {
                var dim = DateTime.DaysInMonth(current.Year, current.Month);
                var targetDay = Math.Min(day1.Value, dim);
                current = new DateTime(current.Year, current.Month, targetDay);
            }

            // Bring current up to the rangeStart if needed
            if (current < rangeStart)
            {
                current = AdjustToFirstOccurrenceOnOrAfter(
                    frequency, current, endDate, day1, day2, rangeStart);
            }

            while (current <= rangeEnd &&
                   (!endDate.HasValue || current <= endDate.Value))
            {
                if (current >= rangeStart && current <= rangeEnd)
                    result.Add(current);

                current = frequency switch
                {
                    Frequency.Weekly => current.AddDays(7),
                    Frequency.BiWeekly => current.AddDays(14),
                    Frequency.Monthly => NextMonthly(current, day1),
                    Frequency.Yearly => current.AddYears(1),
                    Frequency.SemiMonthly => NextSemiMonthly(current, day1, day2),
                    Frequency.OneTime => DateTime.MaxValue,
                    _ => DateTime.MaxValue
                };

                if (frequency == Frequency.OneTime)
                    break;
            }

            return result;
        }

        private DateTime NextMonthly(DateTime current, int? day1)
        {
            // Move to next month on chosen day, clamped to last day of that month
            var nextMonth = current.AddMonths(1);
            var dim = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
            var targetDay = day1.HasValue ? Math.Min(day1.Value, dim) : Math.Min(current.Day, dim);
            return new DateTime(nextMonth.Year, nextMonth.Month, targetDay);
        }

        private DateTime AdjustToFirstOccurrenceOnOrAfter(
            Frequency frequency,
            DateTime firstDate,
            DateTime? endDate,
            int? day1,
            int? day2,
            DateTime rangeStart)
        {
            var current = firstDate;

            if (frequency == Frequency.OneTime)
            {
                return firstDate >= rangeStart ? firstDate : DateTime.MaxValue;
            }

            while (current < rangeStart &&
                   (!endDate.HasValue || current <= endDate.Value))
            {
                current = frequency switch
                {
                    Frequency.Weekly => current.AddDays(7),
                    Frequency.BiWeekly => current.AddDays(14),
                    Frequency.Monthly => current.AddMonths(1),
                    Frequency.Yearly => current.AddYears(1),
                    Frequency.SemiMonthly => NextSemiMonthly(current, day1, day2),
                    _ => DateTime.MaxValue
                };
            }

            return current;
        }

        private DateTime NextSemiMonthly(DateTime current, int? day1, int? day2)
        {
            day1 ??= 1;
            day2 ??= 15;

            var year = current.Year;
            var month = current.Month;

            int dim = DateTime.DaysInMonth(year, month);
            int d1 = Math.Min(day1.Value, dim);
            int d2 = Math.Min(day2.Value, dim);

            // If we haven't hit the first day yet, go there
            if (current.Day < d1)
            {
                return new DateTime(year, month, d1);
            }

            // If we are between first and second, go to second
            if (current.Day < d2)
            {
                return new DateTime(year, month, d2);
            }

            // Otherwise, jump to next month at first day
            var nextMonth = current.AddMonths(1);
            dim = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
            d1 = Math.Min(day1.Value, dim);

            return new DateTime(nextMonth.Year, nextMonth.Month, d1);
        }
    }
}
