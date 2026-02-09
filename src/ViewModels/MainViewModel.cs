using System;
using System.Windows.Input;

namespace EasySave.ViewModels;

/// <summary>
/// Main ViewModel for the application
/// </summary>
public class MainViewModel : ViewModelBase
{
    private string _welcomeMessage = "This is a minimal Avalonia GUI setup for EasySave. Start building your backup management interface here!";

    /// <summary>
    /// Gets or sets the welcome message
    /// </summary>
    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set => SetProperty(ref _welcomeMessage, value);
    }

    /// <summary>
    /// Command to handle the Get Started button click
    /// </summary>
    public ICommand GetStartedCommand { get; }

    /// <summary>
    /// Initializes a new instance of the MainViewModel class
    /// </summary>
    public MainViewModel()
    {
        GetStartedCommand = new RelayCommand(OnGetStarted);
    }

    /// <summary>
    /// Handles the Get Started button click
    /// </summary>
    private void OnGetStarted()
    {
        WelcomeMessage = "Button clicked! You can now start implementing your backup features.";
    }
}

/// <summary>
/// Simple ICommand implementation for button commands
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// Initializes a new instance of the RelayCommand class
    /// </summary>
    /// <param name="execute">Action to execute</param>
    /// <param name="canExecute">Function to determine if command can execute</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Event that is raised when CanExecute changes
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Determines if the command can execute
    /// </summary>
    /// <param name="parameter">Command parameter</param>
    /// <returns>True if command can execute, false otherwise</returns>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <summary>
    /// Executes the command
    /// </summary>
    /// <param name="parameter">Command parameter</param>
    public void Execute(object? parameter) => _execute();

    /// <summary>
    /// Raises the CanExecuteChanged event
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
