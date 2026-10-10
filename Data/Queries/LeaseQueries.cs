using PropertyManagement.Models;

namespace PropertyManagement.Data.Queries;

public static class LeaseQueries
{
    public static IQueryable<Lease> ActiveOn(this IQueryable<Lease> leases, DateTime date)
    {
        var day = date.Date;

        return leases.Where(l => l.StartDate <= day && l.EndDate > day);
    }

    public static IQueryable<Unit> AvailableOn(
        this IQueryable<Unit> units,
        IQueryable<Lease> leases,
        DateTime date)
    {
        var activeLeases = leases.ActiveOn(date);

        return units.Where(u => !activeLeases.Any(l => l.UnitId == u.Id));
    }
}