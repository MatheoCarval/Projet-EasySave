using System;

namespace EasySave.Exceptions
{
    /// <summary>
    /// Exception thrown when a file transfer operation fails during backup execution.
    /// </summary>
    public class FileTransferException : Exception
    {
        /// <summary>
        /// Initializes FileTransferException with no message.
        /// </summary>
        public FileTransferException() : base()
        {
        }

        /// <summary>
        /// Initializes FileTransferException with a descriptive error message.
        /// </summary>
        public FileTransferException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes FileTransferException with a descriptive error message and the underlying exception that caused the failure.
        /// </summary>
        public FileTransferException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
