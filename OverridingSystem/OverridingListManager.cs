using System;
using System.Collections.Generic;

namespace InfernumMode.OverridingSystem
{
	public static class OverridingListManager
    {
        private static string message = "Yes this is extremely cumbersome and a pain in the ass but not doing it resulted in Calamity Rev+ AIs conflicting with this mode's";

        internal static Dictionary<int, Delegate> InfernumNPCPreAIOverrideList = new Dictionary<int, Delegate>();
        internal static Dictionary<int, Delegate> InfernumSetDefaultsOverrideList = new Dictionary<int, Delegate>();
        internal static Dictionary<int, Delegate> InfernumPreDrawOverrideList = new Dictionary<int, Delegate>();
        internal static Dictionary<int, Delegate> InfernumFrameOverrideList = new Dictionary<int, Delegate>();
        
		internal static Dictionary<int, Delegate> InfernumProjectilePreAIOverrideList = new Dictionary<int, Delegate>();
        internal static Dictionary<int, Delegate> InfernumProjectilePreDrawOverrideList = new Dictionary<int, Delegate>();

        internal static void Load()
        {
            InfernumNPCPreAIOverrideList = new Dictionary<int, Delegate>();
            InfernumSetDefaultsOverrideList = new Dictionary<int, Delegate>();
            InfernumPreDrawOverrideList = new Dictionary<int, Delegate>();
            InfernumFrameOverrideList = new Dictionary<int, Delegate>();
            InfernumProjectilePreAIOverrideList = new Dictionary<int, Delegate>();
            InfernumProjectilePreDrawOverrideList = new Dictionary<int, Delegate>();
        }

        internal static void Unload()
        {
            InfernumNPCPreAIOverrideList = null;
            InfernumSetDefaultsOverrideList = null;
            InfernumPreDrawOverrideList = null;
            InfernumFrameOverrideList = null;
            InfernumProjectilePreAIOverrideList = null;
            InfernumProjectilePreDrawOverrideList = null;
        }
    }
}
