using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Constants;
using PropertyManagement.Data;
using PropertyManagement.Data.Queries;
using PropertyManagement.Models;
using PropertyManagement.ViewModels;

namespace PropertyManagement.Controllers;

public class HomeController(PropertyManagementDbContext context, TimeProvider timeProvider) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View(new HomeViewModel());
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isManager = User.IsInRole(AppRoles.PropertyManager);

        // One grouped query: SQL returns a count per status rather than every application.
        var statusCounts = await context.RentalApplications
            .VisibleTo(userId, isManager)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        var availableUnitCount = await context.Units
            .AvailableOn(context.Leases, timeProvider.GetUtcNow().UtcDateTime)
            .CountAsync();

        return View(new HomeViewModel
        {
            IsSignedIn = true,
            IsManager = isManager,
            StatusCounts = statusCounts,
            AvailableUnitCount = availableUnitCount,
            PropertyCount = isManager ? await context.Properties.CountAsync() : 0
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
