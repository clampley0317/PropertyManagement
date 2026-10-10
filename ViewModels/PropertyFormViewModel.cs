using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.ViewModels;

public class PropertyFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Use the two-letter state code.")]
    public string State { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    [Display(Name = "Zip Code")]
    public string ZipCode { get; set; } = string.Empty;

    public bool IsEdit => Id.HasValue;
}
