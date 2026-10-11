using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using PropertyManagement.ViewModels;

namespace PropertyManagement.Tests;

public class ResidenceFormViewModelTests
{
    private static readonly DateTime Today = new(2026, 10, 10);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private static ResidenceFormViewModel ValidResidence() => new()
    {
        ApplicationId = 1,
        Address = "1 Main St",
        City = "Albany",
        State = "NY",
        ZipCode = "12207",
        LandlordName = "Pat Landlord",
        LandlordPhone = "(518) 555-0100",
        MoveInDate = Today.AddYears(-2),
        MoveOutDate = Today.AddMonths(-1)
    };

    private static List<ValidationResult> Validate(ResidenceFormViewModel model)
    {
        var services = new ServiceCollection()
            .AddSingleton<TimeProvider>(new FixedTimeProvider(Today))
            .BuildServiceProvider();

        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model, services, null), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Valid_residence_has_no_errors()
    {
        Assert.Empty(Validate(ValidResidence()));
    }

    [Theory]
    [InlineData(0)]   // same day
    [InlineData(-30)] // before move-in
    public void Move_out_must_be_after_move_in(int moveOutDaysFromMoveIn)
    {
        var model = ValidResidence();
        model.MoveOutDate = model.MoveInDate!.Value.AddDays(moveOutDaysFromMoveIn);

        var error = Assert.Single(Validate(model));
        Assert.Contains(nameof(ResidenceFormViewModel.MoveOutDate), error.MemberNames);
    }

    [Fact]
    public void Move_out_in_the_future_is_rejected()
    {
        var model = ValidResidence();
        model.MoveOutDate = Today.AddDays(1);

        var error = Assert.Single(Validate(model));
        Assert.Contains(nameof(ResidenceFormViewModel.MoveOutDate), error.MemberNames);
    }

    [Fact]
    public void Move_out_today_is_allowed()
    {
        var model = ValidResidence();
        model.MoveOutDate = Today;

        Assert.Empty(Validate(model));
    }

    [Fact]
    public void Missing_dates_are_required_errors()
    {
        var model = ValidResidence();
        model.MoveInDate = null;
        model.MoveOutDate = null;

        var results = Validate(model);

        Assert.Equal(2, results.Count);
    }
}
