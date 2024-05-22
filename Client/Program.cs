using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Core;

#if DEBUG
args = ["127.0.0.1"];
#endif

IPAddress iPAddress;
if (args.Length > 0)
{
    string[] addrs = args[0].Split('.');
    byte[] bytes = new byte[4];
    for (int i = 0; i < bytes.Length; i++)
    {
        bytes[i] = BitConverter.GetBytes(Convert.ToInt32(addrs[i]))[0];
    }
    iPAddress = new IPAddress(bytes);
}
else
{
    iPAddress = IPAddress.Loopback;
}

ArrayPoolMemoryProvider _memoryProvider = new();
CancellationTokenSource _cancellationTokenSource = new();

using TcpClient tcpClient = new();

await tcpClient.ConnectAsync(iPAddress, 50505);

await using NetworkStream stream = tcpClient.GetStream();

// 线程中接收消息
var receiveTask = Task.Run(async () =>
{
    await ReveiveMessage(tcpClient, _cancellationTokenSource.Token);
});

// 循环输入消息
while (!_cancellationTokenSource.Token.IsCancellationRequested)
{
    string input = Console.ReadLine() ?? string.Empty;
    if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
    {
        _cancellationTokenSource.Cancel();
    }

    // 输入时检查 猜拳结果
    if (Fight.IsRockPaperScissors(input, out SPC spc))
    {
        Fight.SetLocalInput(input);
        if (Fight.GetResult(out SPCResult? result))
        {
            Console.WriteLine($"{result}");
        }
    }

    Protocol protocol = new();
    var messageBuffer = protocol.CreatMessage(input);

    await stream.WriteAsync(messageBuffer);
}

Console.WriteLine("结束消息输入循环, 退出客户端");

async Task ReveiveMessage(TcpClient acceptTcpClient, CancellationToken cancellationToken)
{
    Stream stream = acceptTcpClient.GetStream();

    EndPoint remoteEndPoint = acceptTcpClient.Client.RemoteEndPoint!;

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
                continue;
                //Console.WriteLine("Data1 length is 0");
                //acceptTcpClient.Close();
                //acceptTcpClient.Dispose();
                //return null;
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
                continue;
                //Console.WriteLine("Data2 length is 0");
                //acceptTcpClient.Close();
                //acceptTcpClient.Dispose();
                //return;
            }

            rawOffset += count;
            _bytesToRead -= count;
        }

        string message = Encoding.UTF8.GetString(rawBuffer.Bytes, Protocol.HearderLength, dataLength);
        // 接收消息时检查猜拳结果
        if (Fight.IsRockPaperScissors(message, out SPC spc))
        {
            _ = Fight.SetRemoteInput(message);
            Console.WriteLine("对方已准备");
            if (Fight.GetResult(out SPCResult? result))
            {
                Console.WriteLine($"{result}");
            }
        }
        else
        {
            Console.WriteLine($"服务端消息: {message}");
        }
    }

    stream.Close();
    stream.Dispose();

    acceptTcpClient.Close();
    acceptTcpClient.Dispose();
    Debug.WriteLine("关闭tcpclient");
}
