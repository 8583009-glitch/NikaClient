using Moq;
using MyNamespace;
using NUnit.Framework;
using System.Threading.Tasks;

namespace EchoTcpServer.Tests
{
    [TestFixture]
    public class EchoServerTests
    {
        private Mock<ILogger> _mockLogger;
        private Mock<IClientHandler> _mockClientHandler;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger>();
            _mockClientHandler = new Mock<IClientHandler>();
        }

        [Test]
        public void EchoServer_CanBeCreated()
        {
            // Act
            var server = new EchoServer(5555, _mockLogger.Object, _mockClientHandler.Object);

            // Assert
            Assert.That(server, Is.Not.Null);
        }

        [Test]
        public void Stop_LogsStopMessage()
        {
            // Arrange
            var server = new EchoServer(5556, _mockLogger.Object, _mockClientHandler.Object);

            // Act
            server.Stop();

            // Assert
            _mockLogger.Verify(x => x.Log("Server stopped."), Times.Once);
        }

        [Test]
        public async Task StartAsync_LogsStartMessage()
        {
            // Arrange
            var server = new EchoServer(0, _mockLogger.Object, _mockClientHandler.Object);
            
            // Використовуємо порт 0 щоб ОС сама вибрала вільний порт
            var startTask = Task.Run(() => server.StartAsync());
            
            // Даємо більше часу на старт
            await Task.Delay(500);
            
            // Act
            server.Stop();
            
            // Чекаємо завершення з timeout
            var completedTask = await Task.WhenAny(startTask, Task.Delay(2000));
            
            // Assert
            _mockLogger.Verify(x => x.Log(It.Is<string>(s => s.Contains("Server started"))), Times.Once);
            _mockLogger.Verify(x => x.Log("Server stopped."), Times.Once);
        }

        [Test]
        public void Stop_WithoutStart_DoesNotThrow()
        {
            // Arrange
            var server = new EchoServer(5558, _mockLogger.Object, _mockClientHandler.Object);

            // Act & Assert
            Assert.DoesNotThrow(() => server.Stop());
        }
    }
}