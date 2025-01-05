using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using AdminWrapper.Config;
using AdminWrapper.Log;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AdminWrapper;

public class StartCommand : Command<StartCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<Port>")]
        public ushort Port { get; set; }

        [CommandOption("--rd_stderr")]
        [DefaultValue(true)]
        public bool RedirectStdErr { get; set; }

        [CommandOption("--rd_std")]
        [DefaultValue(false)]
        public bool RedirectStd { get; set; }
    }

    // Execute joue le role du CTOR, c'est champs ne sont donc pas null
    private ServerHandler? server;
    private LogArchiver? archiver;
    private StringBuilder RoundLogs = new();
    private Lock @lock = new();

    private bool redirectStdErr;
    private bool redirectStd;

    public override int Execute(CommandContext context, Settings settings)
    {
        redirectStdErr = settings.RedirectStdErr;
        redirectStd = settings.RedirectStd;
        var container = ConfigHandler.LoadPort(settings.Port);
        archiver = new LogArchiver(container.Get<ArchiveConfig>(), settings.Port);
        server = new ServerHandler(container.Get<ServerConfig>(), settings.Port);
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        server.Starting += Server_Starting;
        server.Stopping += Server_Stopping;
        server.Start();
        return 0;
    }

    // Do not log when the main thread close
    #region Event Handlers
    private void Server_Stopping(object? sender, EventArgs e)
    {
        server!.Socket.OnMessage -= Socket_OnMessage;
        server.Socket.OnError -= Socket_OnError;

        if (server!.Process == null)
        {
            AnsiConsole.MarkupLine("[red]Failed to stop server[/]");
            return;
        }

        server.Process.OutputDataReceived -= Process_OutputDataReceived;
        server.Process.ErrorDataReceived -= Process_ErrorDataReceived;
    }

    private void Server_Starting(object? sender, EventArgs e)
    {
        if (server!.Process == null)
        {
            AnsiConsole.MarkupLine("[red]Failed to start server[/]");
            Environment.Exit(1);
        }

        AnsiConsole.MarkupLine($"[green]Server started [bold]{server.Process.Id}[/] [/]");

        server.Process.OutputDataReceived += Process_OutputDataReceived;
        server.Process.ErrorDataReceived += Process_ErrorDataReceived;
        server.Process.Exited += Process_Exited;
        server.Process.BeginOutputReadLine();
        server.Process.BeginErrorReadLine();
        server.Socket.OnMessage += Socket_OnMessage;
        server.Socket.OnError += Socket_OnError;
        server.Socket.OnAction += Socket_OnAction;
    }

    private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
    {
        archiver!.Archive(RoundLogs.ToString());
    }

    private void Process_Exited(object? sender, EventArgs e)
    {
        AnsiConsole.MarkupLine($"[red]Server stopped {server!.Process?.ExitCode.ToString() ?? "None"}[/]");

        lock (@lock)
        {
            if (RoundLogs.Length == 0)
                return;

            archiver!.Archive(RoundLogs.ToString());
            RoundLogs.Clear();
        }
    }

    private void Socket_OnError(object? sender, SocketServer.ErrorEventArgs e)
    {
        AnsiConsole.MarkupLine($"[red]Socket Error! fatal:{e.IsFatal}[/]");

        archiver!.ArchiveCrash("Socket Error:" + e.Exception.ToString());
        lock (@lock)
        {
            if (RoundLogs.Length == 0)
                return;

            RoundLogs.Clear();
        }

        if (e.IsFatal)
        {
            archiver!.Archive(RoundLogs.ToString());
            server!.Restart();
        }
    }

    private void Socket_OnMessage(object? sender, SocketServer.MessageEventArgs e)
    {
        lock (@lock)
        {
            if (RoundLogs.Length == 0)
                return;

            RoundLogs.Append(e.Message);
        }
    }

    private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!redirectStdErr) return;

        lock (@lock)
        {
            RoundLogs.Append("STD ER ");
            RoundLogs.Append(e.Data);
            RoundLogs.AppendLine();
        }
    }

    private void Socket_OnAction(object? sender, SocketServer.ActionEventArgs e)
    {
        switch (e.Action)
        {
            case SocketServer.OutputCodes.RoundRestart:
                lock (@lock)
                {
                    RoundLogs.Append("Round Restart, waiting for players.");
                }
                break;

            case SocketServer.OutputCodes.IdleEnter:
                lock (@lock)
                {
                    RoundLogs.Append("Server entered idle mode.");
                }
                break;

            case SocketServer.OutputCodes.IdleExit:
                lock (@lock)
                {
                    RoundLogs.Append("Server exited idle mode.");
                }
                break;

            case SocketServer.OutputCodes.ExitActionShutdown:
                lock (@lock)
                {
                    RoundLogs.Append("Server shutdown requested.");
                }
                server!.DisableRestart();
                break;
        }
    }

    private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!redirectStd) return;
        
        lock (@lock)
        {
            RoundLogs.Append("STD ");
            RoundLogs.Append(e.Data);
            RoundLogs.AppendLine();
        }
    }
    #endregion
}
