using CalamityMod;
using CalamityMod.CalPlayer;
using InfernumMode.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances.Players
{
    public class ProfanedTempleCinderPlayer : ModPlayer
    {
        // This is a one-frame trigger that indicates that more cinders should be created than usual.
        // This will be true for one frame before it flips back to being false, after influencing cinder spawn conditions.
        // This must be set to continuously true at a point in execution BEFORE PostUpdate is called if the effect is to be perpetual.
        public bool CreateALotOfHolyCinders
        {
            get;
            set;
        }

        public override void PostUpdate()
        {
            bool inProfanedTemple = Player.GetModPlayer<BiomeEffectsPlayer>().ZoneProfaned;

            // This hook is called for all players. As such, a Main.myPlayer check is necessary to ensure that only one client sends packets to
            // spawn the cinder projectiles.
            // Furthermore, cinders are only spawned if the player is in the profaned temple and at a sufficient depth as to be considered within the underworld.
            if (Main.myPlayer != Player.whoAmI || !inProfanedTemple || !Player.ZoneUnderworldHeight)
                return;

            bool createALotOfHolyCinders = CreateALotOfHolyCinders;
            float cinderSpawnInterpolant = CalamityPlayer.areThereAnyDamnBosses ? 0.9f : 0.1f;
            int cinderSpawnRate = (int)MathHelper.Lerp(6f, 2f, cinderSpawnInterpolant);
            float cinderFlySpeed = MathHelper.Lerp(6f, 12f, cinderSpawnInterpolant);
            if (createALotOfHolyCinders)
            {
                cinderSpawnRate = 1;
                cinderFlySpeed = 13.25f;
                CreateALotOfHolyCinders = false;
            }

            for (int i = 0; i < 3; i++)
            {
                if (!Main.rand.NextBool(cinderSpawnRate) || Main.gfxQuality < 0.35f)
                    continue;

                Vector2 cinderSpawnOffset = new(Main.rand.NextFloatDirection() * 1550f, 650f);
                Vector2 cinderVelocity = -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(0.23f, 0.98f)) * Main.rand.NextFloat(0.6f, 1.2f) * cinderFlySpeed;
                if (Main.rand.NextBool())
                {
                    cinderSpawnOffset = cinderSpawnOffset.RotatedBy(-MathHelper.PiOver2) * new Vector2(0.9f, 1f);
                    cinderVelocity = cinderVelocity.RotatedBy(-MathHelper.PiOver2) * new Vector2(1.8f, -1f);
                }

                if (Main.rand.NextBool(createALotOfHolyCinders ? 2 : 6))
                    cinderVelocity.X *= -1f;

                Utilities.NewProjectileBetter(Player.Center + cinderSpawnOffset, cinderVelocity, ModContent.ProjectileType<ProfanedTempleCinder>(), 0, 0f);
            }
        }
    }
}