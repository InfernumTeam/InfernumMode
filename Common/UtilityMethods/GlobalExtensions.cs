using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.GlobalInstances.GlobalItems;
using InfernumMode.Core.GlobalInstances.Players;
using Terraria;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public static BiomeEffectsPlayer Infernum_Biome(this Player player) => player.GetModPlayer<BiomeEffectsPlayer>();

        public static ProfanedTempleCinderPlayer Infernum_TempleCinder(this Player player) => player.GetModPlayer<ProfanedTempleCinderPlayer>();

        public static CameraEffectsPlayer Infernum_Camera(this Player player) => player.GetModPlayer<CameraEffectsPlayer>();

        public static CalShadowHexesPlayer Infernum_CalShadowHex(this Player player) => player.GetModPlayer<CalShadowHexesPlayer>();

        public static InfernumPlayer Infernum(this Player player) => player.GetModPlayer<InfernumPlayer>();

        public static PetsPlayer Infernum_Pet(this Player player) => player.GetModPlayer<PetsPlayer>();

        public static TooltipChangeGlobalItem Infernum_Tooltips(this Item item) => item.GetGlobalItem<TooltipChangeGlobalItem>();

        public static GlobalNPCOverrides Infernum(this NPC npc) => npc.GetGlobalNPC<GlobalNPCOverrides>();

        public static GlobalProjectileOverrides Infernum(this Projectile projectile) => projectile.GetGlobalProjectile<GlobalProjectileOverrides>();
    }
}
