using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Constants;
using PropertyManagement.Data;
using PropertyManagement.Data.Queries;
using PropertyManagement.Domain;
using PropertyManagement.Enums;
using PropertyManagement.Models;
using PropertyManagement.ViewModels;

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

    [HttpGet]
    public async Task<IActionResult> Edit(int id, ApplicationSection? section)
    {
        var application = await FindVisibleApplicationAsync(id);

        if (application is null)
        {
            return NotFound();
        }

        var defaultSection = CanEdit(application) ? ApplicationSection.ApplicantInfo : ApplicationSection.Summary;

        return View(await BuildWizardAsync(application, section ?? defaultSection));
    }

    // One form, one action: the button that was clicked (command) decides what happens.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Applicant)]
    public async Task<IActionResult> Edit(int id, ApplicationWizardViewModel model, string command)
    {
        var application = await FindVisibleApplicationAsync(id);

        if (application is null)
        {
            return NotFound();
        }

        if (!CanEdit(application))
        {
            TempData["Error"] = "This application can no longer be changed.";
            return RedirectToSection(id, ApplicationSection.Summary);
        }

        return command switch
        {
            "back" => RedirectToSection(id, ApplicationRules.Previous(model.Section)),
            "continue" => await ContinueAsync(application, model),
            "submit" => await SubmitAsync(application),
            _ => BadRequest()
        };
    }

    private async Task<IActionResult> ContinueAsync(RentalApplication application, ApplicationWizardViewModel model)
    {
        switch (model.Section)
        {
            case ApplicationSection.ApplicantInfo:
                if (!IsSectionValid(nameof(model.ApplicantInfo)))
                {
                    var invalid = await BuildWizardAsync(application, ApplicationSection.ApplicantInfo);
                    invalid.ApplicantInfo = model.ApplicantInfo;
                    return View(invalid);
                }

                var input = model.ApplicantInfo;
                application.FirstName = input.FirstName.Trim();
                application.LastName = input.LastName.Trim();
                application.Phone = input.Phone.Trim();
                application.Email = input.Email.Trim();
                application.CurrentAddress = input.Address.Trim();
                application.CurrentCity = input.City.Trim();
                application.CurrentState = input.State.Trim().ToUpperInvariant();
                application.CurrentZipCode = input.ZipCode.Trim();
                application.ApplicantInfoCompleted = true;
                break;

            case ApplicationSection.ResidenceHistory:
                if (!await context.Residences.AnyAsync(r => r.RentalApplicationId == application.Id))
                {
                    ModelState.AddModelError(string.Empty, "Add at least one prior residence.");
                    return View(await BuildWizardAsync(application, ApplicationSection.ResidenceHistory));
                }

                application.ResidenceHistoryCompleted = true;
                break;

            default:
                return RedirectToSection(application.Id, ApplicationSection.Summary);
        }

        application.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await context.SaveChangesAsync();

        return RedirectToSection(application.Id, ApplicationRules.Next(model.Section));
    }

    private async Task<IActionResult> SubmitAsync(RentalApplication application)
    {
        if (ApplicationRules.SubmitBlockers(application).Count > 0)
        {
            TempData["Error"] = "Complete every section before submitting.";
            return RedirectToSection(application.Id, ApplicationSection.Summary);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Other open applications for this unit are left alone; this one just can't be submitted.
        var unitLeased = await context.Leases
            .ActiveOn(now)
            .AnyAsync(l => l.UnitId == application.UnitId);

        if (unitLeased)
        {
            TempData["Error"] = "This unit has an active lease, so the application cannot be submitted.";
            return RedirectToSection(application.Id, ApplicationSection.Summary);
        }

        application.ChangeStatus(ApplicationStatus.Submitted, CurrentUserId, now);
        await context.SaveChangesAsync();

        TempData["Success"] = "Your application has been submitted.";
        return RedirectToSection(application.Id, ApplicationSection.Summary);
    }

    private async Task<RentalApplication?> FindVisibleApplicationAsync(int id)
    {
        var query = context.RentalApplications.Where(a => a.Id == id);

        if (!User.IsInRole(AppRoles.PropertyManager))
        {
            query = query.Where(a => a.ApplicantId == CurrentUserId);
        }

        return await query.FirstOrDefaultAsync();
    }

    private bool CanEdit(RentalApplication application) =>
        application.ApplicantId == CurrentUserId && ApplicationRules.IsEditableStatus(application.Status);

    private bool IsSectionValid(string prefix) =>
        ModelState.FindKeysWithPrefix(prefix)
            .All(entry => entry.Value.ValidationState != ModelValidationState.Invalid);

    private RedirectToActionResult RedirectToSection(int id, ApplicationSection section) =>
        RedirectToAction(nameof(Edit), new { id, section });

    private async Task<ApplicationWizardViewModel> BuildWizardAsync(RentalApplication application, ApplicationSection section)
    {
        var unitDescription = await context.Units
            .Where(u => u.Id == application.UnitId)
            .Select(u => u.Property.Name + " · Unit " + u.UnitNumber)
            .FirstAsync();

        var residences = await context.Residences
            .AsNoTracking()
            .Where(r => r.RentalApplicationId == application.Id)
            .OrderByDescending(r => r.MoveOutDate)
            .Select(r => new ResidenceRowViewModel(
                r.Id,
                r.Address + ", " + r.City + ", " + r.State + " " + r.ZipCode,
                r.LandlordName,
                r.LandlordPhone,
                r.MoveInDate,
                r.MoveOutDate))
            .ToListAsync();

        return new ApplicationWizardViewModel
        {
            Id = application.Id,
            Section = section,
            Status = application.Status,
            UnitDescription = unitDescription,
            IsEditable = CanEdit(application),
            Residences = residences,
            SubmitBlockers = ApplicationRules.SubmitBlockers(application),
            CanWithdraw = application.ApplicantId == CurrentUserId
              && ApplicationRules.CanTransition(application.Status, ApplicationStatus.Withdrawn),
            ApplicantInfo = new ApplicantInfoInput
            {
                FirstName = application.FirstName,
                LastName = application.LastName,
                Phone = application.Phone,
                Email = application.Email,
                Address = application.CurrentAddress,
                City = application.CurrentCity,
                State = application.CurrentState,
                ZipCode = application.CurrentZipCode
            }
        };
    }

    // For the modal endpoints: 404 if the application isn't yours, 403 if it can no longer be edited.
    // A plain 403 status (not Forbid()) so fetch gets an error rather than a redirect to the login page.
    private async Task<(RentalApplication? Application, IActionResult? Error)> FindEditableApplicationAsync(int id)
    {
        var application = await FindVisibleApplicationAsync(id);

        if (application is null)
        {
            return (null, NotFound());
        }

        if (!CanEdit(application))
        {
            return (null, StatusCode(StatusCodes.Status403Forbidden));
        }

        return (application, null);
    }

    // The residence list changed, so the section must be confirmed again with Save & Continue.
    private void MarkResidencesChanged(RentalApplication application)
    {
        application.ResidenceHistoryCompleted = false;
        application.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
    }

    private static void ApplyResidence(ResidenceFormViewModel model, Residence residence)
    {
        residence.Address = model.Address.Trim();
        residence.City = model.City.Trim();
        residence.State = model.State.Trim().ToUpperInvariant();
        residence.ZipCode = model.ZipCode.Trim();
        residence.LandlordName = model.LandlordName.Trim();
        residence.LandlordPhone = model.LandlordPhone.Trim();
        residence.MoveInDate = model.MoveInDate!.Value.Date;
        residence.MoveOutDate = model.MoveOutDate!.Value.Date;
    }


    // Returns just the residence section, so the page can refresh it after a modal save.
    [HttpGet]
    public async Task<IActionResult> ResidenceHistory(int id)
    {
        var application = await FindVisibleApplicationAsync(id);

        if (application is null)
        {
            return NotFound();
        }

        return PartialView("_ResidenceHistorySection",
            await BuildWizardAsync(application, ApplicationSection.ResidenceHistory));
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Applicant)]
    public async Task<IActionResult> AddResidence(int id)
    {
        var (application, error) = await FindEditableApplicationAsync(id);

        if (application is null)
        {
            return error!;
        }

        return PartialView("_ResidenceForm", new ResidenceFormViewModel { ApplicationId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Applicant)]
    public async Task<IActionResult> AddResidence(int id, ResidenceFormViewModel model)
    {
        var (application, error) = await FindEditableApplicationAsync(id);

        if (application is null)
        {
            return error!;
        }

        model.ApplicationId = id;
        model.ResidenceId = null;

        if (!ModelState.IsValid)
        {
            return PartialView("_ResidenceForm", model);
        }

        var residence = new Residence { RentalApplicationId = id };
        ApplyResidence(model, residence);

        context.Residences.Add(residence);
        MarkResidencesChanged(application);
        await context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Applicant)]
    public async Task<IActionResult> EditResidence(int id, int residenceId)
    {
        var (application, error) = await FindEditableApplicationAsync(id);

        if (application is null)
        {
            return error!;
        }

        var model = await context.Residences
            .AsNoTracking()
            .Where(r => r.Id == residenceId && r.RentalApplicationId == id)
            .Select(r => new ResidenceFormViewModel
            {
                ApplicationId = r.RentalApplicationId,
                ResidenceId = r.Id,
                Address = r.Address,
                City = r.City,
                State = r.State,
                ZipCode = r.ZipCode,
                LandlordName = r.LandlordName,
                LandlordPhone = r.LandlordPhone,
                MoveInDate = r.MoveInDate,
                MoveOutDate = r.MoveOutDate
            })
            .FirstOrDefaultAsync();

        return model is null ? NotFound() : PartialView("_ResidenceForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Applicant)]
    public async Task<IActionResult> EditResidence(int id, int residenceId, ResidenceFormViewModel model)
    {
        var (application, error) = await FindEditableApplicationAsync(id);

        if (application is null)
        {
            return error!;
        }

        // Scoped to this application, so a tampered residenceId can't reach another applicant's data.
        var residence = await context.Residences
            .FirstOrDefaultAsync(r => r.Id == residenceId && r.RentalApplicationId == id);

        if (residence is null)
        {
            return NotFound();
        }

        model.ApplicationId = id;
        model.ResidenceId = residenceId;

        if (!ModelState.IsValid)
        {
            return PartialView("_ResidenceForm", model);
        }

        ApplyResidence(model, residence);
        MarkResidencesChanged(application);
        await context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Applicant)]
    public async Task<IActionResult> DeleteResidence(int id, int residenceId)
    {
        var (application, error) = await FindEditableApplicationAsync(id);

        if (application is null)
        {
            return error!;
        }

        var residence = await context.Residences
            .FirstOrDefaultAsync(r => r.Id == residenceId && r.RentalApplicationId == id);

        if (residence is null)
        {
            return NotFound();
        }

        context.Residences.Remove(residence);
        MarkResidencesChanged(application);
        await context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Applicant)]
    public async Task<IActionResult> Withdraw(int id)
    {
        var application = await FindVisibleApplicationAsync(id);

        if (application is null || application.ApplicantId != CurrentUserId)
        {
            return NotFound();
        }

        if (!ApplicationRules.CanTransition(application.Status, ApplicationStatus.Withdrawn))
        {
            TempData["Error"] = $"An application that is {application.Status} cannot be withdrawn.";
            return RedirectToSection(id, ApplicationSection.Summary);
        }

        application.ChangeStatus(ApplicationStatus.Withdrawn, CurrentUserId, timeProvider.GetUtcNow().UtcDateTime);
        await context.SaveChangesAsync();

        TempData["Success"] = "Your application has been withdrawn.";
        return RedirectToSection(id, ApplicationSection.Summary);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.PropertyManager)]
    public IActionResult ReviewPanel(int id)
    {
        return ViewComponent("ApplicationReviewPanel", new { applicationId = id });
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.PropertyManager)]
    public async Task<IActionResult> Review(int id)
    {
        var status = await context.RentalApplications
            .Where(a => a.Id == id)
            .Select(a => (ApplicationStatus?)a.Status)
            .FirstOrDefaultAsync();

        if (status is null)
        {
            return NotFound();
        }

        if (!ApplicationRules.IsReviewable(status.Value))
        {
            return Conflict();
        }

        return PartialView("_ReviewForm", new ReviewApplicationViewModel { ApplicationId = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.PropertyManager)]
    public async Task<IActionResult> Review(int id, ReviewApplicationViewModel model)
    {
        model.ApplicationId = id;

        if (!ModelState.IsValid)
        {
            return PartialView("_ReviewForm", model);
        }

        var unitId = await context.RentalApplications
            .Where(a => a.Id == id)
            .Select(a => (int?)a.UnitId)
            .FirstOrDefaultAsync();

        if (unitId is null)
        {
            return NotFound();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        await using var transaction = await context.Database.BeginTransactionAsync();

        // Lock the unit's row until commit. A second manager reviewing an application for the same
        // unit waits here, then sees this transaction's lease, so two approvals can't both pass the check.
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT Id FROM Units WITH (UPDLOCK, ROWLOCK) WHERE Id = {unitId}");

        // Load the application only after taking the lock, so its status is current.
        var application = await context.RentalApplications.FirstAsync(a => a.Id == id);
        var newStatus = ApplicationRules.StatusFor(model.Outcome!.Value);

        if (!ApplicationRules.CanTransition(application.Status, newStatus))
        {
            ModelState.AddModelError(string.Empty,
                $"This application is {application.Status} and can no longer be reviewed.");
            return PartialView("_ReviewForm", model);
        }

        if (newStatus == ApplicationStatus.Approved)
        {
            var unitLeased = await context.Leases
                .ActiveOn(now)
                .AnyAsync(l => l.UnitId == application.UnitId);

            if (unitLeased)
            {
                ModelState.AddModelError(string.Empty,
                    "This unit already has an active lease, so the application cannot be approved.");
                return PartialView("_ReviewForm", model);
            }

            context.Leases.Add(Lease.StartingOn(application, now));
        }

        application.ChangeStatus(newStatus, CurrentUserId, now, model.Comment?.Trim());

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Json(new { success = true });
    }

}