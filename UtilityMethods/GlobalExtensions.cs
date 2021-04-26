using InfernumMode.FuckYouModeAIs.MainAI;
using Terraria;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public static PoDPlayer Infernum(this Player player) => player.GetModPlayer<PoDPlayer>();
        public static FuckYouModeAIsGlobal Infernum(this NPC npc) => npc.GetGlobalNPC<FuckYouModeAIsGlobal>();
        public static FuckYouModeProjectileAIs Infernum(this Projectile projectile) => projectile.GetGlobalProjectile<FuckYouModeProjectileAIs>();
    }
}
