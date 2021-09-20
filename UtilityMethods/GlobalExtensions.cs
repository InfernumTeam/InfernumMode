using InfernumMode.GlobalInstances;
using Terraria;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public static PoDPlayer Infernum(this Player player) => player.GetModPlayer<PoDPlayer>();
        public static BehaviorOverridesGlobal Infernum(this NPC npc) => npc.Infernum();
        public static FuckYouModeProjectileAIs Infernum(this Projectile projectile) => projectile.GetGlobalProjectile<FuckYouModeProjectileAIs>();
    }
}
