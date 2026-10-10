using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.ViewModels;

namespace PropertyManagement.ViewComponents;

public class PropertyTableViewComponent(PropertyManagementDbContext context) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var rows = await context.Properties
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new PropertyRowViewModel(
                p.Id,
                p.Name,
                p.Address,
                p.City,
                p.State,
                p.Units.Count))
            .ToListAsync();

        return View(rows);
    }
}
