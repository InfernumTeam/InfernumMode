using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.Betsy
{
    public class MoltenMeteor : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Meteor");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 30;
            projectile.height = 30;
            projectile.tileCollide = false;
            projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.ignoreWater = true;
            projectile.timeLeft = 210;
        }

        public override void AI()
        {
            if (projectile.localAI[0] == 0f)
            {
                projectile.ai[0] = Main.rand.Next(3);
                projectile.ai[1] = Main.rand.NextFloat(0.9f, 1.1f);
                projectile.localAI[0] = 1f;
                projectile.netUpdate = true;
            }

            projectile.scale = projectile.ai[1];
            projectile.rotation += projectile.velocity.X * 0.045f;
            projectile.velocity *= 0.987f;

            if (projectile.velocity.Length() > 5f)
            {
                Vector2 position = projectile.Center + Vector2.Normalize(projectile.velocity) * 10f;
                Dust fire = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 244, 0f, 0f, 0, new Color(255, 127, 0), 1f);
                fire.position = position;
                fire.velocity = projectile.velocity.RotatedBy(MathHelper.PiOver2) * 0.33f + projectile.velocity / 4f;
                fire.position += projectile.velocity.RotatedBy(MathHelper.PiOver2) + Main.rand.NextVector2Circular(8f, 8f);
                fire.fadeIn = 0.5f;
                fire.noGravity = true;

                fire = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 244, 0f, 0f, 0, new Color(255, 127, 0), 1f);
                fire.position = position;
                fire.velocity = projectile.velocity.RotatedBy(-MathHelper.PiOver2) * 0.33f + projectile.velocity / 4f;
                fire.position += projectile.velocity.RotatedBy(-MathHelper.PiOver2) + Main.rand.NextVector2Circular(8f, 8f);
                fire.fadeIn = 0.5f;
                fire.noGravity = true;

                fire = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 244, 0f, 0f, 0, new Color(255, Main.DiscoG, 0), 1f);
                fire.velocity *= 0.5f;
                fire.scale *= 1.3f;
                fire.fadeIn = 1f;
                fire.noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.GetTexture("CalamityMod/Projectiles/Magic/AsteroidMolten");
            Texture2D glowmask = ModContent.GetTexture("CalamityMod/Projectiles/Magic/AsteroidMoltenGlow");
            switch ((int)projectile.ai[0])
            {
                case 0:
                    break;
                case 1:
                    texture = ModContent.GetTexture("CalamityMod/Projectiles/Magic/AsteroidMolten2");
                    glowmask = ModContent.GetTexture("CalamityMod/Projectiles/Magic/AsteroidMoltenGlow2");
                    break;
                case 2:
                    texture = ModContent.GetTexture("CalamityMod/Projectiles/Magic/AsteroidMolten3");
                    glowmask = ModContent.GetTexture("CalamityMod/Projectiles/Magic/AsteroidMoltenGlow3");
                    break;
                case 3:
                    texture = ModContent.GetTexture("CalamityMod/Projectiles/Magic/AsteroidMolten4");
                    glowmask = ModContent.GetTexture("CalamityMod/Projectiles/Magic/AsteroidMoltenGlow4");
                    break;
                case 4:
                    texture = ModContent.GetTexture("CalamityMod/Projectiles/Magic/AsteroidMolten5");
                    glowmask = null;
                    break;
                case 5:
                    texture = ModContent.GetTexture("CalamityMod/Projectiles/Magic/AsteroidMolten6");
                    glowmask = ModContent.GetTexture("CalamityMod/Projectiles/Magic/AsteroidMoltenGlow6");
                    break;
                default:
                    break;
            }
            Vector2 origin = texture.Size() / 2f;
            CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor, 1, texture);

            if (glowmask != null)
                spriteBatch.Draw(glowmask, projectile.Center - Main.screenPosition, null, Color.White, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.DD2_BetsyFireballImpact, projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 2; i++)
            {
                Vector2 cinderVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(10.5f, 14f);
                Utilities.NewProjectileBetter(projectile.Center, cinderVelocity, ModContent.ProjectileType<MeteorCinder>(), 170, 0f);
            }
        }
    }
}
