using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyNamespace
{
    /// <summary>
    /// This program was designed for test purposes only
    /// Not for a review
    /// </summary>
    public class EchoServer
    {
        private readonly int _port;
        private readonly ILogger _logger;
        private readonly IClientHandler _clientHandler;
        private TcpListener? _listener;
        private CancellationTokenSource _cancellationTokenSource;

        public EchoServer(int port, ILogger logger, IClientHandler clientHandler)
        {
            _port = port;
            _logger = logger;
            _clientHandler = clientHandler;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _logger.Log($"Server started on port {_port}.");
            
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    _logger.Log("Client connected.");
                    _ = Task.Run(() => _clientHandler.HandleClientAsync(client, _cancellationTokenSource.Token));
                }
                catch (ObjectDisposedException)
                {
                    // Listener has been closed
                    break;
                }
            }
            _logger.Log("Server shutdown.");
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener?.Stop();
            _cancellationTokenSource.Dispose();
            _logger.Log("Server stopped.");
        }

        public static async Task Main(string[] args)
        {
            var logger = new ConsoleLogger();
            var clientHandler = new ClientHandler(logger);
            var server = new EchoServer(5000, logger, clientHandler);
            
            _ = Task.Run(() => server.StartAsync());
            
            string host = "127.0.0.1";
            int port = 60000;
            int intervalMilliseconds = 5000;
            
            using (var sender = new UdpTimedSender(host, port, logger))
            {
                Console.WriteLine("Press any key to stop sending...");
                sender.StartSending(intervalMilliseconds);
                Console.WriteLine("Press 'q' to quit...");
                
                while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q)
                {
                    // Just wait until 'q' is pressed
                }
                
                sender.StopSending();
                server.Stop();
                Console.WriteLine("Sender stopped.");
            }
        }
    }

    public class UdpTimedSender : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly ILogger _logger;
        private readonly UdpClient _udpClient;
        private Timer? _timer;

        public UdpTimedSender(string host, int port, ILogger logger)
        {
            _host = host;
            _port = port;
            _logger = logger;
            _udpClient = new UdpClient();
        }

        public void StartSending(int intervalMilliseconds)
        {
            if (_timer != null)
                throw new InvalidOperationException("Sender is already running.");
            _timer = new Timer(SendMessageCallback, null, 0, intervalMilliseconds);
        }

        private ushort i = 0;
        private void SendMessageCallback(object? state)
        {
            try
            {
                Random rnd = new Random();
                byte[] samples = new byte[1024];
                rnd.NextBytes(samples);
                i++;
                byte[] msg = (new byte[] { 0x04, 0x84 })
                    .Concat(BitConverter.GetBytes(i))
                    .Concat(samples)
                    .ToArray();
                var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);
                _udpClient.Send(msg, msg.Length, endpoint);
                _logger.Log($"Message sent to {_host}:{_port}");
            }
            catch (Exception ex)
            {
                _logger.Log($"Error sending message: {ex.Message}");
            }
        }

        public void StopSending()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public void Dispose()
        {
            StopSending();
            _udpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}