using System;
using System.Collections.Generic;

namespace InfernumMode.OverridingSystem
{
    internal struct OverrideExclusionContext
	{
        public int EntityTypeID;
        public EntityOverrideContext ExclusionDomain;
        public OverrideExclusionContext(int entityTypeID, EntityOverrideContext exclusionDomain)
		{
            EntityTypeID = entityTypeID;
            ExclusionDomain = exclusionDomain;
		}
	}
	public static class OverridingListManager
    {
        internal static Dictionary<int, Delegate> InfernumNPCPreAIOverrideList = new Dictionary<int, Delegate>();
        internal static Dictionary<int, Delegate> InfernumSetDefaultsOverrideList = new Dictionary<int, Delegate>();
        internal static Dictionary<int, Delegate> InfernumPreDrawOverrideList = new Dictionary<int, Delegate>();
        internal static Dictionary<int, Delegate> InfernumFrameOverrideList = new Dictionary<int, Delegate>();

		internal static Dictionary<int, Delegate> InfernumProjectilePreAIOverrideList = new Dictionary<int, Delegate>();

        internal static Dictionary<OverrideExclusionContext, Func<bool>> ExclusionList = new Dictionary<OverrideExclusionContext, Func<bool>>();

        internal static void Load()
        {
            InfernumNPCPreAIOverrideList = new Dictionary<int, Delegate>();
            InfernumSetDefaultsOverrideList = new Dictionary<int, Delegate>();
            InfernumPreDrawOverrideList = new Dictionary<int, Delegate>();
            InfernumFrameOverrideList = new Dictionary<int, Delegate>();
            InfernumProjectilePreAIOverrideList = new Dictionary<int, Delegate>();
            ExclusionList = new Dictionary<OverrideExclusionContext, Func<bool>>();
        }

        internal static void Unload()
        {
            InfernumNPCPreAIOverrideList = null;
            InfernumSetDefaultsOverrideList = null;
            InfernumPreDrawOverrideList = null;
            InfernumFrameOverrideList = null;
            InfernumProjectilePreAIOverrideList = null;
            ExclusionList = null;
        }
    }
}
