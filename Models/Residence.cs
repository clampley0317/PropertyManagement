using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Models;

public class Residence
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }
    public RentalApplication RentalApplication { get; set; } = null!;

    [MaxLength(200)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(2)]
    public string State { get; set; } = string.Empty;

    [MaxLength(10)]
    public string ZipCode { get; set; } = string.Empty;

    [MaxLength(200)]
    public string LandlordName { get; set; } = string.Empty;

    [MaxLength(25)]
    public string LandlordPhone { get; set; } = string.Empty;

    public DateTime MoveInDate { get; set; }
    public DateTime MoveOutDate { get; set; }
}