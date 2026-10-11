using PropertyManagement.Data.Queries;
using PropertyManagement.Enums;
using PropertyManagement.Models;

namespace PropertyManagement.Tests;

public class ApplicationQueriesTests
{
    private static readonly Unit UnitAtProperty1 = new() { Id = 1, PropertyId = 1 };
    private static readonly Unit UnitAtProperty2 = new() { Id = 2, PropertyId = 2 };

    private static readonly List<RentalApplication> Applications =
    [
        new() { Id = 1, ApplicantId = "alice", Status = ApplicationStatus.Draft, Unit = UnitAtProperty1 },
        new() { Id = 2, ApplicantId = "alice", Status = ApplicationStatus.Submitted, Unit = UnitAtProperty2 },
        new() { Id = 3, ApplicantId = "bob", Status = ApplicationStatus.Submitted, Unit = UnitAtProperty1 },
        new() { Id = 4, ApplicantId = "bob", Status = ApplicationStatus.Approved, Unit = UnitAtProperty2 }
    ];

    private static int[] Ids(IQueryable<RentalApplication> query) =>
        query.Select(a => a.Id).OrderBy(id => id).ToArray();

    [Fact]
    public void Applicant_sees_only_their_own_applications()
    {
        var result = Applications.AsQueryable().VisibleTo("alice", isManager: false);

        Assert.Equal([1, 2], Ids(result));
    }

    [Fact]
    public void Manager_sees_every_application()
    {
        var result = Applications.AsQueryable().VisibleTo("manager", isManager: true);

        Assert.Equal([1, 2, 3, 4], Ids(result));
    }

    [Fact]
    public void No_filters_returns_everything()
    {
        Assert.Equal([1, 2, 3, 4], Ids(Applications.AsQueryable().FilterBy(null, null)));
    }

    [Fact]
    public void Filters_by_status()
    {
        var result = Applications.AsQueryable().FilterBy(ApplicationStatus.Submitted, null);

        Assert.Equal([2, 3], Ids(result));
    }

    [Fact]
    public void Filters_by_property()
    {
        var result = Applications.AsQueryable().FilterBy(null, propertyId: 2);

        Assert.Equal([2, 4], Ids(result));
    }

    [Fact]
    public void Status_property_and_ownership_combine()
    {
        var result = Applications.AsQueryable()
            .VisibleTo("bob", isManager: false)
            .FilterBy(ApplicationStatus.Submitted, propertyId: 1);

        Assert.Equal([3], Ids(result));
    }
}
