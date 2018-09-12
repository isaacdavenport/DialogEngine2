using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DialogGenerator.Core
{
    public static class Session
    {
        private static readonly Dictionary<string, object> msDictionary = new Dictionary<string, object>();
        private static SessionKeysIndexer msIndexer;
        public static event PropertyChangedEventHandler SessionPropertyChanged;

        private static void _onSessionPropertyChagned(string _propertyName)
        {
            SessionPropertyChanged(typeof(Session), new PropertyChangedEventArgs(_propertyName));
        }

        /// <summary>
        /// Contains helper methods for indexed access to session keys.
        /// </summary>
        public sealed class SessionKeysIndexer
        {
            /// <summary>
            /// Gets or sets session value.
            /// </summary>
            /// <param name="key">Session key.</param>
            /// <returns>Session object.</returns>
            public object this[string key]
            {
                get { return Get(key); }
                set { Set(key, value); }
            }
        }

        /// <summary>
        /// Set session variable. If value is NULL, it will be removed from session or ignored if does not exists in Keys dictionary.
        /// </summary>
        /// <param name="key">Session key..</param>
        /// <param name="value">Session object value.</param>
        public static void Set(string key, object value)
        {
            if (msDictionary.ContainsKey(key))
            {
                if (value != null)
                    msDictionary[key] = value;
                else
                    msDictionary.Remove(key);
            }
            else if (value != null)
            {
                msDictionary.Add(key, value);
            }

            //SessionPropertyChanged(null, key);
        }

        /// <summary>
        /// Sets session variable using specified object type as session key.
        /// </summary>
        /// <param name="value">Session variable object.</param>
        public static void Set(object value)
        {
            var key = value.GetType().Name;

            if (msDictionary.ContainsKey(key))
            {
                msDictionary[key] = value;
            }
            else
            {
                msDictionary.Add(key, value);
            }
        }

        /// <summary>
        /// Removes object from collection.
        /// </summary>
        /// <param name="key">Session key.</param>
        public static void Remove(string key)
        {
            msDictionary.Remove(key);
        }

        /// <summary>
        /// Removes item matched by specified <see cref="T:type" /> from session.
        /// <para>Session key is looked up by name of specified <see cref="T:type" />.</para>
        /// </summary>
        /// <typeparam name="T">Instances of type to remove.</typeparam>
        public static void Remove<T>()
        {
            msDictionary.Remove(typeof(T).Name);
        }

        /// <summary>
        /// Gets value by key.
        /// </summary>
        /// <param name="key">Key to find.</param>
        /// <returns>Session object if exists, otherwise null.</returns>
        public static object Get(string key)
        {
            if (msDictionary.ContainsKey(key))
            {
                return msDictionary[key];
            }

            return null;
        }

        /// <summary>
        /// Gets session object.
        /// </summary>
        /// <typeparam name="T">Type of object to return.</typeparam>
        /// <param name="key">Session key.</param>
        /// <returns>Instance of {T}, if any, otherwise null.</returns>
        public static T Get<T>(string key)
        {
            if (msDictionary.ContainsKey(key))
            {
                return (T)msDictionary[key];
            }

            return default(T);
        }

        /// <summary>
        /// Gets single instance of specified type. Search is performed using Type name.
        /// </summary>
        /// <typeparam name="T">Instance of {T} to return.</typeparam>
        /// <returns>Instance of T.</returns>
        public static T Get<T>()
        {
            string key = typeof(T).Name;

            return Get<T>(key);
        }


        /// <summary>
        /// Gets existing or creates new session object.
        /// </summary>
        /// <typeparam name="T">Type to create or return if exists.</typeparam>
        /// <param name="key">Session key.</param>
        /// <returns>Instance of {T}.</returns>
        public static T GetOrCreate<T>(string key) where T : new()
        {
            if (msDictionary.ContainsKey(key))
            {
                return (T)msDictionary[key];
            }

            T newInstance = new T();

            Set(key, newInstance);

            return newInstance;
        }


        /// <summary>
        /// Gets a value indicating whether object collection contains specified key.
        /// </summary>
        /// <param name="key">Key to find.</param>
        /// <returns>True if exists, otherwise false.</returns>
        public static bool Contains(string key)
        {
            return msDictionary.ContainsKey(key);
        }

        /// <summary>
        /// Clears all session values.
        /// </summary>
        public static void Clear()
        {
            msDictionary.Clear();
        }

        /// <summary>
        /// Gets indexed access to session values.
        /// </summary>
        public static SessionKeysIndexer Keys
        {
            get { return msIndexer ?? (msIndexer = new SessionKeysIndexer()); }
        }
    }


}
