using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PropertyManagement.ViewModels;

public class UnitFormViewModel
{
    public int? Id { get; set; }

    public int PropertyId { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Unit Number")]
    public string UnitNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select a unit type.")]
    [Display(Name = "Unit Type")]
    public int? UnitTypeId { get; set; }

    [Range(0, 20)]
    public int Bedrooms { get; set; }

    [Range(0.01, 99999999.99)]
    [Display(Name = "Monthly Rent")]
    public decimal MonthlyRent { get; set; }

    [ValidateNever]
    public IEnumerable<SelectListItem> UnitTypeOptions { get; set; } = [];

    public bool IsEdit => Id.HasValue;
}