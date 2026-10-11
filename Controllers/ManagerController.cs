using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Constants;
using PropertyManagement.Data;
using PropertyManagement.Models;
using PropertyManagement.ViewModels;

namespace PropertyManagement.Controllers;

[Authorize(Roles = AppRoles.PropertyManager)]
public class ManagerController(PropertyManagementDbContext context) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult PropertyTable()
    {
        return ViewComponent("PropertyTable");
    }

    [HttpGet]
    public IActionResult CreateProperty()
    {
        return PartialView("_PropertyForm", new PropertyFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProperty(PropertyFormViewModel model)
    {
        model.Id = null;

        if (!ModelState.IsValid)
        {
            return PartialView("_PropertyForm", model);
        }

        context.Properties.Add(new Property
        {
            Name = model.Name,
            Address = model.Address,
            City = model.City,
            State = model.State.ToUpperInvariant(),
            ZipCode = model.ZipCode
        });

        await context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> EditProperty(int id)
    {
        var model = await context.Properties
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PropertyFormViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address,
                City = p.City,
                State = p.State,
                ZipCode = p.ZipCode
            })
            .FirstOrDefaultAsync();

        if (model is null)
        {
            return NotFound();
        }

        return PartialView("_PropertyForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProperty(int id, PropertyFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return PartialView("_PropertyForm", model);
        }

        var property = await context.Properties.FirstOrDefaultAsync(p => p.Id == id);

        if (property is null)
        {
            return NotFound();
        }

        property.Name = model.Name;
        property.Address = model.Address;
        property.City = model.City;
        property.State = model.State.ToUpperInvariant();
        property.ZipCode = model.ZipCode;

        await context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProperty(int id)
    {
        var property = await context.Properties.FirstOrDefaultAsync(p => p.Id == id);

        if (property is null)
        {
            return NotFound();
        }

        if (await context.Units.AnyAsync(u => u.PropertyId == id))
        {
            return Conflict(new
            {
                success = false,
                message = "Remove the property's units before deleting it."
            });
        }

        context.Properties.Remove(property);
        await context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> Units(int propertyId)
    {
        var model = await context.Properties
            .AsNoTracking()
            .Where(p => p.Id == propertyId)
            .Select(p => new UnitsPageViewModel
            {
                PropertyId = p.Id,
                PropertyName = p.Name
            })
            .FirstOrDefaultAsync();

        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult UnitTable(int propertyId)
    {
        return ViewComponent("UnitTable", new { propertyId });
    }

    [HttpGet]
    public async Task<IActionResult> CreateUnit(int propertyId)
    {
        if (!await context.Properties.AnyAsync(p => p.Id == propertyId))
        {
            return NotFound();
        }

        var model = new UnitFormViewModel
        {
            PropertyId = propertyId,
            UnitTypeOptions = await GetUnitTypeOptionsAsync(currentUnitTypeId: null)
        };

        return PartialView("_UnitForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUnit(UnitFormViewModel model)
    {
        if (!await context.Properties.AnyAsync(p => p.Id == model.PropertyId))
        {
            return NotFound();
        }

        model.Id = null;
        await ValidateUnitAsync(model, persistedUnitTypeId: null);

        if (!ModelState.IsValid)
        {
            model.UnitTypeOptions = await GetUnitTypeOptionsAsync(null);
            return PartialView("_UnitForm", model);
        }

        context.Units.Add(new Unit
        {
            PropertyId = model.PropertyId,
            UnitNumber = model.UnitNumber,
            UnitTypeId = model.UnitTypeId!.Value,
            Bedrooms = model.Bedrooms,
            MonthlyRent = model.MonthlyRent
        });

        await context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> EditUnit(int id)
    {
        var unit = await context.Units
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (unit is null)
        {
            return NotFound();
        }

        var model = new UnitFormViewModel
        {
            Id = unit.Id,
            PropertyId = unit.PropertyId,
            UnitNumber = unit.UnitNumber,
            UnitTypeId = unit.UnitTypeId,
            Bedrooms = unit.Bedrooms,
            MonthlyRent = unit.MonthlyRent,
            UnitTypeOptions = await GetUnitTypeOptionsAsync(unit.UnitTypeId)
        };

        return PartialView("_UnitForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUnit(int id, UnitFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var unit = await context.Units.FirstOrDefaultAsync(u => u.Id == id);

        if (unit is null)
        {
            return NotFound();
        }

        model.PropertyId = unit.PropertyId;

        await ValidateUnitAsync(model, persistedUnitTypeId: unit.UnitTypeId);

        if (!ModelState.IsValid)
        {
            model.UnitTypeOptions = await GetUnitTypeOptionsAsync(unit.UnitTypeId);
            return PartialView("_UnitForm", model);
        }

        unit.UnitNumber = model.UnitNumber;
        unit.UnitTypeId = model.UnitTypeId!.Value;
        unit.Bedrooms = model.Bedrooms;
        unit.MonthlyRent = model.MonthlyRent;

        await context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUnit(int id)
    {
        var unit = await context.Units.FirstOrDefaultAsync(u => u.Id == id);

        if (unit is null)
        {
            return NotFound();
        }

        var hasHistory =
            await context.RentalApplications.AnyAsync(a => a.UnitId == id) ||
            await context.Leases.AnyAsync(l => l.UnitId == id);

        if (hasHistory)
        {
            return Conflict(new
            {
                success = false,
                message = "This unit has applications or leases and cannot be deleted."
            });
        }

        context.Units.Remove(unit);
        await context.SaveChangesAsync();

        return Json(new { success = true });
    }

    private async Task<List<SelectListItem>> GetUnitTypeOptionsAsync(int? currentUnitTypeId)
    {
        return await context.UnitTypes
            .AsNoTracking()
            .Where(t => t.IsActive || t.Id == currentUnitTypeId)
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.IsActive ? t.Name : t.Name + " (inactive)"
            })
            .ToListAsync();
    }

    private async Task ValidateUnitAsync(UnitFormViewModel model, int? persistedUnitTypeId)
    {
        var unitTypeSelectable = await context.UnitTypes.AnyAsync(t =>
            t.Id == model.UnitTypeId &&
            (t.IsActive || t.Id == persistedUnitTypeId));

        if (!unitTypeSelectable)
        {
            ModelState.AddModelError(nameof(model.UnitTypeId), "Select an active unit type.");
        }

        var duplicateNumber = await context.Units.AnyAsync(u =>
            u.PropertyId == model.PropertyId &&
            u.UnitNumber == model.UnitNumber &&
            u.Id != model.Id);

        if (duplicateNumber)
        {
            ModelState.AddModelError(nameof(model.UnitNumber), "This unit number already exists for the property.");
        }
    }
}