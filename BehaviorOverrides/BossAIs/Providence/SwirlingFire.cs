using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class SwirlingFire : ModProjectile
    {
        public ref float AngularTurnSpeed => ref projectile.ai[0];
        public ref float Time => ref projectile.ai[1];

        public float MaxScale = 0f;
        public const int FadeinTime = 180;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fire");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 10;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 255;
            projectile.scale = 0.04f;
            projectile.penetrate = -1;
            projectile.timeLeft = 1200;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            Time++;
            if (MaxScale == 0f)
                MaxScale = Main.rand.NextFloat(0.8f, 1.25f);

            if (Time >= FadeinTime - 45)
                projectile.velocity *= 0.94f;
            if (Time >= FadeinTime)
            {
                // Fizzle out when close to death. 
                if (!Main.dedServ && projectile.timeLeft < 60)
                {
                    for (int i = 1; i <= 1; i += 2)
                    {
                        Dust fire = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(3f, 3f), !Main.dayTime ? 245 : DustID.Fire);
                        fire.velocity = Main.rand.NextVector2Circular(3f, 3f);
                        fire.scale = Main.rand.NextFloat(1.3f, 1.45f);
                        fire.noGravity = true;
                    }
                    projectile.scale *= 0.95f;
                    projectile.width = projectile.height = (int)(40 * projectile.scale);
                }
                else if (projectile.timeLeft >= 60)
                {
                    projectile.velocity = Vector2.Zero;
                    projectile.scale = MathHelper.Lerp(projectile.scale, MaxScale, 0.13f);
                    projectile.Opacity = MathHelper.Lerp(projectile.Opacity, 1f, 0.25f);

                    projectile.frameCounter++;
                    projectile.frame = projectile.frameCounter / 5 % Main.projFrames[projectile.type];

                    if (projectile.scale < MaxScale)
                        projectile.width = projectile.height = (int)(40 * projectile.scale);
                }

                Lighting.AddLight(projectile.Center, Color.White.ToVector3());
            }

            // Release a bunch of fiery dust from the cinder before it burns.
            else
            {
                if (!Main.dedServ)
                {
                    for (int i = 1; i <= 1; i += 2)
                    {
                        Vector2 fireVelocity = (Time / 6f).ToRotationVector2().RotatedBy(i * MathHelper.PiOver2) * Main.rand.NextFloat(1.7f, 2.2f);
                        Dust fire = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(3f, 3f), !Main.dayTime ? 245 : DustID.Fire);
                        fire.velocity = fireVelocity;
                        fire.scale = Main.rand.NextFloat(1.3f, 1.45f);
                        fire.noGravity = true;
                    }
                }
                projectile.velocity = projectile.velocity.RotatedBy(AngularTurnSpeed);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            if (!Main.dayTime)
                texture = ModContent.GetTexture($"{Texture}Night");

            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            spriteBatch.Draw(texture, drawPosition, frame, projectile.GetAlpha(lightColor * 1.3f), projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool CanDamage() => Time >= FadeinTime + 30f;

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
