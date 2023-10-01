using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class EidolistElectricOrb2 : ModProjectile
    {
        public NPC EidolistOwner => Main.npc[(int)Projectile.ai[0]];

        public Player Target => Main.player[EidolistOwner.target];

        public ref float Time => ref Projectile.localAI[0];

        public ref float LightningCount => ref Projectile.ai[1];

        public const int Lifetime = 65;

        public override string Texture => "InfernumMode/Content/BehaviorOverrides/AbyssAIs/EidolistElectricOrb";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Electric Orb");
            Main.projFrames[Type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 94;
            Projectile.height = 94;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            // Fade in.
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.067f, 0f, 1f);

            // Disappear if there is no eidolist.
            if (!EidolistOwner.active)
            {
                Projectile.Kill();
                return;
            }

            // Determine frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

            // Hover above the Eidolist.
            Projectile.Center = EidolistOwner.Top - Vector2.UnitY * 75f;

            Time++;
        }

        public override bool? CanDamage() => Projectile.velocity == Vector2.Zero ? false : null;

        public override void OnKill(int timeLeft)
        {
            // Play a lightning burst sound.
            SoundEngine.PlaySound(SoundID.Item122, Projectile.Center);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int lightningID = ModContent.ProjectileType<EidolistLightning>();
                for (int i = 0; i < LightningCount; i++)
                {
                    Vector2 lightningVelocity = (TwoPi * i / LightningCount).ToRotationVector2() * 13f;
                    int lightning = Utilities.NewProjectileBetter(Projectile.Center, lightningVelocity, lightningID, 175, 0f);
                    if (Main.projectile.IndexInRange(lightning))
                    {
                        Main.projectile[lightning].ai[0] = lightningVelocity.ToRotation();
                        Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Color color = Projectile.GetAlpha(lightColor);

            // Draw telegraph lines.
            Color telegraphColor = Color.Cyan * Utils.GetLerpValue(16f, 25f, Time, true);
            for (int i = 0; i < LightningCount; i++)
            {
                Vector2 telegraphDirection = (TwoPi * i / LightningCount).ToRotationVector2();
                Main.spriteBatch.DrawLineBetter(Projectile.Center, Projectile.Center + telegraphDirection * 2000f, telegraphColor, 3f);
                Main.spriteBatch.DrawLineBetter(Projectile.Center, Projectile.Center + telegraphDirection * 2000f, telegraphColor with { A = 0 }, 1.2f);
            }

            Main.spriteBatch.Draw(texture, drawPosition, frame, color, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0f);
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
