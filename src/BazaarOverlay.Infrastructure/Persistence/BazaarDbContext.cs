using BazaarOverlay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence;

public class BazaarDbContext : DbContext
{
    public DbSet<Hero> Heroes => Set<Hero>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemTag> ItemTags => Set<ItemTag>();
    public DbSet<ItemTierValue> ItemTierValues => Set<ItemTierValue>();
    public DbSet<ItemEnchantment> ItemEnchantments => Set<ItemEnchantment>();
    public DbSet<Enchantment> Enchantments => Set<Enchantment>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<SkillTag> SkillTags => Set<SkillTag>();
    public DbSet<SkillTierValue> SkillTierValues => Set<SkillTierValue>();
    public DbSet<Monster> Monsters => Set<Monster>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<ShopConstraints> ShopConstraints => Set<ShopConstraints>();
    public DbSet<ShopAllowedTag> ShopAllowedTags => Set<ShopAllowedTag>();
    public DbSet<RarityDayProbability> RarityDayProbabilities => Set<RarityDayProbability>();

    public BazaarDbContext(DbContextOptions<BazaarDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hero>(entity =>
        {
            entity.HasKey(e => e.Name);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.Heroes)
                .WithMany(h => h.Items)
                .UsingEntity("ItemHero");
            entity.HasMany(e => e.Tags)
                .WithOne(t => t.Item)
                .HasForeignKey(t => t.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.TierValues)
                .WithOne(tv => tv.Item)
                .HasForeignKey(tv => tv.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Enchantments)
                .WithOne(ie => ie.Item)
                .HasForeignKey(ie => ie.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.Heroes)
                .WithMany(h => h.Skills)
                .UsingEntity("SkillHero");
            entity.HasMany(e => e.Tags)
                .WithOne(t => t.Skill)
                .HasForeignKey(t => t.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.TierValues)
                .WithOne(tv => tv.Skill)
                .HasForeignKey(tv => tv.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Monster>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.DropItems)
                .WithMany()
                .UsingEntity("MonsterItemDrop");
            entity.HasMany(e => e.DropSkills)
                .WithMany()
                .UsingEntity("MonsterSkillDrop");
        });

        modelBuilder.Entity<Enchantment>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Encounter>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasOne(e => e.ShopConstraints)
                .WithOne(sc => sc.Encounter)
                .HasForeignKey<ShopConstraints>(sc => sc.EncounterId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.ShopAllowedTags)
                .WithOne(t => t.Encounter)
                .HasForeignKey(t => t.EncounterId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.RewardItems)
                .WithMany()
                .UsingEntity("EncounterItemReward");
            entity.HasMany(e => e.RewardSkills)
                .WithMany()
                .UsingEntity("EncounterSkillReward");
        });

        modelBuilder.Entity<RarityDayProbability>(entity =>
        {
            entity.HasKey(e => new { e.Day, e.Rarity });
        });
    }
}
