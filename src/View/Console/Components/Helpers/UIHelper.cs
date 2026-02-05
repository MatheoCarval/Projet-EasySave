using System;
using Terminal.Gui;

namespace EasySave.View.Console.Helpers;

/// <summary>
/// Provides utility methods for creating and positioning common Terminal.Gui UI components with consistent styling and layout.
/// </summary>
public static class UIHelper
{
    /// <summary>
    /// Creates a centered label with optional vertical offset from center position.
    /// </summary>
    public static Label CreateLabel(string text, int yOffset = 0)
    {
        return new Label(text)
        {
            X = Pos.Center() - (text.Length / 2),
            Y = Pos.Center() + yOffset
        };
    }

    /// <summary>
    /// Creates a label positioned at the specified coordinates.
    /// </summary>
    public static Label CreateLabelAt(string text, int x, int y)
    {
        return new Label(text)
        {
            X = x,
            Y = y
        };
    }

    /// <summary>
    /// Creates a text input field at the specified position with optional initial text and configurable width.
    /// </summary>
    public static TextField CreateTextField(string initialText, int x, int y, int width = 40)
    {
        return new TextField(initialText)
        {
            X = x,
            Y = y,
            Width = width,
            Height = 1
        };
    }

    /// <summary>
    /// Creates a centered text input field with optional vertical offset and configurable width.
    /// </summary>
    public static TextField CreateCenteredTextField(string initialText, int yOffset = 0, int width = 40)
    {
        return new TextField(initialText)
        {
            X = Pos.Center() - (width / 2),
            Y = Pos.Center() + yOffset,
            Width = width,
            Height = 1
        };
    }

    /// <summary>
    /// Creates a button at the specified position with optional click handler and default state.
    /// </summary>
    public static Button CreateButton(string text, int x, int y, Action? onClick = null, bool isDefault = false)
    {
        var button = new Button(text)
        {
            X = x,
            Y = y,
            IsDefault = isDefault
        };

        if (onClick != null)
        {
            button.Clicked += onClick;
        }

        return button;
    }

    /// <summary>
    /// Creates a centered button with optional vertical offset, click handler, and default state.
    /// </summary>
    public static Button CreateCenteredButton(string text, int yOffset = 0, Action? onClick = null, bool isDefault = false)
    {
        var button = new Button(text)
        {
            X = Pos.Center() - (text.Length / 2),
            Y = Pos.Center() + yOffset,
            IsDefault = isDefault
        };

        if (onClick != null)
        {
            button.Clicked += onClick;
        }

        return button;
    }

    /// <summary>
    /// Creates a list view at the specified position with given dimensions and populated with items.
    /// </summary>
    public static ListView CreateListView(System.Collections.Generic.List<string> items, int x, int y, int width, int height)
    {
        return new ListView(items)
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            AllowsMarking = false,
            CanFocus = true
        };
    }

    /// <summary>
    /// Creates a centered list view with optional vertical offset and configurable dimensions.
    /// </summary>
    public static ListView CreateCenteredListView(System.Collections.Generic.List<string> items, int yOffset = 0, int width = 40, int height = 10)
    {
        return new ListView(items)
        {
            X = Pos.Center() - (width / 2),
            Y = Pos.Center() + yOffset,
            Width = width,
            Height = height,
            AllowsMarking = false,
            CanFocus = true
        };
    }

    /// <summary>
    /// Converts a byte count into a human-readable string with appropriate unit (B, KB, MB, GB, TB).
    /// </summary>
    public static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Formats a DateTime value into a human-readable string with year, month, day, hour, minute, and second.
    /// </summary>
    public static string FormatTimestamp(DateTime timestamp)
    {
        return timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    }
}