using Avalonia.Controls;
using Avalonia.Input;
using EasySave.ViewModels;

namespace EasySave.View.GUI
{
    /// <summary>
    /// Window that displays backup logs and state information in a tabbed interface.
    /// Follows the same pattern as MainWindow: the ViewModel (LogsVisualizerViewModel)
    /// holds all data and business logic; the code-behind only handles UI-specific event routing.
    /// </summary>
    internal partial class LogsVisualizerWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the LogsVisualizerWindow class
        /// and sets its DataContext to the LogsVisualizerViewModel.
        /// </summary>
        public LogsVisualizerWindow()
        {
            InitializeComponent();
            DataContext = new LogsVisualizerViewModel();
        }

        /// <summary>
        /// Handles click on a job item in the État tab — delegates to the ViewModel to load JSON.
        /// Same pattern as BackupCard_Tapped in MainWindow.axaml.cs.
        /// </summary>
        private void JobItem_Clicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border
                && border.DataContext is StateJobDisplay job
                && DataContext is LogsVisualizerViewModel vm)
            {
                vm.SelectJob(job.JobName);
            }
        }
    }
}
