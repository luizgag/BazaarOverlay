using System.Collections.ObjectModel;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IGameSessionService _gameSession;
    private readonly IHeroRepository _heroRepository;
    private readonly IDataImportService _dataImportService;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private string? _selectedHero;

    [ObservableProperty]
    private int _currentDay = 1;

    [ObservableProperty]
    private bool _isHeroSelected;

    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private string _importStatus = string.Empty;

    public ObservableCollection<string> Heroes { get; } = new();

    public MonsterEncounterViewModel MonsterEncounter { get; }
    public ItemSkillInfoViewModel ItemSkillInfo { get; }

    public MainViewModel(
        IGameSessionService gameSession,
        IHeroRepository heroRepository,
        IDataImportService dataImportService,
        MonsterEncounterViewModel monsterEncounterViewModel,
        ItemSkillInfoViewModel itemSkillInfoViewModel,
        ILogger<MainViewModel> logger)
    {
        _gameSession = gameSession;
        _heroRepository = heroRepository;
        _dataImportService = dataImportService;
        MonsterEncounter = monsterEncounterViewModel;
        ItemSkillInfo = itemSkillInfoViewModel;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var heroes = await _heroRepository.GetAllAsync();
        Heroes.Clear();
        foreach (var hero in heroes.Where(h => h.Name != "Neutral"))
        {
            Heroes.Add(hero.Name);
        }
    }

    partial void OnSelectedHeroChanged(string? value)
    {
        if (value is not null)
        {
            _gameSession.SelectHero(value);
            CurrentDay = _gameSession.CurrentDay;
            IsHeroSelected = true;
        }
    }

    [RelayCommand]
    private void AdvanceDay()
    {
        _gameSession.AdvanceDay();
        CurrentDay = _gameSession.CurrentDay;
    }

    [RelayCommand]
    private async Task DownloadDataAsync()
    {
        if (IsImporting)
            return;

        IsImporting = true;
        try
        {
            var progress = new Progress<string>(msg => ImportStatus = msg);
            await _dataImportService.ImportAllAsync(progress);
            await InitializeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download data failed");
            ImportStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsImporting = false;
        }
    }
}
