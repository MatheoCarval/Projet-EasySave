namespace EasyLog.Abstractions;

public interface ILogFormatter<T>
{
    string Format(T data);

    string FormatCollection(IEnumerable<T> data);

    T Parse(string content);

    IEnumerable<T> ParseCollection(string content);
}
