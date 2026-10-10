using PropertyManagement.Data.Queries;
using PropertyManagement.Models;

namespace PropertyManagement.Tests;

public class LeaseQueriesTests
{
    private static readonly DateTime Today = new(2026, 10, 10);

    private static readonly List<Unit> Units =
    [
        new() { Id = 1, UnitNumber = "1" },
        new() { Id = 2, UnitNumber = "2" }
    ];

    private static Lease TwelveMonthLease(int unitId, DateTime start) => new()
    {
        UnitId = unitId,
        StartDate = start,
        EndDate = start.AddMonths(12)
    };

    private static bool IsAvailable(int unitId, params Lease[] leases) =>
        Units.AsQueryable()
            .AvailableOn(leases.AsQueryable(), Today)
            .Any(u => u.Id == unitId);

    [Fact]
    public void Unit_with_no_leases_is_available()
    {
        Assert.True(IsAvailable(1));
    }

    [Theory]
    [InlineData(0)]   // starts today
    [InlineData(-6)]  // mid-term
    [InlineData(-11)] // final month
    public void Unit_with_lease_covering_today_is_not_available(int startMonthsFromToday)
    {
        var lease = TwelveMonthLease(1, Today.AddMonths(startMonthsFromToday));

        Assert.False(IsAvailable(1, lease));
    }

    [Fact]
    public void Unit_is_available_on_the_day_its_lease_ends()
    {
        var lease = TwelveMonthLease(1, Today.AddMonths(-12)); // EndDate == Today

        Assert.True(IsAvailable(1, lease));
    }

    [Theory]
    [InlineData(-24)] // long expired
    [InlineData(1)]   // starts next month
    public void Lease_not_covering_today_does_not_block(int startMonthsFromToday)
    {
        var lease = TwelveMonthLease(1, Today.AddMonths(startMonthsFromToday));

        Assert.True(IsAvailable(1, lease));
    }

    [Fact]
    public void Lease_on_another_unit_does_not_block()
    {
        var lease = TwelveMonthLease(2, Today);

        Assert.True(IsAvailable(1, lease));
        Assert.False(IsAvailable(2, lease));
    }

    [Fact]
    public void Time_of_day_is_ignored()
    {
        var lease = TwelveMonthLease(1, Today);

        var lateToday = Today.AddHours(23);
        var available = Units.AsQueryable()
            .AvailableOn(new[] { lease }.AsQueryable(), lateToday)
            .Any(u => u.Id == 1);

        Assert.False(available);
    }
}