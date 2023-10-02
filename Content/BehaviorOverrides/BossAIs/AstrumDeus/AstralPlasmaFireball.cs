using CalamityMod;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class AstralPlasmaFireball : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Astral Plasma Flame");
            Main.projFrames[Projectile.type] = 6;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 105;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Handle frames and rotation.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.rotation = Projectile.velocity.ToRotation() - PiOver2;

            Lighting.AddLight(Projectile.Center, Color.Yellow.ToVector3() * Projectile.scale * 0.5f);

            // Fade in.
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor.R = (byte)(255 * Projectile.Opacity);
            lightColor.G = (byte)(255 * Projectile.Opacity);
            lightColor.B = (byte)(255 * Projectile.Opacity);
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item93, Projectile.Center);

            // Release plasma bolts.
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.ai[1] != -1f)
            {
                int totalProjectiles = 6;
                int type = ModContent.ProjectileType<AstralPlasmaSpark>();
                Vector2 spinningPoint = Main.rand.NextVector2Circular(8f, 8f);
                for (int i = 0; i < totalProjectiles; i++)
                {
                    Vector2 shootVelocity = spinningPoint.RotatedBy(TwoPi / totalProjectiles * i);
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, shootVelocity, type, (int)(Projectile.damage * 0.85), 0f, Main.myPlayer);
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
                Dust astralPlasma = Dust.NewDustDirect(Projectile.Center, 6, 6, Main.rand.NextBool(2) ? 107 : 110, 0f, 0f, 100, default, 1f);
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

                astralPlasma.color = Color.Lerp(Color.Yellow, Color.Red, Main.rand.NextFloat(0.7f));
                astralPlasma.velocity *= 0.5f;
                astralPlasma.velocity += astralPlasma.velocity.SafeNormalize(Vector2.UnitY) * dustSpeed;
                astralPlasma.scale = scale;
                astralPlasma.noGravity = true;
            }

            for (int i = 0; i < 2; i++)
                Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 50, default, 1f);

            for (int i = 0; i < 20; i++)
            {
                Dust fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 0, default, 1.5f);
                fire.noGravity = true;
                fire.velocity *= 3f;
            }
        }
    }
}
