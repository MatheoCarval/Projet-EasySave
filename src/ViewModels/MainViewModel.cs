using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Models;
using Models.Enums;
using Models.Entries;
using Services.Managers;
using EasySave.Services.Managers;
using EasyLog.Abstractions;
using EasyLog.Enums;
using EasyLog.Loggers;
using Avalonia.Threading;

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
    private bool _modalEncryptFiles;
    private string _modalEncryptedExtensions = string.Empty;
    private bool _isProgressPopupOpen;
    private bool _isExecuteOrderOpen;
    private bool _isDeleteConfirmOpen;
    private string _deleteConfirmMessage = string.Empty;
    private bool _isBlockedPopupOpen;
    private string _blockedPopupMessage = string.Empty;
    private bool _isSettingsOpen;
    private bool _isHelpOpen;
    private bool _isLogsOpen;
    private string _logsMessage = string.Empty;
    private string _toastMessage = string.Empty;
    private bool _isToastVisible;
    private DispatcherTimer? _toastTimer;

    public ObservableCollection<BackupJobViewModel> ExecuteOrderJobs { get; } = new();
    public ObservableCollection<BackupLogEntry> LogEntries { get; } = new();
    public SettingsViewModel SettingsVM { get; }

    // Modal form fields
    private string _modalName = string.Empty;
    private string _modalTargetPath = string.Empty;
    private int _modalBackupTypeIndex = 0;
    private string _modalValidationError = string.Empty;
    private string _searchText = string.Empty;
    private bool _isFilterOpen;
    private readonly HashSet<string> _selectedTypes = new(StringComparer.OrdinalIgnoreCase) { "COMPLETE", "DIFFERENTIAL" };
    private readonly HashSet<string> _selectedStates = new(StringComparer.OrdinalIgnoreCase) { "ACTIVE", "PAUSED", "COMPLETED", "ERROR", "PENDING" };

    private static readonly string[] AllTypes = { "COMPLETE", "DIFFERENTIAL" };
    private static readonly string[] AllStates = { "ACTIVE", "PAUSED", "COMPLETED", "ERROR", "PENDING" };

    // Progress tracking
    private readonly ProgressViewModel _progressViewModel;

    public MainViewModel(BackupManager backupManager)
    {
        _backupManager = backupManager ?? throw new ArgumentNullException(nameof(backupManager));

        BackupJobs = new ObservableCollection<BackupJobViewModel>();
        FilteredBackupJobs = new ObservableCollection<BackupJobViewModel>();
        ModalSourcePaths = new ObservableCollection<SourcePathViewModel>();
        _progressViewModel = new ProgressViewModel();
        SettingsVM = new SettingsViewModel();

        // When settings are saved, update blocked applications immediately
        SettingsVM.SettingsSaved += OnSettingsSaved;

        // Subscribe to progress events
        _backupManager.FileTransferred += OnFileTransferred;

        // Commands
        AddBackupCommand = new RelayCommand(OpenAddModal);
        EditBackupCommand = new RelayCommand(OpenEditModal, () => SelectedBackupJob != null);
        DeleteBackupCommand = new RelayCommand(DeleteBackup, () => SelectedBackupJob != null);
        DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => BackupJobs.Any(j => j.IsSelected));
        SaveModalCommand = new RelayCommand(SaveModal);
        CancelModalCommand = new RelayCommand(CloseModal);
        ViewLogsCommand = new RelayCommand(ViewLogs);
        CloseLogsCommand = new RelayCommand(() => IsLogsOpen = false);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        OpenHelpCommand = new RelayCommand(OpenHelp);
        GoHomeCommand = new RelayCommand(GoHome);
        ExecuteBackupCommand = new RelayCommand(ExecuteBackup, () => SelectedBackupJob != null);
        ExecuteSelectedCommand = new RelayCommand(ExecuteSelected, () => BackupJobs.Any(j => j.IsSelected));
        AddSourcePathCommand = new RelayCommand(AddSourcePath);
        RemoveSourcePathCommand = new RelayCommand<SourcePathViewModel>(RemoveSourcePath);
        OpenEditModalForJobCommand = new RelayCommand<BackupJobViewModel>(OpenEditModalForJob);
        ExecuteEditingJobCommand = new RelayCommand(ExecuteEditingJob, () => _editingBackupJob != null);
        CloseProgressCommand = new RelayCommand(CloseProgress);
        ShowExecuteOrderCommand = new RelayCommand(ShowExecuteOrder, () => BackupJobs.Any(j => j.IsSelected));
        CancelExecuteOrderCommand = new RelayCommand(() => IsExecuteOrderOpen = false);
        ConfirmExecuteOrderCommand = new RelayCommand(ConfirmExecuteOrder);
        MoveJobUpCommand = new RelayCommand<BackupJobViewModel>(MoveJobUp);
        MoveJobDownCommand = new RelayCommand<BackupJobViewModel>(MoveJobDown);
        ShowDeleteConfirmCommand = new RelayCommand(ShowDeleteConfirm, () => BackupJobs.Any(j => j.IsSelected));
        CancelDeleteCommand = new RelayCommand(() => IsDeleteConfirmOpen = false);
        ConfirmDeleteCommand = new RelayCommand(ConfirmDelete);
        CloseBlockedPopupCommand = new RelayCommand(CloseBlockedPopup);
        DeselectAllCommand = new RelayCommand(DeselectAll);
        SelectAllCommand = new RelayCommand(ToggleSelectAll);
        ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
        ToggleFilterCommand = new RelayCommand(() => IsFilterOpen = !IsFilterOpen);
        ToggleFilterTypeCommand = new RelayCommand<string>(ToggleFilterType);
        ToggleFilterStateCommand = new RelayCommand<string>(ToggleFilterState);
        ToggleAllTypesCommand = new RelayCommand(ToggleAllTypes);
        ToggleAllStatesCommand = new RelayCommand(ToggleAllStates);
        ClearFiltersCommand = new RelayCommand(ClearFilters);
        DismissToastCommand = new RelayCommand(() => { IsToastVisible = false; _toastTimer?.Stop(); });

        // Load real data from BackupManager
        LoadBackupJobs();
    }

    #region Properties

    public ObservableCollection<BackupJobViewModel> BackupJobs { get; }

    public ObservableCollection<BackupJobViewModel> FilteredBackupJobs { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

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

    public string ModalValidationError
    {
        get => _modalValidationError;
        set => SetProperty(ref _modalValidationError, value);
    }

    public bool ModalEncryptFiles
    {
        get => _modalEncryptFiles;
        set => SetProperty(ref _modalEncryptFiles, value);
    }

    public string ModalEncryptedExtensions
    {
        get => _modalEncryptedExtensions;
        set => SetProperty(ref _modalEncryptedExtensions, value);
    }

    public bool IsEditMode => _editingBackupJob != null;

    public bool HasSelectedJobs => BackupJobs.Any(j => j.IsSelected);

    public int SelectedJobsCount => BackupJobs.Count(j => j.IsSelected);

    public bool AreAllSelected => BackupJobs.Count > 0 && BackupJobs.All(j => j.IsSelected);

    public bool HasNoJobs => BackupJobs.Count == 0;

    public bool HasNoFilteredJobs => FilteredBackupJobs.Count == 0 && !HasNoJobs;

    public bool IsFilterOpen
    {
        get => _isFilterOpen;
        set => SetProperty(ref _isFilterOpen, value);
    }

    // Type filter booleans
    public bool IsTypeAllSelected => _selectedTypes.Count == AllTypes.Length;
    public bool IsTypeCompleteSelected => _selectedTypes.Contains("COMPLETE");
    public bool IsTypeDifferentialSelected => _selectedTypes.Contains("DIFFERENTIAL");

    // State filter booleans
    public bool IsStateAllSelected => _selectedStates.Count == AllStates.Length;
    public bool IsStatePendingSelected => _selectedStates.Contains("PENDING");
    public bool IsStateActiveSelected => _selectedStates.Contains("ACTIVE");
    public bool IsStateCompletedSelected => _selectedStates.Contains("COMPLETED");
    public bool IsStatePausedSelected => _selectedStates.Contains("PAUSED");
    public bool IsStateErrorSelected => _selectedStates.Contains("ERROR");

    public bool HasActiveFilters => _selectedTypes.Count < AllTypes.Length || _selectedStates.Count < AllStates.Length;

    public bool IsProgressPopupOpen
    {
        get => _isProgressPopupOpen;
        set => SetProperty(ref _isProgressPopupOpen, value);
    }

    public ProgressViewModel ProgressViewModel => _progressViewModel;

    public bool IsBlockedPopupOpen
    {
        get => _isBlockedPopupOpen;
        set => SetProperty(ref _isBlockedPopupOpen, value);
    }

    public string BlockedPopupMessage
    {
        get => _blockedPopupMessage;
        set => SetProperty(ref _blockedPopupMessage, value);
    }

    public string TxtBlockedPopupTitle => T("gui_blocked_popup_title");
    public string TxtBlockedPopupClose => T("gui_blocked_popup_close");
    public string TxtLastExecution => T("gui_last_execution");
    public string TxtProgress => T("gui_progress");
    public string TxtBackupTasks => T("gui_backup_tasks");
    public string TxtManageSubtitle => T("gui_manage_subtitle");
    public string TxtTotal => T("gui_total");
    public string TxtActive => T("gui_active");
    public string TxtDone => T("gui_done");
    public string TxtErrors => T("gui_errors");
    public string TxtSelectAll => T("gui_select_all");
    public string TxtSearchByName => T("gui_search_by_name");
    public string TxtNoTasksFound => T("gui_no_tasks_found");

    public bool IsExecuteOrderOpen
    {
        get => _isExecuteOrderOpen;
        set => SetProperty(ref _isExecuteOrderOpen, value);
    }

    public bool IsDeleteConfirmOpen
    {
        get => _isDeleteConfirmOpen;
        set => SetProperty(ref _isDeleteConfirmOpen, value);
    }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set
        {
            if (SetProperty(ref _isSettingsOpen, value))
            {
                OnPropertyChanged(nameof(IsHomeActive));
                OnPropertyChanged(nameof(IsHelpOpen));
            }
        }
    }

    public bool IsHelpOpen
    {
        get => _isHelpOpen;
        set
        {
            if (SetProperty(ref _isHelpOpen, value))
            {
                OnPropertyChanged(nameof(IsHomeActive));
                OnPropertyChanged(nameof(IsSettingsOpen));
            }
        }
    }

    public bool IsLogsOpen
    {
        get => _isLogsOpen;
        set => SetProperty(ref _isLogsOpen, value);
    }

    public string LogsMessage
    {
        get => _logsMessage;
        set => SetProperty(ref _logsMessage, value);
    }

    public string DeleteConfirmMessage
    {
        get => _deleteConfirmMessage;
        set => SetProperty(ref _deleteConfirmMessage, value);
    }

    public bool IsHomeActive => !IsSettingsOpen && !IsHelpOpen;

    // Dashboard stats
    public int TotalJobsCount => BackupJobs.Count;
    public int ActiveJobsCount => BackupJobs.Count(j => j.BackupState == BackupState.ACTIVE);
    public int CompletedJobsCount => BackupJobs.Count(j => j.BackupState == BackupState.COMPLETED);
    public int ErrorJobsCount => BackupJobs.Count(j => j.BackupState == BackupState.ERROR);
    public bool HasActiveOrErrorJobs => BackupJobs.Any(j => j.BackupState == BackupState.ACTIVE || j.BackupState == BackupState.ERROR);

    // Toast notification
    public string ToastMessage
    {
        get => _toastMessage;
        set => SetProperty(ref _toastMessage, value);
    }

    public bool IsToastVisible
    {
        get => _isToastVisible;
        set => SetProperty(ref _isToastVisible, value);
    }

    #endregion

    #region Commands

    public ICommand AddBackupCommand { get; }
    public ICommand EditBackupCommand { get; }
    public ICommand DeleteBackupCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand SaveModalCommand { get; }
    public ICommand CancelModalCommand { get; }
    public ICommand ViewLogsCommand { get; }
    public ICommand CloseLogsCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenHelpCommand { get; }
    public ICommand GoHomeCommand { get; }
    public ICommand ExecuteBackupCommand { get; }
    public ICommand ExecuteSelectedCommand { get; }
    public ICommand AddSourcePathCommand { get; }
    public ICommand RemoveSourcePathCommand { get; }
    public ICommand OpenEditModalForJobCommand { get; }
    public ICommand ExecuteEditingJobCommand { get; }
    public ICommand CloseProgressCommand { get; }
    public ICommand ShowExecuteOrderCommand { get; }
    public ICommand CancelExecuteOrderCommand { get; }
    public ICommand ConfirmExecuteOrderCommand { get; }
    public ICommand MoveJobUpCommand { get; }
    public ICommand MoveJobDownCommand { get; }
    public ICommand ShowDeleteConfirmCommand { get; }
    public ICommand CancelDeleteCommand { get; }
    public ICommand ConfirmDeleteCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand ToggleFilterCommand { get; }
    public ICommand ToggleFilterTypeCommand { get; }
    public ICommand ToggleFilterStateCommand { get; }
    public ICommand ToggleAllTypesCommand { get; }
    public ICommand ToggleAllStatesCommand { get; }
    public ICommand ClearFiltersCommand { get; }
    public ICommand DismissToastCommand { get; }
    public ICommand CloseBlockedPopupCommand { get; }

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
        ModalEncryptFiles = false;
        ModalEncryptedExtensions = string.Empty;
        ModalValidationError = string.Empty;
        IsAddEditModalOpen = true;
        OnPropertyChanged(nameof(IsEditMode));
        ((RelayCommand)ExecuteEditingJobCommand).RaiseCanExecuteChanged();
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
        ModalValidationError = string.Empty;
        ModalSourcePaths.Clear();
        foreach (var source in job.GetBackupJob().SourcePath)
        {
            ModalSourcePaths.Add(new SourcePathViewModel(source));
        }
        ModalTargetPath = job.TargetPath;
        ModalBackupTypeIndex = job.BackupType == BackupType.COMPLETE ? 0 : 1;
        ModalEncryptFiles = job.GetBackupJob().EncryptFiles;
        ModalEncryptedExtensions = string.Join(", ", job.GetBackupJob().EncryptedExtensions);
        IsAddEditModalOpen = true;
        OnPropertyChanged(nameof(IsEditMode));
        ((RelayCommand)ExecuteEditingJobCommand).RaiseCanExecuteChanged();
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
        var encryptedExtensions = ParseEncryptedExtensions(ModalEncryptedExtensions);

        try
        {
            if (_editingBackupJob != null)
            {
                // Edit existing
                var job = _editingBackupJob.GetBackupJob();
                job.EncryptFiles = ModalEncryptFiles;
                job.EncryptedExtensions = encryptedExtensions;
                _backupManager.ModifyJob(job.Id, ModalName, sourcePaths, ModalTargetPath, backupType);

                // Reload the list to reflect changes
                ReloadBackupJobs();
            }
            else
            {
                // Add new
                var backupJob = _backupManager.CreateJob(ModalName, sourcePaths, ModalTargetPath, backupType);
                backupJob.EncryptFiles = ModalEncryptFiles;
                backupJob.EncryptedExtensions = encryptedExtensions;
                _backupManager.SaveJob(backupJob);

                // Reload the list to include the new job
                ReloadBackupJobs();
            }

            CloseModal();
            ShowToast("Backup task saved!");
        }
        catch (ArgumentException)
        {
            // Duplicate name — just keep the modal open so the user can fix it
            ModalValidationError = "A job with this name already exists.";
        }
        catch (InvalidOperationException)
        {
            // Max jobs reached
            ModalValidationError = "Maximum number of jobs reached.";
        }
    }

    private void CloseModal()
    {
        IsAddEditModalOpen = false;
        _editingBackupJob = null;
        OnPropertyChanged(nameof(IsEditMode));
        ((RelayCommand)ExecuteEditingJobCommand).RaiseCanExecuteChanged();
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

    private void ShowDeleteConfirm()
    {
        var selected = BackupJobs.Where(j => j.IsSelected).ToList();
        if (selected.Count == 0) return;
        DeleteConfirmMessage = selected.Count == 1
            ? $"Are you sure you want to delete \"{selected[0].Name}\"?"
            : $"Are you sure you want to delete {selected.Count} backup jobs?";
        IsDeleteConfirmOpen = true;
    }

    private void ConfirmDelete()
    {
        IsDeleteConfirmOpen = false;
        DeleteSelected();
        ShowToast("Backup task(s) deleted.");
    }

    private void ShowExecuteOrder()
    {
        ExecuteOrderJobs.Clear();
        foreach (var job in BackupJobs.Where(j => j.IsSelected))
            ExecuteOrderJobs.Add(job);
        if (ExecuteOrderJobs.Count == 0) return;

        // If only 1 task selected, skip the order popup and execute directly
        if (ExecuteOrderJobs.Count == 1)
        {
            ConfirmExecuteOrder();
            return;
        }

        UpdateOrderIndices();
        IsExecuteOrderOpen = true;
    }

    private void MoveJobUp(BackupJobViewModel? job)
    {
        if (job == null) return;
        var idx = ExecuteOrderJobs.IndexOf(job);
        if (idx > 0)
        {
            ExecuteOrderJobs.Move(idx, idx - 1);
            UpdateOrderIndices();
        }
    }

    private void MoveJobDown(BackupJobViewModel? job)
    {
        if (job == null) return;
        var idx = ExecuteOrderJobs.IndexOf(job);
        if (idx >= 0 && idx < ExecuteOrderJobs.Count - 1)
        {
            ExecuteOrderJobs.Move(idx, idx + 1);
            UpdateOrderIndices();
        }
    }

    public void UpdateOrderIndices()
    {
        for (int i = 0; i < ExecuteOrderJobs.Count; i++)
            ExecuteOrderJobs[i].OrderIndex = i + 1;
    }

    private async void ConfirmExecuteOrder()
    {
        IsExecuteOrderOpen = false;
        var orderedJobs = ExecuteOrderJobs.ToList();
        for (int i = 0; i < orderedJobs.Count; i++)
        {
            var job = orderedJobs[i];
            var isLastJob = i == orderedJobs.Count - 1;
            try
            {
                ShowProgressPopup(job.Name);
                var jobId = job.Id;
                var jobToRefresh = job;
                await Task.Run(() => _backupManager.ExecuteJob(jobId));
                jobToRefresh.RefreshDisplay();
                if (isLastJob)
                    _progressViewModel.IsCompleted = true;
            }
            catch (Exception ex)
            {
                HideProgressPopup();
                if (!HandleBlockedAppException(ex))
                    System.Diagnostics.Debug.WriteLine($"Error executing backup {job.Name}: {ex.Message}");
                break;
            }
        }
    }

    private void ViewLogs()
    {
        LogEntries.Clear();
        LogsMessage = string.Empty;

        try
        {
            var config = ConfigurationManager.GetInstance().LoadConfiguration();
            var logPath = config.GetLogFilePath();
            if (string.IsNullOrWhiteSpace(logPath))
            {
                logPath = config.GetDefaultLogPath();
            }

            ILogger logger = config.GetLogFormat() == LogFormat.XML
                ? new XmlLogger(logPath)
                : new JsonLogger(logPath);

            var entries = logger.ReadLog<BackupLogEntry>()
                .OrderByDescending(entry => entry.Timestamp)
                .ToList();

            foreach (var entry in entries)
            {
                LogEntries.Add(entry);
            }

            if (LogEntries.Count == 0)
            {
                LogsMessage = T("gui_logs_empty");
            }
        }
        catch (Exception ex)
        {
            LogsMessage = $"Error: {ex.Message}";
        }

        IsLogsOpen = true;
    }

    private void OpenSettings()
    {
        SettingsVM.LoadSettings();
        _isHelpOpen = false;
        OnPropertyChanged(nameof(IsHelpOpen));
        IsSettingsOpen = true;
    }

    private void OpenHelp()
    {
        _isSettingsOpen = false;
        OnPropertyChanged(nameof(IsSettingsOpen));
        IsHelpOpen = true;
        RefreshHelpTranslations();
    }

    private void GoHome()
    {
        IsSettingsOpen = false;
        IsHelpOpen = false;
    }

    private async void ExecuteBackup()
    {
        if (SelectedBackupJob == null) return;

        var jobId = SelectedBackupJob.Id;
        var jobToRefresh = SelectedBackupJob;

        try
        {
            ShowProgressPopup(SelectedBackupJob.Name);

            // Execute on background thread to keep UI responsive
            await Task.Run(() => _backupManager.ExecuteJob(jobId));

            // Back on UI thread after await - refresh and mark as completed
            jobToRefresh.RefreshDisplay();
            _progressViewModel.IsCompleted = true;
        }
        catch (Exception ex)
        {
            HideProgressPopup();
            if (!HandleBlockedAppException(ex))
                System.Diagnostics.Debug.WriteLine($"Error executing backup: {ex.Message}");
        }
    }

    private async void ExecuteSelected()
    {
        var selectedJobs = BackupJobs.Where(j => j.IsSelected).ToList();
        for (int i = 0; i < selectedJobs.Count; i++)
        {
            var job = selectedJobs[i];
            var isLastJob = i == selectedJobs.Count - 1;

            try
            {
                ShowProgressPopup(job.Name);

                var jobId = job.Id;
                var jobToRefresh = job;

                // Execute on background thread
                await Task.Run(() => _backupManager.ExecuteJob(jobId));

                // Back on UI thread after await - refresh
                jobToRefresh.RefreshDisplay();

                // Only mark as completed on the last job
                if (isLastJob)
                {
                    _progressViewModel.IsCompleted = true;
                }
            }
            catch (Exception ex)
            {
                HideProgressPopup();
                if (!HandleBlockedAppException(ex))
                    System.Diagnostics.Debug.WriteLine($"Error executing backup {job.Name}: {ex.Message}");
                break;
            }
        }
    }

    private async void ExecuteEditingJob()
    {
        if (_editingBackupJob == null) return;

        var jobId = _editingBackupJob.Id;
        var jobToRefresh = _editingBackupJob;

        try
        {
            ShowProgressPopup(_editingBackupJob.Name);

            // Execute on background thread
            await Task.Run(() => _backupManager.ExecuteJob(jobId));

            // Back on UI thread after await - refresh and mark as completed
            jobToRefresh.RefreshDisplay();
            _progressViewModel.IsCompleted = true;
        }
        catch (Exception ex)
        {
            HideProgressPopup();
            if (!HandleBlockedAppException(ex))
                System.Diagnostics.Debug.WriteLine($"Error executing backup: {ex.Message}");
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
        ApplyFilter();
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
        ((RelayCommand)ShowExecuteOrderCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ShowDeleteConfirmCommand).RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(HasSelectedJobs));
        OnPropertyChanged(nameof(SelectedJobsCount));
        OnPropertyChanged(nameof(AreAllSelected));
        NotifyStats();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredBackupJobs.Clear();
        var query = _searchText?.Trim() ?? string.Empty;

        IEnumerable<BackupJobViewModel> filtered = BackupJobs;

        // Text search: name only
        if (!string.IsNullOrEmpty(query))
            filtered = filtered.Where(j => j.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

        // Type filter
        if (_selectedTypes.Count < AllTypes.Length)
            filtered = filtered.Where(j => _selectedTypes.Contains(j.BackupTypeDisplay));

        // State filter
        if (_selectedStates.Count < AllStates.Length)
            filtered = filtered.Where(j => _selectedStates.Contains(j.BackupStateDisplay));

        foreach (var job in filtered)
            FilteredBackupJobs.Add(job);

        OnPropertyChanged(nameof(HasNoFilteredJobs));
    }

    private void ToggleFilterType(string? type)
    {
        if (type == null) return;
        if (_selectedTypes.Contains(type))
        {
            if (_selectedTypes.Count > 1) // keep at least one
                _selectedTypes.Remove(type);
        }
        else
            _selectedTypes.Add(type);
        NotifyFilterTypeChanged();
        ApplyFilter();
    }

    private void ToggleFilterState(string? state)
    {
        if (state == null) return;
        if (_selectedStates.Contains(state))
        {
            if (_selectedStates.Count > 1)
                _selectedStates.Remove(state);
        }
        else
            _selectedStates.Add(state);
        NotifyFilterStateChanged();
        ApplyFilter();
    }

    private void ToggleAllTypes()
    {
        if (_selectedTypes.Count == AllTypes.Length)
            _selectedTypes.Clear();
        else
            foreach (var t in AllTypes) _selectedTypes.Add(t);
        NotifyFilterTypeChanged();
        ApplyFilter();
    }

    private void ToggleAllStates()
    {
        if (_selectedStates.Count == AllStates.Length)
            _selectedStates.Clear();
        else
            foreach (var s in AllStates) _selectedStates.Add(s);
        NotifyFilterStateChanged();
        ApplyFilter();
    }

    private void ClearFilters()
    {
        foreach (var t in AllTypes) _selectedTypes.Add(t);
        foreach (var s in AllStates) _selectedStates.Add(s);
        NotifyFilterTypeChanged();
        NotifyFilterStateChanged();
        ApplyFilter();
        IsFilterOpen = false;
    }

    private void NotifyFilterTypeChanged()
    {
        OnPropertyChanged(nameof(IsTypeAllSelected));
        OnPropertyChanged(nameof(IsTypeCompleteSelected));
        OnPropertyChanged(nameof(IsTypeDifferentialSelected));
        OnPropertyChanged(nameof(HasActiveFilters));
    }

    private void NotifyFilterStateChanged()
    {
        OnPropertyChanged(nameof(IsStateAllSelected));
        OnPropertyChanged(nameof(IsStatePendingSelected));
        OnPropertyChanged(nameof(IsStateActiveSelected));
        OnPropertyChanged(nameof(IsStateCompletedSelected));
        OnPropertyChanged(nameof(IsStatePausedSelected));
        OnPropertyChanged(nameof(IsStateErrorSelected));
        OnPropertyChanged(nameof(HasActiveFilters));
    }

    private void OnJobSelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BackupJobViewModel.IsSelected))
        {
            ((RelayCommand)DeleteSelectedCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ExecuteSelectedCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ShowExecuteOrderCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ShowDeleteConfirmCommand).RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(HasSelectedJobs));
            OnPropertyChanged(nameof(SelectedJobsCount));
            OnPropertyChanged(nameof(AreAllSelected));
        }
    }

    private void DeselectAll()
    {
        foreach (var job in BackupJobs)
            job.IsSelected = false;
    }

    private void ToggleSelectAll()
    {
        bool selectAll = !AreAllSelected;
        foreach (var job in BackupJobs)
            job.IsSelected = selectAll;
    }

    private void ShowProgressPopup(string jobName)
    {
        _progressViewModel.Reset();
        _progressViewModel.JobName = jobName;
        _progressViewModel.StartTracking();
        IsProgressPopupOpen = true;
    }

    private void HideProgressPopup()
    {
        IsProgressPopupOpen = false;
        _progressViewModel.Reset();
    }

    /// <summary>
    /// Called when settings are saved — reload blocked applications into BackupManager immediately.
    /// </summary>
    private void OnSettingsSaved(object? sender, EventArgs e)
    {
        var config = ConfigurationManager.GetInstance().LoadConfiguration();
        _backupManager.UpdateBlockedApplications(config.GetBlockedApplications());
    }

    private void ShowBlockedPopup(string message)
    {
        BlockedPopupMessage = message;
        IsBlockedPopupOpen = true;
    }

    private void CloseBlockedPopup()
    {
        IsBlockedPopupOpen = false;
    }

    /// <summary>
    /// Checks if an exception is caused by a blocked application and shows the popup if so.
    /// Returns true if the exception was a blocked-app error.
    /// </summary>
    private bool HandleBlockedAppException(Exception ex)
    {
        // The blocked app exception is an InvalidOperationException with "Backup blocked" in the message
        var blocked = ex as InvalidOperationException;
        if (blocked != null && blocked.Message.Contains("Backup blocked", StringComparison.OrdinalIgnoreCase))
        {
            ShowBlockedPopup(FormatBlockedMessage(blocked.Message));
            return true;
        }
        // Also check inner exception (when wrapped by ExecuteJob's catch)
        if (ex.InnerException is InvalidOperationException inner &&
            inner.Message.Contains("Backup blocked", StringComparison.OrdinalIgnoreCase))
        {
            ShowBlockedPopup(FormatBlockedMessage(inner.Message));
            return true;
        }
        return false;
    }

    /// <summary>
    /// Formats the raw blocked message into a user-friendly translated message.
    /// </summary>
    private string FormatBlockedMessage(string rawMessage)
    {
        // Extract app names from "Backup blocked because these applications are running: chrome, excel"
        var colonIndex = rawMessage.LastIndexOf(':');
        if (colonIndex >= 0 && colonIndex < rawMessage.Length - 1)
        {
            var apps = rawMessage[(colonIndex + 1)..].Trim();
            return $"{T("gui_blocked_popup_message")}\n\n{apps}";
        }
        return rawMessage;
    }

    private void CloseProgress()
    {
        ShowToast("Backup completed!");
        HideProgressPopup();
        NotifyStats();
    }

    private void NotifyStats()
    {
        OnPropertyChanged(nameof(TotalJobsCount));
        OnPropertyChanged(nameof(ActiveJobsCount));
        OnPropertyChanged(nameof(CompletedJobsCount));
        OnPropertyChanged(nameof(ErrorJobsCount));
        OnPropertyChanged(nameof(HasActiveOrErrorJobs));
    }

    public void ShowToast(string message)
    {
        ToastMessage = message;
        IsToastVisible = true;
        _toastTimer?.Stop();
        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _toastTimer.Tick += (s, e) =>
        {
            IsToastVisible = false;
            _toastTimer?.Stop();
        };
        _toastTimer.Start();
    }

    private void OnFileTransferred(object? sender, FileProgressEventArgs e)
    {
        // Update UI on the UI thread
        Dispatcher.UIThread.Post(() =>
        {
            _progressViewModel.JobName = e.JobName;
            _progressViewModel.CurrentFile = e.CurrentFile;
            _progressViewModel.TotalFiles = e.TotalFiles;
            _progressViewModel.FilesProcessed = e.FilesProcessed;
            _progressViewModel.ProgressPercentage = e.ProgressPercentage;
            _progressViewModel.TotalSize = e.TotalSize;
            _progressViewModel.ProcessedSize = e.TotalSize - e.RemainingSize;
        });
    }

    #endregion

    // ── Help screen translated labels ──
    private string T(string key)
    {
        try { return View.GUI.App.LocalizationService?.GetTextTranslated(key) ?? key; }
        catch { return key; }
    }

    public string HelpTitle => T("help_title");
    public string HelpSubtitle => T("help_subtitle");
    public string HelpGettingStarted => T("help_getting_started");
    public string HelpGettingStartedDesc => T("help_getting_started_desc");
    public string HelpStep1Title => T("help_step1_title");
    public string HelpStep1Desc => T("help_step1_desc");
    public string HelpStep2Title => T("help_step2_title");
    public string HelpStep2Desc => T("help_step2_desc");
    public string HelpStep3Title => T("help_step3_title");
    public string HelpStep3Desc => T("help_step3_desc");
    public string HelpBackupTypes => T("help_backup_types");
    public string HelpComplete => T("help_complete");
    public string HelpCompleteDesc => T("help_complete_desc");
    public string HelpDifferential => T("help_differential");
    public string HelpDifferentialDesc => T("help_differential_desc");
    public string HelpMultiSources => T("help_multi_sources");
    public string HelpMultiSourcesDesc => T("help_multi_sources_desc");
    public string HelpMultiSourcesTip => T("help_multi_sources_tip");
    public string HelpExecOrder => T("help_exec_order");
    public string HelpExecOrderDesc => T("help_exec_order_desc");
    public string HelpExecOrderTip => T("help_exec_order_tip");
    public string HelpSearchFilters => T("help_search_filters");
    public string HelpSearchFiltersDesc => T("help_search_filters_desc");
    public string HelpSearchFiltersTip => T("help_search_filters_tip");
    public string HelpSettings => T("help_settings");
    public string HelpSettingsDesc => T("help_settings_desc");
    public string HelpSettingsTheme => T("help_settings_theme");
    public string HelpSettingsLanguage => T("help_settings_language");
    public string HelpSettingsLogFormat => T("help_settings_log_format");
    public string HelpSettingsLogPath => T("help_settings_log_path");
    public string HelpSettingsStatePath => T("help_settings_state_path");
    public string HelpSettingsBlockedApps => T("help_settings_blocked_apps");
    public string HelpSettingsCryptosoftPath => T("help_settings_cryptosoft_path");
    public string HelpSettingsEncryptedExtensions => T("help_settings_encrypted_ext");
    public string HelpLogs => T("help_logs");
    public string HelpLogsDesc => T("help_logs_desc");
    public string HelpLogsTip => T("help_logs_tip");
    public string HelpTips => T("help_tips");
    public string HelpTipsDesc => T("help_tips_desc");
    public string HelpTip1Title => T("help_tip1_title");
    public string HelpTip1Desc => T("help_tip1_desc");
    public string HelpTip2Title => T("help_tip2_title");
    public string HelpTip2Desc => T("help_tip2_desc");
    public string HelpTip3Title => T("help_tip3_title");
    public string HelpTip3Desc => T("help_tip3_desc");
    public string HelpTip4Title => T("help_tip4_title");
    public string HelpTip4Desc => T("help_tip4_desc");

    public void RefreshHelpTranslations()
    {
        foreach (var prop in GetType().GetProperties()
            .Where(p => p.Name.StartsWith("Help")))
        {
            OnPropertyChanged(prop.Name);
        }
    }

    /// <summary>
    /// Parse a comma, semicolon, or newline-separated list of file extensions.
    /// </summary>
    private List<string> ParseEncryptedExtensions(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new List<string>();

        return input
            .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(ext => ext.Trim())
            .Where(ext => !string.IsNullOrWhiteSpace(ext))
            .Distinct()
            .ToList();
    }
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
