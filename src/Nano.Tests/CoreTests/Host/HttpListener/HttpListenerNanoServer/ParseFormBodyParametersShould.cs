using System.Collections.Specialized;
using System.Diagnostics;
using Nano.Web.Core;
using NUnit.Framework;

namespace Nano.Tests.Host.HttpListener.HttpListenerNanoServer
{
    [TestFixture]
    public class ParseFormBodyParametersShould
    {
        [Test]
        public void Handle_A_Form_Url_Encoded_Request_Body_Value_That_Contains_An_Ampersand_Symbol()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFunc( "/EchoMessage", context => context.GetRequestParameterValue( "message" ) );
                var postData = new NameValueCollection { { "message", "I am a sentence that contains an '&' ampersand." } };

                // Act
                var response = HttpHelper.Post( server.GetUrl() + "/EchoMessage", postData );

                // Visual Assertion
                Trace.WriteLine( response );

                // Assert
                Assert.That( response.Contains( "&" ) );
            }
        }
    }
}