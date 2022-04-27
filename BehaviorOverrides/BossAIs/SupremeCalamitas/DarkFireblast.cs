using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
	public class DarkFireblast : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Fire Blast");
            Main.projFrames[Projectile.type] = 6;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 50;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5 == 4)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            Projectile.Opacity = Utils.GetLerpValue(0f, 16f, Time, true) * Utils.GetLerpValue(0f, 16f, Projectile.timeLeft, true);

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                SoundEngine.PlaySound(SoundID.Item, Projectile.Center, 20);
            }
            Player closestTarget = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            float oldSpeed = Projectile.velocity.Length();
            Projectile.velocity = (Projectile.velocity * 24f + Projectile.SafeDirectionTo(closestTarget.Center) * oldSpeed) / 25f;
            Projectile.velocity.Normalize();
            Projectile.velocity *= oldSpeed;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/SCalSounds/BrimstoneGigablastImpact"), Projectile.Center);

            float spread = 12f * MathHelper.PiOver2 * 0.01f;
            float startAngle = Projectile.velocity.ToRotation() - spread * 0.5f + MathHelper.Pi;
            float deltaAngle = spread / 20f;
            float offsetAngle;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 20; i++)
                {
                    offsetAngle = startAngle + deltaAngle * (i + i * i) / 2f + 32f * i;
                    Vector2 shootVelocity = offsetAngle.ToRotationVector2() * Main.rand.NextFloat(4f, 6f);
                    Projectile.NewProjectile(new InfernumSource(), Projectile.Center, shootVelocity, ModContent.ProjectileType<AcceleratingDarkMagicBurst>(), Projectile.damage, 0f);
                    Projectile.NewProjectile(new InfernumSource(), Projectile.Center, -shootVelocity, ModContent.ProjectileType<AcceleratingDarkMagicBurst>(), Projectile.damage, 0f);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1, Utilities.ProjTexture(Projectile.type), false);
            return false;
        }
    }
}
