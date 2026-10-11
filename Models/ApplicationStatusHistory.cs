using System.ComponentModel.DataAnnotations;
using PropertyManagement.Enums;

namespace PropertyManagement.Models;

public class ApplicationStatusHistory
{
    public int Id { get; set; }

    public int RentalApplicationId { get; set; }
    public RentalApplication RentalApplication { get; set; } = null!;

    public ApplicationStatus? FromStatus { get; set; }
    public ApplicationStatus ToStatus { get; set; }

    public string ChangedByUserId { get; set; } = string.Empty;
    public AspNetUser ChangedByUser { get; set; } = null!;

    public DateTime ChangedAt { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }
}