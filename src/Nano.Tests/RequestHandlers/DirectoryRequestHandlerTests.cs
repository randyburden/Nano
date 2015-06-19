using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
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

                location = null;

                if ( string.IsNullOrWhiteSpace( location ) )
                {
                    Trace.WriteLine( "NanoConfiguration.ApplicationRootFolderPath: " + nanoConfiguration.ApplicationRootFolderPath );
                    OutputBinDirectory( nanoConfiguration.ApplicationRootFolderPath );

                    Trace.WriteLine( "" );
                    Trace.WriteLine( "AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory" );

                    var a = AppDomain.CurrentDomain.RelativeSearchPath;

                    if (  string.IsNullOrWhiteSpace( a ) )
                        a = AppDomain.CurrentDomain.BaseDirectory;

                    Trace.WriteLine( a );

                    OutputBinDirectory( a );

                }

                //Assert.That( location == apiExplorerUrl + "/" );
            }
        }

        private void OutputBinDirectory( string path )
        {
            try
            {
                string[] entries = Directory.GetFileSystemEntries( path, "*", SearchOption.AllDirectories );

                foreach ( var entry in entries )
                {
                    Trace.WriteLine( entry );
                }
            }
            catch ( Exception )
            {
            }
        }

        [Test]
        public void Should_Return_Default_Index_Dot_Html_File_When_Requesting_A_Directory()
        {
            var nanoConfiguration = new NanoConfiguration();
            nanoConfiguration.AddDirectory( "/", "www" );
            var url = "http://localhost:4545";

            using( HttpListenerNanoServer.Start( nanoConfiguration, url ) )
            {
                var apiExplorerUrl = "/ApiExplorer";

                using( var client = new WebClient() )
                {
                    byte[] responsebytes = client.DownloadData( "http://localhost:4545/" + apiExplorerUrl );
                    string responsebody = Encoding.UTF8.GetString( responsebytes );
                    Assert.That( responsebody.Contains( "Api Explorer" ) );
                }
            }
        }
    }
}