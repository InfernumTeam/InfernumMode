using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.GlobalInstances.GlobalItems;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.OverridingSystem;
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

        /// <summary>
        /// Only for use with the CanOverride extension
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        internal static NPCBehaviorOverride NPCOverride(this object container) => container is NPCBehaviorOverrideContainer c ? c.BehaviorOverride : null!;

        /// <summary>
        /// Only for use with the CanOverride extension
        /// </summary>
        /// <param name="container"></param>
        /// <returns></returns>
        internal static ProjectileBehaviorOverride ProjectileOverride(this object container) => container is ProjectileBehaviorOverride c ? c : null!;

        /// <summary>
        /// Utility method to speed up common Infernum NPC/Projectile override checks
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        internal static bool CanOverride(Entity entity, out object container)
        {
            container = null!;
            if (!InfernumMode.CanUseCustomAIs)
                return false;

            if (entity is NPC n)
                container = NPCBehaviorOverride.BehaviorOverrideSet[n.type];
            else if (entity is Projectile p)
                container = ProjectileBehaviorOverride.BehaviorOverrideSet[p.type];

            if (container is null)
                return false;
            return true;
        }
    }
}
