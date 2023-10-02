using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class EidolistElectricOrb : ModProjectile
    {
        public NPC EidolistOwner => Projectile.ai[0] >= 0f ? Main.npc[(int)Projectile.ai[0]] : null;

        public Player Target => Main.player[EidolistOwner?.target ?? Player.FindClosest(Projectile.Center, 1, 1)];

        public int LightningCount => EidolistOwner is null ? 7 : 11;

        public ref float TelegraphDirection => ref Projectile.ai[1];

        public ref float Time => ref Projectile.localAI[0];

        public const int Lifetime = 75;

        public const float LightningSpread = 0.98f;

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
            if (EidolistOwner is not null && !EidolistOwner.active)
            {
                Projectile.Kill();
                return;
            }

            // Aim at the target.
            if (Time < 25f)
            {
                TelegraphDirection = Projectile.AngleTo(Target.Center);
                if (Projectile.timeLeft % 8f == 7f)
                    Projectile.netUpdate = true;
            }

            // Determine frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

            // Hover above the Eidolist.
            if (EidolistOwner is not null)
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
                    Vector2 lightningVelocity = (Lerp(-LightningSpread, LightningSpread, i / (float)(LightningCount - 1f)) + TelegraphDirection).ToRotationVector2() * 13f;
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
                Vector2 telegraphDirection = (Lerp(-LightningSpread, LightningSpread, i / (float)(LightningCount - 1f)) + TelegraphDirection).ToRotationVector2();
                Main.spriteBatch.DrawLineBetter(Projectile.Center, Projectile.Center + telegraphDirection * 2000f, telegraphColor, 3f);
                Main.spriteBatch.DrawLineBetter(Projectile.Center, Projectile.Center + telegraphDirection * 2000f, telegraphColor with { A = 0 }, 1.2f);
            }

            Main.spriteBatch.Draw(texture, drawPosition, frame, color, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0f);
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
