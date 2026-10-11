using System.ComponentModel.DataAnnotations;
using PropertyManagement.Domain;
using PropertyManagement.Enums;

namespace PropertyManagement.ViewModels;

public class ReviewApplicationViewModel : IValidatableObject
{
    public int ApplicationId { get; set; }

    [Required(ErrorMessage = "Choose an outcome.")]
    public ReviewOutcome? Outcome { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Outcome is { } outcome
            && ApplicationRules.RequiresComment(outcome)
            && string.IsNullOrWhiteSpace(Comment))
        {
            yield return new ValidationResult(
                $"A comment is required to {outcome.ToString().ToLowerInvariant()} an application.",
                [nameof(Comment)]);
        }
    }
}
