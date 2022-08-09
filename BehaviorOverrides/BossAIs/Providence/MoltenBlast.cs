using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
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
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = (Projectile.frameCounter / 6) % Main.projFrames[Projectile.type];

            if (Projectile.wet || Projectile.lavaWet)
                Projectile.Kill();

            Projectile.localAI[0]++;
            if (Projectile.localAI[0] % 30f == 29f)
            {
                int dustType = Main.dayTime ? (int)CalamityDusts.ProfanedFire : (int)CalamityDusts.Nightwither;
                for (int i = 0; i < 12; i++)
                {
                    Vector2 spawnOffset = Vector2.UnitX * -Projectile.width / 2f;
                    spawnOffset += -Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / 12f) * new Vector2(8f, 16f);
                    spawnOffset = spawnOffset.RotatedBy(Projectile.rotation - MathHelper.PiOver2);

                    Dust fire = Dust.NewDustDirect(Projectile.Center, 0, 0, dustType, 0f, 0f, 160, default, 1f);
                    fire.scale = 1.1f;
                    fire.noGravity = true;
                    fire.position = Projectile.Center + spawnOffset;
                    fire.velocity = Projectile.velocity * 0.1f;
                    fire.velocity = (Projectile.Center - Projectile.velocity * 3f - fire.position).SafeNormalize(Vector2.UnitY) * 1.25f;
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Main.dayTime ? new Color(250, 150, 0, Projectile.alpha) : new Color(100, 200, 250, Projectile.alpha);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Main.dayTime ? ModContent.Request<Texture2D>(Texture).Value : ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/MoltenBlastNight").Value;
            int height = ModContent.Request<Texture2D>(Texture).Value.Height / Main.projFrames[Projectile.type];
            int top = height * Projectile.frame;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY;
            Rectangle frame = new(0, top, texture.Width, height);
            Main.spriteBatch.Draw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            int blobCount = (int)Projectile.ai[0];
            if (Projectile.owner == Main.myPlayer)
            {
                for (int i = 0; i < blobCount; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(9f, 9f);
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, velocity, ModContent.ProjectileType<MoltenBlob>(), (int)Math.Round(Projectile.damage * 0.75), 0f, Projectile.owner);
                }

                float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                for (int i = 0; i < 8; i++)
                {
                    Vector2 velocity = (MathHelper.TwoPi * i / 8f + offsetAngle).ToRotationVector2() * 4f;
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, velocity, ModContent.ProjectileType<MoltenFire>(), (int)Math.Round(Projectile.damage * 0.75), 0f, Projectile.owner);
                }
            }
            SoundEngine.PlaySound(SoundID.Item20, Projectile.Center);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, 18f, targetHitbox);

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int buffType = Main.dayTime ? ModContent.BuffType<HolyFlames>() : ModContent.BuffType<Nightwither>();
            target.AddBuff(buffType, 240);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
