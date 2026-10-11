using PropertyManagement.Domain;
using PropertyManagement.Enums;
using PropertyManagement.Models;

namespace PropertyManagement.Tests;

public class ApplicationRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 12, 0, 0);

    private static RentalApplication CompleteDraft() => new()
    {
        Status = ApplicationStatus.Draft,
        ApplicantInfoCompleted = true,
        ResidenceHistoryCompleted = true
    };

    [Theory]
    [InlineData(ApplicationStatus.Draft, true)]
    [InlineData(ApplicationStatus.Returned, true)]
    [InlineData(ApplicationStatus.Submitted, false)]
    [InlineData(ApplicationStatus.Approved, false)]
    [InlineData(ApplicationStatus.Denied, false)]
    [InlineData(ApplicationStatus.Withdrawn, false)]
    public void Only_draft_and_returned_are_editable(ApplicationStatus status, bool expected)
    {
        Assert.Equal(expected, ApplicationRules.IsEditableStatus(status));
    }

    [Theory]
    [InlineData(ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Denied)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public void Terminal_statuses_have_no_transitions(ApplicationStatus status)
    {
        Assert.True(ApplicationRules.IsTerminal(status));
        Assert.All(Enum.GetValues<ApplicationStatus>(),
            to => Assert.False(ApplicationRules.CanTransition(status, to)));
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Submitted, true)]
    [InlineData(ApplicationStatus.Returned, ApplicationStatus.Submitted, true)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.Approved, true)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.Returned, true)]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Approved, false)]
    [InlineData(ApplicationStatus.Returned, ApplicationStatus.Approved, false)]
    [InlineData(ApplicationStatus.Submitted, ApplicationStatus.Draft, false)]
    public void Transitions_follow_the_workflow(ApplicationStatus from, ApplicationStatus to, bool expected)
    {
        Assert.Equal(expected, ApplicationRules.CanTransition(from, to));
    }

    [Fact]
    public void Complete_draft_has_no_submit_blockers()
    {
        Assert.Empty(ApplicationRules.SubmitBlockers(CompleteDraft()));
    }

    [Fact]
    public void Each_unsaved_section_blocks_submit()
    {
        var application = CompleteDraft();
        application.ApplicantInfoCompleted = false;
        application.ResidenceHistoryCompleted = false;

        Assert.Equal(2, ApplicationRules.SubmitBlockers(application).Count);
    }

    [Fact]
    public void Submitted_application_cannot_be_submitted_again()
    {
        var application = CompleteDraft();
        application.Status = ApplicationStatus.Submitted;

        Assert.Single(ApplicationRules.SubmitBlockers(application));
    }

    [Fact]
    public void ChangeStatus_records_history_and_submitted_time()
    {
        var application = CompleteDraft();

        application.ChangeStatus(ApplicationStatus.Submitted, "user-1", Now);

        Assert.Equal(ApplicationStatus.Submitted, application.Status);
        Assert.Equal(Now, application.SubmittedAt);

        var entry = Assert.Single(application.StatusHistory);
        Assert.Equal(ApplicationStatus.Draft, entry.FromStatus);
        Assert.Equal(ApplicationStatus.Submitted, entry.ToStatus);
        Assert.Equal("user-1", entry.ChangedByUserId);
    }

    [Fact]
    public void ChangeStatus_rejects_invalid_transition_without_side_effects()
    {
        var application = CompleteDraft();

        Assert.Throws<InvalidOperationException>(() =>
            application.ChangeStatus(ApplicationStatus.Approved, "user-1", Now));

        Assert.Equal(ApplicationStatus.Draft, application.Status);
        Assert.Empty(application.StatusHistory);
    }

    [Theory]
    [InlineData(ApplicationSection.ApplicantInfo, ApplicationSection.ResidenceHistory)]
    [InlineData(ApplicationSection.ResidenceHistory, ApplicationSection.Summary)]
    [InlineData(ApplicationSection.Summary, ApplicationSection.Summary)]
    public void Next_moves_forward_and_stops_at_summary(ApplicationSection from, ApplicationSection expected)
    {
        Assert.Equal(expected, ApplicationRules.Next(from));
    }

    [Theory]
    [InlineData(ApplicationSection.Summary, ApplicationSection.ResidenceHistory)]
    [InlineData(ApplicationSection.ResidenceHistory, ApplicationSection.ApplicantInfo)]
    [InlineData(ApplicationSection.ApplicantInfo, ApplicationSection.ApplicantInfo)]
    public void Previous_moves_back_and_stops_at_first(ApplicationSection from, ApplicationSection expected)
    {
        Assert.Equal(expected, ApplicationRules.Previous(from));
    }

    [Theory]
    [InlineData(ReviewOutcome.Approve, ApplicationStatus.Approved, false)]
    [InlineData(ReviewOutcome.Return, ApplicationStatus.Returned, true)]
    [InlineData(ReviewOutcome.Deny, ApplicationStatus.Denied, true)]
    public void Review_outcomes_map_to_status_and_comment_rule(
        ReviewOutcome outcome, ApplicationStatus expectedStatus, bool commentRequired)
    {
        Assert.Equal(expectedStatus, ApplicationRules.StatusFor(outcome));
        Assert.Equal(commentRequired, ApplicationRules.RequiresComment(outcome));
    }

    [Theory]
    [InlineData(ApplicationStatus.Submitted, true)]
    [InlineData(ApplicationStatus.Draft, false)]
    [InlineData(ApplicationStatus.Returned, false)]
    [InlineData(ApplicationStatus.Approved, false)]
    public void Only_submitted_applications_are_reviewable(ApplicationStatus status, bool expected)
    {
        Assert.Equal(expected, ApplicationRules.IsReviewable(status));
    }

}