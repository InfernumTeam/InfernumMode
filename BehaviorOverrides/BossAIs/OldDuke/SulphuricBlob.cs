using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.OldDuke
{
    public class SulphuricBlob : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sulphuric Blob");
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 56;
            CooldownSlot = 1;
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

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Time++;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 6; i++)
            {
                Vector2 shootVelocity = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 18f;
                Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, ModContent.ProjectileType<HomingAcid>(), 275, 0f);
            }
        }

        

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<SulphuricPoisoning>(), 240);

        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, 127);
    }
}
