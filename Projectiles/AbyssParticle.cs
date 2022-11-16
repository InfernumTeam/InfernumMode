using CalamityMod.DataStructures;
using InfernumMode.BaseEntities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Projectiles
{
    public class AbyssParticle : BaseCinderProjectile, IAdditiveDrawer
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Abyssal Particle");
            Main.projFrames[Type] = 3;
            ProjectileID.Sets.CanDistortWater[Type] = false;
        }

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 90;
            Projectile.minion = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 240;
            Projectile.scale = 0.2f;
            Projectile.hide = true;
        }

        public override void AI()
        {
            // Decide a frame to use.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
                Projectile.localAI[0] = 1f;
            }

            // Fade in and out.
            Projectile.Opacity = Utils.GetLerpValue(0f, 60f, Projectile.timeLeft, true) * Utils.GetLerpValue(240f, 200f, Projectile.timeLeft, true) * 0.36f;
            Projectile.scale = Projectile.Opacity * 0.24f;

            // Rotate.
            Projectile.velocity = Projectile.velocity.RotatedBy(0.003f) * 0.99f;
            Projectile.rotation += Projectile.velocity.X * 0.02f;

            // Fade away more quickly if inside of walls.
            if (Collision.SolidCollision(Projectile.Center, 1, 1))
                Projectile.timeLeft -= 5;
        }

        public void AdditiveDraw(SpriteBatch spritebatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0);
        }
    }
}
