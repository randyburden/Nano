using System;
using System.Collections.Generic;

namespace Nano.Core
{
    /// <summary>
    /// NanoContext representing the context in which an HTTP request is processed.
    /// </summary>
    /// <remarks>
    /// This wraps OWIN environment dictionary and provides strongly typed accessors.
    /// </remarks>
    public class NanoContext
    {
        /// <summary>
        /// Constructs a new NanoContext.
        /// </summary>
        /// <param name="environment">The OWIN environment.</param>
        /// <param name="webApi">Current WebApi being invoked.</param>
        public NanoContext( IDictionary<string, object> environment, WebApi webApi )
        {
            if ( environment == null )
                throw new ArgumentNullException( "environment", "The OWIN environment can not be null." );

            if( webApi == null )
                throw new ArgumentNullException( "webApi", "The webApi can not be null." );

            Environment = environment;

            WebApi = webApi;

            Request = new NanoRequest( this );

            Response = new NanoResponse( this );
        }

        /// <summary>
        /// Gets the OWIN environment.
        /// </summary>  
        /// <returns>
        /// The OWIN environment.
        /// </returns>
        public IDictionary<string, object> Environment;
        
        /// <summary>
        /// WebApi being invoked.
        /// </summary>
        public WebApi WebApi
        {
            get { return Get<WebApi>( NanoConstants.WebApi ); }
            set { Set( NanoConstants.WebApi, value ); }
        }

        /// <summary>
        /// Nano request representing an incoming HTTP request.
        /// </summary>
        public NanoRequest Request;

        /// <summary>
        /// Nano response representing an outgoing HTTP response.
        /// </summary>
        public NanoResponse Response;

        /// <summary>
        /// Gets a value from the OWIN environment, or returns default(T) if not present.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value with the specified key or the default(T) if not present.</returns>
        public T Get<T>( string key )
        {
            object value;

            return Environment.TryGetValue( key, out value ) ? ( T ) value : default( T );
        }

        /// <summary>
        /// Gets a value from the OWIN environment or returns the fallback value.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="fallback">The fallback value to return if the key does not exist.</param>
        /// <returns>The value with the specified key or the fallback value.</returns>
        public T Get<T>( string key, T fallback )
        {
            object value;

            return Environment.TryGetValue( key, out value ) ? ( T ) value : fallback;
        }

        /// <summary>
        /// Sets the given key and value in the OWIN environment.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to set.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>This instance.</returns>
        public void Set<T>( string key, T value )
        {
            Environment[key] = value;
        }
    }
}