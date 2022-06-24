using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena
{
    public class PulseBeamStart : BaseLaserbeamProjectile
    {
        public int OwnerIndex
        {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public bool OwnerIsValid => Main.npc[OwnerIndex].active && Main.npc[OwnerIndex].type == ModContent.NPCType<AthenaNPC>();

        public const int LifetimeConst = 360;

        public override float MaxScale => 1f;

        public override float MaxLaserLength => 3600f;

        public override float Lifetime => LifetimeConst;

        public override Color LaserOverlayColor => new(250, 250, 250, 100);

        public override Color LightCastColor => Color.White;

        public override Texture2D LaserBeginTexture =>
            ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;

        public override Texture2D LaserMiddleTexture =>
            ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/PulseBeamMiddle", AssetRequestMode.ImmediateLoad).Value;

        public override Texture2D LaserEndTexture =>
            ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/PulseBeamEnd", AssetRequestMode.ImmediateLoad).Value;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Pulse Disintegration Beam");
            Main.projFrames[Projectile.type] = 5;

        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
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
            if (OwnerIsValid)
            {
                Vector2 fireFrom = Main.npc[OwnerIndex].ModNPC<AthenaNPC>().MainTurretCenter;
                fireFrom += Projectile.velocity.SafeNormalize(Vector2.UnitY) * Projectile.scale * 168f;
                Projectile.Center = fireFrom;
            }

            // Die of the owner is invalid in some way.
            // This is not done client-side, as it's possible that they may not have recieved the proper owner index yet.
            else
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.Kill();
                return;
            }

            // Die if the owner is not performing Athena' deathray attack.
            if (Main.npc[OwnerIndex].ai[0] != (int)AthenaNPC.AthenaAttackType.AimedPulseLasers)
            {
                Projectile.Kill();
                return;
            }
        }

        public override void UpdateLaserMotion()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        public override void PostAI()
        {
            if (!OwnerIsValid)
                return;

            // Spawn dust at the end of the beam.
            int dustType = (int)CalamityDusts.PurpleCosmilite;
            Vector2 dustCreationPosition = Projectile.Center + Projectile.velocity * (LaserLength - 14f);
            for (int i = 0; i < 2; i++)
            {
                float dustDirection = Projectile.velocity.ToRotation() + Main.rand.NextBool().ToDirectionInt() * MathHelper.PiOver2;
                Vector2 dustVelocity = dustDirection.ToRotationVector2() * Main.rand.NextFloat(2f, 4f);

                Dust redFlame = Dust.NewDustDirect(dustCreationPosition, 0, 0, dustType, dustVelocity.X, dustVelocity.Y, 0, default, 1f);
                redFlame.noGravity = true;
                redFlame.scale = 1.7f;
            }

            if (Main.rand.NextBool(5))
            {
                Vector2 dustSpawnOffset = Projectile.velocity.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloatDirection() * Projectile.width * 0.5f;

                Dust redFlame = Dust.NewDustDirect(dustCreationPosition + dustSpawnOffset - Vector2.One * 4f, 8, 8, dustType, 0f, 0f, 100, default, 1.5f);
                redFlame.velocity *= 0.5f;

                // Ensure that the dust always moves up.
                redFlame.velocity.Y = -Math.Abs(redFlame.velocity.Y);
            }

            // Determine frames.
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5f == 0f)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!OwnerIsValid)
                return false;

            // This should never happen, but just in case.
            if (Projectile.velocity == Vector2.Zero || Projectile.localAI[0] < 2f)
                return false;

            Color beamColor = LaserOverlayColor;
            Rectangle startFrameArea = LaserBeginTexture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Rectangle middleFrameArea = LaserMiddleTexture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Rectangle endFrameArea = LaserEndTexture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

            // Start texture drawing.
            Main.spriteBatch.Draw(LaserBeginTexture,
                             Projectile.Center - Main.screenPosition,
                             startFrameArea,
                             beamColor,
                             Projectile.rotation,
                             LaserBeginTexture.Size() / 2f,
                             Projectile.scale,
                             SpriteEffects.None,
                             0);

            // Prepare things for body drawing.
            float laserBodyLength = LaserLength + middleFrameArea.Height;
            Vector2 centerOnLaser = Projectile.Center + Projectile.velocity * -5f;

            // Body drawing.
            if (laserBodyLength > 0f)
            {
                float laserOffset = middleFrameArea.Height * Projectile.scale;
                float incrementalBodyLength = 0f;
                while (incrementalBodyLength + 1f < laserBodyLength)
                {
                    Main.spriteBatch.Draw(LaserMiddleTexture,
                                     centerOnLaser - Main.screenPosition,
                                     middleFrameArea,
                                     beamColor,
                                     Projectile.rotation,
                                     LaserMiddleTexture.Size() * 0.5f,
                                     Projectile.scale,
                                     SpriteEffects.None,
                                     0);
                    incrementalBodyLength += laserOffset;
                    centerOnLaser += Projectile.velocity * laserOffset;
                    middleFrameArea.Y += LaserMiddleTexture.Height / Main.projFrames[Projectile.type];
                    if (middleFrameArea.Y + middleFrameArea.Height > LaserMiddleTexture.Height)
                        middleFrameArea.Y = 0;
                }
            }

            Vector2 laserEndCenter = centerOnLaser - Main.screenPosition;
            Main.spriteBatch.Draw(LaserEndTexture,
                             laserEndCenter,
                             endFrameArea,
                             beamColor,
                             Projectile.rotation,
                             LaserEndTexture.Size() * 0.5f,
                             Projectile.scale,
                             SpriteEffects.None,
                             0);
            return false;
        }

        public override bool CanHitPlayer(Player target) => OwnerIsValid && Projectile.scale >= 0.5f;

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
