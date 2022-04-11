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
        public ref float TornadoHeight => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Water Jet");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 25;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 90;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(60f, 48f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);

            // Split into a bunch of water bolts.
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.timeLeft == 12f)
            {
                for (int i = 0; i < 7; i++)
                {
                    Vector2 boltShootVelocity = (MathHelper.TwoPi * i / 7f).ToRotationVector2() * 12f;
                    Projectile.NewProjectile(Projectile.Center, boltShootVelocity, ModContent.ProjectileType<AbyssalWaterBolt>(), (int)(Projectile.damage * 0.8f), 0f);
                }
            }

            if (Projectile.velocity.Length() < 34f)
                Projectile.velocity *= 1.042f;
        }

        internal Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.BlueViolet, Color.Black, 0.85f) * Projectile.Opacity * 1.2f;
            return color;
        }

        internal float WidthFunction(float completionRatio)
        {
            return MathHelper.Lerp(Projectile.width * 0.6f, Projectile.width + 16f, 1f - completionRatio);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.oldPos[5] + Projectile.Size * 0.5f,
                Projectile.oldPos[Projectile.oldPos.Length - 6] + Projectile.Size * 0.5f,
                (int)(Projectile.width * 0.525),
                ref _) && Projectile.timeLeft < 72f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (TornadoDrawer is null)
                TornadoDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:DukeTornado"]);

            GameShaders.Misc["Infernum:DukeTornado"].SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Misc/Perlin").Value);
            TornadoDrawer.Draw(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 85);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
