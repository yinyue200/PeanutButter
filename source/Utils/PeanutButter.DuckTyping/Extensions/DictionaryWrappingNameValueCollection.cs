﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace PeanutButter.DuckTyping.Extensions
{
    /// <summary>
    /// Wraps a NameValueCollection in an IDictionary interface
    /// </summary>
    internal class DictionaryWrappingNameValueCollection : IDictionary<string, object>
    {
        private readonly NameValueCollection _data;

        /// <summary>
        /// Construct this dictionary with a NameValueCollection to wrap
        /// </summary>
        /// <param name="data"></param>
        public DictionaryWrappingNameValueCollection(NameValueCollection data)
        {
            _data = data;
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return new DictionaryWrappingNameValueCollectionEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<string, object> item)
        {
            _data.Add(item.Key, item.Value?.ToString());
        }

        /// <inheritdoc />
        public void Clear()
        {
            _data.Clear();
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, object> item)
        {
            if (!_data.AllKeys.Contains(item.Key))
                return false;
            return _data[item.Key] == item.Value?.ToString();
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            // TODO -- not necessary for my requirements
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, object> item)
        {
            if (!Contains(item))
                return false;
            _data.Remove(item.Key);
            return true;
        }

        /// <inheritdoc />
        public int Count => _data.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public bool ContainsKey(string key)
        {
            return _data.AllKeys.Contains(key);
        }

        /// <inheritdoc />
        public void Add(string key, object value)
        {
            _data.Add(key, value?.ToString());
        }

        /// <inheritdoc />
        public bool Remove(string key)
        {
            var result = _data.AllKeys.Contains(key);
            if (result)
                _data.Remove(key);
            return result;
        }

        /// <inheritdoc />
        public bool TryGetValue(string key, out object value)
        {
            if (!_data.AllKeys.Contains(key))
            {
                value = null;
                return false;
            }
            value = _data[key];
            return true;
        }

        /// <inheritdoc />
        public object this[string key]
        {
            get => _data[key];
            set => _data[key] = value?.ToString();  // TODO: could be better
        }

        /// <inheritdoc />
        public ICollection<string> Keys => _data.AllKeys;

        /// <inheritdoc />
        public ICollection<object> Values => _data.AllKeys.Select(k => _data[k]).ToArray();
    }
}