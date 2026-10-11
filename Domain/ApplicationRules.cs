using PropertyManagement.Enums;
using PropertyManagement.Models;

namespace PropertyManagement.Domain;

public static class ApplicationRules
{
    // Every allowed status change. A status with no entry is terminal.
    private static readonly Dictionary<ApplicationStatus, ApplicationStatus[]> AllowedTransitions = new()
    {
        [ApplicationStatus.Draft] = [ApplicationStatus.Submitted, ApplicationStatus.Withdrawn],
        [ApplicationStatus.Submitted] =
        [
            ApplicationStatus.Approved, ApplicationStatus.Returned,
            ApplicationStatus.Denied, ApplicationStatus.Withdrawn
        ],
        [ApplicationStatus.Returned] = [ApplicationStatus.Submitted, ApplicationStatus.Withdrawn]
    };

    public static bool CanTransition(ApplicationStatus from, ApplicationStatus to) =>
        AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static bool IsTerminal(ApplicationStatus status) =>
        !AllowedTransitions.ContainsKey(status);

    public static bool IsEditableStatus(ApplicationStatus status) =>
        status is ApplicationStatus.Draft or ApplicationStatus.Returned;

    // Everything stopping the application from being submitted; empty means it can be.
    public static IReadOnlyList<string> SubmitBlockers(RentalApplication application)
    {
        var blockers = new List<string>();

        if (!IsEditableStatus(application.Status))
        {
            blockers.Add($"An application that is {application.Status} cannot be submitted.");
        }

        if (!application.ApplicantInfoCompleted)
        {
            blockers.Add("Save the Applicant Information section.");
        }

        if (!application.ResidenceHistoryCompleted)
        {
            blockers.Add("Save the Residence History section.");
        }

        return blockers;
    }

    public static ApplicationSection Next(ApplicationSection section) => section switch
    {
        ApplicationSection.ApplicantInfo => ApplicationSection.ResidenceHistory,
        _ => ApplicationSection.Summary
    };

    public static ApplicationSection Previous(ApplicationSection section) => section switch
    {
        ApplicationSection.Summary => ApplicationSection.ResidenceHistory,
        _ => ApplicationSection.ApplicantInfo
    };
}