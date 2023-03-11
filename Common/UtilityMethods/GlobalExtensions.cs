using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.GlobalInstances;
using InfernumMode.GlobalInstances.GlobalItems;
using Terraria;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public static AccessoryPlayer Infernum_Accessory(this Player player) => player.GetModPlayer<AccessoryPlayer>();

        public static BiomeEffectsPlayer Infernum_Biome(this Player player) => player.GetModPlayer<BiomeEffectsPlayer>();

        public static CyberneticImmortalityPlayer Infernum_Immortality(this Player player) => player.GetModPlayer<CyberneticImmortalityPlayer>();

        public static EggPlayer Infernum_Egg(this Player player) => player.GetModPlayer<EggPlayer>();

        public static CustomMusicPlayer Infernum_Music(this Player player) => player.GetModPlayer<CustomMusicPlayer>();

        public static PhysicsDefiancePlayer Infernum_PhysicsDefiance(this Player player) => player.GetModPlayer<PhysicsDefiancePlayer>();

        public static ProfanedTempleCinderPlayer Infernum_TempleCinder(this Player player) => player.GetModPlayer<ProfanedTempleCinderPlayer>();

        public static CameraEffectsPlayer Infernum_Camera(this Player player) => player.GetModPlayer<CameraEffectsPlayer>();

        public static DebuffEffectsPlayer Infernum_Debuff(this Player player) => player.GetModPlayer<DebuffEffectsPlayer>();

        public static CalCloneHexesPlayer Infernum_CalCloneHex(this Player player) => player.GetModPlayer<CalCloneHexesPlayer>();

        public static EelSwallowEffectPlayer Infernum_Eel(this Player player) => player.GetModPlayer<EelSwallowEffectPlayer>();

        public static HatGirlTipsPlayer Infernum_HatGirl(this Player player) => player.GetModPlayer<HatGirlTipsPlayer>();

        public static TooltipChangeGlobalItem Infernum_Tooltips(this Item item) => item.GetGlobalItem<TooltipChangeGlobalItem>();

        public static GlobalNPCOverrides Infernum(this NPC npc) => npc.GetGlobalNPC<GlobalNPCOverrides>();

        public static GlobalProjectileOverrides Infernum(this Projectile projectile) => projectile.GetGlobalProjectile<GlobalProjectileOverrides>();
    }
}
