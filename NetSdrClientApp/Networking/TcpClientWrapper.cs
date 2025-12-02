using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    public class TcpClientWrapper : ITcpClient
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        public bool Connected => _tcpClient != null && _tcpClient.Connected && _stream != null;

        public event EventHandler<byte[]>? MessageReceived;

        public TcpClientWrapper(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public void Connect()
        {
            if (Connected)
            {
                Console.WriteLine($"Already connected to {_host}:{_port}");
                return;
            }

            _tcpClient = new TcpClient();
            try
            {
                _cts?.Dispose();
                _cts = new CancellationTokenSource();

                _tcpClient.Connect(_host, _port);
                _stream = _tcpClient.GetStream();
                Console.WriteLine($"Connected to {_host}:{_port}");

                _ = StartListeningAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
                _cts?.Dispose();
                _cts = null;
            }
        }

        public void Disconnect()
        {
            if (Connected)
            {
                DisconnectInternal();
                Console.WriteLine("Disconnected.");
            }
            else
            {
                Console.WriteLine("No active connection to disconnect.");
            }
        }

        private void DisconnectInternal()
        {
            _cts?.Cancel();
            _stream?.Close();
            _tcpClient?.Close();
            
            _cts?.Dispose();
            _cts = null;
            _tcpClient = null;
            _stream = null;
        }

        public async Task SendMessageAsync(byte[] data)
        {
            ValidateConnection();
            
            Console.WriteLine($"Message sent: {string.Join(" ", data.Select(b => b.ToString("x2")))}");
            await _stream!.WriteAsync(data.AsMemory(0, data.Length));
        }

        public async Task SendMessageAsync(string str)
        {
            var data = Encoding.UTF8.GetBytes(str);
            await SendMessageAsync(data);
        }

        private void ValidateConnection()
        {
            if (!Connected || _stream == null || !_stream.CanWrite)
            {
                throw new InvalidOperationException("Not connected to a server.");
            }
        }

        private async Task StartListeningAsync()
        {
            if (!Connected || _stream == null || !_stream.CanRead || _cts == null)
            {
                throw new InvalidOperationException("Not connected to a server.");
            }

            try
            {
                Console.WriteLine("Starting listening for incoming messages.");
                await ListenForMessagesAsync();
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in listening loop: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Listener stopped.");
            }
        }

        private async Task ListenForMessagesAsync()
        {
            while (!_cts!.Token.IsCancellationRequested)
            {
                byte[] buffer = new byte[8194];
                int bytesRead = await _stream!.ReadAsync(buffer.AsMemory(0, buffer.Length), _cts.Token);
                if (bytesRead > 0)
                {
                    MessageReceived?.Invoke(this, buffer.AsSpan(0, bytesRead).ToArray());
                }
            }
        }
    }
}