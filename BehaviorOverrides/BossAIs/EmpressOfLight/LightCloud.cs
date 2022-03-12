using CalamityMod;
using InfernumMode.ILEditingStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class LightCloud : ModProjectile
    {
        public ref float LightPower => ref projectile.ai[0];

        public ref float Time => ref projectile.ai[1];

        public ref float TelegraphDirection => ref projectile.localAI[0];

        public const int LaserTelegraphTime = 60;

        public const int CloudLifetime = 300;

        public const int LaserLifetime = CloudLifetime - LaserTelegraphTime - 12;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Cloud");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 132;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.magic = true;
            projectile.timeLeft = CloudLifetime;
            projectile.scale = 1.5f;
            projectile.hide = true;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.hide = true;
            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            // Decide scale and initial rotation on the first frame this projectile exists.
            if (projectile.localAI[0] == 0f)
            {
                projectile.scale = 1.3f;
                projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                projectile.localAI[0] = 1f;
            }

            // Calculate light power. This checks below the position of the fog to check if this fog is underground.
            // Without this, it may render over the fullblack that the game renders for obscured tiles.
            float lightPowerBelow = Lighting.GetColor((int)projectile.Center.X / 16, (int)projectile.Center.Y / 16 + 6).ToVector3().Length() / (float)Math.Sqrt(3D);
            LightPower = MathHelper.Lerp(LightPower, lightPowerBelow, 0.15f);
            projectile.Opacity = Utils.InverseLerp(0f, 25f, CloudLifetime - projectile.timeLeft, true) * Utils.InverseLerp(0f, 60f, projectile.timeLeft, true);

            // Release the laserbeam.
            if (Time == LaserTelegraphTime)
            {
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/EmpressOfLightMagicCast"), projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                    float aimDirection = (MathHelper.WrapAngle(projectile.AngleTo(target.Center) - TelegraphDirection) > 0f).ToDirectionInt();
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 laserVelocity = (MathHelper.TwoPi * i / 4f + TelegraphDirection).ToRotationVector2();
                        int laser = Utilities.NewProjectileBetter(projectile.Center, laserVelocity, ModContent.ProjectileType<SpinningPrismLaserbeam>(), 300, 0f);
                        if (Main.projectile.IndexInRange(laser))
                            Main.projectile[laser].ai[0] = aimDirection * 0.025f;
                    }
                }
            }

            // Periodically release bursts of light.
            if (Time >= LaserTelegraphTime && Time % 30f == 29f && projectile.timeLeft >= 64)
            {
                Main.PlaySound(SoundID.Item28, projectile.Center);

                for (int i = 0; i < 16; i++)
                {
                    Vector2 boltVelocity = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * Main.rand.NextFloat(12f, 18f);
                    int bolt = Utilities.NewProjectileBetter(projectile.Center, boltVelocity, ModContent.ProjectileType<LightBolt>(), 180, 0f);
                    if (Main.projectile.IndexInRange(bolt))
                        Main.projectile[bolt].ai[1] = Main.rand.NextFloat();
                }
            }

            Time++;
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            DrawBlackEffectHook.DrawCacheAdditiveLighting.Add(index);
        }

        public override bool CanDamage() => projectile.Opacity > 0.6f;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 screenArea = new Vector2(Main.screenWidth, Main.screenHeight);
            Rectangle screenRectangle = Utils.CenteredRectangle(Main.screenPosition + screenArea * 0.5f, screenArea * 1.33f);

            if (!projectile.Hitbox.Intersects(screenRectangle))
                return false;

            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            float opacity = Utils.InverseLerp(0f, 0.08f, LightPower, true) * projectile.Opacity * 1.2f;
            Vector2 scale = projectile.Size / texture.Size() * projectile.scale * 0.9f;
            float telegraphInterpolant = Utils.InverseLerp(0f, LaserTelegraphTime - 25f, Time, true);

            // Cast a telegraph line.
            if (Time < LaserTelegraphTime)
            {
                Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                if (telegraphInterpolant < 1f)
                    TelegraphDirection = projectile.AngleTo(target.Center);

                for (int i = 0; i < 4; i++)
                {
                    float offsetAngle = MathHelper.TwoPi * i / 4f;
                    Vector2 start = projectile.Center;
                    Vector2 end = start + (TelegraphDirection + offsetAngle).ToRotationVector2() * SpinningPrismLaserbeam.MaxLaserLength;
                    float telegraphWidth = MathHelper.Lerp(1f, 6f, telegraphInterpolant);
                    float innerTelegraphWidth = telegraphWidth * 0.35f;
                    Color telegraphColor = Main.hslToRgb((float)(Math.Sin(MathHelper.TwoPi * telegraphInterpolant) * 0.5f + 0.5f), 1f, 0.6f);
                    Color innerTelegraphColor = Color.Lerp(telegraphColor, Color.White, 0.7f);

                    spriteBatch.DrawLineBetter(start, end, telegraphColor, telegraphWidth);
                    spriteBatch.DrawLineBetter(start, end, innerTelegraphColor, innerTelegraphWidth);
                }
            }

            for (int i = 0; i < 6; i++)
            {
                Color cloudColor = Main.hslToRgb((i / 6f + Main.GlobalTime * 0.19f) % 1f, 1f, 0.82f) * opacity;
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f - Main.GlobalTime * 0.93f).ToRotationVector2() * 25f;
                spriteBatch.Draw(texture, drawPosition + drawOffset, null, cloudColor, projectile.rotation, origin, scale * 1.5f, SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}
