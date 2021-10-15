using InfernumMode.BehaviorOverrides.BossAIs.Twins;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Dragonfolly
{
	public class LightningCloud : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lightning");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 64;
            projectile.hostile = false;
            projectile.friendly = false;
            projectile.tileCollide = true;
            projectile.timeLeft = 45;
		}

        public override void AI()
        {
            projectile.scale = (float)Math.Sin(MathHelper.Pi * projectile.timeLeft / 45f);
            for (int i = 0; i < 16; i++)
			{
                Dust redLightning = Dust.NewDustPerfect(projectile.Center, 60, Main.rand.NextVector2Circular(3f, 3f));
                redLightning.velocity *= Main.rand.NextFloat(1f, 1.9f);
                redLightning.scale *= Main.rand.NextFloat(1.85f, 2.25f);
                redLightning.color = Color.Lerp(Color.White, Color.Red, Main.rand.NextFloat(0.5f, 1f));
                redLightning.fadeIn = 1f;
                redLightning.noGravity = true;
            }
		}

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.DrawLineBetter(projectile.Center - Vector2.UnitY * 4000f, projectile.Center + Vector2.UnitY * 4000f, Color.Red, projectile.scale * 4f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastImpact"), projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < Main.rand.Next(2, 5 + 1); i++)
            {
                Vector2 spawnPosition = projectile.Center + Vector2.UnitX * Main.rand.NextFloat(-10f, 10f);
                spawnPosition.Y -= 1600f;

                int lightning = Utilities.NewProjectileBetter(spawnPosition, Vector2.UnitY * 7f, ModContent.ProjectileType<RedLightning>(), 260, 0f);
                if (Main.projectile.IndexInRange(lightning))
                {
                    Main.projectile[lightning].ai[0] = Main.projectile[lightning].velocity.ToRotation();
                    Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                }
            }
        }
	}
}
