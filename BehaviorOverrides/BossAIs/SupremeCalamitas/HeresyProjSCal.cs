using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class HeresyProjSCal : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];

        public const int ChargeupTime = 135;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Heresy");
            Main.projFrames[projectile.type] = 8;
        }

        public override void SetDefaults()
        {
            projectile.width = 22;
            projectile.height = 22;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.netImportant = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 90000;
            projectile.Opacity = 0f;
            cooldownSlot = 1;
        }

        // Projectile spawning code is done in SCal's AI.
        public override void AI()
        {
            // Die if SCal is gone.
            if (CalamityGlobalNPC.SCal == -1)
            {
                projectile.Kill();
                return;
            }

            // Stay glued to SCal's hand.
            Vector2 handPosition = SupremeCalamitasBehaviorOverride.CalculateHandPosition();
            projectile.Center = handPosition;

            // Fade in. While this happens the projectile emits large amounts of flames.
            int flameCount = (int)((1f - projectile.Opacity) * 12f);
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.05f, 0f, 1f);

            // Create the fade-in dust.
            for (int i = 0; i < flameCount; i++)
            {
                Vector2 fireSpawnPosition = projectile.Center;
                fireSpawnPosition += Vector2.UnitX.RotatedBy(projectile.rotation) * Main.rand.NextFloatDirection() * projectile.width * 0.5f;
                fireSpawnPosition += Vector2.UnitY.RotatedBy(projectile.rotation) * Main.rand.NextFloatDirection() * projectile.height * 0.5f;

                Dust fire = Dust.NewDustPerfect(fireSpawnPosition, 6);
                fire.velocity = -Vector2.UnitY.RotatedByRandom(0.44f) * Main.rand.NextFloat(2f, 4f);
                fire.scale = 1.4f;
                fire.fadeIn = 0.4f;
                fire.noGravity = true;
            }

            // Create dust the rises upward.
            if (projectile.Opacity >= 1f)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (!Main.rand.NextBool(2))
                        continue;

                    Dust fire = Dust.NewDustDirect(projectile.TopLeft, projectile.width, 10, 6);
                    fire.scale = Main.rand.NextFloat(1f, 1.8f);
                    fire.velocity = -Vector2.UnitY.RotatedByRandom(0.36f) * Main.rand.NextFloat(2f, 10f);
                    fire.noGravity = true;
                }
            }

            // Frequently sync.
            if (Main.netMode != NetmodeID.MultiplayerClient && projectile.timeLeft % 12 == 11)
            {
                projectile.netUpdate = true;
                projectile.netSpam = 0;
            }

            // Define the direction.
            projectile.spriteDirection = Main.npc[CalamityGlobalNPC.SCal].spriteDirection;

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            float glowOutwardness = MathHelper.SmoothStep(0f, 5.6f, Utils.InverseLerp(30f, ChargeupTime, Time, true));
            Texture2D bookTexture = ModContent.GetTexture(Texture);
            Rectangle frame = bookTexture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
            Vector2 drawPosition;
            Vector2 origin = frame.Size() * 0.5f;
            Color glowColor = Color.Lerp(Color.Pink, Color.Red, (float)Math.Cos(Main.GlobalTime * 5f) * 0.5f + 0.5f);
            glowColor.A = 0;

            // Draw an ominous glowing backimage of the book after a bit of time.
            for (int i = 0; i < 8; i++)
            {
                drawPosition = projectile.Center + (MathHelper.TwoPi * i / 8f + Main.GlobalTime * 4f).ToRotationVector2() * glowOutwardness - Main.screenPosition;
                Main.spriteBatch.Draw(bookTexture, drawPosition, frame, projectile.GetAlpha(glowColor), projectile.rotation, origin, projectile.scale, 0, 0);
            }

            drawPosition = projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(bookTexture, drawPosition, frame, projectile.GetAlpha(lightColor), projectile.rotation, origin, projectile.scale, 0, 0);
            return false;
        }
    }
}
