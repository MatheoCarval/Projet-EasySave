using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Models;
using Models.Enums;
using Services.Managers;

namespace EasySave.ViewModels;

/// <summary>
/// Main ViewModel for the application
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly BackupManager _backupManager;
    private BackupJobViewModel? _selectedBackupJob;
    private bool _isAddEditModalOpen;
    private BackupJobViewModel? _editingBackupJob;
    private string _modalTitle = "Add Backup Task";

    // Modal form fields
    private string _modalName = string.Empty;
    private string _modalTargetPath = string.Empty;
    private int _modalBackupTypeIndex = 0;

    public MainViewModel(BackupManager backupManager)
    {
        _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));

        BackupJobs = new ObservableCollection<BackupJobViewModel>();
        ModalSourcePaths = new ObservableCollection<SourcePathViewModel>();

        // Commands
        AddBackupCommand = new RelayCommand(OpenAddModal);
        EditBackupCommand = new RelayCommand(OpenEditModal, () => SelectedBackupJob != null);
        DeleteBackupCommand = new RelayCommand(DeleteBackup, () => SelectedBackupJob != null);
        DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => BackupJobs.Any(j => j.IsSelected));
        SaveModalCommand = new RelayCommand(SaveModal);
        CancelModalCommand = new RelayCommand(CloseModal);
        ViewLogsCommand = new RelayCommand(ViewLogs);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        ExecuteBackupCommand = new RelayCommand(ExecuteBackup, () => SelectedBackupJob != null);
        ExecuteSelectedCommand = new RelayCommand(ExecuteSelected, () => BackupJobs.Any(j => j.IsSelected));
        AddSourcePathCommand = new RelayCommand(AddSourcePath);
        RemoveSourcePathCommand = new RelayCommand<SourcePathViewModel>(RemoveSourcePath);
        OpenEditModalForJobCommand = new RelayCommand<BackupJobViewModel>(OpenEditModalForJob);

        // Load real data from BackupManager
        LoadBackupJobs();
    }

    #region Properties

    public ObservableCollection<BackupJobViewModel> BackupJobs { get; }

    public ObservableCollection<SourcePathViewModel> ModalSourcePaths { get; }

    public BackupJobViewModel? SelectedBackupJob
    {
        get => _selectedBackupJob;
        set
        {
            if (SetProperty(ref _selectedBackupJob, value))
            {
                ((RelayCommand)EditBackupCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeleteBackupCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExecuteBackupCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsAddEditModalOpen
    {
        get => _isAddEditModalOpen;
        set => SetProperty(ref _isAddEditModalOpen, value);
    }

    public string ModalTitle
    {
        get => _modalTitle;
        set => SetProperty(ref _modalTitle, value);
    }

    public string ModalName
    {
        get => _modalName;
        set => SetProperty(ref _modalName, value);
    }

    public string ModalTargetPath
    {
        get => _modalTargetPath;
        set => SetProperty(ref _modalTargetPath, value);
    }

    public int ModalBackupTypeIndex
    {
        get => _modalBackupTypeIndex;
        set => SetProperty(ref _modalBackupTypeIndex, value);
    }

    public bool HasSelectedJobs => BackupJobs.Any(j => j.IsSelected);

    public bool HasNoJobs => BackupJobs.Count == 0;

    #endregion

    #region Commands

    public ICommand AddBackupCommand { get; }
    public ICommand EditBackupCommand { get; }
    public ICommand DeleteBackupCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand SaveModalCommand { get; }
    public ICommand CancelModalCommand { get; }
    public ICommand ViewLogsCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand ExecuteBackupCommand { get; }
    public ICommand ExecuteSelectedCommand { get; }
    public ICommand AddSourcePathCommand { get; }
    public ICommand RemoveSourcePathCommand { get; }
    public ICommand OpenEditModalForJobCommand { get; }

    #endregion

    #region Methods

    private void OpenAddModal()
    {
        ModalTitle = "Add Backup Task";
        _editingBackupJob = null;
        ModalName = string.Empty;
        ModalSourcePaths.Clear();
        ModalSourcePaths.Add(new SourcePathViewModel());
        ModalTargetPath = string.Empty;
        ModalBackupTypeIndex = 0;
        IsAddEditModalOpen = true;
    }

    private void OpenEditModal()
    {
        if (SelectedBackupJob == null) return;
        OpenEditModalForJob(SelectedBackupJob);
    }

    private void OpenEditModalForJob(BackupJobViewModel? job)
    {
        if (job == null) return;

        ModalTitle = "Edit Backup Task";
        _editingBackupJob = job;
        ModalName = job.Name;
        ModalSourcePaths.Clear();
        foreach (var source in job.GetBackupJob().SourcePath)
        {
            ModalSourcePaths.Add(new SourcePathViewModel(source));
        }
        ModalTargetPath = job.TargetPath;
        ModalBackupTypeIndex = job.BackupType == BackupType.COMPLETE ? 0 : 1;
        IsAddEditModalOpen = true;
    }

    private void SaveModal()
    {
        if (string.IsNullOrWhiteSpace(ModalName) ||
            !ModalSourcePaths.Any(s => !string.IsNullOrWhiteSpace(s.Path)) ||
            string.IsNullOrWhiteSpace(ModalTargetPath))
        {
            return;
        }

        var backupType = ModalBackupTypeIndex == 0 ? BackupType.COMPLETE : BackupType.DIFFERENTIAL;
        var sourcePaths = ModalSourcePaths.Where(s => !string.IsNullOrWhiteSpace(s.Path)).Select(s => s.Path).ToList();

        if (_editingBackupJob != null)
        {
            // Edit existing
            var job = _editingBackupJob.GetBackupJob();
            _backupManager.ModifyJob(job.Id, ModalName, sourcePaths, ModalTargetPath, backupType);

            // Reload the list to reflect changes
            ReloadBackupJobs();
        }
        else
        {
            // Add new
            var backupJob = _backupManager.CreateJob(ModalName, sourcePaths, ModalTargetPath, backupType);

            // Reload the list to include the new job
            ReloadBackupJobs();
        }

        CloseModal();
    }

    private void CloseModal()
    {
        IsAddEditModalOpen = false;
        _editingBackupJob = null;
    }

    private void DeleteBackup()
    {
        if (SelectedBackupJob != null)
        {
            _backupManager.DeleteJob(SelectedBackupJob.Id);
            SelectedBackupJob = null;
            ReloadBackupJobs();
        }
    }

    private void DeleteSelected()
    {
        var selectedJobs = BackupJobs.Where(j => j.IsSelected).ToList();
        foreach (var job in selectedJobs)
        {
            _backupManager.DeleteJob(job.Id);
        }
        ReloadBackupJobs();
    }

    private void ViewLogs()
    {
        // TODO: Implement logs view
    }

    private void OpenSettings()
    {
        // TODO: Implement settings view
    }

    private void ExecuteBackup()
    {
        if (SelectedBackupJob == null) return;

        try
        {
            _backupManager.ExecuteJob(SelectedBackupJob.Id);
            SelectedBackupJob.RefreshDisplay();
        }
        catch (Exception ex)
        {
            // TODO: Show error message to user
            System.Diagnostics.Debug.WriteLine($"Error executing backup: {ex.Message}");
        }
    }

    private void ExecuteSelected()
    {
        var selectedJobs = BackupJobs.Where(j => j.IsSelected).ToList();
        foreach (var job in selectedJobs)
        {
            try
            {
                _backupManager.ExecuteJob(job.Id);
                job.RefreshDisplay();
            }
            catch (Exception ex)
            {
                // TODO: Show error message to user
                System.Diagnostics.Debug.WriteLine($"Error executing backup {job.Name}: {ex.Message}");
            }
        }
    }

    private void AddSourcePath()
    {
        ModalSourcePaths.Add(new SourcePathViewModel());
    }

    private void RemoveSourcePath(SourcePathViewModel? sourcePathVm)
    {
        if (sourcePathVm != null && ModalSourcePaths.Count > 1)
        {
            ModalSourcePaths.Remove(sourcePathVm);
        }
    }

    private void LoadBackupJobs()
    {
        // Load all backup jobs from BackupManager
        var jobs = _backupManager.GetAllJobs();

        foreach (var job in jobs)
        {
            var viewModel = new BackupJobViewModel(job);
            viewModel.PropertyChanged += OnJobSelectionChanged;
            BackupJobs.Add(viewModel);
        }

        OnPropertyChanged(nameof(HasNoJobs));
    }

    private void ReloadBackupJobs()
    {
        // Clear existing jobs
        BackupJobs.Clear();

        // Reload all jobs from BackupManager
        var jobs = _backupManager.GetAllJobs();

        foreach (var job in jobs)
        {
            var viewModel = new BackupJobViewModel(job);
            viewModel.PropertyChanged += OnJobSelectionChanged;
            BackupJobs.Add(viewModel);
        }

        OnPropertyChanged(nameof(HasNoJobs));
        ((RelayCommand)DeleteSelectedCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ExecuteSelectedCommand).RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(HasSelectedJobs));
    }

    private void OnJobSelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BackupJobViewModel.IsSelected))
        {
            ((RelayCommand)DeleteSelectedCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ExecuteSelectedCommand).RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(HasSelectedJobs));
        }
    }

    #endregion
}

/// <summary>
/// Simple ICommand implementation for button commands
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// Generic ICommand implementation for button commands with parameter
/// </summary>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter) => _execute((T?)parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
