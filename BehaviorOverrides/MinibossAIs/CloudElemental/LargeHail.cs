using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.CloudElemental
{
    public class LargeHail : ModProjectile
    {
        public enum HailType
        {
            Shatter,
            Fall
        }

        public ref float Timer => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Large Hail");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0;
        }

        public override void AI()
        {
            // Pick a random texture frame on the first frame.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.frame = Main.rand.Next(Main.projFrames[Projectile.type]);
                Projectile.localAI[0] = 1f;

                if ((HailType)Projectile.ai[0] == HailType.Fall)
                    Projectile.timeLeft = 240;
                else
                    Projectile.hide = true;
            }

            switch ((HailType)Projectile.ai[0])
            {
                // Slow down over time.
                case HailType.Shatter:
                    Projectile.velocity *= 0.99f;
                    break;
                // Speed up over time.
                case HailType.Fall:
                    Projectile.velocity *= 1.02f;

                    // Release a trail of ice.
                    if (Timer % 10 == 0)
                    {
                        Particle iceParticle = new SnowyIceParticle(Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 5, Projectile.velocity * 0.5f, Color.White, Main.rand.NextFloat(0.75f, 0.95f), 30);
                        GeneralParticleHandler.SpawnParticle(iceParticle);
                    }
                    break;
            }

            Lighting.AddLight(Projectile.Center, Color.Blue.ToVector3() * 0.64f);
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity += 0.03f, 0f, 1f);

            Timer++;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && (HailType)Projectile.ai[0] == HailType.Shatter)
            {
                SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);
                int hailNumber = Main.rand.Next(5, 8);
                int hailSpeed = 7;
                for (int i = 0; i < hailNumber; i++)
                    Utilities.NewProjectileBetter(Projectile.Center, Vector2.UnitY.RotatedBy((MathHelper.TwoPi * i / hailNumber) + Main.rand.NextFloat(-0.3f, 0.3f)) * hailSpeed, ModContent.ProjectileType<SmallHail>(), 60, 0, Main.myPlayer);
            }

            for (int i = 0; i < 20; i++)
            {
                Vector2 velocity =  (Vector2.UnitY * Main.rand.NextFloat(2f, 6f)).RotatedByRandom(MathHelper.TwoPi);
                Particle iceParticle = new SnowyIceParticle(Projectile.Center, velocity, Color.White, Main.rand.NextFloat(0.75f, 0.95f), 60);
                GeneralParticleHandler.SpawnParticle(iceParticle);
            }
        }

        // Draw over players as it spawns from inside you.
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        { 
            if ((HailType)Projectile.ai[0] == HailType.Shatter)
                overPlayers.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            if ((HailType)Projectile.ai[0] is HailType.Shatter or HailType.Fall)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 4f;
                    Main.spriteBatch.Draw(texture, drawPosition + drawOffset, frame, new Color(0.6f, 0.6f, 1f, 0f) * Projectile.Opacity * 0.65f, Projectile.rotation, origin, Projectile.scale, 0, 0f);
                }
            }
            else
            {
                for (int i = 1; i < Projectile.oldPos.Length; i++)
                {
                    if (!CalamityConfig.Instance.Afterimages)
                        break;

                    float scale = Projectile.scale * MathHelper.Lerp(0.9f, 0.45f, i / (float)Projectile.oldPos.Length);
                    float trailLength = MathHelper.Lerp(30f, 55f, Utils.GetLerpValue(3f, 7f, Projectile.velocity.Length(), true));
                    if (Projectile.velocity.Length() < 1.8f)
                        trailLength = 8f;

                    Color drawColor = Color.LightCyan * (1f - i / (float)Projectile.oldPos.Length);
                    drawColor.A = 0;
                    drawColor *= Projectile.Opacity;

                    drawPosition = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * -MathHelper.Lerp(8f, trailLength, i / (float)Projectile.oldPos.Length);

                    Main.spriteBatch.Draw(texture, drawPosition - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), frame, Projectile.GetAlpha(lightColor) * Projectile.Opacity, Projectile.rotation, frame.Size() / 2f, scale, SpriteEffects.None, 0f);
                }
            }
            Main.spriteBatch.Draw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor) * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, 0, 0f);
            return false;
        }
    }
}