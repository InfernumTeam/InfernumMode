using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.OverridingSystem
{
    public class OverridingListManager : ILoadable
    {
#pragma warning disable IDE0051 // Remove unused private members
        private const string Message = "Yes this is extremely cumbersome and a pain in the ass but not doing it resulted in Calamity Rev+ AIs conflicting with this mode's";
#pragma warning restore IDE0051 // Remove unused private members

        internal static Dictionary<int, NPCPreAIDelegate> InfernumNPCPreAIOverrideList = new();
        internal static Dictionary<int, Delegate> InfernumSetDefaultsOverrideList = new();
        internal static Dictionary<int, NPCPreDrawDelegate> InfernumPreDrawOverrideList = new();
        internal static Dictionary<int, NPCFindFrameDelegate> InfernumFrameOverrideList = new();
        internal static Dictionary<int, NPCCheckDeadDelegate> InfernumCheckDeadOverrideList = new();

        internal static Dictionary<int, Delegate> InfernumProjectilePreAIOverrideList = new();
        internal static Dictionary<int, Delegate> InfernumProjectilePreDrawOverrideList = new();

        public delegate bool NPCPreAIDelegate(NPC npc);
        public delegate bool NPCPreDrawDelegate(NPC npc, SpriteBatch spriteBatch, Color lightColor);
        public delegate void NPCFindFrameDelegate(NPC npc, int frameHeight);
        public delegate bool NPCCheckDeadDelegate(NPC npc);

        public static bool Registered(int npcID) => InfernumNPCPreAIOverrideList.ContainsKey(npcID);

        public static bool Registered<T>() where T : ModNPC => Registered(ModContent.NPCType<T>());

        public void Load(Mod mod)
        {
            InfernumNPCPreAIOverrideList = new Dictionary<int, NPCPreAIDelegate>();
            InfernumSetDefaultsOverrideList = new Dictionary<int, Delegate>();
            InfernumPreDrawOverrideList = new Dictionary<int, NPCPreDrawDelegate>();
            InfernumFrameOverrideList = new Dictionary<int, NPCFindFrameDelegate>();
            InfernumCheckDeadOverrideList = new Dictionary<int, NPCCheckDeadDelegate>();
            InfernumProjectilePreAIOverrideList = new Dictionary<int, Delegate>();
            InfernumProjectilePreDrawOverrideList = new Dictionary<int, Delegate>();
        }

        public void Unload()
        {
            InfernumNPCPreAIOverrideList = null;
            InfernumSetDefaultsOverrideList = null;
            InfernumPreDrawOverrideList = null;
            InfernumFrameOverrideList = null;
            InfernumCheckDeadOverrideList = null;
            InfernumProjectilePreAIOverrideList = null;
            InfernumProjectilePreDrawOverrideList = null;
        }
    }
}
