using System.Net;

namespace Core;

public interface ITcpServer : IDisposable
{
    IPEndPoint IPEndPoint { get; }
    bool IsListening { get; }

    Task StartAsync();
}
