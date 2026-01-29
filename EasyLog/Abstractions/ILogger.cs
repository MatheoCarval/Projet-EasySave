namespace EasyLog.Abstractions;

public interface ILogger
{
    void Log<T>(T data) where T : class;

    void LogCollection<T>(IEnumerable<T> data) where T : class;

    void Flush();

    IEnumerable<T> ReadLog<T>() where T : class;
}