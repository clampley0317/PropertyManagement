using System.ComponentModel.DataAnnotations;
using PropertyManagement.Enums;
using PropertyManagement.Models;
using PropertyManagement.ViewModels;

namespace PropertyManagement.Tests;

public class ReviewAndLeaseTests
{
    private static List<ValidationResult> Validate(ReviewApplicationViewModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Theory]
    [InlineData(ReviewOutcome.Return)]
    [InlineData(ReviewOutcome.Deny)]
    public void Return_and_deny_require_a_comment(ReviewOutcome outcome)
    {
        var model = new ReviewApplicationViewModel { Outcome = outcome, Comment = "   " };

        var error = Assert.Single(Validate(model));
        Assert.Contains(nameof(ReviewApplicationViewModel.Comment), error.MemberNames);
    }

    [Fact]
    public void Approve_does_not_require_a_comment()
    {
        Assert.Empty(Validate(new ReviewApplicationViewModel { Outcome = ReviewOutcome.Approve }));
    }

    [Fact]
    public void Outcome_is_required()
    {
        var error = Assert.Single(Validate(new ReviewApplicationViewModel()));
        Assert.Contains(nameof(ReviewApplicationViewModel.Outcome), error.MemberNames);
    }

    [Fact]
    public void Lease_runs_twelve_months_from_the_start_date()
    {
        var application = new RentalApplication { UnitId = 7 };

        var lease = Lease.StartingOn(application, new DateTime(2026, 10, 10, 15, 30, 0));

        Assert.Equal(7, lease.UnitId);
        Assert.Equal(new DateTime(2026, 10, 10), lease.StartDate);
        Assert.Equal(new DateTime(2027, 10, 10), lease.EndDate);
    }
}
