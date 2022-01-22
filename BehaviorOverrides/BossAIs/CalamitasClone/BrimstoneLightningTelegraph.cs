using CalamityMod.Events;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class BrimstoneLightningTelegraph : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Telegraph");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 64;
            projectile.hostile = false;
            projectile.friendly = false;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 45;
		}

        public override void AI()
        {
            projectile.scale = (float)Math.Sin(MathHelper.Pi * projectile.timeLeft / 45f);
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

            for (int i = 0; i < 2; i++)
            {
                Vector2 spawnPosition = projectile.Center + Vector2.UnitX * Main.rand.NextFloat(-10f, 10f);
                spawnPosition.Y -= 1600f;

                bool shouldBeBuffed = CalamityWorld.downedProvidence && !BossRushEvent.BossRushActive && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI;
                int lightningDamage = shouldBeBuffed ? 375 : 160;
                int lightning = Utilities.NewProjectileBetter(spawnPosition, Vector2.UnitY * 7f, ModContent.ProjectileType<BrimstoneLightning>(), lightningDamage, 0f);
                if (Main.projectile.IndexInRange(lightning))
                {
                    Main.projectile[lightning].ai[0] = Main.projectile[lightning].velocity.ToRotation();
                    Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                }
            }
        }
	}
}
