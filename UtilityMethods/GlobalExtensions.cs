using InfernumMode.GlobalInstances;
using Terraria;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public static PoDPlayer Infernum(this Player player) => player.GetModPlayer<PoDPlayer>();
        public static GlobalNPCOverrides Infernum(this NPC npc) => npc.GetGlobalNPC<GlobalNPCOverrides>();
        public static GlobalProjectileOverrides Infernum(this Projectile projectile) => projectile.GetGlobalProjectile<GlobalProjectileOverrides>();
    }
}
