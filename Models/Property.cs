using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Models;

public class Property
{
    public int Id { get; set; }

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
    [StringLength(2, MinimumLength = 2)]
    public string State { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string ZipCode { get; set; } = string.Empty;

    public ICollection<Unit> Units { get; set; } = [];
}