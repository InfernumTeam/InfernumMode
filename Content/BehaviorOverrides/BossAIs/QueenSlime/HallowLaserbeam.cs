using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class HallowLaserbeam : BaseLaserbeamProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy LaserDrawer
        {
            get;
            set;
        }

        public const int LaserLifetime = 21;

        public override float Lifetime => LaserLifetime;

        public override Color LaserOverlayColor => Color.White;

        public override Color LightCastColor => Color.LightPink;

        public override Texture2D LaserBeginTexture => TextureAssets.Projectile[Projectile.type].Value;

        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Lasers/OrangeLaserbeamMid", AssetRequestMode.ImmediateLoad).Value;

        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/Lasers/OrangeLaserbeamEnd", AssetRequestMode.ImmediateLoad).Value;

        public override float MaxLaserLength => 2600f;

        public override float MaxScale => 1f;

        public override string Texture => InfernumTextureRegistry.InvisPath;

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Hallow Deathray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.alpha = 255;
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

        public override bool? CanDamage() => Time >= 2f ? null : false;

        public float LaserWidthFunction(float _) => Projectile.scale * Projectile.width * 2f;

        public static Color LaserColorFunction(float completionRatio)
        {
            float colorInterpolant = 0.5f * Cos(-3.2f * Main.GlobalTimeWrappedHourly + completionRatio * 23f) + 0.5f;
            return Color.Lerp(Color.SkyBlue, Color.Blue, 0.3f * colorInterpolant);
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
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
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseOpacity(-0.35f);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.LightPink);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.FireNoise);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");

            LaserDrawer.DrawPixelated(baseDrawPoints, -Main.screenPosition, 32);
        }
    }
}
