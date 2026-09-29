namespace MmoGame3d.Overseer;

using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

/// <summary>
/// The main game server on this machine, which connected bots play on (bots.md T8). If
/// nothing listens on its port, it is started as scripts/server-up.ps1 starts it, and left
/// running after the run; scripts/server-stop.ps1 stops it so it saves.
/// </summary>
public static class LocalServer
{
    private const int Port = 7070;
    private const int PollMilliseconds = 250;

    private static readonly TimeSpan StartLimit = TimeSpan.FromSeconds(30);

    public static bool Ensure(string godot, string project)
    {
        if (Listening())
        {
            Console.WriteLine("Server: already running");
            return true;
        }

        Console.WriteLine("Server: starting");
        ProcessStartInfo start = new ProcessStartInfo(godot);
        start.UseShellExecute = true;
        start.ArgumentList.Add("--headless");
        start.ArgumentList.Add("--path");
        start.ArgumentList.Add(project);
        start.ArgumentList.Add("--");
        start.ArgumentList.Add("--server");
        Process.Start(start);

        Stopwatch clock = Stopwatch.StartNew();

        while (clock.Elapsed < StartLimit)
        {
            if (Listening())
            {
                Console.WriteLine("Server: up after " + (int)clock.Elapsed.TotalSeconds + " s");
                return true;
            }

            Thread.Sleep(PollMilliseconds);
        }

        Console.WriteLine("Server: not listening after " + StartLimit.TotalSeconds + " s");
        return false;
    }

    // ENet is UDP, so a server is up when something holds its UDP port.
    private static bool Listening()
    {
        foreach (IPEndPoint endPoint in IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners())
        {
            if (endPoint.Port == Port)
            {
                return true;
            }
        }

        return false;
    }
}
