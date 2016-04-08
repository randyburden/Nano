using NUnit.Framework;

namespace Nano.Tests.Host.HttpListener.HttpListenerNanoServer
{
    [TestFixture]
    public class HttpListenerNanoServerShould
    {
        [Test]
        public void Null_The_HttpListener_On_Dispose()
        {
            // Arrange
            var server = NanoTestServer.Start();

            // Pre-Assert
            Assert.That( server.HttpListenerConfiguration.HttpListener != null );
            
            // Act
            server.Dispose();
            
            // Assert
            Assert.That( server.HttpListenerConfiguration == null );
        }
        [Test]
        public void Null_The_HttpListenerConfiguration_On_Dispose()
        {
            // Arrange
            var server = NanoTestServer.Start();

            // Pre-Assert
            Assert.That( server.HttpListenerConfiguration != null );

            // Act
            server.Dispose();

            // Assert
            Assert.That( server.HttpListenerConfiguration == null );
        }
    }
}