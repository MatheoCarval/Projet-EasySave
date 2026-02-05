using System;

namespace EasySave.Exceptions
{
    /// <summary>
    /// Exception thrown when file transfer operations fail
    /// </summary>
    public class FileTransferException : Exception
    {
        public FileTransferException() : base()
        {
        }

        public FileTransferException(string message) : base(message)
        {
        }

        public FileTransferException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
