using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Common.Graphics.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class ConvergingShadowSpark : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadow Spark");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.Opacity = 0f;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.2f, 0f, 1f);

            if (Projectile.velocity.Length() < 14f)
                Projectile.velocity *= 1.023f;

            // Explode if on top of Cal Clone.
            if (CalamityGlobalNPC.calamitas != -1 && Projectile.WithinRange(Main.npc[CalamityGlobalNPC.calamitas].Center, 30f))
            {
                NPC calClone = Main.npc[CalamityGlobalNPC.calamitas];
                Projectile.Center = calClone.Center + Vector2.UnitX * calClone.scale * calClone.spriteDirection * 12f;
                Projectile.Kill();
            }

            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(HolyBlast.ShootSound, Projectile.Center);

            // Explode into dark magic clouds and particles.
            for (int i = 0; i < 10; i++)
            {
                Dust darkMagic = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(30f, 30f), 261);
                darkMagic.color = Color.Lerp(Color.DarkBlue, Color.HotPink, Main.rand.NextFloat(0.7f));
                darkMagic.scale = Main.rand.NextFloat(1f, 1.1f);
                darkMagic.velocity = Main.rand.NextVector2Circular(2f, 2f);
                darkMagic.noGravity = true;
            }

            for (int i = 0; i < 8; i++)
            {
                if (!Main.rand.NextBool(5))
                    continue;

                Color fireColor = Main.rand.NextBool() ? Color.HotPink : Color.DarkBlue;
                CloudParticle fireCloud = new(Projectile.Center, (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 2f + Main.rand.NextVector2Circular(0.3f, 0.3f), fireColor, Color.DarkGray, 23, Main.rand.NextFloat(1.8f, 2f))
                {
                    Rotation = Main.rand.NextFloat(MathHelper.TwoPi)
                };
                GeneralParticleHandler.SpawnParticle(fireCloud);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            for (int i = 0; i < 12; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 2f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, frame, new Color(1f, 1f, 1f, 0f) * Projectile.Opacity * 0.65f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor);
            return false;
        }
    }
}
