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
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            if (!NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsHead>()))
                Projectile.Kill();

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.oldPos = new Vector2[7];
                for (int i = 0; i < Projectile.oldPos.Length; i++)
                {
                    Projectile.oldPos[i] = Projectile.Center + Vector2.UnitY * MathHelper.Lerp(-3400f, 3400f, i / (float)Projectile.oldPos.Length);
                }

                // Add some randomness to the lightning for the base.
                for (int i = 0; i < 40; i++)
                {
                    int segmentIndex = Main.rand.Next(1, Projectile.oldPos.Length - 1);
                    Projectile.oldPos[segmentIndex].X += Main.rand.NextFloat(8f, 22f) * Main.rand.NextBool(2).ToDirectionInt();
                }
                Projectile.localAI[0] = 1f;
            }

            // Randomize the lightning over time
            for (int i = 0; i < 20; i++)
            {
                int segmentIndex = Main.rand.Next(1, Projectile.oldPos.Length - 1);
                Projectile.oldPos[segmentIndex].X += Main.rand.NextFloat(5f, 18f) * Main.rand.NextBool(2).ToDirectionInt();
                Projectile.oldPos[segmentIndex].X = MathHelper.Clamp(Projectile.oldPos[segmentIndex].X, Projectile.Center.X - 30f, Projectile.Center.X + 30f);
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D lightningTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/PhotovisceratorLight").Value;
            for (int i = 0; i < Projectile.oldPos.Length - 1; i++)
            {
                if (Vector2.DistanceSquared(Projectile.oldPos[i], Main.screenPosition) > Main.screenWidth * Main.screenHeight * 3f)
                    continue;
                float angleToNext = (Projectile.oldPos[i + 1] - Projectile.oldPos[i]).ToRotation();

                Vector2 drawPosition = Projectile.oldPos[i];
                while (Vector2.DistanceSquared(drawPosition, Projectile.oldPos[i + 1]) > 3f * 3f)
                {
                    drawPosition += (Projectile.oldPos[i + 1] - drawPosition).SafeNormalize(Vector2.UnitY) * 2f;
                    spriteBatch.Draw(lightningTexture, drawPosition - Main.screenPosition, null, Color.Cyan * 0.31f, angleToNext, lightningTexture.Size() * 0.5f, 0.3f, SpriteEffects.None, 0f);
                    spriteBatch.Draw(lightningTexture, drawPosition - Main.screenPosition, null, Color.White * 0.62f, angleToNext, lightningTexture.Size() * 0.5f, 0.12f, SpriteEffects.None, 0f);
                }
            }
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < Projectile.oldPos.Length - 1; i++)
            {
                float _ = 0f;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.oldPos[i], Projectile.oldPos[i + 1], 10f, ref _))
                    return true;
            }
            return false;
        }
    }
}
