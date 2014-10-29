using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Nano.Core;
using Newtonsoft.Json;
using Owin;

namespace Nano.Host
{
    /// <summary>
    /// Nano Server.
    /// </summary>
    public static class NanoServer
    {
        /// <summary>
        /// Starts the server with the given configuration.
        /// </summary>
        /// <param name="configuration"></param>
        public static void Start( NanoServerConfiguration configuration )
        {
            // hack: problems with Microsoft.Owin.Host.HttpListener.dll being copied to destination project
            Microsoft.Owin.Host.HttpListener.OwinHttpListener i = null;

            // Start the static file servers
            foreach ( var staticFileServer in configuration.StaticFileServers )
            {
                WebApp.Start( configuration.BaseUrl + staticFileServer.WebPath, options =>
                {
                    options.UseFileServer( new FileServerOptions
                    {
                        EnableDirectoryBrowsing = true,
                        FileSystem = new PhysicalFileSystem( staticFileServer.FileSystemPath )
                    } );
                } );
            }

            // Start the background tasks
            foreach ( var backgroundTask in configuration.BackgroundTasks )
            {
                Task.Factory.StartNew( () =>
                {
                    while ( true )
                    {
                        try
                        {
                            if ( backgroundTask.AllowOverlappingRuns )
                            {
                                // Start asynchronously
                                Task.Factory.StartNew( () => { backgroundTask.Task.Invoke(); } );
                            }
                            else
                            {
                                // Start synchronously
                                backgroundTask.Task.Invoke();
                            }
                        }
                        catch
                        {
                            /* //todo: call task error logging action */
                        } // just eat it - parent process is INDESTRUCTABLE (hopefully)

                        Thread.Sleep( backgroundTask.MillisecondInterval );
                    }
                } );
            }

            // Start the web apis
            foreach ( var webApi in configuration.WebApis )
            {
                // By convention use the type name if the web path was not supplied
                if ( webApi.WebPath == null )
                {
                    webApi.WebPath = webApi.ApiType.UnderlyingSystemType.Name;
                }

                WebApp.Start( configuration.BaseUrl + webApi.WebPath, options => options.Run( owinContext =>
                {
                    NanoContext nanoContext = null;

                    try
                    {
                        // Map the OwinContext to a NanoContext
                        nanoContext = owinContext.Map( webApi );

                        // Invoke the Pre Invoke Event Handlers passing in the current NanoContext
                        webApi.InvokePreInvokeEventHandlers( nanoContext );

                        // Handle GetApiMetadata Requests
                        if ( nanoContext.Request.Uri.Segments.Last().ToLower().StartsWith( "getapimetadata" ) )
                        {
                            var metadata = MetadataGenerator.GenerateApiMetadata( webApi.ApiType );

                            nanoContext.Response.ResponseObject = metadata;

                            // Clear any errors for metadata requests
                            nanoContext.Response.Error = null;

                            webApi.InvokePostInvokeEventHandlers( nanoContext );

                            return nanoContext.ReturnAsJsonAsync( metadata, Formatting.Indented );
                        }

                        // Handle any errors that occurred when creating the NanoContext
                        // TODO: We need to decide whether we wish to guarantee a response or to return specific HTTP Status Codes.
                        if ( nanoContext.Response.Error != null && nanoContext.Response.Error.Exception != null )
                            throw nanoContext.Response.Error.Exception;

                        // Order the method parameters
                        var methodParameters = nanoContext.Request.MethodParameters
                            .OrderBy( x => x.ParameterInfo.Position )
                            .Select( x => x.MethodParameterValue )
                            .ToArray();

                        // Invoke the method. Note that void methods will return null.
                        var result = nanoContext.Request.MethodInfo.Invoke( webApi.ApiType, methodParameters );
                        
                        nanoContext.Response.ResponseObject = result;

                        // Invoke the Post Invoke Event Handlers passing in the current NanoContext
                        webApi.InvokePostInvokeEventHandlers( nanoContext );

                        if ( result == null )
                        {
                            // Return an already completed task
                            return Task.FromResult( false );
                        }

                        // Handle Stream responses
                        if ( nanoContext.Request.MethodInfo.ReturnType == typeof ( Stream ) )
                        {
                            return ( ( Stream ) result ).CopyToAsync( nanoContext.Response.Body );
                        }

                        // Write the results as JSON to the response body asynchronously and return the task
                        return nanoContext.ReturnAsJsonAsync( result );
                    }
                    catch ( Exception ex )
                    {
                        if ( nanoContext != null && nanoContext.Response != null )
                            nanoContext.Response.Error = new NanoError { ErrorMessage = ex.Message, Exception = ex };
                        try
                        {
                            // Invoke the Unhandled Exception Event Handlers passing in the exception and the current NanoContext
                            webApi.InvokeUnhandledExceptionEventHandlers( ex, nanoContext );
                        }
                        catch ( Exception e )
                        {
                            // Write the error as JSON to the response body asynchronously and return the task
                            return nanoContext.ReturnAsJsonAsync( new { Error = e.Message } );
                        }

                        // Write the error as JSON to the response body asynchronously and return the task
                        return nanoContext.ReturnAsJsonAsync( new { Error = ex.Message } );
                    }
                } ) );
            }
        }
    }
}