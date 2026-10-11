using PropertyManagement.Enums;

namespace PropertyManagement.ViewModels;

public class HomeViewModel
{
    public bool IsSignedIn { get; set; }

    public bool IsManager { get; set; }

    public IReadOnlyDictionary<ApplicationStatus, int> StatusCounts { get; set; } =
        new Dictionary<ApplicationStatus, int>();

    public int AvailableUnitCount { get; set; }

    public int PropertyCount { get; set; }

    public int CountOf(ApplicationStatus status) => StatusCounts.GetValueOrDefault(status);
}
