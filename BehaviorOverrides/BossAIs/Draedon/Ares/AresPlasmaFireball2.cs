using CalamityMod;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
	public class AresPlasmaFireball2 : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Volatile Plasma Blast");
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
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.Opacity = 0f;
            Projectile.timeLeft = 90;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
        }

        public override void AI()
        {
            if (Projectile.ai[0] != -1f)
            {
                Vector2 targetLocation = new(Projectile.ai[0], Projectile.ai[1]);
                if (Vector2.Distance(targetLocation, Projectile.Center) < 80f)
                    Projectile.tileCollide = true;
            }

            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.35f, 0f, 1f);

            Lighting.AddLight(Projectile.Center, 0f, 0.6f * Projectile.Opacity, 0f);

            // Handle frames and rotation.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            // Create a burst of dust on the first frame.
            if (Projectile.localAI[0] == 0f)
            {
                for (int i = 0; i < 40; i++)
                {
                    Vector2 dustVelocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.35f) * Main.rand.NextFloat(1.8f, 3f);
                    int randomDustType = Main.rand.NextBool() ? 107 : 110;

                    Dust plasma = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, dustVelocity.X, dustVelocity.Y, 200, default, 1.7f);
                    plasma.position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.width);
                    plasma.noGravity = true;
                    plasma.velocity *= 3f;

                    plasma = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, dustVelocity.X, dustVelocity.Y, 100, default, 0.8f);
                    plasma.position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.width);
                    plasma.velocity *= 2f;

                    plasma.noGravity = true;
                    plasma.fadeIn = 1f;
                    plasma.color = Color.Green * 0.5f;
                }

                for (int i = 0; i < 20; i++)
                {
                    Vector2 dustVelocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.35f) * Main.rand.NextFloat(1.8f, 3f);
                    int randomDustType = Main.rand.NextBool() ? 107 : 110;

                    Dust plasma = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, randomDustType, dustVelocity.X, dustVelocity.Y, 0, default, 2f);
                    plasma.position = Projectile.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi).RotatedBy(Projectile.velocity.ToRotation()) * Projectile.width / 3f;
                    plasma.noGravity = true;
                    plasma.velocity *= 0.5f;
                }

                Projectile.localAI[0] = 1f;
            }
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity == 1f;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Projectile.Opacity != 1f)
                return;

            target.AddBuff(BuffID.OnFire, 360);
            target.AddBuff(BuffID.CursedInferno, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor.R = (byte)(255 * Projectile.Opacity);
            lightColor.G = (byte)(255 * Projectile.Opacity);
            lightColor.B = (byte)(255 * Projectile.Opacity);
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            int height = 90;
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = height;
            Projectile.Center = Projectile.position;
            Projectile.Damage();

            SoundEngine.PlaySound(SoundID.Item93, Projectile.Center);

            // Release plasma bolts and gas.
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.ai[1] != -1f)
            {
                int type = ModContent.ProjectileType<PlasmaGas>();
                for (int i = 0; i < 50; i++)
                {
                    Vector2 plasmaVelocity = Main.rand.NextVector2Circular(13f, 13f);
                    Projectile.NewProjectile(new InfernumSource(), Projectile.Center, plasmaVelocity, type, Projectile.damage, 0f, Main.myPlayer);
                }

                int totalProjectiles = 6;
                type = ModContent.ProjectileType<AresPlasmaBolt>();
                Vector2 spinningPoint = Main.rand.NextVector2Circular(0.5f, 0.5f);
                for (int i = 0; i < totalProjectiles; i++)
                {
                    Vector2 shootVelocity = spinningPoint.RotatedBy(MathHelper.TwoPi / totalProjectiles * i);
                    Projectile.NewProjectile(new InfernumSource(), Projectile.Center, shootVelocity, type, (int)(Projectile.damage * 0.9), 0f, Main.myPlayer);
                }
            }
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
