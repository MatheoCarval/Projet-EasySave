using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.VisualTree;
using Avalonia.Animation;
using Avalonia.Threading;
using EasySave.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.Globalization;

namespace EasySave.View.GUI;

/// <summary>
/// Main window of the EasySave application
/// </summary>
public partial class MainWindow : Window
{
    // Drag reorder state
    private Border? _dragItem;
    private BackupJobViewModel? _dragJob;
    private int _dragStartIndex;
    private int _dragTargetIndex;
    private double _dragStartY;
    private bool _isDragging;
    private Transitions? _savedTransitions;

    // Auto-scroll
    private DispatcherTimer? _autoScrollTimer;
    private double _autoScrollSpeed;

    /// <summary>
    /// Initializes a new instance of the MainWindow class
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(App.BackupManager!);

        // Reset to Journal tab whenever Logs panel becomes visible
        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsLogsOpen) && vm.IsLogsOpen)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => ResetLogsToJournal());
                }
            };
        }
    }

    /// <summary>
    /// Handles the Tapped event on the FAB to add a backup
    /// </summary>
    private void FAB_Tapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.AddBackupCommand.CanExecute(null))
            viewModel.AddBackupCommand.Execute(null);
    }

    /// <summary>
    /// Handles the Tapped event on backup cards to open the edit modal
    /// </summary>
    private void BackupCard_Tapped(object? sender, TappedEventArgs e)
    {
        // Don't open modal if user clicked on a checkbox or button (or their children)
        var source = e.Source as Control;
        while (source != null)
        {
            if (source is CheckBox or Button)
            {
                return;
            }
            source = source.Parent as Control;
        }

        // Get the BackupJobViewModel from the DataContext
        if (sender is Border border && border.DataContext is BackupJobViewModel job)
        {
            // Get the MainViewModel and execute the command
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.OpenEditModalForJobCommand.Execute(job);
            }
        }
    }

    /// <summary>
    /// Handles search box text changes for real-time filtering
    /// </summary>
    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && DataContext is MainViewModel vm)
        {
            vm.SearchText = textBox.Text ?? string.Empty;
        }
    }

    /// <summary>
    /// Handles the Tapped event on the selection zone to toggle checkbox
    /// </summary>
    private void SelectionZone_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is BackupJobViewModel job)
        {
            job.IsSelected = !job.IsSelected;
        }
        e.Handled = true; // Prevent the card tap from firing
    }

    /// <summary>
    /// Handles the Browse button click for source paths
    /// </summary>
    private async void BrowseSourcePath_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is SourcePathViewModel sourcePathVm)
        {
            var options = new FolderPickerOpenOptions
            {
                Title = "Select Source Folder",
                AllowMultiple = false
            };

            var result = await StorageProvider.OpenFolderPickerAsync(options);

            if (result.Count > 0)
            {
                sourcePathVm.Path = result[0].Path.LocalPath;
            }
        }
    }

    /// <summary>
    /// Handles the Browse button click for target path
    /// </summary>
    private async void BrowseTargetPath_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            var options = new FolderPickerOpenOptions
            {
                Title = "Select Destination Folder",
                AllowMultiple = false
            };

            var result = await StorageProvider.OpenFolderPickerAsync(options);

            if (result.Count > 0)
            {
                viewModel.ModalTargetPath = result[0].Path.LocalPath;
            }
        }
    }

    /// <summary>
    /// Handles the Browse button click for log file path (Settings)
    /// </summary>
    private async void BrowseLogPath_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel mainVm)
        {
            var options = new FolderPickerOpenOptions
            {
                Title = "Select Log Directory",
                AllowMultiple = false
            };

            var result = await StorageProvider.OpenFolderPickerAsync(options);

            if (result.Count > 0)
            {
                string dir = result[0].Path.LocalPath;
                // Determine log file name from current format setting
                string ext = mainVm.SettingsVM.LogFormatIndex == 1 ? "xml" : "json";
                mainVm.SettingsVM.LogFilePath = System.IO.Path.Combine(dir, $"jobs.{ext}");
            }
        }
    }

    /// <summary>
    /// Handles the Browse button click for state file path (Settings)
    /// </summary>
    private async void BrowseStatePath_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel mainVm)
        {
            var options = new FolderPickerOpenOptions
            {
                Title = "Select State File Directory",
                AllowMultiple = false
            };

            var result = await StorageProvider.OpenFolderPickerAsync(options);

            if (result.Count > 0)
            {
                string dir = result[0].Path.LocalPath;
                mainVm.SettingsVM.StateFilePath = System.IO.Path.Combine(dir, "state.json");
            }
        }
    }

    #region Drag Reorder for Execute Order

    private void OrderItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Don't intercept button clicks (arrow buttons)
        var source = e.Source as Control;
        while (source != null && source != sender)
        {
            if (source is Button) return;
            source = source.Parent as Control;
        }

        if (sender is Border border && border.DataContext is BackupJobViewModel job
            && DataContext is MainViewModel vm)
        {
            _dragItem = border;
            _dragJob = job;
            _dragStartIndex = vm.ExecuteOrderJobs.IndexOf(job);
            _dragTargetIndex = _dragStartIndex;
            _dragStartY = e.GetPosition(this).Y;
            _isDragging = false;
            e.Pointer.Capture(border);
            e.Handled = true;
        }
    }

    private void OrderItem_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragItem == null || _dragJob == null || DataContext is not MainViewModel vm) return;

        var currentY = e.GetPosition(this).Y;
        var delta = currentY - _dragStartY;

        if (!_isDragging && Math.Abs(delta) > 5)
        {
            _isDragging = true;
            // Disable transitions on dragged item so it follows cursor instantly
            _savedTransitions = _dragItem.Transitions;
            _dragItem.Transitions = null;
            _dragItem.Opacity = 0.85;
            _dragItem.ZIndex = 100;
        }

        if (!_isDragging) return;

        // Dragged item follows the cursor
        var b = new TransformOperations.Builder(1);
        b.AppendTranslate(0, delta);
        _dragItem.RenderTransform = b.Build();

        // Auto-scroll near edges
        var scroll = this.FindControl<ScrollViewer>("ExecuteOrderScroll");
        if (scroll != null)
        {
            var posInScroll = e.GetPosition(scroll);
            const double edgeZone = 40;

            if (posInScroll.Y < edgeZone && scroll.Offset.Y > 0)
                _autoScrollSpeed = -Math.Max(3, (edgeZone - posInScroll.Y) * 0.3);
            else if (posInScroll.Y > scroll.Bounds.Height - edgeZone
                     && scroll.Offset.Y < scroll.Extent.Height - scroll.Viewport.Height)
                _autoScrollSpeed = Math.Max(3, (posInScroll.Y - (scroll.Bounds.Height - edgeZone)) * 0.3);
            else
                _autoScrollSpeed = 0;

            if (_autoScrollSpeed != 0 && _autoScrollTimer == null)
            {
                _autoScrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
                _autoScrollTimer.Tick += AutoScrollTick;
                _autoScrollTimer.Start();
            }
            else if (_autoScrollSpeed == 0)
            {
                StopAutoScroll();
            }
        }

        // Calculate target index based on how far we've moved
        var itemHeight = _dragItem.Bounds.Height + 6; // 6 = bottom margin
        if (itemHeight <= 0) return;

        var newTarget = Math.Clamp(
            _dragStartIndex + (int)Math.Round(delta / itemHeight),
            0, vm.ExecuteOrderJobs.Count - 1);

        if (newTarget != _dragTargetIndex)
        {
            _dragTargetIndex = newTarget;
            ShiftOtherItems(vm, itemHeight);
        }
    }

    private void ShiftOtherItems(MainViewModel vm, double itemHeight)
    {
        var itemsControl = this.FindControl<ItemsControl>("ExecuteOrderList");
        if (itemsControl == null) return;

        for (int i = 0; i < vm.ExecuteOrderJobs.Count; i++)
        {
            if (i == _dragStartIndex) continue;

            var container = itemsControl.ContainerFromIndex(i);
            if (container is not ContentPresenter cp || cp.Child is not Border border) continue;

            double shift = 0;
            if (_dragStartIndex < _dragTargetIndex)
            {
                // Dragging down: items between start+1..target shift up
                if (i > _dragStartIndex && i <= _dragTargetIndex)
                    shift = -itemHeight;
            }
            else if (_dragStartIndex > _dragTargetIndex)
            {
                // Dragging up: items between target..start-1 shift down
                if (i >= _dragTargetIndex && i < _dragStartIndex)
                    shift = itemHeight;
            }

            var b = new TransformOperations.Builder(1);
            b.AppendTranslate(0, shift);
            border.RenderTransform = b.Build();
        }
    }

    private void OrderItem_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragItem != null)
        {
            // Reset all visual transforms
            if (DataContext is MainViewModel vm)
            {
                var itemsControl = this.FindControl<ItemsControl>("ExecuteOrderList");
                if (itemsControl != null)
                {
                    for (int i = 0; i < vm.ExecuteOrderJobs.Count; i++)
                    {
                        var container = itemsControl.ContainerFromIndex(i);
                        if (container is ContentPresenter cp && cp.Child is Border border)
                        {
                            // Temporarily disable transitions during reset
                            var t = border.Transitions;
                            border.Transitions = null;
                            border.RenderTransform = null;
                            border.Opacity = 1;
                            border.ZIndex = 0;
                            border.Transitions = t;
                        }
                    }
                }

                // Restore transitions on dragged item
                _dragItem.Transitions = _savedTransitions;
                _savedTransitions = null;

                // Perform the actual collection move
                if (_isDragging && _dragStartIndex != _dragTargetIndex)
                {
                    vm.ExecuteOrderJobs.Move(_dragStartIndex, _dragTargetIndex);
                    vm.UpdateOrderIndices();
                }
            }

            e.Pointer.Capture(null);
        }

        _dragItem = null;
        _dragJob = null;
        _isDragging = false;
        StopAutoScroll();
    }

    private void AutoScrollTick(object? sender, EventArgs e)
    {
        var scroll = this.FindControl<ScrollViewer>("ExecuteOrderScroll");
        if (scroll == null || !_isDragging) { StopAutoScroll(); return; }

        var newOffset = Math.Clamp(
            scroll.Offset.Y + _autoScrollSpeed,
            0, scroll.Extent.Height - scroll.Viewport.Height);
        scroll.Offset = new Avalonia.Vector(0, newOffset);

        // Adjust dragStartY to compensate for scroll so item keeps following cursor
        _dragStartY -= _autoScrollSpeed;
    }

    private void StopAutoScroll()
    {
        _autoScrollTimer?.Stop();
        _autoScrollTimer = null;
        _autoScrollSpeed = 0;
    }

    #endregion

    #region Logs Tab Switching

    /// <summary>
    /// Shortcut to get a translated string from the localization service
    /// </summary>
    private static string T(string key)
    {
        try { return App.LocalizationService?.GetTextTranslated(key) ?? key; }
        catch { return key; }
    }

    /// <summary>
    /// Applies localized text to all Logs panel UI elements
    /// </summary>
    private void ApplyLogsLocalization()
    {
        var journalTab = this.FindControl<TextBlock>("JournalierTabText");
        var etatTab = this.FindControl<TextBlock>("EtatTabText");
        var journalJsonTitle = this.FindControl<TextBlock>("JournalJsonTitle");
        var etatJsonTitle = this.FindControl<TextBlock>("EtatJsonTitle");
        var journalPlaceholder = this.FindControl<TextBlock>("JournalPlaceholderText");
        var etatPlaceholder = this.FindControl<TextBlock>("EtatPlaceholderText");
        var copyBtn = this.FindControl<Button>("CopyJsonButton");
        var downloadBtn = this.FindControl<Button>("DownloadJsonButton");
        var etatCopyBtn = this.FindControl<Button>("EtatCopyJsonButton");
        var etatDownloadBtn = this.FindControl<Button>("EtatDownloadJsonButton");

        if (journalTab != null) journalTab.Text = T("logs_tab_journal");
        if (etatTab != null) etatTab.Text = T("logs_tab_state");
        if (journalJsonTitle != null && _currentJsonFilePath != null)
        {
            var jExt = Path.GetExtension(_currentJsonFilePath).TrimStart('.').ToUpperInvariant();
            journalJsonTitle.Text = jExt == "XML" ? T("logs_xml_content") : T("logs_json_content");
        }
        if (etatJsonTitle != null && _currentEtatJsonContent != null) etatJsonTitle.Text = T("logs_json_content");
        if (journalPlaceholder != null) journalPlaceholder.Text = T("logs_select_date");
        if (etatPlaceholder != null) etatPlaceholder.Text = T("logs_select_job");
        if (copyBtn != null) copyBtn.Content = T("logs_copy");
        if (downloadBtn != null) downloadBtn.Content = T("logs_download");
        if (etatCopyBtn != null) etatCopyBtn.Content = T("logs_copy");
        if (etatDownloadBtn != null) etatDownloadBtn.Content = T("logs_download");

        var journalSearch = this.FindControl<TextBox>("JournalSearchBox");
        var etatSearch = this.FindControl<TextBox>("EtatSearchBox");
        if (journalSearch != null) journalSearch.Watermark = T("logs_search_date");
        if (etatSearch != null) etatSearch.Watermark = T("logs_search_job");

        var journalJsonSearch = this.FindControl<TextBox>("JournalJsonSearchBox");
        var etatJsonSearch = this.FindControl<TextBox>("EtatJsonSearchBox");
        if (journalJsonSearch != null) journalJsonSearch.Watermark = T("logs_search_json");
        if (etatJsonSearch != null) etatJsonSearch.Watermark = T("logs_search_json");

        var journalJsonError = this.FindControl<TextBlock>("JournalJsonSearchError");
        var etatJsonError = this.FindControl<TextBlock>("EtatJsonSearchError");
        if (journalJsonError != null) journalJsonError.Text = T("logs_search_not_found");
        if (etatJsonError != null) etatJsonError.Text = T("logs_search_not_found");

        var journalGotoLabel = this.FindControl<TextBlock>("JournalPageGotoLabel");
        var etatGotoLabel = this.FindControl<TextBlock>("EtatPageGotoLabel");
        if (journalGotoLabel != null) journalGotoLabel.Text = T("logs_page_goto");
        if (etatGotoLabel != null) etatGotoLabel.Text = T("logs_page_goto");

        var calendarTodayBtn = this.FindControl<Button>("CalendarTodayButton");
        var calendarClearBtn = this.FindControl<Button>("CalendarClearButton");
        if (calendarTodayBtn != null) calendarTodayBtn.Content = T("logs_calendar_today");
        if (calendarClearBtn != null) calendarClearBtn.Content = T("logs_calendar_clear");

        var calendarBtn = this.FindControl<Button>("JournalCalendarButton");
        if (calendarBtn != null) ToolTip.SetTip(calendarBtn, T("logs_calendar_tooltip"));
    }

    /// <summary>
    /// Resets the Logs view to show the Journal tab by default
    /// </summary>
    public void ResetLogsToJournal()
    {
        var journalierBorder = this.FindControl<Border>("JournalierTabBorder");
        var etatBorder = this.FindControl<Border>("EtatTabBorder");
        var journalierContent = this.FindControl<Grid>("JournalierContent");
        var etatContent = this.FindControl<Grid>("EtatContent");

        if (journalierContent != null && etatContent != null)
        {
            journalierContent.IsVisible = true;
            etatContent.IsVisible = false;
        }

        if (journalierBorder != null && !journalierBorder.Classes.Contains("ActiveTab"))
            journalierBorder.Classes.Add("ActiveTab");
        if (etatBorder != null && etatBorder.Classes.Contains("ActiveTab"))
            etatBorder.Classes.Remove("ActiveTab");

        // Clear any selected date
        ClearSelectedDates();

        // Load dates from log files
        LoadLogDates();

        // Load state jobs
        LoadStateJobs();

        // Apply localized strings
        ApplyLogsLocalization();
    }

    /// <summary>
    /// Handles click on Journalier tab
    /// </summary>
    private void JournalierTab_Clicked(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        var journalierBorder = this.FindControl<Border>("JournalierTabBorder");
        var etatBorder = this.FindControl<Border>("EtatTabBorder");
        var journalierContent = this.FindControl<Grid>("JournalierContent");
        var etatContent = this.FindControl<Grid>("EtatContent");

        if (journalierBorder != null && !journalierBorder.Classes.Contains("ActiveTab"))
            journalierBorder.Classes.Add("ActiveTab");
        if (etatBorder != null && etatBorder.Classes.Contains("ActiveTab"))
            etatBorder.Classes.Remove("ActiveTab");

        if (journalierContent != null) journalierContent.IsVisible = true;
        if (etatContent != null) etatContent.IsVisible = false;
    }

    /// <summary>
    /// Handles click on Etat tab
    /// </summary>
    private void EtatTab_Clicked(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        var journalierBorder = this.FindControl<Border>("JournalierTabBorder");
        var etatBorder = this.FindControl<Border>("EtatTabBorder");
        var journalierContent = this.FindControl<Grid>("JournalierContent");
        var etatContent = this.FindControl<Grid>("EtatContent");

        if (etatBorder != null && !etatBorder.Classes.Contains("ActiveTab"))
            etatBorder.Classes.Add("ActiveTab");
        if (journalierBorder != null && journalierBorder.Classes.Contains("ActiveTab"))
            journalierBorder.Classes.Remove("ActiveTab");

        if (journalierContent != null) journalierContent.IsVisible = false;
        if (etatContent != null) etatContent.IsVisible = true;

        LoadStateJobs();
    }

    private static readonly IBrush TextPrimaryBrush = new SolidColorBrush(Color.FromRgb(26, 26, 26));
    private static readonly IBrush TextSecondaryBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85));
    private static readonly IBrush AppBackgroundBrush = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    private static readonly IBrush CardBorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));

    private string? _currentJsonContent;
    private string? _currentJsonFilePath;
    private string? _currentEtatJsonContent;

    /// <summary>
    /// Filters the Journal date list based on search text and re-applies pagination
    /// </summary>
    private void JournalSearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_journalSearchUpdating) return;
        if (sender is not TextBox searchBox) return;
        var filter = searchBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(filter))
        {
            // Reset: show all items with pagination
            _journalFilteredDateItems = null;
            _journalCurrentPage = 1;
            ApplyJournalPagination();
            return;
        }

        // Filter and show only matching items (no pagination during search)
        var panel = this.FindControl<StackPanel>("DateListPanel");
        var paginationBorder = this.FindControl<Border>("JournalPaginationBorder");
        if (panel == null) return;

        panel.Children.Clear();
        foreach (var border in _journalAllDateItems)
        {
            var textBlock = border.GetVisualDescendants().OfType<TextBlock>()
                .FirstOrDefault(tb => tb.FontWeight == Avalonia.Media.FontWeight.SemiBold);
            var displayText = textBlock?.Text ?? border.Tag?.ToString() ?? "";
            if (displayText.Contains(filter, StringComparison.OrdinalIgnoreCase))
                panel.Children.Add(border);
        }

        if (paginationBorder != null) paginationBorder.IsVisible = false;
    }

    /// <summary>
    /// Toggles the custom calendar panel visibility
    /// </summary>
    private void JournalCalendarButton_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var panel = this.FindControl<Border>("JournalCalendarPanel");
        if (panel == null) return;
        panel.IsVisible = !panel.IsVisible;

        if (panel.IsVisible)
        {
            var date = (_calendarYear > 0 && _calendarMonth > 0)
                ? new DateTime(_calendarYear, _calendarMonth, 1)
                : DateTime.Today;
            InitializeCalendar(date);
        }
    }

    /// <summary>
    /// Initializes the calendar combos and builds the day grid for the given date
    /// </summary>
    private void InitializeCalendar(DateTime date)
    {
        _calendarUpdating = true;
        _calendarMonth = date.Month;
        _calendarYear = date.Year;

        var monthCombo = this.FindControl<ComboBox>("CalendarMonthCombo");
        var yearCombo = this.FindControl<ComboBox>("CalendarYearCombo");

        var monthsStr = T("logs_calendar_months");
        var months = !string.IsNullOrEmpty(monthsStr)
            ? monthsStr.Split(',')
            : new[] { "January", "February", "March", "April", "May", "June",
                "July", "August", "September", "October", "November", "December" };

        if (monthCombo != null)
        {
            monthCombo.ItemsSource = months;
            monthCombo.SelectedIndex = _calendarMonth - 1;
        }

        if (yearCombo != null)
        {
            var years = new List<string>();
            for (int y = 1980; y <= 2050; y++)
                years.Add(y.ToString());
            yearCombo.ItemsSource = years;
            yearCombo.SelectedItem = _calendarYear.ToString();
        }

        _calendarUpdating = false;
        BuildCalendarDays();
    }

    /// <summary>
    /// Handles month selection change in the calendar
    /// </summary>
    private void CalendarMonth_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (_calendarUpdating) return;
        if (sender is ComboBox combo && combo.SelectedIndex >= 0)
        {
            _calendarMonth = combo.SelectedIndex + 1;
            BuildCalendarDays();
        }
    }

    /// <summary>
    /// Handles year selection change in the calendar
    /// </summary>
    private void CalendarYear_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (_calendarUpdating) return;
        if (sender is ComboBox combo && combo.SelectedItem is string yearStr && int.TryParse(yearStr, out int year))
        {
            _calendarYear = year;
            BuildCalendarDays();
        }
    }

    /// <summary>
    /// Builds the day grid: weekday headers + day buttons in a WrapPanel (7 columns)
    /// </summary>
    private void BuildCalendarDays()
    {
        var daysPanel = this.FindControl<WrapPanel>("CalendarDaysPanel");
        if (daysPanel == null) return;
        daysPanel.PointerReleased -= CalendarDays_PointerReleased;
        daysPanel.PointerMoved -= CalendarDays_PointerMoved;
        daysPanel.Children.Clear();
        daysPanel.PointerReleased += CalendarDays_PointerReleased;
        daysPanel.PointerMoved += CalendarDays_PointerMoved;

        var logsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasySave", "logs");

        // Weekday headers
        var dayHeadersStr = T("logs_calendar_days");
        string[] dayHeaders = !string.IsNullOrEmpty(dayHeadersStr)
            ? dayHeadersStr.Split(',')
            : new[] { "M", "T", "W", "T", "F", "S", "S" };
        foreach (var dh in dayHeaders)
        {
            daysPanel.Children.Add(new TextBlock
            {
                Text = dh,
                Width = 32,
                Height = 28,
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.Parse("#9E9E9E"))
            });
        }

        // Offset for first day of month (Monday = 0)
        var firstDay = new DateTime(_calendarYear, _calendarMonth, 1);
        int startOffset = ((int)firstDay.DayOfWeek + 6) % 7;
        for (int i = 0; i < startOffset; i++)
            daysPanel.Children.Add(new Border { Width = 32, Height = 32 });

        int daysInMonth = DateTime.DaysInMonth(_calendarYear, _calendarMonth);
        var today = DateTime.Today;

        for (int d = 1; d <= daysInMonth; d++)
        {
            var dateStr = $"{_calendarYear}-{_calendarMonth:D2}-{d:D2}";
            var logPathJson = Path.Combine(logsDir, $"jobs_{dateStr}.json");
            var logPathXml = Path.Combine(logsDir, $"jobs_{dateStr}.xml");
            bool hasLog = File.Exists(logPathJson) || File.Exists(logPathXml);
            bool isToday = (d == today.Day && _calendarMonth == today.Month && _calendarYear == today.Year);
            bool isSelected = _calendarSelectedDays.Contains(d);

            var tb = new TextBlock
            {
                Text = d.ToString(),
                FontSize = 12,
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var dayBorder = new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new Avalonia.CornerRadius(4),
                BorderThickness = new Avalonia.Thickness(1),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Tag = d,
                Child = tb
            };

            ApplyDayCellStyle(dayBorder, tb, isSelected, isToday, hasLog);

            dayBorder.PointerPressed += CalendarDay_PointerPressed;
            dayBorder.PointerEntered += CalendarDay_PointerEntered;
            daysPanel.Children.Add(dayBorder);
        }

        // Update selection label
        UpdateCalendarSelectionLabel();
    }

    /// <summary>
    /// Apply visual style to a calendar day cell
    /// </summary>
    private void ApplyDayCellStyle(Border dayBorder, TextBlock tb, bool isSelected, bool isToday, bool hasLog)
    {
        if (isSelected)
        {
            dayBorder.Background = new SolidColorBrush(Color.Parse("#2196F3"));
            tb.Foreground = Brushes.White;
            dayBorder.BorderBrush = new SolidColorBrush(Color.Parse("#2196F3"));
            tb.FontWeight = FontWeight.Bold;
        }
        else if (isToday)
        {
            dayBorder.Background = new SolidColorBrush(Color.Parse("#E3F2FD"));
            tb.Foreground = new SolidColorBrush(Color.Parse("#2196F3"));
            dayBorder.BorderBrush = new SolidColorBrush(Color.Parse("#2196F3"));
            tb.FontWeight = FontWeight.Bold;
        }
        else
        {
            dayBorder.Background = Brushes.Transparent;
            tb.Foreground = new SolidColorBrush(Color.Parse("#555555"));
            dayBorder.BorderBrush = Brushes.Transparent;
            tb.FontWeight = FontWeight.Normal;
        }
    }

    /// <summary>
    /// Pointer pressed on a calendar day — start drag selection
    /// </summary>
    private void CalendarDay_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.Tag is not int day) return;
        _calendarDragStartDay = day;
        _calendarIsDragging = true;
        _calendarSelectedDays.Clear();
        _calendarSelectedDays.Add(day);
        UpdateCalendarDayHighlights();
        e.Handled = true;
    }

    /// <summary>
    /// Pointer enters a day cell — extend selection if dragging
    /// </summary>
    private void CalendarDay_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (!_calendarIsDragging || _calendarDragStartDay < 0) return;
        if (sender is not Border border || border.Tag is not int day) return;
        _calendarSelectedDays.Clear();
        int min = Math.Min(_calendarDragStartDay, day);
        int max = Math.Max(_calendarDragStartDay, day);
        for (int d = min; d <= max; d++)
            _calendarSelectedDays.Add(d);
        UpdateCalendarDayHighlights();
    }

    /// <summary>
    /// Pointer moved on the days panel — extend selection via hit-test while dragging
    /// </summary>
    private void CalendarDays_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (!_calendarIsDragging || _calendarDragStartDay < 0) return;
        if (sender is not WrapPanel panel) return;
        var pos = e.GetPosition(panel);
        // Find the day cell under the pointer
        foreach (var child in panel.Children)
        {
            if (child is not Border border || border.Tag is not int day) continue;
            var bounds = border.Bounds;
            if (bounds.Contains(pos))
            {
                _calendarSelectedDays.Clear();
                int min = Math.Min(_calendarDragStartDay, day);
                int max = Math.Max(_calendarDragStartDay, day);
                for (int d = min; d <= max; d++)
                    _calendarSelectedDays.Add(d);
                UpdateCalendarDayHighlights();
                break;
            }
        }
    }

    /// <summary>
    /// Pointer released on the days panel — finalize drag selection
    /// </summary>
    private void CalendarDays_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (!_calendarIsDragging) return;
        _calendarIsDragging = false;

        // Close calendar
        var calPanel = this.FindControl<Border>("JournalCalendarPanel");
        if (calPanel != null) calPanel.IsVisible = false;

        if (_calendarSelectedDays.Count == 1)
        {
            SelectCalendarDate(new DateTime(_calendarYear, _calendarMonth, _calendarSelectedDays.First()));
        }
        else if (_calendarSelectedDays.Count > 1)
        {
            ApplyCalendarRangeSelection();
        }
    }

    /// <summary>
    /// Refresh visual highlights on all day cells based on _calendarSelectedDays
    /// </summary>
    private void UpdateCalendarDayHighlights()
    {
        var daysPanel = this.FindControl<WrapPanel>("CalendarDaysPanel");
        if (daysPanel == null) return;

        var logsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasySave", "logs");
        var today = DateTime.Today;

        foreach (var child in daysPanel.Children)
        {
            if (child is not Border border || border.Tag is not int day) continue;
            if (border.Child is not TextBlock tb) continue;

            bool isSelected = _calendarSelectedDays.Contains(day);
            var dateStr = $"{_calendarYear}-{_calendarMonth:D2}-{day:D2}";
            var logPathJson = Path.Combine(logsDir, $"jobs_{dateStr}.json");
            var logPathXml = Path.Combine(logsDir, $"jobs_{dateStr}.xml");
            bool hasLog = File.Exists(logPathJson) || File.Exists(logPathXml);
            bool isToday = (day == today.Day && _calendarMonth == today.Month && _calendarYear == today.Year);

            ApplyDayCellStyle(border, tb, isSelected, isToday, hasLog);
        }

        UpdateCalendarSelectionLabel();
    }

    /// <summary>
    /// Update the selection label under the calendar showing the selected range
    /// </summary>
    private void UpdateCalendarSelectionLabel()
    {
        var label = this.FindControl<TextBlock>("CalendarSelectionLabel");
        if (label == null) return;

        if (_calendarSelectedDays.Count == 0)
        {
            label.IsVisible = false;
            return;
        }

        var sorted = _calendarSelectedDays.OrderBy(d => d).ToList();
        if (sorted.Count == 1)
        {
            label.Text = $"{sorted[0]:D2}/{_calendarMonth:D2}/{_calendarYear}";
        }
        else
        {
            label.Text = $"{sorted.First():D2}/{_calendarMonth:D2}/{_calendarYear} → {sorted.Last():D2}/{_calendarMonth:D2}/{_calendarYear}";
        }
        label.IsVisible = true;
    }

    /// <summary>
    /// Apply a multi-day calendar range selection: filter the date list to matching dates
    /// </summary>
    private void ApplyCalendarRangeSelection()
    {
        var selectedDates = _calendarSelectedDays.OrderBy(d => d)
            .Select(d => new DateTime(_calendarYear, _calendarMonth, d))
            .ToList();

        // Find matching items in the date list
        var logsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasySave", "logs");

        var matchingItems = new List<Border>();
        foreach (var date in selectedDates)
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var expectedFileJson = Path.Combine(logsDir, $"jobs_{dateStr}.json");
            var expectedFileXml = Path.Combine(logsDir, $"jobs_{dateStr}.xml");
            var match = _journalAllDateItems.FirstOrDefault(b =>
                b.Tag is string path && (path.Equals(expectedFileJson, StringComparison.OrdinalIgnoreCase)
                    || path.Equals(expectedFileXml, StringComparison.OrdinalIgnoreCase)));
            if (match != null) matchingItems.Add(match);
        }

        // Set filtered list and paginate
        _journalFilteredDateItems = matchingItems;
        _journalCurrentPage = 1;
        ApplyJournalPagination();

        // Auto-select and load the first matching item
        if (matchingItems.Count > 0)
        {
            ClearSelectedDates();
            var firstItem = matchingItems[0];
            if (!firstItem.Classes.Contains("SelectedDate"))
                firstItem.Classes.Add("SelectedDate");

            if (firstItem.Tag is string filePath)
            {
                string jsonContent;
                try { jsonContent = File.ReadAllText(filePath); }
                catch (Exception ex) { jsonContent = $"{T("logs_read_error")}{ex.Message}"; }

                _currentJsonContent = jsonContent;
                _currentJsonFilePath = filePath;

                // Update title based on file format
                var jTitle = this.FindControl<TextBlock>("JournalJsonTitle");
                if (jTitle != null)
                {
                    var fmt = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
                    jTitle.Text = fmt == "XML" ? T("logs_xml_content") : T("logs_json_content");
                }

                var copyBtn = this.FindControl<Button>("CopyJsonButton");
                var dlBtn = this.FindControl<Button>("DownloadJsonButton");
                if (copyBtn != null) copyBtn.IsVisible = true;
                if (dlBtn != null) dlBtn.IsVisible = true;

                var jsonGrid = this.FindControl<Grid>("JournalJsonGrid");
                if (jsonGrid != null)
                {
                    if (jsonGrid.Children.Count > 2) jsonGrid.Children.RemoveAt(2);

                    var searchBar = this.FindControl<Grid>("JournalJsonSearchBar");
                    if (searchBar != null) searchBar.IsVisible = true;

                    var jSearchBox = this.FindControl<TextBox>("JournalJsonSearchBox");
                    if (jSearchBox != null) jSearchBox.Text = "";
                    var searchCount = this.FindControl<TextBlock>("JournalJsonSearchCount");
                    if (searchCount != null) searchCount.Text = "";
                    var searchError = this.FindControl<TextBlock>("JournalJsonSearchError");
                    if (searchError != null) searchError.IsVisible = false;

                    _journalJsonSearchMatches.Clear();
                    _journalJsonSearchIndex = -1;
                    _journalJsonLastQuery = "";

                    var scrollViewer = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
                    };

                    var jsonText = new TextBox
                    {
                        Name = "JournalJsonTextBox",
                        FontFamily = new Avalonia.Media.FontFamily("Consolas"),
                        FontSize = 13,
                        Padding = new Avalonia.Thickness(15),
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        IsReadOnly = true,
                        AcceptsReturn = true,
                        Text = jsonContent
                    };
                    jsonText.Classes.Add("JsonViewer");
                    _journalJsonTextBox = jsonText;

                    scrollViewer.Content = jsonText;
                    Grid.SetRow(scrollViewer, 2);
                    jsonGrid.Children.Add(scrollViewer);
                }
            }
        }
    }

    /// <summary>
    /// Navigates the calendar view to today's month/year (without selecting)
    /// </summary>
    private void CalendarToday_Clicked(object? sender, RoutedEventArgs e)
    {
        _calendarSelectedDays.Clear();
        InitializeCalendar(DateTime.Today);
    }

    /// <summary>
    /// Clears calendar date filter — restores all logs with pagination
    /// </summary>
    private void CalendarClear_Clicked(object? sender, RoutedEventArgs e)
    {
        _calendarSelectedDays.Clear();
        UpdateCalendarDayHighlights();

        // Clear search box safely
        var searchBox = this.FindControl<TextBox>("JournalSearchBox");
        if (searchBox != null)
        {
            searchBox.TextChanged -= JournalSearchBox_TextChanged;
            searchBox.Text = "";
            // Resubscribe after a dispatcher cycle to avoid async TextChanged
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                searchBox.TextChanged += JournalSearchBox_TextChanged;
            });
        }

        // Reset filtered list and restore full pagination
        _journalFilteredDateItems = null;
        _journalCurrentPage = 1;
        ApplyJournalPagination();

        // Hide calendar
        var panel = this.FindControl<Border>("JournalCalendarPanel");
        if (panel != null) panel.IsVisible = false;
    }

    /// <summary>
    /// Selects a single date from the calendar: filters the date list to that date and loads its JSON
    /// </summary>
    private void SelectCalendarDate(DateTime selectedDate)
    {
        var dateStr = selectedDate.ToString("yyyy-MM-dd");
        var logsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasySave", "logs");
        var expectedFileJson = Path.Combine(logsDir, $"jobs_{dateStr}.json");
        var expectedFileXml = Path.Combine(logsDir, $"jobs_{dateStr}.xml");

        // Find the matching item in the list (JSON or XML)
        var matchingItem = _journalAllDateItems.FirstOrDefault(b =>
            b.Tag is string path && (path.Equals(expectedFileJson, StringComparison.OrdinalIgnoreCase)
                || path.Equals(expectedFileXml, StringComparison.OrdinalIgnoreCase)));

        if (matchingItem != null)
        {
            // Set filtered list to just this one item and paginate
            _journalFilteredDateItems = new List<Border> { matchingItem };
            _journalCurrentPage = 1;
            ApplyJournalPagination();

            ClearSelectedDates();
            if (!matchingItem.Classes.Contains("SelectedDate"))
                matchingItem.Classes.Add("SelectedDate");

            if (matchingItem.Tag is string filePath)
            {
                string jsonContent;
                try { jsonContent = File.ReadAllText(filePath); }
                catch (Exception ex) { jsonContent = $"{T("logs_read_error")}{ex.Message}"; }

                _currentJsonContent = jsonContent;
                _currentJsonFilePath = filePath;

                // Update title based on file format
                var jTitle = this.FindControl<TextBlock>("JournalJsonTitle");
                if (jTitle != null)
                {
                    var fmt = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
                    jTitle.Text = fmt == "XML" ? T("logs_xml_content") : T("logs_json_content");
                }

                var copyBtn = this.FindControl<Button>("CopyJsonButton");
                var dlBtn = this.FindControl<Button>("DownloadJsonButton");
                if (copyBtn != null) copyBtn.IsVisible = true;
                if (dlBtn != null) dlBtn.IsVisible = true;

                var jsonGrid = this.FindControl<Grid>("JournalJsonGrid");
                if (jsonGrid != null)
                {
                    if (jsonGrid.Children.Count > 2) jsonGrid.Children.RemoveAt(2);

                    var searchBar = this.FindControl<Grid>("JournalJsonSearchBar");
                    if (searchBar != null) searchBar.IsVisible = true;

                    var jSearchBox = this.FindControl<TextBox>("JournalJsonSearchBox");
                    if (jSearchBox != null) jSearchBox.Text = "";
                    var searchCount = this.FindControl<TextBlock>("JournalJsonSearchCount");
                    if (searchCount != null) searchCount.Text = "";
                    var searchError = this.FindControl<TextBlock>("JournalJsonSearchError");
                    if (searchError != null) searchError.IsVisible = false;

                    _journalJsonSearchMatches.Clear();
                    _journalJsonSearchIndex = -1;
                    _journalJsonLastQuery = "";

                    var scrollViewer = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
                    };

                    var jsonText = new TextBox
                    {
                        Name = "JournalJsonTextBox",
                        FontFamily = new Avalonia.Media.FontFamily("Consolas"),
                        FontSize = 13,
                        Padding = new Avalonia.Thickness(15),
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        IsReadOnly = true,
                        AcceptsReturn = true,
                        Text = jsonContent
                    };
                    jsonText.Classes.Add("JsonViewer");
                    _journalJsonTextBox = jsonText;

                    scrollViewer.Content = jsonText;
                    Grid.SetRow(scrollViewer, 2);
                    jsonGrid.Children.Add(scrollViewer);
                }
            }
        }
        else
        {
            // No log for this date — show empty filtered list with message
            _journalFilteredDateItems = new List<Border>();
            _journalCurrentPage = 1;
            ApplyJournalPagination();
        }
    }

    /// <summary>
    /// Filters the Etat job list based on search text and re-applies pagination
    /// </summary>
    private void EtatSearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox searchBox) return;
        var filter = searchBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(filter))
        {
            _etatCurrentPage = 1;
            ApplyEtatPagination();
            return;
        }

        var panel = this.FindControl<StackPanel>("EtatJobListPanel");
        var paginationBorder = this.FindControl<Border>("EtatPaginationBorder");
        if (panel == null) return;

        panel.Children.Clear();
        foreach (var border in _etatAllJobItems)
        {
            var jobName = border.Tag?.ToString() ?? "";
            if (jobName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                panel.Children.Add(border);
        }

        if (paginationBorder != null) paginationBorder.IsVisible = false;
    }

    /// <summary>
    /// Returns the path to the EasySave state.json file in AppData
    /// </summary>
    private static string GetStateFilePath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasySave", "state.json");
    }

    /// <summary>
    /// Loads job entries from state.json and populates the Etat job list dynamically with pagination
    /// </summary>
    private void LoadStateJobs()
    {
        var panel = this.FindControl<StackPanel>("EtatJobListPanel");
        if (panel == null) return;

        panel.Children.Clear();
        _etatAllJobItems.Clear();

        var stateFilePath = GetStateFilePath();
        if (!File.Exists(stateFilePath)) return;

        try
        {
            var json = File.ReadAllText(stateFilePath);
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}") return;

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var jobName = prop.Name;

                var bullet = new TextBlock
                {
                    Text = "\u25cf",
                    FontSize = 12,
                    Margin = new Avalonia.Thickness(0, 0, 10, 0),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                var nameText = new TextBlock
                {
                    Text = jobName,
                    FontSize = 14,
                    FontWeight = Avalonia.Media.FontWeight.SemiBold,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                var etatFormatTag = new Border
                {
                    Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(46, 204, 113)),
                    CornerRadius = new Avalonia.CornerRadius(4),
                    Padding = new Avalonia.Thickness(6, 2),
                    Margin = new Avalonia.Thickness(8, 0, 0, 0),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = "JSON",
                        FontSize = 10,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        Foreground = Avalonia.Media.Brushes.White
                    }
                };

                var stack = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Height = 40
                };
                stack.Children.Add(bullet);
                stack.Children.Add(nameText);
                stack.Children.Add(etatFormatTag);

                var border = new Border
                {
                    Margin = new Avalonia.Thickness(0, 5),
                    Padding = new Avalonia.Thickness(10),
                    CornerRadius = new Avalonia.CornerRadius(5),
                    Tag = jobName,
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                    Child = stack
                };
                border.Classes.Add("JobItem");
                border.PointerPressed += JobItem_Clicked;

                _etatAllJobItems.Add(border);
            }
        }
        catch { /* state.json might be malformed or locked */ }

        _etatCurrentPage = 1;
        ApplyEtatPagination();
    }

    /// <summary>
    /// Scans the EasySave logs directory and populates the date list dynamically with pagination
    /// </summary>
    private void LoadLogDates()
    {
        var dateListPanel = this.FindControl<StackPanel>("DateListPanel");
        if (dateListPanel == null) return;

        dateListPanel.Children.Clear();
        _journalAllDateItems.Clear();

        var logsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasySave", "logs");

        if (!Directory.Exists(logsDir))
        {
            var noLogsText = new TextBlock
            {
                Text = T("logs_no_logs_found"),
                FontSize = 14,
                Foreground = TextSecondaryBrush,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 20, 0, 0)
            };
            dateListPanel.Children.Add(noLogsText);
            return;
        }

        var jsonFiles = Directory.GetFiles(logsDir, "jobs_*.json");
        var xmlFiles = Directory.GetFiles(logsDir, "jobs_*.xml");
        var logFiles = jsonFiles.Concat(xmlFiles)
            .OrderByDescending(f => f)
            .ToList();

        if (logFiles.Count == 0)
        {
            var noLogsText = new TextBlock
            {
                Text = T("logs_no_logs_found"),
                FontSize = 14,
                Foreground = TextSecondaryBrush,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 20, 0, 0)
            };
            dateListPanel.Children.Add(noLogsText);
            return;
        }

        foreach (var filePath in logFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            // Extract date from filename: jobs_2026-02-10 -> 2026-02-10
            var dateStr = fileName.Replace("jobs_", "");

            if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date))
                continue;

            var displayDate = date.ToString("dd/MM/yyyy");

            var bullet = new TextBlock
            {
                Text = "\u25cf",
                FontSize = 12,
                Margin = new Avalonia.Thickness(0, 0, 10, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var dateText = new TextBlock
            {
                Text = displayDate,
                FontSize = 14,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var ext = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
            var formatTag = new Border
            {
                Background = ext == "XML"
                    ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(230, 126, 34))
                    : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(46, 204, 113)),
                CornerRadius = new Avalonia.CornerRadius(4),
                Padding = new Avalonia.Thickness(6, 2),
                Margin = new Avalonia.Thickness(8, 0, 0, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = ext,
                    FontSize = 10,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    Foreground = Avalonia.Media.Brushes.White
                }
            };

            var stack = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Height = 40
            };
            stack.Children.Add(bullet);
            stack.Children.Add(dateText);
            stack.Children.Add(formatTag);

            var border = new Border
            {
                Margin = new Avalonia.Thickness(0, 5),
                Padding = new Avalonia.Thickness(10),
                CornerRadius = new Avalonia.CornerRadius(5),
                Tag = filePath,  // Store the full file path
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Child = stack
            };
            border.Classes.Add("JobItem");
            border.PointerPressed += DateItem_Clicked;

            _journalAllDateItems.Add(border);
        }

        _journalCurrentPage = 1;
        ApplyJournalPagination();
    }

    /// <summary>
    /// Clears the SelectedDate class from all date items in the Journal list
    /// </summary>
    private void ClearSelectedDates()
    {
        var journalierContent = this.FindControl<Grid>("JournalierContent");
        if (journalierContent == null) return;

        foreach (var border in journalierContent.GetVisualDescendants().OfType<Border>())
        {
            if (border.Classes.Contains("SelectedDate"))
                border.Classes.Remove("SelectedDate");
        }
    }

    /// <summary>
    /// Handles click on a date item in Journal tab to display its JSON in the right panel
    /// </summary>
    private void DateItem_Clicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is string filePath)
        {
            // Mark selected date
            ClearSelectedDates();
            if (!border.Classes.Contains("SelectedDate"))
                border.Classes.Add("SelectedDate");

            // Read the actual JSON file
            string jsonContent;
            try
            {
                jsonContent = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                jsonContent = $"{T("logs_read_error")}{ex.Message}";
            }

            _currentJsonContent = jsonContent;
            _currentJsonFilePath = filePath;

            // Update title based on file format
            var journalTitle = this.FindControl<TextBlock>("JournalJsonTitle");
            if (journalTitle != null)
            {
                var format = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
                journalTitle.Text = format == "XML" ? T("logs_xml_content") : T("logs_json_content");
            }

            // Show copy/download buttons
            var copyBtn = this.FindControl<Button>("CopyJsonButton");
            var dlBtn = this.FindControl<Button>("DownloadJsonButton");
            if (copyBtn != null) copyBtn.IsVisible = true;
            if (dlBtn != null) dlBtn.IsVisible = true;

            var jsonGrid = this.FindControl<Grid>("JournalJsonGrid");
            if (jsonGrid != null)
            {
                // Remove previous content (placeholder or scrollviewer) at index 2
                if (jsonGrid.Children.Count > 2)
                {
                    jsonGrid.Children.RemoveAt(2);
                }

                // Show the JSON search bar
                var searchBar = this.FindControl<Grid>("JournalJsonSearchBar");
                if (searchBar != null) searchBar.IsVisible = true;

                // Reset search state
                var searchBox = this.FindControl<TextBox>("JournalJsonSearchBox");
                if (searchBox != null) searchBox.Text = "";
                var searchCount = this.FindControl<TextBlock>("JournalJsonSearchCount");
                if (searchCount != null) searchCount.Text = "";
                var searchError = this.FindControl<TextBlock>("JournalJsonSearchError");
                if (searchError != null) searchError.IsVisible = false;

                // Clear previous search matches
                _journalJsonSearchMatches.Clear();
                _journalJsonSearchIndex = -1;
                _journalJsonLastQuery = "";

                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
                };

                var jsonText = new TextBox
                {
                    Name = "JournalJsonTextBox",
                    FontFamily = new Avalonia.Media.FontFamily("Consolas"),
                    FontSize = 13,
                    Padding = new Avalonia.Thickness(15),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    IsReadOnly = true,
                    AcceptsReturn = true,
                    Text = jsonContent
                };
                jsonText.Classes.Add("JsonViewer");

                // Store direct reference for search and attach KeyDown for Enter navigation
                _journalJsonTextBox = jsonText;
                jsonText.KeyDown += JournalJsonTextBox_KeyDown;

                scrollViewer.Content = jsonText;
                Grid.SetRow(scrollViewer, 2);
                jsonGrid.Children.Add(scrollViewer);
            }
        }
    }

    /// <summary>
    /// Copies the current JSON content to clipboard
    /// </summary>
    private async void CopyJson_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentJsonContent != null && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(_currentJsonContent);

            // Visual feedback: change button text briefly
            if (sender is Button btn)
            {
                var original = btn.Content;
                btn.Content = T("logs_copied");
                await System.Threading.Tasks.Task.Delay(1500);
                btn.Content = original;
            }
        }
    }

    /// <summary>
    /// Opens a save dialog to download/save the current JSON file
    /// </summary>
    private async void DownloadJson_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentJsonContent == null) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var defaultName = _currentJsonFilePath != null
            ? Path.GetFileName(_currentJsonFilePath)
            : "logs.json";

        var isXml = _currentJsonFilePath != null
            && Path.GetExtension(_currentJsonFilePath).Equals(".xml", StringComparison.OrdinalIgnoreCase);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = isXml ? T("logs_save_xml") : T("logs_save_json"),
            SuggestedFileName = defaultName,
            FileTypeChoices = isXml
                ? new[] { new Avalonia.Platform.Storage.FilePickerFileType("XML") { Patterns = new[] { "*.xml" } } }
                : new[] { new Avalonia.Platform.Storage.FilePickerFileType("JSON") { Patterns = new[] { "*.json" } } }

        });

        if (file != null)
        {
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new System.IO.StreamWriter(stream);
            await writer.WriteAsync(_currentJsonContent);
        }
    }

    /// <summary>
    /// Handles click on a job item in Etat tab to display its JSON
    /// </summary>
    private void JobItem_Clicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is string jobName)
        {
            // Clear previous selection in Etat content
            var etatContent = this.FindControl<Grid>("EtatContent");
            if (etatContent != null)
            {
                foreach (var b in etatContent.GetVisualDescendants().OfType<Border>())
                {
                    if (b.Classes.Contains("SelectedDate"))
                        b.Classes.Remove("SelectedDate");
                }
            }
            // Mark current item as selected
            if (!border.Classes.Contains("SelectedDate"))
                border.Classes.Add("SelectedDate");

            // Read the job's JSON from state.json
            string? jobJson = null;
            try
            {
                var stateFilePath = GetStateFilePath();
                if (File.Exists(stateFilePath))
                {
                    var stateContent = File.ReadAllText(stateFilePath);
                    using var doc = System.Text.Json.JsonDocument.Parse(stateContent);
                    if (doc.RootElement.TryGetProperty(jobName, out var jobElement))
                    {
                        jobJson = System.Text.Json.JsonSerializer.Serialize(jobElement,
                            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    }
                }
            }
            catch { /* state.json might be locked or malformed */ }

            if (jobJson != null)
            {
                _currentEtatJsonContent = jobJson;

                // Show copy/download buttons
                var etatCopyBtn = this.FindControl<Button>("EtatCopyJsonButton");
                var etatDownloadBtn = this.FindControl<Button>("EtatDownloadJsonButton");
                if (etatCopyBtn != null) etatCopyBtn.IsVisible = true;
                if (etatDownloadBtn != null) etatDownloadBtn.IsVisible = true;

                // Find the JSON display grid in Etat content
                var jsonGrid = this.FindControl<Grid>("EtatJsonGrid");

                if (jsonGrid != null)
                {
                    // Remove previous content (placeholder or scrollviewer) at index 2
                    if (jsonGrid.Children.Count > 2)
                    {
                        jsonGrid.Children.RemoveAt(2);
                    }

                    // Show the JSON search bar
                    var etatSearchBar = this.FindControl<Grid>("EtatJsonSearchBar");
                    if (etatSearchBar != null) etatSearchBar.IsVisible = true;

                    // Reset search state
                    var etatSearchBox = this.FindControl<TextBox>("EtatJsonSearchBox");
                    if (etatSearchBox != null) etatSearchBox.Text = "";
                    var etatSearchCount = this.FindControl<TextBlock>("EtatJsonSearchCount");
                    if (etatSearchCount != null) etatSearchCount.Text = "";
                    var etatSearchError = this.FindControl<TextBlock>("EtatJsonSearchError");
                    if (etatSearchError != null) etatSearchError.IsVisible = false;

                    // Clear previous search matches
                    _etatJsonSearchMatches.Clear();
                    _etatJsonSearchIndex = -1;
                    _etatJsonLastQuery = "";

                    var scrollViewer = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
                    };

                    var jsonText = new TextBox
                    {
                        Name = "EtatJsonTextBox",
                        FontFamily = new Avalonia.Media.FontFamily("Consolas"),
                        FontSize = 13,
                        Padding = new Avalonia.Thickness(15),
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        IsReadOnly = true,
                        AcceptsReturn = true,
                        Text = jobJson
                    };
                    jsonText.Classes.Add("JsonViewer");

                    // Store direct reference for search and attach KeyDown for Enter navigation
                    _etatJsonTextBox = jsonText;
                    jsonText.KeyDown += EtatJsonTextBox_KeyDown;

                    scrollViewer.Content = jsonText;
                    Grid.SetRow(scrollViewer, 2);
                    jsonGrid.Children.Add(scrollViewer);
                }
            }
        }
    }

    /// <summary>
    /// Copies the Etat JSON content to clipboard
    /// </summary>
    private async void EtatCopyJson_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentEtatJsonContent != null && TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(_currentEtatJsonContent);

            if (sender is Button btn)
            {
                var original = btn.Content;
                btn.Content = T("logs_copied");
                await System.Threading.Tasks.Task.Delay(1500);
                btn.Content = original;
            }
        }
    }

    /// <summary>
    /// Opens a save dialog to download/save the Etat JSON content
    /// </summary>
    private async void EtatDownloadJson_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_currentEtatJsonContent == null) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = T("logs_save_json"),
            SuggestedFileName = "state.json",
            FileTypeChoices = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("JSON") { Patterns = new[] { "*.json" } }
            }
        });

        if (file != null)
        {
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new System.IO.StreamWriter(stream);
            await writer.WriteAsync(_currentEtatJsonContent);
        }
    }

    #endregion

    #region JSON Search

    // Direct references to dynamically created JSON TextBoxes
    private TextBox? _journalJsonTextBox;
    private TextBox? _etatJsonTextBox;

    // Search state for Journal JSON
    private List<int> _journalJsonSearchMatches = new();
    private int _journalJsonSearchIndex = -1;
    private string _journalJsonLastQuery = "";

    // Search state for Etat JSON
    private List<int> _etatJsonSearchMatches = new();
    private int _etatJsonSearchIndex = -1;
    private string _etatJsonLastQuery = "";

    /// <summary>
    /// Handles Enter key press in Journal JSON search box.
    /// First Enter: performs search. Subsequent Enter: navigates to next match.
    /// </summary>
    private void JournalJsonSearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        if (sender is not TextBox searchBox) return;
        var query = searchBox.Text?.Trim() ?? "";
        var countLabel = this.FindControl<TextBlock>("JournalJsonSearchCount");
        var errorLabel = this.FindControl<TextBlock>("JournalJsonSearchError");

        // Same query and matches exist → navigate to next
        if (query == _journalJsonLastQuery && _journalJsonSearchMatches.Count > 0)
        {
            NavigateJsonSearch(1, _journalJsonTextBox, countLabel, _journalJsonSearchMatches, ref _journalJsonSearchIndex);
            return;
        }

        _journalJsonLastQuery = query;
        PerformJsonSearch(query, _journalJsonTextBox, countLabel, errorLabel,
            ref _journalJsonSearchMatches, ref _journalJsonSearchIndex);
    }

    private void JournalJsonSearchPrev_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var countLabel = this.FindControl<TextBlock>("JournalJsonSearchCount");
        NavigateJsonSearch(-1, _journalJsonTextBox, countLabel, _journalJsonSearchMatches, ref _journalJsonSearchIndex);
    }

    private void JournalJsonSearchNext_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var countLabel = this.FindControl<TextBlock>("JournalJsonSearchCount");
        NavigateJsonSearch(1, _journalJsonTextBox, countLabel, _journalJsonSearchMatches, ref _journalJsonSearchIndex);
    }

    /// <summary>
    /// Handles Enter key press in Etat JSON search box.
    /// First Enter: performs search. Subsequent Enter: navigates to next match.
    /// </summary>
    private void EtatJsonSearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        if (sender is not TextBox searchBox) return;
        var query = searchBox.Text?.Trim() ?? "";
        var countLabel = this.FindControl<TextBlock>("EtatJsonSearchCount");
        var errorLabel = this.FindControl<TextBlock>("EtatJsonSearchError");

        // Same query and matches exist → navigate to next
        if (query == _etatJsonLastQuery && _etatJsonSearchMatches.Count > 0)
        {
            NavigateJsonSearch(1, _etatJsonTextBox, countLabel, _etatJsonSearchMatches, ref _etatJsonSearchIndex);
            return;
        }

        _etatJsonLastQuery = query;
        PerformJsonSearch(query, _etatJsonTextBox, countLabel, errorLabel,
            ref _etatJsonSearchMatches, ref _etatJsonSearchIndex);
    }

    private void EtatJsonSearchPrev_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var countLabel = this.FindControl<TextBlock>("EtatJsonSearchCount");
        NavigateJsonSearch(-1, _etatJsonTextBox, countLabel, _etatJsonSearchMatches, ref _etatJsonSearchIndex);
    }

    private void EtatJsonSearchNext_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var countLabel = this.FindControl<TextBlock>("EtatJsonSearchCount");
        NavigateJsonSearch(1, _etatJsonTextBox, countLabel, _etatJsonSearchMatches, ref _etatJsonSearchIndex);
    }

    /// <summary>
    /// Performs a case-insensitive search in the JSON TextBox, finds all matches,
    /// highlights the first one with SelectionBrush, or shows a red error if not found.
    /// </summary>
    private void PerformJsonSearch(string query, TextBox? textBox, TextBlock? countLabel,
        TextBlock? errorLabel, ref List<int> matches, ref int currentIndex)
    {
        matches.Clear();
        currentIndex = -1;

        // Hide error by default
        if (errorLabel != null) errorLabel.IsVisible = false;

        if (textBox == null || string.IsNullOrEmpty(query) || string.IsNullOrEmpty(textBox.Text))
        {
            if (countLabel != null) countLabel.Text = "";
            // Reset selection
            if (textBox != null)
            {
                textBox.SelectionStart = 0;
                textBox.SelectionEnd = 0;
            }
            return;
        }

        var text = textBox.Text;
        int pos = 0;
        while ((pos = text.IndexOf(query, pos, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            matches.Add(pos);
            pos += query.Length;
        }

        if (matches.Count == 0)
        {
            if (countLabel != null) countLabel.Text = "0/0";
            // Show red error message
            if (errorLabel != null) errorLabel.IsVisible = true;
            // Clear any previous selection
            textBox.SelectionStart = 0;
            textBox.SelectionEnd = 0;
            return;
        }

        // Apply highlight brush for visible selection
        ApplySearchHighlightBrush(textBox);

        currentIndex = 0;
        SelectMatch(textBox, countLabel, matches, currentIndex, query.Length);
    }

    /// <summary>
    /// Sets the SelectionBrush on the TextBox to the theme-aware highlight color
    /// so the selected/found text is clearly visible.
    /// </summary>
    private void ApplySearchHighlightBrush(TextBox textBox)
    {
        try
        {
            var highlightBrush = this.FindResource("SearchHighlight") as IBrush;
            var highlightTextBrush = this.FindResource("SearchHighlightText") as IBrush;
            if (highlightBrush != null)
                textBox.SelectionBrush = highlightBrush;
            if (highlightTextBrush != null)
                textBox.SelectionForegroundBrush = highlightTextBrush;
        }
        catch
        {
            // Fallback: bright yellow highlight
            textBox.SelectionBrush = new SolidColorBrush(Color.FromRgb(255, 235, 59));
            textBox.SelectionForegroundBrush = new SolidColorBrush(Color.FromRgb(26, 26, 26));
        }
    }

    /// <summary>
    /// Navigates to the previous or next match in the JSON TextBox.
    /// </summary>
    private void NavigateJsonSearch(int direction, TextBox? textBox, TextBlock? countLabel,
        List<int> matches, ref int currentIndex)
    {
        if (matches.Count == 0 || textBox == null) return;

        currentIndex += direction;
        if (currentIndex < 0) currentIndex = matches.Count - 1;
        if (currentIndex >= matches.Count) currentIndex = 0;

        // Determine query length from the search box
        var searchBox = textBox == _journalJsonTextBox
            ? this.FindControl<TextBox>("JournalJsonSearchBox")
            : this.FindControl<TextBox>("EtatJsonSearchBox");
        var queryLen = searchBox?.Text?.Trim().Length ?? 0;
        if (queryLen == 0) return;

        SelectMatch(textBox, countLabel, matches, currentIndex, queryLen);
    }

    /// <summary>
    /// Selects the match at the given index in the TextBox and updates the counter label.
    /// Focuses the TextBox so the selection highlight stays visible.
    /// </summary>
    private void SelectMatch(TextBox textBox, TextBlock? countLabel,
        List<int> matches, int index, int queryLength)
    {
        if (index < 0 || index >= matches.Count) return;

        // Move caret first to scroll the match into view
        textBox.CaretIndex = matches[index];
        textBox.SelectionStart = matches[index];
        textBox.SelectionEnd = matches[index] + queryLength;

        // Focus the TextBox so the highlight remains visible
        textBox.Focus();

        if (countLabel != null)
            countLabel.Text = $"{index + 1}/{matches.Count}";
    }

    /// <summary>
    /// Handles Enter key on Journal JSON TextBox to navigate to next search match.
    /// </summary>
    private void JournalJsonTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || _journalJsonSearchMatches.Count == 0) return;
        e.Handled = true;
        var countLabel = this.FindControl<TextBlock>("JournalJsonSearchCount");
        NavigateJsonSearch(1, _journalJsonTextBox, countLabel, _journalJsonSearchMatches, ref _journalJsonSearchIndex);
    }

    /// <summary>
    /// Handles Enter key on Etat JSON TextBox to navigate to next search match.
    /// </summary>
    private void EtatJsonTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || _etatJsonSearchMatches.Count == 0) return;
        e.Handled = true;
        var countLabel = this.FindControl<TextBlock>("EtatJsonSearchCount");
        NavigateJsonSearch(1, _etatJsonTextBox, countLabel, _etatJsonSearchMatches, ref _etatJsonSearchIndex);
    }

    #endregion

    #region Pagination

    private const int ItemsPerPage = 10;

    // Calendar state
    private int _calendarMonth;
    private int _calendarYear;
    private bool _calendarUpdating;
    private HashSet<int> _calendarSelectedDays = new();
    private int _calendarDragStartDay = -1;
    private bool _calendarIsDragging;

    // Journal pagination state
    private List<Border> _journalAllDateItems = new();
    private List<Border>? _journalFilteredDateItems = null;
    private int _journalCurrentPage = 1;
    private bool _journalSearchUpdating;

    // Etat pagination state
    private List<Border> _etatAllJobItems = new();
    private int _etatCurrentPage = 1;

    /// <summary>
    /// Applies pagination to the Journal date list, showing only the current page items.
    /// </summary>
    private void ApplyJournalPagination()
    {
        var panel = this.FindControl<StackPanel>("DateListPanel");
        var paginationBorder = this.FindControl<Border>("JournalPaginationBorder");
        var buttonsPanel = this.FindControl<StackPanel>("JournalPaginationButtons");
        var countLabel = this.FindControl<TextBlock>("JournalPageCountLabel");
        if (panel == null) return;

        panel.Children.Clear();

        var sourceItems = _journalFilteredDateItems ?? _journalAllDateItems;

        if (sourceItems.Count == 0)
        {
            var noLogsText = new TextBlock
            {
                Text = T("logs_no_logs_found"),
                FontSize = 14,
                Foreground = TextSecondaryBrush,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 20, 0, 0)
            };
            panel.Children.Add(noLogsText);
            if (paginationBorder != null) paginationBorder.IsVisible = false;
            return;
        }

        var totalPages = Math.Max(1, (int)Math.Ceiling(sourceItems.Count / (double)ItemsPerPage));
        if (_journalCurrentPage > totalPages) _journalCurrentPage = totalPages;
        if (_journalCurrentPage < 1) _journalCurrentPage = 1;

        var pageItems = sourceItems
            .Skip((_journalCurrentPage - 1) * ItemsPerPage)
            .Take(ItemsPerPage)
            .ToList();

        foreach (var item in pageItems)
        {
            if (item.Parent is Panel oldParent)
                oldParent.Children.Remove(item);
            panel.Children.Add(item);
        }

        BuildPaginationButtons(buttonsPanel, _journalCurrentPage, totalPages, page =>
        {
            _journalCurrentPage = page;
            ApplyJournalPagination();
        });

        UpdatePageCountLabel(countLabel, _journalCurrentPage, totalPages);
        if (paginationBorder != null) paginationBorder.IsVisible = true;
    }

    /// <summary>
    /// Applies pagination to the Etat job list, showing only the current page items.
    /// </summary>
    private void ApplyEtatPagination()
    {
        var panel = this.FindControl<StackPanel>("EtatJobListPanel");
        var paginationBorder = this.FindControl<Border>("EtatPaginationBorder");
        var buttonsPanel = this.FindControl<StackPanel>("EtatPaginationButtons");
        var countLabel = this.FindControl<TextBlock>("EtatPageCountLabel");
        if (panel == null) return;

        panel.Children.Clear();

        var totalPages = Math.Max(1, (int)Math.Ceiling(_etatAllJobItems.Count / (double)ItemsPerPage));
        if (_etatCurrentPage > totalPages) _etatCurrentPage = totalPages;
        if (_etatCurrentPage < 1) _etatCurrentPage = 1;

        var pageItems = _etatAllJobItems
            .Skip((_etatCurrentPage - 1) * ItemsPerPage)
            .Take(ItemsPerPage);

        foreach (var item in pageItems)
            panel.Children.Add(item);

        BuildPaginationButtons(buttonsPanel, _etatCurrentPage, totalPages, page =>
        {
            _etatCurrentPage = page;
            ApplyEtatPagination();
        });

        UpdatePageCountLabel(countLabel, _etatCurrentPage, totalPages);
        if (paginationBorder != null) paginationBorder.IsVisible = true;
    }

    /// <summary>
    /// Updates the "Page X sur Y" label.
    /// </summary>
    private void UpdatePageCountLabel(TextBlock? label, int currentPage, int totalPages)
    {
        if (label == null) return;
        var surText = T("logs_page_of");
        label.Text = $"{T("logs_page_label")} {currentPage} {surText} {totalPages}";
    }

    /// <summary>
    /// Builds pagination buttons (◀ 1 2 3 ... N ▶) inside the given panel.
    /// Always visible, even with just 1 page.
    /// </summary>
    private void BuildPaginationButtons(StackPanel? panel, int currentPage, int totalPages, Action<int> onPageChanged)
    {
        if (panel == null) return;
        panel.Children.Clear();

        // Previous button — only shown when not on first page
        if (currentPage > 1)
        {
            var prevBtn = CreatePageButton("◀", true, () => onPageChanged(currentPage - 1));
            panel.Children.Add(prevBtn);
        }

        // Page number buttons
        var pages = GetPageNumbers(currentPage, totalPages);
        foreach (var p in pages)
        {
            if (p == -1)
            {
                var ellipsis = new TextBlock
                {
                    Text = "...",
                    FontSize = 12,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Margin = new Avalonia.Thickness(2, 0)
                };
                try
                {
                    var fgBrush = this.FindResource("TextSecondary") as IBrush;
                    if (fgBrush != null) ellipsis.Foreground = fgBrush;
                }
                catch { ellipsis.Foreground = TextSecondaryBrush; }
                panel.Children.Add(ellipsis);
            }
            else
            {
                var pageNum = p;
                var btn = CreatePageButton(p.ToString(), true, () => onPageChanged(pageNum));
                if (p == currentPage)
                {
                    btn.Classes.Add("ActivePage");
                }
                panel.Children.Add(btn);
            }
        }

        // Next button — only shown when not on last page
        if (currentPage < totalPages)
        {
            var nextBtn = CreatePageButton("▶", true, () => onPageChanged(currentPage + 1));
            panel.Children.Add(nextBtn);
        }
    }

    /// <summary>
    /// Creates a styled pagination button.
    /// </summary>
    private Button CreatePageButton(string text, bool isEnabled, Action onClick)
    {
        var btn = new Button
        {
            Content = text,
            FontSize = 11,
            Padding = new Avalonia.Thickness(8, 4),
            MinWidth = 30,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            IsEnabled = isEnabled,
            Cursor = isEnabled ? new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) : null,
            Background = Brushes.Transparent,
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(4)
        };

        try
        {
            var borderBrush = this.FindResource("ClickableCardBorder") as IBrush;
            var fgBrush = this.FindResource("TextSecondary") as IBrush;
            if (borderBrush != null) btn.BorderBrush = borderBrush;
            if (fgBrush != null) btn.Foreground = fgBrush;
        }
        catch
        {
            btn.BorderBrush = CardBorderBrush;
            btn.Foreground = TextSecondaryBrush;
        }

        btn.Click += (_, _) => onClick();
        return btn;
    }

    /// <summary>
    /// Generates page number list with ellipsis (-1) for large page counts.
    /// Shows a window of pages around the current page.
    /// </summary>
    private static List<int> GetPageNumbers(int current, int total)
    {
        var pages = new List<int>();
        if (total <= 7)
        {
            for (int i = 1; i <= total; i++) pages.Add(i);
            return pages;
        }

        // Always show first page
        pages.Add(1);

        int start = Math.Max(2, current - 1);
        int end = Math.Min(total - 1, current + 1);

        // Adjust range near boundaries
        if (current <= 3) end = Math.Min(4, total - 1);
        if (current >= total - 2) start = Math.Max(total - 3, 2);

        if (start > 2) pages.Add(-1); // ellipsis

        for (int i = start; i <= end; i++) pages.Add(i);

        if (end < total - 1) pages.Add(-1); // ellipsis

        // Always show last page
        pages.Add(total);

        return pages;
    }

    /// <summary>
    /// Handles page jump for Journal via Enter key in the jump TextBox.
    /// </summary>
    private void JournalPageJumpBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        JumpToJournalPage();
    }

    /// <summary>
    /// Handles page jump for Journal via OK button click.
    /// </summary>
    private void JournalPageJump_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        JumpToJournalPage();
    }

    private void JumpToJournalPage()
    {
        var jumpBox = this.FindControl<TextBox>("JournalPageJumpBox");
        if (jumpBox == null) return;

        var sourceItems = _journalFilteredDateItems ?? _journalAllDateItems;
        var totalPages = Math.Max(1, (int)Math.Ceiling(sourceItems.Count / (double)ItemsPerPage));
        if (int.TryParse(jumpBox.Text?.Trim(), out int page) && page >= 1 && page <= totalPages)
        {
            _journalCurrentPage = page;
            ApplyJournalPagination();
            jumpBox.Text = "";
        }
    }

    /// <summary>
    /// Handles page jump for Etat via Enter key in the jump TextBox.
    /// </summary>
    private void EtatPageJumpBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        JumpToEtatPage();
    }

    /// <summary>
    /// Handles page jump for Etat via OK button click.
    /// </summary>
    private void EtatPageJump_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        JumpToEtatPage();
    }

    private void JumpToEtatPage()
    {
        var jumpBox = this.FindControl<TextBox>("EtatPageJumpBox");
        if (jumpBox == null) return;

        var totalPages = Math.Max(1, (int)Math.Ceiling(_etatAllJobItems.Count / (double)ItemsPerPage));
        if (int.TryParse(jumpBox.Text?.Trim(), out int page) && page >= 1 && page <= totalPages)
        {
            _etatCurrentPage = page;
            ApplyEtatPagination();
            jumpBox.Text = "";
        }
    }

    #endregion
}
