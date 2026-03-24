using System.ComponentModel.DataAnnotations;

namespace BazaarOverlay.Domain.Entities;

public class SkillTag
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(50)]
    public string Tag { get; private set; } = string.Empty;

    public int SkillId { get; private set; }
    public Skill Skill { get; private set; } = null!;

    private SkillTag() { }

    public SkillTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty.", nameof(tag));

        Tag = tag.Trim();
    }
}
