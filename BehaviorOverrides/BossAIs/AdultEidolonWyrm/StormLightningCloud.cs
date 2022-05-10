using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class StormLightningCloud : ModProjectile
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
            projectile.timeLeft = 20;
        }

        public override void AI()
        {
            projectile.scale = (float)Math.Sin(MathHelper.Pi * projectile.timeLeft / 20f);
            for (int i = 0; i < 4; i++)
            {
                Dust redLightning = Dust.NewDustPerfect(projectile.Center, 267, Main.rand.NextVector2Circular(3f, 3f));
                redLightning.velocity *= Main.rand.NextFloat(1f, 1.9f);
                redLightning.scale *= Main.rand.NextFloat(1.85f, 2.25f);
                redLightning.color = Color.Lerp(Color.White, Color.Cyan, Main.rand.NextFloat(0.2f, 1f));
                redLightning.fadeIn = 1f;
                redLightning.noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.DrawLineBetter(projectile.Center - Vector2.UnitY * 4000f, projectile.Center + Vector2.UnitY * 4000f, Color.DeepSkyBlue, projectile.scale * 4f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastImpact"), projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < Main.rand.Next(2, 4 + 1); i++)
            {
                Vector2 spawnPosition = projectile.Center + Vector2.UnitX * Main.rand.NextFloat(-10f, 10f);
                float direction = spawnPosition.Y < 3300f ? 1f : -1f;
                spawnPosition.Y += direction * 3200f;

                int lightning = Utilities.NewProjectileBetter(spawnPosition, Vector2.UnitY * -direction * 20f, ModContent.ProjectileType<StormLightning>(), 640, 0f);
                if (Main.projectile.IndexInRange(lightning))
                {
                    Main.projectile[lightning].ai[0] = Main.projectile[lightning].velocity.ToRotation();
                    Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                }
            }
        }
    }
}
