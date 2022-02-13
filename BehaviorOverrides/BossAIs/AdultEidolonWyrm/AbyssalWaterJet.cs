using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AbyssalWaterJet : ModProjectile
    {
        internal PrimitiveTrailCopy TornadoDrawer;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public ref float TornadoHeight => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Water Jet");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 25;
        }

        public override void SetDefaults()
        {
            projectile.width = 20;
            projectile.height = 20;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.alpha = 255;
            projectile.timeLeft = 90;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(60f, 48f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 12f, projectile.timeLeft, true);

            // Split into a bunch of water bolts.
            if (Main.netMode != NetmodeID.MultiplayerClient && projectile.timeLeft == 12f)
            {
                for (int i = 0; i < 7; i++)
                {
                    Vector2 boltShootVelocity = (MathHelper.TwoPi * i / 7f).ToRotationVector2() * 12f;
                    Projectile.NewProjectile(projectile.Center, boltShootVelocity, ModContent.ProjectileType<AbyssalWaterBolt>(), (int)(projectile.damage * 0.8f), 0f);
                }
            }

            if (projectile.velocity.Length() < 34f)
                projectile.velocity *= 1.042f;
        }

        internal Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.BlueViolet, Color.Black, 0.85f) * projectile.Opacity * 1.2f;
            return color;
        }

        internal float WidthFunction(float completionRatio)
        {
            return MathHelper.Lerp(projectile.width * 0.6f, projectile.width + 16f, 1f - completionRatio);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(),
                targetHitbox.Size(),
                projectile.oldPos[5] + projectile.Size * 0.5f,
                projectile.oldPos[projectile.oldPos.Length - 6] + projectile.Size * 0.5f,
                (int)(projectile.width * 0.525),
                ref _) && projectile.timeLeft < 72f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (TornadoDrawer is null)
                TornadoDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:DukeTornado"]);

            GameShaders.Misc["Infernum:DukeTornado"].SetShaderTexture(ModContent.GetTexture("Terraria/Misc/Perlin"));
            TornadoDrawer.Draw(projectile.oldPos, projectile.Size * 0.5f - Main.screenPosition, 85);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
