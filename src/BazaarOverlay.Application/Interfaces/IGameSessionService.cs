namespace BazaarOverlay.Application.Interfaces;

public interface IGameSessionService
{
    string? CurrentHero { get; }
    int CurrentDay { get; }

    void SelectHero(string heroName);
    void AdvanceDay();
    void Reset();
}
