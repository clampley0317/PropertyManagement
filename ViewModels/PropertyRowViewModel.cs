namespace PropertyManagement.ViewModels;

public record PropertyRowViewModel(
    int Id,
    string Name,
    string Address,
    string City,
    string State,
    int UnitCount
);
