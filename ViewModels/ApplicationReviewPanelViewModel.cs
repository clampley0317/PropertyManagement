using PropertyManagement.Enums;

namespace PropertyManagement.ViewModels;

public class ApplicationReviewPanelViewModel
{
    public int ApplicationId { get; set; }

    public ApplicationStatus Status { get; set; }

    public bool IsReviewable { get; set; }

    public DateTime? LeaseStart { get; set; }

    public DateTime? LeaseEnd { get; set; }

    public IReadOnlyList<StatusHistoryRowViewModel> History { get; set; } = [];
}

public record StatusHistoryRowViewModel(
    ApplicationStatus? FromStatus,
    ApplicationStatus ToStatus,
    string ChangedBy,
    DateTime ChangedAt,
    string? Comment);
