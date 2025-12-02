using Moq;
using MyNamespace;
using NUnit.Framework;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EchoTcpServer.Tests
{
    [TestFixture]
    public class ClientHandlerTests
    {
        private Mock<ILogger> _mockLogger;
        private ClientHandler _handler;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger>();
            _handler = new ClientHandler(_mockLogger.Object);
        }

        [Test]
        public async Task HandleClientAsync_EchoesReceivedData()
        {
            // Arrange
            var testData = Encoding.UTF8.GetBytes("Hello, Server!");
            var memoryStream = new MemoryStream();
            
            // Створюємо два потоки: один для читання, один для запису
            var readStream = new MemoryStream(testData);
            var writeStream = new MemoryStream();

            // Не можемо легко мокнути TcpClient/NetworkStream, тому протестуємо логіку
            // Цей тест перевіряє що handler логує правильні повідомлення
            
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Одразу скасовуємо щоб не чекати

            // Створюємо реальний TcpClient для тесту (локальний)
            var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

            var clientTask = Task.Run(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync(System.Net.IPAddress.Loopback, port);
                var stream = client.GetStream();
                await stream.WriteAsync(testData);
                await stream.FlushAsync();
                
                // Читаємо відповідь
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            });

            var serverTask = Task.Run(async () =>
            {
                var serverClient = await listener.AcceptTcpClientAsync();
                await _handler.HandleClientAsync(serverClient, CancellationToken.None);
            });

            // Act
            var response = await clientTask;
            listener.Stop();

            // Assert
            Assert.That(response, Is.EqualTo("Hello, Server!"));
            _mockLogger.Verify(x => x.Log(It.Is<string>(s => s.Contains("Echoed"))), Times.AtLeastOnce);
            _mockLogger.Verify(x => x.Log("Client disconnected."), Times.Once);
        }

        [Test]
        public void Logger_LogsMessages()
        {
            // Arrange
            var logger = new ConsoleLogger();

            // Act & Assert - просто перевіряємо що не падає
            Assert.DoesNotThrow(() => logger.Log("Test message"));
        }

        [Test]
        public async Task HandleClientAsync_HandlesEmptyStream()
        {
            // Arrange
            var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

            var clientTask = Task.Run(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync(System.Net.IPAddress.Loopback, port);
                client.Close(); // Закриваємо одразу
            });

            var serverTask = Task.Run(async () =>
            {
                var serverClient = await listener.AcceptTcpClientAsync();
                await _handler.HandleClientAsync(serverClient, CancellationToken.None);
            });

            // Act
            await Task.WhenAll(clientTask, serverTask);
            listener.Stop();

            // Assert
            _mockLogger.Verify(x => x.Log("Client disconnected."), Times.Once);
        }

        [Test]
        public async Task HandleClientAsync_RespectsCancellationToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

            var clientTask = Task.Run(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync(System.Net.IPAddress.Loopback, port);
                await Task.Delay(100); // Чекаємо трохи
                client.Close();
            });

            var serverTask = Task.Run(async () =>
            {
                var serverClient = await listener.AcceptTcpClientAsync();
                cts.Cancel(); // Скасовуємо під час обробки
                await _handler.HandleClientAsync(serverClient, cts.Token);
            });

            // Act & Assert
            await Task.WhenAll(clientTask, serverTask);
            listener.Stop();
            
            _mockLogger.Verify(x => x.Log("Client disconnected."), Times.Once);
        }
    }
}