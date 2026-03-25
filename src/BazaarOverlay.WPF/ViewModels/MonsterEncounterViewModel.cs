using System.Collections.ObjectModel;
using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BazaarOverlay.WPF.ViewModels;

public partial class MonsterEncounterViewModel : ObservableObject
{
    private readonly IEncounterService _encounterService;
    private readonly IGameSessionService _gameSession;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private EncounterResult? _selectedEncounter;

    [ObservableProperty]
    private bool _isSearching;

    public ObservableCollection<EncounterResult> SearchResults { get; } = new();

    public MonsterEncounterViewModel(
        IEncounterService encounterService,
        IGameSessionService gameSession)
    {
        _encounterService = encounterService;
        _gameSession = gameSession;
    }

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length >= 2)
        {
            _ = SearchAsync(value);
        }
        else
        {
            SearchResults.Clear();
            SelectedEncounter = null;
        }
    }

    [RelayCommand]
    private async Task SearchAsync(string? searchText = null)
    {
        var query = searchText ?? SearchText;
        if (string.IsNullOrWhiteSpace(query))
            return;

        IsSearching = true;
        try
        {
            var results = await _encounterService.SearchEncountersAsync(
                query, _gameSession.CurrentHero, _gameSession.CurrentDay);

            SearchResults.Clear();
            foreach (var result in results)
            {
                SearchResults.Add(result);
            }

            SelectedEncounter = SearchResults.FirstOrDefault();
        }
        finally
        {
            IsSearching = false;
        }
    }
}
