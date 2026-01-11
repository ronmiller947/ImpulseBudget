using ImpulseBudget.Models;
using ImpulseBudget.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImpulseBudget.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly BudgetProjectionService _projectionService;

        public DashboardController(
            ApplicationDbContext db,
            BudgetProjectionService projectionService)
        {
            _db = db;
            _projectionService = projectionService;
        }

        // startingBalance is optional query parameter (?startingBalance=123.45)
        public async Task<IActionResult> Index(decimal? startingBalance)
        {
            var incomeCount = await _db.IncomeSources.CountAsync();
            var billCount = await _db.Bills.CountAsync();
            var debtCount = await _db.Debts.CountAsync();
            var txCount = await _db.BankTransactions.CountAsync();

            var startBal = startingBalance ?? 0m;

            var projection = await _projectionService
                .GetWeeklyProjectionAsync(startBal, weeks: 26);

            var model = new DashboardViewModel
            {
                IncomeSourceCount = incomeCount,
                BillCount = billCount,
                DebtCount = debtCount,
                TransactionCount = txCount,
                StartingBalance = startBal,
                Projection = projection
            };

            return View(model);
        }
    }

    public class DashboardViewModel
    {
        public int IncomeSourceCount { get; set; }
        public int BillCount { get; set; }
        public int DebtCount { get; set; }
        public int TransactionCount { get; set; }

        public decimal StartingBalance { get; set; }

        public List<ProjectionPoint> Projection { get; set; } = new();
    }
}
