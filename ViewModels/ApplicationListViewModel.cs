using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyManagement.Enums;

namespace PropertyManagement.ViewModels;

public class ApplicationListViewModel
{
    public ApplicationStatus? Status { get; set; }

    public int? PropertyId { get; set; }

    public bool IsManager { get; set; }

    public IReadOnlyList<SelectListItem> Properties { get; set; } = [];

    public IReadOnlyList<ApplicationListRowViewModel> Applications { get; set; } = [];
}

public record ApplicationListRowViewModel(
    int Id,
    string ApplicantName,
    string PropertyName,
    string UnitNumber,
    ApplicationStatus Status,
    DateTime? SubmittedAt,
    DateTime UpdatedAt);
