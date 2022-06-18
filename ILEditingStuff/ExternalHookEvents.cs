using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace InfernumMode.ILEditingStuff
{
    public static partial class HookManager
    {
        public static List<IHookEdit> Hooks
        {
            get;
            private set;
        } = new List<IHookEdit>();

        public static void Load()
        {
            foreach (Type type in InfernumMode.Instance.Code.GetTypes())
            {
                if (!type.IsAbstract && type.GetInterfaces().Contains(typeof(IHookEdit)))
                {
                    IHookEdit hook = (IHookEdit)FormatterServices.GetUninitializedObject(type);
                    hook.Load();
                    Hooks.Add(hook);
                }
            }
        }

        public static void Unload()
        {
            foreach (IHookEdit hook in Hooks)
                hook.Unload();
        }
    }
}