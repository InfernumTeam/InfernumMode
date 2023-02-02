using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.GlobalInstances;
using Terraria;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public static BiomeEffectsPlayer Infernum_Biome(this Player player) => player.GetModPlayer<BiomeEffectsPlayer>();

        public static ProfanedTempleCinderPlayer Infernum_TempleCinder(this Player player) => player.GetModPlayer<ProfanedTempleCinderPlayer>();

        public static CameraEffectsPlayer Infernum_Camera(this Player player) => player.GetModPlayer<CameraEffectsPlayer>();

        public static DebuffEffectsPlayer Infernum_Debuff(this Player player) => player.GetModPlayer<DebuffEffectsPlayer>();

        public static EelSwallowEffectPlayer Infernum_Eel(this Player player) => player.GetModPlayer<EelSwallowEffectPlayer>();

        public static HatGirlTipsPlayer Infernum_HatGirl(this Player player) => player.GetModPlayer<HatGirlTipsPlayer>();

        public static GlobalNPCOverrides Infernum(this NPC npc) => npc.GetGlobalNPC<GlobalNPCOverrides>();

        public static GlobalProjectileOverrides Infernum(this Projectile projectile) => projectile.GetGlobalProjectile<GlobalProjectileOverrides>();
    }
}
