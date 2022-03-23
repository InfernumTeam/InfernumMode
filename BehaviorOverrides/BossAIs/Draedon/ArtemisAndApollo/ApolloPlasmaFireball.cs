using CalamityMod;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo
{
    public class ApolloPlasmaFireball : ModProjectile
    {
        public bool ShouldExplodeDiagonally => projectile.ai[0] == 1f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Volatile Plasma Blast");
            Main.projFrames[projectile.type] = 6;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 48;
            projectile.height = 48;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.Opacity = 0f;
            projectile.timeLeft = 48;
            cooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(projectile.localAI[0]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            projectile.localAI[0] = reader.ReadSingle();
        }

        public override void AI()
        {
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.335f, 0f, 1f);

            Lighting.AddLight(projectile.Center, 0f, 0.6f * projectile.Opacity, 0f);

            projectile.frameCounter++;
            if (projectile.frameCounter > 4)
            {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= Main.projFrames[projectile.type])
                projectile.frame = 0;

            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

            // Create a burst of dust on the first frame.
            if (projectile.localAI[0] == 0f)
            {
                for (int i = 0; i < 40; i++)
                {
                    Vector2 dustVelocity = projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.35f) * Main.rand.NextFloat(1.8f, 3f);
                    int randomDustType = Main.rand.NextBool() ? 107 : 110;

                    Dust plasma = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, randomDustType, dustVelocity.X, dustVelocity.Y, 200, default, 1.7f);
                    plasma.position = projectile.Center + Main.rand.NextVector2Circular(projectile.width, projectile.width);
                    plasma.noGravity = true;
                    plasma.velocity *= 3f;

                    plasma = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, randomDustType, dustVelocity.X, dustVelocity.Y, 100, default, 0.8f);
                    plasma.position = projectile.Center + Main.rand.NextVector2Circular(projectile.width, projectile.width);
                    plasma.velocity *= 2f;

                    plasma.noGravity = true;
                    plasma.fadeIn = 1f;
                    plasma.color = Color.Green * 0.5f;
                }

                for (int i = 0; i < 20; i++)
                {
                    Vector2 dustVelocity = projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.35f) * Main.rand.NextFloat(1.8f, 3f);
                    int randomDustType = Main.rand.NextBool() ? 107 : 110;

                    Dust plasma = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, randomDustType, dustVelocity.X, dustVelocity.Y, 0, default, 2f);
                    plasma.position = projectile.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi).RotatedBy(projectile.velocity.ToRotation()) * projectile.width / 3f;
                    plasma.noGravity = true;
                    plasma.velocity *= 0.5f;
                }

                projectile.localAI[0] = 1f;
            }
        }

        public override bool CanHitPlayer(Player target) => projectile.Opacity == 1f;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (projectile.Opacity != 1f)
                return;

            target.AddBuff(BuffID.OnFire, 360);
            target.AddBuff(BuffID.CursedInferno, 180);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            lightColor.R = (byte)(255 * projectile.Opacity);
            lightColor.G = (byte)(255 * projectile.Opacity);
            lightColor.B = (byte)(255 * projectile.Opacity);
            CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor, 1);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            int height = 90;
            projectile.position = projectile.Center;
            projectile.width = projectile.height = height;
            projectile.Center = projectile.position;
            projectile.Damage();

            Main.PlaySound(SoundID.Item93, projectile.Center);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int projectileCount = 6;
                int type = ModContent.ProjectileType<AresPlasmaBolt>();
                for (int i = 0; i < projectileCount; i++)
                {
                    Vector2 shootVelocity = (MathHelper.TwoPi * i / projectileCount).ToRotationVector2() * 0.5f;
                    if (ShouldExplodeDiagonally)
                        shootVelocity = shootVelocity.RotatedBy(MathHelper.Pi / projectileCount);
                    Projectile.NewProjectile(projectile.Center, shootVelocity, type, (int)(projectile.damage * 0.8), 0f);
                }
            }

            for (int i = 0; i < 120; i++)
            {
                float dustSpeed = 16f;
                if (i < 150)
                    dustSpeed = 12f;
                if (i < 100)
                    dustSpeed = 8f;
                if (i < 50)
                    dustSpeed = 4f;

                float scale = 1f;
                Dust plasma = Dust.NewDustDirect(projectile.Center, 6, 6, Main.rand.NextBool(2) ? 107 : 110, 0f, 0f, 100, default, 1f);
                switch ((int)dustSpeed)
                {
                    case 4:
                        scale = 1.2f;
                        break;
                    case 8:
                        scale = 1.1f;
                        break;
                    case 12:
                        scale = 1f;
                        break;
                    case 16:
                        scale = 0.9f;
                        break;
                    default:
                        break;
                }

                plasma.velocity *= 0.5f;
                plasma.velocity += plasma.velocity.SafeNormalize(Vector2.UnitY) * dustSpeed;
                plasma.scale = scale;
                plasma.noGravity = true;
            }
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
