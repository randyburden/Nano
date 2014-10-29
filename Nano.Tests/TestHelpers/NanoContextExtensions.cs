using System;
using System.Collections.Generic;
using System.Dynamic;
using Nano.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nano.Tests.TestHelpers
{
    /// <summary>
    /// NanoContext extensions.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Serializes the NanoContext.Environment 'nano' properties to JSON.
        /// </summary>
        /// <param name="nanoContext">NanoContext.</param>
        /// <param name="formatting">Formatting option.</param>
        /// <returns>JSON representation of the NanoContext.Environment.</returns>
        public static string NanoEnvironmentPropertiesToJson( this NanoContext nanoContext, Formatting formatting = Formatting.Indented )
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
                    if( key.ToLower().StartsWith( "nano" ) )
                        dynamicDictionary.Add( key, nanoContext.Environment[key] );
                }
                catch( Exception )
                {
                    // Gulp: We obviously don't want to add entries that throw exceptions upon accessing
                }

            }

            return JsonConvert.SerializeObject( dynamicDictionary, settings );
        }
    }
}