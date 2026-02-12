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
    /// Returns the path to the EasySave state.json file in AppData
    /// </summary>
    private static string GetStateFilePath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasySave", "state.json");
    }

    /// <summary>
    /// Loads job entries from state.json and populates the Etat job list dynamically
    /// </summary>
    private void LoadStateJobs()
    {
        var panel = this.FindControl<StackPanel>("EtatJobListPanel");
        if (panel == null) return;

        panel.Children.Clear();

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

                var stack = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Height = 40
                };
                stack.Children.Add(bullet);
                stack.Children.Add(nameText);

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

                panel.Children.Add(border);
            }
        }
        catch { /* state.json might be malformed or locked */ }
    }

    /// <summary>
    /// Scans the EasySave logs directory and populates the date list dynamically
    /// </summary>
    private void LoadLogDates()
    {
        var dateListPanel = this.FindControl<StackPanel>("DateListPanel");
        if (dateListPanel == null) return;

        dateListPanel.Children.Clear();

        var logsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasySave", "logs");

        if (!Directory.Exists(logsDir))
        {
            var noLogsText = new TextBlock
            {
                Text = "Aucun fichier log trouv\u00e9",
                FontSize = 14,
                Foreground = TextSecondaryBrush,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 20, 0, 0)
            };
            dateListPanel.Children.Add(noLogsText);
            return;
        }

        var logFiles = Directory.GetFiles(logsDir, "jobs_*.json")
            .OrderByDescending(f => f)
            .ToList();

        if (logFiles.Count == 0)
        {
            var noLogsText = new TextBlock
            {
                Text = "Aucun fichier log trouv\u00e9",
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

            var stack = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Height = 40
            };
            stack.Children.Add(bullet);
            stack.Children.Add(dateText);

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

            dateListPanel.Children.Add(border);
        }
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
                jsonContent = $"Erreur de lecture : {ex.Message}";
            }

            _currentJsonContent = jsonContent;
            _currentJsonFilePath = filePath;

            // Show copy/download buttons
            var copyBtn = this.FindControl<Button>("CopyJsonButton");
            var dlBtn = this.FindControl<Button>("DownloadJsonButton");
            if (copyBtn != null) copyBtn.IsVisible = true;
            if (dlBtn != null) dlBtn.IsVisible = true;

            var jsonGrid = this.FindControl<Grid>("JournalJsonGrid");
            if (jsonGrid != null)
            {
                if (jsonGrid.Children.Count > 1)
                {
                    jsonGrid.Children.RemoveAt(1);
                }

                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
                };

                var jsonText = new TextBox
                {
                    FontFamily = new Avalonia.Media.FontFamily("Consolas"),
                    FontSize = 13,
                    Padding = new Avalonia.Thickness(15),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    IsReadOnly = true,
                    AcceptsReturn = true,
                    Text = jsonContent
                };
                jsonText.Classes.Add("JsonViewer");

                scrollViewer.Content = jsonText;
                Grid.SetRow(scrollViewer, 1);
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
                btn.Content = "\u2705 Copi\u00e9 !";
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

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "Enregistrer le fichier JSON",
            SuggestedFileName = defaultName,
            FileTypeChoices = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("JSON") { Patterns = new[] { "*.json" } }
            }
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
                    // Remove the placeholder and add the JSON content
                    if (jsonGrid.Children.Count > 1)
                    {
                        jsonGrid.Children.RemoveAt(1);
                    }

                    var scrollViewer = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
                    };

                    var jsonText = new TextBox
                    {
                        FontFamily = new Avalonia.Media.FontFamily("Consolas"),
                        FontSize = 13,
                        Padding = new Avalonia.Thickness(15),
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        IsReadOnly = true,
                        AcceptsReturn = true,
                        Text = jobJson
                    };
                    jsonText.Classes.Add("JsonViewer");

                    scrollViewer.Content = jsonText;
                    Grid.SetRow(scrollViewer, 1);
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
                btn.Content = "\u2705 Copi\u00e9 !";
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
            Title = "Enregistrer le fichier JSON",
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
}
