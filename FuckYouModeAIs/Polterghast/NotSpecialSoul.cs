using CalamityMod.NPCs;
using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Polterghast
{
	public class NotSpecialSoul : ModProjectile
    {
        public bool Cyan => projectile.ai[0] == 1f;
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
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
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
            projectile.Opacity = Utils.InverseLerp(200f, 175f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 25f, projectile.timeLeft, true);

            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

            projectile.frameCounter++;
            if (projectile.frameCounter % 5 == 4)
                projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];

            if (projectile.timeLeft % 18 == 17)
			{
                // Release a circle of dust every so often.
                for (int i = 0; i < 16; i++)
                {
                    Vector2 dustOffset = Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / 16f) * new Vector2(4f, 1f);
                    dustOffset = dustOffset.RotatedBy(projectile.velocity.ToRotation());

                    Dust ectoplasm = Dust.NewDustDirect(projectile.Center, 0, 0, 175, 0f, 0f);
                    ectoplasm.position = projectile.Center + dustOffset;
                    ectoplasm.velocity = dustOffset.SafeNormalize(Vector2.Zero) * 1.5f;
                    ectoplasm.color = Color.Lerp(Color.Purple, Color.White, 0.5f);
                    ectoplasm.scale = 1.5f;
                    ectoplasm.noGravity = true;
                }
            }

            if (projectile.timeLeft < 3)
            {
                projectile.Center = Vector2.Lerp(projectile.Center, polterghast.Center, 0.06f);
                projectile.velocity = (projectile.velocity * 11f + projectile.SafeDirectionTo(polterghast.Center) * 36f) / 12f;
                if (projectile.Hitbox.Intersects(polterghast.Hitbox))
				{
                    polterghast.ai[2]--;
                    projectile.Kill();
				}
                projectile.timeLeft = 2;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Polterghast/SoulLarge" + (Cyan ? "Cyan" : ""));
            if (projectile.whoAmI % 2 == 0)
                texture = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Polterghast/SoulMedium" + (Cyan ? "Cyan" : ""));

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
    }
}
