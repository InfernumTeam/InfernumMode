using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Polterghast
{
    public class EctoplasmShot : ModProjectile
    {
        public static readonly Color[] ColorSet =
        [
            Color.Pink,
            Color.Cyan
        ];

        public bool ShouldFall => Projectile.ai[0] == 1f;

        public ref float Lifetime => ref Projectile.ai[1];

        public Color StreakBaseColor => Color.Lerp(LumUtils.MulticolorLerp(Projectile.ai[1] % 0.999f, ColorSet), Color.White, 0.2f);

        public override string Texture => "CalamityMod/Projectiles/StarProj";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ectoplasm Blast");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 7;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1200;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            
        }

        public override void AI()
        {
            // Prevent drawing offscreen.
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 250;

            if (Projectile.timeLeft < 1215f - Lifetime)
                Projectile.damage = 0;

            if (Projectile.timeLeft < 1200f - Lifetime)
                Projectile.Kill();

            // Initialize the hue.
            if (Projectile.ai[1] == 0f)
                Projectile.ai[1] = Main.rand.NextFloat();

            if (ShouldFall)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (Projectile.timeLeft > 1080f)
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 18f, 0.032f);
            }
            else
                Projectile.velocity *= 0.985f;
            Projectile.Opacity = Utils.GetLerpValue(1200f, 1180f, Projectile.timeLeft, true) * Utils.GetLerpValue(1200f - Lifetime, 1220f - Lifetime, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;

            // Emit ectoplasm dust.
            if (Main.rand.NextBool())
            {
                Color dustColor = Color.Lerp(Color.Cyan, Color.Pink, Main.rand.NextFloat());
                dustColor = Color.Lerp(dustColor, Color.White, 0.7f);

                Dust ectoplasm = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(9f, 9f) + Projectile.velocity, 267, Projectile.velocity * -2.6f + Main.rand.NextVector2Circular(0.6f, 0.6f), 0, dustColor);
                ectoplasm.scale = 0.3f;
                ectoplasm.fadeIn = Main.rand.NextFloat() * 1.2f;
                ectoplasm.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D streakTexture = TextureAssets.Projectile[Projectile.type].Value;
            for (int i = 1; i < Projectile.oldPos.Length; i += InfernumConfig.Instance.ReducedGraphicsConfig ? 2 : 1)
            {
                if (Projectile.oldPos[i - 1] == Vector2.Zero || Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completionRatio = i / (float)Projectile.oldPos.Length;
                float fade = Pow(completionRatio, 2f);
                float scale = Projectile.scale * Lerp(1.2f, 0.9f, Utils.GetLerpValue(0f, 0.24f, completionRatio, true)) * Lerp(0.9f, 0.56f, Utils.GetLerpValue(0.5f, 0.78f, completionRatio, true));
                Color drawColor = Color.HotPink * (1f - fade) * Projectile.Opacity;
                drawColor.A = 0;

                Vector2 drawPosition = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.spriteBatch.Draw(streakTexture, drawPosition, null, drawColor, Projectile.oldRot[i], streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < 3; i++)
            {
                if (targetHitbox.Intersects(Utils.CenteredRectangle(Projectile.oldPos[i] + Projectile.Size * 0.5f, Projectile.Size)))
                    return true;
            }
            return false;
        }
    }
}
