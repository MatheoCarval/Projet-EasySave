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
using EasySave.Services;
using EasySave.Services.Managers;
using EasyLog.Abstractions;
using EasyLog.Enums;
using EasyLog.Loggers;
using Avalonia.Threading;
using EasySave.Models;
using EasySave.View.GUI;

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
    private bool _isErrorToast;
    private bool _isSettingsOpen;
    private bool _isHelpOpen;
    private bool _isLogsOpen;
    private bool _isSchedulerOpen;
    private string _logsMessage = string.Empty;
    private string _toastMessage = string.Empty;
    private bool _isToastVisible;
    private DispatcherTimer? _toastTimer;
    private bool _isCompactView;

    // Onboarding tutorial
    private bool _isOnboardingActive;
    private int _onboardingStep; // 0 = welcome, 1-5 = sidebar steps
    private const int OnboardingTotalSteps = 6; // 0=welcome + 5 sidebar

    public ObservableCollection<BackupJobViewModel> ExecuteOrderJobs { get; } = new();
    public ObservableCollection<BackupLogEntry> LogEntries { get; } = new();
    public ObservableCollection<ScheduledTask> ScheduledTasks { get; } = new();
    public SettingsViewModel SettingsVM { get; }

    public string RemoteLogStatus
    {
        get => _remoteLogStatus;
        private set => SetProperty(ref _remoteLogStatus, value);
    }
    public bool IsRemoteLogSuccess => !string.IsNullOrEmpty(_remoteLogStatus) && !_isRemoteLogError;
    public bool IsRemoteLogError => !string.IsNullOrEmpty(_remoteLogStatus) && _isRemoteLogError;

    /// <summary>
    /// Callback to open a folder picker dialog. Set by the View (MainWindow) to decouple ViewModel from UI.
    /// Returns the selected folder path or null if cancelled.
    /// </summary>
    public Func<Task<string?>>? BrowseFolderCallback { get; set; } // kept for potential future use

    // Modal form fields
    private string _modalName = string.Empty;
    private string _modalTargetPath = string.Empty;
    private int _modalBackupTypeIndex = 0;
    private string _modalValidationError = string.Empty;
    private string _searchText = string.Empty;
    private bool _isFilterOpen;
    private readonly HashSet<string> _selectedTypes = new(StringComparer.OrdinalIgnoreCase) { "COMPLETE", "DIFFERENTIAL" };
    private readonly HashSet<string> _selectedStates = new(StringComparer.OrdinalIgnoreCase) { "ACTIVE", "PAUSED", "COMPLETED", "ERROR", "PENDING" };
    private int _currentPage = 1;
    private int _filteredCount;
    private int _pageSize = 5;
    private static readonly int[] PageSizeOptions = { 5, 10, 20, 30 };

    private static readonly string[] AllTypes = { "COMPLETE", "DIFFERENTIAL" };
    private static readonly string[] AllStates = { "ACTIVE", "PAUSED", "COMPLETED", "ERROR", "PENDING" };

    // Progress tracking
    private readonly ProgressViewModel _progressViewModel;

    // Remote log status (displayed in header when remote logging is active)
    private RemoteLogger? _activeRemoteLogger;
    private int _remoteSentCount;
    private string _remoteLogStatus = string.Empty;
    private bool _isRemoteLogError;

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
        _backupManager.JobAutoPaused += OnJobAutoPaused;
        _backupManager.JobAutoResumed += OnJobAutoResumed;

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
        BrowseSourcePathCommand = new RelayCommand<SourcePathViewModel>(BrowseSourcePath);
        BrowseTargetPathCommand = new RelayCommand(BrowseTargetPath);
        OpenEditModalForJobCommand = new RelayCommand<BackupJobViewModel>(OpenEditModalForJob);
        ExecuteEditingJobCommand = new RelayCommand(ExecuteEditingJob, () => _editingBackupJob != null);
        CloseProgressCommand = new RelayCommand(CloseProgress);
        DismissProgressCommand = new RelayCommand(DismissProgress);
        ReopenProgressCommand = new RelayCommand(ReopenProgress);
        ShowExecuteOrderCommand = new RelayCommand(ShowExecuteOrder, () => BackupJobs.Any(j => j.IsSelected));
        CancelExecuteOrderCommand = new RelayCommand(() => IsExecuteOrderOpen = false);
        ConfirmExecuteOrderCommand = new RelayCommand(ConfirmExecuteOrder);
        MoveJobUpCommand = new RelayCommand<BackupJobViewModel>(MoveJobUp);
        MoveJobDownCommand = new RelayCommand<BackupJobViewModel>(MoveJobDown);
        ShowDeleteConfirmCommand = new RelayCommand(ShowDeleteConfirm, () => BackupJobs.Any(j => j.IsSelected));
        CancelDeleteCommand = new RelayCommand(() => IsDeleteConfirmOpen = false);
        ConfirmDeleteCommand = new RelayCommand(ConfirmDelete);
        CloseBlockedPopupCommand = new RelayCommand(CloseBlockedPopup);
        FilterErrorJobsCommand = new RelayCommand(FilterErrorJobs);
        FilterAllJobsCommand = new RelayCommand(FilterAllJobs);
        FilterActiveJobsCommand = new RelayCommand(FilterActiveJobs);
        FilterPausedJobsCommand = new RelayCommand(FilterPausedJobs);
        FilterCompletedJobsCommand = new RelayCommand(FilterCompletedJobs);
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
        PreviousPageCommand = new RelayCommand(PreviousPage, () => CanGoToPreviousPage);
        NextPageCommand = new RelayCommand(NextPage, () => CanGoToNextPage);
        PlayJobCommand = new RelayCommand<BackupJobViewModel>(PlayJob);
        PauseJobCommand = new RelayCommand<string>(PauseJob);
        ResumeJobCommand = new RelayCommand<string>(ResumeJob);
        SetCardViewCommand = new RelayCommand(() => IsCompactView = false);
        SetCompactViewCommand = new RelayCommand(() => IsCompactView = true);
        SetPageSize5Command = new RelayCommand(() => PageSize = 5);
        SetPageSize10Command = new RelayCommand(() => PageSize = 10);
        SetPageSize20Command = new RelayCommand(() => PageSize = 20);
        SetPageSize30Command = new RelayCommand(() => PageSize = 30);
        OpenSchedulerCommand = new RelayCommand(OpenScheduler);
        AddScheduleCommand = new RelayCommand(AddSchedule, () => BackupJobs.Count > 0);
        DeleteScheduleCommand = new RelayCommand<ScheduledTask>(DeleteSchedule);
        ToggleScheduleCommand = new RelayCommand<ScheduledTask>(ToggleSchedule);

        // Onboarding commands
        NextOnboardingStepCommand = new RelayCommand(NextOnboardingStep);
        PrevOnboardingStepCommand = new RelayCommand(PrevOnboardingStep);
        SkipOnboardingCommand = new RelayCommand(SkipOnboarding);

        // Load real data from BackupManager
        LoadBackupJobs();
        LoadSchedules();

        // Check if onboarding should be shown
        CheckOnboarding();
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
                ApplyFilter(resetPage: true);
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

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                ApplyFilter(resetPage: false);
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));
                ((RelayCommand)PreviousPageCommand).RaiseCanExecuteChanged();
                ((RelayCommand)NextPageCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (SetProperty(ref _pageSize, value))
            {
                OnPropertyChanged(nameof(IsPageSize5));
                OnPropertyChanged(nameof(IsPageSize10));
                OnPropertyChanged(nameof(IsPageSize20));
                OnPropertyChanged(nameof(IsPageSize30));
                ApplyFilter(resetPage: true);
            }
        }
    }

    public bool IsPageSize5 => _pageSize == 5;
    public bool IsPageSize10 => _pageSize == 10;
    public bool IsPageSize20 => _pageSize == 20;
    public bool IsPageSize30 => _pageSize == 30;

    public ICommand SetPageSize5Command { get; }
    public ICommand SetPageSize10Command { get; }
    public ICommand SetPageSize20Command { get; }
    public ICommand SetPageSize30Command { get; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)_filteredCount / _pageSize));

    public bool CanGoToPreviousPage => CurrentPage > 1;

    public bool CanGoToNextPage => CurrentPage < TotalPages;

    public int PageItemCount
    {
        get
        {
            if (_filteredCount == 0)
                return 0;
            int start = (CurrentPage - 1) * _pageSize + 1;
            int end = Math.Min(CurrentPage * _pageSize, _filteredCount);
            return end - start + 1;
        }
    }

    public string TxtPerPage => T("gui_per_page");

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
        set
        {
            if (SetProperty(ref _isProgressPopupOpen, value))
                OnPropertyChanged(nameof(HasProgressData));
        }
    }

    /// <summary>
    /// True when popup is closed but there is progress data to show (running or completed)
    /// </summary>
    public bool HasProgressData => !IsProgressPopupOpen && _progressViewModel.Jobs.Count > 0;

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

    // Error toast color flag
    public bool IsErrorToast
    {
        get => _isErrorToast;
        set
        {
            if (SetProperty(ref _isErrorToast, value))
                OnPropertyChanged(nameof(ToastBackground));
        }
    }

    public string ToastBackground => _isErrorToast ? "#F44336" : "#4CAF50";

    public string TxtErrorReasonLabel => T("gui_error_popup_reason");
    public string TxtLastExecution => T("gui_last_execution");
    public string TxtProgress => T("gui_progress");
    public string TxtPlayJob => T("gui_play_job");
    public string TxtBackupTasks => T("gui_backup_tasks");
    public string TxtManageSubtitle => T("gui_manage_subtitle");
    public string TxtTotal => T("gui_total");
    public string TxtActive => T("gui_active");
    public string TxtDone => T("gui_done");
    public string TxtErrors => T("gui_errors");
    public string TxtSelectAll => T("gui_select_all");
    public string TxtSearchByName => T("gui_search_by_name");
    public string TxtNoTasksFound => T("gui_no_tasks_found");
    public string TxtPaginationPage => T("gui_pagination_page");
    public string TxtPaginationPageOf => T("gui_pagination_page_of");
    public string TxtPaginationPrevious => T("gui_pagination_previous");
    public string TxtPaginationNext => T("gui_pagination_next");

    // Sidebar tooltips
    public string TxtTooltipHome => T("gui_tooltip_home");
    public string TxtTooltipAddBackup => T("gui_tooltip_add_backup");
    public string TxtTooltipLogs => T("gui_tooltip_logs");
    public string TxtTooltipSettings => T("gui_tooltip_settings");
    public string TxtTooltipHelp => T("gui_tooltip_help");
    public string TxtTooltipScheduler => T("gui_tooltip_scheduler");

    // Scheduler text bindings
    public string TxtSchedulerTitle => T("gui_scheduler_title");
    public string TxtSchedulerEmpty => T("gui_scheduler_empty");
    public string TxtSchedulerEmptyDesc => T("gui_scheduler_empty_desc");
    public string TxtSchedulerAdd => T("gui_scheduler_add");
    public string TxtSchedulerJob => T("gui_scheduler_job");
    public string TxtSchedulerDate => T("gui_scheduler_date");
    public string TxtSchedulerTime => T("gui_scheduler_time");
    public string TxtSchedulerEnabled => T("gui_scheduler_enabled");
    public string TxtSchedulerDisabled => T("gui_scheduler_disabled");
    public string TxtSchedulerScheduledFor => T("gui_scheduler_scheduled_for");
    public string TxtSchedulerOverdue => T("gui_scheduler_overdue");
    public string TxtSchedulerNoJobs => T("gui_scheduler_no_jobs");

    // Onboarding text bindings
    public string TxtOnboardingSkip => T("onboarding_skip");
    public string TxtOnboardingPrev => T("onboarding_prev");

    // Filter panel
    public string TxtFilters => T("gui_filters");
    public string TxtClearAll => T("gui_clear_all");
    public string TxtFilterBackupType => T("gui_filter_backup_type");
    public string TxtFilterAll => T("gui_filter_all");
    public string TxtFilterComplete => T("gui_filter_complete");
    public string TxtFilterDifferential => T("gui_filter_differential");
    public string TxtFilterStatus => T("gui_filter_status");
    public string TxtFilterPending => T("gui_filter_pending");
    public string TxtFilterActive => T("gui_filter_active");
    public string TxtFilterCompleted => T("gui_filter_completed");
    public string TxtFilterPaused => T("gui_filter_paused");
    public string TxtFilterError => T("gui_filter_error");

    // Empty state
    public string TxtEmptyTitle => T("gui_empty_title");
    public string TxtEmptyDesc => T("gui_empty_desc");

    // Floating bar tooltips
    public string TxtTooltipDeselect => T("gui_tooltip_deselect");
    public string TxtTooltipExecute => T("gui_tooltip_execute");
    public string TxtTooltipDelete => T("gui_tooltip_delete");
    public string TxtTooltipBrowse => T("gui_tooltip_browse");
    public string TxtTooltipMoveUp => T("gui_tooltip_move_up");
    public string TxtTooltipMoveDown => T("gui_tooltip_move_down");

    // About section
    public string TxtAboutSoftware => T("gui_about_software");
    public string TxtAboutCompany => T("gui_about_company");
    public string TxtAboutLicense => T("gui_about_license");
    public string TxtAboutLicenseValue => T("gui_about_license_value");
    public string TxtAboutArchitecture => T("gui_about_architecture");

    // Modal
    public string TxtModalAddTitle => T("gui_modal_add_title");
    public string TxtModalEditTitle => T("gui_modal_edit_title");
    public string TxtModalBackupName => T("gui_modal_backup_name");
    public string TxtModalBackupNamePlaceholder => T("gui_modal_backup_name_placeholder");
    public string TxtModalSourcePaths => T("gui_modal_source_paths");
    public string TxtModalSourcePlaceholder => T("gui_modal_source_placeholder");
    public string TxtModalDestPath => T("gui_modal_dest_path");
    public string TxtModalDestPlaceholder => T("gui_modal_dest_placeholder");
    public string TxtModalBackupType => T("gui_modal_backup_type");
    public string TxtModalEncryptTitle => T("gui_modal_encrypt_title");
    public string TxtModalEncryptDesc => T("gui_modal_encrypt_desc");
    public string TxtBtnCancel => T("gui_btn_cancel");
    public string TxtBtnExecute => T("gui_btn_execute");
    public string TxtBtnSave => T("gui_btn_save");
    public string TxtBtnClose => T("gui_btn_close");
    public string TxtBtnDelete => T("gui_btn_delete");
    public string TxtBtnExecuteRun => T("gui_btn_execute_run");
    public string TxtComplete => T("gui_complete");
    public string TxtDifferential => T("gui_differential");

    // Progress popup
    public string TxtProgressTitle => T("gui_progress_title");
    public string TxtProgressComplete => T("gui_progress_complete");
    public string TxtProgressJobName => T("gui_progress_job_name");
    public string TxtProgressCurrentFile => T("gui_progress_current_file");
    public string TxtProgressProgress => T("gui_progress_progress");
    public string TxtProgressFiles => T("gui_progress_files");
    public string TxtProgressSize => T("gui_progress_size");
    public string TxtProgressElapsed => T("gui_progress_elapsed");
    public string TxtProgressSpeed => T("gui_progress_speed");
    public string TxtProgressRemaining => T("gui_progress_remaining");

    // Execute order popup
    public string TxtExecOrderTitle => T("gui_exec_order_title");
    public string TxtExecOrderDesc => T("gui_exec_order_desc");

    // Delete confirm popup
    public string TxtDeleteConfirmTitle => T("gui_delete_confirm_title");
    public string TxtDeleteConfirmWarning => T("gui_delete_confirm_warning");

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
                if (value)
                {
                    _isHelpOpen = false;
                    OnPropertyChanged(nameof(IsHelpOpen));
                    _isLogsOpen = false;
                    OnPropertyChanged(nameof(IsLogsOpen));
                    _isSchedulerOpen = false;
                    OnPropertyChanged(nameof(IsSchedulerOpen));
                }
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
                if (value)
                {
                    _isSettingsOpen = false;
                    OnPropertyChanged(nameof(IsSettingsOpen));
                    _isLogsOpen = false;
                    OnPropertyChanged(nameof(IsLogsOpen));
                    _isSchedulerOpen = false;
                    OnPropertyChanged(nameof(IsSchedulerOpen));
                }
            }
        }
    }

    public bool IsLogsOpen
    {
        get => _isLogsOpen;
        set
        {
            if (SetProperty(ref _isLogsOpen, value))
            {
                OnPropertyChanged(nameof(IsHomeActive));
                if (value)
                {
                    _isSettingsOpen = false;
                    OnPropertyChanged(nameof(IsSettingsOpen));
                    _isHelpOpen = false;
                    OnPropertyChanged(nameof(IsHelpOpen));
                    _isSchedulerOpen = false;
                    OnPropertyChanged(nameof(IsSchedulerOpen));
                }
            }
        }
    }

    public bool IsSchedulerOpen
    {
        get => _isSchedulerOpen;
        set
        {
            if (SetProperty(ref _isSchedulerOpen, value))
            {
                OnPropertyChanged(nameof(IsHomeActive));
                if (value)
                {
                    _isSettingsOpen = false;
                    OnPropertyChanged(nameof(IsSettingsOpen));
                    _isHelpOpen = false;
                    OnPropertyChanged(nameof(IsHelpOpen));
                    _isLogsOpen = false;
                    OnPropertyChanged(nameof(IsLogsOpen));
                }
            }
        }
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

    public bool IsHomeActive => !IsSettingsOpen && !IsHelpOpen && !IsLogsOpen && !IsSchedulerOpen;

    // Dashboard stats
    public int TotalJobsCount => BackupJobs.Count;
    public int ActiveJobsCount => BackupJobs.Count(j => j.BackupState == BackupState.ACTIVE);
    public int PausedJobsCount => BackupJobs.Count(j => j.BackupState == BackupState.PAUSED);
    public int CompletedJobsCount => BackupJobs.Count(j => j.BackupState == BackupState.COMPLETED);
    public int ErrorJobsCount => BackupJobs.Count(j => j.BackupState == BackupState.ERROR);
    public bool HasActiveOrErrorJobs => BackupJobs.Any(j => j.BackupState == BackupState.ACTIVE || j.BackupState == BackupState.ERROR);
    public string TxtPaused => T("gui_filter_paused");

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

    // ── Onboarding properties ──
    public bool IsOnboardingActive
    {
        get => _isOnboardingActive;
        set
        {
            if (SetProperty(ref _isOnboardingActive, value))
            {
                OnPropertyChanged(nameof(OnboardingTitle));
                OnPropertyChanged(nameof(OnboardingDescription));
                OnPropertyChanged(nameof(OnboardingStepLabel));
                OnPropertyChanged(nameof(IsOnboardingWelcome));
                OnPropertyChanged(nameof(ShowOnboardingPrev));
                OnPropertyChanged(nameof(OnboardingNextLabel));
            }
        }
    }

    public int OnboardingStep
    {
        get => _onboardingStep;
        set
        {
            if (SetProperty(ref _onboardingStep, value))
            {
                OnPropertyChanged(nameof(OnboardingTitle));
                OnPropertyChanged(nameof(OnboardingDescription));
                OnPropertyChanged(nameof(OnboardingStepLabel));
                OnPropertyChanged(nameof(IsOnboardingWelcome));
                OnPropertyChanged(nameof(ShowOnboardingPrev));
                OnPropertyChanged(nameof(OnboardingNextLabel));
                OnPropertyChanged(nameof(OnboardingDot0));
                OnPropertyChanged(nameof(OnboardingDot1));
                OnPropertyChanged(nameof(OnboardingDot2));
                OnPropertyChanged(nameof(OnboardingDot3));
                OnPropertyChanged(nameof(OnboardingDot4));
                OnPropertyChanged(nameof(OnboardingDot5));
            }
        }
    }

    public string OnboardingTitle => _onboardingStep == 0
        ? T("onboarding_welcome_title")
        : T($"onboarding_step{_onboardingStep}_title");

    public string OnboardingDescription => _onboardingStep == 0
        ? T("onboarding_welcome_desc")
        : T($"onboarding_step{_onboardingStep}_desc");

    public string OnboardingStepLabel =>
        $"{_onboardingStep + 1} {T("onboarding_step_of")} {OnboardingTotalSteps}";

    public bool IsOnboardingWelcome => _onboardingStep == 0;
    public bool ShowOnboardingPrev => _onboardingStep > 0;

    public string OnboardingNextLabel =>
        _onboardingStep >= OnboardingTotalSteps - 1 ? T("onboarding_done") : T("onboarding_next");

    // Step dots (active/inactive)
    public bool OnboardingDot0 => _onboardingStep == 0;
    public bool OnboardingDot1 => _onboardingStep == 1;
    public bool OnboardingDot2 => _onboardingStep == 2;
    public bool OnboardingDot3 => _onboardingStep == 3;
    public bool OnboardingDot4 => _onboardingStep == 4;
    public bool OnboardingDot5 => _onboardingStep == 5;

    /// <summary>
    /// Event raised when onboarding step changes, so code-behind can reposition spotlight
    /// </summary>
    public event Action<int>? OnboardingStepChanged;

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
    public ICommand BrowseSourcePathCommand { get; }
    public ICommand BrowseTargetPathCommand { get; }
    public ICommand OpenEditModalForJobCommand { get; }
    public ICommand ExecuteEditingJobCommand { get; }
    public ICommand CloseProgressCommand { get; }
    public ICommand DismissProgressCommand { get; }
    public ICommand ReopenProgressCommand { get; }
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
    public ICommand FilterErrorJobsCommand { get; }
    public ICommand FilterAllJobsCommand { get; }
    public ICommand FilterActiveJobsCommand { get; }
    public ICommand FilterPausedJobsCommand { get; }
    public ICommand FilterCompletedJobsCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PlayJobCommand { get; }
    public ICommand PauseJobCommand { get; }
    public ICommand ResumeJobCommand { get; }
    public ICommand NextOnboardingStepCommand { get; }
    public ICommand PrevOnboardingStepCommand { get; }
    public ICommand SkipOnboardingCommand { get; }
    public ICommand SetCardViewCommand { get; }
    public ICommand SetCompactViewCommand { get; }
    public ICommand OpenSchedulerCommand { get; }
    public ICommand AddScheduleCommand { get; }
    public ICommand DeleteScheduleCommand { get; }
    public ICommand ToggleScheduleCommand { get; }

    /// <summary>
    /// List of job names for the scheduler ComboBox
    /// </summary>
    public List<string> SchedulerJobNames => BackupJobs.Select(j => j.Name).ToList();

    public bool IsCompactView
    {
        get => _isCompactView;
        set
        {
            if (SetProperty(ref _isCompactView, value))
            {
                OnPropertyChanged(nameof(ShowPagination));
                ApplyFilter(resetPage: true);
            }
        }
    }

    /// <summary>
    /// Pagination is only visible in card (detailed) view when there are jobs.
    /// </summary>
    public bool ShowPagination => !IsCompactView && !HasNoJobs;

    public string TxtCompactView => T("gui_compact_view");
    public string TxtCardView => T("gui_card_view");
    public string TxtDropHint => T("gui_drop_hint");

    #endregion

    #region Methods

    private void OpenAddModal()
    {
        ModalTitle = TxtModalAddTitle;
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

        ModalTitle = TxtModalEditTitle;
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
            ShowToast(T("gui_toast_saved"));
        }
        catch (ArgumentException)
        {
            // Duplicate name — just keep the modal open so the user can fix it
            ModalValidationError = T("gui_error_duplicate_name");
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
            ? $"{T("gui_delete_confirm_single")} \"{selected[0].Name}\" ?"
            : $"{T("gui_delete_confirm_multi")} {selected.Count} {T("gui_delete_confirm_jobs")} ?";
        IsDeleteConfirmOpen = true;
    }

    private void ConfirmDelete()
    {
        IsDeleteConfirmOpen = false;
        DeleteSelected();
        ShowToast(T("gui_toast_deleted"));
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
        if (orderedJobs.Count == 0) return;

        AddJobsToProgress(orderedJobs);

        var tasks = orderedJobs.Select(job => Task.Run(() =>
        {
            var jobId = job.Id;
            try
            {
                _backupManager.ExecuteJob(jobId);
                Dispatcher.UIThread.Post(() =>
                {
                    _progressViewModel.MarkJobCompleted(jobId, job.EncryptedFilesCount);
                    job.RefreshDisplay();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _progressViewModel.MarkJobError(jobId, GetErrorReason(ex));
                    job.RefreshDisplay();
                    HandleBlockedAppException(ex);
                });
            }
        })).ToArray();

        await Task.WhenAll(tasks);
        ReloadBackupJobs();
        OnPropertyChanged(nameof(HasProgressData));
    }

    private void ViewLogs()
    {
        _isSettingsOpen = false;
        _isHelpOpen = false;
        _isLogsOpen = true;
        NotifyNavigationChanged();
    }

    private void OpenSettings()
    {
        SettingsVM.LoadSettings();
        IsSettingsOpen = true;
        _isSettingsOpen = true;
        _isHelpOpen = false;
        _isLogsOpen = false;
        NotifyNavigationChanged();
    }

    private void OpenHelp()
    {
        IsHelpOpen = true;
        _isSettingsOpen = false;
        _isHelpOpen = true;
        _isLogsOpen = false;
        NotifyNavigationChanged();
        RefreshHelpTranslations();
    }

    private void GoHome()
    {
        _isSettingsOpen = false;
        _isHelpOpen = false;
        _isLogsOpen = false;
        _isSchedulerOpen = false;
        OnPropertyChanged(nameof(IsSettingsOpen));
        OnPropertyChanged(nameof(IsHelpOpen));
        OnPropertyChanged(nameof(IsLogsOpen));
        OnPropertyChanged(nameof(IsSchedulerOpen));
        OnPropertyChanged(nameof(IsHomeActive));
    }

    #region Scheduler

    private void OpenScheduler()
    {
        IsSchedulerOpen = true;
    }

    private void AddSchedule()
    {
        if (BackupJobs.Count == 0) return;
        var first = BackupJobs[0];
        // Default: tomorrow at 09:00
        var tomorrow = DateTime.Now.Date.AddDays(1).AddHours(9);
        var task = new ScheduledTask
        {
            BackupJobId = first.Id,
            BackupJobName = first.Name,
            IsEnabled = true,
            ScheduledDateTime = tomorrow
        };
        task.PropertyChanged += OnScheduledTaskChanged;
        ScheduledTasks.Add(task);
        OnPropertyChanged(nameof(HasScheduledTasks));
        OnPropertyChanged(nameof(ScheduleStats));
        SaveSchedules();
    }

    private void DeleteSchedule(ScheduledTask? task)
    {
        if (task == null) return;
        task.PropertyChanged -= OnScheduledTaskChanged;
        ScheduledTasks.Remove(task);
        OnPropertyChanged(nameof(HasScheduledTasks));
        OnPropertyChanged(nameof(ScheduleStats));
        SaveSchedules();
    }

    private void ToggleSchedule(ScheduledTask? task)
    {
        if (task == null) return;
        task.IsEnabled = !task.IsEnabled;
        OnPropertyChanged(nameof(ScheduleStats));
        NotifyNavigationChanged();
    }

    /// <summary>
    /// Notifies all navigation-related properties at once to avoid
    /// inconsistent intermediate states during page transitions.
    /// </summary>
    private void NotifyNavigationChanged()
    {
        OnPropertyChanged(nameof(IsSettingsOpen));
        OnPropertyChanged(nameof(IsHelpOpen));
        OnPropertyChanged(nameof(IsLogsOpen));
        OnPropertyChanged(nameof(IsHomeActive));
    }

    private void OnScheduledTaskChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Sync BackupJobId when job name changes via ComboBox
        if (e.PropertyName == nameof(ScheduledTask.BackupJobName) && sender is ScheduledTask task)
        {
            var job = BackupJobs.FirstOrDefault(j => j.Name == task.BackupJobName);
            if (job != null && task.BackupJobId != job.Id)
                task.BackupJobId = job.Id;
        }
        SaveSchedules();
    }

    public bool HasScheduledTasks => ScheduledTasks.Count > 0;

    public string ScheduleStats
    {
        get
        {
            var total = ScheduledTasks.Count;
            var active = ScheduledTasks.Count(t => t.IsEnabled);
            return $"{active}/{total}";
        }
    }

    private void SaveSchedules()
    {
        try
        {
            var path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "schedules.json");
            var json = System.Text.Json.JsonSerializer.Serialize(ScheduledTasks.ToList(),
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(path, json);
        }
        catch { }
    }

    private void LoadSchedules()
    {
        try
        {
            var path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "schedules.json");
            if (!System.IO.File.Exists(path)) return;
            var json = System.IO.File.ReadAllText(path);
            var tasks = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<ScheduledTask>>(json);
            if (tasks != null)
            {
                foreach (var t in tasks)
                {
                    t.PropertyChanged += OnScheduledTaskChanged;
                    ScheduledTasks.Add(t);
                }
            }
            OnPropertyChanged(nameof(HasScheduledTasks));
            OnPropertyChanged(nameof(ScheduleStats));
        }
        catch { }
    }

    #endregion

    #region Onboarding

    private void CheckOnboarding()
    {
        try
        {
            var config = ConfigurationManager.GetInstance().LoadConfiguration();
            if (!config.GetOnboardingCompleted())
            {
                // If user already has backup jobs, they're not new — skip onboarding silently
                if (BackupJobs.Count > 0)
                {
                    config.SetOnboardingCompleted(true);
                    ConfigurationManager.GetInstance().SaveConfiguration(config);
                    return;
                }

                _onboardingStep = 0;
                IsOnboardingActive = true;
            }
        }
        catch
        {
            // If config fails, don't block the app
        }
    }

    private void NextOnboardingStep()
    {
        if (_onboardingStep >= OnboardingTotalSteps - 1)
        {
            CompleteOnboarding();
            return;
        }

        OnboardingStep = _onboardingStep + 1;
        OnboardingStepChanged?.Invoke(_onboardingStep);
    }

    private void PrevOnboardingStep()
    {
        if (_onboardingStep > 0)
        {
            OnboardingStep = _onboardingStep - 1;
            OnboardingStepChanged?.Invoke(_onboardingStep);
        }
    }

    private void SkipOnboarding()
    {
        CompleteOnboarding();
    }

    private void CompleteOnboarding()
    {
        IsOnboardingActive = false;

        try
        {
            var config = ConfigurationManager.GetInstance().LoadConfiguration();
            config.SetOnboardingCompleted(true);
            ConfigurationManager.GetInstance().SaveConfiguration(config);
        }
        catch
        {
            // Non-critical — don't crash if save fails
        }
    }

    #endregion

    private async void ExecuteBackup()
    {
        if (SelectedBackupJob == null) return;
        LaunchSingleJob(SelectedBackupJob);
    }

    private void PlayJob(BackupJobViewModel? job)
    {
        if (job == null) return;
        LaunchSingleJob(job);
    }

    /// <summary>
    /// Launches a single job using the same multi-job pattern so it can coexist with other running jobs.
    /// </summary>
    private void LaunchSingleJob(BackupJobViewModel jobVm)
    {
        var jobId = jobVm.Id;
        AddJobsToProgress(new[] { jobVm });

        _ = Task.Run(() =>
        {
            try
            {
                _backupManager.ExecuteJob(jobId);
                Dispatcher.UIThread.Post(() =>
                {
                    _progressViewModel.MarkJobCompleted(jobId, jobVm.EncryptedFilesCount);
                    jobVm.RefreshDisplay();
                    ReloadBackupJobs();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _progressViewModel.MarkJobError(jobId, GetErrorReason(ex));
                    jobVm.RefreshDisplay();
                    ReloadBackupJobs();
                    HandleBlockedAppException(ex);
                });
            }
        });
    }

    /// <summary>
    /// Manually pauses a running job (called from pause button in popup).
    /// </summary>
    private void PauseJob(string? jobId)
    {
        if (jobId == null) return;
        _backupManager.PauseJob(jobId);
        Dispatcher.UIThread.Post(() =>
        {
            _progressViewModel.MarkJobPaused(jobId);
            var card = BackupJobs.FirstOrDefault(j => j.Id == jobId);
            card?.RefreshDisplay();
        });
    }

    /// <summary>
    /// Resumes a manually or auto-paused job.
    /// </summary>
    private void ResumeJob(string? jobId)
    {
        if (jobId == null) return;
        _backupManager.ResumeJob(jobId);
        Dispatcher.UIThread.Post(() =>
        {
            _progressViewModel.MarkJobResumed(jobId);
            var card = BackupJobs.FirstOrDefault(j => j.Id == jobId);
            card?.RefreshDisplay();
        });
    }

    /// <summary>
    /// Called when BackupManager auto-pauses all running jobs because a blocked application started.
    /// </summary>
    private void OnJobAutoPaused(object? sender, JobPauseEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var id in e.JobIds)
            {
                _progressViewModel.MarkJobPaused(id);
                var card = BackupJobs.FirstOrDefault(j => j.Id == id);
                card?.RefreshDisplay();
            }
            NotifyStats();
            ShowToast($"{T("gui_toast_paused_blocked")} ({string.Join(", ", e.BlockedApps)})");
        });
    }

    /// <summary>
    /// Called when BackupManager auto-resumes all paused jobs because the blocked application stopped.
    /// </summary>
    private void OnJobAutoResumed(object? sender, List<string> resumedIds)
    {
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var id in resumedIds)
            {
                _progressViewModel.MarkJobResumed(id);
                var card = BackupJobs.FirstOrDefault(j => j.Id == id);
                card?.RefreshDisplay();
            }
            NotifyStats();
            ShowToast(T("gui_toast_resumed"));
        });
    }

    private async void ExecuteSelected()
    {
        var selectedJobs = BackupJobs.Where(j => j.IsSelected).ToList();
        if (selectedJobs.Count == 0) return;

        AddJobsToProgress(selectedJobs);

        var tasks = selectedJobs.Select(job => Task.Run(() =>
        {
            var jobId = job.Id;
            try
            {
                _backupManager.ExecuteJob(jobId);
                Dispatcher.UIThread.Post(() =>
                {
                    _progressViewModel.MarkJobCompleted(jobId, job.EncryptedFilesCount);
                    job.RefreshDisplay();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _progressViewModel.MarkJobError(jobId, GetErrorReason(ex));
                    job.RefreshDisplay();
                    HandleBlockedAppException(ex);
                });
            }
        })).ToArray();

        await Task.WhenAll(tasks);
        ReloadBackupJobs();
        OnPropertyChanged(nameof(HasProgressData));
    }

    private void ExecuteEditingJob()
    {
        if (_editingBackupJob == null) return;
        LaunchSingleJob(_editingBackupJob);
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

    private async void BrowseSourcePath(SourcePathViewModel? sourcePathVm)
    {
        // Now handled in code-behind via BrowseSourcePath_Click
    }

    private async void BrowseTargetPath()
    {
        // Now handled in code-behind via BrowseTargetPath_Click
    }

    private void LoadBackupJobs()
    {
        // Load all backup jobs from BackupManager, sorted by most recently executed first
        var jobs = _backupManager.GetAllJobs()
            .OrderByDescending(j => j.LastExecution == DateTime.MinValue ? 0 : 1)
            .ThenByDescending(j => j.LastExecution);

        foreach (var job in jobs)
        {
            var viewModel = new BackupJobViewModel(job);
            viewModel.PropertyChanged += OnJobSelectionChanged;
            BackupJobs.Add(viewModel);
        }

        OnPropertyChanged(nameof(HasNoJobs));
        ApplyFilter(resetPage: true);
    }

    private void ReloadBackupJobs()
    {
        // Clear existing jobs
        BackupJobs.Clear();

        // Reload all jobs from BackupManager, sorted by most recently executed first
        var jobs = _backupManager.GetAllJobs()
            .OrderByDescending(j => j.LastExecution == DateTime.MinValue ? 0 : 1)
            .ThenByDescending(j => j.LastExecution);

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
        ApplyFilter(resetPage: true);
    }

    private void ApplyFilter(bool resetPage)
    {
        FilteredBackupJobs.Clear();
        var query = _searchText?.Trim() ?? string.Empty;

        var filteredList = GetFilteredJobs(query).ToList();
        _filteredCount = filteredList.Count;

        if (resetPage)
        {
            _currentPage = 1;
            OnPropertyChanged(nameof(CurrentPage));
        }

        // Apply pagination only in card (non-compact) view
        IEnumerable<BackupJobViewModel> paginatedFiltered;
        if (!_isCompactView)
        {
            paginatedFiltered = filteredList
                .Skip((CurrentPage - 1) * _pageSize)
                .Take(_pageSize);
        }
        else
        {
            paginatedFiltered = filteredList;
        }

        foreach (var job in paginatedFiltered)
            FilteredBackupJobs.Add(job);

        OnPropertyChanged(nameof(HasNoFilteredJobs));
        OnPropertyChanged(nameof(PageItemCount));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(ShowPagination));
        OnPropertyChanged(nameof(CanGoToNextPage));
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        ((RelayCommand)PreviousPageCommand).RaiseCanExecuteChanged();
        ((RelayCommand)NextPageCommand).RaiseCanExecuteChanged();
    }

    private IEnumerable<BackupJobViewModel> GetFilteredJobs(string query)
    {
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

        return filtered;
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
        ApplyFilter(resetPage: true);
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
        ApplyFilter(resetPage: true);
    }

    private void ToggleAllTypes()
    {
        if (_selectedTypes.Count == AllTypes.Length)
            _selectedTypes.Clear();
        else
            foreach (var t in AllTypes) _selectedTypes.Add(t);
        NotifyFilterTypeChanged();
        ApplyFilter(resetPage: true);
    }

    private void ToggleAllStates()
    {
        if (_selectedStates.Count == AllStates.Length)
            _selectedStates.Clear();
        else
            foreach (var s in AllStates) _selectedStates.Add(s);
        NotifyFilterStateChanged();
        ApplyFilter(resetPage: true);
    }

    private void ClearFilters()
    {
        foreach (var t in AllTypes) _selectedTypes.Add(t);
        foreach (var s in AllStates) _selectedStates.Add(s);
        NotifyFilterTypeChanged();
        NotifyFilterStateChanged();
        ApplyFilter(resetPage: true);
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

    /// <summary>
    /// Adds one or more jobs to the progress popup. If completed jobs from a previous run exist, clears them first.
    /// Never resets while jobs are still running.
    /// </summary>
    private void AddJobsToProgress(IList<BackupJobViewModel> jobs)
    {
        // Clear finished data from previous run, but keep running jobs
        if (_progressViewModel.Jobs.Count > 0 && _progressViewModel.AllCompleted)
        {
            _progressViewModel.Reset();
        }

        foreach (var job in jobs)
        {
            // Don't add duplicates (same job launched twice)
            if (_progressViewModel.Jobs.All(j => j.JobId != job.Id))
                _progressViewModel.AddJob(job.Id, job.Name);
        }

        _progressViewModel.IsCompleted = false;
        IsProgressPopupOpen = true;
    }

    /// <summary>
    /// Dismisses the popup without stopping backups - they continue in background
    /// </summary>
    private void DismissProgress()
    {
        IsProgressPopupOpen = false;
        OnPropertyChanged(nameof(HasProgressData));
    }

    /// <summary>
    /// Reopens the progress popup to see ongoing backup progress
    /// </summary>
    private void ReopenProgress()
    {
        IsProgressPopupOpen = true;
        OnPropertyChanged(nameof(HasProgressData));
    }

    /// <summary>
    /// Called when settings are saved — reload blocked applications and logger format into BackupManager immediately.
    /// </summary>
    /// <summary>
    /// Subscribes to the given RemoteLogger's StatusCallback so the UI shows live send status.
    /// Pass null to detach (e.g. when switching back to local-only mode).
    /// </summary>
    public void BindRemoteLogger(RemoteLogger? logger)
    {
        if (_activeRemoteLogger != null)
            _activeRemoteLogger.StatusCallback = null;

        _activeRemoteLogger = logger;
        _remoteSentCount = 0;
        _isRemoteLogError = false;
        RemoteLogStatus = string.Empty;
        OnPropertyChanged(nameof(IsRemoteLogSuccess));
        OnPropertyChanged(nameof(IsRemoteLogError));

        if (logger == null) return;

        logger.StatusCallback = (success, error) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _isRemoteLogError = !success;
                if (success)
                {
                    _remoteSentCount++;
                    RemoteLogStatus = $"📡 {_remoteSentCount} log(s) envoyé(s)  ·  {DateTime.Now:HH:mm:ss}";
                }
                else
                {
                    var shortErr = error?.Split('\n')[0] ?? "connexion";
                    RemoteLogStatus = $"📡 Erreur : {shortErr}";
                }
                OnPropertyChanged(nameof(IsRemoteLogSuccess));
                OnPropertyChanged(nameof(IsRemoteLogError));
            });
        };
    }

    private void OnSettingsSaved(object? sender, EventArgs e)
    {
        var config = ConfigurationManager.GetInstance().LoadConfiguration();
        _backupManager.UpdateBlockedApplications(config.GetBlockedApplications());
        _backupManager.UpdatePriorityExtensions(config.GetPriorityExtensions());
        _backupManager.UpdateMaxParallelSize(config.GetMaxParallelTransferSizeValue(), config.GetMaxParallelTransferSizeUnit());
        _backupManager.UpdateCryptageManager(config.GetCryptosoftPath(), config.GetCryptosoftPublicKey(), config.GetEncryptedExtensions());

        // Rebuild logger (format or remote settings may have changed)
        var logPath = config.GetLogFilePath();
        if (string.IsNullOrWhiteSpace(logPath))
            logPath = config.GetDefaultLogPath();

        ILogger localLogger = config.GetLogFormat() == EasyLog.Enums.LogFormat.XML
            ? new DailyXmlLogger(logPath)
            : new DailyJsonLogger(logPath);

        var mode = config.GetLogStorageMode();
        ILogger effectiveLogger = localLogger;
        if (mode != LogStorageMode.Local)
        {
            var remoteUrl = config.GetRemoteLoggingUrl();
            var remoteKey = config.GetRemoteLoggingApiKey();
            if (!string.IsNullOrWhiteSpace(remoteUrl) && !string.IsNullOrWhiteSpace(remoteKey))
                effectiveLogger = new RemoteLogger(localLogger, remoteUrl, remoteKey, mode == LogStorageMode.Both);
        }

        var newRemoteLogger = effectiveLogger as RemoteLogger;
        App.RemoteLogger = newRemoteLogger;
        BindRemoteLogger(newRemoteLogger);

        _backupManager.UpdateLogger(effectiveLogger);

        // Refresh all translated labels on the main page
        RefreshHelpTranslations();
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

    private void ShowErrorPopup(string jobName, Exception ex)
    {
        var reason = GetErrorReason(ex);
        NotificationService.NotifyBackupFailed(jobName, reason);
        ShowErrorToast($"{jobName} — {reason}");
    }

    private void ShowErrorToast(string message)
    {
        IsErrorToast = true;
        ToastMessage = message;
        IsToastVisible = true;
        _toastTimer?.Stop();
        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        _toastTimer.Tick += (s, e) =>
        {
            IsToastVisible = false;
            _toastTimer?.Stop();
        };
        _toastTimer.Start();
    }

    private void FilterErrorJobs()
    {
        // Set filter to show only ERROR state
        _selectedStates.Clear();
        _selectedStates.Add("ERROR");
        NotifyFilterStateChanged();
        ApplyFilter(resetPage: true);
    }

    private void FilterAllJobs()
    {
        // Reset filter to show all states
        _selectedStates.Clear();
        foreach (var s in AllStates) _selectedStates.Add(s);
        NotifyFilterStateChanged();
        ApplyFilter(resetPage: true);
    }

    private void FilterActiveJobs()
    {
        _selectedStates.Clear();
        _selectedStates.Add("ACTIVE");
        NotifyFilterStateChanged();
        ApplyFilter(resetPage: true);
    }

    private void FilterPausedJobs()
    {
        _selectedStates.Clear();
        _selectedStates.Add("PAUSED");
        NotifyFilterStateChanged();
        ApplyFilter(resetPage: true);
    }

    private void FilterCompletedJobs()
    {
        _selectedStates.Clear();
        _selectedStates.Add("COMPLETED");
        NotifyFilterStateChanged();
        ApplyFilter(resetPage: true);
    }

    private void PreviousPage()
    {
        if (CanGoToPreviousPage)
        {
            CurrentPage--;
        }
    }

    private void NextPage()
    {
        if (CanGoToNextPage)
        {
            CurrentPage++;
        }
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
    /// Extracts a user-friendly error reason from an execution exception.
    /// </summary>
    private string GetErrorReason(Exception ex)
    {
        // Walk the full exception chain to find the root cause
        Exception? current = ex;
        while (current != null)
        {
            if (current is DirectoryNotFoundException)
                return T("gui_error_path_not_found");
            if (current is UnauthorizedAccessException)
                return T("gui_error_access_denied");
            if (current is IOException)
                return T("gui_error_io");
            current = current.InnerException;
        }
        return T("gui_execution_error");
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
        // Windows system notification
        var completedName = _progressViewModel.Jobs.FirstOrDefault()?.JobName ?? string.Empty;
        NotificationService.NotifyBackupCompleted(completedName);
        ShowToast(T("gui_toast_completed"));
        DismissProgress(); // just hide, keep data so user can reopen
        NotifyStats();
    }

    private void NotifyStats()
    {
        OnPropertyChanged(nameof(TotalJobsCount));
        OnPropertyChanged(nameof(ActiveJobsCount));
        OnPropertyChanged(nameof(PausedJobsCount));
        OnPropertyChanged(nameof(CompletedJobsCount));
        OnPropertyChanged(nameof(ErrorJobsCount));
        OnPropertyChanged(nameof(HasActiveOrErrorJobs));
    }

    public void ShowToast(string message)
    {
        IsErrorToast = false;
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
            // Route progress to the correct job item
            _progressViewModel.UpdateJobProgress(
                e.JobId,
                e.CurrentFile,
                e.TotalFiles,
                e.FilesProcessed,
                e.ProgressPercentage,
                e.TotalSize,
                e.TotalSize - e.RemainingSize);

            // Also update the card on the home page in real-time
            var card = BackupJobs.FirstOrDefault(j => j.Id == e.JobId);
            card?.RefreshDisplay();
        });
    }

    #endregion

    // ── Help screen translated labels ──
    public string T(string key)
    {
        try { return App.LocalizationService?.GetTextTranslated(key) ?? key; }
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

    // New sections
    public string HelpSectionFeatures => T("help_section_features");
    public string HelpDashboard => T("help_dashboard");
    public string HelpDashboardDesc => T("help_dashboard_desc");
    public string HelpDashboardTip => T("help_dashboard_tip");
    public string HelpEncryption => T("help_encryption");
    public string HelpEncryptionDesc => T("help_encryption_desc");
    public string HelpErrorHandling => T("help_error_handling");
    public string HelpErrorHandlingDesc => T("help_error_handling_desc");
    public string HelpScheduler => T("help_scheduler");
    public string HelpSchedulerDesc => T("help_scheduler_desc");
    public string HelpSchedulerTip => T("help_scheduler_tip");
    public string HelpShortcuts => T("help_shortcuts");
    public string HelpShortcutsDesc => T("help_shortcuts_desc");
    public string HelpDragDrop => T("help_drag_drop");
    public string HelpDragDropDesc => T("help_drag_drop_desc");
    public string HelpAccentColors => T("help_accent_colors");
    public string HelpAccentColorsDesc => T("help_accent_colors_desc");
    public string HelpCompactView => T("help_compact_view");
    public string HelpCompactViewDesc => T("help_compact_view_desc");
    public string HelpFaq => T("help_faq");
    public string HelpFaq1Q => T("help_faq1_q");
    public string HelpFaq1A => T("help_faq1_a");
    public string HelpFaq2Q => T("help_faq2_q");
    public string HelpFaq2A => T("help_faq2_a");
    public string HelpFaq3Q => T("help_faq3_q");
    public string HelpFaq3A => T("help_faq3_a");
    public string HelpFaq4Q => T("help_faq4_q");
    public string HelpFaq4A => T("help_faq4_a");
    public string HelpFaq5Q => T("help_faq5_q");
    public string HelpFaq5A => T("help_faq5_a");
    public string HelpFaq6Q => T("help_faq6_q");
    public string HelpFaq6A => T("help_faq6_a");
    public string HelpNeedHelp => T("help_need_help");
    public string HelpNeedHelpDesc => T("help_need_help_desc");
    public string HelpCopyEmail => T("help_copy_email");
    public string HelpSendEmail => T("help_send_email");

    public void RefreshHelpTranslations()
    {
        foreach (var prop in GetType().GetProperties()
            .Where(p => p.Name.StartsWith("Help") || p.Name.StartsWith("Txt")))
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
