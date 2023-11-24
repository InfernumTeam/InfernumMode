using System;
using System.Diagnostics;

namespace InfernumMode.Common.DataStructures
{
    /// <summary>
    /// A wrapper class for a generic value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("{Value}")]
    public class Referenced<T>
    {
        private T value;

        public T Value
        {
            get => (T)Get();
            set => Set(value);
        }

        public Func<object> Get;

        public Action<object> Set;

        protected Referenced(Func<object> get, Action<object> set)
        {
            Get = get;
            Set = set;
        }

        public Referenced(T value)
        {
            this.value = value;
            Get = () => this.value;
            Set = v => this.value = (T)v;
        }

        public static implicit operator T(Referenced<T> referenced) => referenced.value;

        public static implicit operator Referenced<T>(Referenced<object> boxedRef) => new(boxedRef.Get, boxedRef.Set);
    }
}
