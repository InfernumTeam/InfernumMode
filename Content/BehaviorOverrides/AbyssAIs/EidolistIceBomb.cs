using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class EidolistIceBomb : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults() => DisplayName.SetDefault("Abyssal Ice");

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
        }

        public override void AI()
        {
            // Decelerate.
            Projectile.velocity *= 0.98f;

            // Fade in.
            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Time, true);
            
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0));
            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);

            // Release a bit of ice dust.
            for (int i = 0; i < 10; i++)
            {
                Dust ice = Dust.NewDustDirect(Projectile.TopLeft, Projectile.width, Projectile.height, DustID.Ice);
                ice.velocity = Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 2f;
                ice.noGravity = Main.rand.NextBool(3);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int splitCount = 6;
            int iceID = ModContent.ProjectileType<EidolistIce>();
            float shootOffsetAngle = Main.rand.NextBool().ToInt() * MathHelper.Pi / splitCount;
            for (int i = 0; i < splitCount; i++)
            {
                Vector2 icicleShootVelocity = (MathHelper.TwoPi * i / splitCount + shootOffsetAngle).ToRotationVector2() * 8f;
                Utilities.NewProjectileBetter(Projectile.Center, icicleShootVelocity, iceID, 160, 0f);
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
    }
}
