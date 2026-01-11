using ImpulseBudget.Models;
using ImpulseBudget.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace ImpulseBudget.Controllers
{
    public class ImportController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly RecurringDetectionService _recurring;

        public ImportController(ApplicationDbContext db, RecurringDetectionService recurring)
        {
            _db = db; 
            _recurring = recurring;
        }

        // Simple view that shows all three import sections + messages
        [HttpGet]
        public IActionResult Index()
        {
            return View(new ImportResultViewModel());
        }

        // ---------- TEMPLATE DOWNLOAD ACTIONS ----------

        [HttpGet]
        public IActionResult DownloadIncomeTemplate()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name,Amount,Frequency,StartDate,EndDate,IsActive,DayOfMonth1,DayOfMonth2");
            sb.AppendLine("# Frequency options: OneTime, Weekly, BiWeekly, SemiMonthly, Monthly, Yearly");
            sb.AppendLine("Example Paycheck,1500.00,BiWeekly,2026-01-15,,true,,");
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "IncomeTemplate.csv");
        }

        [HttpGet]
        public IActionResult DownloadBillsTemplate()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name,Amount,Frequency,NextDueDate,IsPastDue,PastDueAmount,IsEssential,IsActive");
            sb.AppendLine("# Frequency options: OneTime, Weekly, BiWeekly, SemiMonthly, Monthly, Yearly");
            sb.AppendLine("Rent,1200.00,Monthly,2026-02-01,false,0,true,true");
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "BillsTemplate.csv");
        }

        [HttpGet]
        public IActionResult DownloadDebtsTemplate()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name,Balance,AprPercent,MinimumPayment,NextDueDate,IsEssential");
            sb.AppendLine("Visa Card,3000.00,24.99,100.00,2026-02-10,false");
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "DebtsTemplate.csv");
        }

        // ---------- IMPORT BANK TRANSACTIONS: STEP 1 (PREVIEW) ----------

        [HttpPost]
        public async Task<IActionResult> ImportBankTransactions(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                var errorModel = new ImportResultViewModel();
                errorModel.Errors.Add("No file was uploaded for bank transactions.");
                return View("Index", errorModel);
            }

            var preview = new BankTransactionImportPreviewViewModel
            {
                SourceLabel = "SOFI (or similar CSV)"
            };

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);

                // Read header
                var headerLine = await reader.ReadLineAsync();
                if (headerLine == null)
                {
                    var errorModel = new ImportResultViewModel();
                    errorModel.Errors.Add("The bank transactions file is empty.");
                    return View("Index", errorModel);
                }

                var headerParts = headerLine.Split(',').Select(h => h.Trim()).ToArray();

                // Build a dictionary headerName -> index for flexible mapping
                var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headerParts.Length; i++)
                {
                    headerMap[headerParts[i]] = i;
                }

                // We require at least Date, Description, Amount
                if (!TryGetIndex(headerMap, "Date", out var idxDate) ||
                    !TryGetIndex(headerMap, "Description", out var idxDesc) ||
                    !TryGetIndex(headerMap, "Amount", out var idxAmount))
                {
                    var errorModel = new ImportResultViewModel();
                    errorModel.Errors.Add("The bank CSV must contain at least 'Date', 'Description', and 'Amount' columns.");
                    errorModel.Errors.Add("Detected header: " + headerLine);
                    return View("Index", errorModel);
                }

                // Optional columns
                TryGetIndex(headerMap, "Type", out var idxType);
                if (!idxType.HasValue && TryGetIndex(headerMap, "Transaction Type", out var tmpType))
                    idxType = tmpType;

                TryGetIndex(headerMap, "Status", out var idxStatus);

                int rowNumber = 2;
                var rows = new List<BankTransactionImportRow>();

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    {
                        rowNumber++;
                        continue;
                    }

                    var parts = line.Split(',');

                    // Normalize to length of header
                    if (parts.Length < headerParts.Length)
                    {
                        Array.Resize(ref parts, headerParts.Length);
                    }

                    for (int i = 0; i < parts.Length; i++)
                        parts[i] = parts[i]?.Trim() ?? string.Empty;

                    var dateStr = SafeGet(parts, idxDate.Value);
                    var descStr = SafeGet(parts, idxDesc.Value);
                    var amountStr = SafeGet(parts, idxAmount.Value);
                    var typeStr = idxType.HasValue ? SafeGet(parts, idxType.Value) : null;
                    var statusStr = idxStatus.HasValue ? SafeGet(parts, idxStatus.Value) : null;

                    // Parse date and amount with your existing helpers
                    if (!TryParseDate(dateStr, out var date, out var dateErr) ||
                        !TryParseDecimal(amountStr, out var amount, out var amountErr))
                    {
                        // Skip completely invalid rows, but you could also collect errors instead
                        rowNumber++;
                        continue;
                    }

                    var row = new BankTransactionImportRow
                    {
                        Date = date,
                        Description = descStr,
                        Amount = amount,
                        Type = typeStr,
                        Status = statusStr,
                        Import = true // default to import; we may turn this off for duplicates
                    };

                    rows.Add(row);
                    rowNumber++;
                }

                // Duplicate detection vs existing DB
                await MarkDuplicatesAsync(rows);

                preview.Rows = rows;
            }
            catch (Exception ex)
            {
                var errorModel = new ImportResultViewModel();
                errorModel.Errors.Add("Unexpected error while reading bank transactions file: " + ex.Message);
                return View("Index", errorModel);
            }

            // Show preview to user
            return View("ImportBankTransactionsPreview", preview);
        }

        // Helper: check if header exists
        private bool TryGetIndex(Dictionary<string, int> map, string name, out int? index)
        {
            if (map.TryGetValue(name, out var idx))
            {
                index = idx;
                return true;
            }

            index = null;
            return false;
        }

        // Helper: safe array access
        private string SafeGet(string[] parts, int index)
        {
            return index >= 0 && index < parts.Length ? parts[index] : string.Empty;
        }

        // Duplicate detection: Date + Amount + Description (case-insensitive) already exists in DB?
        private async Task MarkDuplicatesAsync(List<BankTransactionImportRow> rows)
        {
            if (!rows.Any())
                return;

            var dates = rows.Select(r => r.Date.Date).Distinct().ToList();
            var amounts = rows.Select(r => r.Amount).Distinct().ToList();

            // Pull only candidates with matching date + amount once
            var candidates = await _db.BankTransactions
                .Where(t => dates.Contains(t.Date.Date) && amounts.Contains(t.Amount))
                .ToListAsync();

            foreach (var row in rows)
            {
                var sameDayAndAmount = candidates
                    .Where(t => t.Date.Date == row.Date.Date && t.Amount == row.Amount)
                    .ToList();

                if (!sameDayAndAmount.Any())
                {
                    row.IsDuplicate = false;
                    row.DuplicateSeverity = DuplicateSeverity.None;
                    continue;
                }

                // Compute best description similarity vs existing transactions
                double bestScore = 0.0;
                foreach (var t in sameDayAndAmount)
                {
                    var score = DescriptionSimilarity(row.Description, t.Description);
                    if (score > bestScore)
                        bestScore = score;
                }

                if (bestScore >= 0.85)        // very close / equal -> likely duplicate (pink)
                {
                    row.IsDuplicate = true;
                    row.DuplicateSeverity = DuplicateSeverity.Likely;
                }
                else if (bestScore >= 0.6)    // somewhat similar -> possible duplicate (yellow)
                {
                    row.IsDuplicate = true;
                    row.DuplicateSeverity = DuplicateSeverity.Possible;
                }
                else                          // same date+amount, but descriptions differ enough
                {
                    row.IsDuplicate = false;
                    row.DuplicateSeverity = DuplicateSeverity.None;
                }
            }
        }

        /// <summary>
        /// Very simple token-based similarity 0..1.
        /// 1 = identical, 0 = no shared tokens.
        /// </summary>
        private static double DescriptionSimilarity(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return 0.0;

            var tokensA = Tokenize(a);
            var tokensB = Tokenize(b);

            if (tokensA.Count == 0 || tokensB.Count == 0)
                return 0.0;

            var intersection = tokensA.Intersect(tokensB).Count();
            var union = tokensA.Union(tokensB).Count();

            return union == 0 ? 0.0 : (double)intersection / union;
        }

        private static HashSet<string> Tokenize(string s)
        {
            return s
                .ToLowerInvariant()
                .Split(' ', '\t', '\r', '\n', ',', '.', '-', '/', '\\', '(', ')', '[', ']', ':', ';', '!', '?', '"', '\'')
                .Where(t => t.Length > 2) // ignore tiny words like "of", "to"
                .ToHashSet();
        }

        // ---------- IMPORT BANK TRANSACTIONS: STEP 2 (CONFIRM) ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBankTransactionsImport(
    [FromForm] BankTransactionImportPreviewViewModel model)
        {
            var keys = Request.Form.Keys.ToList();
            var contentType = Request.ContentType;
            var method = Request.Method;

            if (model == null || model.Rows == null)
            {
                var result = new ImportResultViewModel();
                result.Errors.Add("No transaction data was submitted. Please upload the CSV again.");
                return View("Index", result);
            }

            var errors = new List<string>();

            var toImport = model.Rows
                .Where(r => r.Import)
                .ToList();

            if (!toImport.Any())
            {
                var result = new ImportResultViewModel();
                result.Errors.Add("No transactions were selected for import.");
                return View("Index", result);
            }

            var entities = new List<BankTransaction>();

            // Preload candidate existing transactions only once:
            var importDates = toImport.Select(r => r.Date.Date).Distinct().ToList();
            var importAmounts = toImport.Select(r => r.Amount).Distinct().ToList();

            var existingCandidates = await _db.BankTransactions
                .Where(t => importDates.Contains(t.Date.Date) && importAmounts.Contains(t.Amount))
                .ToListAsync();

            foreach (var row in toImport)
            {
                var rowDate = row.Date.Date;
                var candidates = existingCandidates
                    .Where(t => t.Date.Date == rowDate && t.Amount == row.Amount)
                    .ToList();

                var isRealDuplicate = false;

                foreach (var t in candidates)
                {
                    var score = DescriptionSimilarity(row.Description, t.Description);
                    if (score >= 0.85) // same threshold as "Likely"
                    {
                        isRealDuplicate = true;
                        break;
                    }
                }

                if (isRealDuplicate)
                {
                    // skip inserting an obvious duplicate
                    continue;
                }
                entities.Add(new BankTransaction
                {
                    Date = row.Date,
                    Description = row.Description,
                    Amount = row.Amount,
                    Category = row.Type,
                    IsImported = true,
                    IsIncoming = row.Amount > 0
                });
            }

            if (!entities.Any())
            {
                var result = new ImportResultViewModel();
                result.Errors.Add("No new transactions were imported (all selected rows were duplicates that already exist).");
                return View("Index", result);
            }

            await _db.BankTransactions.AddRangeAsync(entities);
            await _db.SaveChangesAsync();

            var success = new ImportResultViewModel
            {
                SuccessMessage = $"Successfully imported {entities.Count} transactions.",
                RecurringSuggestions = await _recurring.FindRecurringAsync()
            };

            return View("Index", success);
        }

        // ---------- IMPORT INCOME ----------

        [HttpPost]
        public async Task<IActionResult> ImportIncome(IFormFile file)
        {
            var result = new ImportResultViewModel();

            if (file == null || file.Length == 0)
            {
                result.Errors.Add("No file was uploaded.");
                return View("Index", result);
            }

            var errors = new List<string>();
            var imported = new List<IncomeSource>();

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);

                // Read header
                var headerLine = await reader.ReadLineAsync();
                if (headerLine == null)
                {
                    errors.Add("The file is empty.");
                    result.Errors = errors;
                    return View("Index", result);
                }

                var expectedHeader = "Name,Amount,Frequency,StartDate,EndDate,IsActive,DayOfMonth1,DayOfMonth2";
                if (!string.Equals(headerLine.Trim(), expectedHeader, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Header row does not match expected format. Expected: '{expectedHeader}'. Found: '{headerLine}'.");
                    result.Errors = errors;
                    return View("Index", result);
                }

                int rowNumber = 2; // data starts on row 2
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    {
                        rowNumber++;
                        continue;
                    }

                    var parts = line.Split(',');

                    if (parts.Length != 8)
                    {
                        errors.Add($"Row {rowNumber}: Expected 8 columns, found {parts.Length}.");
                        rowNumber++;
                        continue;
                    }

                    // Trim all values
                    for (int i = 0; i < parts.Length; i++)
                        parts[i] = parts[i].Trim();

                    string name = parts[0];
                    string amountStr = parts[1];
                    string freqStr = parts[2];
                    string startStr = parts[3];
                    string endStr = parts[4];
                    string isActiveStr = parts[5];
                    string day1Str = parts[6];
                    string day2Str = parts[7];

                    // Per-row error list so we can continue validating all columns
                    var rowErrors = new List<string>();

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        rowErrors.Add($"Row {rowNumber}, Column 'Name': value is required.");
                    }

                    if (!TryParseDecimal(amountStr, out var amount, out var amountErr))
                    {
                        rowErrors.Add($"Row {rowNumber}, Column 'Amount': {amountErr}");
                    }

                    if (!Enum.TryParse<Frequency>(freqStr, true, out var frequency))
                    {
                        rowErrors.Add($"Row {rowNumber}, Column 'Frequency': '{freqStr}' is not a valid frequency. Use OneTime, Weekly, BiWeekly, SemiMonthly, Monthly, or Yearly.");
                    }

                    if (!TryParseDate(startStr, out var startDate, out var startErr))
                    {
                        rowErrors.Add($"Row {rowNumber}, Column 'StartDate': {startErr}");
                    }

                    DateTime? endDate = null;
                    if (!string.IsNullOrWhiteSpace(endStr))
                    {
                        if (!TryParseDate(endStr, out var endDateParsed, out var endErr))
                        {
                            rowErrors.Add($"Row {rowNumber}, Column 'EndDate': {endErr}");
                        }
                        else
                        {
                            endDate = endDateParsed;
                        }
                    }

                    bool isActive = true;
                    if (!string.IsNullOrWhiteSpace(isActiveStr))
                    {
                        if (!bool.TryParse(isActiveStr, out isActive))
                        {
                            rowErrors.Add($"Row {rowNumber}, Column 'IsActive': '{isActiveStr}' is not a valid boolean. Use true or false.");
                        }
                    }

                    int? day1 = null;
                    if (!string.IsNullOrWhiteSpace(day1Str))
                    {
                        if (!int.TryParse(day1Str, out var d1))
                        {
                            rowErrors.Add($"Row {rowNumber}, Column 'DayOfMonth1': '{day1Str}' is not a valid integer.");
                        }
                        else if (d1 < 1 || d1 > 31)
                        {
                            rowErrors.Add($"Row {rowNumber}, Column 'DayOfMonth1': '{d1}' must be between 1 and 31.");
                        }
                        else
                        {
                            day1 = d1;
                        }
                    }

                    int? day2 = null;
                    if (!string.IsNullOrWhiteSpace(day2Str))
                    {
                        if (!int.TryParse(day2Str, out var d2))
                        {
                            rowErrors.Add($"Row {rowNumber}, Column 'DayOfMonth2': '{day2Str}' is not a valid integer.");
                        }
                        else if (d2 < 1 || d2 > 31)
                        {
                            rowErrors.Add($"Row {rowNumber}, Column 'DayOfMonth2': '{d2}' must be between 1 and 31.");
                        }
                        else
                        {
                            day2 = d2;
                        }
                    }

                    // Additional frequency-specific validation
                    if (rowErrors.Count == 0)
                    {
                        if (frequency == Frequency.Monthly && !day1.HasValue)
                        {
                            rowErrors.Add($"Row {rowNumber}: DayOfMonth1 is required when Frequency is Monthly.");
                        }

                        if (frequency == Frequency.SemiMonthly)
                        {
                            if (!day1.HasValue)
                                rowErrors.Add($"Row {rowNumber}: DayOfMonth1 is required when Frequency is SemiMonthly.");

                            if (!day2.HasValue)
                                rowErrors.Add($"Row {rowNumber}: DayOfMonth2 is required when Frequency is SemiMonthly.");

                            if (day1.HasValue && day2.HasValue && day1.Value >= day2.Value)
                                rowErrors.Add($"Row {rowNumber}: DayOfMonth1 must be less than DayOfMonth2 for SemiMonthly income.");
                        }
                    }

                    if (rowErrors.Any())
                    {
                        errors.AddRange(rowErrors);
                    }
                    else
                    {
                        // Row is valid → build entity
                        imported.Add(new IncomeSource
                        {
                            Name = name,
                            Amount = amount,
                            Frequency = frequency,
                            StartDate = startDate,
                            EndDate = endDate,
                            DayOfMonth1 = day1,
                            DayOfMonth2 = day2,
                            IsActive = isActive
                        });
                    }

                    rowNumber++;
                }
            }
            catch (Exception ex)
            {
                errors.Add("Unexpected error while reading file: " + ex.Message);
            }

            if (errors.Any())
            {
                // DO NOT import anything
                result.Errors = errors;
                return View("Index", result);
            }

            // No errors → import all in one transaction
            if (imported.Any())
            {
                await _db.IncomeSources.AddRangeAsync(imported);
                await _db.SaveChangesAsync();

                result.SuccessMessage = $"Successfully imported {imported.Count} income records.";
            }
            else
            {
                result.SuccessMessage = "File was processed but no rows were found.";
            }

            return View("Index", result);
        }

        // ---------- HELPER METHODS ----------

        private bool TryParseDecimal(string value, out decimal result, out string error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = 0;
                error = "value is required.";
                return false;
            }

            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
            {
                error = $"'{value}' is not a valid decimal number. Use '.' as the decimal separator (e.g. 1500.00).";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private bool TryParseDate(string value, out DateTime result, out string error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = default;
                error = "value is required.";
                return false;
            }

            // Common formats we want to support explicitly
            var formats = new[]
            {
                "yyyy-MM-dd",   // 2026-01-15
                "MM/dd/yyyy",   // 01/15/2026
                "M/d/yyyy",     // 1/5/2026
                "MM/dd/yy",     // 01/15/26
                "M/d/yy"        // 1/5/26
            };

            // 1) Try exact matches against a set of known formats (culture-independent)
            if (DateTime.TryParseExact(
                    value,
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out result))
            {
                error = string.Empty;
                return true;
            }

            // 2) Try using the current culture (likely en-US on your machine)
            if (DateTime.TryParse(
                    value,
                    CultureInfo.CurrentCulture,
                    DateTimeStyles.None,
                    out result))
            {
                error = string.Empty;
                return true;
            }

            // 3) Fallback: invariant culture loose parsing
            if (DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out result))
            {
                error = string.Empty;
                return true;
            }

            error = $"'{value}' is not a recognized date. Try formats like 2026-01-15, 01/15/2026, or 1/15/26.";
            return false;
        }


        // ---------- IMPORT BILLS ----------

        [HttpPost]
        public async Task<IActionResult> ImportBills(IFormFile file)
        {
            var result = new ImportResultViewModel();

            if (file == null || file.Length == 0)
            {
                result.Errors.Add("No file was uploaded for bills.");
                return View("Index", result);
            }

            var errors = new List<string>();
            var imported = new List<Bill>();

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);

                var headerLine = await reader.ReadLineAsync();
                if (headerLine == null)
                {
                    errors.Add("The bills file is empty.");
                    result.Errors = errors;
                    return View("Index", result);
                }

                var expectedHeader = "Name,Amount,Frequency,NextDueDate,IsPastDue,PastDueAmount,IsEssential,IsActive";
                if (!string.Equals(headerLine.Trim(), expectedHeader, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Bills header row does not match expected format. Expected: '{expectedHeader}'. Found: '{headerLine}'.");
                    result.Errors = errors;
                    return View("Index", result);
                }

                int rowNumber = 2;

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    {
                        rowNumber++;
                        continue;
                    }

                    var parts = line.Split(',');

                    if (parts.Length != 8)
                    {
                        errors.Add($"Row {rowNumber} (Bills): Expected 8 columns, found {parts.Length}.");
                        rowNumber++;
                        continue;
                    }

                    for (int i = 0; i < parts.Length; i++)
                        parts[i] = parts[i].Trim();

                    string name = parts[0];
                    string amountStr = parts[1];
                    string freqStr = parts[2];
                    string nextDueStr = parts[3];
                    string isPastDueStr = parts[4];
                    string pastDueAmountStr = parts[5];
                    string isEssentialStr = parts[6];
                    string isActiveStr = parts[7];

                    var rowErrors = new List<string>();

                    if (string.IsNullOrWhiteSpace(name))
                        rowErrors.Add($"Row {rowNumber}, Column 'Name': value is required.");

                    if (!TryParseDecimal(amountStr, out var amount, out var amountErr))
                        rowErrors.Add($"Row {rowNumber}, Column 'Amount': {amountErr}");

                    if (!Enum.TryParse<Frequency>(freqStr, true, out var frequency))
                        rowErrors.Add($"Row {rowNumber}, Column 'Frequency': '{freqStr}' is not valid.");

                    if (!TryParseDate(nextDueStr, out var nextDue, out var nextErr))
                        rowErrors.Add($"Row {rowNumber}, Column 'NextDueDate': {nextErr}");

                    bool isPastDue = false;
                    if (!string.IsNullOrWhiteSpace(isPastDueStr))
                    {
                        if (!bool.TryParse(isPastDueStr, out isPastDue))
                            rowErrors.Add($"Row {rowNumber}, Column 'IsPastDue': '{isPastDueStr}' is not a valid boolean.");
                    }

                    if (!TryParseDecimal(pastDueAmountStr, out var pastDueAmount, out var pdErr))
                    {
                        rowErrors.Add($"Row {rowNumber}, Column 'PastDueAmount': {pdErr}");
                    }
                    else if (pastDueAmount < 0)
                    {
                        rowErrors.Add($"Row {rowNumber}, Column 'PastDueAmount': value cannot be negative.");
                    }

                    bool isEssential = false;
                    if (!string.IsNullOrWhiteSpace(isEssentialStr))
                    {
                        if (!bool.TryParse(isEssentialStr, out isEssential))
                            rowErrors.Add($"Row {rowNumber}, Column 'IsEssential': '{isEssentialStr}' is not a valid boolean.");
                    }

                    bool isActive = true;
                    if (!string.IsNullOrWhiteSpace(isActiveStr))
                    {
                        if (!bool.TryParse(isActiveStr, out isActive))
                            rowErrors.Add($"Row {rowNumber}, Column 'IsActive': '{isActiveStr}' is not a valid boolean.");
                    }

                    if (!isPastDue && pastDueAmount > 0)
                    {
                        rowErrors.Add($"Row {rowNumber}: PastDueAmount > 0 but IsPastDue is false.");
                    }

                    if (rowErrors.Any())
                    {
                        errors.AddRange(rowErrors);
                    }
                    else
                    {
                        imported.Add(new Bill
                        {
                            Name = name,
                            Amount = amount,
                            Frequency = frequency,
                            NextDueDate = nextDue,
                            IsPastDue = isPastDue,
                            PastDueAmount = pastDueAmount,
                            IsEssential = isEssential,
                            IsActive = isActive
                        });
                    }

                    rowNumber++;
                }
            }
            catch (Exception ex)
            {
                errors.Add("Unexpected error while reading bills file: " + ex.Message);
            }

            if (errors.Any())
            {
                result.Errors = errors;
                return View("Index", result);
            }

            if (imported.Any())
            {
                await _db.Bills.AddRangeAsync(imported);
                await _db.SaveChangesAsync();
                result.SuccessMessage = $"Successfully imported {imported.Count} bills.";
            }
            else
            {
                result.SuccessMessage = "Bills file was processed but no rows were found.";
            }

            return View("Index", result);
        }

        // ---------- IMPORT DEBTS ----------

        [HttpPost]
        public async Task<IActionResult> ImportDebts(IFormFile file)
        {
            var result = new ImportResultViewModel();

            if (file == null || file.Length == 0)
            {
                result.Errors.Add("No file was uploaded for debts.");
                return View("Index", result);
            }

            var errors = new List<string>();
            var imported = new List<Debt>();

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);

                var headerLine = await reader.ReadLineAsync();
                if (headerLine == null)
                {
                    errors.Add("The debts file is empty.");
                    result.Errors = errors;
                    return View("Index", result);
                }

                var expectedHeader = "Name,Balance,AprPercent,MinimumPayment,NextDueDate,IsEssential";
                if (!string.Equals(headerLine.Trim(), expectedHeader, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Debts header row does not match expected format. Expected: '{expectedHeader}'. Found: '{headerLine}'.");
                    result.Errors = errors;
                    return View("Index", result);
                }

                int rowNumber = 2;

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    {
                        rowNumber++;
                        continue;
                    }

                    var parts = line.Split(',');
                    if (parts.Length != 6)
                    {
                        errors.Add($"Row {rowNumber} (Debts): Expected 6 columns, found {parts.Length}.");
                        rowNumber++;
                        continue;
                    }

                    for (int i = 0; i < parts.Length; i++)
                        parts[i] = parts[i].Trim();

                    string name = parts[0];
                    string balanceStr = parts[1];
                    string aprStr = parts[2];
                    string minPayStr = parts[3];
                    string nextDueStr = parts[4];
                    string isEssentialStr = parts[5];

                    var rowErrors = new List<string>();

                    if (string.IsNullOrWhiteSpace(name))
                        rowErrors.Add($"Row {rowNumber}, Column 'Name': value is required.");

                    if (!TryParseDecimal(balanceStr, out var balance, out var balanceErr))
                        rowErrors.Add($"Row {rowNumber}, Column 'Balance': {balanceErr}");

                    if (!TryParseDecimal(aprStr, out var apr, out var aprErr))
                        rowErrors.Add($"Row {rowNumber}, Column 'AprPercent': {aprErr}");

                    if (!TryParseDecimal(minPayStr, out var minPay, out var minPayErr))
                        rowErrors.Add($"Row {rowNumber}, Column 'MinimumPayment': {minPayErr}");

                    if (!TryParseDate(nextDueStr, out var nextDue, out var nextErr))
                        rowErrors.Add($"Row {rowNumber}, Column 'NextDueDate': {nextErr}");

                    bool isEssential = false;
                    if (!string.IsNullOrWhiteSpace(isEssentialStr))
                    {
                        if (!bool.TryParse(isEssentialStr, out isEssential))
                            rowErrors.Add($"Row {rowNumber}, Column 'IsEssential': '{isEssentialStr}' is not a valid boolean.");
                    }

                    if (rowErrors.Any())
                    {
                        errors.AddRange(rowErrors);
                    }
                    else
                    {
                        imported.Add(new Debt
                        {
                            Name = name,
                            Balance = balance,
                            AprPercent = apr,
                            MinimumPayment = minPay,
                            NextDueDate = nextDue,
                            IsEssential = isEssential
                        });
                    }

                    rowNumber++;
                }
            }
            catch (Exception ex)
            {
                errors.Add("Unexpected error while reading debts file: " + ex.Message);
            }

            if (errors.Any())
            {
                result.Errors = errors;
                return View("Index", result);
            }

            if (imported.Any())
            {
                await _db.Debts.AddRangeAsync(imported);
                await _db.SaveChangesAsync();
                result.SuccessMessage = $"Successfully imported {imported.Count} debts.";
            }
            else
            {
                result.SuccessMessage = "Debts file was processed but no rows were found.";
            }

            return View("Index", result);
        }
    }
}
