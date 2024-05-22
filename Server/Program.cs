using System.Net;
using System.Net.Sockets;
using Core;
using Server;

Dictionary<string, TcpClient> clients = [];

IPEndPoint iPEndPoint = new(IPAddress.Any, 50505);
CancellationTokenSource tokenSource = new();
TcpServer tcpServer = new(iPEndPoint, tokenSource.Token);
tcpServer.OnAcceptTcpClient += tcpClient =>
{
    EndPoint endPoint = tcpClient.Client.RemoteEndPoint!;
    clients.Add(endPoint.ToString()!, tcpClient);
    Console.WriteLine("客户端已连接: " + tcpClient.Client.RemoteEndPoint);
};

tcpServer.OnReceiveMessage += (remoteEndPoint, message) =>
{
    if (Fight.IsRockPaperScissors(message, out SPC spc))
    {
        _ = Fight.SetRemoteInput(message);
        //Console.Title = message;
        Console.WriteLine("对方已准备");
        if (Fight.GetResult(out SPCResult? result))
        {
            Console.WriteLine($"{result}");
        }
    }
    else
    {
        Console.WriteLine($"客户端消息 {remoteEndPoint}: {message}");
    }
};

Task tcpServerTask = tcpServer.StartAsync();

Task inputTask = Task.Run(async () =>
{
    string command;
    while ((command = Console.ReadLine()!) != null)
    {
        var cmd = Ana(command);
        if (cmd.IsValid)
        {
            switch (cmd.Code)
            {
                case "/help":
                    await Console.Out.WriteLineAsync("/help\n/exit\n/list\n/send");
                    break;
                case "/list":
                    foreach (var client in clients)
                    {
                        Console.WriteLine(client.Key);
                    }
                    break;
                case "/send":
                    if (cmd.Args.Count >= 2)
                    {
                        string key = cmd.Args[0];
                        for (int i = 1; i < cmd.Args.Count; i++)
                        {
                            await tcpServer.SendMessage(key, cmd.Args[i]);
                        }
                    }
                    else
                    {
                        Console.WriteLine("参数1->客户端IP:Port; 参数2->发送的消息字符串");
                    }
                    break;
                case "/exit":
                    tokenSource.Cancel();
                    break;
                default:
                    Console.WriteLine($"指令有误: {cmd.Code}");
                    break;
            }
        }
        else
        {
            Fight.SetLocalInput(command);
            //foreach (var client in clients)
            {
                await tcpServer.SendMessage(clients.First().Key, command);
            }
            if (Fight.GetResult(out SPCResult? result))
            {
                await Console.Out.WriteLineAsync($"{result}");
            }
        }
    }
});

Task.WaitAll(tcpServerTask, inputTask);
Console.WriteLine("所有线程结束");

Command Ana(string command)
{
    command = command.Trim();

    Command result = new();
    if (!command.StartsWith('/')) return result;

    result.IsValid = true;

    int index = command.IndexOf(' ');
    if (index == -1)
    {
        result.Code = command;
    }
    else
    {
        result.Code = command[..index];
        result.Args.AddRange(command[(index + 1)..].Split(' '));
    }

    return result;
}




//const int _headLength = 4;
//const int _maxBytesToRead = 10; // 16384;

//CancellationTokenSource cts = new CancellationTokenSource();

//TcpServer tcpServer = new TcpServer(cts);
//await tcpServer.StartAsync("localhost", 50505);



//InputService inputService = new(cts.Token);
//await inputService.StartRead();


//IPEndPoint iPEndPoint = new(IPAddress.Any, 50505);
//using TcpListener listener = new(ipendPoint);

//CancellationToken token = new CancellationToken();
//try
//{
//    listener.Start();

//    while (true)
//    {
//        //Task<TcpClient> acceptTcpClientTask = ;
//        //Task<TcpClient> awaiter = await Task.WhenAny(acceptTcpClientTask).ConfigureAwait(false);

//        var tcpClient = await listener.AcceptTcpClientAsync();
//        Stream stream = tcpClient.GetStream();

//    _loop:
//        int _bytesToRead = _headLength;
//        byte[] headBuffer = new byte[_bytesToRead];
//        int count = stream.Read(headBuffer, 0, _bytesToRead);

//        do
//        {
//            if (count == 0)
//            {
//                Console.WriteLine("Data1 length is 0");
//                tcpClient.Close();
//                tcpClient.Dispose();
//                return;
//            }

//            _bytesToRead -= count;

//            if (_bytesToRead > 0)
//            {
//                count = await stream.ReadAsync(headBuffer, headBuffer.Length - _bytesToRead, _bytesToRead).ConfigureAwait(false);
//            }


//        } while (_bytesToRead > 0);

//        int datLength = BitConverter.ToInt32(headBuffer, 0);

//        _bytesToRead = datLength;

//        int rawBufferLength = datLength + _headLength;
//        byte[] rawBuffer = new byte[rawBufferLength];

//        Array.Copy(headBuffer, 0, rawBuffer, 0, _headLength);
//        int rawOffset = _headLength;
//        while (_bytesToRead > 0)
//        {
//            int bytesToRead = Math.Min(_bytesToRead, _maxBytesToRead);

//            count = await stream.ReadAsync(rawBuffer, rawOffset, bytesToRead).ConfigureAwait(false);

//            if (count == 0)
//            {
//                Console.WriteLine("Data2 length is 0");
//                tcpClient.Close();
//                tcpClient.Dispose();
//                return;
//            }

//            rawOffset += count;
//            _bytesToRead -= count;
//        }

//        string message = Encoding.UTF8.GetString(rawBuffer, 4, datLength);
//        Console.WriteLine(message);
//        goto _loop;
//        //}
//    }

//}
//catch (Exception ex)
//{
//    listener.Stop();
//    Console.WriteLine(ex);
//    return;
//}
