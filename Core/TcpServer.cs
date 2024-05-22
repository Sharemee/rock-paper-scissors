using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Core;

public class TcpServer : ITcpServer
{
    public CancellationToken CancellationToken { get; }

    public IPEndPoint IPEndPoint { get; private set; }
    public bool IsListening { get; private set; }

    private readonly Dictionary<string, TcpClient> _clients;
    private readonly CancellationTokenSource? _tokenSource;
    private readonly TcpListener _listener;
    private readonly ArrayPoolMemoryProvider _memoryProvider;

    public delegate void AcceptTcpClientDelegate(TcpClient tcpClient);
    public event AcceptTcpClientDelegate? OnAcceptTcpClient;
    public delegate void ReceiveMessageDelegate(IPEndPoint? endPoint, string message);
    public event ReceiveMessageDelegate? OnReceiveMessage;

    public TcpServer(IPEndPoint endPoint, CancellationToken? cancellationToken = null)
    {
        IPEndPoint = endPoint;

        IsListening = false;

        if (cancellationToken is null)
        {
            _tokenSource = new CancellationTokenSource();
        }
        CancellationToken = cancellationToken ?? _tokenSource!.Token;

        _clients = [];
        _listener = new TcpListener(endPoint);
        _memoryProvider = new ArrayPoolMemoryProvider();

        OnAcceptTcpClient += TcpServer_OnAcceptTcpClient;
    }

    private void TcpServer_OnAcceptTcpClient(TcpClient tcpClient)
    {
        if (tcpClient.Client.RemoteEndPoint is not IPEndPoint endPoint)
        {
            throw new InvalidOperationException("IPEndPoint is null");
        }
        _clients.Add(endPoint.ToString(), tcpClient);
    }

    public async Task StartAsync()
    {
        _listener.Start();

        while (!CancellationToken.IsCancellationRequested)
        {
            TcpClient acceptTcpClient = await _listener.AcceptTcpClientAsync();

            OnAcceptTcpClient?.Invoke(acceptTcpClient);

            // 扔到线程中处理, 不阻塞线程, 继续接收新的连接
            _ = ReveiveMessage(acceptTcpClient, CancellationToken);
        }
    }

    private async Task ReveiveMessage(TcpClient acceptTcpClient, CancellationToken cancellationToken)
    {
        Stream stream = acceptTcpClient.GetStream();

        IPEndPoint? remoteEndPoint = acceptTcpClient.Client.RemoteEndPoint as IPEndPoint;

        while (!cancellationToken.IsCancellationRequested)
        {
            // 根据自定义协议解析流
            int _bytesToRead = Protocol.HearderLength;

            using IMemory headerBuffer = _memoryProvider.Provide(Protocol.HearderLength);

            int count = await stream.ReadAsync(headerBuffer.Bytes, 0, headerBuffer.Length, cancellationToken).ConfigureAwait(false);

            do
            {
                if (count == 0)
                {
                    Console.WriteLine("Data1 length is 0");
                    acceptTcpClient.Close();
                    acceptTcpClient.Dispose();
                    return;
                }

                _bytesToRead -= count;

                if (_bytesToRead > 0)
                {
                    count = await stream.ReadAsync(headerBuffer.Bytes, headerBuffer.Length - _bytesToRead, _bytesToRead, cancellationToken).ConfigureAwait(false);
                }


            } while (_bytesToRead > 0);

            int dataLength = BitConverter.ToInt32(headerBuffer.Bytes);

            _bytesToRead = dataLength;

            int rawBufferLength = dataLength + Protocol.HearderLength;
            using IMemory rawBuffer = _memoryProvider.Provide(rawBufferLength);

            Array.Copy(headerBuffer.Bytes, 0, rawBuffer.Bytes, 0, Protocol.HearderLength);

            int rawOffset = Protocol.HearderLength;
            while (_bytesToRead > 0)
            {
                int bytesToRead = Math.Min(_bytesToRead, (int)Protocol.MaxByteToRead);

                count = await stream.ReadAsync(rawBuffer.Bytes, rawOffset, bytesToRead).ConfigureAwait(false);

                if (count == 0)
                {
                    Console.WriteLine("Data2 length is 0");
                    acceptTcpClient.Close();
                    acceptTcpClient.Dispose();
                    return;
                }

                rawOffset += count;
                _bytesToRead -= count;
            }

            string message = Encoding.UTF8.GetString(rawBuffer.Bytes, Protocol.HearderLength, dataLength);
            OnReceiveMessage?.Invoke(remoteEndPoint, message);
            //Console.WriteLine($"客户端消息 [{remoteEndPoint}]: {message}");
        }

        stream.Close();
        stream.Dispose();

        acceptTcpClient.Close();
        acceptTcpClient.Dispose();
        Debug.WriteLine("关闭tcpclient");
    }

    public async Task<bool> SendMessage(string key, string message)
    {
        if (_clients.TryGetValue(key, out TcpClient? client))
        {
            Stream stream = client.GetStream();

            Protocol protocol = new();
            ReadOnlyMemory<byte> messageBuffer = protocol.CreatMessage(message);

            await stream.WriteAsync(messageBuffer);
            return true;
        }
        return false;
    }

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: 释放托管状态(托管对象)
            }

            // TODO: 释放未托管的资源(未托管的对象)并重写终结器
            // TODO: 将大型字段设置为 null
            disposedValue = true;
        }
    }

    // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
    // ~TcpServer()
    // {
    //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
