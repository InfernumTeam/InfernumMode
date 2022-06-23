using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.Projectiles.Boss;
using InfernumMode.BehaviorOverrides.BossAIs.Twins;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Dragonfolly
{
    public class LightningCloud2 : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lightning");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 64;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 60;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(PlasmaGrenade.ExplosionSound, Projectile.Center);
                Projectile.localAI[0] = 1f;
            }
            for (int i = 0; i < 16; i++)
            {
                Dust redLightning = Dust.NewDustPerfect(Projectile.Center, 60, Main.rand.NextVector2Circular(3f, 3f));
                redLightning.velocity *= Main.rand.NextFloat(1f, 1.9f);
                redLightning.scale *= Main.rand.NextFloat(1.85f, 2.25f);
                redLightning.color = Color.Lerp(Color.White, Color.Red, Main.rand.NextFloat(0.5f, 1f));
                redLightning.fadeIn = 1f;
                redLightning.noGravity = true;
            }
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color lineColor = Color.Red;
            float lineWidth = MathHelper.Lerp(0.25f, 3f, Utils.GetLerpValue(0f, 22f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 22f, Time, true));
            Utils.DrawLine(Main.spriteBatch, Projectile.Center - Vector2.UnitY * 1900f, Projectile.Center + Vector2.UnitY * 1900f, lineColor, lineColor, lineWidth);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(HolyBlast.ImpactSound, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < Main.rand.Next(2, 5 + 1); i++)
            {
                Vector2 spawnPosition = Projectile.Center + Vector2.UnitX * Main.rand.NextFloat(-7f, 7f);
                spawnPosition.Y -= 2800f;

                int lightning = Utilities.NewProjectileBetter(spawnPosition, Vector2.UnitY * 15f, ModContent.ProjectileType<RedLightning>(), 305, 0f);
                if (Main.projectile.IndexInRange(lightning))
                {
                    Main.projectile[lightning].ai[0] = Main.projectile[lightning].velocity.ToRotation();
                    Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                }
            }
        }
    }
}
