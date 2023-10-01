using CalamityMod;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Projectiles.BaseProjectiles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresSpinningDeathBeam : BaseLaserbeamProjectile, IPixelPrimitiveDrawer
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public int OwnerIndex
        {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public PrimitiveTrailCopy BeamDrawer;

        public float InitialSpinDirection = -100f;

        public bool SuperLaser => Main.npc[OwnerIndex].localAI[3] >= 0.1f;

        public float LifetimeThing = 600f;
        public override float MaxScale => 1f;
        public override float MaxLaserLength => AresDeathBeamTelegraph.TelegraphWidth;
        public override float Lifetime => LifetimeThing;
        public override Color LaserOverlayColor => new(250, 250, 250, 100);
        public override Color LightCastColor => Color.White;
        public override Texture2D LaserBeginTexture => ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/AresDeathBeamStart", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/AresDeathBeamMiddle", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/AresDeathBeamEnd", AssetRequestMode.ImmediateLoad).Value;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Exo Overload Beam");
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 85;
            Projectile.height = 56;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 1600;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
            writer.Write(InitialSpinDirection);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
            InitialSpinDirection = reader.ReadSingle();
        }

        public override void AttachToSomething()
        {
            if (InitialSpinDirection == -100f)
                InitialSpinDirection = Projectile.velocity.ToRotation();

            // Adjust the size if this is a super laser.
            if (Projectile.width != 160 && SuperLaser)
                Projectile.width = 160;

            if (Main.npc[OwnerIndex].active && Main.npc[OwnerIndex].type == ModContent.NPCType<AresBody>() && Main.npc[OwnerIndex].Opacity > 0.35f)
            {
                float spinOffset = Main.npc[OwnerIndex].Infernum().ExtraAI[0];
                Projectile.velocity = (InitialSpinDirection + spinOffset).ToRotationVector2();
                Vector2 fireFrom = new(Main.npc[OwnerIndex].Center.X - 1f, Main.npc[OwnerIndex].Center.Y + 23f);
                fireFrom += Projectile.velocity.SafeNormalize(Vector2.UnitY) * Lerp(2f, 16f, Projectile.scale * Projectile.scale);
                Projectile.Center = fireFrom;
            }

            // Die of the owner is invalid in some way.
            else
            {
                Projectile.Kill();
                return;
            }
        }

        public override float DetermineLaserLength() => MaxLaserLength;

        public override void PostAI()
        {
            // Spawn dust at the end of the beam.
            int dustType = 107;
            Vector2 dustCreationPosition = Projectile.Center + Projectile.velocity * (LaserLength - 14f);
            for (int i = 0; i < 2; i++)
            {
                float dustDirection = Projectile.velocity.ToRotation() + Main.rand.NextBool().ToDirectionInt() * PiOver2;
                Vector2 dustVelocity = dustDirection.ToRotationVector2() * Main.rand.NextFloat(2f, 4f);
                Dust exoEnergy = Dust.NewDustDirect(dustCreationPosition, 0, 0, dustType, dustVelocity.X, dustVelocity.Y, 0, new Color(0, 255, 255), 1f);
                exoEnergy.noGravity = true;
                exoEnergy.scale = 1.7f;
            }

            if (Main.rand.NextBool(5))
            {
                Vector2 dustSpawnOffset = Projectile.velocity.RotatedBy(PiOver2) * Main.rand.NextFloatDirection() * Projectile.width * 0.5f;
                Dust exoEnergy = Dust.NewDustDirect(dustCreationPosition + dustSpawnOffset - Vector2.One * 4f, 8, 8, dustType, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);
                exoEnergy.velocity *= 0.5f;

                // Ensure that the dust always moves up.
                exoEnergy.velocity.Y = -Math.Abs(exoEnergy.velocity.Y);
            }

            // Determine frames.
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5f == 0f)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
        }

        public float WidthFunction(float completionRatio)
        {
            return Clamp(Projectile.width * Projectile.scale, 0f, Projectile.width);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Main.hslToRgb((completionRatio * 2f + Main.GlobalTimeWrappedHourly * 0.4f + Projectile.identity * 0.27f) % 1f, 1f, 0.6f);
            if (SuperLaser)
            {
                Color laserFireColor = Color.Lerp(Color.Orange, Color.Red, Sin(Main.GlobalTimeWrappedHourly * 1.7f + completionRatio * 2.3f) * 0.5f + 0.5f);
                color = Color.Lerp(color, laserFireColor, 0.8f);
            }

            color.A = 5;
            return color * 2f;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, false, GameShaders.Misc["CalamityMod:Bordernado"]);

            GameShaders.Misc["CalamityMod:Bordernado"].UseSaturation(Lerp(0.23f, 0.29f, Projectile.identity / 9f % 1f));
            GameShaders.Misc["CalamityMod:Bordernado"].SetShaderTexture(InfernumTextureRegistry.CultistRayMap);

            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
                points.Add(Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, i / 8f));

            if (Time >= 2f)
            {
                for (float offset = 0f; offset < 6f; offset += 0.75f)
                {
                    BeamDrawer.DrawPixelated(points, -Main.screenPosition, 11);
                    BeamDrawer.DrawPixelated(points, (Main.GlobalTimeWrappedHourly * 1.8f).ToRotationVector2() * offset - Main.screenPosition, 9);
                    BeamDrawer.DrawPixelated(points, -(Main.GlobalTimeWrappedHourly * 1.8f).ToRotationVector2() * offset - Main.screenPosition, 9);
                }
            }
        }

        public override bool CanHitPlayer(Player target) => Projectile.scale >= 0.5f;
    }
}
