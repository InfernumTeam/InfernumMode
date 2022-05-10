using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class DarkFireblast : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Fire Blast");
            Main.projFrames[projectile.type] = 6;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 50;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 90;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            if (projectile.frameCounter % 5 == 4)
                projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];
            projectile.Opacity = Utils.InverseLerp(0f, 16f, Time, true) * Utils.InverseLerp(0f, 16f, projectile.timeLeft, true);

            if (projectile.localAI[0] == 0f)
            {
                projectile.localAI[0] = 1f;
                Main.PlaySound(SoundID.Item, projectile.Center, 20);
            }
            Player closestTarget = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            float oldSpeed = projectile.velocity.Length();
            projectile.velocity = (projectile.velocity * 24f + projectile.SafeDirectionTo(closestTarget.Center) * oldSpeed) / 25f;
            projectile.velocity.Normalize();
            projectile.velocity *= oldSpeed;

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/SCalSounds/BrimstoneGigablastImpact"), projectile.Center);

            float spread = 12f * MathHelper.PiOver2 * 0.01f;
            float startAngle = projectile.velocity.ToRotation() - spread * 0.5f + MathHelper.Pi;
            float deltaAngle = spread / 20f;
            float offsetAngle;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 20; i++)
                {
                    offsetAngle = startAngle + deltaAngle * (i + i * i) / 2f + 32f * i;
                    Vector2 shootVelocity = offsetAngle.ToRotationVector2() * Main.rand.NextFloat(4f, 6f);
                    Projectile.NewProjectile(projectile.Center, shootVelocity, ModContent.ProjectileType<AcceleratingDarkMagicBurst>(), projectile.damage, 0f);
                    Projectile.NewProjectile(projectile.Center, -shootVelocity, ModContent.ProjectileType<AcceleratingDarkMagicBurst>(), projectile.damage, 0f);
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1, Main.projectileTexture[projectile.type], false);
            return false;
        }
    }
}
