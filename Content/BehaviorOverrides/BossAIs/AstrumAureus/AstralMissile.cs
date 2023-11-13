using CalamityMod;
using CalamityMod.Dusts;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AstrumAureus
{
    public class AstralMissile : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy FlameTrailDrawer;

        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Astral Missile");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;

            Lighting.AddLight(Projectile.Center, 0.5f, 0.5f, 0.5f);

            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

            // Fly towards the closest player.
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(closestPlayer.Center) * Projectile.velocity.Length(), 0.033f);

            if (Projectile.WithinRange(closestPlayer.Center, 30f))
                Projectile.Kill();

            if (Time >= 45f && Projectile.velocity.Length() < 24f)
                Projectile.velocity *= 1.021f;

            Vector2 backOfMissile = Projectile.Center - (Projectile.rotation - PiOver2).ToRotationVector2() * 20f;
            Dust.NewDustDirect(backOfMissile, 5, 5, ModContent.DustType<AstralOrange>());

            Time++;
        }

        public static float FlameTrailWidthFunction(float completionRatio) => SmoothStep(21f, 8f, completionRatio);

        public static Color FlameTrailColorFunction(float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.8f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true);
            Color startingColor = Color.Lerp(Color.Cyan, Color.White, 0.4f);
            Color middleColor = Color.Lerp(Color.Orange, Color.Yellow, 0.3f);
            Color endColor = Color.Lerp(Color.Orange, Color.Red, 0.67f);
            return CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AstrumAureus/AstralMissileGlowmask").Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // Draw the base sprite and glowmask.
            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, direction, 0);
            Main.EntitySpriteDraw(glowmask, drawPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, direction, 0);
            return false;
        }

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            // Initialize the flame trail drawer.
            FlameTrailDrawer ??= new(FlameTrailWidthFunction, FlameTrailColorFunction, null, true, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);
            Vector2 trailOffset = Projectile.Size * 0.5f;
            trailOffset += (Projectile.rotation + PiOver2).ToRotationVector2() * 10f;
            Utilities.SetTexture1(InfernumTextureRegistry.StreakMagma.Value);
            FlameTrailDrawer.DrawPixelated(Projectile.oldPos, trailOffset - Main.screenPosition, 61);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Zombie103, Projectile.Center);

            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 96;
            Projectile.position -= Projectile.Size * 0.5f;

            for (int i = 0; i < 2; i++)
                Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<AstralBlue>(), 0f, 0f, 50, default, 1f);

            for (int i = 0; i < 20; i++)
            {
                Dust fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<AstralOrange>(), 0f, 0f, 0, default, 1.5f);
                fire.noGravity = true;
                fire.velocity *= 3f;
            }
            Projectile.Damage();
        }
    }
}
