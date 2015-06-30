using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using Nano.Web.Core;
using NUnit.Framework;

namespace Nano.Tests.ModelBinder
{
    [TestFixture]
    public class GetRequestParameterValueShould
    {
        [Test]
        public void Handle_A_Form_Url_Encoded_Request_Body_Value_That_Contains_An_Ampersand_Symbol()
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFunc( "/EchoMessage", context => context.GetRequestParameterValue( "message" ) );
                var postData = new NameValueCollection { { "message", "I am a sentence that contains an '&' ampersand." } };

                var parameters = new NameValueCollection { { "customerNbr", "1" } };

                using( var client = new WebClient() )
                {
                    byte[] responsebytes = client.UploadValues( "http://localhost:4545/api/Customer/GetCustomer", "POST", parameters );
                    string responsebody = Encoding.UTF8.GetString( responsebytes );
                    Trace.WriteLine( responsebody );
                }

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