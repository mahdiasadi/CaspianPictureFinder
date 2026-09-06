using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.App.ViewModels;

public partial class PeopleViewModel : ObservableObject
{
    private readonly IPersonRepository _personRepository;
    private readonly IFaceRepository _faceRepository;

    [ObservableProperty] private ObservableCollection<PersonViewModel> _people = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private PersonViewModel? _selectedPerson;
    [ObservableProperty] private bool _showUnknownFaces = true;
    [ObservableProperty] private double _clusteringThreshold = 0.75;
    [ObservableProperty] private string _newPersonName = string.Empty;

    public PeopleViewModel(IPersonRepository personRepository, IFaceRepository faceRepository)
    {
        _personRepository = personRepository;
        _faceRepository = faceRepository;
        _ = LoadPeopleAsync();
    }

    [RelayCommand]
    private async Task LoadPeopleAsync()
    {
        IsLoading = true;
        try
        {
            People.Clear();
            var persons = await _personRepository.GetAllAsync();
            foreach (var person in persons)
            {
                People.Add(new PersonViewModel(person));
            }

            // Add unknown faces count
            if (_showUnknownFaces)
            {
                var unknownCount = await _faceRepository.GetUnknownCountAsync();
                if (unknownCount > 0)
                {
                    People.Insert(0, new PersonViewModel(new Person
                    {
                        Id = 0,
                        Name = $"Unknown ({unknownCount})",
                        PhotoCount = (int)unknownCount
                    }));
                }
            }

            StatusMessage = $"Found {People.Count} people";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleUnknownFacesAsync()
    {
        await LoadPeopleAsync();
    }

    [RelayCommand]
    private async Task ClusterUnknownFacesAsync()
    {
        StatusMessage = "Clustering unknown faces...";
        IsLoading = true;
        try
        {
            await _faceRepository.ClusterUnknownFacesAsync(_clusteringThreshold);
            await LoadPeopleAsync();
            StatusMessage = "Face clustering completed";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Clustering failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task MergePeopleAsync()
    {
        if (_selectedPerson == null || _selectedPerson.Id == 0) return;
        
        var targetName = _newPersonName.Trim();
        if (string.IsNullOrEmpty(targetName)) return;

        try
        {
            var targetPerson = (await _personRepository.GetAllAsync()).FirstOrDefault(p => p.Name == targetName);
            if (targetPerson == null)
            {
                targetPerson = new Person { Name = targetName };
                await _personRepository.AddAsync(targetPerson);
            }

            var sourceFaces = await _faceRepository.GetByPersonIdAsync(_selectedPerson.Id);
            var faceIds = sourceFaces.Select(f => f.Id).ToList();
            
            if (faceIds.Count > 0)
            {
                await _faceRepository.AssignToPersonAsync(faceIds, targetPerson.Id);
                await _personRepository.DeleteAsync(_selectedPerson.Id);
                await LoadPeopleAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Merge failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SplitPersonAsync()
    {
        if (_selectedPerson == null || _selectedPerson.Id == 0) return;
        
        var newName = _newPersonName.Trim();
        if (string.IsNullOrEmpty(newName)) return;

        try
        {
            var newPerson = new Person { Name = newName };
            await _personRepository.AddAsync(newPerson);
            
            // User would select faces to move in UI - placeholder
            StatusMessage = $"Created new person '{newName}'. Select faces to move in detail view.";
            _newPersonName = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Split failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RenamePersonAsync()
    {
        if (_selectedPerson == null || _selectedPerson.Id == 0) return;
        
        var newName = _newPersonName.Trim();
        if (string.IsNullOrEmpty(newName)) return;

        try
        {
            var person = await _personRepository.GetByIdAsync(_selectedPerson.Id);
            if (person != null)
            {
                person.Name = newName;
                await _personRepository.UpdateAsync(person);
                await LoadPeopleAsync();
                _newPersonName = string.Empty;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Rename failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AddReferencePhotosAsync()
    {
        if (_selectedPerson == null) return;
        
        StatusMessage = "Select reference photos in the detail view";
    }

    [RelayCommand]
    private async Task RemoveIncorrectFaceAsync(long faceId)
    {
        try
        {
            await _faceRepository.UnassignFromPersonAsync(faceId);
            await LoadPeopleAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Remove failed: {ex.Message}";
        }
    }
}

public partial class PersonViewModel : ObservableObject
{
    private readonly Person _person;

    public long Id => _person.Id;
    public string Name => _person.Name;
    public int PhotoCount => _person.PhotoCount;
    public DateTime? FirstSeen => _person.FirstSeen;
    public DateTime? LastSeen => _person.LastSeen;
    public string? RepresentativeFacePath => _person.RepresentativeFacePath;

    public PersonViewModel(Person person)
    {
        _person = person;
    }
}
