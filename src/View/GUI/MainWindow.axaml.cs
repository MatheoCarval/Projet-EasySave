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
                // Store only the directory path - daily log files will be created automatically
                mainVm.SettingsVM.LogFilePath = result[0].Path.LocalPath;
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

    /// <summary>
    /// Handles the Browse button click for Cryptosoft executable path (Settings)
    /// </summary>
    private async void BrowseCryptosoftPath_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel mainVm)
        {
            var fileTypes = new System.Collections.Generic.List<FilePickerFileType>();

            if (OperatingSystem.IsWindows())
            {
                fileTypes.Add(new FilePickerFileType("Executables") { Patterns = new[] { "*.exe" } });
            }
            else
            {
                fileTypes.Add(new FilePickerFileType("All files") { Patterns = new[] { "*" } });
            }

            var options = new FilePickerOpenOptions
            {
                Title = OperatingSystem.IsWindows()
                    ? "Select Cryptosoft executable (.exe)"
                    : "Select Cryptosoft executable",
                AllowMultiple = false,
                FileTypeFilter = fileTypes
            };

            var result = await StorageProvider.OpenFilePickerAsync(options);

            if (result.Count > 0)
            {
                mainVm.SettingsVM.CryptosoftPath = result[0].Path.LocalPath;
            }
        }
    }

    /// <summary>
    /// Handles the Browse button click to add a process name from an executable file.
    /// Cross-platform: .exe on Windows, .app bundles or any file on macOS, any file on Linux.
    /// </summary>
    private async void BrowseExe_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel mainVm)
        {
            var fileTypes = new System.Collections.Generic.List<FilePickerFileType>();

            if (OperatingSystem.IsWindows())
            {
                fileTypes.Add(new FilePickerFileType("Executables") { Patterns = new[] { "*.exe" } });
            }
            else if (OperatingSystem.IsMacOS())
            {
                fileTypes.Add(new FilePickerFileType("Applications") { Patterns = new[] { "*.app", "*" } });
            }
            else
            {
                // Linux: executables have no extension
                fileTypes.Add(new FilePickerFileType("All files") { Patterns = new[] { "*" } });
            }

            var options = new FilePickerOpenOptions
            {
                Title = OperatingSystem.IsWindows()
                    ? "Select an application (.exe)"
                    : "Select an application",
                AllowMultiple = false,
                FileTypeFilter = fileTypes
            };

            var result = await StorageProvider.OpenFilePickerAsync(options);

            if (result.Count > 0)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(result[0].Name);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    mainVm.SettingsVM.AddBlockedApplication(fileName);
                }
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
}
