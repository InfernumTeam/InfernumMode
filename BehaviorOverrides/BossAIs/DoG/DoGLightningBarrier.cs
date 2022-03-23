using CalamityMod;
using CalamityMod.NPCs.DevourerofGods;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class DoGLightningBarrier : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lightning Barrier");
        }

        public override void SetDefaults()
        {
            projectile.width = 80;
            projectile.height = 80;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.alpha = 255;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            if (!NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsHead>()))
                projectile.Kill();

            if (projectile.localAI[0] == 0f)
            {
                projectile.oldPos = new Vector2[7];
                for (int i = 0; i < projectile.oldPos.Length; i++)
                {
                    projectile.oldPos[i] = projectile.Center + Vector2.UnitY * MathHelper.Lerp(-3400f, 3400f, i / (float)projectile.oldPos.Length);
                }

                // Add some randomness to the lightning for the base.
                for (int i = 0; i < 40; i++)
                {
                    int segmentIndex = Main.rand.Next(1, projectile.oldPos.Length - 1);
                    projectile.oldPos[segmentIndex].X += Main.rand.NextFloat(8f, 22f) * Main.rand.NextBool(2).ToDirectionInt();
                }
                projectile.localAI[0] = 1f;
            }

            // Randomize the lightning over time
            for (int i = 0; i < 20; i++)
            {
                int segmentIndex = Main.rand.Next(1, projectile.oldPos.Length - 1);
                projectile.oldPos[segmentIndex].X += Main.rand.NextFloat(5f, 18f) * Main.rand.NextBool(2).ToDirectionInt();
                projectile.oldPos[segmentIndex].X = MathHelper.Clamp(projectile.oldPos[segmentIndex].X, projectile.Center.X - 30f, projectile.Center.X + 30f);
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D lightningTexture = ModContent.GetTexture("CalamityMod/ExtraTextures/PhotovisceratorLight");
            for (int i = 0; i < projectile.oldPos.Length - 1; i++)
            {
                if (Vector2.DistanceSquared(projectile.oldPos[i], Main.screenPosition) > Main.screenWidth * Main.screenHeight * 3f)
                    continue;
                float angleToNext = (projectile.oldPos[i + 1] - projectile.oldPos[i]).ToRotation();

                Vector2 drawPosition = projectile.oldPos[i];
                while (Vector2.DistanceSquared(drawPosition, projectile.oldPos[i + 1]) > 3f * 3f)
                {
                    drawPosition += (projectile.oldPos[i + 1] - drawPosition).SafeNormalize(Vector2.UnitY) * 2f;
                    spriteBatch.Draw(lightningTexture, drawPosition - Main.screenPosition, null, Color.Cyan * 0.31f, angleToNext, lightningTexture.Size() * 0.5f, 0.3f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(lightningTexture, drawPosition - Main.screenPosition, null, Color.White * 0.62f, angleToNext, lightningTexture.Size() * 0.5f, 0.12f, SpriteEffects.None, 0f);
                }
            }
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < projectile.oldPos.Length - 1; i++)
            {
                float _ = 0f;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), projectile.oldPos[i], projectile.oldPos[i + 1], 10f, ref _))
                    return true;
            }
            return false;
        }
    }
}
