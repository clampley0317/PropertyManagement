namespace PropertyManagement.Models;

public class Residence
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }
    public RentalApplication RentalApplication { get; set; } = null!;

    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;

    public string LandlordName { get; set; } = string.Empty;
    public string LandlordPhone { get; set; } = string.Empty;

    public DateTime MoveInDate { get; set; }
    public DateTime? MoveOutDate { get; set; }
}