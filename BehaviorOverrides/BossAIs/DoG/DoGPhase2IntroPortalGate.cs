using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.NPCs.DevourerofGods;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class DoGPhase2IntroPortalGate : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public const int Phase2AnimationTime = 280;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Portal");
        }

        public override void SetDefaults()
        {
            Projectile.width = 420;
            Projectile.height = 420;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.timeLeft = Phase2AnimationTime;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                Projectile.localAI[1] = 1f;
            }

            Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = (float)Math.Pow(MathHelper.Clamp(Time / 160f, 0f, 1f), 9D) * 45f + 5f;

            // Play idle sounds.
            if (Main.netMode != NetmodeID.Server)
            {
                if (Time is 10f or 70f or 130f)
                    SoundEngine.PlaySound(PlasmaGrenade.ExplosionSound with { Pitch = 1f }, Projectile.Center);
            }
            Time++;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            float leftCleaveAngularOffset = MathHelper.Pi * -0.18f;
            float rightCleaveAngularOffset = MathHelper.Pi * 0.18f;

            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/StarProj").Value;
            Vector2 leftStart = Projectile.Center - Vector2.UnitY.RotatedBy(leftCleaveAngularOffset) * 2700f;
            Vector2 leftEnd = Projectile.Center + Vector2.UnitY.RotatedBy(leftCleaveAngularOffset) * 2700f;

            Vector2 rightStart = Projectile.Center - Vector2.UnitY.RotatedBy(rightCleaveAngularOffset) * 2700f;
            Vector2 rightEnd = Projectile.Center + Vector2.UnitY.RotatedBy(rightCleaveAngularOffset) * 2700f;

            Vector2 centerStart = Projectile.Center - Vector2.UnitY * 2900f;
            Vector2 centerEnd = Projectile.Center + Vector2.UnitY * 2900f;

            Color rendLineColor = Color.Cyan;
            rendLineColor.A = 0;

            drawLineFromPoints(leftStart, Vector2.Lerp(leftStart, leftEnd, Utils.GetLerpValue(0f, 60f, Time, true)));
            if (Time > 60f)
                drawLineFromPoints(rightStart, Vector2.Lerp(rightStart, rightEnd, Utils.GetLerpValue(60f, 120f, Time, true)));
            if (Time > 120f)
                drawLineFromPoints(centerStart, Vector2.Lerp(centerStart, centerEnd, Utils.GetLerpValue(120f, 180f, Time, true)));

            void drawLineFromPoints(Vector2 startingPosition, Vector2 endingPosition)
            {
                Vector2 drawPosition = startingPosition;
                float rotation = (endingPosition - startingPosition).ToRotation() - MathHelper.PiOver2;
                while (Vector2.Distance(drawPosition, endingPosition) > 90f)
                {
                    drawPosition += (endingPosition - drawPosition).SafeNormalize(Vector2.UnitY) * texture.Width * 0.2f;
                    Main.spriteBatch.Draw(texture, drawPosition - Main.screenPosition, null, rendLineColor, rotation, texture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(texture, drawPosition - Main.screenPosition, null, Color.Lerp(rendLineColor, Color.White, 0.5f), rotation, texture.Size() * 0.5f, new Vector2(0.5f, 1f), SpriteEffects.None, 0f);
                }
            }

            Main.spriteBatch.EnterShaderRegion();

            float fade = Utils.GetLerpValue(Phase2AnimationTime, Phase2AnimationTime - 45f, Projectile.timeLeft, true);
            if (Projectile.timeLeft <= 45f)
                fade = Utils.GetLerpValue(0f, 45f, Projectile.timeLeft, true);

            Texture2D noiseTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/VoronoiShapes").Value;
            Vector2 drawPosition2 = Projectile.Center - Main.screenPosition;
            Vector2 origin = noiseTexture.Size() * 0.5f;
            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(fade);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(Color.Cyan);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.Fuchsia);
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply();

            Main.spriteBatch.Draw(noiseTexture, drawPosition2, null, Color.White, 0f, origin, 3.5f, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    float pushSpeed = MathHelper.Lerp(0f, 45f, Utils.GetLerpValue(3800f, 250f, Projectile.Distance(player.Center), true));
                    player.velocity -= player.SafeDirectionTo(Projectile.Center) * pushSpeed;
                }
            }

            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(DevourerofGodsHead.SpawnSound with { Volume = 2.5f }, Main.LocalPlayer.Center);
                SoundEngine.PlaySound(InfernumSoundRegistry.DoGLaughSound with { Volume = 5f }, Main.LocalPlayer.Center);
                SoundEngine.PlaySound(TeslaCannon.FireSound with { Pitch = 0.2f, Volume = 3f }, Main.LocalPlayer.Center);
            }
        }
    }
}
