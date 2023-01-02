using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class AimedDeathray : BaseLaserbeamProjectile
    {
        public PrimitiveTrailCopy LaserDrawer
        {
            get;
            set;
        } = null;

        public const int LifetimeConst = 27;

        public const float LaserLengthConst = 2820f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override float MaxScale => 1.05f;

        public override float MaxLaserLength => LaserLengthConst;

        public override float Lifetime => LifetimeConst;

        public override Color LaserOverlayColor => Color.Lerp(Color.IndianRed, Color.Red, 0.6f) * 1.2f;

        public override Color LightCastColor => LaserOverlayColor;

        public override Texture2D LaserBeginTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/UltimaRayStart", AssetRequestMode.ImmediateLoad).Value;

        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/UltimaRayMid", AssetRequestMode.ImmediateLoad).Value;

        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/UltimaRayEnd", AssetRequestMode.ImmediateLoad).Value;
        
        // To allow easy, static access from different locations.
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Deathray");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 44;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AttachToSomething()
        {
            if (!Main.npc.IndexInRange((int)Projectile.ai[1]) || !Main.npc[(int)Projectile.ai[1]].active)
                Projectile.Kill();
            
            Projectile.velocity = (Main.npc[(int)Projectile.ai[1]].rotation + MathHelper.PiOver2).ToRotationVector2();
            Projectile.Center = Main.npc[(int)Projectile.ai[1]].Center + Projectile.velocity * 40f;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (float i = 0f; i < LaserLength; i += 180f)
            {
                for (int direction = -1; direction <= 1; direction += 2)
                {
                    Vector2 shootVelocity = Projectile.velocity.RotatedBy(MathHelper.PiOver2 * direction) * 4.2f;
                    Utilities.NewProjectileBetter(Projectile.Center + Projectile.velocity * i, shootVelocity, ProjectileID.DeathLaser, 130, 0f);
                }
            }
        }


        public float LaserWidthFunction(float _) => Projectile.scale * Projectile.width * Projectile.localAI[1] * 0.5f;

        public Color LaserColorFunction(float completionRatio)
        {
            float colorInterpolant = CalamityUtils.Convert01To010(Time / Lifetime) * 0.45f + 0.15f;
            colorInterpolant = MathHelper.Lerp(colorInterpolant, 1f, 1f - 1f / Projectile.localAI[1]);

            return Color.Lerp(Color.Red, Color.White, colorInterpolant) * (1f / Projectile.localAI[1]);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // This should never happen, but just in case.
            if (Projectile.velocity == Vector2.Zero)
                return false;

            // Initialize the laser drawer.
            LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader);
            
            Vector2 laserEnd = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * LaserLength;
            Vector2[] baseDrawPoints = new Vector2[8];
            for (int i = 0; i < baseDrawPoints.Length; i++)
                baseDrawPoints[i] = Vector2.Lerp(Projectile.Center, laserEnd, i / (float)(baseDrawPoints.Length - 1f));

            // Select textures to pass to the shader, along with the electricity color.
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.White);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage1("Images/Extra_197");
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");

            float oldLocalAI = Projectile.localAI[1];
            for (float scaleFactor = 3f; scaleFactor >= 1f; scaleFactor -= 0.6f)
            {
                Projectile.localAI[1] = scaleFactor;
                LaserDrawer.Draw(baseDrawPoints, -Main.screenPosition, 54);
            }
            Projectile.localAI[1] = oldLocalAI;
            return false;
        }

        public override void DetermineScale() => Projectile.scale = CalamityUtils.Convert01To010(Time / Lifetime);

        public override bool ShouldUpdatePosition() => false;
    }
}
