using CalamityMod.NPCs;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid.CeaselessVoidBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class CeaselessVortexTear : ModProjectile
    {
        public bool CreateShockwave
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value.ToInt();
        }

        public ref float Time => ref Projectile.ai[1];

        public static int Lifetime => 90;

        public static float Acceleration => 1.03f;

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ceaseless Tear");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = Lifetime;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.timeLeft = Lifetime;
            Projectile.MaxUpdates = 3;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Disappear if the Ceaseless Void is not present.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.voidBoss))
            {
                Projectile.Kill();
                return;
            }

            // Accelerate.
            Projectile.velocity *= Acceleration;

            // Dissipate into nothingness before dying.
            Projectile.scale = Utils.GetLerpValue(0f, 26f, Projectile.timeLeft, true);
            Projectile.Opacity = Projectile.scale;

            Time++;
        }

        public override bool ShouldUpdatePosition() => true;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Time <= 3f || Projectile.timeLeft < 26f)
                return false;

            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.oldPos[0] + Projectile.Size * 0.5f, Projectile.Center, WidthFunction(0.3f) * 0.25f, ref _);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !CreateShockwave)
                return;

            Vector2 end = Projectile.Center - 20f * Projectile.velocity;
            Utilities.NewProjectileBetter(end, Vector2.Zero, ModContent.ProjectileType<CeaselessEnergyPulse>(), EnergyPulseDamage, 0f);

            for (int i = 0; i < 6; i++)
                Utilities.NewProjectileBetter(end, (TwoPi * i / 6f + Pi / 6f).ToRotationVector2() * 6f, ModContent.ProjectileType<OtherworldlyBolt>(), OtherworldlyBoltDamage, 0f, -1, (int)OtherworldlyBolt.OtherwordlyBoltAttackState.AccelerateFromBelow);
        }

        public float WidthFunction(float completionRatio)
        {
            float tip = Utils.GetLerpValue(0f, 0.26f, completionRatio, true);
            return tip * Projectile.scale * Projectile.width;
        }

        public Color ColorFunction(float completionRatio)
        {
            Color baseColor = Color.Lerp(Color.Purple, Color.DarkBlue, 0.65f);
            float opacity = 0.35f * Utils.GetLerpValue(0.8f, 0.65f, completionRatio, true) * Projectile.Opacity;
            return baseColor * opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            InfernumEffectsRegistry.RealityTearVertexShader.SetShaderTexture(InfernumTextureRegistry.Stars);
            InfernumEffectsRegistry.RealityTearVertexShader.Shader.Parameters["useOutline"].SetValue(true);
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(WidthFunction, ColorFunction, _ => Projectile.Size * 0.5f, Shader: InfernumEffectsRegistry.RealityTearVertexShader), 16);
            return false;
        }
    }
}
