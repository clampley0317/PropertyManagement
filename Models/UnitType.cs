using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.Models;

public class UnitType
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Unit> Units { get; set; } = [];
}