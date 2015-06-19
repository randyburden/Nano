/*
    Nano v0.7.0
    
    Nano is a micro web framework for building web-based HTTP services for .NET.
    To find out more, visit the project home page at: 
    https://github.com/AmbitEnergyLabs/Nano

    The MIT License (MIT)

    Copyright (c) 2015 Ambit Energy. All rights reserved.

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Nano.Web.Core.Internal;
using Nano.Web.Core.Metadata;
using Nano.Web.Core.RequestHandlers;
using Nano.Web.Core.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Formatting = Newtonsoft.Json.Formatting;
using TypeConverter = Nano.Web.Core.Internal.TypeConverter;

// ReSharper disable once CheckNamespace
namespace Nano.Web.Core
{
    #region Nano.Web.Core

    /// <summary>Nano configuration.</summary>
    public class NanoConfiguration
    {
        /// <summary>The host applications root folder path.</summary>
        public string ApplicationRootFolderPath;

        /// <summary>
        /// The default event handlers to apply to each added <see cref="IRequestHandler" /> if one is not supplied.
        /// </summary>
        public EventHandler DefaultEventHandler = new EventHandler();

        /// <summary>The default metadata provider for <see cref="MethodRequestHandler" />s.</summary>
        public MethodRequestHandlerMetadataProvider DefaultMethodRequestHandlerMetadataProvider = new MethodRequestHandlerMetadataProvider();

        /// <summary>The default background task event handler to apply to all tasks if one is not specified.</summary>
        public BackgroundTaskEventHandler DefaultBackgroundTaskEventHandler = new BackgroundTaskEventHandler();

        /// <summary>The global event handlers that will be invoked for all <see cref="IRequestHandler" />s.</summary>
        public EventHandler GlobalEventHandler = new EventHandler();

        /// <summary>The underlying HTTP host.</summary>
        public dynamic Host;

        /// <summary>The request handlers.</summary>
        public IList<IRequestHandler> RequestHandlers = new List<IRequestHandler>();

        /// <summary>The background tasks</summary>
        public IList<BackgroundTask> BackgroundTasks = new List<BackgroundTask>();

        /// <summary>The serialization service used to serialize/deserialize requests and responses.</summary>
        public ISerializationService SerializationService;

        /// <summary>Gets the default method url path. Defaulted to: '/api/' + type.Name</summary>
        public Func<Type, string> GetDefaultMethodUrlPath = type => "/api/" + type.Name;

        /// <summary>Initializes a new instance of the <see cref="NanoConfiguration" /> class.</summary>
        /// <param name="serializationService">
        /// The optional serialization service used to serialize/deserialize requests and
        /// responses.
        /// </param>
        public NanoConfiguration( ISerializationService serializationService = null )
        {
            SerializationService = serializationService ?? new JsonNetSerializer();
            RequestHandlers.Add( new MetadataRequestHandler( "/metadata/GetNanoMetadata", DefaultEventHandler ) );
            this.EnableCorrelationId();
        }

        /// <summary>Adds all public static methods in the given type.</summary>
        /// <param name="type">The type to scan methods for.</param>
        /// <param name="urlPath">The URL path.</param>
        /// <param name="eventHandler">The event handlers to invoke on requests.</param>
        /// <param name="metadataProvider">The metadata provider.</param>
        /// <returns>List of <see cref="MethodRequestHandler" />s.</returns>
        public IList<MethodRequestHandler> AddMethods( Type type, string urlPath = null, EventHandler eventHandler = null, IApiMetaDataProvider metadataProvider = null )
        {
            MethodInfo[] methods = type.GetMethods( BindingFlags.Public | BindingFlags.Static );
            urlPath = string.IsNullOrWhiteSpace( urlPath ) == false ? urlPath.TrimStart( '/' ).TrimEnd( '/' ) : GetDefaultMethodUrlPath( type ).TrimStart( '/' ).TrimEnd( '/' );

            IList<MethodRequestHandler> handlers = new List<MethodRequestHandler>();

            foreach( MethodInfo methodInfo in methods )
            {
                string methodUrlPath = string.Format( "/{0}/{1}", urlPath, methodInfo.Name ).ToLower();
                var requestHandler = new MethodRequestHandler( methodUrlPath, eventHandler ?? DefaultEventHandler, methodInfo ) { MetadataProvider = metadataProvider ?? DefaultMethodRequestHandlerMetadataProvider };
                RequestHandlers.Add( requestHandler );
                handlers.Add( requestHandler );
            }

            return handlers;
        }

        /// <summary>Adds all public static methods in the given type.</summary>
        /// <typeparam name="T">The type to scan methods for.</typeparam>
        /// <param name="urlPath">The URL path.</param>
        /// <param name="eventHandler">The event handlers to invoke on requests.</param>
        /// <param name="metadataProvider">The metadata provider.</param>
        /// <returns>List of <see cref="MethodRequestHandler" />s.</returns>
        public IList<MethodRequestHandler> AddMethods<T>( string urlPath = null, EventHandler eventHandler = null, IApiMetaDataProvider metadataProvider = null )
        {
            return AddMethods( typeof( T ), urlPath, eventHandler, metadataProvider );
        }

        /// <summary>Adds a Func.</summary>
        /// <param name="urlPath">The URL path.</param>
        /// <param name="func">The function to invoke.</param>
        /// <param name="eventHandler">The event handlers to invoke on requests.</param>
        /// <param name="metadataProvider">The metadata provider.</param>
        /// <returns><see cref="FuncRequestHandler" />.</returns>
        public FuncRequestHandler AddFunc( string urlPath, Func<NanoContext, object> func, EventHandler eventHandler = null, IApiMetaDataProvider metadataProvider = null )
        {
            urlPath = "/" + urlPath.TrimStart( '/' ).TrimEnd( '/' );
            var handler = new FuncRequestHandler( urlPath, eventHandler ?? DefaultEventHandler, func );
            RequestHandlers.Add( handler );
            return handler;
        }

        /// <summary>Adds the file.</summary>
        /// <param name="urlPath">The URL path.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="eventHandler">The event handlers.</param>
        /// <returns><see cref="FileRequestHandler" />.</returns>
        public FileRequestHandler AddFile( string urlPath, string filePath, EventHandler eventHandler = null )
        {
            var handler = new FileRequestHandler( urlPath, eventHandler ?? DefaultEventHandler, filePath );
            RequestHandlers.Add( handler );
            return handler;
        }

        /// <summary>Adds the directory.</summary>
        /// <param name="urlPath">The URL path.</param>
        /// <param name="directoryPath">The directory path.</param>
        /// <param name="eventHandler">The event handlers.</param>
        /// <param name="returnHttp404WhenFileWasNoFound">Should return an 'HTTP 404 - File Not Found' when no file was found.</param>
        /// <param name="defaultDocuments">
        /// The default documents to serve when the root of a directory is requested. Default is
        /// index.html.
        /// </param>
        /// <returns><see cref="DirectoryRequestHandler" />.</returns>
        public DirectoryRequestHandler AddDirectory( string urlPath, string directoryPath, EventHandler eventHandler = null, bool returnHttp404WhenFileWasNoFound = false, IList<string> defaultDocuments = null )
        {
            var handler = new DirectoryRequestHandler( urlPath, eventHandler ?? DefaultEventHandler, directoryPath, returnHttp404WhenFileWasNoFound, defaultDocuments );
            RequestHandlers.Add( handler );
            return handler;
        }

        /// <summary>
        /// Adds the background task.
        /// </summary>
        /// <param name="taskName">The task name.</param>
        /// <param name="millisecondInterval">The millisecond interval.</param>
        /// <param name="task">The task which takes no parameters and returns a result of type <see cref="object"/>.</param>
        /// <param name="allowOverlappingRuns">If set to <c>true</c> [allow overlapping runs].</param>
        /// <param name="backgroundTaskEventHandler">The background task event handler.</param>
        /// <returns></returns>
        public BackgroundTask AddBackgroundTask( string taskName, int millisecondInterval, Func<object> task, bool allowOverlappingRuns = false, BackgroundTaskEventHandler backgroundTaskEventHandler = null )
        {
            var backgroundTask = new BackgroundTask { Name = taskName, MillisecondInterval = millisecondInterval, Task = task, AllowOverlappingRuns = allowOverlappingRuns, BackgroundTaskEventHandler = backgroundTaskEventHandler ?? DefaultBackgroundTaskEventHandler };
            BackgroundTasks.Add( backgroundTask);
            return backgroundTask;
        }
    }

    /// <summary>The context of the current web request.</summary>
    public class NanoContext : IDisposable
    {
        /// <summary>The current user.</summary>
        public IUserIdentity CurrentUser;

        /// <summary>The errors for this context.</summary>
        public IList<Exception> Errors = new List<Exception>();

        /// <summary>Indicates if the request has been handled.</summary>
        public bool Handled;

        /// <summary>The underlying HTTP host context.</summary>
        /// <remarks>
        /// This enables accessing host-only features although this will make code dependent on this object non-host agnostic. The
        /// main intent of this is to enable features that are not currently supported by Nano.Web directly.
        /// </remarks>
        public object HostContext;

        /// <summary>Per-request storage. All IDisposable items will be disposed of when the context is.</summary>
        public IDictionary<string, object> Items = new DynamicDictionary();

        /// <summary>The <see cref="NanoConfiguration" />.</summary>
        public NanoConfiguration NanoConfiguration;

        /// <summary>The incoming request.</summary>
        public NanoRequest Request;

        /// <summary>The <see cref="IRequestHandler" /> for this context.</summary>
        public IRequestHandler RequestHandler;

        /// <summary>The outgoing response.</summary>
        public NanoResponse Response;

        /// <summary>The root folder path.</summary>
        /// <value>The root folder path.</value>
        public string RootFolderPath;
        
        /// <summary>Initializes a new instance of the <see cref="NanoContext" /> class.</summary>
        /// <param name="nanoRequest">The nano request.</param>
        /// <param name="nanoResponse">The nano response.</param>
        /// <param name="nanoConfiguration">The nano configuration.</param>
        public NanoContext( NanoRequest nanoRequest, NanoResponse nanoResponse, NanoConfiguration nanoConfiguration )
        {
            nanoRequest.NanoContext = this;
            Request = nanoRequest;

            nanoResponse.NanoContext = this;
            Response = nanoResponse;

            NanoConfiguration = nanoConfiguration;
        }

        /// <summary>Disposes any disposable items in the <see cref="Items" /> dictionary.</summary>
        public void Dispose()
        {
            foreach( IDisposable disposableItem in Items.Values.OfType<IDisposable>() )
                disposableItem.Dispose();

            Items.Clear();
        }
    }

    /// <summary>Holds properties associated with the current HTTP request.</summary>
    public class NanoRequest
    {
        private readonly Func<Stream> _requestBodyAccessor;

        /// <summary>The HTTP form body parameters sent by the client.</summary>
        public NameValueCollection FormBodyParameters;

        /// <summary>The HTTP header parameters sent by the client.</summary>
        public NameValueCollection HeaderParameters;

        /// <summary>HTTP Method ( GET, POST, HEAD, etc. ).</summary>
        public string HttpMethod;

        /// <summary>The Nano context.</summary>
        public NanoContext NanoContext;

        /// <summary>The HTTP query string parameters sent by the client..</summary>
        public NameValueCollection QueryStringParameters;

        /// <summary>Full URL being requested.</summary>
        public Url Url;
        
        /// <summary>Initializes a new instance of the <see cref="NanoRequest" /> class.</summary>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <param name="url">The URL being requested.</param>
        /// <param name="requestBodyAccessor">A function that returns the request body stream.</param>
        public NanoRequest( string httpMethod, Url url, Func<Stream> requestBodyAccessor )
        {
            HttpMethod = httpMethod;
            Url = url;
            _requestBodyAccessor = requestBodyAccessor;
        }

        /// <summary>Gets the request body.</summary>
        /// <value>The request body.</value>
        public Stream RequestBody
        {
            get { return _requestBodyAccessor(); }
        }
    }

    /// <summary>Holds properties associated with the current HTTP response.</summary>
    public class NanoResponse
    {
        /// <summary>The HTTP character set of the current response.</summary>
        public string Charset = "utf-8";

        /// <summary>The content encoding of the current response.</summary>
        public Encoding ContentEncoding = Encoding.UTF8;

        /// <summary>The HTTP MIME type of the current response.</summary>
        public string ContentType = string.Empty;

        /// <summary>The cookies.</summary>
        public IList<NanoCookie> Cookies = new List<NanoCookie>();

        /// <summary>The header parameters.</summary>
        public NameValueCollection HeaderParameters = new NameValueCollection();

        /// <summary>The HTTP status code of the output returned to the client.</summary>
        public int HttpStatusCode = Constants.HttpStatusCode.Ok.ToInt();

        /// <summary>The <see cref="NanoContext" />.</summary>
        public NanoContext NanoContext;

        /// <summary>The response object to be serialized and written to the response stream.</summary>
        public object ResponseObject;

        /// <summary>The action that will write to the response body stream.</summary>
        public Action<Stream> ResponseStreamWriter;

        /// <summary>Initializes a new instance of the <see cref="NanoResponse" /> class.</summary>
        public NanoResponse()
        {
            HeaderParameters.Add( "X-Nano-Version", Constants.Version );
        }

        /// <summary>
        /// Permanently redirects to the specified URL. This should almost always be used over a temporary redirect.
        /// </summary>
        /// <param name="url">The URL.</param>
        public void Redirect( string url )
        {
            NanoContext.Handled = true;
            HttpStatusCode = Constants.HttpStatusCode.MovedPermanently.ToInt();
            HeaderParameters["Location"] = url;
        }

        /// <summary>
        /// Temporarily redirects to the specified URL. This should be rarely used with a permanent redirect strongly being
        /// recommended.
        /// </summary>
        /// <param name="url">The URL.</param>
        public void TemporaryRedirect( string url )
        {
            HttpStatusCode = Constants.HttpStatusCode.FoundOrMovedTemporarily.ToInt();
            HeaderParameters["Location"] = url;
        }
    }

    /// <summary>Represents an HTTP Cookie.</summary>
    public class NanoCookie
    {
        /// <summary>Initializes a new instance of a <see cref="NanoCookie" />.</summary>
        /// <param name="name">The name of the cookie.</param>
        /// <param name="value">The value of the cookie.</param>
        /// <param name="path">The cookie path.</param>
        /// <param name="httpOnly">Whether a cookie is accessible by client-side script.</param>
        /// <param name="secure">Whether the cookie is secure ( HTTPS only ).</param>
        /// <param name="expires">
        /// The expiration date of the cookie. Can be <see langword="null" /> if it should expire at the end of the session.
        /// </param>
        public NanoCookie( string name, string value, string path = "/", bool httpOnly = false, bool secure = false, DateTime? expires = null )
        {
            Name = name;
            Value = value;
            Path = path;
            HttpOnly = httpOnly;
            Secure = secure;
            Expires = expires;
        }

        /// <summary>The domain to restrict the cookie to</summary>
        public string Domain { get; set; }

        /// <summary>When the cookie should expire</summary>
        /// <value>
        /// A <see cref="DateTime" /> instance containing the date and time when the cookie should expire; otherwise
        /// <see langword="null" /> if it should expire at the end of the session.
        /// </value>
        public DateTime? Expires { get; set; }

        /// <summary>The name of the cookie</summary>
        public string Name { get; private set; }
        
        /// <summary>The path to restrict the cookie to</summary>
        public string Path { get; set; }

        /// <summary>The value of the cookie</summary>
        public string Value { get; private set; }
        
        /// <summary>Whether a cookie is accessible by client-side script.</summary>
        public bool HttpOnly { get; private set; }

        /// <summary>Whether the cookie is secure ( HTTPS only )</summary>
        public bool Secure { get; private set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder( 50 ).AppendFormat( "{0}={1}; path={2}", Name, Value, Path );

            if( Expires != null )
            {
                sb.Append( "; expires=" );
                sb.Append( Expires.Value.ToUniversalTime().ToString( "ddd, dd-MMM-yyyy HH:mm:ss", DateTimeFormatInfo.InvariantInfo ) );
                sb.Append( " GMT" );
            }

            if( Domain != null )
                sb.Append( "; domain=" ).Append( Domain );

            if( Secure )
                sb.Append( "; Secure" );

            if( HttpOnly )
                sb.Append( "; HttpOnly" );

            return sb.ToString();
        }
    }

    /// <summary>Provides helper methods for CORS ( Cross-origin resource sharing ) requests.</summary>
    public static class CorsHelper
    {
        /// <summary>Enables CORS ( Cross-origin resource sharing ) requests.</summary>
        /// <param name="nanoConfiguration">The nano configuration.</param>
        /// <param name="allowedOrigin">The allowed origin.</param>
        public static void EnableCors( this NanoConfiguration nanoConfiguration, string allowedOrigin = "*" )
        {
            nanoConfiguration.GlobalEventHandler.EnableCors( allowedOrigin );
        }
        
        /// <summary>Enables CORS ( Cross-origin resource sharing ) requests.</summary>
        /// <param name="eventHandler">The event handler.</param>
        /// <param name="allowedOrigin">The allowed origin.</param>
        public static void EnableCors( this EventHandler eventHandler, string allowedOrigin = "*" )
        {
            eventHandler.PreInvokeHandlers.Add( context =>
            {
                context.Response.HeaderParameters.Add( "Access-Control-Allow-Origin", allowedOrigin );

                if ( context.Request.HttpMethod == "OPTIONS" )
                {
                    context.Response.HeaderParameters.Add( "Access-Control-Allow-Methods", "GET, PUT, POST, DELETE, HEAD, OPTIONS" );
                    context.Response.HeaderParameters.Add( "Access-Control-Max-Age", "86400" ); // cache for 1 day
                    context.Response.HttpStatusCode = Constants.HttpStatusCode.NoContent.ToInt();
                    context.Response.ContentType = "text/plain";
                    context.Handled = true;
                }
            } );
        }
    }

    /// <summary>Enables CorrelationId support which passes through or creates new CorrelationIds per request in order to support request tracking.</summary>
    public static class CorrelationIdHelper
    {
        /// <summary>Enables CorrelationId support which passes through or creates new CorrelationIds per request in order to support request tracking.</summary>
        /// <param name="nanoConfiguration">The nano configuration.</param>
        public static void EnableCorrelationId( this NanoConfiguration nanoConfiguration )
        {
            nanoConfiguration.GlobalEventHandler.EnableCorrelationId();
        }

        /// <summary>Disables CorrelationId support which passes through or creates new CorrelationIds per request in order to support request tracking.</summary>
        /// <param name="nanoConfiguration">The nano configuration.</param>
        public static void DisableCorrelationId( this NanoConfiguration nanoConfiguration )
        {
            nanoConfiguration.GlobalEventHandler.DisableCorrelationId();
        }

        /// <summary>Enables CorrelationId support which passes through or creates new CorrelationIds per request in order to support request tracking.</summary>
        /// <param name="eventHandler">The event handler.</param>
        public static void EnableCorrelationId( this EventHandler eventHandler )
        {
            if ( eventHandler.PreInvokeHandlers.Contains( EnableCorrelationIdPreInvokeHandler ) == false )
                eventHandler.PreInvokeHandlers.Add( EnableCorrelationIdPreInvokeHandler );
        }

        /// <summary>Disables CorrelationId support which passes through or creates new CorrelationIds per request in order to support request tracking.</summary>
        /// <param name="eventHandler">The event handler.</param>
        public static void DisableCorrelationId( this EventHandler eventHandler )
        {
            eventHandler.PreInvokeHandlers.Remove( EnableCorrelationIdPreInvokeHandler );
        }

        /// <summary>Enables CorrelationId support which passes through or creates new CorrelationIds per request in order to support request tracking.</summary>
        /// <param name="nanoContext">The nano context.</param>
        public static void EnableCorrelationIdPreInvokeHandler( NanoContext nanoContext )
        {
            var correlationId = nanoContext.GetRequestParameterValue( Constants.CorrelationIdRequestParameterName );

            if ( string.IsNullOrWhiteSpace( correlationId ) )
                correlationId = Guid.NewGuid().ToString();

            nanoContext.Response.HeaderParameters.Add( Constants.CorrelationIdRequestParameterName, correlationId );
            nanoContext.Items.Add( Constants.CorrelationIdRequestParameterName, correlationId );
            System.Runtime.Remoting.Messaging.CallContext.LogicalSetData( Constants.CorrelationIdRequestParameterName, correlationId );
        }
    }

    /// <summary>Global constant configuration.</summary>
    public static class Constants
    {
        /// <summary>HTTP Status Codes</summary>
        public enum HttpStatusCode
        {
            /// <summary>200 OK</summary>
            Ok = 200,

            /// <summary>204 No Content</summary>
            NoContent = 204,

            /// <summary>301 MovedPermanently</summary>
            MovedPermanently = 301,

            /// <summary>301 Found/MovedPermanently</summary>
            FoundOrMovedTemporarily = 301,

            /// <summary>304 NotModified</summary>
            NotModified = 304,

            /// <summary>404 NotFound</summary>
            NotFound = 404,

            /// <summary>500 InternalServerError</summary>
            InternalServerError = 500
        }

        /// <summary>The version number of Nano.Web.Core.</summary>
        public static readonly string Version;

        /// <summary>
        /// Provides a platform-specific character used to separate directory levels in a path string that reflects a hierarchical
        /// file system organization.
        /// </summary>
        public static readonly char DirectorySeparatorChar = Path.DirectorySeparatorChar;

        /// <summary>
        /// Provides a platform-specific character used to separate directory levels in a path string that reflects a hierarchical
        /// file system organization.
        /// </summary>
        public static readonly string DirectorySeparatorString = Path.DirectorySeparatorChar.ToString();

        /// <summary>Default file buffer size for returning file. Default size is 4 Megabytes.</summary>
        public static int DefaultFileBufferSize = 1024 * 1024 * 4;

        /// <summary>CorrelationId request parameter name.</summary>
        public static string CorrelationIdRequestParameterName = "X-CorrelationId";

        static Constants()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            if( assembly.FullName.Contains( "Nano.Web.Core" ) )
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo( assembly.Location );
                Version = fvi.FileVersion;
            }
            else
            {
                Version = "0.7.0.0";
            }
        }

        /// <summary>Custom error responses.</summary>
        public static class CustomErrorResponse
        {
            /// <summary>404 Not Found custom error response.</summary>
            public static string NotFound404 = "<html><head><title>Page Not Found</title></head><body><h3>Page Not Found: Error 404</h3><p>Oops, the page you requested was not found.</p></body></html>";

            /// <summary>500 Internal Server Error custom error response.</summary>
            public static string InternalServerError500 = "<html><head><title>Internal Server Error</title></head><body><h3>Internal Server Error: Error 500</h3><p>Oops, an internal error occurred.</p></body></html>";
        }
    }

    /// <summary>Event handler configuration.</summary>
    public class EventHandler
    {
        /// <summary>Event handler triggered when an unhandled exception occurs.</summary>
        /// <param name="exception">Unhandled exception.</param>
        /// <param name="nanoContext">Current NanoContext.</param>
        public delegate void ErrorHandler( Exception exception, NanoContext nanoContext );

        /// <summary>Event handler triggered after the target method has been invoked and returned a response.</summary>
        /// <param name="nanoContext">Current NanoContext.</param>
        public delegate void PostInvokeHandler( NanoContext nanoContext );

        /// <summary>Event handler triggered before the target method is invoked.</summary>
        /// <param name="nanoContext">Current NanoContext.</param>
        public delegate void PreInvokeHandler( NanoContext nanoContext );

        /// <summary>Handlers triggered after the target method has been invoked and returned a response.</summary>
        public IList<PostInvokeHandler> PostInvokeHandlers = new List<PostInvokeHandler>();

        /// <summary>Handlers triggered before the target method is invoked.</summary>
        public IList<PreInvokeHandler> PreInvokeHandlers = new List<PreInvokeHandler>();

        /// <summary>Handlers triggered when an unhandled exception occurs.</summary>
        public IList<ErrorHandler> UnhandledExceptionHandlers = new List<ErrorHandler>();

        /// <summary>Invokes the unhandled exception handlers.</summary>
        /// <param name="exception">Unhandled exception.</param>
        /// <param name="nanoContext">Current NanoContext.</param>
        public void InvokeUnhandledExceptionHandlers( Exception exception, NanoContext nanoContext )
        {
            foreach( ErrorHandler unhandledExceptionHandler in UnhandledExceptionHandlers )
            {
                unhandledExceptionHandler.Invoke( exception, nanoContext );
            }
        }

        /// <summary>Invokes the pre-invoke handlers.</summary>
        /// <param name="nanoContext">Current NanoContext.</param>
        public void InvokePreInvokeHandlers( NanoContext nanoContext )
        {
            foreach( PreInvokeHandler preInvokeHandler in PreInvokeHandlers )
            {
                preInvokeHandler.Invoke( nanoContext );
            }
        }

        /// <summary>Invokes the post-invoke handlers.</summary>
        /// <param name="nanoContext">Current NanoContext.</param>
        public void InvokePostInvokeHandlers( NanoContext nanoContext )
        {
            foreach( PostInvokeHandler postInvokeHandler in PostInvokeHandlers )
            {
                postInvokeHandler.Invoke( nanoContext );
            }
        }
    }

    /// <summary>Background task.</summary>
    public class BackgroundTask
    {
        /// <summary>Background task name.</summary>
        public string Name;

        /// <summary>Background task.</summary>
        public Func<object> Task;

        /// <summary>The millisecond interval to wait between each background task run.</summary>
        public int MillisecondInterval;

        /// <summary>Indicates whether to allow overlapping runs between each background task run. Default is false.</summary>
        public bool AllowOverlappingRuns;

        /// <summary>The background task event handler.</summary>
        public BackgroundTaskEventHandler BackgroundTaskEventHandler;

        /// <summary>The background task context history for the last n number of runs.</summary>
        public BackgroundTaskContext[] BackgroundTaskRunHistory = new BackgroundTaskContext[10];

        /// <summary>The current background task history position.</summary>
        public int CurrentBackgroundTaskHistoryPosition;
    }

    /// <summary>Background task context.</summary>
    public class BackgroundTaskContext
    {
        /// <summary>The current background task.</summary>
        public BackgroundTask BackgroundTask;

        /// <summary>The nano configuration.</summary>
        public NanoConfiguration NanoConfiguration;

        /// <summary>The start date time.</summary>
        public DateTime StartDateTime;

        /// <summary>The end date time</summary>
        public DateTime EndDateTime;

        /// <summary>The task result.</summary>
        public object TaskResult;

        /// <summary>The task exception if one occurred.</summary>
        public Exception TaskException;
    }

    /// <summary>Background task event handler configuration.</summary>
    public class BackgroundTaskEventHandler
    {
        /// <summary>Event handler triggered when an unhandled exception occurs.</summary>
        /// <param name="exception">Unhandled exception.</param>
        /// <param name="backgroundTaskContext">Current <see cref="BackgroundTaskContext" />.</param>
        public delegate void ErrorHandler( Exception exception, BackgroundTaskContext backgroundTaskContext );

        /// <summary>Event handler triggered after the background task has been invoked and returned a response.</summary>
        /// <param name="backgroundTaskContext">Current <see cref="BackgroundTaskContext" />.</param>
        public delegate void PostInvokeHandler( BackgroundTaskContext backgroundTaskContext );

        /// <summary>Event handler triggered before the background task is invoked.</summary>
        /// <param name="backgroundTaskContext">Current <see cref="BackgroundTaskContext" />.</param>
        public delegate void PreInvokeHandler( BackgroundTaskContext backgroundTaskContext );

        /// <summary>Handlers triggered after the background task has been invoked and returned a response.</summary>
        public IList<PostInvokeHandler> PostInvokeHandlers = new List<PostInvokeHandler>();

        /// <summary>Handlers triggered before the background task is invoked.</summary>
        public IList<PreInvokeHandler> PreInvokeHandlers = new List<PreInvokeHandler>();

        /// <summary>Handlers triggered when an unhandled exception occurs.</summary>
        public IList<ErrorHandler> UnhandledExceptionHandlers = new List<ErrorHandler>();

        /// <summary>Invokes the unhandled exception handlers.</summary>
        /// <param name="exception">Unhandled exception.</param>
        /// <param name="backgroundTaskContext">Current <see cref="BackgroundTaskContext" />.</param>
        public void InvokeUnhandledExceptionHandlers( Exception exception, BackgroundTaskContext backgroundTaskContext )
        {
            foreach ( ErrorHandler unhandledExceptionHandler in UnhandledExceptionHandlers )
            {
                unhandledExceptionHandler.Invoke( exception, backgroundTaskContext );
            }
        }

        /// <summary>Invokes the pre-invoke handlers.</summary>
        /// <param name="backgroundTaskContext">Current <see cref="BackgroundTaskContext" />.</param>
        public void InvokePreInvokeHandlers( BackgroundTaskContext backgroundTaskContext )
        {
            foreach ( PreInvokeHandler preInvokeHandler in PreInvokeHandlers )
            {
                preInvokeHandler.Invoke( backgroundTaskContext );
            }
        }

        /// <summary>Invokes the post-invoke handlers.</summary>
        /// <param name="backgroundTaskContext">Current <see cref="BackgroundTaskContext" />.</param>
        public void InvokePostInvokeHandlers( BackgroundTaskContext backgroundTaskContext )
        {
            foreach ( PostInvokeHandler postInvokeHandler in PostInvokeHandlers )
            {
                postInvokeHandler.Invoke( backgroundTaskContext );
            }
        }
    }

    /// <summary>Background task runner.</summary>
    public static class BackgroundTaskRunner
    {
        /// <summary>Starts the background tasks defined in the Nano configuration.</summary>
        /// <param name="nanoConfiguration">The nano configuration.</param>
        public static void Start( NanoConfiguration nanoConfiguration )
        {
            foreach ( BackgroundTask backgroundTask in nanoConfiguration.BackgroundTasks )
            {
                BackgroundTask task = backgroundTask;
                Task.Factory.StartNew( () =>
                {
                    while ( true )
                    {
                        var backgroundTaskContext = new BackgroundTaskContext { BackgroundTask = task, NanoConfiguration = nanoConfiguration };

                        try
                        {
                            if( backgroundTaskContext.BackgroundTask.AllowOverlappingRuns )
                            {
                                // Run async
                                Task.Factory.StartNew( () => Run( backgroundTaskContext ) );
                            }
                            else
                            {
                                // Run sync
                                Run( backgroundTaskContext );
                            }
                        }
                        catch ( Exception )
                        {
                            // Big gulp
                        }

                        Thread.Sleep( backgroundTaskContext.BackgroundTask.MillisecondInterval );
                    }
                } );
            }
        }

        /// <summary>Runs the specified background task context.</summary>
        /// <param name="backgroundTaskContext">The background task context.</param>
        public static void Run( BackgroundTaskContext backgroundTaskContext )
        {
            try
            {
                backgroundTaskContext.StartDateTime = DateTime.Now;
                backgroundTaskContext.BackgroundTask.BackgroundTaskRunHistory[ backgroundTaskContext.BackgroundTask.CurrentBackgroundTaskHistoryPosition ] = backgroundTaskContext;
                backgroundTaskContext.BackgroundTask.CurrentBackgroundTaskHistoryPosition = ( backgroundTaskContext.BackgroundTask.CurrentBackgroundTaskHistoryPosition + 1 ) % backgroundTaskContext.BackgroundTask.BackgroundTaskRunHistory.Length;
                backgroundTaskContext.BackgroundTask.BackgroundTaskEventHandler.InvokePreInvokeHandlers( backgroundTaskContext );
                backgroundTaskContext.TaskResult = backgroundTaskContext.BackgroundTask.Task.Invoke();
            }
            catch ( Exception e )
            {
                backgroundTaskContext.TaskException = e;
                backgroundTaskContext.BackgroundTask.BackgroundTaskEventHandler.InvokeUnhandledExceptionHandlers( e, backgroundTaskContext );
            }
            finally
            {
                backgroundTaskContext.BackgroundTask.BackgroundTaskEventHandler.InvokePostInvokeHandlers( backgroundTaskContext );
                backgroundTaskContext.EndDateTime = DateTime.Now;
            }
        }
    }

    /// <summary>
    /// <see cref="NanoContext" /> extensions.
    /// </summary>
    public static class NanoContextExtensions
    {
        /// <summary>Writes the response object to the host's response stream.</summary>
        /// <param name="nanoContext">The nano context.</param>
        /// <param name="stream">The stream.</param>
        public static void WriteResponseObjectToStream( this NanoContext nanoContext, Stream stream )
        {
            // Handle null responses and void methods
            if( nanoContext.Response.ResponseObject == null )
                return;

            // Handle a Stream response object
            var streamResponse = nanoContext.Response.ResponseObject as Stream;

            if( streamResponse != null && streamResponse.Length >= 0 )
            {
                using( streamResponse )
                {
                    streamResponse.CopyTo( stream );
                    return;
                }
            }

            // Handle response object serialization
            string serializedResponse = nanoContext.NanoConfiguration.SerializationService.Serialize( nanoContext.Response.ResponseObject );
            stream.Write( serializedResponse );
        }

        /// <summary>Returns an 'HTTP 404 - NOT FOUND' to the client using the default 'NOT FOUND' HTML.</summary>
        /// <param name="nanoContext">The nano context.</param>
        public static void ReturnFileNotFound( this NanoContext nanoContext )
        {
            nanoContext.Handled = true;
            nanoContext.Response.HttpStatusCode = Constants.HttpStatusCode.NotFound.ToInt();
            nanoContext.Response.ContentType = "text/html";
            nanoContext.Response.ResponseStreamWriter = stream => stream.Write( Constants.CustomErrorResponse.NotFound404 );
        }

        /// <summary>
        /// Returns an 'HTTP 500 - INTERNAL SERVER ERROR' to the client using the default 'INTERNAL SERVER ERROR' HTML.
        /// </summary>
        /// <param name="nanoContext">The nano context.</param>
        public static void ReturnInternalServerError( this NanoContext nanoContext )
        {
            nanoContext.Handled = true;
            nanoContext.Response.HttpStatusCode = Constants.HttpStatusCode.InternalServerError.ToInt();
            nanoContext.Response.ResponseStreamWriter = nanoContext.WriteErrorsToStream;
        }

        /// <summary>Writes any errors to the response stream.</summary>
        /// <param name="nanoContext">The nano context.</param>
        /// <param name="stream">The stream.</param>
        public static void WriteErrorsToStream( this NanoContext nanoContext, Stream stream )
        {
            nanoContext.Response.ContentType = "text/html";

            if( Debugger.IsAttached )
            {
                string errorString = "<html><head><title>Internal Server Error</title></head><body><h3>Internal Server Error: Error 500</h3><p>Oops, an internal error occurred.</p>{{CustomErrorToken}}</body></html>";

                string customErrorToken = "";

                foreach( Exception exception in nanoContext.Errors )
                {
                    string msg = "<hr />";
                    msg += "<p><b>Error Message:</b></p>";
                    msg += "<p>" + exception.Message + "</p>";
                    msg += "<p><b>Inner Exception:</b></p>";
                    msg += "<p>" + exception.InnerException + "</p>";
                    msg += "<p><b>Stack Trace:</b></p>";
                    msg += "<p>" + exception.StackTrace + "</p>";

                    customErrorToken += msg;
                }

                errorString = errorString.Replace( "{{CustomErrorToken}}", customErrorToken );
                stream.Write( errorString );
            }
            else
            {
                stream.Write( Constants.CustomErrorResponse.InternalServerError500 );
            }
        }

        /// <summary>Returns a file if it exists.</summary>
        /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
        /// <param name="fileInfo">The file to return.</param>
        /// <returns>Returns true if the file exists else false.</returns>
        public static bool TryReturnFile( this NanoContext nanoContext, FileInfo fileInfo )
        {
            if( fileInfo.Exists )
            {
                nanoContext.Handled = true;
                nanoContext.Response.ContentType = FileExtensionToContentTypeConverter.GetContentType( fileInfo.Extension );

                nanoContext.Response.ResponseStreamWriter = stream =>
                {
                    using( FileStream file = fileInfo.OpenRead() )
                    {
                        file.CopyTo( stream, (int)( fileInfo.Length < Constants.DefaultFileBufferSize ? fileInfo.Length : Constants.DefaultFileBufferSize ) );
                    }
                };

                return true;
            }

            return false;
        }
    }

    /// <summary>Method invocation parameter.</summary>
    public class MethodParameter
    {
        /// <summary>Parameter description;</summary>
        public string Description;

        /// <summary>Indicates if the parameter type is dynamic.</summary>
        public bool IsDynamic;

        /// <summary>Indicates if the parameter is optional.</summary>
        public bool IsOptional;

        /// <summary>Parameter name.</summary>
        public string Name;

        /// <summary>The zero-based ordinal position of the parameter in the formal parameter list.</summary>
        public int Position;

        /// <summary>Parameter type.</summary>
        public Type Type;
    }

    /// <summary>Routes requests.</summary>
    public static class RequestRouter
    {
        /// <summary>Routes requests to the appropriate <see cref="RequestHandler" />.</summary>
        /// <param name="nanoContext">The <see cref="NanoContext" /> for the current request.</param>
        /// <returns>The <see cref="NanoContext" /> for the current request.</returns>
        public static NanoContext RouteRequest( NanoContext nanoContext )
        {
            string path = nanoContext.Request.Url.Path.ToLower();
            var requestHandlerMatches = new List<IRequestHandler>();

            foreach( IRequestHandler handler in nanoContext.NanoConfiguration.RequestHandlers )
            {
                // Look for exact match
                if( path.Equals( handler.UrlPath, StringComparison.InvariantCultureIgnoreCase ) )
                {
                    nanoContext.RequestHandler = handler;
                    return nanoContext.RequestHandler.ProcessRequest( nanoContext );
                }

                // Look for partial match
                if( path.StartsWith( handler.UrlPath, StringComparison.InvariantCultureIgnoreCase ) )
                {
                    requestHandlerMatches.Add( handler );
                }
            }

            foreach( IRequestHandler handler in requestHandlerMatches )
            {
                nanoContext.RequestHandler = handler;
                return nanoContext.RequestHandler.ProcessRequest( nanoContext );
            }

            return nanoContext;
        }
    }

    /// <summary>Binds a request parameter to a Type.</summary>
    public static class ModelBinder
    {
        /// <summary>Binds a request parameter to the requested type.</summary>
        /// <typeparam name="T">Type to bind to.</typeparam>
        /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
        /// <param name="parameterName">Name of the request parameter.</param>
        /// <returns>Instance of <typeparamref name="T" />.</returns>
        public static T Bind<T>( this NanoContext nanoContext, string parameterName )
        {
            string requestParameterValue = nanoContext.GetRequestParameterValue( parameterName );
            return nanoContext.NanoConfiguration.SerializationService.Deserialize<T>( requestParameterValue );
        }

        /// <summary>Binds a request parameter to the requested type.</summary>
        /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
        /// <param name="type">The type to bind to.</param>
        /// <param name="parameterName">Name of the request parameter.</param>
        /// <returns>The object of the requested type.</returns>
        public static object Bind( this NanoContext nanoContext, Type type, string parameterName )
        {
            string requestParameterValue = nanoContext.GetRequestParameterValue( parameterName );
            return nanoContext.NanoConfiguration.SerializationService.Deserialize( requestParameterValue, type );
        }

        /// <summary>
        /// Gets a request parameter string value given a NanoContext and the parameter name. By default this will return the first
        /// value found in the following sources in this order: query string, form body, headers.
        /// </summary>
        /// <param name="nanoContext">The nano context.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <returns></returns>
        public static string GetRequestParameterValue( this NanoContext nanoContext, string parameterName )
        {
            return nanoContext.Request.GetRequestParameterValue( parameterName );
        }

        /// <summary>
        /// Gets a request parameter string value given a NanoRequest and the parameter name. By default this will return the first
        /// value found in the following sources in this order: query string, form body, headers.
        /// </summary>
        /// <param name="nanoRequest">The nano request.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <returns></returns>
        public static string GetRequestParameterValue( this NanoRequest nanoRequest, string parameterName )
        {
            // Try to get the method parameter value from the request parameters
            string requestParameterValue = nanoRequest.QueryStringParameters.Get( parameterName ) ??
                                           nanoRequest.FormBodyParameters.Get( parameterName ) ??
                                           nanoRequest.HeaderParameters.Get( parameterName );

            return requestParameterValue;
        }
    }

    /// <summary>User identity.</summary>
    public interface IUserIdentity
    {
        /// <summary>The username of the authenticated user.</summary>
        string UserName { get; }

        /// <summary>The claims of the authenticated user.</summary>
        IEnumerable<string> Claims { get; }
    }

    /// <summary>
    /// Represents a full Url of the form scheme://hostname:port/basepath/path?query
    /// </summary>
    public sealed class Url : ICloneable
    {
        private string _basePath;
        private string _query;

        /// <summary>
        /// Creates an instance of the <see cref="Url" /> class
        /// </summary>
        public Url()
        {
            Scheme = Uri.UriSchemeHttp;
            HostName = string.Empty;
            Port = null;
            BasePath = string.Empty;
            Path = string.Empty;
            Query = string.Empty;
        }

        /// <summary>
        /// Creates an instance of the <see cref="Url" /> class
        /// </summary>
        /// <param name="url">A <see cref="string" /> containing a URL.</param>
        public Url( string url )
        {
            var uri = new Uri( url );
            HostName = uri.Host;
            Path = uri.LocalPath;
            Port = uri.Port;
            Query = uri.Query;
            Scheme = uri.Scheme;
        }

        /// <summary>
        /// Gets or sets the HTTP protocol used by the client.
        /// </summary>
        /// <value>The protocol.</value>
        public string Scheme { get; set; }

        /// <summary>
        /// Gets the host name of the request
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Gets the port name of the request
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Gets the base path of the request i.e. the application root
        /// </summary>
        public string BasePath
        {
            get { return _basePath; }
            set
            {
                if( string.IsNullOrEmpty( value ) )
                {
                    return;
                }

                _basePath = value.TrimEnd( '/' );
            }
        }

        /// <summary>
        /// Gets the path of the request, relative to the base path.
        /// This property drives the route matching.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets the query string.
        /// </summary>
        public string Query
        {
            get { return _query; }
            set { _query = GetQuery( value ); }
        }

        /// <summary>
        /// Gets the domain part of the request.
        /// </summary>
        public string SiteBase
        {
            get
            {
                return new StringBuilder()
                    .Append( Scheme )
                    .Append( Uri.SchemeDelimiter )
                    .Append( GetHostName( HostName ) )
                    .Append( GetPort( Port ) )
                    .ToString();
            }
        }

        /// <summary>
        /// Gets whether the URL is secure or not.
        /// </summary>
        public bool IsSecure
        {
            get
            {
                return Uri.UriSchemeHttps.Equals( Scheme, StringComparison.OrdinalIgnoreCase );
            }
        }

        public override string ToString()
        {
            return new StringBuilder()
                .Append( Scheme )
                .Append( Uri.SchemeDelimiter )
                .Append( GetHostName( HostName ) )
                .Append( GetPort( Port ) )
                .Append( GetCorrectPath( BasePath ) )
                .Append( GetCorrectPath( Path ) )
                .Append( Query )
                .ToString();
        }

        /// <summary>
        /// Clones the url.
        /// </summary>
        /// <returns>Returns a new cloned instance of the url.</returns>
        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// Clones the url.
        /// </summary>
        /// <returns>Returns a new cloned instance of the url.</returns>
        public Url Clone()
        {
            return new Url
            {
                BasePath = BasePath,
                HostName = HostName,
                Port = Port,
                Query = Query,
                Path = Path,
                Scheme = Scheme
            };
        }

        /// <summary>
        /// Casts the current <see cref="Url"/> instance to a <see cref="string"/> instance.
        /// </summary>
        /// <param name="url">The instance that should be cast.</param>
        /// <returns>A <see cref="string"/> representation of the <paramref name="url"/>.</returns>
        public static implicit operator string( Url url )
        {
            return url.ToString();
        }

        /// <summary>
        /// Casts the current <see cref="string"/> instance to a <see cref="Url"/> instance.
        /// </summary>
        /// <param name="url">The instance that should be cast.</param>
        /// <returns>An <see cref="Url"/> representation of the <paramref name="url"/>.</returns>
        public static implicit operator Url( string url )
        {
            return new Uri( url );
        }

        /// <summary>
        /// Casts the current <see cref="Url"/> instance to a <see cref="Uri"/> instance.
        /// </summary>
        /// <param name="url">The instance that should be cast.</param>
        /// <returns>An <see cref="Uri"/> representation of the <paramref name="url"/>.</returns>
        public static implicit operator Uri( Url url )
        {
            return new Uri( url.ToString(), UriKind.Absolute );
        }

        /// <summary>
        /// Casts a <see cref="Uri"/> instance to a <see cref="Url"/> instance
        /// </summary>
        /// <param name="uri">The instance that should be cast.</param>
        /// <returns>An <see cref="Url"/> representation of the <paramref name="uri"/>.</returns>
        public static implicit operator Url( Uri uri )
        {
            return new Url
            {
                HostName = uri.Host,
                Path = uri.LocalPath,
                Port = uri.Port,
                Query = uri.Query,
                Scheme = uri.Scheme
            };
        }

        private static string GetQuery( string query )
        {
            return string.IsNullOrEmpty( query ) ? string.Empty : ( query[0] == '?' ? query : '?' + query );
        }

        private static string GetCorrectPath( string path )
        {
            return ( string.IsNullOrEmpty( path ) || path.Equals( "/" ) ) ? string.Empty : path;
        }

        private static string GetPort( int? port )
        {
            return port.HasValue ? string.Concat( ":", port.Value ) : string.Empty;
        }

        private static string GetHostName( string hostName )
        {
            IPAddress address;

            if( IPAddress.TryParse( hostName, out address ) )
            {
                var addressString = address.ToString();

                return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                    ? string.Format( "[{0}]", addressString )
                    : addressString;
            }

            return hostName;
        }
    }

    /// <summary>
    /// <see cref="Stream" /> extensions.
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>Writes the given text to the stream using UTF-8.</summary>
        /// <param name="stream">Stream to write the text to.</param>
        /// <param name="text">The text to write.</param>
        public static void Write( this Stream stream, string text )
        {
            byte[] bytes = Encoding.UTF8.GetBytes( text );
            stream.Write( bytes, 0, bytes.Length );
        }
    }

    #endregion Nano.Web.Core

    #region Nano.Web.Core.Host.SystemWeb

    namespace Host.SystemWeb
    {
        /// <summary>Nano server for a System.Web hosted application.</summary>
        public class SystemWebNanoServer
        {
            /// <summary>The <see cref="NanoConfiguration" />.</summary>
            public NanoConfiguration NanoConfiguration;

            /// <summary>Starts the Nano server for a System.Web hosted application.</summary>
            /// <param name="httpApplication">System.Web.HttpApplication.</param>
            /// <param name="nanoConfiguration">The <see cref="NanoConfiguration" />.</param>
            public static void Start( dynamic httpApplication, NanoConfiguration nanoConfiguration )
            {
                nanoConfiguration.Host = httpApplication;
                nanoConfiguration.ApplicationRootFolderPath = httpApplication.Server.MapPath( "~/" );

                EventInfo eventInfo = httpApplication.GetType().GetEvent( "BeginRequest" );
                MethodInfo methodInfo = typeof( SystemWebNanoServer ).GetMethod( "HttpApplicationOnBeginRequest", BindingFlags.Public | BindingFlags.Instance );
                var server = new SystemWebNanoServer { NanoConfiguration = nanoConfiguration };
                Delegate eventHandlerDelegate = Delegate.CreateDelegate( eventInfo.EventHandlerType, server, methodInfo );
                eventInfo.AddEventHandler( httpApplication, eventHandlerDelegate );
                BackgroundTaskRunner.Start( nanoConfiguration );
            }

            /// <summary>BeginRequest event for the HttpApplication.</summary>
            /// <param name="httpApplication">System.Web.HttpApplication.</param>
            /// <param name="eventArgs">The <see cref="EventArgs" /> instance containing the event data.</param>
            public void HttpApplicationOnBeginRequest( dynamic httpApplication, EventArgs eventArgs )
            {
                HandleRequest( httpApplication.Context, NanoConfiguration );
            }

            /// <summary>Handles a System.Web request.</summary>
            /// <param name="httpContext">System.Web.HttpContext.</param>
            /// <param name="nanoConfiguration">The <see cref="NanoConfiguration" />.</param>
            public static void HandleRequest( dynamic httpContext, NanoConfiguration nanoConfiguration )
            {
                NanoContext nanoContext = MapHttpContextBaseToNanoContext( httpContext, nanoConfiguration );

                nanoContext = RequestRouter.RouteRequest( nanoContext );

                if( nanoContext.RequestHandler == null || nanoContext.Handled == false )
                    return;

                if( nanoContext.Response.ResponseStreamWriter != null )
                {
                    nanoContext.Response.ResponseStreamWriter( httpContext.Response.OutputStream );
                }

                httpContext.Response.Charset = nanoContext.Response.Charset;
                httpContext.Response.ContentEncoding = nanoContext.Response.ContentEncoding;
                httpContext.Response.ContentType = nanoContext.Response.ContentType;

                foreach( string headerName in nanoContext.Response.HeaderParameters )
                    httpContext.Response.Headers.Add( headerName, nanoContext.Response.HeaderParameters[headerName] );

                foreach( dynamic cookie in nanoContext.Response.Cookies )
                    httpContext.Response.Headers.Add( "Set-Cookie", cookie.ToString() );

                httpContext.Response.StatusCode = nanoContext.Response.HttpStatusCode;
                httpContext.Response.End();
            }

            /// <summary>Maps a System.Web.HttpContext to a NanoContext.</summary>
            /// <param name="httpContext">System.Web.HttpContext.</param>
            /// <param name="nanoConfiguration">The <see cref="NanoConfiguration" />.</param>
            /// <returns>Mapped <see cref="NanoContext" />.</returns>
            public static NanoContext MapHttpContextBaseToNanoContext( dynamic httpContext, NanoConfiguration nanoConfiguration )
            {
                dynamic httpMethod = httpContext.Request.HttpMethod;

                var basePath = httpContext.Request.ApplicationPath.TrimEnd( '/' );
                var path = httpContext.Request.Url.AbsolutePath.Substring( basePath.Length );
                path = string.IsNullOrWhiteSpace( path ) ? "/" : path;

                var url = new Url
                {
                    Scheme = httpContext.Request.Url.Scheme,
                    HostName = httpContext.Request.Url.Host,
                    Port = httpContext.Request.Url.Port,
                    BasePath = basePath,
                    Path = path,
                    Query = httpContext.Request.Url.Query,
                };
                
                Func<Stream> requestBodyAccessor = () => httpContext.Request.InputStream;
                var nanoRequest = new NanoRequest( httpMethod, url, requestBodyAccessor ) { QueryStringParameters = httpContext.Request.QueryString, FormBodyParameters = httpContext.Request.Form, HeaderParameters = httpContext.Request.Headers };
                var nanoContext = new NanoContext( nanoRequest, new NanoResponse(), nanoConfiguration ) { HostContext = httpContext, RootFolderPath = nanoConfiguration.ApplicationRootFolderPath };
                return nanoContext;
            }
        }
    }

    #endregion Nano.Web.Core.Host.SystemWeb

    #region Nano.Web.Core.Host.HttpListener

    namespace Host.HttpListener
    {
        /// <summary>Nano server for a System.Net.HttpListener hosted application.</summary>
        public class HttpListenerNanoServer : IDisposable
        {
            /// <summary>Flag to determine if Dispose has been called.</summary>
            private bool _disposed;

            /// <summary>The <see cref="HttpListenerConfiguration" />.</summary>
            public HttpListenerConfiguration HttpListenerConfiguration;

            /// <summary>The <see cref="NanoConfiguration" />.</summary>
            public NanoConfiguration NanoConfiguration;

            /// <summary>
            /// Initializes a new instance of the <see cref="HttpListenerNanoServer"/> class.
            /// </summary>
            /// <param name="nanoConfiguration">The <see cref="NanoConfiguration"/>.</param>
            /// <param name="httpListenerConfiguration">The HTTP listener configuration.</param>
            public HttpListenerNanoServer( NanoConfiguration nanoConfiguration, HttpListenerConfiguration httpListenerConfiguration )
            {
                NanoConfiguration = nanoConfiguration;
                HttpListenerConfiguration = httpListenerConfiguration;
            }

            /// <summary>Starts the Nano server for a System.Net.HttpListener hosted application.</summary>
            /// <param name="nanoConfiguration">The <see cref="NanoConfiguration" />.</param>
            /// <param name="httpListenerConfiguration">The <see cref="HttpListenerConfiguration" />.</param>
            /// <returns>New instance of a <see cref="HttpListenerNanoServer"/>.</returns>
            public static HttpListenerNanoServer Start( NanoConfiguration nanoConfiguration, HttpListenerConfiguration httpListenerConfiguration )
            {
                nanoConfiguration.ApplicationRootFolderPath = GetRootPath();
                httpListenerConfiguration.HttpListener.Start();
                var server = new HttpListenerNanoServer( nanoConfiguration, httpListenerConfiguration );
                httpListenerConfiguration.HttpListener.BeginGetContext( server.BeginGetContextCallback, server );
                BackgroundTaskRunner.Start( nanoConfiguration );
                return server;
            }

            /// <summary>Starts the Nano server for a System.Net.HttpListener hosted application.</summary>
            /// <param name="nanoConfiguration">The <see cref="NanoConfiguration" />.</param>
            /// <param name="urls">List of URLs to list on.</param>
            /// <returns>New instance of a <see cref="HttpListenerNanoServer"/>.</returns>
            public static HttpListenerNanoServer Start( NanoConfiguration nanoConfiguration, params string[] urls )
            {
                var uriList = urls.Select( url => new Uri( url ) ).ToList();
                var httpListenerConfig = new HttpListenerConfiguration( uriList );
                return Start( nanoConfiguration, httpListenerConfig );
            }

            /// <summary>The BeginGetContext callback for the <see cref="HttpListener" />.</summary>
            /// <param name="asyncResult">The asynchronous result.</param>
            public void BeginGetContextCallback( IAsyncResult asyncResult )
            {
                try
                {
                    HttpListenerContext httpListenerContext = HttpListenerConfiguration.HttpListener.EndGetContext( asyncResult );
                    HttpListenerConfiguration.HttpListener.BeginGetContext( BeginGetContextCallback, this );
                    HandleRequest( httpListenerContext, this );
                }
                catch( Exception e )
                {
                    HttpListenerConfiguration.UnhandledExceptionHandler( e );
                }
            }

            /// <summary>Handles the request.</summary>
            /// <param name="httpListenerContext">The HTTP listener context.</param>
            /// <param name="server">The server.</param>
            public static void HandleRequest( HttpListenerContext httpListenerContext, HttpListenerNanoServer server )
            {
                try
                {
                    NanoContext nanoContext = MapHttpListenerContextToNanoContext( httpListenerContext, server );

                    nanoContext = RequestRouter.RouteRequest( nanoContext );

                    if( nanoContext.RequestHandler == null || nanoContext.Handled == false )
                        return;

                    httpListenerContext.Response.ContentEncoding = nanoContext.Response.ContentEncoding;
                    httpListenerContext.Response.ContentType = nanoContext.Response.ContentType;

                    foreach( string headerName in nanoContext.Response.HeaderParameters )
                    {
                        if( !IgnoredHeaders.IsIgnored( headerName ) )
                        {
                            httpListenerContext.Response.Headers.Add( headerName, nanoContext.Response.HeaderParameters[headerName] );
                        }
                    }

                    foreach( NanoCookie cookie in nanoContext.Response.Cookies )
                        httpListenerContext.Response.Headers.Add( "Set-Cookie", cookie.ToString() );

                    httpListenerContext.Response.StatusCode = nanoContext.Response.HttpStatusCode;

                    if( nanoContext.Response.ResponseStreamWriter != null )
                    {
                        if( IsGZipSupported( httpListenerContext ) )
                        {
                            httpListenerContext.Response.Headers.Add( "Content-Encoding", "gzip" );

                            using( Stream outputStream = httpListenerContext.Response.OutputStream )
                            {
                                using( var gZipStream = new GZipStream( outputStream, CompressionMode.Compress, true ) )
                                {
                                    nanoContext.Response.ResponseStreamWriter( gZipStream );
                                }
                            }
                        }
                        else
                        {
                            using( Stream outputStream = httpListenerContext.Response.OutputStream )
                            {
                                nanoContext.Response.ResponseStreamWriter( outputStream );
                            }
                        }
                    }
                }
                finally
                {
                    httpListenerContext.Response.Close();
                }
            }

            /// <summary>Determines whether GZip is supported by the client.</summary>
            /// <param name="httpListenerContext">The HTTP listener context.</param>
            /// <returns>Return true if GZip is supported.</returns>
            public static bool IsGZipSupported( HttpListenerContext httpListenerContext )
            {
                string encoding = httpListenerContext.Request.Headers["Accept-Encoding"];

                if( !string.IsNullOrEmpty( encoding ) && encoding.Contains( "gzip" ) )
                {
                    return true;
                }

                return false;
            }

            /// <summary>Maps a <see cref="System.Net.HttpListenerContext" /> to <see cref="NanoContext" />.</summary>
            /// <param name="httpListenerContext">The HTTP listener context.</param>
            /// <param name="server">The server.</param>
            /// <returns>Mapped <see cref="NanoContext" />.</returns>
            public static NanoContext MapHttpListenerContextToNanoContext( HttpListenerContext httpListenerContext, HttpListenerNanoServer server )
            {
                string httpMethod = httpListenerContext.Request.HttpMethod;
                Uri url = httpListenerContext.Request.Url;
                Func<Stream> requestBodyAccessor = () => httpListenerContext.Request.InputStream;
                var nanoRequest = new NanoRequest( httpMethod, url, requestBodyAccessor ) { QueryStringParameters = httpListenerContext.Request.QueryString, FormBodyParameters = ParseFormBodyParameters( httpListenerContext, server ), HeaderParameters = httpListenerContext.Request.Headers };
                var nanoContext = new NanoContext( nanoRequest, new NanoResponse(), server.NanoConfiguration ) { HostContext = httpListenerContext, RootFolderPath = server.NanoConfiguration.ApplicationRootFolderPath };
                return nanoContext;
            }

            /// <summary>Gets the root path of the host application.</summary>
            /// <returns>The root path of the host application.</returns>
            public static string GetRootPath()
            {
                Assembly assembly = Assembly.GetEntryAssembly();

                return assembly != null ?
                    Path.GetDirectoryName( assembly.Location ) :
                    Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );
            }

            /// <summary>Parses the form body parameters.</summary>
            /// <param name="httpListenerContext">The HTTP listener context.</param>
            /// <param name="server">The server.</param>
            /// <returns><see cref="NameValueCollection" /> of the form body parameters.</returns>
            /// <exception cref="System.Exception">The maximum number of form parameters posted was exceeded.</exception>
            public static NameValueCollection ParseFormBodyParameters( HttpListenerContext httpListenerContext, HttpListenerNanoServer server )
            {
                var nameValueCollection = new NameValueCollection();

                if( httpListenerContext.Request.HasEntityBody == false )
                    return nameValueCollection;

                string contentType = httpListenerContext.Request.Headers["Content-Type"];

                if( string.IsNullOrWhiteSpace( contentType ) )
                    return nameValueCollection;

                string mimeType = contentType.Split( ';' ).FirstOrDefault();

                if( string.IsNullOrWhiteSpace( mimeType ) )
                    return nameValueCollection;

                if( mimeType.ToLower() == "application/x-www-form-urlencoded" )
                {
                    var sr = new StreamReader( httpListenerContext.Request.InputStream, httpListenerContext.Request.ContentEncoding );
                    string formData = sr.ReadToEnd();
                    string decodedFormData = UrlDecode( formData );

                    string[] parameters = decodedFormData.Split( '&' );

                    if( parameters.Length > server.HttpListenerConfiguration.MaximumFormParameters )
                        throw new Exception( "The maximum number of form parameters posted was exceeded." );

                    foreach( string parameter in parameters )
                    {
                        string[] keyValuePair = parameter.Split( '=' );
                        nameValueCollection.Add( keyValuePair[0], keyValuePair[1] );
                    }

                    return nameValueCollection;
                }

                return nameValueCollection;
            }

            /// <summary>Decodes a URL string.</summary>
            /// <param name="text">String to decode.</param>
            /// <returns>Decoded string</returns>
            public static string UrlDecode( string text )
            {
                // pre-process for + sign space formatting since System.Uri doesn't handle it
                // plus literals are encoded as %2b normally so this should be safe
                text = text.Replace( "+", " " );
                return Uri.UnescapeDataString( text );
            }

            /// <summary>
            /// A helper class that checks for a header against a list of headers that should be ignored when populating the headers of
            /// an <see cref="T:System.Net.HttpListenerResponse" /> object.
            /// </summary>
            public static class IgnoredHeaders
            {
                private static readonly HashSet<string> KnownHeaders = new HashSet<string>( StringComparer.OrdinalIgnoreCase )
                {
                    "content-length",
                    "content-type",
                    "transfer-encoding",
                    "keep-alive"
                };

                /// <summary>
                /// Determines if a header is ignored when populating the headers of an
                /// <see cref="T:System.Net.HttpListenerResponse" /> object.
                /// </summary>
                /// <param name="headerName">The name of the header.</param>
                /// <returns><c>true</c> if the header is ignored; otherwise, <c>false</c>.</returns>
                public static bool IsIgnored( string headerName )
                {
                    return KnownHeaders.Contains( headerName );
                }
            }

            #region IDisposable Members

            /// <summary>Disposes of the underlying <see cref="HttpListener" />.</summary>
            public void Dispose()
            {
                Dispose( true );

                // Use SupressFinalize in case a subclass
                // of this type implements a finalizer.
                GC.SuppressFinalize( this );
            }

            /// <summary>Disposes of the underlying <see cref="HttpListener" />.</summary>
            /// <param name="disposing">Indicates if being called from the Dispose method.</param>
            protected virtual void Dispose( bool disposing )
            {
                if( _disposed == false )
                {
                    if( disposing )
                    {
                        if( HttpListenerConfiguration != null && HttpListenerConfiguration.HttpListener != null
                            && HttpListenerConfiguration.HttpListener.IsListening )
                        {
                            try
                            {
                                HttpListenerConfiguration.HttpListener.Close();
                            }
                            // ReSharper disable once EmptyGeneralCatchClause - CA1065: Do not raise exceptions in unexpected locations: http://msdn.microsoft.com/en-us/library/bb386039.aspx
                            catch
                            {
                                // Don't throw an exception while disposing
                            }
                            finally
                            {
                                HttpListenerConfiguration.HttpListener = null;
                                HttpListenerConfiguration = null;
                            }
                        }
                    }

                    _disposed = true;
                }
            }

            #endregion IDisposable Members
        }

        /// <summary>
        /// <see cref="System.Net.HttpListener" /> configuration.
        /// </summary>
        public class HttpListenerConfiguration
        {
            /// <summary>The HTTP listener.</summary>
            public System.Net.HttpListener HttpListener;

            /// <summary>The maximum concurrent connections.</summary>
            public int MaximumConcurrentConnections = 100;

            /// <summary>The maximum form parameters.</summary>
            public int MaximumFormParameters = 10000;

            /// <summary>
            /// Invoked on unhandled exceptions that occur during the HttpListenerContext to NanoContext mapping. Note: These will
            /// *not* be called for normal Nano exceptions which are handled by the Nano event handlers. Defaults to writing to Trace
            /// output.
            /// </summary>
            public Action<Exception> UnhandledExceptionHandler = exception =>
            {
                string message = string.Format( "---\n{0}\n---\n", exception );
                Trace.Write( message );
            };

            /// <summary>
            /// Gets or sets a property that determines if localhost uris are rewritten to htp://+:port/ style uris to allow for
            /// listening on all ports, but requiring either a url reservation, or admin access Defaults to false.
            /// </summary>
            public bool RewriteLocalhost;

            /// <summary>Initializes a new instance of the <see cref="HttpListenerConfiguration" /> class.</summary>
            /// <param name="httpListener">The HTTP listener.</param>
            public HttpListenerConfiguration( System.Net.HttpListener httpListener )
            {
                HttpListener = httpListener;
            }

            /// <summary>Initializes a new instance of the <see cref="HttpListenerConfiguration" /> class.</summary>
            /// <param name="uris">List of URIs to listen on.</param>
            /// <param name="rewriteLocalhost">if set to <c>true</c> it will rewrite localhost to '+' sign.</param>
            public HttpListenerConfiguration( IList<Uri> uris, bool rewriteLocalhost = false )
            {
                HttpListener = new System.Net.HttpListener();
                RewriteLocalhost = rewriteLocalhost;
                AddPrefixes( uris );
            }

            /// <summary>Adds the prefixes to the <see cref="HttpListener" />.</summary>
            /// <param name="uris">List of URIs.</param>
            public void AddPrefixes( IList<Uri> uris )
            {
                foreach( Uri uri in uris )
                {
                    string prefix = uri.ToString();

                    if( RewriteLocalhost && !uri.Host.Contains( "." ) )
                    {
                        prefix = prefix.Replace( "localhost", "+" );
                    }

                    HttpListener.Prefixes.Add( prefix );
                }
            }
        }
    }

    #endregion Nano.Web.Core.Host.HttpListener

    #region Nano.Web.Core.RequestHandlers

    namespace RequestHandlers
    {
        /// <summary>Request handler.</summary>
        public interface IRequestHandler
        {
            /// <summary>The URL path for this handler.</summary>
            /// <value>The URL path.</value>
            string UrlPath { get; set; }

            /// <summary>The event handlers associated with this handler.</summary>
            /// <value>The event handlers.</value>
            EventHandler EventHandler { get; set; }

            /// <summary>Handles the request.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" /> to handle.</param>
            /// <returns>The handled <see cref="NanoContext" />.</returns>
            NanoContext HandleRequest( NanoContext nanoContext );

            /// <summary>
            /// Called by the framework to process the request. Encapsulates invoking the event handlers and user implemented
            /// HandleRequest method.
            /// </summary>
            /// <param name="nanoContext">The <see cref="NanoContext" /> to process.</param>
            /// <returns>The processed <see cref="NanoContext" />.</returns>
            NanoContext ProcessRequest( NanoContext nanoContext );
        }

        /// <summary>Base Nano request handler that provides common functionality for request handler implementers.</summary>
        public abstract class RequestHandler : IRequestHandler
        {
            /// <summary>Initializes a new instance of the <see cref="RequestHandler" /> class.</summary>
            /// <param name="urlPath">The URL path.</param>
            /// <param name="eventHandler">The event handler.</param>
            /// <exception cref="System.ArgumentNullException">urlPath or eventHandler</exception>
            public RequestHandler( string urlPath, EventHandler eventHandler )
            {
                if( string.IsNullOrWhiteSpace( urlPath ) )
                    throw new ArgumentNullException( "urlPath" );

                if( eventHandler == null )
                    throw new ArgumentNullException( "eventHandler" );

                UrlPath = urlPath;
                EventHandler = eventHandler;
            }

            /// <summary>The URL path for this handler.</summary>
            /// <value>The URL path.</value>
            public string UrlPath { get; set; }

            /// <summary>The event handlers associated with this handler.</summary>
            /// <value>The event handlers.</value>
            public EventHandler EventHandler { get; set; }

            /// <summary>Handles the request.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" /> to handle.</param>
            /// <returns>The handled <see cref="NanoContext" />.</returns>
            public abstract NanoContext HandleRequest( NanoContext nanoContext );

            /// <summary>
            /// Called by the framework to process the request. Encapsulates invoking the various event handlers and user implemented
            /// HandleRequest method.
            /// </summary>
            /// <param name="nanoContext">The <see cref="NanoContext" /> to process.</param>
            /// <returns>The processed <see cref="NanoContext" />.</returns>
            public virtual NanoContext ProcessRequest( NanoContext nanoContext )
            {
                try
                {
                    if( EventHandler != null )
                        EventHandler.InvokePreInvokeHandlers( nanoContext );

                    nanoContext.NanoConfiguration.GlobalEventHandler.InvokePreInvokeHandlers( nanoContext );

                    if( nanoContext.Handled )
                        return nanoContext;

                    nanoContext = HandleRequest( nanoContext );
                    return nanoContext;
                }
                catch( Exception e )
                {
                    nanoContext.Errors.Add( e );
                    nanoContext.ReturnInternalServerError();

                    if( EventHandler != null )
                        EventHandler.InvokeUnhandledExceptionHandlers( e, nanoContext );

                    nanoContext.NanoConfiguration.GlobalEventHandler.InvokeUnhandledExceptionHandlers( e, nanoContext );
                }
                finally
                {
                    if( EventHandler != null )
                        EventHandler.InvokePostInvokeHandlers( nanoContext );

                    nanoContext.NanoConfiguration.GlobalEventHandler.InvokePostInvokeHandlers( nanoContext );
                }

                return nanoContext;
            }
        }

        /// <summary>Handles requests to defined file system directories and files within those directories.</summary>
        public class DirectoryRequestHandler : RequestHandler
        {
            /// <summary>Initializes a new instance of the <see cref="DirectoryRequestHandler" /> class.</summary>
            /// <param name="urlPath">The URL path.</param>
            /// <param name="eventHandler">The event handlers.</param>
            /// <param name="fileSystemPath">The file system path.</param>
            /// <param name="returnHttp404WhenFileWasNoFound">Should return an 'HTTP 404 - File Not Found' when no file was found.</param>
            /// <param name="defaultDocuments">
            /// The default document to search for and serve when a request is made to a directory and not a file. The first document
            /// found in the list will be returned. If no document is found a 'HTTP 404 File Does Not Exist' will be sent to the
            /// client.
            /// </param>
            /// <exception cref="System.ArgumentNullException">fileSystemPath</exception>
            /// <exception cref="Nano.Web.Core.RequestHandlers.DirectoryRequestHandler.RootDirectoryException"></exception>
            public DirectoryRequestHandler( string urlPath, EventHandler eventHandler, string fileSystemPath, bool returnHttp404WhenFileWasNoFound = false, IList<string> defaultDocuments = null )
                : base( urlPath, eventHandler )
            {
                if( string.IsNullOrWhiteSpace( fileSystemPath ) )
                    throw new ArgumentNullException( "fileSystemPath" );

                if( defaultDocuments == null )
                    defaultDocuments = new[] { "index.html" };

                FileSystemPath = fileSystemPath.Replace( "/", Constants.DirectorySeparatorString ).TrimStart( '~', Constants.DirectorySeparatorChar ).TrimEnd( '/', Constants.DirectorySeparatorChar );

                if( FileSystemPath == Constants.DirectorySeparatorString )
                    throw new RootDirectoryException();

                ReturnHttp404WhenFileWasNoFound = returnHttp404WhenFileWasNoFound;
                DefaultDocuments = defaultDocuments;
            }

            /// <summary>Gets or sets the file system path.</summary>
            /// <value>The file system path.</value>
            public string FileSystemPath { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether it should return an 'HTTP 404 - File Not Found' when no file was found..
            /// </summary>
            /// <value>
            /// <c>true</c> if [return HTTP404 when file was no found]; otherwise, <c>false</c>.
            /// </value>
            public bool ReturnHttp404WhenFileWasNoFound { get; set; }

            /// <summary>Gets or sets the default documents.</summary>
            /// <value>The default documents.</value>
            public IList<string> DefaultDocuments { get; set; }

            /// <summary>Handles the request.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <returns>Handled <see cref="NanoContext" />.</returns>
            public override NanoContext HandleRequest( NanoContext nanoContext )
            {
                // Turn this: ( http://localhost:50463/dashboard/sub/helpers.js ) into this: ( C:\YourApp\www\dashboard\sub\helpers.js )
                string partialRequestPath = ReplaceFirstOccurrence( nanoContext.Request.Url.Path.ToLower(), UrlPath.ToLower(), "" );
                string encodedPartialRequestPath = partialRequestPath.Replace( "/", Constants.DirectorySeparatorString ).TrimStart( Constants.DirectorySeparatorChar );
                string fullFileSystemPath = Path.Combine( nanoContext.RootFolderPath, FileSystemPath, encodedPartialRequestPath );

                if( nanoContext.TryReturnFile( new FileInfo( fullFileSystemPath ) ) )
                    return nanoContext;

                
                var directoryInfo = new DirectoryInfo( fullFileSystemPath );

                if( directoryInfo.Exists )
                {
                    // If the URL does not end with a forward slash then redirect to the same URL with a forward slash
                    // so that relative URLs will work correctly
                    if( nanoContext.Request.Url.Path.EndsWith( "/", StringComparison.Ordinal ) == false )
                    {
                        string url = nanoContext.Request.Url.BasePath + nanoContext.Request.Url.Path + "/" + nanoContext.Request.Url.Query;
                        nanoContext.Response.Redirect( url );
                        return nanoContext;
                    }

                    foreach( string defaultDocument in DefaultDocuments )
                    {
                        string path = Path.Combine( fullFileSystemPath, defaultDocument );

                        if( nanoContext.TryReturnFile( new FileInfo( path ) ) )
                            return nanoContext;
                    }
                }

                if( ReturnHttp404WhenFileWasNoFound )
                    nanoContext.ReturnFileNotFound();

                return nanoContext;
            }

            /// <summary>
            /// Returns a new string in which the first occurrence of a specified string in the current instance is replaced with
            /// another specified string.
            /// </summary>
            /// <param name="originalString">The original string.</param>
            /// <param name="stringToReplace">The string to be replaced.</param>
            /// <param name="replacementString">The replacement string.</param>
            /// <returns></returns>
            public static string ReplaceFirstOccurrence( string originalString, string stringToReplace, string replacementString )
            {
                int pos = originalString.IndexOf( stringToReplace, StringComparison.Ordinal );
                if( pos < 0 )
                    return originalString;
                return originalString.Substring( 0, pos ) + replacementString + originalString.Substring( pos + stringToReplace.Length );
            }

            /// <summary>Root Directory exception.</summary>
            public class RootDirectoryException : Exception
            {
                /// <summary>Initializes a new instance of the <see cref="RootDirectoryException" /> class.</summary>
                /// <param name="message">The message that describes the error.</param>
                public RootDirectoryException( string message = "Mapping a directory to the root of the application is NOT allowed as this is big security concern and would expose application code and internals." )
                    : base( message )
                {
                }
            }
        }

        /// <summary>Handles requests to defined file system files.</summary>
        public class FileRequestHandler : RequestHandler
        {
            /// <summary>Initializes a new instance of the <see cref="FileRequestHandler" /> class.</summary>
            /// <param name="urlPath">The URL path.</param>
            /// <param name="eventHandler">The event handler.</param>
            /// <param name="fileSystemPath">The file system path.</param>
            /// <exception cref="System.ArgumentNullException">fileSystemPath</exception>
            public FileRequestHandler( string urlPath, EventHandler eventHandler, string fileSystemPath )
                : base( urlPath, eventHandler )
            {
                if( string.IsNullOrWhiteSpace( fileSystemPath ) )
                    throw new ArgumentNullException( "fileSystemPath" );

                FileSystemPath = fileSystemPath.Replace( "/", Constants.DirectorySeparatorString ).TrimStart( '~', Constants.DirectorySeparatorChar );
            }

            /// <summary>Gets or sets the file system path.</summary>
            /// <value>The file system path.</value>
            public string FileSystemPath { get; set; }

            /// <summary>Handles the request.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <returns>Handled <see cref="NanoContext" />.</returns>
            public override NanoContext HandleRequest( NanoContext nanoContext )
            {
                string fullFilePath = Path.Combine( nanoContext.RootFolderPath, FileSystemPath );

                if( nanoContext.TryReturnFile( new FileInfo( fullFilePath ) ) )
                    return nanoContext;

                nanoContext.ReturnFileNotFound();
                return nanoContext;
            }
        }

        /// <summary>Handles requests to <see cref="Func" />s.</summary>
        public class FuncRequestHandler : RequestHandler
        {
            /// <summary>Initializes a new instance of the <see cref="FuncRequestHandler" /> class.</summary>
            /// <param name="urlPath">The URL path.</param>
            /// <param name="eventHandler">The event handler.</param>
            /// <param name="func">The function.</param>
            /// <exception cref="System.ArgumentNullException">func</exception>
            public FuncRequestHandler( string urlPath, EventHandler eventHandler, Func<NanoContext, object> func )
                : base( urlPath, eventHandler )
            {
                if( func == null )
                    throw new ArgumentNullException( "func" );

                Func = func;
            }

            /// <summary>Gets the function.</summary>
            /// <value>The function.</value>
            public Func<NanoContext, object> Func { get; private set; }

            /// <summary>Gets or sets the metadata provider.</summary>
            /// <value>The metadata provider.</value>
            public IApiMetaDataProvider MetadataProvider { get; set; }

            /// <summary>Handles the request.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <returns>Handled <see cref="NanoContext" />.</returns>
            public override NanoContext HandleRequest( NanoContext nanoContext )
            {
                nanoContext.Handled = true;
                nanoContext.Response.ResponseObject = Func( nanoContext );

                // Handle null responses and void methods
                if( nanoContext.Response.ResponseObject == null )
                    return nanoContext;

                if ( string.IsNullOrWhiteSpace( nanoContext.Response.ContentType ) )
                    nanoContext.Response.ContentType =  "application/json";

                nanoContext.Response.ResponseStreamWriter = nanoContext.Response.ResponseStreamWriter ?? nanoContext.WriteResponseObjectToStream;
                return nanoContext;
            }
        }

        /// <summary>Metadata request handler.</summary>
        public class MetadataRequestHandler : RequestHandler
        {
            /// <summary>Initializes a new instance of the <see cref="MetadataRequestHandler" /> class.</summary>
            /// <param name="urlPath">The URL path.</param>
            /// <param name="eventHandler">The event handler.</param>
            public MetadataRequestHandler( string urlPath, EventHandler eventHandler )
                : base( urlPath, eventHandler )
            {
            }

            /// <summary>Handles the request.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <returns>Handled <see cref="NanoContext" />.</returns>
            public override NanoContext HandleRequest( NanoContext nanoContext )
            {
                nanoContext.Handled = true;
                var apiMetadata = new ApiMetadata();

                foreach( MethodRequestHandler methodRequestHandler in nanoContext.NanoConfiguration.RequestHandlers.OfType<MethodRequestHandler>() )
                {
                    IApiMetaDataProvider metadataProvider = methodRequestHandler.MetadataProvider;

                    if( metadataProvider == null )
                        continue;

                    var metadata = new OperationMetaData { UrlPath = methodRequestHandler.UrlPath };
                    metadata.Name = metadataProvider.GetOperationName( nanoContext, methodRequestHandler );
                    metadata.Description = metadataProvider.GetOperationDescription( nanoContext, methodRequestHandler );
                    IList<MethodParameter> parameters = metadataProvider.GetOperationParameters( nanoContext, methodRequestHandler );

                    foreach( MethodParameter methodParameter in parameters )
                    {
                        if( methodParameter.Name.ToLower() == "nanocontext" || methodParameter.Type == typeof( NanoContext ) )
                            continue;

                        var inputParameter = new OperationParameter { Name = methodParameter.Name, Description = methodParameter.Description, Type = GetTypeName( methodParameter.Type ), IsOptional = methodParameter.IsOptional };

                        metadata.InputParameters.Add( inputParameter );

                        // Adding all user types as "Models
                        AddModels( apiMetadata, methodParameter.Type );
                    }

                    var returnParameterType = metadataProvider.GetOperationReturnParameterType( nanoContext, methodRequestHandler );
                    metadata.ReturnParameterType = returnParameterType.Name;
                    AddModels( apiMetadata, returnParameterType );
                    apiMetadata.Operations.Add( metadata );
                }

                nanoContext.Response.ResponseObject = apiMetadata;
                nanoContext.Response.ContentType = "application/json";
                nanoContext.Response.ResponseStreamWriter = nanoContext.WriteResponseObjectToStream;
                return nanoContext;
            }

            /// <summary>
            /// Recursive method that crawls each of the types fields and properties creating Models for each user type.
            /// </summary>
            /// <param name="apiMetadata">ApiMetadata to add each model to.</param>
            /// <param name="type">Type to crawl.</param>
            public static void AddModels( ApiMetadata apiMetadata, Type type )
            {
                var nestedUserTypes = new List<Type>();

                if( type.IsGenericType )
                {
                    type = type.GetGenericArguments().FirstOrDefault();
                }

                // Adding all user types as "Models"
                if( IsUserType( type ) && apiMetadata.Models.Any( x => x.Type == type.Name ) == false )
                {
                    var modelMetadata = new ModelMetadata { Type = type.Name, Description = GetDescription( type ) };
                    FieldInfo[] fields = type.GetFields();

                    foreach( FieldInfo field in fields )
                    {
                        // Add nested user types
                        if( IsUserType( type ) && apiMetadata.Models.Any( x => x.Type == type.Name ) == false )
                            nestedUserTypes.Add( field.FieldType );

                        var modelProperty = new ModelProperty { Name = field.Name, Type = GetTypeName( field.FieldType ), Description = GetDescription( field ) };
                        modelMetadata.Properties.Add( modelProperty );
                    }

                    PropertyInfo[] properties = type.GetProperties();

                    foreach( PropertyInfo property in properties )
                    {
                        // Add nested user types
                        if( IsUserType( type ) && apiMetadata.Models.Any( x => x.Type == type.Name ) == false )
                            nestedUserTypes.Add( property.PropertyType );

                        var modelProperty = new ModelProperty { Name = property.Name, Type = GetTypeName( property.PropertyType ), Description = GetDescription( property ) };
                        modelMetadata.Properties.Add( modelProperty );
                    }

                    apiMetadata.Models.Add( modelMetadata );

                    // For each nested user type recursively call this same method to add them
                    // Note it is important to do this after the current modelMetadata has been added
                    // to avoid a stack overflow due to possible circular dependencies.
                    foreach( Type nestedUserType in nestedUserTypes )
                        AddModels( apiMetadata, nestedUserType );
                }
            }

            /// <summary>
            /// Gets a description for the type by first attempting to get it from a [Description] attribute and falling back to the
            /// summary node of any XML documentation.
            /// </summary>
            /// <param name="type">Type to get a description for.</param>
            /// <returns>Description text.</returns>
            public static string GetDescription( Type type )
            {
                object descriptionAttribute = type.GetCustomAttributes( typeof( DescriptionAttribute ), true ).FirstOrDefault();

                if( descriptionAttribute != null )
                    return ( (DescriptionAttribute)descriptionAttribute ).Description;

                return type.GetXmlDocumentation().GetSummary();
            }

            /// <summary>
            /// Gets a description for the member by first attempting to get it from a [Description] attribute and falling back to the
            /// summary node of any XML documentation.
            /// </summary>
            /// <param name="fieldInfo">Member to get a description for.</param>
            /// <returns>Description text.</returns>
            public static string GetDescription( FieldInfo fieldInfo )
            {
                object descriptionAttribute = fieldInfo.GetCustomAttributes( typeof( DescriptionAttribute ), true ).FirstOrDefault();

                if( descriptionAttribute != null )
                    return ( (DescriptionAttribute)descriptionAttribute ).Description;

                return fieldInfo.GetXmlDocumentation().GetSummary();
            }

            /// <summary>
            /// Gets a description for the member by first attempting to get it from a [Description] attribute and falling back to the
            /// summary node of any XML documentation.
            /// </summary>
            /// <param name="propertyInfo">Member to get a description for.</param>
            /// <returns>Description text.</returns>
            public static string GetDescription( PropertyInfo propertyInfo )
            {
                object descriptionAttribute = propertyInfo.GetCustomAttributes( typeof( DescriptionAttribute ), true ).FirstOrDefault();

                if( descriptionAttribute != null )
                    return ( (DescriptionAttribute)descriptionAttribute ).Description;

                return propertyInfo.GetXmlDocumentation().GetSummary();
            }

            /// <summary>Determines if a type is a User Type meaning it is not a standard .NET type.</summary>
            /// <param name="type">Type.</param>
            /// <returns>Boolean value.</returns>
            public static bool IsUserType( Type type )
            {
                return type != null && ( type.Namespace != null &&
                                         type.Namespace.StartsWith( "System" ) == false &&
                                         type.Namespace.StartsWith( "Microsoft" ) == false );
            }

            /// <summary>Determines if a type is a nested User Type meaning it is not a standard .NET type.</summary>
            /// <param name="type">Type.</param>
            /// <returns>Boolean value.</returns>
            public static bool IsNestedUserType( Type type )
            {
                if( type.IsGenericType )
                {
                    type = type.GetGenericArguments().FirstOrDefault();
                }

                return IsUserType( type );
            }

            /// <summary>Gets the type name.</summary>
            /// <param name="type">Type.</param>
            /// <returns>Type name.</returns>
            public static string GetTypeName( Type type )
            {
                if ( type.IsGenericType )
                {
                    var genericArgument = type.GetGenericArguments().FirstOrDefault();

                    var genericIndex = type.FullName.IndexOf( "`1", StringComparison.Ordinal );

                    if ( genericArgument != null && genericIndex > 1 )
                    {
                        var typeName = type.FullName.Substring( 0, genericIndex );

                        return typeName + "<" + genericArgument.Name + ">";
                    }
                }

                Type underlyingType = Nullable.GetUnderlyingType( type );

                if( underlyingType != null )
                    return "Nullable<" + underlyingType.Name + ">";

                return type.Name;
            }
        }

        /// <summary>Handles requests to public static methods.</summary>
        public class MethodRequestHandler : RequestHandler
        {
            /// <summary>Initializes a new instance of the <see cref="MethodRequestHandler" /> class.</summary>
            /// <param name="urlPath">The URL path.</param>
            /// <param name="eventHandler">The event handler.</param>
            /// <param name="methodInfo">The method information.</param>
            /// <exception cref="System.ArgumentNullException">methodInfo</exception>
            public MethodRequestHandler( string urlPath, EventHandler eventHandler, MethodInfo methodInfo )
                : base( urlPath, eventHandler )
            {
                if( methodInfo == null )
                    throw new ArgumentNullException( "methodInfo" );

                Method = methodInfo;
                MethodParameters = GetMethodParameters( methodInfo );
                Description = GetDescription( Method );
            }

            /// <summary>Gets the method.</summary>
            /// <value>The method.</value>
            public MethodInfo Method { get; private set; }

            /// <summary>Gets the method parameters.</summary>
            /// <value>The method parameters.</value>
            public IList<MethodParameter> MethodParameters { get; private set; }

            /// <summary>Gets or sets the metadata provider.</summary>
            /// <value>The metadata provider.</value>
            public IApiMetaDataProvider MetadataProvider { get; set; }

            /// <summary>Gets or sets the description for the method.</summary>
            /// <value>The description.</value>
            public string Description { get; set; }

            /// <summary>Handles the request.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <returns>Handled <see cref="NanoContext" />.</returns>
            public override NanoContext HandleRequest( NanoContext nanoContext )
            {
                nanoContext.Handled = true;
                object[] parameters = Bind( nanoContext, this );
                nanoContext.Response.ResponseObject = Method.Invoke( null, parameters );

                // Handle null responses and void methods
                if( nanoContext.Response.ResponseObject == null )
                    return nanoContext;

                nanoContext.Response.ContentType = "application/json";
                nanoContext.Response.ResponseStreamWriter = nanoContext.Response.ResponseStreamWriter ?? nanoContext.WriteResponseObjectToStream;
                return nanoContext;
            }

            /// <summary>
            /// Gets a description for the member by first attempting to get it from a [Description] attribute and falling back to the
            /// summary node of any XML documentation.
            /// </summary>
            /// <param name="methodInfo">Member to get a description for.</param>
            /// <returns>Description text.</returns>
            public static string GetDescription( MethodInfo methodInfo )
            {
                object descriptionAttribute = methodInfo.GetCustomAttributes( typeof( DescriptionAttribute ), true ).FirstOrDefault();

                if( descriptionAttribute != null )
                    return ( (DescriptionAttribute)descriptionAttribute ).Description;

                return methodInfo.GetXmlDocumentation().GetSummary();
            }

            /// <summary>Gets the method parameters.</summary>
            /// <param name="methodInfo">The method information.</param>
            /// <returns>List of <see cref="MethodParameter" />s.</returns>
            public static IList<MethodParameter> GetMethodParameters( MethodInfo methodInfo )
            {
                var methodParameters = new List<MethodParameter>();

                XmlElement xmlMethodDocumentation = methodInfo.GetXmlDocumentation();

                foreach( ParameterInfo parameterInfo in methodInfo.GetParameters().OrderBy( x => x.Position ) )
                {
                    var methodParameter = new MethodParameter { Position = parameterInfo.Position, Name = parameterInfo.Name, Type = parameterInfo.ParameterType, IsOptional = parameterInfo.IsOptional, IsDynamic = IsDynamic( parameterInfo ), Description = xmlMethodDocumentation.GetParameterDescription( parameterInfo.Name ) };
                    methodParameters.Add( methodParameter );
                }

                return methodParameters;
            }

            /// <summary>Determines if the parameter is dynamic.</summary>
            /// <param name="parameterInfo">Parameter type.</param>
            /// <returns>True if dynamic.</returns>
            public static bool IsDynamic( ParameterInfo parameterInfo )
            {
                return parameterInfo.GetCustomAttributes( typeof( DynamicAttribute ), true ).Length > 0;
            }

            /// <summary>Binds the request parameters in the <see cref="NanoContext" /> to the method parameters.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <param name="handler">The handler.</param>
            /// <returns>List of method parameter values.</returns>
            /// <exception cref="System.Exception">
            /// Expected a MethodRoute but was a  + nanoContext.RequestHandler.GetType().Name or or Type conversion error or
            /// </exception>
            public static object[] Bind( NanoContext nanoContext, MethodRequestHandler handler )
            {
                var methodInvokationParameters = new List<object>();

                if( handler == null )
                    throw new Exception( "Expected a MethodRoute but was a " + nanoContext.RequestHandler.GetType().Name );

                foreach( MethodParameter methodParameter in handler.MethodParameters.OrderBy( x => x.Position ) )
                {
                    // Handle NanoContext parameters by injecting the current NanoContext as the parameter value
                    if( methodParameter.Type == typeof( NanoContext ) || methodParameter.Name.ToLower() == "nanocontext" )
                    {
                        methodInvokationParameters.Add( nanoContext );
                        continue;
                    }

                    object itemEntry;

                    // Handle injecting value from the context's Items dictionary
                    // Note that this facilitates method-level dependency injection, passing in User Ids obtained in a PreInvokeHandler, language preferences, etc.
                    if( nanoContext.Items.TryGetValue( methodParameter.Name, out itemEntry ) )
                    {
                        methodInvokationParameters.Add( itemEntry );
                        continue;
                    }

                    // Try to get the method parameter value from the request parameters
                    string requestParameterValue = nanoContext.GetRequestParameterValue( methodParameter.Name );

                    // If the method parameter value is null or could not be found in the request parameters
                    // then as a last attempt try to read any value directly from the request body if one exists
                    // Note that 'Content-Type: application/json' often just puts a JSON blob directly in the request body
                    if ( String.IsNullOrWhiteSpace( requestParameterValue ) )
                    {
                        if ( nanoContext.Request.FormBodyParameters.Count == 0 )
                        {
                            try
                            {
                                var sr = new StreamReader( nanoContext.Request.RequestBody );
                                requestParameterValue = sr.ReadToEnd();
                            }
                            catch ( Exception e )
                            {
                            }
                        }
                    }

                    // If the method parameter value is null or could not be found in the request parameters
                    if( String.IsNullOrWhiteSpace( requestParameterValue ) )
                    {
                        // Handle optional parameters
                        if( methodParameter.IsOptional )
                        {
                            methodInvokationParameters.Add( Type.Missing );
                            continue;
                        }

                        // Handle nullable parameters
                        if( Nullable.GetUnderlyingType( methodParameter.Type ) != null )
                        {
                            methodInvokationParameters.Add( null );
                            continue;
                        }

                        string errorMessage = String.Format( "The query string, form body, and header parameters do not contain a parameter named '{0}' which is a required parameter for method '{1}'", methodParameter.Name, handler.Method.Name );
                        throw new Exception( errorMessage );
                    }

                    object methodInvokationParameterValue;

                    if( methodParameter.IsDynamic == false )
                    {
                        // First try to convert the type using the TypeConverter which handles 'simple' types
                        try
                        {
                            methodInvokationParameterValue = TypeConverter.ConvertType( requestParameterValue, methodParameter.Type );
                            methodInvokationParameters.Add( methodInvokationParameterValue );
                            continue;
                        }
                        catch( Exception )
                        {
                            // Swallow the exception here so that we drop down to the next try/catch block
                        }
                    }

                    // As a last attempt use the heavy weight JSON converter to convert the type
                    try
                    {
                        Type underlyingType = Nullable.GetUnderlyingType( methodParameter.Type ) ?? methodParameter.Type;

                        // Try to convert the request parameter value to the method parameter values type
                        // Note we are currently leveraging Json.Net to handle the heavy load of type conversions
                        if( nanoContext.NanoConfiguration.SerializationService.TryParseJson( requestParameterValue, underlyingType, methodParameter.IsDynamic, out methodInvokationParameterValue ) )
                            methodInvokationParameters.Add( methodInvokationParameterValue );
                        else
                            throw new Exception( "Type conversion error" );
                    }
                    catch( Exception )
                    {
                        string errorMessage = String.Format( "An error occurred converting the parameter named '{0}' and value '{1}' to type {2} which is a required parameter for method '{3}'", methodParameter.Name, requestParameterValue, methodParameter.Type, handler.Method.Name );
                        throw new Exception( errorMessage );
                    }
                }

                return methodInvokationParameters.ToArray();
            }
        }
    }

    #endregion Nano.Web.Core.RequestHandlers

    #region Nano.Web.Core.Metadata

    namespace Metadata
    {
        /// <summary>Provides API metadata.</summary>
        public interface IApiMetaDataProvider
        {
            /// <summary>Gets the name of the operation.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <param name="requestHandler">The <see cref="IRequestHandler" />.</param>
            /// <returns>Operation name.</returns>
            string GetOperationName( NanoContext nanoContext, IRequestHandler requestHandler );

            /// <summary>Gets the operation description.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <param name="requestHandler">The <see cref="IRequestHandler" />.</param>
            /// <returns>Operation description.</returns>
            string GetOperationDescription( NanoContext nanoContext, IRequestHandler requestHandler );

            /// <summary>Gets the operation parameters.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <param name="requestHandler">The <see cref="IRequestHandler" />.</param>
            /// <returns>List of <see cref="MethodParameter" />s.</returns>
            IList<MethodParameter> GetOperationParameters( NanoContext nanoContext, IRequestHandler requestHandler );

            /// <summary>Gets the type of the operation return parameter.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <param name="requestHandler">The <see cref="IRequestHandler" />.</param>
            /// <returns>Operation return parameter type.</returns>
            Type GetOperationReturnParameterType( NanoContext nanoContext, IRequestHandler requestHandler );
        }

        /// <summary>Provides API metadata for a <see cref="MethodRequestHandler" />.</summary>
        public class MethodRequestHandlerMetadataProvider : IApiMetaDataProvider
        {
            /// <summary>Gets the name of the operation.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <param name="requestHandler">The <see cref="IRequestHandler" />.</param>
            /// <returns>Operation name.</returns>
            public virtual string GetOperationName( NanoContext nanoContext, IRequestHandler requestHandler )
            {
                MethodRequestHandler handler = GetMethodRequestHandler( requestHandler );
                return handler.Method.Name;
            }

            /// <summary>Gets the operation description.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <param name="requestHandler">The <see cref="IRequestHandler" />.</param>
            /// <returns>Operation description.</returns>
            public virtual string GetOperationDescription( NanoContext nanoContext, IRequestHandler requestHandler )
            {
                MethodRequestHandler handler = GetMethodRequestHandler( requestHandler );
                return handler.Description;
            }

            /// <summary>Gets the operation parameters.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <param name="requestHandler">The <see cref="IRequestHandler" />.</param>
            /// <returns>List of <see cref="MethodParameter" />s.</returns>
            public virtual IList<MethodParameter> GetOperationParameters( NanoContext nanoContext, IRequestHandler requestHandler )
            {
                MethodRequestHandler handler = GetMethodRequestHandler( requestHandler );
                return handler.MethodParameters;
            }

            /// <summary>Gets the type of the operation return parameter.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <param name="requestHandler">The <see cref="IRequestHandler" />.</param>
            /// <returns>Operation return parameter type.</returns>
            public virtual Type GetOperationReturnParameterType( NanoContext nanoContext, IRequestHandler requestHandler )
            {
                MethodRequestHandler handler = GetMethodRequestHandler( requestHandler );
                return handler.Method.ReturnType;
            }

            /// <summary>Gets the <see cref="MethodRequestHandler" />.</summary>
            /// <param name="requestHandler">The <see cref="IRequestHandler" />.</param>
            /// <returns>
            ///     <see cref="MethodRequestHandler" />
            /// </returns>
            /// <exception cref="System.Exception">
            /// nanoContext.RequestHandler is NULL or Expected a MethodRequestHandler but was a  + requestHandler.GetType()
            /// </exception>
            public MethodRequestHandler GetMethodRequestHandler( IRequestHandler requestHandler )
            {
                if( requestHandler == null )
                    throw new Exception( "nanoContext.RequestHandler is NULL" );

                var handler = requestHandler as MethodRequestHandler;

                if( handler == null )
                    throw new Exception( "Expected a MethodRequestHandler but was a " + requestHandler.GetType() );

                return handler;
            }
        }

        /// <summary>Api metadata.</summary>
        public class ApiMetadata
        {
            /// <summary>Api metadata version.</summary>
            public string Version = "0.2.0";

            /// <summary>The API models.</summary>
            public IList<ModelMetadata> Models = new List<ModelMetadata>();

            /// <summary>The API operations.</summary>
            public IList<OperationMetaData> Operations = new List<OperationMetaData>();
        }

        /// <summary>Model metadata.</summary>
        public class ModelMetadata
        {
            /// <summary>The model description.</summary>
            public string Description;

            /// <summary>The model type.</summary>
            public string Type;

            /// <summary>The model properties.</summary>
            public IList<ModelProperty> Properties = new List<ModelProperty>();
        }

        /// <summary>Model property.</summary>
        public class ModelProperty
        {
            /// <summary>The description of the model.</summary>
            public string Description;

            /// <summary>The model name.</summary>
            public string Name;
            
            /// <summary>The model type.</summary>
            public string Type;
        }

        /// <summary>Operation metadata.</summary>
        public class OperationMetaData
        {
            /// <summary>The description of the operation.</summary>
            public string Description;
            
            /// <summary>The input parameters for the operation.</summary>
            public IList<OperationParameter> InputParameters = new List<OperationParameter>();

            /// <summary>The operation name.</summary>
            public string Name;

            /// <summary>The return parameter type of the operation.</summary>
            public string ReturnParameterType;

            /// <summary>The operation url path.</summary>
            public string UrlPath;
        }

        /// <summary>Operation parameter.</summary>
        public class OperationParameter
        {
            /// <summary>Parameter description;</summary>
            public string Description;

            /// <summary>Indicates if the parameter is optional.</summary>
            public bool IsOptional;

            /// <summary>Parameter name.</summary>
            public string Name;

            /// <summary>Parameter type.</summary>
            public string Type;
        }
    }

    #endregion Nano.Web.Core.Metadata

    #region Nano.Web.Core.Serialization

    namespace Serialization
    {
        /// <summary>Provides serialization / deserialization services.</summary>
        public interface ISerializationService
        {
            /// <summary>Serializes an object to a string.</summary>
            /// <param name="obj">Object to serialize</param>
            /// <returns>String representation of serialized object</returns>
            string Serialize( object obj );

            /// <summary>Deserializes a string to an object of arbitrary type <typeparamref name="T" />.</summary>
            /// <typeparam name="T">Arbitrary type to deserializes to</typeparam>
            /// <param name="input">String representation of serialized object</param>
            /// <returns>Object of arbitrary type <typeparamref name="T" /></returns>
            T Deserialize<T>( string input );

            /// <summary>Deserializes a string to an object.</summary>
            /// <param name="input">String representation of serialized object</param>
            /// <param name="type">Type to deserialize to</param>
            /// <returns>Deserialized object.</returns>
            object Deserialize( string input, Type type );
        }

        /// <summary>Provides various helpers for working with JSON.</summary>
        public static class JsonHelpers
        {
            /// <summary>Tries to safely parse the input string as JSON.</summary>
            /// <remarks>
            /// If the input string is not JSON this method will serialize the input to JSON in order to deserialize the input into the
            /// requested type.
            /// </remarks>
            /// <param name="serializationService">Serialization service.</param>
            /// <param name="input">Input to deserialize.</param>
            /// <param name="type">Type to convert the input to.</param>
            /// <param name="isDynamic">Indicates whether the type is dynamic.</param>
            /// <param name="deserializedObject">Deserialized object.</param>
            /// <returns>Indicates whether the input was successfully parsed.</returns>
            public static bool TryParseJson( this ISerializationService serializationService, string input, Type type, bool isDynamic, out object deserializedObject )
            {
                input = input.Trim();

                bool isJson = IsJson( input );

                if( isJson )
                {
                    try
                    {
                        if( isDynamic )
                        {
                            // Converting to a DynamicDictionary allows case-insensitive member access when using C# dynamic.. sweet!
                            deserializedObject = serializationService.Deserialize<DynamicDictionary>( input );
                            return true;
                        }

                        deserializedObject = serializationService.Deserialize( input, type );
                        return true;
                    }
                    catch( Exception )
                    {
                        // Swallow the exception here so that we drop down to the next try/catch block
                    }
                }

                try
                {
                    string serializedValue = serializationService.Serialize( input );
                    deserializedObject = serializationService.Deserialize( serializedValue, type );
                    return true;
                }
                catch( Exception )
                {
                    deserializedObject = null;
                    return false;
                }
            }

            /// <summary>Determines if the given string conforms to standard JSON syntax.</summary>
            /// <param name="input">String to evaluate.</param>
            /// <returns>Returns true if the string looks like JSON.</returns>
            public static bool IsJson( string input )
            {
                input = input.Trim();
                return input.StartsWith( "{" ) && input.EndsWith( "}" ) || input.StartsWith( "[" ) && input.EndsWith( "]" );
            }
        }

        /// <summary>Json.NET serialization service.</summary>
        public class JsonNetSerializer : ISerializationService
        {
            /// <summary>Json.NET serializer settings.</summary>
            public JsonSerializerSettings JsonSerializerSettings;

            /// <summary>Initializes a new instance of the <see cref="JsonNetSerializer" /> class.</summary>
            public JsonNetSerializer()
            {
                JsonSerializerSettings = new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    Converters = new JsonConverter[] { new StringEnumConverter(), new DynamicDictionaryConverter() },
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Formatting = Debugger.IsAttached ? Formatting.Indented : Formatting.None,
                };
            }

            /// <summary>Serializes an object to a string.</summary>
            /// <param name="obj">Object to serialize</param>
            /// <returns>String representation of serialized object</returns>
            public string Serialize( object obj )
            {
                return JsonConvert.SerializeObject( obj, JsonSerializerSettings );
            }

            /// <summary>Deserializes a string to an object of arbitrary type <typeparamref name="T" />.</summary>
            /// <typeparam name="T">Arbitrary type to deserializes to</typeparam>
            /// <param name="input">String representation of serialized object</param>
            /// <returns>Object of arbitrary type <typeparamref name="T" />
            /// </returns>
            public T Deserialize<T>( string input )
            {
                return JsonConvert.DeserializeObject<T>( input, JsonSerializerSettings );
            }

            /// <summary>Deserializes a string to an object.</summary>
            /// <param name="input">String representation of serialized object</param>
            /// <param name="type">Type to deserialize to</param>
            /// <returns>Deserialized object.</returns>
            public object Deserialize( string input, Type type )
            {
                return JsonConvert.DeserializeObject( input, type, JsonSerializerSettings );
            }
        }

        /// <summary>Converts a DynamicDictionary to and from JSON.</summary>
        /// <remarks>A majority of the class was borrowed from the ExpandoObjectConverter found in the Json.Net library.</remarks>
        public class DynamicDictionaryConverter : JsonConverter
        {
            /// <summary>Gets a value indicating whether this <see cref="JsonConverter" /> can write JSON.</summary>
            /// <value>
            /// <c>true</c> if this <see cref="JsonConverter" /> can write JSON; otherwise, <c>false</c>.
            /// </value>
            public override bool CanWrite
            {
                get { return false; }
            }

            /// <summary>Writes the JSON representation of the object.</summary>
            /// <param name="writer">The <see cref="JsonWriter" /> to write to.</param>
            /// <param name="value">The value.</param>
            /// <param name="serializer">The calling serializer.</param>
            public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
            {
                // can write is set to false
            }

            /// <summary>Reads the JSON representation of the object.</summary>
            /// <param name="reader">The <see cref="JsonReader" /> to read from.</param>
            /// <param name="objectType">Type of the object.</param>
            /// <param name="existingValue">The existing value of object being read.</param>
            /// <param name="serializer">The calling serializer.</param>
            /// <returns>The object value.</returns>
            public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
            {
                return ReadValue( reader );
            }

            private object ReadValue( JsonReader reader )
            {
                while( reader.TokenType == JsonToken.Comment )
                {
                    if( !reader.Read() )
                        throw CreateJsonSerializationException( reader, "Unexpected end when reading ExpandoObject." );
                }

                switch( reader.TokenType )
                {
                    case JsonToken.StartObject:
                        return ReadObject( reader );
                    case JsonToken.StartArray:
                        return ReadList( reader );
                    default:
                        if( IsPrimitiveToken( reader.TokenType ) )
                            return reader.Value;
                        throw CreateJsonSerializationException( reader, string.Format( CultureInfo.InvariantCulture, "Unexpected token when converting ExpandoObject: {0}", reader.TokenType ) );
                }
            }

            private object ReadList( JsonReader reader )
            {
                IList<object> list = new List<object>();

                while( reader.Read() )
                {
                    switch( reader.TokenType )
                    {
                        case JsonToken.Comment:
                            break;
                        default:
                            list.Add( ReadValue( reader ) );
                            break;
                        case JsonToken.EndArray:
                            return list;
                    }
                }

                throw CreateJsonSerializationException( reader, "Unexpected end when reading ExpandoObject." );
            }

            private object ReadObject( JsonReader reader )
            {
                IDictionary<string, object> expandoObject = new DynamicDictionary();

                while( reader.Read() )
                {
                    switch( reader.TokenType )
                    {
                        case JsonToken.PropertyName:
                            string propertyName = reader.Value.ToString();

                            if( !reader.Read() )
                                throw CreateJsonSerializationException( reader, "Unexpected end when reading ExpandoObject." );

                            expandoObject[propertyName] = ReadValue( reader );
                            break;
                        case JsonToken.Comment:
                            break;
                        case JsonToken.EndObject:
                            return expandoObject;
                    }
                }

                throw CreateJsonSerializationException( reader, "Unexpected end when reading ExpandoObject." );
            }

            /// <summary>Determines whether this instance can convert the specified object type.</summary>
            /// <param name="objectType">Type of the object.</param>
            /// <returns>
            /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
            /// </returns>
            public override bool CanConvert( Type objectType )
            {
                return ( objectType == typeof( DynamicDictionary ) );
            }

            internal static bool IsPrimitiveToken( JsonToken token )
            {
                switch( token )
                {
                    case JsonToken.Integer:
                    case JsonToken.Float:
                    case JsonToken.String:
                    case JsonToken.Boolean:
                    case JsonToken.Undefined:
                    case JsonToken.Null:
                    case JsonToken.Date:
                    case JsonToken.Bytes:
                        return true;
                    default:
                        return false;
                }
            }

            internal static JsonSerializationException CreateJsonSerializationException( JsonReader reader, string message )
            {
                return CreateJsonSerializationException( reader, message, null );
            }

            internal static JsonSerializationException CreateJsonSerializationException( JsonReader reader, string message, Exception ex )
            {
                return CreateJsonSerializationException( reader as IJsonLineInfo, reader.Path, message, ex );
            }

            internal static JsonSerializationException CreateJsonSerializationException( IJsonLineInfo lineInfo, string path, string message, Exception ex )
            {
                message = FormatMessage( lineInfo, path, message );
                return new JsonSerializationException( message, ex );
            }

            internal static string FormatMessage( IJsonLineInfo lineInfo, string path, string message )
            {
                // don't add a fullstop and space when message ends with a new line
                if( !message.EndsWith( Environment.NewLine, StringComparison.Ordinal ) )
                {
                    message = message.Trim();

                    if( !message.EndsWith( "." ) )
                        message += ".";

                    message += " ";
                }

                message += string.Format( CultureInfo.InvariantCulture, "Path '{0}'", path );

                if( lineInfo != null && lineInfo.HasLineInfo() )
                    message += string.Format( CultureInfo.InvariantCulture, ", line {0}, position {1}", lineInfo.LineNumber, lineInfo.LinePosition );

                message += ".";

                return message;
            }
        }
    }

    #endregion Nano.Web.Core.Serialization

    #region Nano.Web.Core.Internal

    namespace Internal
    {
#pragma warning disable 1591 // The DynamicDictionary is meant to be used as a dynamic thus XML comments are unnecessary

        /// <summary>
        /// A dynamic dictionary allowing case-insensitive access and returns null when accessing non-existent properties.
        /// </summary>
        /// <example>
        /// // Non-existent properties will return null dynamic obj = new DynamicDictionary(); var firstName = obj.FirstName;
        /// Assert.Null( firstName ); // Allows case-insensitive property access dynamic obj = new DynamicDictionary();
        /// obj.SuperHeroName = "Superman"; Assert.That( obj.SUPERMAN == "Superman" ); Assert.That( obj.superman == "Superman" );
        /// Assert.That( obj.sUpErMaN == "Superman" );
        /// </example>
        public class DynamicDictionary : DynamicObject, IDictionary<string, object>
        {
            private readonly IDictionary<string, object> _dictionary = new DefaultValueDictionary<string, object>( StringComparer.InvariantCultureIgnoreCase );

            public void Add( string key, object value )
            {
                _dictionary.Add( key, value );
            }

            public bool ContainsKey( string key )
            {
                return _dictionary.ContainsKey( key );
            }

            public ICollection<string> Keys
            {
                get { return _dictionary.Keys; }
            }

            public bool Remove( string key )
            {
                return _dictionary.Remove( key );
            }

            public bool TryGetValue( string key, out object value )
            {
                return _dictionary.TryGetValue( key, out value );
            }

            public ICollection<object> Values
            {
                get { return _dictionary.Values; }
            }

            public object this[string key]
            {
                get
                {
                    object value;
                    _dictionary.TryGetValue( key, out value );
                    return value;
                }
                set { _dictionary[key] = value; }
            }

            public void Add( KeyValuePair<string, object> item )
            {
                _dictionary.Add( item );
            }

            public void Clear()
            {
                _dictionary.Clear();
            }

            public bool Contains( KeyValuePair<string, object> item )
            {
                return _dictionary.Contains( item );
            }

            public void CopyTo( KeyValuePair<string, object>[] array, int arrayIndex )
            {
                _dictionary.CopyTo( array, arrayIndex );
            }

            public int Count
            {
                get { return _dictionary.Count; }
            }

            public bool IsReadOnly
            {
                get { return _dictionary.IsReadOnly; }
            }

            public bool Remove( KeyValuePair<string, object> item )
            {
                return _dictionary.Remove( item );
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                return _dictionary.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _dictionary.GetEnumerator();
            }

            #region Nested Types

            /// <summary>
            /// A dictionary that returns the default value when accessing keys that do not exist in the dictionary.
            /// </summary>
            public class DefaultValueDictionary<TKey, TValue> : IDictionary<TKey, TValue>
            {
                private readonly IDictionary<TKey, TValue> _dictionary;

                /// <summary>
                /// Initializes a dictionary that returns the default value when accessing keys that do not exist in the dictionary.
                /// </summary>
                public DefaultValueDictionary()
                {
                    _dictionary = new Dictionary<TKey, TValue>();
                }

                /// <summary>Initializes with an existing dictionary.</summary>
                public DefaultValueDictionary( IDictionary<TKey, TValue> dictionary )
                {
                    _dictionary = dictionary;
                }

                /// <summary>Initializes using the given equality comparer.</summary>
                public DefaultValueDictionary( IEqualityComparer<TKey> comparer )
                {
                    _dictionary = new Dictionary<TKey, TValue>( comparer );
                }

                public void Add( TKey key, TValue value )
                {
                    _dictionary.Add( key, value );
                }

                public bool ContainsKey( TKey key )
                {
                    return _dictionary.ContainsKey( key );
                }

                public ICollection<TKey> Keys
                {
                    get { return _dictionary.Keys; }
                }

                public bool Remove( TKey key )
                {
                    return _dictionary.Remove( key );
                }

                public bool TryGetValue( TKey key, out TValue value )
                {
                    return _dictionary.TryGetValue( key, out value );
                }

                public ICollection<TValue> Values
                {
                    get { return _dictionary.Values; }
                }

                public TValue this[TKey key]
                {
                    get
                    {
                        TValue value;
                        _dictionary.TryGetValue( key, out value );
                        return value;
                    }
                    set { _dictionary[key] = value; }
                }

                public void Add( KeyValuePair<TKey, TValue> item )
                {
                    _dictionary.Add( item );
                }

                public void Clear()
                {
                    _dictionary.Clear();
                }

                public bool Contains( KeyValuePair<TKey, TValue> item )
                {
                    return _dictionary.Contains( item );
                }

                public void CopyTo( KeyValuePair<TKey, TValue>[] array, int arrayIndex )
                {
                    _dictionary.CopyTo( array, arrayIndex );
                }

                public int Count
                {
                    get { return _dictionary.Count; }
                }

                public bool IsReadOnly
                {
                    get { return _dictionary.IsReadOnly; }
                }

                public bool Remove( KeyValuePair<TKey, TValue> item )
                {
                    return _dictionary.Remove( item );
                }

                public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
                {
                    return _dictionary.GetEnumerator();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return _dictionary.GetEnumerator();
                }
            }

            #endregion Nested Types

            public override bool TryGetMember( GetMemberBinder binder, out object result )
            {
                result = _dictionary[binder.Name];
                return true;
            }

            public override bool TrySetMember( SetMemberBinder binder, object value )
            {
                if( _dictionary.ContainsKey( binder.Name ) )
                    _dictionary[binder.Name] = value;
                else
                    _dictionary.Add( binder.Name, value );

                return true;
            }

            public override bool TryInvokeMember( InvokeMemberBinder binder, object[] args, out object result )
            {
                if( _dictionary.ContainsKey( binder.Name ) && _dictionary[binder.Name] is Delegate )
                {
                    var delegateValue = ( Delegate ) _dictionary[binder.Name];
                    result = delegateValue.DynamicInvoke( args );
                    return true;
                }

                return base.TryInvokeMember( binder, args, out result );
            }
        }

#pragma warning restore 1591 // The DynamicDictionary is meant to be used as a dynamic thus XML comments are unnecessary

        /// <summary>Maps file extensions to MIME types.</summary>
        public static class FileExtensionToContentTypeConverter
        {
            /// <summary>Creates a new file extension converter with a set of default mappings.</summary>
            static FileExtensionToContentTypeConverter()
            {
                Mappings = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase )
                {
                    { ".atom", "application/atom+xml" },
                    { ".avi", "video/x-msvideo" },
                    { ".bmp", "image/bmp" },
                    { ".chm", "application/octet-stream" },
                    { ".css", "text/css" },
                    { ".csv", "application/octet-stream" },
                    { ".doc", "application/msword" },
                    { ".docm", "application/vnd.ms-word.document.macroEnabled.12" },
                    { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                    { ".gif", "image/gif" },
                    { ".htm", "text/html" },
                    { ".html", "text/html" },
                    { ".ical", "text/calendar" },
                    { ".icalendar", "text/calendar" },
                    { ".ico", "image/x-icon" },
                    { ".ics", "text/calendar" },
                    { ".jar", "application/java-archive" },
                    { ".java", "application/octet-stream" },
                    { ".jpe", "image/jpeg" },
                    { ".jpeg", "image/jpeg" },
                    { ".jpg", "image/jpeg" },
                    { ".js", "application/javascript" },
                    { ".jsx", "text/jscript" },
                    { ".m3u", "audio/x-mpegurl" },
                    { ".m4a", "audio/mp4" },
                    { ".m4v", "video/mp4" },
                    { ".map", "text/plain" },
                    { ".mdb", "application/x-msaccess" },
                    { ".mid", "audio/mid" },
                    { ".midi", "audio/mid" },
                    { ".mov", "video/quicktime" },
                    { ".mp2", "video/mpeg" },
                    { ".mp3", "audio/mpeg" },
                    { ".mp4", "video/mp4" },
                    { ".mp4v", "video/mp4" },
                    { ".mpa", "video/mpeg" },
                    { ".mpe", "video/mpeg" },
                    { ".mpeg", "video/mpeg" },
                    { ".mpg", "video/mpeg" },
                    { ".mpp", "application/vnd.ms-project" },
                    { ".mpv2", "video/mpeg" },
                    { ".oga", "audio/ogg" },
                    { ".ogg", "video/ogg" },
                    { ".ogv", "video/ogg" },
                    { ".otf", "font/otf" },
                    { ".pdf", "application/pdf" },
                    { ".png", "image/png" },
                    { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
                    { ".ps", "application/postscript" },
                    { ".qt", "video/quicktime" },
                    { ".rtf", "application/rtf" },
                    { ".svg", "image/svg+xml" },
                    { ".swf", "application/x-shockwave-flash" },
                    { ".tar", "application/x-tar" },
                    { ".tgz", "application/x-compressed" },
                    { ".tif", "image/tiff" },
                    { ".tiff", "image/tiff" },
                    { ".txt", "text/plain" },
                    { ".wav", "audio/wav" },
                    { ".wm", "video/x-ms-wm" },
                    { ".wma", "audio/x-ms-wma" },
                    { ".wmv", "video/x-ms-wmv" },
                    { ".xaml", "application/xaml+xml" },
                    { ".xap", "application/x-silverlight-app" },
                    { ".xbap", "application/x-ms-xbap" },
                    { ".xbm", "image/x-xbitmap" },
                    { ".xdr", "text/plain" },
                    { ".xls", "application/vnd.ms-excel" },
                    { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
                    { ".xml", "text/xml" },
                    { ".zip", "application/x-zip-compressed" },
                };
            }

            /// <summary>File extension and MIME type mapping.</summary>
            public static IDictionary<string, string> Mappings { get; set; }

            /// <summary>Gets the MIME type for the extension.</summary>
            /// <param name="extension">The file extension.</param>
            /// <returns>The associated MIME type for the file extension or NULL.</returns>
            public static string GetContentType( string extension )
            {
                string contentType;
                Mappings.TryGetValue( extension, out contentType );
                return contentType;
            }
        }

        /// <summary>
        /// Provides methods for extracting documentation for types, methods, properties, and fields from .NET XML comments.
        /// </summary>
        public static class XmlDocumentationHelper
        {
            /// <summary>Cache for storing XML documentation.</summary>
            public static readonly Dictionary<Assembly, XmlDocument> XmlDocumentCache = new Dictionary<Assembly, XmlDocument>();

            /// <summary>Gets the XML comments for the given assembly.</summary>
            /// <param name="assembly">Assembly to get XML documentation for.</param>
            /// <returns>
            /// Returns null if the XML file for the assembly does not exist else returns the documentation in the form of an
            /// XmlDocument.
            /// </returns>
            public static XmlDocument GetXmlDocumentation( this Assembly assembly )
            {
                if( assembly == null )
                    throw new ArgumentNullException( "assembly", @"The parameter 'assembly' must not be null." );

                if( XmlDocumentCache.ContainsKey( assembly ) )
                    return XmlDocumentCache[assembly];

                string assemblyFilename = assembly.CodeBase;
                const string prefix = "file:///";

                if( string.IsNullOrWhiteSpace( assemblyFilename ) == false && assemblyFilename.StartsWith( prefix ) )
                {
                    try
                    {
                        string xmlDocumentationPath = Path.ChangeExtension( assemblyFilename.Substring( prefix.Length ), ".xml" );

                        if( File.Exists( xmlDocumentationPath ) )
                        {
                            var xmlDocument = new XmlDocument();
                            xmlDocument.Load( xmlDocumentationPath );
                            XmlDocumentCache.Add( assembly, xmlDocument );
                            return xmlDocument;
                        }
                    }
                    catch( Exception )
                    {
                    }
                }

                return null;
            }

            /// <summary>Gets the XML comments for the given type in the form of an XmlElement.</summary>
            /// <param name="type">Type to get XML comments for.</param>
            /// <returns>
            /// Returns null if the XML file for the assembly does not exist or if comments for the given type do not exist else
            /// returns an XmlElement for the given type.
            /// </returns>
            public static XmlElement GetXmlDocumentation( this Type type )
            {
                if( type == null )
                    throw new ArgumentNullException( "type", @"The parameter 'type' must not be null." );

                XmlDocument xmlDocument = type.Assembly.GetXmlDocumentation();

                if( xmlDocument == null )
                    return null;

                string xmlMemberName = string.Format( "T:{0}", GetFullTypeName( type ) );
                var memberElement = xmlDocument.GetMemberByName( xmlMemberName );
                return memberElement;
            }

            /// <summary>Gets the XML comments for the given method in the form of an XmlElement.</summary>
            /// <param name="methodInfo">Method to get XML comments for.</param>
            /// <returns>
            /// Returns null if the XML file for the assembly does not exist or if comments for the given method do not exist else
            /// returns an XmlElement representing the comments for the given method.
            /// </returns>
            public static XmlElement GetXmlDocumentation( this MethodInfo methodInfo )
            {
                if( methodInfo == null )
                    throw new ArgumentNullException( "methodInfo", @"The parameter 'methodInfo' must not be null." );

                Type declaryingType = methodInfo.DeclaringType;

                if( declaryingType == null )
                    return null;

                XmlDocument xmlDocument = declaryingType.Assembly.GetXmlDocumentation();

                if( xmlDocument == null )
                    return null;

                string parameterList = "";

                foreach( ParameterInfo parameterInfo in methodInfo.GetParameters().OrderBy( x => x.Position ) )
                {
                    if( parameterList.Length > 0 )
                    {
                        parameterList += ",";
                    }

                    parameterList += GetParameterTypeName( methodInfo, parameterInfo );
                }

                Type[] genericArguments = methodInfo.GetGenericArguments();
                string xmlMethodName = string.Format( "M:{0}.{1}{2}{3}", GetFullTypeName( methodInfo.DeclaringType ), methodInfo.Name, genericArguments.Length > 0 ? string.Format( "``{0}", genericArguments.Length ) : "", parameterList.Length > 0 ? string.Format( "({0})", parameterList ) : "" );
                XmlElement memberElement = xmlDocument.GetMemberByName( xmlMethodName );
                return memberElement;
            }

            /// <summary>Gets the XML comments for the given field in the form of an XmlElement.</summary>
            /// <param name="fieldInfo">Field to get XML comments for.</param>
            /// <returns>
            /// Returns null if the XML file for the assembly does not exist or if comments for the given field do not exist else
            /// returns an XmlElement representing the comments for the given field.
            /// </returns>
            public static XmlElement GetXmlDocumentation( this FieldInfo fieldInfo )
            {
                if( fieldInfo == null )
                    throw new ArgumentNullException( "fieldInfo", @"The parameter 'fieldInfo' must not be null." );

                Type declaryingType = fieldInfo.DeclaringType;

                if( declaryingType == null )
                    return null;

                XmlDocument xmlDocument = declaryingType.Assembly.GetXmlDocumentation();

                if( xmlDocument == null )
                    return null;

                string xmlPropertyName = string.Format( "F:{0}.{1}", GetFullTypeName( declaryingType ), fieldInfo.Name );
                var memberElement = xmlDocument.GetMemberByName( xmlPropertyName );
                return memberElement;
            }

            /// <summary>Gets the XML comments for the given property in the form of an XmlElement.</summary>
            /// <param name="propertyInfo">Property to get XML comments for.</param>
            /// <returns>
            /// Returns null if the XML file for the assembly does not exist or if comments for the given property do not exist else
            /// returns an XmlElement representing the comments for the given property.
            /// </returns>
            public static XmlElement GetXmlDocumentation( this PropertyInfo propertyInfo )
            {
                if( propertyInfo == null )
                    throw new ArgumentNullException( "propertyInfo", @"The parameter 'propertyInfo' must not be null." );

                Type declaryingType = propertyInfo.DeclaringType;

                if( declaryingType == null )
                    return null;

                XmlDocument xmlDocument = declaryingType.Assembly.GetXmlDocumentation();

                if( xmlDocument == null )
                    return null;

                string xmlPropertyName = string.Format( "P:{0}.{1}", GetFullTypeName( declaryingType ), propertyInfo.Name );
                var memberElement = xmlDocument.GetMemberByName( xmlPropertyName );
                return memberElement;
            }

            /// <summary>Gets the XML comments for the given member in the form of an XmlElement.</summary>
            /// <remarks>
            /// This method attempts to generically handle any reflected members that do not have an explicit GetXmlDocumentation
            /// method for the member type. This has been tested to work with PropertyInfo, FieldInfo, and EventInfo but may not work
            /// for all member types.
            /// </remarks>
            /// <param name="memberInfo">Member to get XML comments for.</param>
            /// <returns>
            /// Returns null if the XML file for the assembly does not exist or if comments for the given member do not exist else
            /// returns an XmlElement representing the comments for the given member.
            /// </returns>
            public static XmlElement GetXmlDocumentation( this MemberInfo memberInfo )
            {
                if( memberInfo == null )
                    throw new ArgumentNullException( "memberInfo", @"The parameter 'memberInfo' must not be null." );

                Type declaryingType = memberInfo.DeclaringType;

                if( declaryingType == null )
                    return null;

                XmlDocument xmlDocument = declaryingType.Assembly.GetXmlDocumentation();

                if( xmlDocument == null )
                    return null;

                string xmlPropertyName = string.Format( "{0}:{1}.{2}", memberInfo.MemberType.ToString()[0], GetFullTypeName( declaryingType ), memberInfo.Name );
                var memberElement = xmlDocument.GetMemberByName( xmlPropertyName );
                return memberElement;
            }

            /// <summary>Gets the parameter type name used in XML documentation.</summary>
            /// <remarks>
            /// This method is needed due to how nullable type names and generic parameters are formatted in XML documentation.
            /// </remarks>
            /// <param name="methodInfo">MethodInfo where the parameter is implemented.</param>
            /// <param name="parameterInfo">ParameterInfo to get the parameter type name from.</param>
            /// <returns>Parameter type name.</returns>
            public static string GetParameterTypeName( this MethodInfo methodInfo, ParameterInfo parameterInfo )
            {
                // Handle nullable types
                Type underlyingType = Nullable.GetUnderlyingType( parameterInfo.ParameterType );

                if( underlyingType != null )
                    return string.Format( "System.Nullable{{{0}}}", GetFullTypeName( underlyingType ) );

                string parameterTypeFullName = GetFullTypeName( parameterInfo.ParameterType );

                // Handle generic types
                if( string.IsNullOrWhiteSpace( parameterTypeFullName ) )
                {
                    string typeName = parameterInfo.ParameterType.Name;
                    Type genericParameter = methodInfo.GetGenericArguments().FirstOrDefault( x => x.Name == typeName );

                    if( genericParameter != null )
                    {
                        int genericParameterPosition = genericParameter.GenericParameterPosition;
                        return "``" + genericParameterPosition;
                    }
                }

                return parameterTypeFullName;
            }

            /// <summary>Gets the full type name.</summary>
            /// <remarks>This method is needed in order to replace a nested types '+' plus sign with a '.' dot.</remarks>
            /// <param name="type">Type.</param>
            /// <returns>Full type name.</returns>
            public static string GetFullTypeName( Type type )
            {
                string parameterTypeFullName = type.FullName;

                if( string.IsNullOrWhiteSpace( parameterTypeFullName ) == false )
                    return parameterTypeFullName.Replace( "+", "." );

                return null;
            }

            /// <summary>Gets a member by name.</summary>
            /// <param name="xmlDocument">XmlDocument to search for the member in.</param>
            /// <param name="memberName">Member name to search for.</param>
            /// <returns>Returns null if the member is not found or returns an XmlElement representing the found member.</returns>
            public static XmlElement GetMemberByName( this XmlDocument xmlDocument, string memberName )
            {
                if( xmlDocument == null )
                    return null;

                if( string.IsNullOrWhiteSpace( memberName ) )
                    throw new ArgumentNullException( "memberName", @"The parameter 'memberName' must not be null." );

                XmlElement docElement = xmlDocument["doc"];

                if( docElement == null )
                    return null;

                XmlElement membersElement = docElement["members"];

                if( membersElement == null )
                    return null;

                foreach( XmlElement member in membersElement )
                {
                    if( member == null )
                        continue;

                    if( member.Attributes["name"].Value == memberName )
                        return member;
                }

                return null;
            }

            /// <summary>Gets the summary text.</summary>
            /// <param name="xmlElement">XmlElement.</param>
            /// <returns>Summary text.</returns>
            public static string GetSummary( this XmlElement xmlElement )
            {
                string summary = xmlElement.GetNodeText( "summary" );
                return summary;
            }

            /// <summary>Gets the remarks text.</summary>
            /// <param name="xmlElement">XmlElement.</param>
            /// <returns>Remarks text.</returns>
            public static string GetRemarks( this XmlElement xmlElement )
            {
                string summary = xmlElement.GetNodeText( "remarks" );
                return summary;
            }

            /// <summary>Gets the description text for a parameter.</summary>
            /// <param name="xmlElement">XmlElement.</param>
            /// <param name="parameterName">Parameter name.</param>
            /// <returns>Description text.</returns>
            public static string GetParameterDescription( this XmlElement xmlElement, string parameterName )
            {
                string summary = xmlElement.GetNodeText( string.Format( "param[@name='{0}']", parameterName ) );
                return summary;
            }

            /// <summary>Gets the node text from the given XmlElement.</summary>
            /// <param name="xmlElement">XmlElement.</param>
            /// <param name="xpath">XPath to locate the node.</param>
            /// <returns>Node text.</returns>
            public static string GetNodeText( this XmlElement xmlElement, string xpath )
            {
                if( xmlElement == null )
                    return null;

                if( string.IsNullOrWhiteSpace( xpath ) )
                    return null;

                XmlNode summaryNode = xmlElement.SelectSingleNode( xpath );

                if( summaryNode == null )
                    return null;

                // Note we are explicitly using InnerXml so that we can support adding HTML in the XML comments! =)
                string summary = FormatXmlInnerText( summaryNode.InnerXml );

                return summary;
            }

            /// <summary>Formats XML inner text.</summary>
            /// <remarks>This method attempts to remove excessive whitespace yet maintain line breaks.</remarks>
            /// <param name="xmlInnerText">XML inner text to format.</param>
            /// <returns>Formatted text.</returns>
            public static string FormatXmlInnerText( string xmlInnerText )
            {
                if( string.IsNullOrWhiteSpace( xmlInnerText ) )
                    return xmlInnerText;

                string[] lines = xmlInnerText.Trim().Replace( "\r", "" ).Split( new[] { "\n" }, StringSplitOptions.None );
                string formattedText = "";

                foreach( string line in lines )
                {
                    if( formattedText.Length > 0 )
                        formattedText += "\n";

                    formattedText += line.Trim();
                }

                return formattedText;
            }
        }

        /// <summary>Provides type conversion helpers.</summary>
        public static class TypeConverter
        {
            /// <summary>
            /// Cache that stores the default value for value types to reduce unnecessary redundant Activator.CreateInstance calls.
            /// </summary>
            public static readonly ConcurrentDictionary<Type, object> GetDefaultValueCache = new ConcurrentDictionary<Type, object>();

            /// <summary>Converts the given value to the given type.</summary>
            /// <param name="value">Value to convert.</param>
            /// <param name="type">Type to convert the given value to.</param>
            /// <returns>Converted value.</returns>
            /// <exception cref="TypeConversionException">Thrown when an error occurs attempting to convert a value to an enum.</exception>
            /// <exception cref="TypeConversionException">Thrown when an error occurs attempting to convert a value to a type.</exception>
            public static object ConvertType( object value, Type type )
            {
                // Handle DBNull
                if( value == DBNull.Value )
                    value = null;

                // Handle value type conversion of null to the values types default value
                if( value == null && type.IsValueType )
                    return GetDefaultValue( type ); // Extension method internally handles caching

                Type underlyingType = Nullable.GetUnderlyingType( type ) ?? type;

                // Handle Enums
                if( underlyingType.IsEnum )
                {
                    try
                    {
                        // ReSharper disable once PossibleNullReferenceException // Because an enum and a nullable enum are both value types, it's actually not possible to reach the next line of code when the value variable is null
                        value = Enum.Parse( underlyingType, value.ToString(), true );
                    }
                    catch( Exception exception )
                    {
                        throw new TypeConversionException( String.Format( "An error occurred while attempting to convert the value '{0}' to an enum of type '{1}'", value, underlyingType ), exception );
                    }
                }

                try
                {
                    // Handle Guids
                    if( underlyingType == typeof( Guid ) )
                    {
                        if( value is string )
                            value = new Guid( value as string );
                        if( value is byte[] )
                            value = new Guid( value as byte[] );
                    }

                    object result = Convert.ChangeType( value, underlyingType );

                    return result;
                }
                catch( Exception exception )
                {
                    throw new TypeConversionException( String.Format( "An error occurred while attempting to convert the value '{0}' to type '{1}'", value, underlyingType ), exception );
                }
            }

            /// <summary>Gets the default value for the given type.</summary>
            /// <param name="type">Type to get the default value for.</param>
            /// <returns>Default value of the given type.</returns>
            public static object GetDefaultValue( Type type )
            {
                return type.IsValueType ? GetDefaultValueCache.GetOrAdd( type, Activator.CreateInstance ) : null;
            }

            /// <summary>Converts an object to a int.</summary>
            /// <param name="obj">The object to convert to an int.</param>
            /// <returns>The value of the object as an int.</returns>
            public static int ToInt( this object obj )
            {
                return (int)obj;
            }

            /// <summary>Thrown when an exception occurs while converting a value from one type to another.</summary>
            [Serializable]
            public class TypeConversionException : Exception
            {
                /// <summary>Instantiates a new <see cref="TypeConversionException" /> with a specified error message.</summary>
                /// <param name="message">The message that describes the error.</param>
                /// <param name="innerException">
                /// The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner
                /// exception is specified.
                /// </param>
                public TypeConversionException( string message, Exception innerException )
                    : base( message, innerException )
                {
                }
            }
        }
    }

    #endregion Nano.Web.Core.Internal

    #region Open Source Attributions

    /*
Open Source Attributions
------------------------
Nano made use of substantial portions and/or was heavily influenced by the following open source software:

 - Nancy: https://github.com/NancyFx/Nancy

        The MIT License
        Copyright (c) 2010 Andreas Håkansson, Steven Robbins and contributors
        License available at: https://github.com/NancyFx/Nancy/blob/master/license.txt

 - Katana Project: http://katanaproject.codeplex.com/

        Apache License
        Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
        License available at: http://katanaproject.codeplex.com/SourceControl/latest#LICENSE.txt

 - DynamicDictionary: https://github.com/randyburden/DynamicDictionary

        The MIT License
        Copyright (c) 2014 Randy Burden ( http://randyburden.com ) All rights reserved.
        License available at: https://github.com/randyburden/DynamicDictionary/blob/master/LICENSE

 - JSON.NET: https://github.com/JamesNK/Newtonsoft.Json

        The MIT License
        Copyright (c) 2007 James Newton-King
        License available at: https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md
    
*/

    #endregion Open Source Attributions
}