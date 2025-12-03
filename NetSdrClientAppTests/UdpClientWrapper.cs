using NetSdrClientApp.Networking;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class UdpClientWrapperTests
    {
        [Test]
        public void Constructor_CreatesInstance()
        {
            // Arrange & Act
            var wrapper = new UdpClientWrapper(60000);

            // Assert
            Assert.That(wrapper, Is.Not.Null);
        }

        [Test]
        public void StopListening_WhenNotStarted_DoesNotThrow()
        {
            // Arrange
            var wrapper = new UdpClientWrapper(60001);

            // Act & Assert
            Assert.DoesNotThrow(() => wrapper.StopListening());
        }

        [Test]
        public void Exit_WhenNotStarted_DoesNotThrow()
        {
            // Arrange
            var wrapper = new UdpClientWrapper(60002);

            // Act & Assert
            Assert.DoesNotThrow(() => wrapper.Exit());
        }

        [Test]
        public void StopListening_MultipleTimes_DoesNotThrow()
        {
            // Arrange
            var wrapper = new UdpClientWrapper(60003);

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                wrapper.StopListening();
                wrapper.StopListening();
                wrapper.StopListening();
            });
        }

        [Test]
        public void Exit_MultipleTimes_DoesNotThrow()
        {
            // Arrange
            var wrapper = new UdpClientWrapper(60004);

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                wrapper.Exit();
                wrapper.Exit();
            });
        }

        [Test]
        public void MessageReceived_Event_CanBeSubscribed()
        {
            // Arrange
            var wrapper = new UdpClientWrapper(60005);
            bool eventFired = false;

            // Act
            wrapper.MessageReceived += (sender, data) =>
            {
                eventFired = true;
            };

            // Assert - подія підписана, але не спрацювала (немає повідомлень)
            Assert.That(eventFired, Is.False);
        }

        [Test]
        public void GetHashCode_ReturnsSameValueForSamePort()
        {
            // Arrange
            var wrapper1 = new UdpClientWrapper(60006);
            var wrapper2 = new UdpClientWrapper(60006);

            // Act
            var hash1 = wrapper1.GetHashCode();
            var hash2 = wrapper2.GetHashCode();

            // Assert
            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void GetHashCode_ReturnsDifferentValueForDifferentPort()
        {
            // Arrange
            var wrapper1 = new UdpClientWrapper(60007);
            var wrapper2 = new UdpClientWrapper(60008);

            // Act
            var hash1 = wrapper1.GetHashCode();
            var hash2 = wrapper2.GetHashCode();

            // Assert
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public async Task StartListeningAsync_CanBeCancelled()
        {
            // Arrange
            var wrapper = new UdpClientWrapper(60009);

            // Act
            var listenTask = Task.Run(() => wrapper.StartListeningAsync());
            await Task.Delay(100); // Даємо час на старт
            wrapper.StopListening();

            // Assert - має завершитись без exception
            var completed = await Task.WhenAny(listenTask, Task.Delay(2000));
            Assert.That(completed, Is.EqualTo(listenTask), "Listening should stop when StopListening is called");
        }

        [Test]
        public void Constructor_WithDifferentPorts_CreatesUniqueInstances()
        {
            // Arrange & Act
            var wrapper1 = new UdpClientWrapper(60010);
            var wrapper2 = new UdpClientWrapper(60011);

            // Assert
            Assert.That(wrapper1, Is.Not.Null);
            Assert.That(wrapper2, Is.Not.Null);
            Assert.That(wrapper1, Is.Not.SameAs(wrapper2));
        }
    }
}