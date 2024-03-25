using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        internal static List<int> InfernumNPCPreAIOverrideList = [];
        internal static List<int> InfernumSetDefaultsOverrideList = [];
        internal static List<int> InfernumPreDrawOverrideList = [];
        internal static List<int> InfernumFrameOverrideList = [];
        internal static List<int> InfernumCheckDeadOverrideList = [];

        internal static List<int> InfernumProjectilePreAIOverrideList = [];
        internal static List<int> InfernumProjectilePreDrawOverrideList = [];

        public delegate bool NPCPreAIDelegate(NPC npc);
        public delegate bool NPCPreDrawDelegate(NPC npc, SpriteBatch spriteBatch, Color lightColor);
        public delegate void NPCFindFrameDelegate(NPC npc, int frameHeight);
        public delegate bool NPCCheckDeadDelegate(NPC npc);

        public static bool Registered(int npcID) => NPCBehaviorOverride.BehaviorOverrides.ContainsKey(npcID);

        public static bool Registered<T>() where T : ModNPC => Registered(ModContent.NPCType<T>());

        public void Load(Mod mod)
        {
            InfernumNPCPreAIOverrideList = [];
            InfernumSetDefaultsOverrideList = [];
            InfernumPreDrawOverrideList = [];
            InfernumFrameOverrideList = [];
            InfernumCheckDeadOverrideList = [];

            InfernumProjectilePreAIOverrideList = [];
            InfernumProjectilePreDrawOverrideList = [];
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
