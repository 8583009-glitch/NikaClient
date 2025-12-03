using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class NetSdrClientAdditionalTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _udpMock;

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
            .Callback<byte[]>((bytes) =>
            {
                _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
            })
            .Returns(Task.CompletedTask);

        _udpMock = new Mock<IUdpClient>();
        _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
    }

    [Test]
    public async Task SendMessageAsync_WithValidData_SendsCorrectly()
    {
        // Arrange
        await ConnectAsync();
        
        // Act - SendMessageAsync викликається всередині ConnectAsync
        // Assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.AtLeast(3));
    }

    [Test]
    public async Task StartIQAsync_SetsIQStartedToTrue()
    {
        // Arrange
        await ConnectAsync();
        
        // Act
        await _client.StartIQAsync();
        
        // Assert
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQAsync_SetsIQStartedToFalse()
    {
        // Arrange
        await ConnectAsync();
        await _client.StartIQAsync();
        
        // Act
        await _client.StopIQAsync();
        
        // Assert
        Assert.That(_client.IQStarted, Is.False);
    }

    [Test]
    public async Task ChangeFrequencyAsync_SendsCorrectMessage()
    {
        // Arrange
        await ConnectAsync();
        _tcpMock.ResetCalls();
        
        // Act
        await _client.ChangeFrequencyAsync(14000000, 1);
        
        // Assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
    }

    [Test]
    public async Task ChangeFrequencyAsync_WithDifferentChannels_SendsMessages()
    {
        // Arrange
        await ConnectAsync();
        
        // Act
        await _client.ChangeFrequencyAsync(7000000, 0);
        await _client.ChangeFrequencyAsync(21000000, 2);
        
        // Assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.AtLeast(5)); // 3 from connect + 2 frequency changes
    }

    [Test]
    public void Disconnect_CallsTcpClientDisconnect()
    {
        // Act
        _client.Disconect();
        
        // Assert
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task ConnectAsync_MultipleTimes_OnlyConnectsOnce()
    {
        // Arrange
        await ConnectAsync();
        _tcpMock.ResetCalls();
        
        // Act
        await _client.ConnectAsync();
        await _client.ConnectAsync();
        
        // Assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Never);
    }

    [Test]
    public async Task StartIQAsync_WhenNotConnected_DoesNotStartUdpListening()
    {
        // Act
        await _client.StartIQAsync();
        
        // Assert
        _udpMock.Verify(udp => udp.StartListeningAsync(), Times.Never);
    }

    [Test]
    public async Task StopIQAsync_WhenNotConnected_DoesNotStopUdpListening()
    {
        // Act
        await _client.StopIQAsync();
        
        // Assert
        _udpMock.Verify(udp => udp.StopListening(), Times.Never);
    }

    [Test]
    public async Task TcpMessageReceived_TriggersResponseHandling()
    {
        // Arrange
        await ConnectAsync();
        byte[] testData = new byte[] { 1, 2, 3, 4 };
        
        // Act
        _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, testData);
        
        // Assert - метод викликався без exception
        Assert.Pass();
    }

    [Test]
    public async Task UdpMessageReceived_ProcessesDataItemMessages()
    {
        // Arrange
        await ConnectAsync();
        await _client.StartIQAsync();
        
        // Створюємо валідне DataItem повідомлення
        var msgType = NetSdrClientApp.Messages.NetSdrMessageHelper.MsgTypes.DataItem1;
        var testData = new byte[100];
        var message = NetSdrClientApp.Messages.NetSdrMessageHelper.GetDataItemMessage(msgType, testData);
        
        // Act - не кидає exception
        Assert.DoesNotThrow(() =>
        {
            _udpMock.Raise(udp => udp.MessageReceived += null, _udpMock.Object, message);
        });
    }

    [Test]
    public async Task ConnectAsync_SendsAllInitializationMessages()
    {
        // Act
        await ConnectAsync();
        
        // Assert - перевіряємо що відправлено 3 повідомлення ініціалізації
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task StartIQAsync_StartsUdpListening()
    {
        // Arrange
        await ConnectAsync();
        
        // Act
        await _client.StartIQAsync();
        
        // Assert
        _udpMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
    }

    [Test]
    public async Task StopIQAsync_StopsUdpListening()
    {
        // Arrange
        await ConnectAsync();
        await _client.StartIQAsync();
        
        // Act
        await _client.StopIQAsync();
        
        // Assert
        _udpMock.Verify(udp => udp.StopListening(), Times.Once);
    }

    [Test]
    public async Task IQStarted_InitiallyFalse_ThenTrueAfterStart()
    {
        // Arrange
        Assert.That(_client.IQStarted, Is.False);
        await ConnectAsync();
        
        // Act
        await _client.StartIQAsync();
        
        // Assert
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task MultipleFrequencyChanges_AllSendMessages()
    {
        // Arrange
        await ConnectAsync();
        
        // Act
        await _client.ChangeFrequencyAsync(3500000, 1);
        await _client.ChangeFrequencyAsync(7000000, 1);
        await _client.ChangeFrequencyAsync(14000000, 1);
        await _client.ChangeFrequencyAsync(21000000, 1);
        
        // Assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.AtLeast(7)); // 3 init + 4 freq changes
    }

    [Test]
    public async Task ChangeFrequencyAsync_WithoutConnection_DoesNotThrow()
    {
        // Arrange - не підключаємось
        
        // Act & Assert - метод повинен обробити відсутність з'єднання
        Assert.DoesNotThrowAsync(async () => 
            await _client.ChangeFrequencyAsync(14000000, 1));
    }

    private async Task ConnectAsync()
    {
        await _client.ConnectAsync();
    }
}