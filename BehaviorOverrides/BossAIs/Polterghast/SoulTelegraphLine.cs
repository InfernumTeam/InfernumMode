using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Polterghast
{
    public class SoulTelegraphLine : ModProjectile
    {
        public PrimitiveTrailCopy TelegraphDrawer = null;

        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.alpha = 0;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 24;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            Time++;
            Projectile.Opacity = CalamityUtils.Convert01To010(Projectile.timeLeft / 24f);
        }

        public static float TelegraphWidthFunction(float _) => 70f;

        public Color TelegraphColorFunction(float completionRatio)
        {
            float endFadeOpacity = Utils.GetLerpValue(0f, 0.15f, completionRatio, true) * Utils.GetLerpValue(1f, 0.8f, completionRatio, true);
            return Color.LightCyan * endFadeOpacity * Projectile.Opacity * 0.4f;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            TelegraphDrawer ??= new(TelegraphWidthFunction, TelegraphColorFunction, null, false, InfernumEffectsRegistry.SideStreakVertexShader);
            
            Vector2 telegraphStart = Projectile.Center;
            Vector2 telegraphEnd = Projectile.Center + Projectile.velocity * 5000f;
            Vector2[] telegraphPoints = new Vector2[]
            {
                telegraphStart,
                (telegraphStart + telegraphEnd) * 0.5f,
                telegraphEnd
            };
            TelegraphDrawer.Draw(telegraphPoints, -Main.screenPosition, 72);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector2 soulSpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(120f, 120f);
                    Vector2 soulVelocity = Projectile.velocity * 24.5f;
                    Utilities.NewProjectileBetter(soulSpawnPosition, soulVelocity, ModContent.ProjectileType<NonReturningSoul>(), 300, 0f);
                }
            }
        }
    }
}
