using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class HeresyProjSCal : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public const int ChargeupTime = 135;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Heresy");
            Main.projFrames[Projectile.type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90000;
            Projectile.Opacity = 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        // Projectile spawning code is done in SCal's AI.
        public override void AI()
        {
            // Die if SCal is gone.
            if (CalamityGlobalNPC.SCal == -1 || !Main.npc[CalamityGlobalNPC.SCal].active)
            {
                Projectile.Kill();
                return;
            }

            // Stay glued to SCal's hand.
            Vector2 handPosition = SupremeCalamitasBehaviorOverride.CalculateHandPosition();
            Projectile.Center = handPosition;

            // Fade in. While this happens the projectile emits large amounts of flames.
            int flameCount = (int)((1f - Projectile.Opacity) * 12f);
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.05f, 0f, 1f);

            // Create the fade-in dust.
            for (int i = 0; i < flameCount; i++)
            {
                Vector2 fireSpawnPosition = Projectile.Center;
                fireSpawnPosition += Vector2.UnitX.RotatedBy(Projectile.rotation) * Main.rand.NextFloatDirection() * Projectile.width * 0.5f;
                fireSpawnPosition += Vector2.UnitY.RotatedBy(Projectile.rotation) * Main.rand.NextFloatDirection() * Projectile.height * 0.5f;

                Dust fire = Dust.NewDustPerfect(fireSpawnPosition, 6);
                fire.velocity = -Vector2.UnitY.RotatedByRandom(0.44f) * Main.rand.NextFloat(2f, 4f);
                fire.scale = 1.4f;
                fire.fadeIn = 0.4f;
                fire.noGravity = true;
            }

            // Create dust the rises upward.
            if (Projectile.Opacity >= 1f)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (!Main.rand.NextBool(2))
                        continue;

                    Dust fire = Dust.NewDustDirect(Projectile.TopLeft, Projectile.width, 10, DustID.Torch);
                    fire.scale = Main.rand.NextFloat(1f, 1.8f);
                    fire.velocity = -Vector2.UnitY.RotatedByRandom(0.36f) * Main.rand.NextFloat(2f, 10f);
                    fire.noGravity = true;
                }
            }

            // Frequently sync.
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.timeLeft % 12 == 11)
            {
                Projectile.netUpdate = true;
                Projectile.netSpam = 0;
            }

            // Define the direction.
            Projectile.spriteDirection = Main.npc[CalamityGlobalNPC.SCal].spriteDirection;

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float glowOutwardness = SmoothStep(0f, 5.6f, Utils.GetLerpValue(30f, ChargeupTime, Time, true));
            Texture2D bookTexture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = bookTexture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 drawPosition;
            Vector2 origin = frame.Size() * 0.5f;
            Color glowColor = Color.Lerp(Color.Pink, Color.Red, Cos(Main.GlobalTimeWrappedHourly * 5f) * 0.5f + 0.5f);
            glowColor.A = 0;

            // Draw an ominous glowing backimage of the book after a bit of time.
            for (int i = 0; i < 8; i++)
            {
                drawPosition = Projectile.Center + (TwoPi * i / 8f + Main.GlobalTimeWrappedHourly * 4f).ToRotationVector2() * glowOutwardness - Main.screenPosition;
                Main.spriteBatch.Draw(bookTexture, drawPosition, frame, Projectile.GetAlpha(glowColor), Projectile.rotation, origin, Projectile.scale, 0, 0);
            }

            drawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(bookTexture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, 0, 0);
            return false;
        }
    }
}
