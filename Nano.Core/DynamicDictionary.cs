using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace Nano.Core
{
    /// <summary>
    /// Dictionary that implements DynamicObject allowing case insensitive access to
    /// keys via direct member access as if the property exists on the object itself.
    /// </summary>
    public class DynamicDictionary : DynamicObject, IDictionary<string, object>
    {
        private readonly IDictionary<string, object> _dictionary = new Dictionary<string, object>( StringComparer.InvariantCultureIgnoreCase );

        #region DynamicObject Overrides

        public override bool TryGetMember( GetMemberBinder binder, out object result )
        {
            if ( _dictionary.ContainsKey( binder.Name ) )
            {
                result = _dictionary[ binder.Name ];

                return true;
            }

            return base.TryGetMember( binder, out result );
        }

        public override bool TrySetMember( SetMemberBinder binder, object value )
        {
            if ( _dictionary.ContainsKey( binder.Name ) )
            {
                _dictionary[binder.Name] = value;
            }
            else
            {
                _dictionary.Add( binder.Name, value );
            }

            return true;
        }

        public override bool TryInvokeMember( InvokeMemberBinder binder, object[] args, out object result )
        {
            if ( _dictionary.ContainsKey( binder.Name ) && _dictionary[ binder.Name ] is Delegate )
            {
                var delegateValue = _dictionary[ binder.Name ] as Delegate;

                result = delegateValue.DynamicInvoke( args );

                return true;
            }

            return base.TryInvokeMember( binder, args, out result );
        }

        public override bool TryDeleteMember( DeleteMemberBinder binder )
        {
            if ( _dictionary.ContainsKey( binder.Name ) )
            {
                _dictionary.Remove( binder.Name );

                return true;
            }

            return base.TryDeleteMember( binder );
        }

        #endregion DynamicObject Overrides

        #region IDictionary<string,object> Members

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

        public object this[ string key ]
        {
            get { return _dictionary[ key ]; }
            set { _dictionary[ key ] = value; }
        }

        #endregion IDictionary<string,object> Members

        #region ICollection<KeyValuePair<string,object>> Members

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

        #endregion ICollection<KeyValuePair<string,object>> Members

        #region IEnumerable<KeyValuePair<string,object>> Members

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        #endregion IEnumerable<KeyValuePair<string,object>> Members

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        #endregion IEnumerable Members
    }
}