using System.Text;

namespace Core;

public class Protocol
{
    public const ushort HearderLength = 4;
    public const uint MaxByteToRead = 16384;

    private readonly ArrayPoolMemoryProvider _memoryProvider;

    public Protocol()
    {
        _memoryProvider = new ArrayPoolMemoryProvider();
    }

    public ReadOnlyMemory<byte> CreatMessage(string message)
    {
        byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
        int messageBufferLength = messageBuffer.Length;

        IMemory memory = _memoryProvider.Provide(messageBufferLength + HearderLength);

        Array.Copy(messageBuffer, 0, memory.Bytes, HearderLength, messageBufferLength);

        BitConverter.GetBytes(messageBuffer.Length).CopyTo(memory.Bytes, 0);
        return memory.Memory;
    }
}
