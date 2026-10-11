namespace PropertyManagement.Models;

public class Lease
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }
    public RentalApplication RentalApplication { get; set; } = null!;

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public const int TermMonths = 12;

    public static Lease StartingOn(RentalApplication application, DateTime startDate) => new()
    {
        RentalApplication = application,
        UnitId = application.UnitId,
        StartDate = startDate.Date,
        EndDate = startDate.Date.AddMonths(TermMonths)
    };
}