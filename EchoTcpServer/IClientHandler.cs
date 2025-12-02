using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MyNamespace
{
    public interface IClientHandler
    {
        Task HandleClientAsync(TcpClient client, CancellationToken token);
    }

    public interface ILogger
    {
        void Log(string message);
    }

    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }

    public class ClientHandler : IClientHandler
    {
        private readonly ILogger _logger;

        public ClientHandler(ILogger logger)
        {
            _logger = logger;
        }

        public async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    while (!token.IsCancellationRequested && 
                           (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        // Echo back the received message
                        await stream.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), token);
                        _logger.Log($"Echoed {bytesRead} bytes to the client.");
                    }
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _logger.Log($"Error: {ex.Message}");
                }
                finally
                {
                    client.Close();
                    _logger.Log("Client disconnected.");
                }
            }
        }
    }
}