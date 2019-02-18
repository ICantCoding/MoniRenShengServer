using System;
using System.Diagnostics;

public class Debug
{
    public static void Log(string msg)
    {
        Console.WriteLine(msg);
    }
    public static void LogError(string msg)
    {
        Console.WriteLine("Error:" + msg);
        PrintStackTrace();
    }
    public static void LogWarning(string msg)
    {
        Console.WriteLine(msg);

    }

    private static void PrintStackTrace()
    {
        var st = new StackTrace(true);
        for (var i = 0; i < st.FrameCount; i++)
        {
            var sf = st.GetFrame(i);
            Debug.Log(sf.ToString());
        }
    }
}