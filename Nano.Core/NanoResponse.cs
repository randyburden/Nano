using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nano.Core
{
    /// <summary>
    /// Nano response representing an outgoing HTTP response.
    /// </summary>
    public class NanoResponse
    {
        /// <summary>
        /// Constructs a new NanoRequest.
        /// </summary>
        /// <param name="nanoContext">The requests context.</param>
        public NanoResponse( NanoContext nanoContext )
        {
            NanoContext = nanoContext;
        }

        /// <summary>
        /// The requests context.
        /// </summary>
        public NanoContext NanoContext;

        /// <summary>
        /// Response object returned from the invoked method.
        /// </summary>
        public object ResponseObject
        {
            get { return NanoContext.Get<object>( NanoConstants.ResponseObject ); }
            set { NanoContext.Set( NanoConstants.ResponseObject, value ); }
        }

        /// <summary>
        /// Nano error.
        /// </summary>
        public NanoError Error
        {
            get { return NanoContext.Get<NanoError>( NanoConstants.ResponseError ); }
            set { NanoContext.Set( NanoConstants.ResponseError, value ); }
        }

        /// <summary>
        /// Gets or sets the Content-Type header.
        /// </summary>
        /// <returns>The Content-Type header.</returns>
        public string ContentType
        {
            get { return GetHeader( NanoConstants.ContentType ); }
            set { SetHeader( NanoConstants.ContentType, value ); }
        }

        /// <summary>
        /// Gets the response headers.
        /// </summary>
        public IDictionary<string, string[]> Headers
        {
            get { return NanoContext.Get<IDictionary<string, string[]>>( OwinConstants.ResponseHeaders ); }
        }

        /// <summary>
        /// Gets or sets the owin.ResponseBody Stream.
        /// </summary>
        /// <returns>The owin.ResponseBody Stream.</returns>
        public virtual Stream Body
        {
            get { return NanoContext.Get<Stream>( OwinConstants.ResponseBody ); }
            set { NanoContext.Set( OwinConstants.ResponseBody, value ); }
        }

        /// <summary>
        /// Gets or sets the optional owin.ResponseStatusCode.
        /// </summary>
        /// <returns>The optional owin.ResponseStatusCode, or 200 if not set.</returns>
        public virtual int StatusCode
        {
            get { return NanoContext.Get<int>( OwinConstants.ResponseStatusCode, 200 ); }
            set { NanoContext.Set( OwinConstants.ResponseStatusCode, value ); }
        }

        /// <summary>
        /// Sets a response header.
        /// </summary>
        /// <param name="key">Header key.</param>
        /// <param name="value">Header value.</param>
        public void SetHeader( string key, string value )
        {
            if ( string.IsNullOrWhiteSpace( key ) )
            {
                throw new ArgumentNullException( "key" );
            }

            if ( string.IsNullOrWhiteSpace( value ) )
            {
                Headers.Remove( key );
            }

            else
            {
                Headers[ key ] = new[] { value };
            }
        }

        /// <summary>
        /// Gets a header value by key.
        /// </summary>
        /// <param name="key">Header key.</param>
        /// <returns></returns>
        public string GetHeader( string key )
        {
            string[] values = Headers.TryGetValue( key, out values ) ? values : null;

            return values == null ? null : string.Join( ",", values );
        }

        /// <summary>
        /// Writes the given text to the response body stream using UTF-8.
        /// </summary>
        /// <param name="text">The response data.</param>
        public virtual void Write( string text )
        {
            Write( Encoding.UTF8.GetBytes( text ) );
        }

        /// <summary>
        /// Writes the given bytes to the response body stream.
        /// </summary>
        /// <param name="data">The response data.</param>
        public virtual void Write( byte[] data )
        {
            Write( data, 0, data == null ? 0 : data.Length );
        }

        /// <summary>
        /// Writes the given bytes to the response body stream.
        /// </summary>
        /// <param name="data">The response data.</param>
        /// <param name="offset">The zero-based byte offset in the <paramref name="data" /> parameter at which to begin copying bytes.</param>
        /// <param name="count">The number of bytes to write.</param>
        public virtual void Write( byte[] data, int offset, int count )
        {
            Body.Write( data, offset, count );
        }

        /// <summary>
        /// Asynchronously writes the given text to the response body stream using UTF-8.
        /// </summary>
        /// <param name="text">The response data.</param>
        /// <returns>A Task tracking the state of the write operation.</returns>
        public virtual Task WriteAsync( string text )
        {
            return WriteAsync( text, CancellationToken.None );
        }

        /// <summary>
        /// Asynchronously writes the given text to the response body stream using UTF-8.
        /// </summary>
        /// <param name="text">The response data.</param>
        /// <param name="token">A token used to indicate cancellation.</param>
        /// <returns>A Task tracking the state of the write operation.</returns>
        public virtual Task WriteAsync( string text, CancellationToken token )
        {
            return WriteAsync( Encoding.UTF8.GetBytes( text ), token );
        }

        /// <summary>
        /// Asynchronously writes the given bytes to the response body stream.
        /// </summary>
        /// <param name="data">The response data.</param>
        /// <returns>A Task tracking the state of the write operation.</returns>
        public virtual Task WriteAsync( byte[] data )
        {
            return WriteAsync( data, CancellationToken.None );
        }

        /// <summary>
        /// Asynchronously writes the given bytes to the response body stream.
        /// </summary>
        /// <param name="data">The response data.</param>
        /// <param name="token">A token used to indicate cancellation.</param>
        /// <returns>A Task tracking the state of the write operation.</returns>
        public virtual Task WriteAsync( byte[] data, CancellationToken token )
        {
            return WriteAsync( data, 0, data == null ? 0 : data.Length, token );
        }

        /// <summary>
        /// Asynchronously writes the given bytes to the response body stream.
        /// </summary>
        /// <param name="data">The response data.</param>
        /// <param name="offset">The zero-based byte offset in the <paramref name="data" /> parameter at which to begin copying bytes.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <param name="token">A token used to indicate cancellation.</param>
        /// <returns>A Task tracking the state of the write operation.</returns>
        public virtual Task WriteAsync( byte[] data, int offset, int count, CancellationToken token )
        {
            return Body.WriteAsync( data, offset, count, token );
        }
    }
}