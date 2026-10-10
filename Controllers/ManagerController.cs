using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Models;
using System.Text.Json;

namespace PropertyManagement.Controllers;

[Authorize(Roles = "Property Manager")]
public class ManagerController(PropertyManagementDbContext context) : Controller
{
    private readonly PropertyManagementDbContext _context = context;

    public async Task<IActionResult> Index()
    {
        var properties = await _context.Properties
            .AsNoTracking()
            .Include(p => p.Units)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return View(properties);
    }

    [HttpGet]
    public IActionResult CreateProperty()
    {
        return PartialView("_CreateProperty", new Property());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProperty(Property property)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_CreateProperty", property);
        }

        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> EditProperty(int id)
    {
        var property = await _context.Properties
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property is null)
        {
            return NotFound();
        }

        return PartialView("_EditProperty", property);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProperty(
        int id,
        Property property)
    {
        if (id != property.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return PartialView("_EditProperty", property);
        }

        var existingProperty = await _context.Properties
            .FirstOrDefaultAsync(p => p.Id == id);

        if (existingProperty is null)
        {
            return NotFound();
        }

        existingProperty.Name = property.Name;
        existingProperty.Address = property.Address;
        existingProperty.City = property.City;
        existingProperty.State = property.State;
        existingProperty.ZipCode = property.ZipCode;

        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProperty(int id)
    {
        var property = await _context.Properties
            .Include(p => p.Units)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property is null)
        {
            return NotFound();
        }

        if (property.Units.Count > 0)
        {
            return Conflict(new
            {
                success = false,
                message = "Remove the property's units before deleting it."
            });
        }

        _context.Properties.Remove(property);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> Units(int propertyId)
    {
        var property = await _context.Properties
            .AsNoTracking()
            .Include(p => p.Units)
                .ThenInclude(u => u.UnitType)
            .FirstOrDefaultAsync(p => p.Id == propertyId);

        if (property is null)
        {
            return NotFound();
        }

        ViewBag.PropertyId = property.Id;
        ViewBag.PropertyName = property.Name;

        return View(property.Units
            .OrderBy(u => u.UnitNumber)
            .ToList());
    }

    [HttpGet]
    public async Task<IActionResult> CreateUnit(int propertyId)
    {
        var propertyExists = await _context.Properties
            .AnyAsync(p => p.Id == propertyId);

        if (!propertyExists)
        {
            return NotFound();
        }

        await LoadActiveUnitTypesAsync();

        return PartialView("_CreateUnit", new Unit
        {
            PropertyId = propertyId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUnit(Unit unit)
    {
        var propertyExists = await _context.Properties
            .AnyAsync(p => p.Id == unit.PropertyId);

        if (!propertyExists)
        {
            return NotFound();
        }

        var unitTypeIsActive = await _context.UnitTypes
            .AnyAsync(t => t.Id == unit.UnitTypeId && t.IsActive);

        if (!unitTypeIsActive)
        {
            ModelState.AddModelError(
                nameof(unit.UnitTypeId),
                "Select an active unit type.");
        }

        var unitNumberExists = await _context.Units
            .AnyAsync(u =>
                u.PropertyId == unit.PropertyId &&
                u.UnitNumber == unit.UnitNumber);

        if (unitNumberExists)
        {
            ModelState.AddModelError(
                nameof(unit.UnitNumber),
                "This unit number already exists for the property.");
        }

        if (!ModelState.IsValid)
        {
            await LoadActiveUnitTypesAsync();
            return PartialView("_CreateUnit", unit);
        }

        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    private async Task LoadActiveUnitTypesAsync()
    {
        ViewBag.UnitTypes = await _context.UnitTypes
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }
}