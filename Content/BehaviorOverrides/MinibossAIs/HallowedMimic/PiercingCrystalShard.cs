using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.HallowedMimic
{
    public class PiercingCrystalShard : ModProjectile
    {
        public int NPCIndex => (int)Projectile.ai[1];

        public float CrystalLength => Projectile.Distance(Main.npc[NPCIndex].Center);

        public ref float Time => ref Projectile.ai[0];

        public const int PierceTime = 42;

        public const int FadeOutTime = 54;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Crystal Shard");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = PierceTime + FadeOutTime;
            Projectile.penetrate = -1;
            Projectile.MaxUpdates = 2;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Time++;

            if (Time < PierceTime)
                Projectile.velocity *= 1.05f;
            else
            {
                Projectile.MaxUpdates = 1;
                Projectile.velocity = Vector2.Zero;
                Projectile.Opacity = Utils.GetLerpValue(0f, FadeOutTime, Projectile.timeLeft, true);
            }
            Projectile.rotation = Main.npc[NPCIndex].AngleTo(Projectile.Center) + PiOver2;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool? CanDamage() => Projectile.Opacity > 0.67f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Main.npc[NPCIndex].Center, Projectile.Center, Projectile.width, ref _);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.instance.LoadProjectile(ProjectileID.CrystalVileShardShaft);
            Texture2D crystalBeginTexture = TextureAssets.Projectile[ProjectileID.CrystalVileShardShaft].Value;
            Texture2D crystalMiddleTexture = crystalBeginTexture;
            Texture2D crystalEndTexture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle startFrameArea = crystalBeginTexture.Frame(1, Main.projFrames[Projectile.type], 0, 0);
            Rectangle middleFrameArea = crystalMiddleTexture.Frame(1, Main.projFrames[Projectile.type], 0, 0);
            Rectangle endFrameArea = crystalEndTexture.Frame(1, Main.projFrames[Projectile.type], 0, 0);

            // Start texture drawing.
            Main.spriteBatch.Draw(crystalBeginTexture,
                             Projectile.Center - Main.screenPosition,
                             startFrameArea,
                             Color.White * Projectile.Opacity,
                             Projectile.rotation,
                             crystalBeginTexture.Size() / 2f,
                             Projectile.scale,
                             SpriteEffects.None,
                             0f);

            // Prepare things for body drawing.
            float crystalBodyLength = CrystalLength + crystalBeginTexture.Height * Projectile.scale;
            Vector2 centerOncrystal = Main.npc[NPCIndex].Center;

            // Body drawing.
            if (crystalBodyLength > 0f)
            {
                float crystalOffset = middleFrameArea.Height * Projectile.scale;
                float incrementalBodyLength = 0f;
                while (incrementalBodyLength + 1f < crystalBodyLength)
                {
                    Main.spriteBatch.Draw(crystalMiddleTexture,
                                     centerOncrystal - Main.screenPosition,
                                     middleFrameArea,
                                     Color.White * Projectile.Opacity,
                                     Projectile.rotation,
                                     crystalMiddleTexture.Width * 0.5f * Vector2.UnitX,
                                     Projectile.scale,
                                     SpriteEffects.None,
                                     0f);
                    incrementalBodyLength += crystalOffset;
                    centerOncrystal += (Projectile.rotation - PiOver2).ToRotationVector2() * crystalOffset;
                }
            }

            // End texture drawing.
            Vector2 crystalEndCenter = centerOncrystal - Main.screenPosition;
            Main.spriteBatch.Draw(crystalEndTexture,
                             crystalEndCenter,
                             endFrameArea,
                             Color.White * Projectile.Opacity,
                             Projectile.rotation,
                             crystalEndTexture.Frame(1, 1, 0, 0).Top(),
                             Projectile.scale,
                             SpriteEffects.None,
                             0f);
            return false;
        }
    }
}
