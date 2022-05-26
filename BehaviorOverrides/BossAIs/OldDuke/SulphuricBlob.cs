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
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sulphuric Blob");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 40;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.timeLeft = 80;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            // Fade in.
            projectile.Opacity = Utils.InverseLerp(0f, 10f, Time, true);

            // Emit sulphuric dust.
            Dust acid = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(30f, 30f) - projectile.velocity, (int)CalamityDusts.SulfurousSeaAcid);
            acid.scale *= 1.45f;
            acid.velocity = Main.rand.NextVector2Circular(5f, 5f);
            acid.noGravity = true;

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Time++;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 6; i++)
            {
                Vector2 shootVelocity = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 10f;
                Utilities.NewProjectileBetter(projectile.Center, shootVelocity, ModContent.ProjectileType<HomingAcid>(), 275, 0f);
            }
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<SulphuricPoisoning>(), 240);

        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, 127);
    }
}
