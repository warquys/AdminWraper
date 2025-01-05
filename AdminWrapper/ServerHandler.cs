using AdminWrapper.Config;
using System.Diagnostics;

namespace AdminWrapper;

public class ServerHandler : IDisposable
{
    #region Properties & Variables
    private readonly ServerConfig _config;
    private CancellationTokenSource _source;
    private bool disposedValue;
    public readonly ushort port;

    public State CurentState { get; private set; }

    public Process? Process { get; private set; }
    public SocketServer Socket { get; }
    #endregion

    #region Constructor & Destructor
    public ServerHandler(ServerConfig config, ushort port)
    {
        _config = config;
        this.port = port;
        Socket = new SocketServer();
        _source = new CancellationTokenSource();
        CurentState = State.Stopped;
    }
    #endregion

    #region Methods
    public void DisableRestart()
    {
        if (disposedValue)
            throw new ObjectDisposedException(nameof(ServerHandler));

        if (CurentState != State.Running)
            return;

        if (Process == null)
            throw new Exception("Failed to stop server");

        CurentState = State.WaitEnd;
        _source.Cancel();
        _source = new CancellationTokenSource();
        Process.WaitForExitAsync().ContinueWith(t =>
        {
            Stopping?.Invoke(this, EventArgs.Empty);
            Socket.Stop();
            Process?.Kill();
            Process?.Dispose();
            Process = null;
            CurentState = State.Stopped;
        });
    }

    public void Dispose()
    {
        if (!disposedValue)
        {
            Stop();
            _source.Dispose();
            Socket?.Dispose();
            disposedValue = true;
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Start and do not return until the process stop
    /// </summary>
    public void Start()
    {
        if (disposedValue)
            throw new ObjectDisposedException(nameof(ServerHandler));

        if (CurentState == State.Running)
            return;

        var source = _source;
        try
        {
            do
            {
                Socket.Start();

                string args = "-batchmode "
                    + "-nographics "
                    + "-silent-crashes "
                    + "-nodedicateddelete "
                    + $"-id{Environment.ProcessId} "
                    + $"-console{Socket.Port} "
                    + $"-port{port} ";

                args += " " + string.Join(" ", _config.ScpSlArgument);

                ProcessStartInfo startInfo = new(_config.ServerPath, args + " " + _config.ScpSlArgument)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                Process = Process.Start(startInfo);

                if (Process == null)
                    throw new Exception("Failed to start server");

                Process.EnableRaisingEvents = true;
                Starting?.Invoke(this, EventArgs.Empty);
                Process.WaitForExit();
            }
            while (_config.RestartWhenCrash && !source.IsCancellationRequested);
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            CurentState = State.Stopped;
            source.Dispose();
        }
    }

    public void Restart()
    {
        if (disposedValue)
            throw new ObjectDisposedException(nameof(ServerHandler));

        if (CurentState != State.Running)
            return;

        Process!.Kill();
        Process.Dispose();
        Process = null;
    }

    public void Stop()
    {
        if (disposedValue)
            throw new ObjectDisposedException(nameof(ServerHandler));

        if (CurentState != State.Running)
            return;

        if (Process == null)
            throw new Exception("Failed to stop server");

        CurentState = State.Stopping;
        _source.Cancel();
        _source = new CancellationTokenSource();
        Stopping?.Invoke(this, EventArgs.Empty);
        Socket.Stop();
        Process!.Kill();
        Process.Dispose();
        Process = null;
        CurentState = State.Stopped;
    }
    #endregion

    #region Events
    public event EventHandler? Starting;
    public event EventHandler? Stopping;
    #endregion

    #region Nesteds
    public enum State
    {
        Running,
        Stopping,
        WaitEnd,
        Stopped
    }
    #endregion
}
