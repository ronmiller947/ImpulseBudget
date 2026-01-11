using ImpulseBudget.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImpulseBudget.Controllers
{
    public class BudgetSettingsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BudgetSettingsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var settings = await _db.BudgetSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new BudgetSettings
                {
                    StartingBalance = 0m,
                    StartingBalanceDate = DateTime.Today
                };
            }

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BudgetSettings model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _db.BudgetSettings.FirstOrDefaultAsync();
            if (existing == null)
            {
                _db.BudgetSettings.Add(model);
            }
            else
            {
                existing.StartingBalance = model.StartingBalance;
                existing.StartingBalanceDate = model.StartingBalanceDate;
                _db.BudgetSettings.Update(existing);
            }

            await _db.SaveChangesAsync();

            ViewBag.Message = "Starting balance updated.";
            return View(model);
        }
    }
}
