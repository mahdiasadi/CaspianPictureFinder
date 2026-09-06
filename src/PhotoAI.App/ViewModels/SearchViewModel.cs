using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhotoAI.Core.Models;
using PhotoAI.Search;

namespace PhotoAI.App.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly HybridSearchService _hybridSearch;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private ObservableCollection<MediaItemViewModel> _results = new();
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private int _resultCount;
    [ObservableProperty] private string _selectedFilter = "All";

    public string[] Filters { get; } = ["All", "Images", "Videos", "People", "Objects", "Scenes"];

    public SearchViewModel(HybridSearchService hybridSearch)
    {
        _hybridSearch = hybridSearch;
    }

    [RelayCommand]
    public async Task ExecuteSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return;

        IsSearching = true;
        Results.Clear();

        try
        {
            var query = new SearchQuery
            {
                Text = SearchText,
                MediaTypes = SelectedFilter switch
                {
                    "Images" => new[] { MediaType.Image },
                    "Videos" => new[] { MediaType.Video },
                    _ => null
                }
            };

            var results = await _hybridSearch.SearchAsync(query, 200);

            foreach (var item in results)
            {
                Results.Add(new MediaItemViewModel(item.MediaItem));
            }

            ResultCount = Results.Count;
            StatusMessage = $"Found {ResultCount} results for \"{SearchText}\"";
        }
        catch (Exception ex)
        {
            StatusMessage = "Search failed";
        }
        finally
        {
            IsSearching = false;
        }
    }
}
