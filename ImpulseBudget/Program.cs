using ImpulseBudget.Services;
using ImpulseBudget.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features; // at the top of the file

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<BudgetProjectionService>();
builder.Services.AddScoped<RecurringDetectionService>();
builder.Services.AddScoped<BalanceService>();

builder.Services.AddControllersWithViews();

builder.Services.Configure<FormOptions>(options =>
{
    // default is 1024; bump it high enough for your import scenarios
    options.ValueCountLimit = int.MaxValue; // or int.MaxValue if you really want
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
