using System.IO;
using System.Linq;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Deerclops
{
    public class DeerclopsEyeLaserbeam : BaseLaserbeamProjectile, IPixelPrimitiveDrawer
    {
        public int OwnerIndex
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public PrimitiveTrailCopy LaserDrawer;

        public const int LaserLifetime = 60;
        public override float MaxScale => 0.64f;
        public override float MaxLaserLength => 2500f;
        public override float Lifetime => LaserLifetime;
        public override Color LaserOverlayColor => Color.White;
        public override Color LightCastColor => Color.White;
        public override Texture2D LaserBeginTexture => TextureAssets.Projectile[Projectile.type].Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/AresLaserBeamMiddle", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/AresLaserBeamEnd", AssetRequestMode.ImmediateLoad).Value;
        public override string Texture => "CalamityMod/Projectiles/Boss/AresLaserBeamStart";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Firebeam");

        public override void SetDefaults()
        {
            Projectile.width = 38;
            Projectile.height = 38;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = LaserLifetime;
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
            if (Main.npc[OwnerIndex].active && Main.npc[OwnerIndex].type == NPCID.Deerclops)
            {
                Vector2 fireFrom = DeerclopsBehaviorOverride.GetEyePosition(Main.npc[OwnerIndex]) + Vector2.UnitY * Main.npc[OwnerIndex].gfxOffY;
                Projectile.Center = fireFrom;
            }

            // Die of the owner is invalid in some way.
            else
            {
                Projectile.Kill();
                return;
            }

            bool notUsingReleventAttack = Main.npc[OwnerIndex].ai[0] != (int)DeerclopsBehaviorOverride.DeerclopsAttackState.FeastclopsEyeLaserbeam;
            if (Main.npc[OwnerIndex].Opacity <= 0f || notUsingReleventAttack)
            {
                Projectile.Kill();
                return;
            }
        }

        public override float DetermineLaserLength()
        {
            float[] sampledLengths = new float[10];
            Collision.LaserScan(Projectile.Center, Projectile.velocity, Projectile.width * Projectile.scale, MaxLaserLength, sampledLengths);
            float newLaserLength = sampledLengths.Average() + 32f;

            // Fire laser through walls at max length if target is behind tiles.
            if (!Collision.CanHitLine(Main.npc[OwnerIndex].Center, 1, 1, Main.player[Main.npc[OwnerIndex].target].Center, 1, 1))
                newLaserLength = MaxLaserLength;

            return newLaserLength;
        }

        public override void UpdateLaserMotion() => Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[1]);

        public override void PostAI()
        {
            // Determine scale.
            Time = Lifetime - Projectile.timeLeft;
            Projectile.scale = LumUtils.Convert01To010(Time / Lifetime) * MaxScale * 3f;
            if (Projectile.scale > MaxScale)
                Projectile.scale = MaxScale;

            // Create impact stuff at the end of the laser.
            Vector2 endOfLaser = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * DetermineLaserLength() - Vector2.UnitY * 30f;
            Vector2 lightVelocity = -Vector2.UnitY.RotatedByRandom(1.03f) * 6f;
            SquishyLightParticle light = new(endOfLaser, lightVelocity, 1.25f, Color.Red, 30, 1f, 5f);
            GeneralParticleHandler.SpawnParticle(light);
        }

        public float LaserWidthFunction(float _) => Projectile.scale * Projectile.width;

        public static Color LaserColorFunction(float completionRatio)
        {
            float opacity = Utils.GetLerpValue(0f, 0.12f, completionRatio, true);
            float colorInterpolant = Sin(Main.GlobalTimeWrappedHourly * -3.2f + completionRatio * 13f) * 0.5f + 0.5f;
            return Color.Lerp(Color.Red, new(249, 225, 193), colorInterpolant * 0.67f) * opacity;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            // This should never happen, but just in case.
            if (Projectile.velocity == Vector2.Zero)
                return;

            LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, InfernumEffectsRegistry.ArtemisLaserVertexShader);

            Vector2 laserEnd = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * LaserLength;
            Vector2[] baseDrawPoints = new Vector2[8];
            for (int i = 0; i < baseDrawPoints.Length; i++)
                baseDrawPoints[i] = Vector2.Lerp(Projectile.Center, laserEnd, i / (float)(baseDrawPoints.Length - 1f));

            // Select textures to pass to the shader, along with the electricity color.
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.White);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakFaded);
            Main.instance.GraphicsDevice.Textures[2] = ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin").Value;

            LaserDrawer.DrawPixelated(baseDrawPoints, -Main.screenPosition, 64);
        }

        public override bool CanHitPlayer(Player target) => Projectile.scale >= 0.5f;

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {

        }
    }
}
