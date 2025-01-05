using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Spectre.Console;

namespace AdminWrapper;

public class SocketServer : IDisposable
{
    #region Properties & Variables
    const int DELAY_NO_MESSAGE = 500;
    
    private CancellationTokenSource _source = new ();
    private TcpClient? _client;
    private TcpListener _listener;
    private Stream _stream;
    private Task? _streamReader;
    private bool disposedValue;

    public bool Connected => _client?.Connected ?? false;
    public int Port => (_listener?.LocalEndpoint as IPEndPoint)?.Port ?? 0;
    public bool Running { get; private set; }
    #endregion

    #region Constructor & Destructor
    public SocketServer()
    {
        _listener = new TcpListener(new IPEndPoint(IPAddress.Loopback, 0));
        _stream = Stream.Null;
    }
    #endregion

    #region Methods
    // ~SocketServer()
    // {
    //     // Ne changez pas ce code. Placez le code de nettoyage dans la méthode 'Dispose(bool disposing)'
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Ne changez pas ce code. Placez le code de nettoyage dans la méthode 'Dispose(bool disposing)'
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public void Reset()
    {
        Stop();
        Start();
    }

    public void SendMessage(string message)
    {
        if (disposedValue)
            throw new ObjectDisposedException(nameof(SocketServer));

        if (_stream == null) return;

        Span<byte> buff = stackalloc byte[sizeof(int) + Encoding.UTF8.GetMaxByteCount(message.Length)];
        int size = Encoding.UTF8.GetBytes(message, buff.Slice(sizeof(int)));
        MemoryMarshal.Cast<byte, int>(buff.Slice(sizeof(OutputCodes)))[0] = size;

        try
        {
            if (_stream.CanWrite)
                _stream.Write(buff);
        }
        catch (Exception e)
        {
            OnError?.Invoke(this, new ErrorEventArgs(e, false));
        }
    }

    public void Stop()
    {
        if (disposedValue)
            throw new ObjectDisposedException(nameof(SocketServer));
      
        Running = false;

        _source.Cancel();
        _listener.Stop();
        if (_client != null)
        {
            _client.Close();
            _client.Dispose();
        }

        _listener.Dispose();
        _stream.Dispose();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Stop();
                try
                {
                    _streamReader?.Wait(TimeSpan.FromSeconds(5));
                }
                catch (AggregateException ex) when (ex.InnerException is OperationCanceledException) { }
                _source.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Start()
    {
        if (disposedValue)
            throw new ObjectDisposedException(nameof(SocketServer));

        if (Running) return;

        Running = true;
        _listener.Start();
        _source = new CancellationTokenSource();

        _listener.BeginAcceptTcpClient(asyncResult =>
        {
            _client = _listener.EndAcceptTcpClient(asyncResult);
            _stream = _client.GetStream();
            
            _streamReader = Task.Run(ListenRequests);
        }, _listener);
    }

  #region Read Stream
    private void HandleAction(OutputCodes action)
    {
        OnAction?.Invoke(this, new ActionEventArgs(action));
    }

    private void ReadMessage(int size, ConsoleColor color)
    {
        Span<byte> buff = stackalloc byte[size];
        int messageBytesRead = _stream.Read(buff);

        string message = Encoding.UTF8.GetString(buff);
        OnMessage?.Invoke(this, new MessageEventArgs(message, color));
    }

    // [Message]
    // Color    | OutputCodes (byte)
    // size     | int
    // Text     | string
    // [Code]
    // Code     | OutputCodes
    private void ListenRequests()
    {
        // C# byte is the equivalent of C char
        Span<byte> buff = stackalloc byte[sizeof(OutputCodes) + sizeof(int)];
        CancellationTokenSource source = _source;

        try
        {
            while (!source.IsCancellationRequested && _stream != null)
            {
                int codeBytes = _stream.Read(buff);

                if (codeBytes == 0) return;

                // No need to MemoryMarshal.Cast<byte, int>() or cast same size, OutputCodes is a byte
                OutputCodes code = (OutputCodes)buff[0];

                if (code >= OutputCodes.RoundRestart)
                {
                    HandleAction(code);
                    continue;
                }

                int size = MemoryMarshal.Cast<byte, int>(buff.Slice(sizeof(OutputCodes)))[0];
                ReadMessage(size, (ConsoleColor)code);
            }
        }
        catch (Exception e)
        {
            OnError?.Invoke(this, new ErrorEventArgs(e, true));
            throw;
        }
        finally
        {
            source.Dispose();
        }
    }
    #endregion

    #endregion

    #region Events
    public event EventHandler<MessageEventArgs>? OnMessage;
    public event EventHandler<ActionEventArgs>? OnAction;
    public event EventHandler<ErrorEventArgs>? OnError;
    #endregion

    #region Nesteds

    public class ErrorEventArgs : EventArgs
    {
        public bool IsFatal { get; }
        public Exception Exception { get; }
        public ErrorEventArgs(Exception exception, bool isFatal)
        {
            Exception = exception;
            IsFatal = isFatal;
        }
    }

    public class ActionEventArgs : EventArgs
    {
        public OutputCodes Action { get; }
        public ActionEventArgs(OutputCodes action)
        {
            Action = action;
        }
    }

    public class MessageEventArgs : EventArgs
    {
        public string Message { get; }
        public ConsoleColor Color { get; }
        public MessageEventArgs(string message, ConsoleColor color)
        {
            Message = message;
            Color = color;
        }
    }

    /// <summary>
    /// If the code is link to a color that mean it's a message (log).
    /// Else it a specific code.
    /// </summary>
    public enum OutputCodes : byte
    {
        // System.ConsoleColor
        Black = 0b0000_0000,
        DarkBlue,
        DarkGreen,
        DarkCyan,
        DarkRed,
        DarkMagenta,
        DarkYellow,
        Gray,
        DarkGray,
        Blue,
        Green,
        Cyan,
        Red,
        Magenta,
        Yellow,
        White,

        RoundRestart = 0b0001_1111,
        IdleEnter,
        IdleExit,
        ExitActionReset,
        ExitActionShutdown,
        ExitActionSilentShutdown,
        ExitActionRestart,
        Heartbeat,
    }
    #endregion

  

}