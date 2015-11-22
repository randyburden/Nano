using System;
using System.Collections.Generic;
using System.Diagnostics;
using Nano.Web.Core.Host.HttpListener;
using Topshelf;

namespace Nano.Demo.SelfHost.Topshelf
{
    internal class Program
    {
        internal static void Main( string[] args )
        {
            string uris = "http://localhost:4545";

            HostFactory.Run( x =>
            {
                // Defining a custom command line parameter in order to obtain the list of URI's to listen on
                x.AddCommandLineDefinition( "uri", input => { uris = input; } );
                x.ApplyCommandLine();

                x.Service<Startup>( settings =>
                {
                    settings.ConstructUsing( hostSettings => new Startup( hostSettings.ServiceName ) );
                    settings.WhenStarted( nano => nano.Start( uris ) );
                    settings.WhenStopped( nano => nano.Stop() );
                } );

                x.StartAutomatically();
            } );
        }

        internal class Startup
        {
            private readonly string _applicationName;

            public Startup( string applicationName )
            {
                _applicationName = applicationName;
            }

            private HttpListenerNanoServer _server;

            public void Start( string urls )
            {
                var validatedUrls = ValidateUrls( urls );

                var config = NanoConfigurationHelper.GetNanoConfiguration();

                // Specify your application name. A reasonable default is automatically used if not 
                // supplied but it's definitely recommended to supply one.
                config.ApplicationName = _applicationName ?? "Nano.Demo.SelfHost.Topshelf";

                // Start the server passing in the NanoConfiguration and a list of URLs to listen on.
                _server = HttpListenerNanoServer.Start( config, validatedUrls );

                // Optionally specify a virtual application path if your application URL is going to use one.
                // Example: http://localhost:4545/ExecutiveDashboard/
                _server.HttpListenerConfiguration.ApplicationPath = "";

                string url = _server.HttpListenerConfiguration.GetFirstUrlBeingListenedOn();

                // If the debugger is attached go ahead and launch a browser window directly to the Api Explorer page.
                if ( Debugger.IsAttached )
                    Process.Start( url + "ApiExplorer" );

                Console.WriteLine( "Nano Server is running on: " + url );
                Console.WriteLine( config.ToString() );
            }

            public void Stop()
            {
                if ( _server.HttpListenerConfiguration.HttpListener.IsListening )
                {
                    _server.HttpListenerConfiguration.HttpListener.Stop();
                }
            }

            private string[] ValidateUrls( string urls )
            {
                if ( string.IsNullOrWhiteSpace( urls ) )
                    throw new Exception( "No URIs were supplied" );

                string[] urlArray = urls.Split( ',' );

                var list = new List<string>();

                foreach ( var url in urlArray )
                {
                    var correctedUrl = url.Replace( "\\", "" ).Replace( "\"", "" );

                    try
                    {
                        var uri = new Uri( correctedUrl ); // Validate that the URL is valid
                        list.Add( uri.ToString() );
                    }
                    catch ( Exception ex )
                    {
                        throw new Exception( "The following URI is not valid: " + correctedUrl + " Exception: " + ex.Message );
                    }
                }

                return list.ToArray();
            }
        }
    }
}