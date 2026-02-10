using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using EasySave.ViewModels;
using System.Linq;

namespace EasySave.View.GUI;

/// <summary>
/// Main window of the EasySave application
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the MainWindow class
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(App.BackupManager!);
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
}
