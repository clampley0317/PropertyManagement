using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PropertyManagement.Enums;

namespace PropertyManagement.ViewModels;

public class ApplicationWizardViewModel
{
    public int Id { get; set; }

    public ApplicationSection Section { get; set; }

    public ApplicantInfoInput ApplicantInfo { get; set; } = new();

    // Display-only. The server fills these on every render; they are never bound from the form.
    [BindNever] public ApplicationStatus Status { get; set; }
    [BindNever] public string UnitDescription { get; set; } = string.Empty;
    [BindNever] public bool IsEditable { get; set; }
    [BindNever] public IReadOnlyList<ResidenceRowViewModel> Residences { get; set; } = [];
    [BindNever] public IReadOnlyList<string> SubmitBlockers { get; set; } = [];
    [BindNever] public bool CanWithdraw { get; set; }

    public bool CanSubmit => IsEditable && SubmitBlockers.Count == 0;
}

public class ApplicantInfoInput
{
    [Required]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(25)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Display(Name = "Current Address")]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Use the two-letter state code.")]
    public string State { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Enter a valid ZIP code.")]
    [Display(Name = "Zip Code")]
    public string ZipCode { get; set; } = string.Empty;
}

public record ResidenceRowViewModel(
    int Id,
    string Address,
    string LandlordName,
    string LandlordPhone,
    DateTime MoveInDate,
    DateTime MoveOutDate);