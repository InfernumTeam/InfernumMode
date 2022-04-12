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
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 300;
            Projectile.Calamity().affectedByMaliceModeVelocityMultiplier = true;
            CooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
        }

        public override void AI()
        {
            if (Projectile.timeLeft < 210)
                Projectile.tileCollide = true;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            if (Projectile.alpha > 0)
                Projectile.alpha -= 17;

            Projectile.ai[1] += 1f;
            if (Projectile.ai[1] <= 20f)
                Projectile.velocity *= 0.95f;
            else if (Projectile.ai[1] is > 20f and <= 39f)
                Projectile.velocity *= 1.1f;
            else if (Projectile.ai[1] == 40f)
                Projectile.ai[1] = 0f;

            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == 30f)
            {
                Projectile.localAI[0] = 0f;
                for (int l = 0; l < 12; l++)
                {
                    Vector2 spawnOffset = Vector2.UnitX * -Projectile.width / 2f;
                    spawnOffset += -Vector2.UnitY.RotatedBy(MathHelper.TwoPi * l / 12f) * new Vector2(8f, 16f);
                    spawnOffset = spawnOffset.RotatedBy(Projectile.rotation - MathHelper.PiOver2);
                    int fire = Dust.NewDust(Projectile.Center, 0, 0, 244, 0f, 0f, 160, default, 1f);
                    Main.dust[fire].scale = 1.1f;
                    Main.dust[fire].noGravity = true;
                    Main.dust[fire].position = Projectile.Center + spawnOffset;
                    Main.dust[fire].velocity = Projectile.velocity * 0.1f;
                    Main.dust[fire].velocity = Vector2.Normalize(Projectile.Center - Projectile.velocity * 3f - Main.dust[fire].position) * 1.25f;
                }
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color baseColor = new(250, 150, 0, Projectile.alpha);
            if (!Main.dayTime)
            {
                baseColor = CalamityUtils.MulticolorLerp(Projectile.identity / 6f % 0.65f, ProvidenceBehaviorOverride.NightPalette);
                baseColor.A = (byte)Projectile.alpha;
            }
            return baseColor;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int buffType = Main.dayTime ? ModContent.BuffType<HolyFlames>() : ModContent.BuffType<Nightwither>();
            target.AddBuff(buffType, 180);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
