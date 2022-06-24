using CalamityMod;
using InfernumMode.ILEditingStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class LightCloud : ModProjectile
    {
        public ref float LightPower => ref Projectile.ai[0];

        public ref float Time => ref Projectile.ai[1];

        public ref float TelegraphDirection => ref Projectile.localAI[0];

        public const int LaserTelegraphTime = 135;

        public const int CloudLifetime = 420;

        public const int LaserLifetime = CloudLifetime - LaserTelegraphTime - 12;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Cloud");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 132;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = CloudLifetime;
            Projectile.scale = 1.5f;
            Projectile.hide = true;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            // Decide scale and initial rotation on the first frame this projectile exists.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale = 1.3f;
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                Projectile.localAI[0] = 1f;
            }

            // Calculate light power. This checks below the position of the fog to check if this fog is underground.
            // Without this, it may render over the fullblack that the game renders for obscured tiles.
            float lightPowerBelow = Lighting.GetColor((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16 + 6).ToVector3().Length() / (float)Math.Sqrt(3D);
            LightPower = MathHelper.Lerp(LightPower, lightPowerBelow, 0.15f);
            Projectile.Opacity = Utils.GetLerpValue(0f, 25f, CloudLifetime - Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 60f, Projectile.timeLeft, true);

            // Release the laserbeam.
            if (Time == LaserTelegraphTime)
            {
                SoundEngine.PlaySound(SoundID.Item163, Projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                    float aimDirection = (MathHelper.WrapAngle(Projectile.AngleTo(target.Center) - TelegraphDirection) > 0f).ToDirectionInt();
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 laserVelocity = (MathHelper.TwoPi * i / 4f + TelegraphDirection).ToRotationVector2();
                        int laser = Utilities.NewProjectileBetter(Projectile.Center, laserVelocity, ModContent.ProjectileType<SpinningPrismLaserbeam>(), EmpressOfLightBehaviorOverride.LaserbeamDamage, 0f);
                        if (Main.projectile.IndexInRange(laser))
                            Main.projectile[laser].ai[0] = aimDirection * 0.0167f;
                    }
                }
            }

            // Periodically release bursts of light.
            if (Time >= LaserTelegraphTime && Time % 30f == 29f && Projectile.timeLeft >= 64)
            {
                SoundEngine.PlaySound(SoundID.Item28, Projectile.Center);

                for (int i = 0; i < 16; i++)
                {
                    Vector2 boltVelocity = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * Main.rand.NextFloat(12f, 18f);
                    int bolt = Utilities.NewProjectileBetter(Projectile.Center, boltVelocity, ModContent.ProjectileType<LightBolt>(), EmpressOfLightBehaviorOverride.PrismaticBoltDamage, 0f);
                    if (Main.projectile.IndexInRange(bolt))
                        Main.projectile[bolt].ai[1] = Main.rand.NextFloat();
                }
            }

            Time++;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            DrawBlackEffectHook.DrawCacheAdditiveLighting.Add(index);
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.6f ? null : false;

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 screenArea = new(Main.screenWidth, Main.screenHeight);
            Rectangle screenRectangle = Utils.CenteredRectangle(Main.screenPosition + screenArea * 0.5f, screenArea * 1.33f);

            if (!Projectile.Hitbox.Intersects(screenRectangle))
                return false;

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(0f, 0.08f, LightPower, true) * Projectile.Opacity * 1.2f;
            Vector2 scale = Projectile.Size / texture.Size() * Projectile.scale * 0.9f;
            float telegraphInterpolant = Utils.GetLerpValue(0f, LaserTelegraphTime - 25f, Time, true);

            // Cast a telegraph line.
            if (Time < LaserTelegraphTime)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (telegraphInterpolant < 1f)
                    TelegraphDirection = Projectile.AngleTo(target.Center);

                for (int i = 0; i < 4; i++)
                {
                    float offsetAngle = MathHelper.TwoPi * i / 4f;
                    Vector2 start = Projectile.Center;
                    Vector2 end = start + (TelegraphDirection + offsetAngle).ToRotationVector2() * SpinningPrismLaserbeam.MaxLaserLength;
                    float telegraphWidth = MathHelper.Lerp(1f, 6f, telegraphInterpolant);
                    float innerTelegraphWidth = telegraphWidth * 0.35f;
                    Color telegraphColor = Main.hslToRgb((float)(Math.Sin(MathHelper.TwoPi * telegraphInterpolant) * 0.5f + 0.5f), 1f, 0.6f);
                    Color innerTelegraphColor = Color.Lerp(telegraphColor, Color.White, 0.7f);

                    Main.spriteBatch.DrawLineBetter(start, end, telegraphColor, telegraphWidth);
                    Main.spriteBatch.DrawLineBetter(start, end, innerTelegraphColor, innerTelegraphWidth);
                }
            }

            for (int i = 0; i < 6; i++)
            {
                Color cloudColor = Main.hslToRgb((i / 6f + Main.GlobalTimeWrappedHourly * 0.19f) % 1f, 1f, 0.82f) * opacity;
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f - Main.GlobalTimeWrappedHourly * 0.93f).ToRotationVector2() * 25f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, cloudColor, Projectile.rotation, origin, scale * 1.5f, SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}
