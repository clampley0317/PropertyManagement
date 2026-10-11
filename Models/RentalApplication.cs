using System.ComponentModel.DataAnnotations;
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

    // Section 1: Applicant Information. Empty while the application is a draft.
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(25)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string CurrentAddress { get; set; } = string.Empty;

    [MaxLength(100)]
    public string CurrentCity { get; set; } = string.Empty;

    [MaxLength(2)]
    public string CurrentState { get; set; } = string.Empty;

    [MaxLength(10)]
    public string CurrentZipCode { get; set; } = string.Empty;

    // Set when a section passes validation and is saved; Submit requires both.
    public bool ApplicantInfoCompleted { get; set; }
    public bool ResidenceHistoryCompleted { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public ICollection<Residence> Residences { get; set; } = [];
    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = [];
}