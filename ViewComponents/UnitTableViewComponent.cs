using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.ViewModels;

namespace PropertyManagement.ViewComponents;

public class UnitTableViewComponent(PropertyManagementDbContext context) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int propertyId)
    {
        var rows = await context.Units
            .AsNoTracking()
            .Where(u => u.PropertyId == propertyId)
            // Unit numbers are text; sorting by length first gives 1, 2, ... 10 rather than 1, 10, 2.
            .OrderBy(u => u.UnitNumber.Length)
            .ThenBy(u => u.UnitNumber)
            .Select(u => new UnitRowViewModel(
                u.Id,
                u.UnitNumber,
                u.UnitType.Name,
                u.UnitType.IsActive,
                u.Bedrooms,
                u.MonthlyRent))
            .ToListAsync();

        return View(rows);
    }
}