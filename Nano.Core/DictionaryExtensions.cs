using System.Collections.Generic;
using System.Dynamic;

namespace Nano.Core
{
    /// <summary>
    /// Dictionary extensions.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Merges the contents of one Dictionary{T,T}(string,string) to another Dictionary{T,T}(string,string).
        /// </summary>
        /// <param name="dictionary">This dictionary.</param>
        /// <param name="dictionaryToAdd">Dictionary to merge.</param>
        /// <returns>Merged dictionary.</returns>
        public static IDictionary<string, string> Merge( this IDictionary<string, string> dictionary, IDictionary<string, string> dictionaryToAdd )
        {
            if( dictionary == null )
            {
                dictionary = new Dictionary<string, string>();
            }

            foreach( var keyValuePair in dictionaryToAdd )
            {
                if( dictionary.ContainsKey( keyValuePair.Key ) == false )
                {
                    dictionary.Add( keyValuePair.Key, keyValuePair.Value );
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Converts a dictionary of string object to a dynamic.
        /// </summary>
        /// <param name="dictionary">Dictionary to convert.</param>
        /// <returns>dynamic representation of the dictionary.</returns>
        public static dynamic ToDynamic( this IDictionary<string, object> dictionary )
        {
            var dynamicDictionary = new DynamicDictionary();

            foreach( var keyValuePair in dictionary )
            {
                dynamicDictionary.Add( keyValuePair );
            }

            return dynamicDictionary;
        }
    }
}