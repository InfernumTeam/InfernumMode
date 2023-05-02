using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.GlobalInstances.GlobalItems;
using InfernumMode.Core.GlobalInstances.Players;
using Terraria;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public static AccessoryPlayer Infernum_Accessory(this Player player) => player.GetModPlayer<AccessoryPlayer>();

        public static BiomeEffectsPlayer Infernum_Biome(this Player player) => player.GetModPlayer<BiomeEffectsPlayer>();

        public static CyberneticImmortalityPlayer Infernum_Immortality(this Player player) => player.GetModPlayer<CyberneticImmortalityPlayer>();

        public static EggPlayer Infernum_Egg(this Player player) => player.GetModPlayer<EggPlayer>();

        public static PhysicsDefiancePlayer Infernum_PhysicsDefiance(this Player player) => player.GetModPlayer<PhysicsDefiancePlayer>();

        public static ProfanedTempleCinderPlayer Infernum_TempleCinder(this Player player) => player.GetModPlayer<ProfanedTempleCinderPlayer>();

        public static CameraEffectsPlayer Infernum_Camera(this Player player) => player.GetModPlayer<CameraEffectsPlayer>();

        public static DebuffEffectsPlayer Infernum_Debuff(this Player player) => player.GetModPlayer<DebuffEffectsPlayer>();

        public static CalShadowHexesPlayer Infernum_CalShadowHex(this Player player) => player.GetModPlayer<CalShadowHexesPlayer>();

        public static EelSwallowEffectPlayer Infernum_Eel(this Player player) => player.GetModPlayer<EelSwallowEffectPlayer>();

        public static TipsPlayer Infernum_Tips(this Player player) => player.GetModPlayer<TipsPlayer>();

        public static PetsPlayer Infernum_Pet(this Player player) => player.GetModPlayer<PetsPlayer>();

        public static UIPlayer Infernum_UI(this Player player) => player.GetModPlayer<UIPlayer>();

        public static TooltipChangeGlobalItem Infernum_Tooltips(this Item item) => item.GetGlobalItem<TooltipChangeGlobalItem>();

        public static GlobalNPCOverrides Infernum(this NPC npc) => npc.GetGlobalNPC<GlobalNPCOverrides>();

        public static GlobalProjectileOverrides Infernum(this Projectile projectile) => projectile.GetGlobalProjectile<GlobalProjectileOverrides>();
    }
}
