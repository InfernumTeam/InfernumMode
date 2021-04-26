using CalamityMod.NPCs;
using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Polterghast
{
	public class PairedSoul : ModProjectile
    {
        public Projectile Twin => Main.projectile[(int)projectile.ai[1]];
        public Player Target => Main.player[projectile.owner];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Soul");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
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
            projectile.Opacity = Utils.InverseLerp(200f, 185f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 15f, projectile.timeLeft, true);

            if (!projectile.WithinRange(Twin.Center, 35f))
                projectile.velocity = (projectile.velocity * 39f + projectile.SafeDirectionTo(Twin.Center) * 14f) / 40f;

            if (projectile.timeLeft < 3)
            {
                projectile.velocity = (projectile.velocity * 11f + projectile.SafeDirectionTo(polterghast.Center) * 29f) / 12f;
                if (projectile.Hitbox.Intersects(polterghast.Hitbox))
                {
                    polterghast.ai[2]--;
                    projectile.Kill();
                }
                projectile.timeLeft = 2;
            }
            else
                projectile.velocity = projectile.velocity.RotateTowards(projectile.AngleTo(Target.Center), 0.011f);
            projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * 14f;
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

            projectile.frameCounter++;
            if (projectile.frameCounter % 5 == 4)
                projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Polterghast/SoulLarge");
            if (projectile.whoAmI % 2 == 0)
                texture = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Polterghast/SoulLargeCyan");

            CalamityGlobalProjectile.DrawCenteredAndAfterimage(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 2, texture);
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
