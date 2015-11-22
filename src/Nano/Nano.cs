/*
    Nano v0.14.0
    
    Nano is a .NET cross-platform micro web framework for building web-based HTTP services and websites.

    To find out more, visit the project home page at: https://github.com/AmbitEnergyLabs/Nano

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
using System.Text.RegularExpressions;
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

        /// <summary>The optional request handler to invoke when a request was not handled by any other request or event handler.</summary>
        public IRequestHandler UnhandledRequestHandler;

        /// <summary>The background tasks.</summary>
        public IList<BackgroundTask> BackgroundTasks = new List<BackgroundTask>();

        /// <summary>The serialization service used to serialize/deserialize requests and responses.</summary>
        public ISerializationService SerializationService = new JsonNetSerializer();

        /// <summary>Gets the default method url path. Defaulted to: '/api/' + type.Name</summary>
        public Func<Type, string> GetDefaultMethodUrlPath = type => "/api/" + type.Name;

        /// <summary>Determines whether errors are logged to the operating system event log.</summary>
        public bool LogErrorsToEventLog = true;

        /// <summary>Application name.</summary>
        public string ApplicationName = AppDomain.CurrentDomain.FriendlyName;

        /// <summary>The limit on the number of query string variables, form fields, or multipart sections in a request. Default is 1,000. The reason to limit the number of parameters processed by the server is detailed in the following security notice regarding a particular 'Collisions in HashTable' denial-of-service (DoS) attack vector: http://www.ocert.org/advisories/ocert-2011-003.html </summary>
        public int RequestParameterLimit = 1000;

        /// <summary>Initializes a new instance of the <see cref="NanoConfiguration" /> class.</summary>
        public NanoConfiguration()
        {
            RequestHandlers.Add( new MetadataRequestHandler( "/metadata/GetNanoMetadata", DefaultEventHandler ) );
            this.EnableCorrelationId();
            this.DisableKeepAlive();
            this.EnableElapsedMillisecondsResponseHeader();
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
                string caseSensitiveUrlPath = string.Format( "/{0}/{1}", urlPath, methodInfo.Name );
                string methodUrlPath = caseSensitiveUrlPath.ToLower();
                var requestHandler = new MethodRequestHandler( methodUrlPath, eventHandler ?? DefaultEventHandler, methodInfo ) { MetadataProvider = metadataProvider ?? DefaultMethodRequestHandlerMetadataProvider, CaseSensitiveUrlPath = caseSensitiveUrlPath };
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
        /// <param name="returnHttp404WhenFileWasNotFound">Should return an 'HTTP 404 - File Not Found' when no file was found.</param>
        /// <param name="defaultDocuments">The default documents to serve when the root of a directory is requested. Default is index.html.</param>
        /// <returns><see cref="DirectoryRequestHandler" />.</returns>
        public DirectoryRequestHandler AddDirectory( string urlPath, string directoryPath, EventHandler eventHandler = null, bool returnHttp404WhenFileWasNotFound = false, IList<string> defaultDocuments = null )
        {
            var handler = new DirectoryRequestHandler( urlPath, eventHandler ?? DefaultEventHandler, directoryPath, returnHttp404WhenFileWasNotFound, defaultDocuments );
            RequestHandlers.Add( handler );
            return handler;
        }

        /// <summary>Adds the background task.</summary>
        /// <param name="taskName">The task name.</param>
        /// <param name="millisecondInterval">The millisecond interval.</param>
        /// <param name="task">The task which takes no parameters and returns a result of type <see cref="object"/>.</param>
        /// <param name="allowOverlappingRuns">If set to <c>true</c> [allow overlapping runs].</param>
        /// <param name="backgroundTaskEventHandler">The background task event handler.</param>
        /// <returns>The added <see cref="BackgroundTask"/>.</returns>
        public BackgroundTask AddBackgroundTask( string taskName, int millisecondInterval, Func<object> task, bool allowOverlappingRuns = false, BackgroundTaskEventHandler backgroundTaskEventHandler = null )
        {
            var backgroundTask = new BackgroundTask { Name = taskName, MillisecondInterval = millisecondInterval, Task = task, AllowOverlappingRuns = allowOverlappingRuns, BackgroundTaskEventHandler = backgroundTaskEventHandler ?? DefaultBackgroundTaskEventHandler };
            BackgroundTasks.Add( backgroundTask);
            return backgroundTask;
        }

        /// <summary>Returns a <see cref="System.String" /> containing all request handler types and paths.</summary>
        /// <returns>Returns a <see cref="System.String" /> containing all request handler types and paths.</returns>
		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine( "[Nano Configuration]" );

            foreach ( IRequestHandler handler in RequestHandlers )
                builder.AppendFormat( "Handler [{0}] -> Path: {1}{2}", handler.GetType().Name, handler.UrlPath, Environment.NewLine );

            return builder.ToString();
		}
    }

    /// <summary>The context of the current web request.</summary>
    public class NanoContext : IDisposable
    {
        /// <summary>Correlation / request identifier.</summary>
        public string CorrelationId;

        /// <summary>The current user.</summary>
        public IUserIdentity CurrentUser;

        /// <summary>The errors for this context.</summary>
        public IList<Exception> Errors = new List<Exception>();

        /// <summary>Indicates if the request has been handled.</summary>
        public bool Handled;

        /// <summary>The underlying HTTP host context.</summary>
        /// <remarks>
        /// This enables accessing host-only features although this will make code dependent on this object non-host agnostic. The
        /// main intent of this is to enable features that are not currently supported by Nano directly.
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

        /// <summary>The initial timestamp of the current HTTP request.</summary>
        public DateTime RequestTimestamp;

        /// <summary>The outgoing response.</summary>
        public NanoResponse Response;

        /// <summary>The root folder path.</summary>
        /// <value>The root folder path.</value>
        public string RootFolderPath;

        /// <summary>
        /// Flag to indicate if a dispose has been called already
        /// </summary>
        private bool _disposed;

        /// <summary>Initializes a new instance of the <see cref="NanoContext" /> class.</summary>
        /// <param name="nanoRequest">The nano request.</param>
        /// <param name="nanoResponse">The nano response.</param>
        /// <param name="nanoConfiguration">The nano configuration.</param>
        /// <param name="requestTimestamp">The initial timestamp of the current HTTP request.</param>
        public NanoContext( NanoRequest nanoRequest, NanoResponse nanoResponse, NanoConfiguration nanoConfiguration, DateTime requestTimestamp )
        {
            nanoRequest.NanoContext = this;
            Request = nanoRequest;
            nanoResponse.NanoContext = this;
            Response = nanoResponse;
            NanoConfiguration = nanoConfiguration;
            RequestTimestamp = requestTimestamp;
        }

        /// <summary>Disposes any disposable items in the <see cref="Items" /> dictionary.</summary>
        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        /// <summary>
        /// Disposes any disposable items in the <see cref="Items" /> dictionary.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose( bool disposing )
        {
            if ( _disposed )
                return;

            if ( disposing )
            {
                // Free any other managed objects here. 
                foreach ( IDisposable disposableItem in Items.Values.OfType<IDisposable>() )
                    disposableItem.Dispose();

                Items.Clear();
            }

            // Free any unmanaged objects here. 
            _disposed = true;
        }
    }

    /// <summary>Holds properties associated with the current HTTP request.</summary>
    public class NanoRequest
    {
        /// <summary>IP address of the client.</summary>
        public string ClientIpAddress;

        /// <summary>The files sent by the client in a multipart message.</summary>
        public IList<HttpFile> Files;

        /// <summary>The HTTP form body parameters sent by the client.</summary>
        public NameValueCollection FormBodyParameters;

        /// <summary>The HTTP header parameters sent by the client.</summary>
        public NameValueCollection HeaderParameters;

        /// <summary>HTTP Method ( GET, POST, HEAD, etc. ).</summary>
        public string HttpMethod;

        /// <summary>The Nano context.</summary>
        public NanoContext NanoContext;

        /// <summary>The HTTP query string parameters sent by the client.</summary>
        public NameValueCollection QueryStringParameters;

        /// <summary>Full URL being requested.</summary>
        public Url Url;

        /// <summary>Initializes a new instance of the <see cref="NanoRequest" /> class.</summary>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <param name="url">The URL being requested.</param>
        /// <param name="requestStream">Request stream body.</param>
        /// <param name="queryStringParameters">The HTTP query string parameters sent by the client.</param>
        /// <param name="formBodyParameters">The HTTP form body parameters sent by the client.</param>
        /// <param name="headerParameters">The HTTP header parameters sent by the client.</param>
        /// <param name="files">The files sent by the client in a multipart message.</param>
        /// <param name="clientIpAddress">IP address of the client.</param>
        public NanoRequest( string httpMethod, Url url, RequestStream requestStream, NameValueCollection queryStringParameters, NameValueCollection formBodyParameters, NameValueCollection headerParameters, IList<HttpFile> files, string clientIpAddress )
        {
            HttpMethod = httpMethod;
            Url = url;
            RequestBody = requestStream;
            QueryStringParameters = queryStringParameters ?? new NameValueCollection();
            FormBodyParameters = formBodyParameters ?? new NameValueCollection();
            HeaderParameters = headerParameters ?? new NameValueCollection();
            Files = files ?? new List<HttpFile>();
        }

        /// <summary>Gets a <see cref="RequestStream"/> that can be used to read the incoming HTTP body</summary>
        public RequestStream RequestBody;
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
        public string Domain;

        /// <summary>When the cookie should expire</summary>
        /// <value>
        /// A <see cref="DateTime" /> instance containing the date and time when the cookie should expire; otherwise
        /// <see langword="null" /> if it should expire at the end of the session.
        /// </value>
        public DateTime? Expires;

        /// <summary>The name of the cookie</summary>
        public string Name;

        /// <summary>The path to restrict the cookie to</summary>
        public string Path;

        /// <summary>The value of the cookie</summary>
        public string Value;

        /// <summary>Whether a cookie is accessible by client-side script.</summary>
        public bool HttpOnly;

        /// <summary>Whether the cookie is secure ( HTTPS only )</summary>
        public bool Secure;

        /// <summary>Returns a <see cref="System.String" /> that represents this instance as a valid Set-Cookie header value.</summary>
        /// <returns>A <see cref="System.String" /> that represents this instance as a valid Set-Cookie header value.</returns>
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

    /// <summary>Represents a file that was captured in a HTTP multipart/form-data request</summary>
    public class HttpFile
    {
        /// <summary>Initializes a new instance of an <see cref="HttpFile"/>.</summary>
        /// <paramref name="contentType">The content type of the file.</paramref>
        /// <paramref name="fileName">The name of the file.</paramref>
        /// <paramref name="value">The content of the file.</paramref>
        /// <paramref name="name">The name of the field that uploaded the file.</paramref>
        public HttpFile(string contentType, string fileName, Stream value, string name)
        {
            ContentType = contentType;
            FileName = fileName;
            Value = value;
            Name = name;
        }

        /// <summary>The type of the content.</summary>
        public readonly string ContentType;

        /// <summary>The name of the file.</summary>
        public readonly string FileName;

        /// <summary>The form element name of the file.</summary>
        public readonly string Name;

        /// <summary>A <see cref="Stream"/> containing the contents of the file.</summary>
        /// <remarks>This is a <see cref="Multipart.HttpMultipartSubStream"/> instance that sits ontop of the request stream.</remarks>
        public readonly Stream Value;
    }

    /// <summary>A <see cref="Stream"/> decorator that can handle moving the stream out from memory and on to disk when the contents reaches a certain length.</summary>
    public class RequestStream : Stream
    {
        /// <summary>The number of bytes that indicate the input-stream buffering threshold before the data is buffered transparently onto disk. The default is 80 kilobytes.</summary>
        public static long RequestLengthDiskThreshold = 81920;

        private bool _disableStreamSwitching;
        private readonly long _thresholdLength;
        private bool _isSafeToDisposeStream;
        private Stream _stream;

        /// <summary>Buffer size for copy operations</summary>
        private const int BufferSize = 4096;

        /// <summary>Initializes a new instance of the <see cref="RequestStream"/> class.</summary>
        /// <param name="stream">The <see cref="Stream"/> that should be handled by the request stream.</param>
        /// <param name="expectedLength">The expected length of the contents in the stream.</param>
        /// <param name="thresholdLength">The content length that will trigger the stream to be moved out of memory.</param>
        /// <param name="disableStreamSwitching">If set to <see langword="true"/> the stream will never explicitly be moved to disk.</param>
        public RequestStream( Stream stream, long expectedLength, long thresholdLength, bool disableStreamSwitching )
        {
            _thresholdLength = thresholdLength;
            _disableStreamSwitching = disableStreamSwitching;
            _stream = stream ?? CreateDefaultMemoryStream( expectedLength );

            ThrowExceptionIfCtorParametersWereInvalid( _stream, expectedLength, _thresholdLength );

            if ( !MoveStreamOutOfMemoryIfExpectedLengthExceedSwitchLength( expectedLength ) )
                MoveStreamOutOfMemoryIfContentsLengthExceedThresholdAndSwitchingIsEnabled();

            if ( !_stream.CanSeek )
            {
                var task = MoveToWritableStream();
                task.Wait();

                if ( task.IsFaulted )
                    throw new InvalidOperationException( "Unable to copy stream", task.Exception );
            }

            _stream.Position = 0;
        }

        private Task<object> MoveToWritableStream()
        {
            var tcs = new TaskCompletionSource<object>();
            var sourceStream = _stream;
            _stream = new MemoryStream( BufferSize );

            CopyTo( sourceStream, this, ( source, destination, ex ) =>
            {
                if ( ex != null )
                    tcs.SetException( ex );
                else
                    tcs.SetResult( null );
            } );

            return tcs.Task;
        }

        /// <summary>Copies the contents between two <see cref="Stream"/> instances in an async fashion.</summary>
        /// <param name="source">The source stream to copy from.</param>
        /// <param name="destination">The destination stream to copy to.</param>
        /// <param name="onComplete">Delegate that should be invoked when the operation has completed. Will pass the source, destination and exception (if one was thrown) to the function. Can pass in <see langword="null" />.</param>
        public static void CopyTo( Stream source, Stream destination, Action<Stream, Stream, Exception> onComplete )
        {
            var buffer = new byte[BufferSize];

            Action<Exception> done = e =>
            {
                if ( onComplete != null )
                    onComplete.Invoke( source, destination, e );
            };

            AsyncCallback rc = null;

            rc = readResult =>
            {
                try
                {
                    var read = source.EndRead( readResult );

                    if ( read <= 0 )
                    {
                        done.Invoke( null );
                        return;
                    }

                    destination.BeginWrite( buffer, 0, read, writeResult =>
                    {
                        try
                        {
                            destination.EndWrite( writeResult );
                            source.BeginRead( buffer, 0, buffer.Length, rc, null );
                        }
                        catch ( Exception ex )
                        {
                            done.Invoke( ex );
                        }

                    }, null );
                }
                catch ( Exception ex )
                {
                    done.Invoke( ex );
                }
            };

            source.BeginRead( buffer, 0, buffer.Length, rc, null );
        }

        /// <summary>Gets a value indicating whether the current stream supports reading.</summary>
        /// <returns>Always returns <see langword="true"/>.</returns>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <summary>Gets a value indicating whether the current stream supports seeking.</summary>
        /// <returns>Always returns <see langword="true"/>.</returns>
        public override bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        /// <summary>Gets a value that determines whether the current stream can time out.</summary>
        /// <returns>Always returns <see langword="false"/>.</returns>
        public override bool CanTimeout
        {
            get { return false; }
        }

        /// <summary>Gets a value indicating whether the current stream supports writing.</summary>
        /// <returns>Always returns <see langword="true"/>.</returns>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>Gets the length in bytes of the stream.</summary>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        public override long Length
        {
            get { return _stream.Length; }
        }

        /// <summary>Gets a value indicating whether the current stream is stored in memory.</summary>
        /// <value><see langword="true"/> if the stream is stored in memory; otherwise, <see langword="false"/>.</value>
        /// <remarks>The stream is moved to disk when either the length of the contents or expected content length exceeds the threshold specified in the constructor.</remarks>
        public bool IsInMemory
        {
            get { return !( _stream.GetType() == typeof ( FileStream ) ); }
        }

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        /// <returns>The current position within the stream.</returns>
        public override long Position
        {
            get { return _stream.Position; }
            set
            {
                if ( value < 0 )
                    throw new InvalidOperationException( "The position of the stream cannot be set to less than zero." );

                if ( value > Length )
                    throw new InvalidOperationException( "The position of the stream cannot exceed the length of the stream." );

                _stream.Position = value;
            }
        }

        /// <summary>
        /// Begins an asynchronous read operation.
        /// </summary>
        /// <returns>An <see cref="T:System.IAsyncResult"/> that represents the asynchronous read, which could still be pending.</returns>
        /// <param name="buffer">The buffer to read the data into. </param>
        /// <param name="offset">The byte offset in <paramref name="buffer"/> at which to begin writing data read from the stream. </param>
        /// <param name="count">The maximum number of bytes to read. </param>
        /// <param name="callback">An optional asynchronous callback, to be called when the read is complete. </param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous read request from other requests. </param>
        public override IAsyncResult BeginRead( byte[] buffer, int offset, int count, AsyncCallback callback, object state )
        {
            return _stream.BeginRead( buffer, offset, count, callback, state );
        }

        /// <summary>
        /// Begins an asynchronous write operation.
        /// </summary>
        /// <returns>An <see cref="IAsyncResult"/> that represents the asynchronous write, which could still be pending.</returns>
        /// <param name="buffer">The buffer to write data from. </param>
        /// <param name="offset">The byte offset in <paramref name="buffer"/> from which to begin writing. </param>
        /// <param name="count">The maximum number of bytes to write. </param>
        /// <param name="callback">An optional asynchronous callback, to be called when the write is complete. </param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous write request from other requests.</param>
        public override IAsyncResult BeginWrite( byte[] buffer, int offset, int count, AsyncCallback callback, object state )
        {
            return _stream.BeginWrite( buffer, offset, count, callback, state );
        }

        /// <summary>Disposes of the stream and if the stream was written to disk deletes the temporary file on disk.</summary>
        /// <param name="disposing"></param>
        protected override void Dispose( bool disposing )
        {
            if ( _isSafeToDisposeStream )
            {
                ( ( IDisposable ) _stream ).Dispose();

                var fileStream = _stream as FileStream;
                if ( fileStream != null )
                    DeleteTemporaryFile( fileStream.Name );
            }

            base.Dispose( disposing );
        }

        /// <summary>Waits for the pending asynchronous read to complete.</summary>
        /// <returns>The number of bytes read from the stream, between zero (0) and the number of bytes you requested. Streams return zero (0) only at the end of the stream, otherwise, they should block until at least one byte is available.</returns>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish. </param>
        public override int EndRead( IAsyncResult asyncResult )
        {
            return _stream.EndRead( asyncResult );
        }

        /// <summary>Ends an asynchronous write operation.</summary>
        /// <param name="asyncResult">A reference to the outstanding asynchronous I/O request. </param>
        public override void EndWrite( IAsyncResult asyncResult )
        {
            _stream.EndWrite( asyncResult );
            ShiftStreamToFileStreamIfNecessary();
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            _stream.Flush();
        }

        private static long GetExpectedRequestLength(string contentLengthHeaderValue)
        {
            if (contentLengthHeaderValue == null)
                return 0;

            long contentLength;
            return !long.TryParse(contentLengthHeaderValue, NumberStyles.Any, CultureInfo.InvariantCulture, out contentLength) ? 0 : contentLength;
        }

        /// <summary>Initializes a new instance of the <see cref="RequestStream"/> class.</summary>
        /// <param name="stream">The <see cref="Stream"/> that should be handled by the request stream.</param>
        /// <param name="contentLength">Content-Length header value.</param>
        /// <returns><see cref="RequestStream"/> instance.</returns>
        public static RequestStream FromStream(Stream stream, string contentLength)
        {
            long expectedLength = GetExpectedRequestLength( contentLength );
            return FromStream(stream, expectedLength, RequestLengthDiskThreshold, false);
        }

        /// <summary>Initializes a new instance of the <see cref="RequestStream"/> class.</summary>
        /// <param name="stream">The <see cref="Stream"/> that should be handled by the request stream.</param>
        /// <param name="expectedLength">The expected length of the contents in the stream.</param>
        /// <param name="thresholdLength">The content length that will trigger the stream to be moved out of memory.</param>
        /// <param name="disableStreamSwitching">If set to <see langword="true"/> the stream will never explicitly be moved to disk.</param>
        /// <returns><see cref="RequestStream"/> instance.</returns>
        public static RequestStream FromStream( Stream stream, long expectedLength, long thresholdLength, bool disableStreamSwitching )
        {
            return new RequestStream( stream, expectedLength, thresholdLength, disableStreamSwitching );
        }

        /// <summary>Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source. </param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream. </param>
        /// <param name="count">The maximum number of bytes to be read from the current stream. </param>
        public override int Read( byte[] buffer, int offset, int count )
        {
            return _stream.Read( buffer, offset, count );
        }

        /// <summary>Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.</summary>
        /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
        public override int ReadByte()
        {
            return _stream.ReadByte();
        }

        /// <summary>Sets the position within the current stream.</summary>
        /// <returns>The new position within the current stream.</returns>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter. </param>
        /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"/> indicating the reference point used to obtain the new position. </param>
        public override long Seek( long offset, SeekOrigin origin )
        {
            return _stream.Seek( offset, origin );
        }

        /// <summary>Sets the length of the current stream.</summary>
        /// <param name="value">The desired length of the current stream in bytes. </param>
        /// <exception cref="NotSupportedException">The stream does not support having it's length set.</exception>
        /// <remarks>This functionality is not supported by the <see cref="RequestStream"/> type and will always throw <see cref="NotSupportedException"/>.</remarks>
        public override void SetLength( long value )
        {
            throw new NotSupportedException();
        }

        /// <summary>Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.</summary>
        /// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream. </param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream. </param>
        /// <param name="count">The number of bytes to be written to the current stream. </param>
        public override void Write( byte[] buffer, int offset, int count )
        {
            _stream.Write( buffer, offset, count );
            ShiftStreamToFileStreamIfNecessary();
        }

        private void ShiftStreamToFileStreamIfNecessary()
        {
            if ( _disableStreamSwitching )
                return;

            if ( _stream.Length >= _thresholdLength )
            {
                var old = _stream;
                MoveStreamContentsToFileStream();
                old.Close();
            }
        }

        private static FileStream CreateTemporaryFileStream()
        {
            var filePath = Path.GetTempFileName();
            return new FileStream( filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 8192, true );
        }

        private Stream CreateDefaultMemoryStream( long expectedLength )
        {
            _isSafeToDisposeStream = true;

            if ( _disableStreamSwitching || expectedLength < _thresholdLength )
                return new MemoryStream( ( int ) expectedLength );

            _disableStreamSwitching = true;
            return CreateTemporaryFileStream();
        }

        private static void DeleteTemporaryFile( string fileName )
        {
            if ( string.IsNullOrEmpty( fileName ) || !File.Exists( fileName ) )
                return;

            try
            {
                File.Delete( fileName );
            }
            catch
            {
            }
        }

        private void MoveStreamOutOfMemoryIfContentsLengthExceedThresholdAndSwitchingIsEnabled()
        {
            if ( !_stream.CanSeek )
                return;

            try
            {
                if ( ( _stream.Length > _thresholdLength ) && !_disableStreamSwitching )
                    MoveStreamContentsToFileStream();
            }
            catch ( NotSupportedException )
            {
            }
        }

        private bool MoveStreamOutOfMemoryIfExpectedLengthExceedSwitchLength( long expectedLength )
        {
            if ( ( expectedLength < _thresholdLength ) || _disableStreamSwitching )
                return false;

            MoveStreamContentsToFileStream();
            return true;
        }

        private void MoveStreamContentsToFileStream()
        {
            var targetStream = CreateTemporaryFileStream();
            _isSafeToDisposeStream = true;

            if ( _stream.CanSeek && _stream.Length == 0 )
            {
                _stream.Close();
                _stream = targetStream;
                return;
            }

            if ( _stream.CanSeek )
                _stream.Position = 0;

            _stream.CopyTo( targetStream, 8196 );

            if ( _stream.CanSeek )
                _stream.Flush();

            _stream = targetStream;
            _disableStreamSwitching = true;
        }

        private static void ThrowExceptionIfCtorParametersWereInvalid( Stream stream, long expectedLength, long thresholdLength )
        {
            if ( !stream.CanRead )
                throw new InvalidOperationException( "The stream must support reading." );

            if ( expectedLength < 0 )
                throw new ArgumentOutOfRangeException( "expectedLength", expectedLength, "The value of the expectedLength parameter cannot be less than zero." );

            if ( thresholdLength < 0 )
                throw new ArgumentOutOfRangeException( "thresholdLength", thresholdLength, "The value of the threshHoldLength parameter cannot be less than zero." );
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

    /// <summary>Enables / disables CorrelationId support which passes through or creates new CorrelationIds per request in order to support request tracking.</summary>
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

            nanoContext.CorrelationId = correlationId;
            nanoContext.Response.HeaderParameters.Add( Constants.CorrelationIdRequestParameterName, correlationId );
            System.Runtime.Remoting.Messaging.CallContext.LogicalSetData( Constants.CorrelationIdRequestParameterName, correlationId );
        }
    }

    /// <summary>Enables / disables Keep-Alive ( persistent connection ) support.</summary>
    public static class KeepAliveHelper
    {
        private const string KeepAliveIsDisabled = "KeepAliveIsDisabled";

        /// <summary>Enables Keep-Alive ( persistent connection ) support.</summary>
        /// <param name="nanoConfiguration">The nano configuration.</param>
        public static void EnableKeepAlive( this NanoConfiguration nanoConfiguration )
        {
            nanoConfiguration.GlobalEventHandler.EnableKeepAlive();
        }

        /// <summary>Disables Keep-Alive ( persistent connection ) support. Note that in a modern web hosting environment applications are typically hosted behind a load balancer and in order to get proper web traffic distribution "Keep Alive" should be disabled.</summary>
        /// <param name="nanoConfiguration">The nano configuration.</param>
        public static void DisableKeepAlive( this NanoConfiguration nanoConfiguration )
        {
            nanoConfiguration.GlobalEventHandler.DisableKeepAlive();
        }

        /// <summary>Enables Keep-Alive ( persistent connection ) support.</summary>
        /// <param name="eventHandler">The event handler.</param>
        public static void EnableKeepAlive( this EventHandler eventHandler )
        {
            eventHandler.PreInvokeHandlers.Remove( DisableKeepAlivePreInvokeHandler );
        }

        /// <summary>Disables Keep-Alive ( persistent connection ) support. Note that in a modern web hosting environment applications are typically hosted behind a load balancer and in order to get proper web traffic distribution "Keep Alive" should be disabled.</summary>
        /// <param name="eventHandler">The event handler.</param>
        public static void DisableKeepAlive( this EventHandler eventHandler )
        {
            if ( eventHandler.PreInvokeHandlers.Contains( DisableKeepAlivePreInvokeHandler ) == false )
                eventHandler.PreInvokeHandlers.Add( DisableKeepAlivePreInvokeHandler );
        }

        /// <summary>Enables Keep-Alive ( persistent connection ) support.</summary>
        /// <param name="nanoContext">The nano context.</param>
        public static void DisableKeepAlivePreInvokeHandler( NanoContext nanoContext )
        {
            nanoContext.Items.Add( KeepAliveIsDisabled, null );
        }

        /// <summary>Determines if the 'ElapsedMilliseconds" response header is enabled.</summary>
        /// <param name="nanoContext">The nano context.</param>
        /// <returns>True if enabled.</returns>
        public static bool IsKeepAliveDisabled( this NanoContext nanoContext )
        {
            return nanoContext.Items.ContainsKey( KeepAliveIsDisabled );
        }
    }

    /// <summary>Enables / disables adding an 'ElapsedMilliseconds" response header per request.</summary>
    public static class ElapsedMillisecondsHelper
    {
        private const string ElapsedMillisecondsResponseHeaderIsEnabled = "ElapsedMillisecondsResponseHeaderIsEnabled";

        /// <summary>Enables adding an 'ElapsedMilliseconds" response header per request.</summary>
        /// <param name="nanoConfiguration">The nano configuration.</param>
        public static void EnableElapsedMillisecondsResponseHeader( this NanoConfiguration nanoConfiguration )
        {
            nanoConfiguration.GlobalEventHandler.EnableElapsedMillisecondsResponseHeader();
        }

        /// <summary>Disables adding an 'ElapsedMilliseconds" response header per request.</summary>
        /// <param name="nanoConfiguration">The nano configuration.</param>
        public static void DisableElapsedMillisecondsResponseHeader( this NanoConfiguration nanoConfiguration )
        {
            nanoConfiguration.GlobalEventHandler.DisableElapsedMillisecondsResponseHeader();
        }

        /// <summary>Enables adding an 'ElapsedMilliseconds" response header per request.</summary>
        /// <param name="eventHandler">The event handler.</param>
        public static void EnableElapsedMillisecondsResponseHeader( this EventHandler eventHandler )
        {
            if ( eventHandler.PreInvokeHandlers.Contains( EnableElapsedMillisecondsResponseHeaderPostInvokeHandler ) == false )
                eventHandler.PreInvokeHandlers.Add( EnableElapsedMillisecondsResponseHeaderPostInvokeHandler );
        }

        /// <summary>Disables adding an 'ElapsedMilliseconds" response header per request.</summary>
        /// <param name="eventHandler">The event handler.</param>
        public static void DisableElapsedMillisecondsResponseHeader( this EventHandler eventHandler )
        {
            eventHandler.PreInvokeHandlers.Remove( EnableElapsedMillisecondsResponseHeaderPostInvokeHandler );
        }

        /// <summary>Enables adding an 'ElapsedMilliseconds" response header per request.</summary>
        /// <param name="nanoContext">The nano context.</param>
        public static void EnableElapsedMillisecondsResponseHeaderPostInvokeHandler( NanoContext nanoContext )
        {
            nanoContext.Items.Add( ElapsedMillisecondsResponseHeaderIsEnabled, null );
        }

        /// <summary>Determines if the 'ElapsedMilliseconds" response header is enabled.</summary>
        /// <param name="nanoContext">The nano context.</param>
        /// <returns>True if enabled.</returns>
        public static bool IsElapsedMillisecondsResponseHeaderEnabled( this NanoContext nanoContext )
        {
            return nanoContext.Items.ContainsKey( ElapsedMillisecondsResponseHeaderIsEnabled );
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

            /// <summary>401 Unauthorized</summary>
            Unauthorized = 401,

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

        /// <summary>Elapsed milliseconds response header name.</summary>
        public static string ElapsedMillisecondsResponseHeaderName = "X-Nano-ElapsedMilliseconds";

        static Constants()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            if ( assembly.FullName.Contains( "Nano.Web.Core" ) )
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo( assembly.Location );
                Version = fvi.FileVersion;
            }
            else
                Version = "0.14.0.0";
        }

        /// <summary>Custom error responses.</summary>
        public static class CustomErrorResponse
        {
            /// <summary>401 Unauthorized custom error response.</summary>
            public static string Unauthorized401 = "<html><head><title>Unauthorized</title></head><body><h3>Unauthorized: Error 401</h3><p>Oops, access is denied due to invalid credentials.</p></body></html>";
        
            /// <summary>404 Not Found custom error response.</summary>
            public static string NotFound404 = "<html><head><title>Page Not Found</title></head><body><h3>Page Not Found: Error 404</h3><p>Oops, the page you requested was not found.</p></body></html>";

            /// <summary>500 Internal Server Error custom error response.</summary>
            public static string InternalServerError500 = "<html><head><title>Internal Server Error</title></head><body><h3>Internal Server Error: Error 500</h3><p>Oops, an internal error occurred.</p><!--ErrorMessage--></body></html>";
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
            foreach ( ErrorHandler unhandledExceptionHandler in UnhandledExceptionHandlers )
                unhandledExceptionHandler.Invoke( exception, nanoContext );
        }

        /// <summary>Invokes the pre-invoke handlers.</summary>
        /// <param name="nanoContext">Current NanoContext.</param>
        public void InvokePreInvokeHandlers( NanoContext nanoContext )
        {
            foreach ( PreInvokeHandler preInvokeHandler in PreInvokeHandlers )
                preInvokeHandler.Invoke( nanoContext );
        }

        /// <summary>Invokes the post-invoke handlers.</summary>
        /// <param name="nanoContext">Current NanoContext.</param>
        public void InvokePostInvokeHandlers( NanoContext nanoContext )
        {
            foreach ( PostInvokeHandler postInvokeHandler in PostInvokeHandlers )
                postInvokeHandler.Invoke( nanoContext );
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
                unhandledExceptionHandler.Invoke( exception, backgroundTaskContext );
        }

        /// <summary>Invokes the pre-invoke handlers.</summary>
        /// <param name="backgroundTaskContext">Current <see cref="BackgroundTaskContext" />.</param>
        public void InvokePreInvokeHandlers( BackgroundTaskContext backgroundTaskContext )
        {
            foreach ( PreInvokeHandler preInvokeHandler in PreInvokeHandlers )
                preInvokeHandler.Invoke( backgroundTaskContext );
        }

        /// <summary>Invokes the post-invoke handlers.</summary>
        /// <param name="backgroundTaskContext">Current <see cref="BackgroundTaskContext" />.</param>
        public void InvokePostInvokeHandlers( BackgroundTaskContext backgroundTaskContext )
        {
            foreach ( PostInvokeHandler postInvokeHandler in PostInvokeHandlers )
                postInvokeHandler.Invoke( backgroundTaskContext );
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
                
                Task.Run( () =>
                {
                    while ( true )
                    {
                        var backgroundTaskContext = new BackgroundTaskContext { BackgroundTask = task, NanoConfiguration = nanoConfiguration };

                        try
                        {
                            if ( backgroundTaskContext.BackgroundTask.AllowOverlappingRuns )
                                Task.Run( () => Run( backgroundTaskContext ) ); // Run async
                            else
                                Run( backgroundTaskContext ); // Run sync
                        }
                        catch ( Exception ) { }

                        Thread.Sleep( backgroundTaskContext.BackgroundTask.MillisecondInterval );
                    } // ReSharper disable once FunctionNeverReturns
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

    /// <summary><see cref="NanoConfiguration" /> extensions.</summary>
    public static class NanoConfigurationExtensions
    {
        /// <summary>
        /// Writes an error message to the operating system event log.
        /// </summary>
        /// <param name="nanoConfiguration">The Nano configuration.</param>
        /// <param name="errorMessage">The error message to write.</param>
        /// <returns>Indicates whether the error message successfully wrote to the event log.</returns>
        public static bool WriteErrorToEventLog( this NanoConfiguration nanoConfiguration, string errorMessage )
        {
            return EventLogHelper.WriteErrorToEventLog( nanoConfiguration.ApplicationName, errorMessage );
        }
    }

    /// <summary><see cref="NanoContext" /> extensions.</summary>
    public static class NanoContextExtensions
    {
        /// <summary>Writes the response object to the host's response stream.</summary>
        /// <param name="nanoContext">The nano context.</param>
        public static void WriteResponseObjectToResponseStream( this NanoContext nanoContext )
        {
            // Return early if there is already a response stream writer
            if ( nanoContext.Response.ResponseStreamWriter != null )
                return;

            // Handle null responses and void methods by returning early
            if ( nanoContext.Response.ResponseObject == null )
                return;

            // Handle a Stream response object by copying the stream to the response stream
            var streamResponse = nanoContext.Response.ResponseObject as Stream;

            if ( streamResponse != null && streamResponse.Length >= 0 )
            {
                nanoContext.Response.ResponseStreamWriter = stream =>
                {
                    using ( streamResponse )
                        streamResponse.CopyTo( stream );
                };

                return;
            }

            // Serialize the response object
            string serializedResponse = nanoContext.NanoConfiguration.SerializationService.Serialize( nanoContext.Response.ResponseObject );
            byte[] bytes = Encoding.UTF8.GetBytes( serializedResponse );

            // Generate an ETag and return an 'HTTP 304 - NOT MODIFIED' if the request contains a matching ETag in the If-None-Match header
            using ( var md5 = System.Security.Cryptography.MD5.Create() )
            {
                byte[] hash = md5.ComputeHash( bytes );
                var eTag = "\"" + BitConverter.ToString( hash ).Replace( "-", string.Empty) + "\"";
                nanoContext.Response.HeaderParameters[ "ETag" ] = eTag;

                var requestETag = nanoContext.Request.HeaderParameters[ "If-None-Match" ];
                if ( string.IsNullOrWhiteSpace( requestETag ) == false && requestETag.Equals( eTag, StringComparison.Ordinal ) )
                {
                    nanoContext.Response.HttpStatusCode = Constants.HttpStatusCode.NotModified.ToInt();
                    return;
                }
            }

            // Write serialized object to response stream
            nanoContext.Response.ResponseStreamWriter = stream =>
            {
                stream.Write( bytes, 0, bytes.Length );
            };
        }

        /// <summary>Returns an 'HTTP 404 - NOT FOUND' to the client using the default 'NOT FOUND' HTML.</summary>
        /// <param name="nanoContext">The nano context.</param>
        public static NanoContext ReturnHttp404NotFound( this NanoContext nanoContext )
        {
            nanoContext.Handled = true;
            nanoContext.Response.HttpStatusCode = Constants.HttpStatusCode.NotFound.ToInt();
            nanoContext.Response.ContentType = "text/html";
            nanoContext.Response.ResponseStreamWriter = stream => stream.Write( Constants.CustomErrorResponse.NotFound404 );
            return nanoContext;
        }

        /// <summary>Returns an 'HTTP 500 - INTERNAL SERVER ERROR' to the client using the default 'INTERNAL SERVER ERROR' HTML.</summary>
        /// <param name="nanoContext">The nano context.</param>
        public static void ReturnHttp500InternalServerError( this NanoContext nanoContext )
        {
            nanoContext.Handled = true;
            nanoContext.Response.HttpStatusCode = Constants.HttpStatusCode.InternalServerError.ToInt();
            nanoContext.Response.ResponseStreamWriter = nanoContext.WriteErrorsToStream;
        }

        private static void GenerateHtmlErrorMessage( Exception exception, StringBuilder stringBuilder, int recursionLevel )
        {
            if ( recursionLevel > 25 )
                return; // Something has most likely went very wrong so return early to avoid a stack overflow

            if ( recursionLevel > 0 )
            {
                int marginLeft = recursionLevel * 4;
                stringBuilder.AppendFormat( "<p style=\"margin-left:{0}px;\"><b>Inner Exception Error Message:</b></p>", marginLeft ).AppendLine();
                stringBuilder.AppendFormat( "<p style=\"margin-left:{0}px;\">{1}</p>", marginLeft, exception.Message ).AppendLine();
                stringBuilder.AppendFormat( "<p style=\"margin-left:{0}px;\"><b>Inner Exception Stack Trace:</b></p>", marginLeft ).AppendLine();
                stringBuilder.AppendFormat( "<p style=\"margin-left:{0}px;\">{1}</p>", marginLeft, exception.StackTrace ).AppendLine();
            }
            else
            {
                stringBuilder.AppendLine( "<hr />" );
                stringBuilder.AppendLine( "<p><b>Error Message:</b></p>" );
                stringBuilder.AppendFormat( "<p>{0}</p>", exception.Message ).AppendLine();
                stringBuilder.AppendLine( "<p><b>Stack Trace:</b></p>" );
                stringBuilder.AppendFormat( "<p>{0}</p>", exception.StackTrace ).AppendLine();
            }

            if (exception.InnerException != null)
                GenerateHtmlErrorMessage(exception.InnerException, stringBuilder, ++recursionLevel);
        }

        /// <summary>Writes any errors to the response stream.</summary>
        /// <param name="nanoContext">The nano context.</param>
        /// <param name="stream">The stream.</param>
        public static void WriteErrorsToStream( this NanoContext nanoContext, Stream stream )
        {
            nanoContext.Response.ContentType = "text/html";

            if ( nanoContext.NanoConfiguration.LogErrorsToEventLog )
            {
                var textErrorMessageBuilder = new StringBuilder();
                textErrorMessageBuilder.AppendLine( "Nano Error:" );
                textErrorMessageBuilder.AppendLine( "*************" ).AppendLine();

                foreach ( Exception exception in nanoContext.Errors )
                {
                    nanoContext.WriteNanoContextDataToExceptionData( exception );
                    EventLogHelper.GenerateTextErrorMessage( exception, textErrorMessageBuilder, 0 );
                }

                nanoContext.NanoConfiguration.WriteErrorToEventLog( textErrorMessageBuilder.ToString() );
            }

            if ( Debugger.IsAttached )
            {
                var htmlErrorMessageBuilder = new StringBuilder();

                foreach ( Exception exception in nanoContext.Errors )
                    GenerateHtmlErrorMessage( exception, htmlErrorMessageBuilder, 0 );

                var errorMessage = Constants.CustomErrorResponse.InternalServerError500.Replace( "<!--ErrorMessage-->", htmlErrorMessageBuilder.ToString() );
                stream.Write( errorMessage );
                return;
            }

            stream.Write( Constants.CustomErrorResponse.InternalServerError500 );
        }

        /// <summary>Writes NanoContext data to the exceptions key/value pair Data property.</summary>
        /// <param name="nanoContext">The <see cref="NanoContext"/>.</param>
        /// <param name="exception">The <see cref="Exception"/> to write data to.</param>
        public static void WriteNanoContextDataToExceptionData( this NanoContext nanoContext, Exception exception )
        {
            exception.Data[ "Request URL" ] = nanoContext.Request.Url.ToString();
        }

        /// <summary>Returns a file if it exists.</summary>
        /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
        /// <param name="fileInfo">The file to return.</param>
        /// <returns>Returns true if the file exists else false.</returns>
        public static bool TryReturnFile( this NanoContext nanoContext, FileInfo fileInfo )
        {
            if ( fileInfo.Exists )
            {
                nanoContext.Handled = true;
                nanoContext.Response.ContentType = FileExtensionToContentTypeConverter.GetContentType( fileInfo.Extension );
                var eTag = "\"" + fileInfo.LastWriteTimeUtc.Ticks.ToString( "X" ) + "\""; // Quoted hexadecimal string
                nanoContext.Response.HeaderParameters[ "ETag" ] = eTag;
                nanoContext.Response.HeaderParameters[ "Last-Modified" ] = fileInfo.LastWriteTimeUtc.ToString( "R" ); // RFC-1123 - Example: Fri, 03 Jul 2015 02:44:49 GMT

                var requestETag = nanoContext.Request.HeaderParameters[ "If-None-Match" ];
                if ( string.IsNullOrWhiteSpace( requestETag ) == false && requestETag.Equals( eTag, StringComparison.Ordinal ) )
                {
                    nanoContext.Response.HttpStatusCode = Constants.HttpStatusCode.NotModified.ToInt();
                    return true;
                }

                var requestDateString = nanoContext.Request.HeaderParameters[ "If-Modified-Since" ];
                if ( string.IsNullOrWhiteSpace( requestDateString ) == false )
                {
                    DateTime requestDate;
                    if ( DateTime.TryParseExact( requestDateString, "R", CultureInfo.InvariantCulture, DateTimeStyles.None, out requestDate ) )
                    {
                        if ( ( ( int ) ( fileInfo.LastWriteTimeUtc - requestDate ).TotalSeconds ) <= 0 )
                        {
                            nanoContext.Response.HttpStatusCode = Constants.HttpStatusCode.NotModified.ToInt();
                            return true;
                        }
                    }
                }

                nanoContext.Response.HeaderParameters[ "Content-Length" ] = fileInfo.Length.ToString();

                if ( fileInfo.Length > 0 )
                {
                    nanoContext.Response.ResponseStreamWriter = stream =>
                    {
                        using ( FileStream file = fileInfo.OpenRead() )
                            file.CopyTo( stream, ( int ) ( fileInfo.Length < Constants.DefaultFileBufferSize ? fileInfo.Length : Constants.DefaultFileBufferSize ) );
                    };
                }

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

            if ( nanoContext.RequestHandler == null || nanoContext.Handled == false )
            {
                if ( nanoContext.NanoConfiguration.UnhandledRequestHandler != null )
                {
                    nanoContext.RequestHandler = nanoContext.NanoConfiguration.UnhandledRequestHandler;
                    nanoContext.NanoConfiguration.UnhandledRequestHandler.ProcessRequest( nanoContext );
                }
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
            var type = typeof( T );
            return (T) Bind( nanoContext, type, parameterName );
        }

        /// <summary>Binds a request parameter to the requested type.</summary>
        /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
        /// <param name="type">The type to bind to.</param>
        /// <param name="parameterName">Name of the request parameter.</param>
        /// <returns>The object of the requested type.</returns>
        public static object Bind( this NanoContext nanoContext, Type type, string parameterName )
        {
            string requestParameterValue = nanoContext.GetRequestParameterValue( parameterName );

            if ( JsonHelpers.IsJson( requestParameterValue ) == false )
            {
                // First try to convert the type using the TypeConverter which handles 'simple' types
                try
                {
                    return TypeConverter.ConvertType( requestParameterValue, type );
                }
                catch( Exception )
                {
                    // Swallow the exception here so that we drop down to the next try/catch block
                }
            }

            // As a last attempt use the heavy weight JSON converter to convert the type
            try
            {
                Type underlyingType = Nullable.GetUnderlyingType( type ) ?? type;
                object convertedValue;

                // Try to convert the request parameter value to the method parameter values type
                // Note we are currently leveraging Json.Net to handle the heavy load of type conversions
                if( nanoContext.NanoConfiguration.SerializationService.TryParseJson( requestParameterValue, underlyingType, false, out convertedValue ) )
                    return convertedValue;

                throw new Exception( "Type conversion error" );
            }
            catch( Exception )
            {
                string errorMessage = String.Format( "An error occurred converting the parameter named '{0}' and value '{1}' to type {2}.", parameterName, requestParameterValue, type );
                throw new Exception( errorMessage );
            }
        }

        /// <summary>Gets the request parameter value.</summary>
        /// <typeparam name="T">The type to convert the parameter to.</typeparam>
        /// <param name="nanoContext">The nano context.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <returns>The request parameter object converted to the requested type.</returns>
        public static object GetRequestParameterValue<T>( this NanoContext nanoContext, string parameterName )
        {
            return Bind<T>( nanoContext, parameterName );
        }

        /// <summary>Gets the request parameter value.</summary>
        /// <param name="nanoContext">The nano context.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="type">The type to convert the parameter to.</param>
        /// <returns>The request parameter object converted to the requested type.</returns>
        public static object GetRequestParameterValue( this NanoContext nanoContext, string parameterName, Type type )
        {
            return Bind( nanoContext, type, parameterName );
        }

        /// <summary>
        /// Gets a request parameter string value given a NanoContext and the parameter name. By default this will return the first
        /// value found in the following sources in this order: query string, form body, headers.
        /// </summary>
        /// <param name="nanoContext">The nano context.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <returns>Request parameter value.</returns>
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
        /// <returns>Request parameter value.</returns>
        public static string GetRequestParameterValue( this NanoRequest nanoRequest, string parameterName )
        {
            // Try to get the method parameter value from the request parameters
            string requestParameterValue = nanoRequest.QueryStringParameters.Get( parameterName ) ??
                                           nanoRequest.FormBodyParameters.Get( parameterName ) ??
                                           nanoRequest.HeaderParameters.Get( parameterName );

            return requestParameterValue;
        }
    }

    /// <summary>User Identity.</summary>
    public interface IUserIdentity
    {
        /// <summary>The username of the authenticated user.</summary>
        string UserName { get; }

        /// <summary>The claims of the authenticated user.</summary>
        IEnumerable<string> Claims { get; }
    }

    /// <summary>Represents a full Url of the form scheme://hostname:port/basepath/path?query</summary>
    public sealed class Url : ICloneable
    {
        private string _basePath;
        private string _query;

        /// <summary>Creates an instance of the <see cref="Url" /> class.</summary>
        public Url()
        {
            Scheme = Uri.UriSchemeHttp;
            HostName = string.Empty;
            Port = null;
            BasePath = string.Empty;
            Path = string.Empty;
            Query = string.Empty;
        }

        /// <summary>Creates an instance of the <see cref="Url" /> class.</summary>
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

        /// <summary>Gets or sets the HTTP protocol used by the client.</summary>
        /// <value>The protocol.</value>
        public string Scheme { get; set; }

        /// <summary>Gets the host name of the request.</summary>
        public string HostName { get; set; }

        /// <summary>Gets the port name of the request.</summary>
        public int? Port { get; set; }

        /// <summary>Gets the base path of the request i.e. the application root.</summary>
        public string BasePath
        {
            get { return _basePath; }
            set
            {
                if ( string.IsNullOrWhiteSpace( value ) )
                    return;

                _basePath = value.TrimEnd( '/' );
            }
        }

        /// <summary>Gets the path of the request, relative to the base path. This property drives the route matching.</summary>
        public string Path { get; set; }

        /// <summary>Gets the query string.</summary>
        public string Query
        {
            get { return _query; }
            set { _query = GetQuery( value ); }
        }

        /// <summary>Gets the domain part of the request.</summary>
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

        /// <summary>Gets whether the URL is secure or not.</summary>
        public bool IsSecure
        {
            get
            {
                return Uri.UriSchemeHttps.Equals( Scheme, StringComparison.OrdinalIgnoreCase );
            }
        }

        /// <summary>Returns a <see cref="System.String" /> that represents this instance as a URI.</summary>
        /// <returns>A <see cref="System.String" /> that represents this instance as a URI.</returns>
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

        /// <summary>Clones the url.</summary>
        /// <returns>Returns a new cloned instance of the url.</returns>
        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>Clones the url.</summary>
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

        /// <summary>Casts the current <see cref="Url"/> instance to a <see cref="string"/> instance.</summary>
        /// <param name="url">The instance that should be cast.</param>
        /// <returns>A <see cref="string"/> representation of the <paramref name="url"/>.</returns>
        public static implicit operator string( Url url )
        {
            return url.ToString();
        }

        /// <summary>Casts the current <see cref="string"/> instance to a <see cref="Url"/> instance.</summary>
        /// <param name="url">The instance that should be cast.</param>
        /// <returns>An <see cref="Url"/> representation of the <paramref name="url"/>.</returns>
        public static implicit operator Url( string url )
        {
            return new Uri( url );
        }

        /// <summary>Casts the current <see cref="Url"/> instance to a <see cref="Uri"/> instance.</summary>
        /// <param name="url">The instance that should be cast.</param>
        /// <returns>An <see cref="Uri"/> representation of the <paramref name="url"/>.</returns>
        public static implicit operator Uri( Url url )
        {
            return new Uri( url.ToString(), UriKind.Absolute );
        }

        /// <summary>Casts a <see cref="Uri"/> instance to a <see cref="Url"/> instance.</summary>
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
            return string.IsNullOrWhiteSpace( query ) ? string.Empty : ( query[0] == '?' ? query : '?' + query );
        }

        private static string GetCorrectPath( string path )
        {
            return ( string.IsNullOrWhiteSpace( path ) || path.Equals( "/" ) ) ? string.Empty : path;
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

    /// <summary><see cref="Stream" /> extensions.</summary>
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
                HandleRequest( httpApplication.Context, NanoConfiguration, DateTime.UtcNow );
            }

            /// <summary>Handles a System.Web request.</summary>
            /// <param name="httpContext">System.Web.HttpContext.</param>
            /// <param name="nanoConfiguration">The <see cref="NanoConfiguration" />.</param>
            /// <param name="requestTimestamp">The initial timestamp of the current HTTP request.</param>
            public static void HandleRequest( dynamic httpContext, NanoConfiguration nanoConfiguration, DateTime requestTimestamp )
            {
                NanoContext nanoContext = MapHttpContextBaseToNanoContext( httpContext, nanoConfiguration, requestTimestamp );
                nanoContext = RequestRouter.RouteRequest( nanoContext );

                if( nanoContext.RequestHandler == null || nanoContext.Handled == false )
                    return;

                if ( nanoContext.Response.ResponseStreamWriter != null )
                    nanoContext.Response.ResponseStreamWriter( httpContext.Response.OutputStream );

                httpContext.Response.Charset = nanoContext.Response.Charset;
                httpContext.Response.ContentEncoding = nanoContext.Response.ContentEncoding;
                httpContext.Response.ContentType = nanoContext.Response.ContentType;

                foreach( string headerName in nanoContext.Response.HeaderParameters )
                    httpContext.Response.Headers.Add( headerName, nanoContext.Response.HeaderParameters[headerName] );
                
                if ( nanoContext.IsElapsedMillisecondsResponseHeaderEnabled() )
                {
                    var elapsedTime = DateTime.UtcNow - nanoContext.RequestTimestamp;
                    httpContext.Response.Headers.Add( Constants.ElapsedMillisecondsResponseHeaderName, elapsedTime.TotalMilliseconds.ToString() );
                }

                foreach ( dynamic cookie in nanoContext.Response.Cookies )
                    httpContext.Response.Headers.Add( "Set-Cookie", cookie.ToString() );
                
                httpContext.Response.StatusCode = nanoContext.Response.HttpStatusCode;
                httpContext.Response.End();
            }

            /// <summary>Maps a System.Web.HttpContext to a NanoContext.</summary>
            /// <param name="httpContext">System.Web.HttpContext.</param>
            /// <param name="nanoConfiguration">The <see cref="NanoConfiguration" />.</param>
            /// <param name="requestTimestamp">The initial timestamp of the current HTTP request.</param>
            /// <returns>Mapped <see cref="NanoContext" />.</returns>
            public static NanoContext MapHttpContextBaseToNanoContext( dynamic httpContext, NanoConfiguration nanoConfiguration, DateTime requestTimestamp )
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
                    Query = httpContext.Request.Url.Query
                };

                RequestStream requestStream = RequestStream.FromStream( httpContext.Request.InputStream, httpContext.Request.Headers[ "Content-Length" ] );
                var results = RequestBodyParser.ParseRequestBody( httpContext.Request.Headers[ "Content-Type" ], httpContext.Request.ContentEncoding, requestStream, nanoConfiguration.RequestParameterLimit );
                var nanoRequest = new NanoRequest( httpMethod, url, requestStream, httpContext.Request.QueryString, httpContext.Request.Form, httpContext.Request.Headers, results.Files, httpContext.Request.UserHostAddress );
                var nanoContext = new NanoContext( nanoRequest, new NanoResponse(), nanoConfiguration, requestTimestamp ) { HostContext = httpContext, RootFolderPath = nanoConfiguration.ApplicationRootFolderPath };
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
                if ( string.IsNullOrWhiteSpace( nanoConfiguration.ApplicationRootFolderPath ) )
                    nanoConfiguration.ApplicationRootFolderPath = GetApplicationRootFolderPath();

                if ( nanoConfiguration.UnhandledRequestHandler == null )
                    nanoConfiguration.UnhandledRequestHandler = new FuncRequestHandler( "/UnhandledRequestHandler", nanoConfiguration.DefaultEventHandler, context => context.ReturnHttp404NotFound() );

                httpListenerConfiguration.HttpListener.Start();
                var server = new HttpListenerNanoServer( nanoConfiguration, httpListenerConfiguration );
                httpListenerConfiguration.HttpListener.BeginGetContext( server.BeginGetContextCallback, server );

                httpListenerConfiguration.UnhandledExceptionHandler = exception =>
                {
                    var errorMessage = new StringBuilder()
                        .AppendLine( "Nano Error:" )
                        .AppendLine( "*************" ).AppendLine();

                    EventLogHelper.GenerateTextErrorMessage(exception, errorMessage, 0);
                    nanoConfiguration.WriteErrorToEventLog( errorMessage.ToString() );
                };

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
                DateTime requestTimestamp = DateTime.UtcNow;

                try
                {
                    if ( HttpListenerConfiguration == null || HttpListenerConfiguration.HttpListener == null || HttpListenerConfiguration.HttpListener.IsListening == false )
                        return;
                    HttpListenerContext httpListenerContext = HttpListenerConfiguration.HttpListener.EndGetContext( asyncResult );
                    HttpListenerConfiguration.HttpListener.BeginGetContext( BeginGetContextCallback, this );
                    HandleRequest( httpListenerContext, this, requestTimestamp );
                }
                catch( Exception e )
                {
                    if ( HttpListenerConfiguration == null || HttpListenerConfiguration.UnhandledExceptionHandler == null )
                        return;
                    HttpListenerConfiguration.UnhandledExceptionHandler( e );
                }
            }

            /// <summary>Handles the request.</summary>
            /// <param name="httpListenerContext">The HTTP listener context.</param>
            /// <param name="server">The server.</param>
            /// <param name="requestTimestamp">The initial timestamp of the current HTTP request.</param>
            public static void HandleRequest( HttpListenerContext httpListenerContext, HttpListenerNanoServer server, DateTime requestTimestamp )
            {
                NanoContext nanoContext = null;

                try
                {
                    nanoContext = MapHttpListenerContextToNanoContext( httpListenerContext, server, requestTimestamp );

                    nanoContext = RequestRouter.RouteRequest( nanoContext );

                    if ( nanoContext.RequestHandler == null || nanoContext.Handled == false )
                        return;

                    httpListenerContext.Response.ContentEncoding = nanoContext.Response.ContentEncoding;
                    httpListenerContext.Response.ContentType = nanoContext.Response.ContentType;

                    if ( nanoContext.IsKeepAliveDisabled() )
                    {
                        httpListenerContext.Response.KeepAlive = false;
                    }

                    foreach ( string headerName in nanoContext.Response.HeaderParameters )
                    {
                        if ( !IgnoredHeaders.IsIgnored( headerName ) )
                            httpListenerContext.Response.Headers.Add( headerName, nanoContext.Response.HeaderParameters[ headerName ] );
                    }
                    
                    if ( nanoContext.IsElapsedMillisecondsResponseHeaderEnabled() )
                    {
                        var elapsedTime = DateTime.UtcNow - nanoContext.RequestTimestamp;
                        httpListenerContext.Response.Headers.Add( Constants.ElapsedMillisecondsResponseHeaderName, elapsedTime.TotalMilliseconds.ToString() );
                    }

                    foreach ( NanoCookie cookie in nanoContext.Response.Cookies )
                        httpListenerContext.Response.Headers.Add( "Set-Cookie", cookie.ToString() );

                    httpListenerContext.Response.StatusCode = nanoContext.Response.HttpStatusCode;

                    if ( nanoContext.Response.ResponseStreamWriter != null )
                    {
                        if ( IsGZipSupported( httpListenerContext ) )
                        {
                            httpListenerContext.Response.Headers.Add( "Content-Encoding", "gzip" );

                            using ( var gZipStream = new GZipStream(httpListenerContext.Response.OutputStream, CompressionMode.Compress, true ) )
                                nanoContext.Response.ResponseStreamWriter( gZipStream );
                        }
                        else
                            nanoContext.Response.ResponseStreamWriter( httpListenerContext.Response.OutputStream );
                    }
                }
                catch ( Exception exception )
                {
                    try
                    {
                        httpListenerContext.Response.OutputStream.Write( Constants.CustomErrorResponse.InternalServerError500 ); // Attempt to write an error message
                    }
                    catch ( Exception )
                    {
                         /* Gulp */
                    }

                    if ( exception.GetType() == typeof ( HttpListenerException ) && server.HttpListenerConfiguration.IgnoreHttpListenerExceptions )
                    {
                        return;
                    }

                    if ( nanoContext != null )
                    {
                        nanoContext.WriteNanoContextDataToExceptionData( exception );
                    }

                    throw;
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

                if ( !string.IsNullOrEmpty( encoding ) && encoding.Contains( "gzip" ) )
                    return true;

                return false;
            }

            /// <summary>Maps a <see cref="System.Net.HttpListenerContext" /> to <see cref="NanoContext" />.</summary>
            /// <param name="httpListenerContext">The HTTP listener context.</param>
            /// <param name="server">The server.</param>
            /// <param name="requestTimestamp">The initial timestamp of the current HTTP request.</param>
            /// <returns>Mapped <see cref="NanoContext" />.</returns>
            public static NanoContext MapHttpListenerContextToNanoContext( HttpListenerContext httpListenerContext, HttpListenerNanoServer server, DateTime requestTimestamp )
            {
                string httpMethod = httpListenerContext.Request.HttpMethod;
                string basePath = String.Empty;
				string path = "/" + httpListenerContext.Request.Url.AbsolutePath.TrimStart( '/' ).ToLower();

                if ( string.IsNullOrWhiteSpace( server.HttpListenerConfiguration.ApplicationPath ) == false )
                {
                    basePath = "/" + server.HttpListenerConfiguration.ApplicationPath.TrimStart( '/' ).TrimEnd( '/' ).ToLower();
					if ( path.StartsWith( basePath ) ) path = path.Substring( basePath.Length );
                }
                
                path = string.IsNullOrWhiteSpace( path ) ? "/" : path;

                var url = new Url
                {
                    Scheme = httpListenerContext.Request.Url.Scheme,
                    HostName = httpListenerContext.Request.Url.Host,
                    Port = httpListenerContext.Request.Url.Port,
                    BasePath = basePath,
                    Path = path,
                    Query = httpListenerContext.Request.Url.Query
                };
                
                RequestStream requestStream = RequestStream.FromStream( httpListenerContext.Request.InputStream, httpListenerContext.Request.Headers["Content-Length"] );
                var results = RequestBodyParser.ParseRequestBody( httpListenerContext.Request.Headers[ "Content-Type" ], httpListenerContext.Request.ContentEncoding, requestStream, server.NanoConfiguration.RequestParameterLimit );
                var nanoRequest = new NanoRequest( httpMethod, url, requestStream, httpListenerContext.Request.QueryString, results.FormBodyParameters, httpListenerContext.Request.Headers, results.Files, httpListenerContext.Request.RemoteEndPoint == null ? null : httpListenerContext.Request.RemoteEndPoint.Address.ToString() );
                var nanoContext = new NanoContext( nanoRequest, new NanoResponse(), server.NanoConfiguration, requestTimestamp ) { HostContext = httpListenerContext, RootFolderPath = server.NanoConfiguration.ApplicationRootFolderPath };
                return nanoContext;
            }

            /// <summary>Gets the root path of the host application.</summary>
            /// <returns>The root path of the host application.</returns>
            public static string GetApplicationRootFolderPath()
            {
                return AppDomain.CurrentDomain.BaseDirectory;
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
        public class HttpListenerConfiguration : IDisposable
        {
            /// <summary>The HTTP listener.</summary>
            public System.Net.HttpListener HttpListener;
            
            /// <summary>
            /// Invoked on unhandled exceptions that occur during the HttpListenerContext to NanoContext mapping. Note: These will
            /// *not* be called for normal Nano exceptions which are handled by the Nano event handlers.
            /// </summary>
            public Action<Exception> UnhandledExceptionHandler;

            /// <summary>Ignore all <see cref="HttpListenerException"/>s that occur. Defaults to <see langword="true"/>.</summary>
            /// <remarks>The most common scenario is when the underlying client connection closes but the server is still trying to send a response. In this case there is nothing that can be done except ignore the exception which is the intent of this configuration setting.</remarks>
            public bool IgnoreHttpListenerExceptions = true;

            /// <summary>
            /// Gets or sets a property that determines if localhost uris are rewritten to htp://+:port/ style uris to allow for
            /// listening on all ports, but requiring either a url reservation, or admin access Defaults to false.
            /// </summary>
            public bool RewriteLocalhost;

            /// <summary>The application's virtual application root path on the server. Set this if the application runs under a virtual directory.</summary>
            /// <remarks>The virtual path of the current application.</remarks>
            public string ApplicationPath = string.Empty;

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
                HttpListener.IgnoreWriteExceptions = true;
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

                    if ( RewriteLocalhost && !uri.Host.Contains( "." ) )
                        prefix = prefix.Replace( "localhost", "+" );

                    HttpListener.Prefixes.Add( prefix );
                }
            }

            /// <summary>
            /// Gets the first URL being listened on and adds the <see cref="ApplicationPath"/> if one has been supplied.
            /// </summary>
            /// <returns>First URL being listened on.</returns>
            public string GetFirstUrlBeingListenedOn()
            {
                var firstUrlBeingListenedOn = HttpListener.Prefixes.FirstOrDefault();

                if ( string.IsNullOrWhiteSpace( firstUrlBeingListenedOn ) )
                    return null;

                var uriBuilder = new UriBuilder( firstUrlBeingListenedOn );

                if ( string.IsNullOrWhiteSpace( ApplicationPath ) == false )
                    uriBuilder.Path = ApplicationPath + "/";

                return uriBuilder.Uri.ToString();
            }

            /// <summary>
            /// Dispose resources
            /// </summary>
            public void Dispose()
            {
                Dispose( true );
                GC.SuppressFinalize( this );
            }

            /// <summary>
            /// Dispose the HttpListener
            /// </summary>
            /// <param name="disposing"></param>
            protected virtual void Dispose( bool disposing )
            {
                try
                {
                    if ( true )
                    {

                        if ( HttpListener == null ) return;

                        HttpListener.Close();

                        HttpListener = null;
                    }
                }
                catch ( ObjectDisposedException )
                {
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
                    if ( e is TargetInvocationException && e.InnerException != null )
                        e = e.InnerException; // Hide Nano's outer 'TargetInvocationException' and just use the inner exception which is what almost everyone is going to desire

                    nanoContext.Errors.Add( e );
                    nanoContext.ReturnHttp500InternalServerError();

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

            /// <summary>Returns a <see cref="System.String" /> that represents the request handler type and path.</summary>
            /// <returns>A <see cref="System.String" /> that represents the request handler type and path.</returns>
			public override string ToString ()
			{
				return string.Format( "[RequestHandler: UrlPath={0}, Type={1}]", UrlPath, GetType().Name );
			}
        }

		/// <summary>
		/// Defines a request handler that returns files from a file system.
		/// </summary>
		public abstract class FileSystemRequestHandler : RequestHandler
		{
			private static readonly ConcurrentDictionary<string, string> Paths = new ConcurrentDictionary<string, string>();
            
			static FileSystemRequestHandler() 
			{
				string path = Path.GetTempFileName();

				IsCaseSensitiveFileSystem = !File.Exists( path.ToUpper() );
			}

            /// <summary>
            /// Initializes a new instance of the <see cref="FileSystemRequestHandler" /> class.
            /// </summary>
            /// <param name="fileSystemPath">The file system path.</param>
            /// <param name="urlPath">The URL path.</param>
            /// <param name="handler">The event handlers.</param>
		    protected FileSystemRequestHandler( string fileSystemPath, string urlPath, EventHandler handler ) : base( urlPath, handler )
			{
				FileSystemPath = fileSystemPath.Replace( "/", Constants.DirectorySeparatorString ).TrimStart( '~', Constants.DirectorySeparatorChar ).TrimEnd( '/', Constants.DirectorySeparatorChar );
			}

            /// <summary>
            /// Flag indicating whether or not the file system is case sensitive.
            /// </summary>
			public static bool IsCaseSensitiveFileSystem { get; set; }

            /// <summary>
            /// The file system path.
            /// </summary>
			public string FileSystemPath { get; set; }

            /// <summary>
            /// Returns the corrected path after a case-sensitive search has been performed.
            /// </summary>
            /// <param name="path">The path to search for.</param>
            /// <returns>The valid case-sensitive path or null.</returns>
			protected string GetPathCaseSensitive( string path )
			{
				if ( Paths.ContainsKey( path ) ) return Paths[path];

				string[] segments = path.Split( new[] { Constants.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries );

				var current = new DirectoryInfo( FileSystemPath ).Root;

				for ( int i = 0; i < segments.Length; i++ ) 
				{	
					string result = GetPathSegmentCaseSensitive( segments[i], current );

					if ( i == segments.Length - 1 )
					{
						if ( result != null ) Paths[path] = result;

						return result;
					} 

					if ( result == null ) return null;

					current = new DirectoryInfo( result );
				}

				return null;
			}

			private string GetPathSegmentCaseSensitive( string name, DirectoryInfo directory )
			{
				if ( directory == null ) return null;

				FileSystemInfo match = directory.GetFileSystemInfos().FirstOrDefault( f => f.Name.Equals( name, StringComparison.CurrentCultureIgnoreCase ) );

				return match != null ? match.FullName : null;
			}
		}

        /// <summary>Handles requests to defined file system directories and files within those directories.</summary>
		public class DirectoryRequestHandler : FileSystemRequestHandler
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
				: base( fileSystemPath, urlPath, eventHandler )
            {
                if( string.IsNullOrWhiteSpace( fileSystemPath ) )
                    throw new ArgumentNullException( "fileSystemPath" );

                if( defaultDocuments == null )
                    defaultDocuments = new[] { "index.html" };

                if( FileSystemPath == Constants.DirectorySeparatorString )
                    throw new RootDirectoryException();

                ReturnHttp404WhenFileWasNoFound = returnHttp404WhenFileWasNoFound;
                DefaultDocuments = defaultDocuments;
            }

            /// <summary>
            /// Gets or sets a value indicating whether it should return an 'HTTP 404 - File Not Found' when no file was found..
            /// </summary>
            /// <value><c>true</c> if [return HTTP404 when file was no found]; otherwise, <c>false</c>.</value>
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

				if ( IsCaseSensitiveFileSystem ) fullFileSystemPath = GetPathCaseSensitive( fullFileSystemPath );

				if ( !String.IsNullOrWhiteSpace( fullFileSystemPath ) )
				{					
					if ( nanoContext.TryReturnFile( new FileInfo( fullFileSystemPath ) ) ) return nanoContext;
                
					var directoryInfo = new DirectoryInfo( fullFileSystemPath );			

					if ( directoryInfo.Exists )
					{
						// If the URL does not end with a forward slash then redirect to the same URL with a forward slash
						// so that relative URLs will work correctly
						if ( nanoContext.Request.Url.Path.EndsWith( "/", StringComparison.Ordinal ) == false )
						{
							string url = nanoContext.Request.Url.BasePath + nanoContext.Request.Url.Path + "/" + nanoContext.Request.Url.Query;
							nanoContext.Response.Redirect( url );
							return nanoContext;
						}

						foreach ( string defaultDocument in DefaultDocuments )
						{
							string path = Path.Combine( fullFileSystemPath, defaultDocument );

							if ( IsCaseSensitiveFileSystem )
								path = GetPathCaseSensitive( path );

							if ( nanoContext.TryReturnFile( new FileInfo( path ) ) )
								return nanoContext;
						}
					}
				}

                if( ReturnHttp404WhenFileWasNoFound ) return nanoContext.ReturnHttp404NotFound();

                return nanoContext;
            }

            /// <summary>
            /// Returns a new string in which the first occurrence of a specified string in the current instance is replaced with
            /// another specified string.
            /// </summary>
            /// <param name="originalString">The original string.</param>
            /// <param name="stringToReplace">The string to be replaced.</param>
            /// <param name="replacementString">The replacement string.</param>
            /// <returns>Replaced string.</returns>
            public static string ReplaceFirstOccurrence( string originalString, string stringToReplace, string replacementString )
            {
                int pos = originalString.IndexOf( stringToReplace, StringComparison.Ordinal );
                if( pos < 0 )
                    return originalString;
                return originalString.Substring( 0, pos ) + replacementString + originalString.Substring( pos + stringToReplace.Length );
            }

            /// <summary>Root Directory exception.</summary>
            [Serializable]
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
		public class FileRequestHandler : FileSystemRequestHandler
        {
            /// <summary>Initializes a new instance of the <see cref="FileRequestHandler" /> class.</summary>
            /// <param name="urlPath">The URL path.</param>
            /// <param name="eventHandler">The event handler.</param>
            /// <param name="fileSystemPath">The file system path.</param>
            /// <exception cref="System.ArgumentNullException">fileSystemPath</exception>
            public FileRequestHandler( string urlPath, EventHandler eventHandler, string fileSystemPath )
                : base( fileSystemPath, urlPath, eventHandler )
            {
                if( string.IsNullOrWhiteSpace( fileSystemPath ) )
                    throw new ArgumentNullException( "fileSystemPath" );
            }

            /// <summary>Handles the request.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <returns>Handled <see cref="NanoContext" />.</returns>
            public override NanoContext HandleRequest( NanoContext nanoContext )
            {
                string fullFilePath = Path.Combine( nanoContext.RootFolderPath, FileSystemPath );

				if ( IsCaseSensitiveFileSystem ) fullFilePath = GetPathCaseSensitive( fullFilePath );

                if( nanoContext.TryReturnFile( new FileInfo( fullFilePath ) ) )  return nanoContext;

                return nanoContext.ReturnHttp404NotFound();
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
            
            /// <summary>Handles the request.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <returns>Handled <see cref="NanoContext" />.</returns>
            public override NanoContext HandleRequest( NanoContext nanoContext )
            {
                nanoContext.Handled = true;
                nanoContext.Response.ResponseObject = Func( nanoContext );

                // Handle null responses and void methods
                if( nanoContext.Response.ResponseObject == null || nanoContext.Response.ResponseObject == nanoContext )
                    return nanoContext;

                if ( string.IsNullOrWhiteSpace( nanoContext.Response.ContentType ) )
                    nanoContext.Response.ContentType =  "application/json";

                nanoContext.WriteResponseObjectToResponseStream();
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

                    var metadata = new OperationMetaData { UrlPath = methodRequestHandler.CaseSensitiveUrlPath };
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
                    metadata.ReturnParameterType = GetTypeName( returnParameterType );
                    AddModels( apiMetadata, returnParameterType );
                    apiMetadata.Operations.Add( metadata );
                }

                nanoContext.Response.ResponseObject = apiMetadata;
                nanoContext.Response.ContentType = "application/json";
                nanoContext.WriteResponseObjectToResponseStream();
                return nanoContext;
            }

            /// <summary>Recursive method that crawls each of the types fields and properties creating Models for each user type.</summary>
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
                if ( type != null && IsUserType( type ) && apiMetadata.Models.Any( x => x.Type == type.Name ) == false )
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

            /// <summary>The case sensitive URL path for this handler.</summary>
            /// <value>The case sensitive URL path.</value>
            public string CaseSensitiveUrlPath { get; set; }

            /// <summary>Handles the request.</summary>
            /// <param name="nanoContext">The <see cref="NanoContext" />.</param>
            /// <returns>Handled <see cref="NanoContext" />.</returns>
            public override NanoContext HandleRequest( NanoContext nanoContext )
            {
                nanoContext.Handled = true;
                object[] parameters = Bind( nanoContext, this );
                nanoContext.Response.ResponseObject = Method.Invoke( null, parameters );

                // Handle null responses and void methods
                if( nanoContext.Response.ResponseObject == null || nanoContext.Response.ResponseObject == nanoContext )
                    return nanoContext;

                if ( string.IsNullOrWhiteSpace( nanoContext.Response.ContentType ) )
                    nanoContext.Response.ContentType = "application/json";

                nanoContext.WriteResponseObjectToResponseStream();
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
            /// <exception cref="System.Exception">Expected a MethodRoute but was a  + nanoContext.RequestHandler.GetType().Name or or Type conversion error or</exception>
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
                            catch ( Exception )
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
                        if( Nullable.GetUnderlyingType( methodParameter.Type ) != null || ( !methodParameter.Type.IsValueType ) )
                        {
                            methodInvokationParameters.Add( null );
                            continue;
                        }

                        string errorMessage = String.Format( "The query string, form body, and header parameters do not contain a parameter named '{0}' which is a required parameter for method '{1}'", methodParameter.Name, handler.Method.Name );
                        throw new Exception( errorMessage );
                    }

                    object methodInvokationParameterValue;

                    if ( JsonHelpers.IsJson( requestParameterValue ) == false && methodParameter.IsDynamic == false )
                    {
                        // First try to convert the type using the TypeConverter which handles 'simple' types
                        try
                        {
                            methodInvokationParameterValue = TypeConverter.ConvertType( requestParameterValue, methodParameter.Type );
                            methodInvokationParameters.Add( methodInvokationParameterValue );
                            continue;
                        }
                        catch ( Exception )
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
            /// <returns><see cref="MethodRequestHandler" /></returns>
            /// <exception cref="System.Exception">nanoContext.RequestHandler is NULL or Expected a MethodRequestHandler but was a  + requestHandler.GetType()</exception>
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
                        parameterList += ",";

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
                        else if( value is byte[] )
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

        /// <summary>Provides helper methods for writing to the operating system event log.</summary>
        public static class EventLogHelper
        {
            /// <summary>Writes an error message to the operating system event log.</summary>
            /// <param name="applicationName">The application name.</param>
            /// <param name="errorMessage">The error message to write.</param>
            /// <returns>Indicates whether the error message successfully wrote to the event log.</returns>
            public static bool WriteErrorToEventLog( string applicationName, string errorMessage )
            {
                bool successfullyWroteToEventLog = true;

                try
                {
                    if ( !EventLog.SourceExists( applicationName ) )
                        EventLog.CreateEventSource( applicationName, "Application" );

                    if (errorMessage.Length > 31788)
                        errorMessage = errorMessage.Substring(0, 31788) + " ...Log Message Truncated. Maximum length is 31839.";

                    EventLog.WriteEntry( applicationName, errorMessage, EventLogEntryType.Error );
                }
                catch ( Exception )
                {
                    successfullyWroteToEventLog = false;
                }

                if ( Environment.UserInteractive )
                    Console.WriteLine( errorMessage );

                return successfullyWroteToEventLog;
            }

            /// <summary>Generates a text-based error message.</summary>
            /// <param name="exception">The exception to generate the error message from.</param>
            /// <param name="stringBuilder">StringBuilder instance to append the error message to.</param>
            /// <param name="recursionLevel">The number of iterations this method has been called recursively.</param>
            public static void GenerateTextErrorMessage( Exception exception, StringBuilder stringBuilder, int recursionLevel )
            {
                if ( recursionLevel > 25 ) return; // Something has most likely went very wrong so return early to avoid a stack overflow

                string prefix = "";

                if ( recursionLevel > 0 )
                {
                    for ( int i = 0; i < recursionLevel; i++ )
                        prefix += "  ";

                    prefix += "Inner ";
                }

                stringBuilder.AppendFormat( "{0}Exception Information:", prefix ).AppendLine();
                stringBuilder.AppendFormat( "  {0}Exception Type: {1}", prefix, exception.GetType() ).AppendLine();
                stringBuilder.AppendFormat( "  {0}Exception Message: {1}", prefix, exception.Message ).AppendLine();
                stringBuilder.AppendLine( exception.StackTrace ).AppendLine();

                if ( exception.Data.Keys.Count > 0 )
                {
                    stringBuilder.AppendFormat( "{0}Additional Information:", prefix ).AppendLine();

                    foreach ( DictionaryEntry dictionaryEntry in exception.Data )
                    {
                        stringBuilder.AppendFormat("  {0}{1}: {2}", prefix, dictionaryEntry.Key, dictionaryEntry.Value ).AppendLine();
                    }

                    stringBuilder.AppendLine();
                }

                if (exception.InnerException != null)
                    GenerateTextErrorMessage(exception.InnerException, stringBuilder, ++recursionLevel);
            }
        }

        #region Multipart

        /* Note that the entire Multipart implementation was borrowed from the Nancy project. */
        internal class Multipart
        {
            /// <summary>Retrieves <see cref="HttpMultipartBoundary"/> instances from a request stream.</summary>
            internal class HttpMultipart
            {
                private const byte Lf = (byte)'\n';
                private readonly HttpMultipartBuffer _readBuffer;
                private readonly Stream _requestStream;

                /// <summary>Initializes a new instance of the <see cref="HttpMultipart"/> class.</summary>
                /// <param name="requestStream">The request stream to parse.</param>
                /// <param name="boundary">The boundary marker to look for.</param>
                public HttpMultipart(Stream requestStream, string boundary)
                {
                    _requestStream = requestStream;
                    var boundaryAsBytes = GetBoundaryAsBytes(boundary, false);
                    var closingBoundaryAsBytes = GetBoundaryAsBytes(boundary, true);
                    _readBuffer = new HttpMultipartBuffer(boundaryAsBytes, closingBoundaryAsBytes);
                }

                /// <summary>Gets the <see cref="HttpMultipartBoundary"/> instances from the request stream.</summary>
                /// <returns>An <see cref="IEnumerable{T}"/> instance, containing the found <see cref="HttpMultipartBoundary"/> instances.</returns>
                public IEnumerable<HttpMultipartBoundary> GetBoundaries(int requestQueryFormMultipartLimit)
                {
                    var list = new List<HttpMultipartBoundary>();

                    foreach (var boundaryStream in GetBoundarySubStreams(requestQueryFormMultipartLimit))
                        list.Add(new HttpMultipartBoundary(boundaryStream));

                    return list;
                }

                private IEnumerable<HttpMultipartSubStream> GetBoundarySubStreams(int requestQueryFormMultipartLimit)
                {
                    var boundarySubStreams = new List<HttpMultipartSubStream>();
                    var boundaryStart = GetNextBoundaryPosition();

                    var found = 0;
                    while (MultipartIsNotCompleted(boundaryStart) && found < requestQueryFormMultipartLimit)
                    {
                        var boundaryEnd = GetNextBoundaryPosition();
                        boundarySubStreams.Add(new HttpMultipartSubStream(_requestStream, boundaryStart, GetActualEndOfBoundary(boundaryEnd)));
                        boundaryStart = boundaryEnd;
                        found++;
                    }

                    return boundarySubStreams;
                }

                private bool MultipartIsNotCompleted(long boundaryPosition)
                {
                    return boundaryPosition > -1 && !_readBuffer.IsClosingBoundary;
                }

                private long GetActualEndOfBoundary(long boundaryEnd)
                {
                    if (CheckIfFoundEndOfStream())
                        return _requestStream.Position - (_readBuffer.Length + 2); // Add two because or the \r\n before the boundary

                    return boundaryEnd - (_readBuffer.Length + 2); // Add two because or the \r\n before the boundary
                }

                private bool CheckIfFoundEndOfStream()
                {
                    return _requestStream.Position.Equals(_requestStream.Length);
                }

                private static byte[] GetBoundaryAsBytes(string boundary, bool closing)
                {
                    var boundaryBuilder = new StringBuilder();

                    boundaryBuilder.Append("--");
                    boundaryBuilder.Append(boundary);

                    if (closing)
                        boundaryBuilder.Append("--");
                    else
                    {
                        boundaryBuilder.Append('\r');
                        boundaryBuilder.Append('\n');
                    }

                    var bytes = Encoding.ASCII.GetBytes(boundaryBuilder.ToString());
                    return bytes;
                }

                private long GetNextBoundaryPosition()
                {
                    _readBuffer.Reset();
                    while (true)
                    {
                        var byteReadFromStream = _requestStream.ReadByte();

                        if (byteReadFromStream == -1)
                            return -1;

                        _readBuffer.Insert((byte)byteReadFromStream);

                        if (_readBuffer.IsFull && (_readBuffer.IsBoundary || _readBuffer.IsClosingBoundary))
                            return _requestStream.Position;

                        if (byteReadFromStream.Equals(Lf) || _readBuffer.IsFull)
                            _readBuffer.Reset();
                    }
                }
            }

            /// <summary>A buffer that is used to locate a HTTP multipart/form-data boundary in a stream.</summary>
            internal class HttpMultipartBuffer
            {
                private readonly byte[] _boundaryAsBytes;
                private readonly byte[] _closingBoundaryAsBytes;
                private readonly byte[] _buffer;
                private int _position;

                /// <summary>Initializes a new instance of the <see cref="HttpMultipartBuffer"/> class.</summary>
                /// <param name="boundaryAsBytes">The boundary as a byte-array.</param>
                /// <param name="closingBoundaryAsBytes">The closing boundary as byte-array</param>
                public HttpMultipartBuffer(byte[] boundaryAsBytes, byte[] closingBoundaryAsBytes)
                {
                    _boundaryAsBytes = boundaryAsBytes;
                    _closingBoundaryAsBytes = closingBoundaryAsBytes;
                    _buffer = new byte[_boundaryAsBytes.Length];
                }

                /// <summary>Gets a value indicating whether the buffer contains the same values as the boundary.</summary>
                /// <value><see langword="true"/> if buffer contains the same values as the boundary; otherwise, <see langword="false"/>.</value>
                public bool IsBoundary
                {
                    get { return _buffer.SequenceEqual(_boundaryAsBytes); }
                }

                /// <summary>Indicates whether this is the closing boundary.</summary>
                public bool IsClosingBoundary
                {
                    get { return _buffer.SequenceEqual(_closingBoundaryAsBytes); }
                }

                /// <summary>Gets a value indicating whether this buffer is full.</summary>
                /// <value><see langword="true"/> if buffer is full; otherwise, <see langword="false"/>.</value>
                public bool IsFull
                {
                    get { return _position.Equals(_buffer.Length); }
                }

                /// <summary>Gets the number of bytes that can be stored in the buffer.</summary>
                /// <value>The number of bytes that can be stored in the buffer.</value>
                public int Length
                {
                    get { return _buffer.Length; }
                }

                /// <summary>Resets the buffer so that inserts happens from the start again.</summary>
                /// <remarks>This does not clear any previously written data, just resets the buffer position to the start. Data that is inserted after Reset has been called will overwrite old data.</remarks>
                public void Reset()
                {
                    _position = 0;
                }

                /// <summary>Inserts the specified value into the buffer and advances the internal position.</summary>
                /// <param name="value">The value to insert into the buffer.</param>
                /// <remarks>This will throw an <see cref="ArgumentOutOfRangeException"/> is you attempt to call insert more times then the <see cref="Length"/> of the buffer and <see cref="Reset"/> was not invoked.</remarks>
                public void Insert(byte value)
                {
                    _buffer[_position++] = value;
                }
            }

            /// <summary>Represents the content boundary of a HTTP multipart/form-data boundary in a stream.</summary>
            internal class HttpMultipartBoundary
            {
                private const byte Lf = (byte)'\n';
                private const byte Cr = (byte)'\r';

                /// <summary>Initializes a new instance of the <see cref="HttpMultipartBoundary"/> class.</summary>
                /// <param name="boundaryStream">The stream that contains the boundary information.</param>
                public HttpMultipartBoundary(HttpMultipartSubStream boundaryStream)
                {
                    Value = boundaryStream;
                    ExtractHeaders();
                }

                /// <summary>Gets the contents type of the boundary value.</summary>
                /// <value>A <see cref="string"/> containing the name of the value if it is available; otherwise <see cref="string.Empty"/>.</value>
                public string ContentType { get; private set; }

                /// <summary>Gets or the filename for the boundary value.</summary>
                /// <value>A <see cref="string"/> containing the filename value if it is available; otherwise <see cref="string.Empty"/>.</value>
                /// <remarks>This is the RFC2047 decoded value of the filename attribute of the Content-Disposition header.</remarks>
                public string Filename { get; private set; }

                /// <summary>Gets name of the boundary value.</summary>
                /// <remarks>This is the RFC2047 decoded value of the name attribute of the Content-Disposition header.</remarks>
                public string Name { get; private set; }

                /// <summary>A stream containing the value of the boundary.</summary>
                /// <remarks>This is the RFC2047 decoded value of the Content-Type header.</remarks>
                public HttpMultipartSubStream Value { get; private set; }

                private void ExtractHeaders()
                {
                    while (true)
                    {
                        var header = ReadLineFromStream(Value);

                        if (string.IsNullOrEmpty(header))
                            break;

                        if (header.StartsWith("Content-Disposition", StringComparison.CurrentCultureIgnoreCase))
                        {
                            Name = Regex.Match(header, @"name=""?(?<name>[^\""]*)", RegexOptions.IgnoreCase).Groups["name"].Value;
                            Filename = Regex.Match(header, @"filename=""?(?<filename>[^\""]*)", RegexOptions.IgnoreCase).Groups["filename"].Value;
                        }

                        if (header.StartsWith("Content-Type", StringComparison.InvariantCultureIgnoreCase))
                            ContentType = header.Split(' ').Last().Trim();
                    }

                    Value.PositionStartAtCurrentLocation();
                }

                private static string ReadLineFromStream(Stream stream)
                {
                    var readBuffer = new List<byte>();

                    while (true)
                    {
                        var byteReadFromStream = stream.ReadByte();

                        if (byteReadFromStream == -1)
                            return null;

                        if (byteReadFromStream.Equals(Lf))
                            break;

                        readBuffer.Add((byte)byteReadFromStream);
                    }

                    return Encoding.UTF8.GetString(readBuffer.ToArray()).Trim((char)Cr);
                }
            }

            /// <summary>A decorator stream that sits on top of an existing stream and appears as a unique stream.</summary>
            internal class HttpMultipartSubStream : Stream
            {
                private readonly Stream _stream;
                private long _start;
                private readonly long _end;
                private long _position;

                /// <summary>Initializes a new instance of the <see cref="HttpMultipartSubStream"/> class.</summary>
                /// <param name="stream">The stream to create the sub-stream ontop of.</param>
                /// <param name="start">The start offset on the parent stream where the sub-stream should begin.</param>
                /// <param name="end">The end offset on the parent stream where the sub-stream should end.</param>
                public HttpMultipartSubStream(Stream stream, long start, long end)
                {
                    _stream = stream;
                    _start = start;
                    _position = start;
                    _end = end;
                }

                /// <summary>When overridden in a derived class, gets a value indicating whether the current stream supports reading.</summary>
                /// <returns><see langword="true"/> if the stream supports reading; otherwise, <see langword="false"/>.</returns>
                public override bool CanRead
                {
                    get { return true; }
                }

                /// <summary>When overridden in a derived class, gets a value indicating whether the current stream supports seeking.</summary>
                /// <returns><see langword="true"/> if the stream supports seeking; otherwise, <see langword="false"/>.</returns>
                public override bool CanSeek
                {
                    get { return true; }
                }

                /// <summary>When overridden in a derived class, gets a value indicating whether the current stream supports writing.</summary>
                /// <returns><see langword="true"/> if the stream supports writing; otherwise, <see langword="false"/>.</returns>
                public override bool CanWrite
                {
                    get { return false; }
                }

                /// <summary>When overridden in a derived class, gets the length in bytes of the stream.</summary>
                /// <returns>A long value representing the length of the stream in bytes.</returns>
                /// <exception cref="NotSupportedException">A class derived from Stream does not support seeking. </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
                public override long Length
                {
                    get { return (_end - _start); }
                }

                /// <summary>When overridden in a derived class, gets or sets the position within the current stream.</summary>
                /// <returns>The current position within the stream.</returns>
                /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><exception cref="T:System.NotSupportedException">The stream does not support seeking. </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
                public override long Position
                {
                    get { return _position - _start; }
                    set { _position = Seek(value, SeekOrigin.Begin); }
                }

                private long CalculateSubStreamRelativePosition(SeekOrigin origin, long offset)
                {
                    var subStreamRelativePosition = 0L;

                    switch (origin)
                    {
                        case SeekOrigin.Begin:
                            subStreamRelativePosition = _start + offset;
                            break;
                        case SeekOrigin.Current:
                            subStreamRelativePosition = _position + offset;
                            break;
                        case SeekOrigin.End:
                            subStreamRelativePosition = _end + offset;
                            break;
                    }
                    return subStreamRelativePosition;
                }

                /// <summary>Sets the start position at the current location.</summary>
                public void PositionStartAtCurrentLocation()
                {
                    _start = _stream.Position;
                }

                /// <summary>When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.</summary>
                /// <remarks>In the <see cref="HttpMultipartSubStream"/> type this method is implemented as no-op.</remarks>
                public override void Flush()
                {
                }

                /// <summary>When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
                /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached. </returns>
                /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source. </param>
                /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
                /// <param name="count">The maximum number of bytes to be read from the current stream. </param>
                public override int Read(byte[] buffer, int offset, int count)
                {
                    if (count > (_end - _position))
                        count = (int)(_end - _position);

                    if (count <= 0)
                        return 0;

                    _stream.Position = _position;
                    var bytesReadFromStream = _stream.Read(buffer, offset, count);
                    RepositionAfterRead(bytesReadFromStream);
                    return bytesReadFromStream;
                }

                /// <summary>Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.</summary>
                /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
                public override int ReadByte()
                {
                    if (_position >= _end)
                        return -1;

                    _stream.Position = _position;
                    var byteReadFromStream = _stream.ReadByte();
                    RepositionAfterRead(1);
                    return byteReadFromStream;
                }

                private void RepositionAfterRead(int bytesReadFromStream)
                {
                    if (bytesReadFromStream == -1)
                        _position = _end;
                    else
                        _position += bytesReadFromStream;
                }

                /// <summary>When overridden in a derived class, sets the position within the current stream.</summary>
                /// <returns>The new position within the current stream.</returns>
                /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
                /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
                public override long Seek(long offset, SeekOrigin origin)
                {
                    var subStreamRelativePosition =
                        CalculateSubStreamRelativePosition(origin, offset);

                    if (subStreamRelativePosition < 0 || subStreamRelativePosition > _end)
                        throw new InvalidOperationException();

                    _position = _stream.Seek(subStreamRelativePosition, SeekOrigin.Begin);
                    return _position;
                }

                /// <summary>When overridden in a derived class, sets the length of the current stream.</summary>
                /// <param name="value">The desired length of the current stream in bytes.</param>
                /// <remarks>This will always throw a <see cref="InvalidOperationException"/> for the <see cref="HttpMultipartSubStream"/> type.</remarks>
                public override void SetLength(long value)
                {
                    throw new InvalidOperationException();
                }

                /// <summary>When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.</summary>
                /// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream. </param>
                /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream. </param>
                /// <param name="count">The number of bytes to be written to the current stream. </param>
                /// <remarks>This will always throw a <see cref="InvalidOperationException"/> for the <see cref="HttpMultipartSubStream"/> type.</remarks>
                public override void Write(byte[] buffer, int offset, int count)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        #endregion Multipart

        internal static class RequestBodyParser
        {
            internal class RequestBodyParserResults
            {
                internal readonly IList<HttpFile> Files = new List<HttpFile>();
                internal readonly NameValueCollection FormBodyParameters = new NameValueCollection();
            }

            internal static RequestBodyParserResults ParseRequestBody( string contentType, Encoding contentEncoding, Stream requestBody, int parameterLimit )
            {
                var results = new RequestBodyParserResults();

                if ( string.IsNullOrWhiteSpace( contentType ) )
                    return results;

                string mimeType = contentType.Split( ';' ).FirstOrDefault();

                if ( string.IsNullOrWhiteSpace( mimeType ) )
                    return results;

                if ( mimeType.Equals( "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase ) )
                {
                    var sr = new StreamReader( requestBody, contentEncoding );
                    string formData = sr.ReadToEnd();

                    if ( string.IsNullOrWhiteSpace( formData ) )
                        return results;

                    string[] parameters = formData.Split( '&' );

                    if ( parameters.Length > parameterLimit )
                        throw new Exception( "The limit of " + parameterLimit + " request parameters sent was exceeded. The reason to limit the number of parameters processed by the server is detailed in the following security notice regarding a DoS attack vector: http://www.ocert.org/advisories/ocert-2011-003.html" );

                    foreach ( string parameter in parameters )
                    {
                        if ( parameter.Contains( '=' ) == false )
                            throw new Exception( "Can not parse the malformed form-urlencoded request parameters. Current parameter being parsed: " + parameter );

                        string[] keyValuePair = parameter.Split( '=' );
                        string decodedKey = UrlDecode( keyValuePair[ 0 ] );
                        string decodedValue = UrlDecode( keyValuePair[ 1 ] );
                        results.FormBodyParameters.Add( decodedKey, decodedValue );
                    }

                    return results;
                }

                if ( !mimeType.Equals( "multipart/form-data", StringComparison.OrdinalIgnoreCase ) )
                    return results;

                var boundary = Regex.Match( contentType, @"boundary=""?(?<token>[^\n\;\"" ]*)" ).Groups[ "token" ].Value;
                var multipart = new Multipart.HttpMultipart( requestBody, boundary );

                foreach ( var httpMultipartBoundary in multipart.GetBoundaries( parameterLimit ) )
                {
                    if ( string.IsNullOrEmpty( httpMultipartBoundary.Filename ) )
                    {
                        var reader = new StreamReader( httpMultipartBoundary.Value );
                        results.FormBodyParameters.Add( httpMultipartBoundary.Name, reader.ReadToEnd() );
                    }
                    else
                        results.Files.Add( new HttpFile( httpMultipartBoundary.ContentType, httpMultipartBoundary.Filename, httpMultipartBoundary.Value, httpMultipartBoundary.Name ) );
                }

                requestBody.Position = 0;

                return results;
            }

            /// <summary>Decodes a URL string.</summary>
            /// <param name="text">String to decode.</param>
            /// <returns>Decoded string</returns>
            public static string UrlDecode(string text)
            {
                return Uri.UnescapeDataString(text.Replace("+", " "));
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
