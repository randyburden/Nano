using System.Diagnostics;
using System.Net;
using Nano.Web.Core;
using Nano.Web.Core.Host.HttpListener;
using NUnit.Framework;

namespace Nano.Tests.RequestHandlers
{
    [TestFixture]
    public class DirectoryRequestHandlerTests
    {
        [Test]
        public void Add_A_Trailing_Slash_When_The_Requested_Directory_Exists_And_The_Requested_Url_Does_Not_Have_A_Trailing_Slash()
        {
            var nanoConfiguration = new NanoConfiguration();
            nanoConfiguration.AddDirectory( "/", "www" );
            var url = "http://localhost:4545";

            using ( HttpListenerNanoServer.Start( nanoConfiguration, url ) )
            {
                var apiExplorerUrl = "/ApiExplorer";

                var request = ( HttpWebRequest ) WebRequest.Create( url + apiExplorerUrl );
                request.Method = "HEAD";
                request.AllowAutoRedirect = false;

                string location;
                using ( var response = request.GetResponse() as HttpWebResponse )
                {
                    location = response.GetResponseHeader( "Location" );
                }

                Trace.WriteLine( "Location: " + location );
                Assert.That( location == apiExplorerUrl + "/" );
            }
        }
    }
}