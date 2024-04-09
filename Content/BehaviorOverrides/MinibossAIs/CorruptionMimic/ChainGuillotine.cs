using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.CorruptionMimic
{
    public class ChainGuillotine : ModProjectile
    {
        public int NPCIndex => (int)Projectile.ai[1];

        public float ChainLength => Projectile.Distance(Main.npc[NPCIndex].Center);

        public ref float Time => ref Projectile.ai[0];

        public const int PierceTime = 42;

        // This does not account for extra updates.
        public const int ReturnTime = 45;

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Chain Guillotine");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = PierceTime + ReturnTime;
            Projectile.penetrate = -1;
            Projectile.MaxUpdates = 2;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Time++;

            if (Time < PierceTime)
                Projectile.velocity *= 1.03f;
            else
            {
                Projectile.MaxUpdates = 3;
                Projectile.velocity = Vector2.Zero;
                Projectile.Center = Vector2.Lerp(Projectile.Center, Main.npc[NPCIndex].Center, 0.04f).MoveTowards(Main.npc[NPCIndex].Center, 5f);
            }
            Projectile.rotation = Main.npc[NPCIndex].AngleTo(Projectile.Center) + PiOver2;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Main.npc[NPCIndex].Center, Projectile.Center, Projectile.width, ref _);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D chainBeginTexture = TextureAssets.Chain40.Value;
            Texture2D chainMiddleTexture = chainBeginTexture;
            Texture2D chainEndTexture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle startFrameArea = chainBeginTexture.Frame(1, Main.projFrames[Projectile.type], 0, 0);
            Rectangle middleFrameArea = chainMiddleTexture.Frame(1, Main.projFrames[Projectile.type], 0, 0);
            Rectangle endFrameArea = chainEndTexture.Frame(1, Main.projFrames[Projectile.type], 0, 0);

            // Start texture drawing.
            Main.spriteBatch.Draw(chainBeginTexture,
                             Projectile.Center - Main.screenPosition,
                             startFrameArea,
                             Color.White,
                             Projectile.rotation,
                             chainBeginTexture.Size() / 2f,
                             Projectile.scale,
                             SpriteEffects.None,
                             0f);

            // Prepare things for body drawing.
            float chainBodyLength = ChainLength;
            Vector2 centerOnchain = Main.npc[NPCIndex].Center;

            // Body drawing.
            if (chainBodyLength > 0f)
            {
                float chainOffset = middleFrameArea.Height * Projectile.scale;
                float incrementalBodyLength = 0f;
                while (incrementalBodyLength + 1f < chainBodyLength)
                {
                    Main.spriteBatch.Draw(chainMiddleTexture,
                                     centerOnchain - Main.screenPosition,
                                     middleFrameArea,
                                     Color.White,
                                     Projectile.rotation,
                                     chainMiddleTexture.Width * 0.5f * Vector2.UnitX,
                                     Projectile.scale,
                                     SpriteEffects.None,
                                     0f);
                    incrementalBodyLength += chainOffset;
                    centerOnchain += (Projectile.rotation - PiOver2).ToRotationVector2() * chainOffset;
                }
            }

            // End texture drawing.
            Vector2 chainEndCenter = centerOnchain - Main.screenPosition;
            Main.spriteBatch.Draw(chainEndTexture,
                             chainEndCenter,
                             endFrameArea,
                             Color.White,
                             Projectile.rotation,
                             chainEndTexture.Frame(1, 1, 0, 0).Top(),
                             Projectile.scale,
                             SpriteEffects.None,
                             0f);
            return false;
        }
    }
}
