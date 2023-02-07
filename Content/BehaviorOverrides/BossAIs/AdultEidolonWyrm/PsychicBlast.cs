using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class PsychicBlast : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Psychic Blast");
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 280;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 90;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 15f, Time, true);
            Projectile.scale = Projectile.Opacity * 1.5f;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            // Decide frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

            Time++;
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.9f;

        public override bool PreDraw(ref Color lightColor)
        {
            float dissipationInterpolant = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);
            float opacity = Projectile.Opacity * dissipationInterpolant;
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            ScreenOverlaysSystem.ThingsToDrawOnTopOfBlurAdditive.Add(new(texture, drawPosition, frame, Color.Wheat * opacity, Projectile.rotation, origin, Projectile.scale, 0, 0));

            Color dissipationColor = Color.Wheat * opacity * 0.8f;
            for (int i = 0; i < 12; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * ((1f - dissipationInterpolant) * 50f + 30f);
                ScreenOverlaysSystem.ThingsToDrawOnTopOfBlurAdditive.Add(new(texture, drawPosition + drawOffset, frame, dissipationColor, Projectile.rotation, origin, Projectile.scale, 0, 0));
            }
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Utilities.CreateShockwave(Projectile.Center);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float shootOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                for (int i = 0; i < 35; i++)
                {
                    Vector2 lightBoltVelocity = (MathHelper.TwoPi * i / 35f + shootOffsetAngle).ToRotationVector2() * 3f;
                    Utilities.NewProjectileBetter(Projectile.Center, lightBoltVelocity, ModContent.ProjectileType<DivineLightBolt>(), AEWHeadBehaviorOverride.StrongerNormalShotDamage, 0f, -1, 0f, 22f);
                }
            }
        }
    }
}
