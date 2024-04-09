using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.OldDuke
{
    public class SulphuricBlob : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Sulphuric Blob");
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 56;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Fade in.
            Projectile.Opacity = Utils.GetLerpValue(0f, 10f, Time, true);

            // Emit sulphuric dust.
            Dust acid = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(30f, 30f) - Projectile.velocity, (int)CalamityDusts.SulfurousSeaAcid);
            acid.scale *= 1.45f;
            acid.velocity = Main.rand.NextVector2Circular(5f, 5f);
            acid.noGravity = true;

            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
            Time++;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 6; i++)
            {
                Vector2 shootVelocity = (TwoPi * i / 6f).ToRotationVector2() * 18f;
                Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, ModContent.ProjectileType<HomingAcid>(), OldDukeBehaviorOverride.HomingAcidDamage, 0f);
            }
        }

        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, 127);
    }
}
