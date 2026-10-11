using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Domain;
using PropertyManagement.ViewModels;

namespace PropertyManagement.ViewComponents;

public class ApplicationReviewPanelViewComponent(PropertyManagementDbContext context) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int applicationId)
    {
        var status = await context.RentalApplications
            .Where(a => a.Id == applicationId)
            .Select(a => (Enums.ApplicationStatus?)a.Status)
            .FirstOrDefaultAsync();

        if (status is null)
        {
            return Content(string.Empty);
        }

        var history = await context.ApplicationStatusHistories
            .AsNoTracking()
            .Where(h => h.RentalApplicationId == applicationId)
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Id)
            .Select(h => new StatusHistoryRowViewModel(
                h.FromStatus,
                h.ToStatus,
                h.ChangedByUser.FirstName + " " + h.ChangedByUser.LastName,
                h.ChangedAt,
                h.Comment))
            .ToListAsync();

        var lease = await context.Leases
            .Where(l => l.RentalApplicationId == applicationId)
            .Select(l => new { l.StartDate, l.EndDate })
            .FirstOrDefaultAsync();

        return View(new ApplicationReviewPanelViewModel
        {
            ApplicationId = applicationId,
            Status = status.Value,
            IsReviewable = ApplicationRules.IsReviewable(status.Value),
            LeaseStart = lease?.StartDate,
            LeaseEnd = lease?.EndDate,
            History = history
        });
    }
}
