using PropertyManagement.Enums;
using PropertyManagement.Models;

namespace PropertyManagement.Data.Queries;

public static class ApplicationQueries
{
    public static IQueryable<RentalApplication> VisibleTo(
        this IQueryable<RentalApplication> applications,
        string userId,
        bool isManager)
    {
        return isManager
            ? applications
            : applications.Where(a => a.ApplicantId == userId);
    }

    public static IQueryable<RentalApplication> FilterBy(
        this IQueryable<RentalApplication> applications,
        ApplicationStatus? status,
        int? propertyId)
    {
        if (status.HasValue)
        {
            applications = applications.Where(a => a.Status == status.Value);
        }

        if (propertyId.HasValue)
        {
            applications = applications.Where(a => a.Unit.PropertyId == propertyId.Value);
        }

        return applications;
    }
}
