using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.ViewModels
{
    /// <summary>
    ///  To show the backup logs in the LogsView, this class is used to bind the data to the view.
    /// </summary>
    internal class LogsVisualizerViewModel : ViewModelBase
    {
        /// <summary>
        /// Gets the logs content in JSON format to display in the LogsView. This is a placeholder implementation and should be replaced with actual logic to read logs from a file or other source.
        /// </summary>
        /// <returns></returns>
        public string getLogsJsonContent()
        {
            return "";
        }

        private string logText = "";

        /// <summary>
        /// Gets or sets the log text content displayed in the logs viewer
        /// </summary>
        public string LogText
        {
            get { return logText ?? ""; }
            set
            {
                logText = value;
                OnPropertyChanged(nameof(LogText));
            }
        }
    }
}
