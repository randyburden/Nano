using System;
using Newtonsoft.Json;

namespace Nano.Core
{
    /// <summary>
    /// Provides various helpers for working with JSON.
    /// </summary>
    public static class JsonHelpers
    {
        /// <summary>
        /// Tries to safely parse the input string as JSON.
        /// </summary>
        /// <remarks>
        /// If the input string is not JSON this method will serialize the input to JSON in order
        /// to deserialize the input into the requested type.
        /// </remarks>
        /// <param name="input">Input to deserialize.</param>
        /// <param name="type">Type to convert the input to.</param>
        /// <param name="isDynamic">Indicates whether the type is dynamic.</param>
        /// <param name="deserializedObject">Deserialized object.</param>
        /// <returns>Indicates whether the input was successfully parsed.</returns>
        public static bool TryParseJson( string input, Type type, bool isDynamic, out object deserializedObject )
        {
            input = input.Trim();

            var isJson = IsJson( input );

            if ( isJson )
            {
                try
                {
                    if ( isDynamic )
                    {
                        // Converting to a Nano.DynamicDictionary allows case insenstive member access when using C# dynamic.. sweet!
                        deserializedObject = JsonConvert.DeserializeObject<DynamicDictionary>( input, new DynamicDictionaryConverter() );

                        return true;
                    }

                    deserializedObject = JsonConvert.DeserializeObject( input, type );

                    return true;
                }
                catch ( Exception )
                {
                    // Swallow the exception here so that we drop down to the next try/catch block
                }
            }

            try
            {
                var serializedValue = JsonConvert.SerializeObject( input );

                deserializedObject = JsonConvert.DeserializeObject( serializedValue, type );

                return true;
            }
            catch ( Exception )
            {
                deserializedObject = null;

                return false;
            }
        }

        /// <summary>
        /// Determines if the given string conforms to standard JSON syntax.
        /// </summary>
        /// <param name="input">String to evaluate.</param>
        /// <returns>Returns true if the string looks like JSON.</returns>
        public static bool IsJson( string input )
        {
            input = input.Trim();

            return input.StartsWith( "{" ) && input.EndsWith( "}" ) || input.StartsWith( "[" ) && input.EndsWith( "]" );
        }

        /// <summary>
        /// Serializes the given type to JSON.
        /// </summary>
        /// <param name="obj">Object to serialize.</param>
        /// <param name="settings">Optional serialization settings.</param>
        /// <returns>JSON serialized representation of the given object.</returns>
        public static string ToJson( this object obj, JsonSerializerSettings settings = null )
        {
            if ( settings == null )
                settings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };

            return JsonConvert.SerializeObject( obj, settings );
        }
    }
}