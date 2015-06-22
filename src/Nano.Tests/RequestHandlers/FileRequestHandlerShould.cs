using System.Diagnostics;
using System.Net;
using NUnit.Framework;

namespace Nano.Tests.RequestHandlers
{
    [TestFixture]
    public class FileRequestHandlerShould
    {
        [TestCase( "/ApiExplorer/" )]
        [TestCase( "/ApiExplorer/index.html" )]
        [TestCase( "/APIEXPLORER/INDEX.HTML", Description = "Testing all UPPERCASE" )]
        [TestCase( "/apiexplorer/index.html", Description = "Testing all lowercase" )]
        [TestCase( "/aPiExpLoRer/InDeX.hTMl", Description = "Testing UPPER and lower case" )]
        public void Return_A_File_That_Exists( string path )
        {
            using ( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFile( path, "www/ApiExplorer/index.html" );

                // Act
                var response = HttpHelper.GetResponseString( server.GetUrl() + path );

                // Assert
                Assert.That( response.Contains( "Api Explorer" ) );
            }
        }

        [TestCase( "/ApiExplorer/" )]
        [TestCase( "/ApiExplorer/index.htm" )]
        [TestCase( "/NoSuchFile.html" )]
        [TestCase( "//////lkjasldkffj409803948/laijsdfoiuwpo0e9ouphasldifj/ljpowu98723984uoiasjdlfknasl;dijpasoeurpoweur/" )]
        public void Return_An_Http_404_When_The_Requested_File_Is_Not_Found( string path )
        {
            using( var server = NanoTestServer.Start() )
            {
                // Arrange
                server.NanoConfiguration.AddFile( "/ApiExplorer/index.html", "www/ApiExplorer/index.html" );

                // Act
                var response = HttpHelper.GetHttpWebResponse( server.GetUrl() + path );

                // Visual Assertion
                Trace.WriteLine( response.StatusCode );
                Trace.WriteLine( response.GetResponseString());

                // Assert
                Assert.That( response.StatusCode == HttpStatusCode.NotFound );
            }
        }
    }
}