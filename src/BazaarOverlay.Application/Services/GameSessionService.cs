using BazaarOverlay.Application.Interfaces;

namespace BazaarOverlay.Application.Services;

public class GameSessionService : IGameSessionService
{
    public string? CurrentHero { get; private set; }
    public int CurrentDay { get; private set; } = 1;

    public void SelectHero(string heroName)
    {
        if (string.IsNullOrWhiteSpace(heroName))
            throw new ArgumentException("Hero name cannot be empty.", nameof(heroName));

        CurrentHero = heroName;
        CurrentDay = 1;
    }

    public void AdvanceDay()
    {
        CurrentDay++;
    }

    public void Reset()
    {
        CurrentHero = null;
        CurrentDay = 1;
    }
}
