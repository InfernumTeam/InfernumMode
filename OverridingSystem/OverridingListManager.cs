using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace InfernumMode.OverridingSystem
{
    public static class OverridingListManager
    {
#pragma warning disable IDE0051 // Remove unused private members
        private const string message = "Yes this is extremely cumbersome and a pain in the ass but not doing it resulted in Calamity Rev+ AIs conflicting with this mode's";
#pragma warning restore IDE0051 // Remove unused private members

        internal static Dictionary<int, NPCPreAIDelegate> InfernumNPCPreAIOverrideList = new Dictionary<int, NPCPreAIDelegate>();
        internal static Dictionary<int, Delegate> InfernumSetDefaultsOverrideList = new Dictionary<int, Delegate>();
        internal static Dictionary<int, NPCPreDrawDelegate> InfernumPreDrawOverrideList = new Dictionary<int, NPCPreDrawDelegate>();
        internal static Dictionary<int, Delegate> InfernumFrameOverrideList = new Dictionary<int, Delegate>();

        internal static Dictionary<int, Delegate> InfernumProjectilePreAIOverrideList = new Dictionary<int, Delegate>();
        internal static Dictionary<int, Delegate> InfernumProjectilePreDrawOverrideList = new Dictionary<int, Delegate>();

        public delegate bool NPCPreAIDelegate(NPC npc);
        public delegate bool NPCPreDrawDelegate(NPC npc, SpriteBatch spriteBatch, Color lightColor);

        internal static void Load()
        {
            InfernumNPCPreAIOverrideList = new Dictionary<int, NPCPreAIDelegate>();
            InfernumSetDefaultsOverrideList = new Dictionary<int, Delegate>();
            InfernumPreDrawOverrideList = new Dictionary<int, NPCPreDrawDelegate>();
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
