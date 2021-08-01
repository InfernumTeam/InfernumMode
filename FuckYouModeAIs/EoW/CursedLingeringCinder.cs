using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.EoW
{
	public class CursedLingeringCinder : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cursed Cinder");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.hostile = true;
			projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 300;
		}

        public override void AI()
        {
            if (projectile.velocity != Vector2.Zero)
            {
                projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
                if (projectile.velocity.Y < 14f)
                    projectile.velocity.Y += 0.3f;
            }
            else
            {
                while (!Collision.SolidCollision(projectile.Center, 4, 4))
                {
                    projectile.position.Y++;
                    if (!WorldGen.InWorld((int)projectile.Center.X / 16, (int)projectile.Center.Y / 16, 8))
                    {
                        projectile.Kill();
                        break;
                    }
                }
            }

            Dust cursedFlame = Dust.NewDustPerfect(projectile.Center, 267);
            cursedFlame.color = Color.LightGreen;
            cursedFlame.velocity = (projectile.rotation - MathHelper.PiOver2).ToRotationVector2().RotatedByRandom(0.3f) * Main.rand.NextFloat(2.4f, 3.5f);
            cursedFlame.scale = Main.rand.NextFloat(0.85f, 1.1f);
            cursedFlame.noGravity = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (projectile.velocity != Vector2.Zero)
            {
                projectile.velocity = Vector2.Zero;
                projectile.netUpdate = true;
            }    
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.AddBuff(BuffID.CursedInferno, 120);

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, Color.White, ProjectileID.Sets.TrailingMode[projectile.type], 2);
            return false;
        }
    }
}
