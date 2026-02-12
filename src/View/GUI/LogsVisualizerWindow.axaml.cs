using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using EasySave.ViewModels;

namespace EasySave.View.GUI
{
    internal partial class LogsVisualizerWindow : Window
    {
        public LogsVisualizerWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles click on Journalier tab
        /// </summary>
        private void JournalierTab_Clicked(object? sender, PointerPressedEventArgs e)
        {
            var journalierBorder = this.FindControl<Border>("JournalierTabBorder");
            var etatBorder = this.FindControl<Border>("EtatTabBorder");
            var journalierText = this.FindControl<TextBlock>("JournalierTabText");
            var etatText = this.FindControl<TextBlock>("EtatTabText");
            var journalierContent = this.FindControl<Grid>("JournalierContent");
            var etatContent = this.FindControl<Grid>("EtatContent");

            if (journalierBorder != null && etatBorder != null && journalierContent != null && etatContent != null)
            {
                // Update tab styles
                journalierBorder.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                journalierBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                journalierBorder.BorderThickness = new Avalonia.Thickness(0, 0, 0, 3);
                
                etatBorder.Background = Brushes.Transparent;
                etatBorder.BorderThickness = new Avalonia.Thickness(0);

                // Update text styles
                if (journalierText != null)
                {
                    journalierText.FontWeight = Avalonia.Media.FontWeight.Bold;
                    journalierText.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                }
                
                if (etatText != null)
                {
                    etatText.FontWeight = Avalonia.Media.FontWeight.Normal;
                    etatText.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
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
            var journalierText = this.FindControl<TextBlock>("JournalierTabText");
            var etatText = this.FindControl<TextBlock>("EtatTabText");
            var journalierContent = this.FindControl<Grid>("JournalierContent");
            var etatContent = this.FindControl<Grid>("EtatContent");

            if (journalierBorder != null && etatBorder != null && journalierContent != null && etatContent != null)
            {
                // Update tab styles
                etatBorder.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                etatBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                etatBorder.BorderThickness = new Avalonia.Thickness(0, 0, 0, 3);
                
                journalierBorder.Background = Brushes.Transparent;
                journalierBorder.BorderThickness = new Avalonia.Thickness(0);

                // Update text styles
                if (etatText != null)
                {
                    etatText.FontWeight = Avalonia.Media.FontWeight.Bold;
                    etatText.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                }
                
                if (journalierText != null)
                {
                    journalierText.FontWeight = Avalonia.Media.FontWeight.Normal;
                    journalierText.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
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
                            Background = Brushes.WhiteSmoke,
                            Padding = new Avalonia.Thickness(15),
                            BorderBrush = Brushes.LightGray,
                            BorderThickness = new Avalonia.Thickness(1),
                            CornerRadius = new Avalonia.CornerRadius(8)
                        };

                        var jsonText = new TextBlock
                        {
                            FontFamily = new Avalonia.Media.FontFamily("Consolas"),
                            FontSize = 13,
                            Foreground = Brushes.Black,
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
    }
}
