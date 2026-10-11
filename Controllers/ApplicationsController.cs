using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Constants;
using PropertyManagement.Data;
using PropertyManagement.Data.Queries;
using PropertyManagement.Enums;
using PropertyManagement.Models;

namespace PropertyManagement.Controllers;

[Authorize]
public class ApplicationsController(
    PropertyManagementDbContext context,
    UserManager<AspNetUser> userManager,
    TimeProvider timeProvider) : Controller
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Applicant)]
    public async Task<IActionResult> Start(int unitId)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var unitAvailable = await context.Units
            .AvailableOn(context.Leases, now)
            .AnyAsync(u => u.Id == unitId);

        if (!unitAvailable)
        {
            TempData["Error"] = "That unit is no longer available.";
            return RedirectToAction("Index", "Units");
        }

        // Don't create a duplicate if this applicant already has an editable application for the unit.
        var existingId = await context.RentalApplications
            .Where(a => a.ApplicantId == CurrentUserId
                        && a.UnitId == unitId
                        && (a.Status == ApplicationStatus.Draft || a.Status == ApplicationStatus.Returned))
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync();

        if (existingId is not null)
        {
            return RedirectToAction(nameof(Edit), new { id = existingId });
        }

        var user = await userManager.GetUserAsync(User);

        if (user is null)
        {
            return Challenge();
        }

        var application = new RentalApplication
        {
            ApplicantId = user.Id,
            UnitId = unitId,
            Status = ApplicationStatus.Draft,

            // Pre-fill from the profile; the applicant can change these in section 1.
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            Phone = user.PhoneNumber ?? string.Empty,

            CreatedAt = now,
            UpdatedAt = now
        };

        application.StatusHistory.Add(new ApplicationStatusHistory
        {
            FromStatus = null,
            ToStatus = ApplicationStatus.Draft,
            ChangedByUserId = user.Id,
            ChangedAt = now
        });

        context.RentalApplications.Add(application);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Edit), new { id = application.Id });
    }

    // Placeholder: the single-page wizard replaces this in the next step.
    [HttpGet]
    public IActionResult Edit(int id)
    {
        return Content($"Application {id}: the wizard is coming in the next step.");
    }
}