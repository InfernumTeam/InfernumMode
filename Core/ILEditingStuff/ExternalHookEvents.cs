using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace InfernumMode.Core.ILEditingStuff
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

            // Load the override system detours.
            ModifyDetour(SetDefaultMethod, OverrideSystemHooks.SetDefaultDetourMethod);
            ModifyDetour(FindFrameMethod, OverrideSystemHooks.FindFrameDetourMethod);
            ModifyDetour(CalPreAIMethod, OverrideSystemHooks.CalPreAIDetourMethod);
            ModifyDetour(CalGetAdrenalineDamageMethod, NerfAdrenalineHook.NerfAdrenDamageMethod);
            ModifyDetour(CalApplyRippersToDamageMethod, NerfAdrenalineHook.ApplyRippersToDamageDetour);
            ModifyDetour(CalModifyHitNPCWithItemMethod, NerfAdrenalineHook.ModifyHitNPCWithItemDetour);
            ModifyDetour(CalModifyHitNPCWithProjMethod, NerfAdrenalineHook.ModifyHitNPCWithProjDetour);
            ModifyDetour(CalGlobalNPCPredrawMethod, OverrideSystemHooks.CalGlobalNPCPredrawDetourMethod);
        }

        public static void Unload()
        {
            foreach (IHookEdit hook in Hooks)
                hook.Unload();

            foreach (var fuck in IlHooks)
                fuck?.Undo();

            foreach (var fuck2 in OnHooks)
                fuck2.Undo();
        }
    }
}
