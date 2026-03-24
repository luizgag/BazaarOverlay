using System.Collections.ObjectModel;
using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BazaarOverlay.WPF.ViewModels;

public partial class ItemSkillInfoViewModel : ObservableObject
{
    private readonly IItemInfoService _itemInfoService;
    private readonly ISkillInfoService _skillInfoService;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ItemInfoResult? _selectedItem;

    [ObservableProperty]
    private SkillInfoResult? _selectedSkill;

    [ObservableProperty]
    private bool _isItemSelected;

    [ObservableProperty]
    private bool _isSkillSelected;

    public ObservableCollection<string> SearchResults { get; } = new();

    private readonly List<ItemInfoResult> _itemResults = new();
    private readonly List<SkillInfoResult> _skillResults = new();

    public ItemSkillInfoViewModel(
        IItemInfoService itemInfoService,
        ISkillInfoService skillInfoService)
    {
        _itemInfoService = itemInfoService;
        _skillInfoService = skillInfoService;
    }

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length >= 2)
        {
            _ = SearchAsync(value);
        }
        else
        {
            ClearResults();
        }
    }

    private async Task SearchAsync(string query)
    {
        var items = await _itemInfoService.SearchItemsAsync(query);
        var skills = await _skillInfoService.SearchSkillsAsync(query);

        _itemResults.Clear();
        _itemResults.AddRange(items);
        _skillResults.Clear();
        _skillResults.AddRange(skills);

        SearchResults.Clear();
        foreach (var item in items)
            SearchResults.Add($"[Item] {item.Name}");
        foreach (var skill in skills)
            SearchResults.Add($"[Skill] {skill.Name}");
    }

    public void SelectResult(string displayName)
    {
        if (displayName.StartsWith("[Item] "))
        {
            var name = displayName.Substring(7);
            SelectedItem = _itemResults.FirstOrDefault(i => i.Name == name);
            SelectedSkill = null;
            IsItemSelected = SelectedItem is not null;
            IsSkillSelected = false;
        }
        else if (displayName.StartsWith("[Skill] "))
        {
            var name = displayName.Substring(8);
            SelectedSkill = _skillResults.FirstOrDefault(s => s.Name == name);
            SelectedItem = null;
            IsSkillSelected = SelectedSkill is not null;
            IsItemSelected = false;
        }
    }

    private void ClearResults()
    {
        SearchResults.Clear();
        _itemResults.Clear();
        _skillResults.Clear();
        SelectedItem = null;
        SelectedSkill = null;
        IsItemSelected = false;
        IsSkillSelected = false;
    }
}
