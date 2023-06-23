using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class DivineLightLaserbeam : BaseLaserbeamProjectile, IAboveWaterProjectileDrawer
    {
        public PrimitiveTrailCopy LaserDrawer
        {
            get;
            set;
        } = null;

        public int OwnerIndex => (int)Projectile.ai[1];
        public override float Lifetime => LifetimeConst;
        public override Color LaserOverlayColor => Color.White;
        public override Color LightCastColor => Color.Cyan;
        public override Texture2D LaserBeginTexture => InfernumTextureRegistry.Invisible.Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Lasers/BlueLaserbeamMid", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Lasers/BlueLaserbeamEnd", AssetRequestMode.ImmediateLoad).Value;
        public override float MaxLaserLength => LaserLengthCost;
        public override float MaxScale => 1f;

        public static int LifetimeConst => 120;

        public static float LaserLengthCost => 3200f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Divine Light Deathray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = (int)Lifetime;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
        }

        public override void AttachToSomething()
        {
            if (!Main.npc.IndexInRange(OwnerIndex))
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = DivineLightOrb.GetHoverDestination(Main.npc[OwnerIndex]);
            Projectile.Opacity = 1f;
            Projectile.velocity = Main.npc[OwnerIndex].velocity.SafeNormalize(Vector2.UnitY);
            RotationalSpeed = 0f;
        }

        public override bool? CanDamage() => Time > 35f ? null : false;

        public float LaserWidthFunction(float _) => Projectile.scale * Projectile.width * 2f;

        public static Color LaserColorFunction(float completionRatio)
        {
            float colorInterpolant = Sin(Main.GlobalTimeWrappedHourly * -3.1f + completionRatio * 20f) * 0.5f + 0.5f;
            return Color.Lerp(Color.Wheat, Color.Yellow, colorInterpolant * 0.64f);
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawAboveWater(SpriteBatch spriteBatch)
        {
            // This should never happen, but just in case.
            if (Projectile.velocity == Vector2.Zero)
                return;

            LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader);
            Vector2 laserEnd = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * LaserLength;
            Vector2[] baseDrawPoints = new Vector2[20];
            for (int i = 0; i < baseDrawPoints.Length; i++)
                baseDrawPoints[i] = Vector2.Lerp(Projectile.Center, laserEnd, i / (float)(baseDrawPoints.Length - 1f));

            // Select textures to pass to the shader, along with the electricity color.
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.White);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakThickGlow);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");

            LaserDrawer.Draw(baseDrawPoints, -Main.screenPosition, 54);
        }
    }
}
