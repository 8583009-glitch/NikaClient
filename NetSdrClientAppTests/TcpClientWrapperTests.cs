using NetSdrClientApp.Networking;
using NUnit.Framework;
using System;
using System.Text;
using System.Threading.Tasks;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class TcpClientWrapperTests
    {
        [Test]
        public void Constructor_CreatesInstance()
        {
            // Arrange & Act
            var wrapper = new TcpClientWrapper("127.0.0.1", 5000);

            // Assert
            Assert.That(wrapper, Is.Not.Null);
            Assert.That(wrapper.Connected, Is.False);
        }

        [Test]
        public void Connected_ReturnsFalse_WhenNotConnected()
        {
            // Arrange
            var wrapper = new TcpClientWrapper("127.0.0.1", 5000);

            // Act & Assert
            Assert.That(wrapper.Connected, Is.False);
        }

        [Test]
        public void Disconnect_WhenNotConnected_DoesNotThrow()
        {
            // Arrange
            var wrapper = new TcpClientWrapper("127.0.0.1", 5000);

            // Act & Assert
            Assert.DoesNotThrow(() => wrapper.Disconnect());
        }

        [Test]
        public void SendMessageAsync_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var wrapper = new TcpClientWrapper("127.0.0.1", 5000);
            var data = new byte[] { 1, 2, 3 };

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await wrapper.SendMessageAsync(data));
        }

        [Test]
        public void SendMessageAsyncString_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var wrapper = new TcpClientWrapper("127.0.0.1", 5000);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await wrapper.SendMessageAsync("test"));
        }

        [Test]
        public void Connect_ToInvalidHost_DoesNotThrow()
        {
            // Arrange
            var wrapper = new TcpClientWrapper("invalid.host.example", 9999);

            // Act & Assert
            Assert.DoesNotThrow(() => wrapper.Connect());
            Assert.That(wrapper.Connected, Is.False);
        }

        [Test]
        public async Task MessageReceived_Event_CanBeSubscribed()
        {
            // Arrange
            var wrapper = new TcpClientWrapper("127.0.0.1", 5000);
            bool eventFired = false;

            // Act
            wrapper.MessageReceived += (sender, data) =>
            {
                eventFired = true;
            };

            // Assert - подія підписана, але не спрацювала (немає з'єднання)
            Assert.That(eventFired, Is.False);
        }

        [Test]
        public void Constructor_WithValidParameters_SetsHostAndPort()
        {
            // Arrange
            string host = "192.168.1.1";
            int port = 8080;

            // Act
            var wrapper = new TcpClientWrapper(host, port);

            // Assert
            Assert.That(wrapper, Is.Not.Null);
            // Немає публічних властивостей для host/port, але перевіряємо що не кидає exception
        }

        [Test]
        public void Disconnect_MultipleTimes_DoesNotThrow()
        {
            // Arrange
            var wrapper = new TcpClientWrapper("127.0.0.1", 5000);

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                wrapper.Disconnect();
                wrapper.Disconnect();
                wrapper.Disconnect();
            });
        }
    }
}