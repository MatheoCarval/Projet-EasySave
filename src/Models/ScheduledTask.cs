using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace EasySave.Models;

/// <summary>
/// Represents a one-time scheduled backup task: a specific job at a specific date and time.
/// </summary>
public class ScheduledTask : INotifyPropertyChanged
{
    private string _backupJobId = string.Empty;
    private string _backupJobName = string.Empty;
    private DateTime _scheduledDateTime;
    private bool _isEnabled = true;

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string BackupJobId
    {
        get => _backupJobId;
        set { _backupJobId = value; OnPropertyChanged(); }
    }

    public string BackupJobName
    {
        get => _backupJobName;
        set { _backupJobName = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The exact date and time this backup should run (serialized to JSON).
    /// </summary>
    public DateTime ScheduledDateTime
    {
        get => _scheduledDateTime;
        set
        {
            _scheduledDateTime = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ScheduledDate));
            OnPropertyChanged(nameof(SelectedTime));
            OnPropertyChanged(nameof(ScheduleDescription));
            OnPropertyChanged(nameof(IsOverdue));
        }
    }

    /// <summary>
    /// DatePicker binding — DateTimeOffset? for Avalonia DatePicker.SelectedDate
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset? ScheduledDate
    {
        get => new DateTimeOffset(_scheduledDateTime.Date);
        set
        {
            if (value.HasValue)
            {
                var date = value.Value.Date;
                _scheduledDateTime = new DateTime(date.Year, date.Month, date.Day,
                    _scheduledDateTime.Hour, _scheduledDateTime.Minute, 0);
                OnPropertyChanged();
                OnPropertyChanged(nameof(ScheduledDateTime));
                OnPropertyChanged(nameof(ScheduleDescription));
                OnPropertyChanged(nameof(IsOverdue));
            }
        }
    }

    /// <summary>
    /// TimePicker binding — TimeSpan? for Avalonia TimePicker.SelectedTime
    /// </summary>
    [JsonIgnore]
    public TimeSpan? SelectedTime
    {
        get => new TimeSpan(_scheduledDateTime.Hour, _scheduledDateTime.Minute, 0);
        set
        {
            if (value.HasValue)
            {
                _scheduledDateTime = new DateTime(
                    _scheduledDateTime.Year, _scheduledDateTime.Month, _scheduledDateTime.Day,
                    value.Value.Hours, value.Value.Minutes, 0);
                OnPropertyChanged();
                OnPropertyChanged(nameof(ScheduledDateTime));
                OnPropertyChanged(nameof(ScheduleDescription));
            }
        }
    }

    /// <summary>
    /// Human-readable description shown in the card header.
    /// </summary>
    [JsonIgnore]
    public string ScheduleDescription =>
        $"{_scheduledDateTime:dddd d MMMM yyyy} — {_scheduledDateTime:HH:mm}";

    /// <summary>
    /// True if the scheduled date/time is in the past.
    /// </summary>
    [JsonIgnore]
    public bool IsOverdue => IsEnabled && _scheduledDateTime < DateTime.Now;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOverdue));
        }
    }

    public DateTime? LastRun { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
