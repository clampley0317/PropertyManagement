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
}