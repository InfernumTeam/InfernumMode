using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using InfernumMode.Content.Achievements;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Twins;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Rogue
{
    public class StormMaidensRetributionWorldProj : ModProjectile
    {
        public Vector2 TipOfSpear => Projectile.Center + Projectile.velocity * Projectile.width * 0.45f;

        public ref float Time => ref Projectile.ai[1];

        public ref float PinkLightningFormInterpolant => ref Projectile.localAI[0];

        public override string Texture => "InfernumMode/Content/Items/Weapons/Rogue/StormMaidensRetribution";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Storm Maiden's Retribution");
            Main.projFrames[Type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = 144;
            Projectile.height = 144;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.Opacity = 1f;
            Projectile.timeLeft = 14400;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            // Use a pink glow effect at first.
            PinkLightningFormInterpolant = Utils.GetLerpValue(45f, 0f, Time, true);

            // Create a blur at first.
            if (Projectile.localAI[1] == 0f)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidStrikeSound with { Pitch = 0.4f });
                ScreenEffectSystem.SetBlurEffect(Projectile.Center, 1f, 40);
                Projectile.localAI[1] = 1f;
            }

            // Slam downward.
            if (Projectile.velocity.Y != 0f)
            {
                Projectile.velocity.Y = Clamp(Projectile.velocity.Y + 1.5f, 0.01f, 50f);
                Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
            }

            // Update frames once ground has been reached.
            else
            {
                Projectile.frameCounter++;
                Projectile.frame = Projectile.frameCounter / 7 % Main.projFrames[Type];
            }

            // Be picked up if a player gets close enough.
            Player closest = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Projectile.WithinRange(closest.Center, 120f) && Projectile.velocity.Y == 0f)
            {
                AchievementPlayer.ExtraUpdateHandler(closest, AchievementUpdateCheck.ProjectileKill, Projectile.whoAmI);
                Projectile.Kill();
                return;
            }

            // Disappear if the player goes away.
            if (!Projectile.WithinRange(closest.Center, 3200f))
                Projectile.Kill();

            Time++;
        }

        public void DrawBackglow()
        {
            float backglowWidth = PinkLightningFormInterpolant * 2f;
            if (backglowWidth <= 0.5f)
                backglowWidth = 0f;

            Color backglowColor = Color.Lerp(Color.IndianRed, Color.White, 1f - PinkLightningFormInterpolant);
            backglowColor = Color.Lerp(backglowColor, Color.Wheat, Utils.GetLerpValue(0.7f, 1f, PinkLightningFormInterpolant, true) * 0.56f) * Lerp(1f, 0.4f, PinkLightningFormInterpolant);
            backglowColor.A = (byte)Lerp(20f, 255f, 1f - PinkLightningFormInterpolant);

            Texture2D glowmaskTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Items/Weapons/Rogue/StormMaidensRetributionSpear").Value;
            Rectangle frame = glowmaskTexture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            for (int i = 0; i < 10; i++)
            {
                Vector2 drawOffset = (TwoPi * i / 10f).ToRotationVector2() * backglowWidth;
                Main.spriteBatch.Draw(glowmaskTexture, drawPosition + drawOffset, frame, backglowColor, Projectile.rotation, origin, Projectile.scale, direction, 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color baseColor = Projectile.GetAlpha(Color.White) * (1f - PinkLightningFormInterpolant);

            Texture2D spearTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Items/Weapons/Rogue/StormMaidensRetributionGlowmask").Value;
            Rectangle frame = spearTexture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;

            DrawBackglow();
            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(spearTexture, drawPosition, frame, baseColor, Projectile.rotation, origin, Projectile.scale, direction, 0f);
            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.velocity != Vector2.Zero)
            {
                // Release lightning from above.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 lightningSpawnPosition = Projectile.Center - Vector2.UnitY * 950f;
                        Vector2 lightningShootVelocity = Vector2.UnitY * Main.rand.NextFloat(25f, 34f);
                        float aimDirection = lightningShootVelocity.ToRotation();
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), lightningSpawnPosition, lightningShootVelocity, ModContent.ProjectileType<StormMaidensLightning>(), 0, 0f, Projectile.owner, aimDirection, Main.rand.Next(100));
                    }
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Vector2.UnitY * 128f, Vector2.UnitY, ModContent.ProjectileType<LaserGroundShock>(), 0, 0f);
                }

                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceSpearHitSound with { Pitch = 0.3f, Volume = 2f }, Projectile.Center);
                Projectile.velocity = Vector2.Zero;
                Projectile.position += Vector2.UnitY * 60f;
            }
            return false;
        }
    }
}
