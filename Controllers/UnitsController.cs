using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Constants;
using PropertyManagement.Data;
using PropertyManagement.Data.Queries;
using PropertyManagement.ViewModels;

namespace PropertyManagement.Controllers;

[Authorize(Roles = AppRoles.Applicant)]
public class UnitsController(PropertyManagementDbContext context, TimeProvider timeProvider) : Controller
{
    public async Task<IActionResult> Index(int? propertyId)
    {
        var today = timeProvider.GetUtcNow().UtcDateTime;

        var query = context.Units
            .AsNoTracking()
            .AvailableOn(context.Leases, today);

        if (propertyId.HasValue)
        {
            query = query.Where(u => u.PropertyId == propertyId);
        }

        var units = await query
            .OrderBy(u => u.Property.Name)
            .ThenBy(u => u.UnitNumber)
            .Select(u => new AvailableUnitRowViewModel(
                u.Id,
                u.Property.Name,
                u.Property.Address + ", " + u.Property.City + ", " + u.Property.State,
                u.UnitNumber,
                u.UnitType.Name,
                u.Bedrooms,
                u.MonthlyRent))
            .ToListAsync();

        var properties = await context.Properties
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new SelectListItem(p.Name, p.Id.ToString()))
            .ToListAsync();

        return View(new AvailableUnitsViewModel
        {
            PropertyId = propertyId,
            Properties = properties,
            Units = units
        });
    }
}