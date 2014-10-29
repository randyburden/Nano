using System;
using System.Collections.Generic;

namespace Nano.Core
{
    /// <summary>
    /// Web API
    /// </summary>
    public class WebApi
    {
        /// <summary>
        /// Instantiates a new WebApi which routes requests to the given web path to the given class.
        /// </summary>
        /// <param name="apiType">Class type to expose as a web api endpoint.</param>
        /// <param name="webPath">Web path / URL fragment that will route to this endpoint.</param>
        public WebApi( Type apiType, string webPath )
        {
            ApiType = apiType;

            WebPath = webPath;
        }

        /// <summary>
        /// Class type to expose as a web api endpoint.
        /// </summary>
        public Type ApiType;

        /// <summary>
        /// Web path / URL fragment that will route to this endpoint.
        /// </summary>
        public string WebPath;
        
        /// <summary>
        /// Event handler when an unhandled exception occurs.
        /// </summary>
        /// <param name="exception">Unhandled exception.</param>
        /// <param name="nanoContext">Current NanoContext.</param>
        public delegate void ApiErrorHandler( Exception exception, NanoContext nanoContext );

        /// <summary>
        /// Event handler before the target method is invoked.
        /// </summary>
        /// <param name="nanoContext">Current NanoContext.</param>
        public delegate void ApiPreInvokeHandler( NanoContext nanoContext );

        /// <summary>
        /// Event handler after the target method has been invoked and returned a response.
        /// </summary>
        /// <param name="nanoContext">Current NanoContext.</param>
        public delegate void ApiPostInvokeHandler( NanoContext nanoContext );

        /// <summary>
        /// Event triggered when an unhandled exception occurs.
        /// </summary>
        public List<ApiErrorHandler> ApiUnhandledExceptionEvent = new List<ApiErrorHandler>();

        /// <summary>
        /// Event triggered before the target method is invoked.
        /// </summary>
        public List<ApiPreInvokeHandler> ApiPreInvokeEvent = new List<ApiPreInvokeHandler>();

        /// <summary>
        /// Event triggered after the target method has been invoked and returned a response.
        /// </summary>
        public List<ApiPostInvokeHandler> ApiPostInvokeEvent = new List<ApiPostInvokeHandler>();

        /// <summary>
        /// Invokes the unhandled exception event handlers.
        /// </summary>
        /// <param name="exception">Unhandled exception.</param>
        /// <param name="nanoContext">Current NanoContext.</param>
        public void InvokeUnhandledExceptionEventHandlers( Exception exception, NanoContext nanoContext )
        {
            foreach( var apiUnhandledExceptionHandler in ApiUnhandledExceptionEvent )
            {
                apiUnhandledExceptionHandler.Invoke( exception, nanoContext );
            }
        }

        /// <summary>
        /// Invokes the pre-invoke event handlers.
        /// </summary>
        /// <param name="nanoContext">Current NanoContext.</param>
        public void InvokePreInvokeEventHandlers( NanoContext nanoContext )
        {
            foreach( var apiPreInvokeHandler in ApiPreInvokeEvent )
            {
                apiPreInvokeHandler.Invoke( nanoContext );
            }
        }

        /// <summary>
        /// Invokes the post-invoke event handlers.
        /// </summary>
        /// <param name="nanoContext">Current NanoContext.</param>
        public void InvokePostInvokeEventHandlers( NanoContext nanoContext )
        {
            foreach( var apiPostInvokeHandler in ApiPostInvokeEvent )
            {
                apiPostInvokeHandler.Invoke( nanoContext );
            }
        }
    }
}