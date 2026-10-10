using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PropertyManagement.Models;

public class Unit
{
    public int Id { get; set; }

    public int PropertyId { get; set; }

    public int UnitTypeId { get; set; }

    [Required]
    [StringLength(50)]
    public string UnitNumber { get; set; } = string.Empty;

    [Range(0, 20)]
    public int Bedrooms { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 99999999.99)]
    public decimal MonthlyRent { get; set; }

    public Property Property { get; set; } = null!;

    public UnitType UnitType { get; set; } = null!;
}