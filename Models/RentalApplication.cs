using PropertyManagement.Enums;

namespace PropertyManagement.Models;

public class RentalApplication
{
    public int Id { get; set; }

    public string ApplicantId { get; set; } = string.Empty;
    public AspNetUser Applicant { get; set; } = null!;

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public ApplicationStatus Status { get; set; }

    public string CurrentAddress { get; set; } = string.Empty;
    public string CurrentCity { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public string CurrentZipCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Residence> Residences { get; set; } = new List<Residence>();
    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new List<ApplicationStatusHistory>();
}