using ImpulseBudget.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImpulseBudget.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var incomeCount = await _db.IncomeSources.CountAsync();
            var billCount = await _db.Bills.CountAsync();
            var debtCount = await _db.Debts.CountAsync();
            var txCount = await _db.BankTransactions.CountAsync();

            var model = new DashboardViewModel
            {
                IncomeSourceCount = incomeCount,
                BillCount = billCount,
                DebtCount = debtCount,
                TransactionCount = txCount,
                Projection = new List<ProjectionPoint>() // empty for now
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

        public List<ProjectionPoint> Projection { get; set; } = new();
    }
}
