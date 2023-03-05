using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class HolyPushbackWall : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy FlameDrawer { get; private set; } = null;

        public ref float Timer => ref Projectile.ai[0];

        public const float LaserDistance = 6000;

        public static int Lifetime => 660;

        public int SpearReleaseRate => 15;

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fire Wall");
        }

        public override void SetDefaults()
        {
            Projectile.width = 200;
            Projectile.height = 1000;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = false;
            Projectile.Opacity = 0;
            Projectile.scale = 0;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Rapidly fade in.
            if (Projectile.timeLeft >= Lifetime - 100)
            {
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.025f, 0f, 1f);
                Projectile.scale = MathHelper.Clamp(Projectile.scale + 0.025f, 0f, 1f);
            }

            // Fade out.
            if (Projectile.timeLeft <= 40)
            {
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity - 0.025f, 0f, 1f);
                Projectile.scale = MathHelper.Clamp(Projectile.scale - 0.025f, 0f, 1f);
            }

            // Force anyone close to it to be to the left.
            foreach (Player player in Main.player)
                if (player.active && !player.dead && player.Center.WithinRange(Projectile.Center, 6000f))
                    if (player.Center.X > Projectile.Center.X)
                        player.Center = new(Projectile.Center.X, player.Center.Y);

            if (Projectile.Opacity == 1f)
            {
                if (Timer % SpearReleaseRate == 0f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 velocity = -Vector2.UnitX * 10f;
                    Vector2 position = new(Projectile.Center.X, Projectile.Center.Y + Main.rand.NextFloat(-800f, 800f));
                    Utilities.NewProjectileBetter(position, velocity, ModContent.ProjectileType<TelegraphedProfanedSpearInfernum>(), 200, 0f, ai1: Projectile.whoAmI);
                }
            }
            Timer++;
        }

        public override bool CanHitPlayer(Player target) => Projectile.Opacity >= 0.75f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 drawPos = Projectile.Center - new Vector2(0f, LaserDistance / 2);
            Vector2 endPos = Projectile.Center + new Vector2(0f, LaserDistance / 2);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), drawPos, endPos);

        }

        public float WidthFunction(float completionRatio) => 200 * Projectile.scale;

        public Color ColorFunction(float completionRatio) => new Color(255, 191, 73) * Projectile.Opacity;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            FlameDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.GenericLaserVertexShader);

            // The gap is determined by the projectile center, and thus controlled by the attacking guardian.
            // Draw a set distance above and below the center to give a gap in the wall.
            Vector2 drawPos = Projectile.Center - new Vector2(0f, LaserDistance /2);
            Vector2 endPos = Projectile.Center + new Vector2(0f, LaserDistance / 2);
            Vector2[] topDrawPoints = new Vector2[8];
            for (int i = 0; i < topDrawPoints.Length; i++)
                topDrawPoints[i] = Vector2.Lerp(drawPos, endPos, (float)i / topDrawPoints.Length);

            InfernumEffectsRegistry.GenericLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.CrustyNoise);
            InfernumEffectsRegistry.GenericLaserVertexShader.UseColor(new Color(255, 255, 150) * Projectile.Opacity);
            InfernumEffectsRegistry.GenericLaserVertexShader.Shader.Parameters["strongerFade"].SetValue(true);

            FlameDrawer.DrawPixelated(topDrawPoints, -Main.screenPosition, 40);
        }
    }
}
