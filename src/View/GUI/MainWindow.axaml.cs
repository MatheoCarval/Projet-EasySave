using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Animation;
using Avalonia.Threading;
using EasySave.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

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
    /// Handles click on Journalier tab
    /// </summary>
    private void JournalierTab_Clicked(object? sender, PointerPressedEventArgs e)
    {
        var journalierBorder = this.FindControl<Border>("JournalierTabBorder");
        var etatBorder = this.FindControl<Border>("EtatTabBorder");
        var journalierContent = this.FindControl<Grid>("JournalierContent");
        var etatContent = this.FindControl<Grid>("EtatContent");

        if (journalierBorder != null && etatBorder != null && journalierContent != null && etatContent != null)
        {
            // Update tab styles
            journalierBorder.Background = this.FindResource("AppBackground") as IBrush;
            journalierBorder.BorderBrush = this.FindResource("AccentBlue") as IBrush;
            journalierBorder.BorderThickness = new Avalonia.Thickness(0, 0, 0, 3);
            
            etatBorder.Background = Brushes.Transparent;
            etatBorder.BorderThickness = new Avalonia.Thickness(0);

            // Update text styles
            if (journalierBorder.Child is TextBlock journalierText)
            {
                journalierText.FontWeight = Avalonia.Media.FontWeight.Bold;
                journalierText.Foreground = this.FindResource("TextPrimary") as IBrush;
            }
            
            if (etatBorder.Child is TextBlock etatText)
            {
                etatText.FontWeight = Avalonia.Media.FontWeight.Normal;
                etatText.Foreground = this.FindResource("TextSecondary") as IBrush;
            }

            // Show/hide content
            journalierContent.IsVisible = true;
            etatContent.IsVisible = false;
        }
    }

    /// <summary>
    /// Handles click on Etat tab
    /// </summary>
    private void EtatTab_Clicked(object? sender, PointerPressedEventArgs e)
    {
        var journalierBorder = this.FindControl<Border>("JournalierTabBorder");
        var etatBorder = this.FindControl<Border>("EtatTabBorder");
        var journalierContent = this.FindControl<Grid>("JournalierContent");
        var etatContent = this.FindControl<Grid>("EtatContent");

        if (journalierBorder != null && etatBorder != null && journalierContent != null && etatContent != null)
        {
            // Update tab styles
            etatBorder.Background = this.FindResource("AppBackground") as IBrush;
            etatBorder.BorderBrush = this.FindResource("AccentBlue") as IBrush;
            etatBorder.BorderThickness = new Avalonia.Thickness(0, 0, 0, 3);
            
            journalierBorder.Background = Brushes.Transparent;
            journalierBorder.BorderThickness = new Avalonia.Thickness(0);

            // Update text styles
            if (etatBorder.Child is TextBlock etatText)
            {
                etatText.FontWeight = Avalonia.Media.FontWeight.Bold;
                etatText.Foreground = this.FindResource("TextPrimary") as IBrush;
            }
            
            if (journalierBorder.Child is TextBlock journalierText)
            {
                journalierText.FontWeight = Avalonia.Media.FontWeight.Normal;
                journalierText.Foreground = this.FindResource("TextSecondary") as IBrush;
            }

            // Show/hide content
            journalierContent.IsVisible = false;
            etatContent.IsVisible = true;
        }
    }

    /// <summary>
    /// Handles click on a job item in Etat tab to display its JSON
    /// </summary>
    private void JobItem_Clicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is string jobName)
        {
            // Define the JSON for each job
            var jobJsons = new Dictionary<string, string>
            {
                ["test"] = @"{
  ""jobName"": ""test"",
  ""timestamp"": ""2026-02-10T14:57:39.9197699Z"",
  ""state"": 2,
  ""totalFiles"": 5,
  ""totalSize"": 402251,
  ""progress"": 100,
  ""remainingFiles"": 0,
  ""remainingSize"": 0,
  ""currentSourceFile"": ""\\\\localhost\\C$\\Users\\feita\\Documents\\test\\WebSite1\\w-brand.png"",
  ""currentTargetFile"": ""\\\\localhost\\C$\\Users\\feita\\Documents\\test2\\WebSite1\\w-brand.png""
}",
                ["TEST MID"] = @"{
  ""jobName"": ""TEST MID"",
  ""timestamp"": ""2026-02-10T13:53:10.5556231Z"",
  ""state"": 2,
  ""totalFiles"": 155,
  ""totalSize"": 13104971,
  ""progress"": 100,
  ""remainingFiles"": 0,
  ""remainingSize"": 0,
  ""currentSourceFile"": ""\\\\localhost\\C$\\Users\\feita\\Pictures\\Screenshots\\Screenshot 2026-02-10 144656.png"",
  ""currentTargetFile"": ""\\\\localhost\\C$\\Users\\feita\\Pictures\\Dupli\\Screenshot 2026-02-10 144656.png""
}",
                ["tessthhhhhdqqsdqsdqsdqsd"] = @"{
  ""jobName"": ""tessthhhhhdqqsdqsdqsdqsd"",
  ""timestamp"": ""2026-02-10T10:48:34.7043449Z"",
  ""state"": 3,
  ""totalFiles"": 0,
  ""totalSize"": 0,
  ""progress"": 0,
  ""remainingFiles"": 0,
  ""remainingSize"": 0,
  ""currentSourceFile"": """",
  ""currentTargetFile"": """"
}",
                ["qdhbh"] = @"{
  ""jobName"": ""qdhbh"",
  ""timestamp"": ""2026-02-10T13:51:47.8372417Z"",
  ""state"": 3,
  ""totalFiles"": 0,
  ""totalSize"": 0,
  ""progress"": 0,
  ""remainingFiles"": 0,
  ""remainingSize"": 0,
  ""currentSourceFile"": """",
  ""currentTargetFile"": """"
}"
            };

            if (jobJsons.ContainsKey(jobName))
            {
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

                    var jsonContentBorder = new Border
                    {
                        Background = this.FindResource("AppBackground") as IBrush,
                        Padding = new Avalonia.Thickness(15),
                        BorderBrush = this.FindResource("ClickableCardBorder") as IBrush,
                        BorderThickness = new Avalonia.Thickness(1),
                        CornerRadius = new Avalonia.CornerRadius(8)
                    };

                    var jsonText = new TextBlock
                    {
                        FontFamily = new Avalonia.Media.FontFamily("Consolas"),
                        FontSize = 13,
                        Foreground = this.FindResource("TextPrimary") as IBrush,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Text = jobJsons[jobName]
                    };

                    jsonContentBorder.Child = jsonText;
                    scrollViewer.Content = jsonContentBorder;
                    Grid.SetRow(scrollViewer, 1);
                    jsonGrid.Children.Add(scrollViewer);
                }
            }
        }
    }

    #endregion
}
