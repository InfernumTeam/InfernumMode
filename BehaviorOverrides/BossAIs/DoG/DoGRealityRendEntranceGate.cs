using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using Terraria.ID;
using CalamityMod.Projectiles.Boss;
using System;
using Terraria.Graphics.Shaders;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class DoGRealityRendEntranceGate : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Portal");
        }

        public override void SetDefaults()
        {
            projectile.width = 420;
            projectile.height = 420;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 255;
            projectile.timeLeft = 280;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            if (projectile.localAI[1] == 0f)
            {
                projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                projectile.localAI[1] = 1f;
            }

            Main.LocalPlayer.Infernum().CurrentScreenShakePower = (float)Math.Pow(MathHelper.Clamp(Time / 160f, 0f, 1f), 9D) * 45f + 5f;

            if (Main.netMode != NetmodeID.MultiplayerClient && Time > 150f && Time % 20f == 19f)
                Utilities.NewProjectileBetter(projectile.Center, Main.rand.NextVector2CircularEdge(18f, 18f), ModContent.ProjectileType<DoGFire>(), 300, 0f);

            // Play idle sounds.
            if (Main.netMode != NetmodeID.Server)
			{
                if (Time == 10f || Time == 70f || Time == 130f)
                {
                    var soundInstance = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaGrenadeExplosion"), projectile.Center);
                    if (soundInstance != null)
                    {
                        soundInstance.Pitch = 1f;
                        if (Time == 10f)
                            soundInstance.Pan = -0.7f;
                        if (Time == 70f)
                            soundInstance.Pan = 0.7f;
                        if (Time == 130f)
                            soundInstance.Pan = 0f;
                    }
                }
            }
            Time++;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            float leftCleaveAngularOffset = MathHelper.Pi * -0.18f;
            float rightCleaveAngularOffset = MathHelper.Pi * 0.18f;

            Texture2D texture = ModContent.GetTexture("CalamityMod/Projectiles/StarProj");
            Vector2 leftStart = projectile.Center - Vector2.UnitY.RotatedBy(leftCleaveAngularOffset) * 2700f;
            Vector2 leftEnd = projectile.Center + Vector2.UnitY.RotatedBy(leftCleaveAngularOffset) * 2700f;

            Vector2 rightStart = projectile.Center - Vector2.UnitY.RotatedBy(rightCleaveAngularOffset) * 2700f;
            Vector2 rightEnd = projectile.Center + Vector2.UnitY.RotatedBy(rightCleaveAngularOffset) * 2700f;

            Vector2 centerStart = projectile.Center - Vector2.UnitY * 2900f;
            Vector2 centerEnd = projectile.Center + Vector2.UnitY * 2900f;

            Color rendLineColor = Color.Cyan;
            rendLineColor.A = 0;

            drawLineFromPoints(leftStart, Vector2.Lerp(leftStart, leftEnd, Utils.InverseLerp(0f, 60f, Time, true)));
            if (Time > 60f)
                drawLineFromPoints(rightStart, Vector2.Lerp(rightStart, rightEnd, Utils.InverseLerp(60f, 120f, Time, true)));
            if (Time > 120f)
                drawLineFromPoints(centerStart, Vector2.Lerp(centerStart, centerEnd, Utils.InverseLerp(120f, 180f, Time, true)));

            void drawLineFromPoints(Vector2 startingPosition, Vector2 endingPosition)
            {
                Vector2 drawPosition = startingPosition;
                float rotation = (endingPosition - startingPosition).ToRotation() - MathHelper.PiOver2;
                while (Vector2.Distance(drawPosition, endingPosition) > 90f)
                {
                    drawPosition += (endingPosition - drawPosition).SafeNormalize(Vector2.UnitY) * texture.Width * 0.2f;
                    spriteBatch.Draw(texture, drawPosition - Main.screenPosition, null, rendLineColor, rotation, texture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(texture, drawPosition - Main.screenPosition, null, Color.Lerp(rendLineColor, Color.White, 0.5f), rotation, texture.Size() * 0.5f, new Vector2(0.5f, 1f), SpriteEffects.None, 0f);
                }
            }

            spriteBatch.EnterShaderRegion();

            float fade = Utils.InverseLerp(280f, 235f, projectile.timeLeft, true);
            if (projectile.timeLeft <= 45f)
                fade = Utils.InverseLerp(0f, 45f, projectile.timeLeft, true);
            Texture2D noiseTexture = ModContent.GetTexture("CalamityMod/ExtraTextures/VoronoiShapes");
            Vector2 drawPosition2 = projectile.Center - Main.screenPosition;
            Vector2 origin = noiseTexture.Size() * 0.5f;
            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(fade);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(Color.Cyan);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.Fuchsia);
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply();

            spriteBatch.Draw(noiseTexture, drawPosition2, null, Color.White, 0f, origin, 3.5f, SpriteEffects.None, 0f);
            spriteBatch.ExitShaderRegion();

            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    float pushSpeed = MathHelper.Lerp(0f, 45f, Utils.InverseLerp(3800f, 250f, projectile.Distance(player.Center), true));
                    player.velocity -= player.SafeDirectionTo(projectile.Center) * pushSpeed;
                }
            }

            if (Main.netMode != NetmodeID.Server)
            {
                var soundInstance = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/DevourerSpawn"), projectile.Center);
                if (soundInstance != null)
                    soundInstance.Volume = MathHelper.Clamp(soundInstance.Volume * 1.6f, 0f, 1f);

                for (int i = 0; i < 3; i++)
                {
                    soundInstance = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), projectile.Center);
                    if (soundInstance != null)
                    {
                        soundInstance.Pitch = -MathHelper.Lerp(0.1f, 0.4f, i / 3f);
                        soundInstance.Volume = 0.21f;
                    }
                }
            }
        }
    }
}
