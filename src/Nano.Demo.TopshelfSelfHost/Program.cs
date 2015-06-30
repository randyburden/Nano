using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Nano.Web.Core;
using Nano.Web.Core.Host.HttpListener;
using Topshelf;

namespace Nano.Demo.TopshelfSelfHost
{
    /// <summary>
    /// To install as a Windows Service, run this from the command prompt as an Administrator:
    /// Nano.Demo.TopshelfSelfHost.exe install -servicename:NanoDemo -uri:http://localhost:4545
    /// Nano.Demo.TopshelfSelfHost.exe uninstall -servicename:Your.Application.Name
    /// </summary>
    class Program
    {
        static void Main( string[] args )
        {
            string uris = "http://localhost:4545";

            HostFactory.Run( x =>
            {
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

        public class Startup
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

                var config = new NanoConfiguration();

                config.AddDirectory( "/", "www", null, true );
                config.AddMethods<Customer>( "/api/customer/" );
                config.AddFunc( "/hi", context => "Hello World!" );

                config.GlobalEventHandler.UnhandledExceptionHandlers.Add( ( exception, context ) =>
                {
                    try
                    {
                        if ( !EventLog.SourceExists( _applicationName ) )
                            EventLog.CreateEventSource( _applicationName, "Application" );

                        var msg = new StringBuilder()
                            .AppendLine( "Nano Error:" )
                            .AppendLine( "-----------" ).AppendLine()
                            .AppendLine( "URL: " + context.Request.Url ).AppendLine()
                            .AppendLine( "Message: " + exception.Message ).AppendLine()
                            .AppendLine( "StackTrace:" )
                            .AppendLine( exception.StackTrace )
                            .ToString();

                        EventLog.WriteEntry( _applicationName, msg, EventLogEntryType.Error );
                    }
                    catch ( Exception )
                    {
                        // Gulp: Never throw an exception in the unhandled exception handler
                    }
                } );

                _server = HttpListenerNanoServer.Start( config, validatedUrls );
                _server.HttpListenerConfiguration.ApplicationPath = "YourOptionalVirtualAppPathName";
                
                if( Debugger.IsAttached )
                    Process.Start( _server.HttpListenerConfiguration.GetFirstUrlBeingListenedOn().TrimEnd( '/' ) + "/ApiExplorer" );

                Console.WriteLine( "Nano Server is running on: " + _server.HttpListenerConfiguration.GetFirstUrlBeingListenedOn() );
            }

            public void Stop()
            {
                if( _server.HttpListenerConfiguration.HttpListener.IsListening )
                {
                    _server.HttpListenerConfiguration.HttpListener.Stop();
                }
            }

            private string[] ValidateUrls( string urls )
            {
                if( string.IsNullOrWhiteSpace( urls ) )
                    throw new Exception( "No URIs were supplied" );

                string[] urlArray = urls.Split( ',' );

                var list = new List<string>();

                foreach( var url in urlArray )
                {
                    var correctedUrl = url.Replace( "\\", "" ).Replace( "\"", "" );

                    try
                    {
                        var uri = new Uri( correctedUrl ); // Validate that the URL is valid
                        list.Add( uri.ToString() );
                    }
                    catch( Exception ex )
                    {
                        throw new Exception( "The following URI is not valid: " + correctedUrl + " Exception: " + ex.Message );
                    }
                }

                return list.ToArray();
            }
        }
    }
}