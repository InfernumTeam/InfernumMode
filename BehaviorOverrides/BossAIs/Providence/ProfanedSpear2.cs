using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class ProfanedSpear2 : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Profaned Spear");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 32;
            projectile.height = 32;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.alpha = 255;
            projectile.timeLeft = 300;
            projectile.Calamity().affectedByMaliceModeVelocityMultiplier = true;
            cooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(projectile.localAI[0]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            projectile.localAI[0] = reader.ReadSingle();
        }

        public override void AI()
        {
            if (projectile.timeLeft < 210)
                projectile.tileCollide = true;

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver4;

            if (projectile.alpha > 0)
                projectile.alpha -= 17;

            projectile.ai[1] += 1f;
            if (projectile.ai[1] <= 20f)
                projectile.velocity *= 0.95f;
            else if (projectile.ai[1] > 20f && projectile.ai[1] <= 39f)
                projectile.velocity *= 1.1f;
            else if (projectile.ai[1] == 40f)
                projectile.ai[1] = 0f;

            projectile.localAI[0]++;
            if (projectile.localAI[0] == 30f)
            {
                projectile.localAI[0] = 0f;
                for (int l = 0; l < 12; l++)
                {
                    Vector2 spawnOffset = Vector2.UnitX * -projectile.width / 2f;
                    spawnOffset += -Vector2.UnitY.RotatedBy(MathHelper.TwoPi * l / 12f) * new Vector2(8f, 16f);
                    spawnOffset = spawnOffset.RotatedBy(projectile.rotation - MathHelper.PiOver2);
                    int fire = Dust.NewDust(projectile.Center, 0, 0, 244, 0f, 0f, 160, default, 1f);
                    Main.dust[fire].scale = 1.1f;
                    Main.dust[fire].noGravity = true;
                    Main.dust[fire].position = projectile.Center + spawnOffset;
                    Main.dust[fire].velocity = projectile.velocity * 0.1f;
                    Main.dust[fire].velocity = Vector2.Normalize(projectile.Center - projectile.velocity * 3f - Main.dust[fire].position) * 1.25f;
                }
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color baseColor = new Color(250, 150, 0, projectile.alpha);
            if (!Main.dayTime)
            {
                baseColor = CalamityUtils.MulticolorLerp(projectile.identity / 6f % 0.65f, ProvidenceBehaviorOverride.NightPalette);
                baseColor.A = (byte)projectile.alpha;
            }
            return baseColor;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor, 1);
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int buffType = Main.dayTime ? ModContent.BuffType<HolyFlames>() : ModContent.BuffType<Nightwither>();
            target.AddBuff(buffType, 180);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
