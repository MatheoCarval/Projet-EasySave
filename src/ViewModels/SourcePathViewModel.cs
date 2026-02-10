namespace EasySave.ViewModels;

/// <summary>
/// ViewModel wrapper for a source path string to enable two-way binding
/// </summary>
public class SourcePathViewModel : ViewModelBase
{
    private string _path = string.Empty;

    public string Path
    {
        get => _path;
        set => SetProperty(ref _path, value);
    }

    public SourcePathViewModel()
    {
    }

    public SourcePathViewModel(string path)
    {
        _path = path;
    }
}
