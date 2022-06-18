using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Projectiles.Boss;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class MoltenBlast : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Boss/MoltenBlast";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Molten Blast");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = 42;
            projectile.height = 42;
            projectile.hostile = true;
            projectile.penetrate = 1;
            projectile.timeLeft = 180;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            projectile.frame = (projectile.frameCounter / 6) % Main.projFrames[projectile.type];

            if (projectile.wet || projectile.lavaWet)
                projectile.Kill();

            projectile.localAI[0]++;
            if (projectile.localAI[0] % 30f == 29f)
            {
                int dustType = (Main.dayTime && !CalamityWorld.malice) ? (int)CalamityDusts.ProfanedFire : (int)CalamityDusts.Nightwither;
                for (int i = 0; i < 12; i++)
                {
                    Vector2 spawnOffset = Vector2.UnitX * -projectile.width / 2f;
                    spawnOffset += -Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / 12f) * new Vector2(8f, 16f);
                    spawnOffset = spawnOffset.RotatedBy(projectile.rotation - MathHelper.PiOver2);

                    Dust fire = Dust.NewDustDirect(projectile.Center, 0, 0, dustType, 0f, 0f, 160, default, 1f);
                    fire.scale = 1.1f;
                    fire.noGravity = true;
                    fire.position = projectile.Center + spawnOffset;
                    fire.velocity = projectile.velocity * 0.1f;
                    fire.velocity = (projectile.Center - projectile.velocity * 3f - fire.position).SafeNormalize(Vector2.UnitY) * 1.25f;
                }
            }

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return (Main.dayTime && !CalamityWorld.malice) ? new Color(250, 150, 0, projectile.alpha) : new Color(100, 200, 250, projectile.alpha);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.dayTime ? ModContent.GetTexture(Texture) : ModContent.GetTexture("CalamityMod/Projectiles/Boss/MoltenBlastNight");
            int height = ModContent.GetTexture(Texture).Height / Main.projFrames[projectile.type];
            int top = height * projectile.frame;
            Vector2 drawPosition = projectile.Center - Main.screenPosition + Vector2.UnitY * projectile.gfxOffY;
            Rectangle frame = new Rectangle(0, top, texture.Width, height);
            Main.spriteBatch.Draw(texture, drawPosition, frame, projectile.GetAlpha(lightColor), projectile.rotation, frame.Size() * 0.5f, projectile.scale, 0, 0);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            int blobCount = (int)projectile.ai[0];
            if (projectile.owner == Main.myPlayer)
            {
                for (int i = 0; i < blobCount; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(9f, 9f);
                    Projectile.NewProjectile(projectile.Center, velocity, ModContent.ProjectileType<MoltenBlob>(), (int)Math.Round(projectile.damage * 0.75), 0f, projectile.owner);
                }

                float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                for (int i = 0; i < 8; i++)
                {
                    Vector2 velocity = (MathHelper.TwoPi * i / 8f + offsetAngle).ToRotationVector2() * 4f;
                    Projectile.NewProjectile(projectile.Center, velocity, ModContent.ProjectileType<MoltenFire>(), (int)Math.Round(projectile.damage * 0.75), 0f, projectile.owner);
                }
            }
            Main.PlaySound(SoundID.Item20, projectile.Center);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(projectile.Center, 18f, targetHitbox);

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int buffType = (Main.dayTime && !CalamityWorld.malice) ? ModContent.BuffType<HolyFlames>() : ModContent.BuffType<Nightwither>();
            target.AddBuff(buffType, 240);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
