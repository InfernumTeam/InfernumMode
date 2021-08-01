using CalamityMod.NPCs;
using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Polterghast
{
	public class WavySoul : ModProjectile
    {
        public float Time => 200f - projectile.timeLeft;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Soul");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 32;
            projectile.hostile = true;
            projectile.friendly = false;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 200;
		}

        public override void AI()
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.ghostBoss))
            {
                projectile.Kill();
                return;
            }

            NPC polterghast = Main.npc[CalamityGlobalNPC.ghostBoss];
            if (projectile.timeLeft < 9)
            {
                projectile.velocity = (projectile.velocity * 11f + projectile.SafeDirectionTo(polterghast.Center) * 29f) / 12f;
                if (projectile.Hitbox.Intersects(polterghast.Hitbox))
                {
                    polterghast.ai[2]--;
                    projectile.Kill();
                }
                projectile.timeLeft = 8;
            }
            else if (Time < 100f)
            {
                float movementOffset = (float)Math.Sin(Time / 24f) * 0.02f;
                projectile.velocity = projectile.velocity.RotatedBy(movementOffset);
            }

            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
            projectile.Opacity = Utils.InverseLerp(0f, 35f, Time, true) * Utils.InverseLerp(0f, 35f, projectile.timeLeft, true);

            projectile.frameCounter++;
            if (projectile.frameCounter % 5 == 4)
                projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Polterghast/SoulMediumCyan");
            if (projectile.whoAmI % 2 == 0)
                texture = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Polterghast/SoulLargeCyan");

            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 2, texture);
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color color = Color.White;
            color.A = 0;
            return color * projectile.Opacity;
        }

        public override bool CanDamage() => projectile.Opacity >= 1f;

        public override void Kill(int timeLeft)
        {
            projectile.position = projectile.Center;
            projectile.width = projectile.height = 64;
            projectile.position.X = projectile.position.X - projectile.width / 2;
            projectile.position.Y = projectile.position.Y - projectile.height / 2;
            projectile.maxPenetrate = -1;
            projectile.Damage();
        }
    }
}
