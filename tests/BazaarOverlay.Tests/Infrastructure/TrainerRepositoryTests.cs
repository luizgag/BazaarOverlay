using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Infrastructure.Persistence.Repositories;
using BazaarOverlay.Tests.Infrastructure;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class TrainerRepositoryTests : IDisposable
{
    private readonly BazaarOverlay.Infrastructure.Persistence.BazaarDbContext _context;
    private readonly TrainerRepository _repository;

    public TrainerRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new TrainerRepository(_context);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingTrainer_ReturnsTrainer()
    {
        _context.Trainers.Add(new Trainer("Adira", Rarity.Diamond, tooltip: "Teaches Diamond skills"));
        await _context.SaveChangesAsync();

        var result = await _repository.GetByNameAsync("Adira");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Adira");
        result.Tier.ShouldBe(Rarity.Diamond);
    }

    [Fact]
    public async Task GetByNameAsync_WithSkillPool_ReturnsSkillPool()
    {
        var skill = new Skill("Burning Temper", Rarity.Bronze);
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();

        var trainer = new Trainer("Adira", Rarity.Diamond, tooltip: "Teaches skills");
        trainer.SkillPool.Add(skill);
        _context.Trainers.Add(trainer);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByNameAsync("Adira");

        result.ShouldNotBeNull();
        result.SkillPool.Count.ShouldBe(1);
    }

    public void Dispose() => _context.Dispose();
}
