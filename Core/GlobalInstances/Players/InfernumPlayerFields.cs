using System.Collections.Generic;
using InfernumMode.Common.DataStructures;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public partial class InfernumPlayer
    {
        /// <summary>
        /// Contains each field that the modplayer should have. These are created when accessed, either from LoadData or simply get/setting the field.
        /// This is done to drastically cut down on boilerplate code.
        /// </summary>
        private readonly Dictionary<string, Referenced<object>> PlayerFields = new();

        // If the value does not exist or is not the expected type, set it to default.
        private void LoadValue<T>(string key) where T : struct
        {
            if (!PlayerFields.TryGetValue(key, out var value) || value.Value is not T)
                PlayerFields[key] = new(default(T));
        }

        public T GetValue<T>(string key) where T : struct
        {
            LoadValue<T>(key);
            return (T)PlayerFields[key];
        }

        public void SetValue<T>(string key, object value) where T : struct
        {
            LoadValue<T>(key);
            PlayerFields[key].Value = value;
        }

        public Referenced<T> GetRefValue<T>(string key) where T : struct
        {
            LoadValue<T>(key);
            return PlayerFields[key];
        }
    }
}
