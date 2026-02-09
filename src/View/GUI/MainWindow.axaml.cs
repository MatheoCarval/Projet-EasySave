using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EasySave.ViewModels;

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
        DataContext = new MainViewModel();
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
}
