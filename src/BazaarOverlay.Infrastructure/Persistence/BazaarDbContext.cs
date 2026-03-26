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
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<Trainer> Trainers => Set<Trainer>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventOption> EventOptions => Set<EventOption>();

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
            entity.HasMany(e => e.BoardItems)
                .WithMany()
                .UsingEntity("MonsterBoardItem");
            entity.HasMany(e => e.BoardSkills)
                .WithMany()
                .UsingEntity("MonsterBoardSkill");
        });

        modelBuilder.Entity<Enchantment>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.ItemPool)
                .WithMany()
                .UsingEntity("MerchantItem");
        });

        modelBuilder.Entity<Trainer>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.SkillPool)
                .WithMany()
                .UsingEntity("TrainerSkill");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.Options)
                .WithOne(o => o.Event)
                .HasForeignKey(o => o.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
