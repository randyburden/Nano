// A majority of the class was borrowed from the ExpandoObjectConverter found in the Json.Net library
#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace Nano.Core
{
    /// <summary>
    /// Converts a DynamicDictionary to and from JSON.
    /// </summary>
    public class DynamicDictionaryConverter : JsonConverter
    {
        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
        {
            // can write is set to false
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
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
                        object v = ReadValue( reader );

                        list.Add( v );
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

                        object v = ReadValue( reader );

                        expandoObject[propertyName] = v;
                        break;
                    case JsonToken.Comment:
                        break;
                    case JsonToken.EndObject:
                        return expandoObject;
                }
            }

            throw CreateJsonSerializationException( reader, "Unexpected end when reading ExpandoObject." );
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert( Type objectType )
        {
            return ( objectType == typeof( DynamicDictionary ) );
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="JsonConverter"/> can write JSON.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this <see cref="JsonConverter"/> can write JSON; otherwise, <c>false</c>.
        /// </value>
        public override bool CanWrite
        {
            get { return false; }
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