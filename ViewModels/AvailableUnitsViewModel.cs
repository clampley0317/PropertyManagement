using Microsoft.AspNetCore.Mvc.Rendering;

namespace PropertyManagement.ViewModels;

public class AvailableUnitsViewModel
{
    public int? PropertyId { get; set; }

    public IReadOnlyList<SelectListItem> Properties { get; set; } = [];

    public IReadOnlyList<AvailableUnitRowViewModel> Units { get; set; } = [];
}

public record AvailableUnitRowViewModel(
    int Id,
    string PropertyName,
    string PropertyAddress,
    string UnitNumber,
    string UnitTypeName,
    int Bedrooms,
    decimal MonthlyRent);