using PropertyManagement.Enums;
using PropertyManagement.Models;

namespace PropertyManagement.Data.Queries;

public static class ApplicationQueries
{
    // The single ownership rule: managers see every application, applicants only their own.
    public static IQueryable<RentalApplication> VisibleTo(
        this IQueryable<RentalApplication> applications,
        string userId,
        bool isManager)
    {
        return isManager
            ? applications
            : applications.Where(a => a.ApplicantId == userId);
    }

    // Each filter is optional and only adds a WHERE clause when it is set.
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
