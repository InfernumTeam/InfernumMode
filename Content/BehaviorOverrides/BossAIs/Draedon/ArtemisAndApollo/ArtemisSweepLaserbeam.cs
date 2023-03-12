using CalamityMod;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.Projectiles.BaseProjectiles;
using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo
{
    public class ArtemisSweepLaserbeam : BaseLaserbeamProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy LaserDrawer
        {
            get;
            set;
        } = null;

        public int OwnerIndex
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public const int LifetimeConst = 540;

        public const float MaxLaserRayConst = 6000f;

        public override float MaxScale => 1.5f;
        public override float MaxLaserLength => MaxLaserRayConst;
        public override float Lifetime => LifetimeConst;
        public override Color LaserOverlayColor => new(250, 180, 100, 100);
        public override Color LightCastColor => Color.White;
        public override Texture2D LaserBeginTexture => ModContent.Request<Texture2D>(Texture).Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/AresLaserBeamMiddle", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/AresLaserBeamEnd", AssetRequestMode.ImmediateLoad).Value;
        public override string Texture => "CalamityMod/Projectiles/Boss/AresLaserBeamStart";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ohio Beam");
            Main.projFrames[Projectile.type] = 5;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
            // This is its serious name
            // DisplayName.SetDefault("Exothermal Artemis Beam");
        }

        public override void SetDefaults()
        {
            Projectile.Calamity().DealsDefenseDamage = true;
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
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
            if (Main.npc[OwnerIndex].active && Main.npc[OwnerIndex].type == ModContent.NPCType<Artemis>() && Main.npc[OwnerIndex].Opacity > 0.25f)
            {
                Vector2 fireFrom = Main.npc[OwnerIndex].Center + Vector2.UnitY * Main.npc[OwnerIndex].gfxOffY;
                fireFrom += Projectile.velocity.SafeNormalize(Vector2.UnitY) * 50f;
                Projectile.Center = fireFrom;
            }

            // Die of the owner is invalid in some way.
            else
            {
                Projectile.Kill();
                return;
            }
        }

        public override float DetermineLaserLength()
        {
            float[] sampledLengths = new float[10];
            Collision.LaserScan(Projectile.Center, Projectile.velocity, Projectile.width * Projectile.scale, MaxLaserLength, sampledLengths);

            float newLaserLength = sampledLengths.Average();

            // Fire laser through walls at max length if target is behind tiles.
            if (!Collision.CanHitLine(Main.npc[OwnerIndex].Center, 1, 1, Main.player[Main.npc[OwnerIndex].target].Center, 1, 1))
                newLaserLength = MaxLaserLength;

            return newLaserLength;
        }

        public override void UpdateLaserMotion()
        {
            Projectile.rotation = Main.npc[OwnerIndex].rotation;
            Projectile.velocity = (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2();
        }

        public override void PostAI()
        {
            // Determine frames.
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5f == 0f)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
        }

        public float LaserWidthFunction(float _) => Projectile.scale * Projectile.width;

        public static Color LaserColorFunction(float completionRatio)
        {
            float colorInterpolant = (float)Math.Sin(Main.GlobalTimeWrappedHourly * -3.2f + completionRatio * 23f) * 0.5f + 0.5f;
            return Color.Lerp(Color.Orange, Color.Red, colorInterpolant * 0.67f);
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
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseColor(Color.Cyan);
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage1("Images/Extra_189");
            InfernumEffectsRegistry.ArtemisLaserVertexShader.UseImage2("Images/Misc/Perlin");

            LaserDrawer.DrawPixelated(baseDrawPoints, -Main.screenPosition, 54);
        }

        public override bool CanHitPlayer(Player target) => Projectile.scale >= 0.5f;
    }
}
