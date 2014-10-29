using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Nano.Core
{
    /// <summary>
    /// NanoContext extensions.
    /// </summary>
    public static class NanoContextExtensions
    {
        /// <summary>
        /// Writes the given object as JSON to the response body.
        /// </summary>
        /// <param name="context">Owin context.</param>
        /// <param name="obj">Object to serialize to JSON.</param>
        /// <param name="formatting">JSON formatting.</param>
        public static void ReturnAsJson( this NanoContext context, object obj, Formatting formatting = Formatting.None )
        {
            context.Response.ContentType = NanoConstants.JsonContentType;

            var json = JsonConvert.SerializeObject( obj, formatting );

            context.Response.Write( json );
        }

        /// <summary>
        /// Asynchronously writes the given object as JSON to the response body.
        /// </summary>
        /// <param name="context">Owin context.</param>
        /// <param name="obj">Object to serialize to JSON.</param>
        /// <param name="formatting">JSON formatting.</param>
        /// <returns>Task to write the JSON to the response body.</returns>
        public static Task ReturnAsJsonAsync( this NanoContext context, object obj, Formatting formatting = Formatting.None )
        {
            context.Response.ContentType = NanoConstants.JsonContentType;

            var json = JsonConvert.SerializeObject( obj, formatting );

            return context.Response.WriteAsync( json );
        }

        /// <summary>
        /// Serializes the NanoContext.Environment to JSON.
        /// </summary>
        /// <param name="nanoContext">NanoContext.</param>
        /// <param name="formatting">Formatting option.</param>
        /// <returns>JSON representation of the NanoContext.Environment.</returns>
        public static string EnvironmentToJson( this NanoContext nanoContext, Formatting formatting = Formatting.Indented )
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = formatting,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                
                // Ignore any errors
                Error = ( serializer, err ) =>
                {
                    err.ErrorContext.Handled = true;
                }
            };

            // Convert the key value pairs of the Environment to properties on the JSON string
            var dynamicDictionary = new DynamicDictionary();
            
            // Note that iterating through the keys prevents throwing exceptions upon
            // accessing the dictionary via an iterator over the key value pairs
            foreach( var key in nanoContext.Environment.Keys )
            {
                try
                {
                    dynamicDictionary.Add( key, nanoContext.Environment[ key ] );
                }
                catch ( Exception )
                {
                    // Gulp: We obviously don't want to add entries that throw exceptions upon accessing
                }
                
            }

            return JsonConvert.SerializeObject( dynamicDictionary, settings );
        }
    }
}