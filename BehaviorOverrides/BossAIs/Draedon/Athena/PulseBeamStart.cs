using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
			get => (int)projectile.ai[1];
			set => projectile.ai[1] = value;
		}

		public bool OwnerIsValid => Main.npc[OwnerIndex].active && Main.npc[OwnerIndex].type == ModContent.NPCType<AthenaNPC>();

		public const int LifetimeConst = 360;

		public override float MaxScale => 1f;

		public override float MaxLaserLength => 3600f;

		public override float Lifetime => LifetimeConst;

		public override Color LaserOverlayColor => new Color(250, 250, 250, 100);

		public override Color LightCastColor => Color.White;

		public override Texture2D LaserBeginTexture =>
			ModContent.GetTexture(Texture);

		public override Texture2D LaserMiddleTexture =>
			ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/PulseBeamMiddle");

		public override Texture2D LaserEndTexture =>
			ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Draedon/Athena/PulseBeamEnd");

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Pulse Disintegration Beam");
			Main.projFrames[projectile.type] = 5;

		}

		public override void SetDefaults()
		{
			projectile.width = projectile.height = 40;
			projectile.hostile = true;
			projectile.alpha = 255;
			projectile.penetrate = -1;
			projectile.tileCollide = false;
			projectile.timeLeft = 600;
			projectile.Calamity().canBreakPlayerDefense = true;
			cooldownSlot = 1;
		}

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write(projectile.localAI[0]);
			writer.Write(projectile.localAI[1]);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			projectile.localAI[0] = reader.ReadSingle();
			projectile.localAI[1] = reader.ReadSingle();
		}

		public override void AttachToSomething()
		{
			if (OwnerIsValid)
			{
				Vector2 fireFrom = Main.npc[OwnerIndex].ModNPC<AthenaNPC>().MainTurretCenter;
				fireFrom += projectile.velocity.SafeNormalize(Vector2.UnitY) * projectile.scale * 168f;
				projectile.Center = fireFrom;
			}

			// Die of the owner is invalid in some way.
			// This is not done client-side, as it's possible that they may not have recieved the proper owner index yet.
			else
			{
				if (Main.netMode != NetmodeID.MultiplayerClient)
					projectile.Kill();
				return;
			}

			// Die if the owner is not performing Athena' deathray attack.
			if (Main.npc[OwnerIndex].ai[0] != (int)AthenaNPC.AthenaAttackType.AimedPulseLasers)
			{
				projectile.Kill();
				return;
			}
		}

		public override void UpdateLaserMotion()
		{
			projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
		}

		public override void PostAI()
		{
			if (!OwnerIsValid)
				return;

			// Spawn dust at the end of the beam.
			int dustType = (int)CalamityDusts.PurpleCosmilite;
			Vector2 dustCreationPosition = projectile.Center + projectile.velocity * (LaserLength - 14f);
			for (int i = 0; i < 2; i++)
			{
				float dustDirection = projectile.velocity.ToRotation() + Main.rand.NextBool().ToDirectionInt() * MathHelper.PiOver2;
				Vector2 dustVelocity = dustDirection.ToRotationVector2() * Main.rand.NextFloat(2f, 4f);

				Dust redFlame = Dust.NewDustDirect(dustCreationPosition, 0, 0, dustType, dustVelocity.X, dustVelocity.Y, 0, default, 1f);
				redFlame.noGravity = true;
				redFlame.scale = 1.7f;
			}

			if (Main.rand.NextBool(5))
			{
				Vector2 dustSpawnOffset = projectile.velocity.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloatDirection() * projectile.width * 0.5f;

				Dust redFlame = Dust.NewDustDirect(dustCreationPosition + dustSpawnOffset - Vector2.One * 4f, 8, 8, dustType, 0f, 0f, 100, default, 1.5f);
				redFlame.velocity *= 0.5f;

				// Ensure that the dust always moves up.
				redFlame.velocity.Y = -Math.Abs(redFlame.velocity.Y);
			}

			// Determine frames.
			projectile.frameCounter++;
			if (projectile.frameCounter % 5f == 0f)
				projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			if (!OwnerIsValid)
				return false;

			// This should never happen, but just in case.
			if (projectile.velocity == Vector2.Zero || projectile.localAI[0] < 2f)
				return false;

			Color beamColor = LaserOverlayColor;
			Rectangle startFrameArea = LaserBeginTexture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
			Rectangle middleFrameArea = LaserMiddleTexture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
			Rectangle endFrameArea = LaserEndTexture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);

			// Start texture drawing.
			Main.spriteBatch.Draw(LaserBeginTexture,
							 projectile.Center - Main.screenPosition,
							 startFrameArea,
							 beamColor,
							 projectile.rotation,
							 LaserBeginTexture.Size() / 2f,
							 projectile.scale,
							 SpriteEffects.None,
							 0);

			// Prepare things for body drawing.
			float laserBodyLength = LaserLength + middleFrameArea.Height;
			Vector2 centerOnLaser = projectile.Center + projectile.velocity * -5f;

			// Body drawing.
			if (laserBodyLength > 0f)
			{
				float laserOffset = middleFrameArea.Height * projectile.scale;
				float incrementalBodyLength = 0f;
				while (incrementalBodyLength + 1f < laserBodyLength)
				{
					Main.spriteBatch.Draw(LaserMiddleTexture,
									 centerOnLaser - Main.screenPosition,
									 middleFrameArea,
									 beamColor,
									 projectile.rotation,
									 LaserMiddleTexture.Size() * 0.5f,
									 projectile.scale,
									 SpriteEffects.None,
									 0);
					incrementalBodyLength += laserOffset;
					centerOnLaser += projectile.velocity * laserOffset;
					middleFrameArea.Y += LaserMiddleTexture.Height / Main.projFrames[projectile.type];
					if (middleFrameArea.Y + middleFrameArea.Height > LaserMiddleTexture.Height)
						middleFrameArea.Y = 0;
				}
			}

			Vector2 laserEndCenter = centerOnLaser - Main.screenPosition;
			Main.spriteBatch.Draw(LaserEndTexture,
							 laserEndCenter,
							 endFrameArea,
							 beamColor,
							 projectile.rotation,
							 LaserEndTexture.Size() * 0.5f,
							 projectile.scale,
							 SpriteEffects.None,
							 0);
			return false;
		}

		public override bool CanHitPlayer(Player target) => OwnerIsValid && projectile.scale >= 0.5f;

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
		{
			target.Calamity().lastProjectileHit = projectile;
		}
	}
}
