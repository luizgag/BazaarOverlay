using System.ComponentModel.DataAnnotations;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Entities;

public class Trainer
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    public Rarity Tier { get; private set; }

    [MaxLength(500)]
    public string? Tooltip { get; private set; }

    [MaxLength(200)]
    public string? SelectionRule { get; private set; }

    [MaxLength(200)]
    public string? CostRule { get; private set; }

    [MaxLength(200)]
    public string? LeaveRule { get; private set; }

    public int? RerollCount { get; private set; }
    public int? RerollCost { get; private set; }

    [MaxLength(50)]
    public string? BazaarDbId { get; private set; }

    public ICollection<Skill> SkillPool { get; private set; } = new List<Skill>();

    private Trainer() { }

    public Trainer(string name, Rarity tier, string? tooltip = null,
        string? selectionRule = null, string? costRule = null, string? leaveRule = null,
        int? rerollCount = null, int? rerollCost = null, string? bazaarDbId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Trainer name cannot be empty.", nameof(name));

        Name = name.Trim();
        Tier = tier;
        Tooltip = tooltip?.Trim();
        SelectionRule = selectionRule?.Trim();
        CostRule = costRule?.Trim();
        LeaveRule = leaveRule?.Trim();
        RerollCount = rerollCount;
        RerollCost = rerollCost;
        BazaarDbId = bazaarDbId?.Trim();
    }
}
