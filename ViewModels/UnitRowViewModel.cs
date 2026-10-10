namespace PropertyManagement.ViewModels;

public record UnitRowViewModel(
    int Id,
    string UnitNumber,
    string UnitTypeName,
    bool UnitTypeIsActive,
    int Bedrooms,
    decimal MonthlyRent
);