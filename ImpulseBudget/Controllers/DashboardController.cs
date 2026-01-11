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
        private readonly BalanceService _balanceService;

        public DashboardController(
            ApplicationDbContext db,
            BudgetProjectionService projectionService,
            BalanceService balanceService)
        {
            _db = db;
            _projectionService = projectionService;
            _balanceService = balanceService;
        }

        // startingBalance is optional query parameter (?startingBalance=123.45)
        public async Task<IActionResult> Index()
        {
            var incomeCount = await _db.IncomeSources.CountAsync();
            var billCount = await _db.Bills.CountAsync();
            var debtCount = await _db.Debts.CountAsync();
            var txCount = await _db.BankTransactions.CountAsync();

            var startBal = await _balanceService.GetCurrentBalanceAsync();

            var projection = await _projectionService
                .GetWeeklyProjectionAsync(startBal, weeks: 26);

            var currentBal = await _balanceService.GetCurrentBalanceAsync();

            var model = new DashboardViewModel
            {
                IncomeSourceCount = incomeCount,
                BillCount = billCount,
                DebtCount = debtCount,
                TransactionCount = txCount,
                StartingBalance = startBal,
                Projection = projection,
                CurrentBalance = currentBal
            };

            return View(model);
        }
    }
}
