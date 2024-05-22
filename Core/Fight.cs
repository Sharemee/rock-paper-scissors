namespace Core;

public enum SPC : int
{
    石头 = 1,
    布 = 2,
    剪子 = 3,
}

public enum SPCResult
{
    你输了 = -1,
    平手 = 0,
    你赢了 = 1,
}

public class Fight
{
    public static bool SetLocalInput(string message)
    {
        //if (LocalInput is not null) return false;

        if (IsRockPaperScissors(message, out SPC spc))
        {
            LocalInput = spc;
            return true;
        }
        return false;
    }

    public static SPC? LocalInput { get; private set; }

    public static SPC? RemoteInput { get; private set; }

    public static bool SetRemoteInput(string message)
    {

        //if (RemoteInput is not null) return false;

        if (IsRockPaperScissors(message, out SPC spc))
        {
            RemoteInput = spc;
            return true;
        }
        return false;
    }

    /// <summary>
    /// -1 输了, 0 平手, 1 赢了
    /// </summary>
    /// <returns></returns>
    public static bool GetResult(out SPCResult? result)
    {
        if (LocalInput is null || RemoteInput is null)
        {
            result = null;
            return false;
        };

        // 平手
        if (LocalInput == RemoteInput)
        {
            result = SPCResult.平手;
            Resert();
            return true;
        }

        if ((LocalInput - RemoteInput) == 1 || (LocalInput - RemoteInput) == -2)
        {
            result = SPCResult.你赢了;
        }
        else
        {
            result = SPCResult.你输了;
        }
        Resert();
        return true;
    }

    public static bool IsRockPaperScissors(string message, out SPC spc)
    {
        return Enum.TryParse(message, out spc);
    }

    private static void Resert()
    {
        LocalInput = null;
        RemoteInput = null;
    }
}
