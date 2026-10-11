using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.ViewModels;

public class ResidenceFormViewModel : IValidatableObject
{
    public int ApplicationId { get; set; }

    public int? ResidenceId { get; set; }

    [Required]
    [StringLength(200)]
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

    [Required]
    [StringLength(200)]
    [Display(Name = "Landlord Name")]
    public string LandlordName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(25)]
    [Display(Name = "Landlord Phone")]
    public string LandlordPhone { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Move-in Date")]
    public DateTime? MoveInDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Move-out Date")]
    public DateTime? MoveOutDate { get; set; }

    public bool IsEdit => ResidenceId.HasValue;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MoveInDate is null || MoveOutDate is null)
        {
            yield break;
        }

        if (MoveOutDate.Value.Date <= MoveInDate.Value.Date)
        {
            yield return new ValidationResult(
                "Move-out date must be after the move-in date.", [nameof(MoveOutDate)]);
        }

        var clock = validationContext.GetService<TimeProvider>() ?? TimeProvider.System;

        if (MoveOutDate.Value.Date > clock.GetUtcNow().UtcDateTime.Date)
        {
            yield return new ValidationResult(
                "A prior residence must have ended by today.", [nameof(MoveOutDate)]);
        }
    }
}