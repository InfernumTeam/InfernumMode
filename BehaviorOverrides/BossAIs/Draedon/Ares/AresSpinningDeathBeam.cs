using CalamityMod;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
	public class AresSpinningDeathBeam : BaseLaserbeamProjectile
	{
		public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

		public int OwnerIndex
		{
			get => (int)projectile.ai[1];
			set => projectile.ai[1] = value;
		}

		public float InitialSpinDirection = -100f;
		public float LifetimeThing;
		public override float MaxScale => 1f;
		public override float MaxLaserLength => AresDeathBeamTelegraph.TelegraphWidth;
		public override float Lifetime => LifetimeThing;
		public override Color LaserOverlayColor => new Color(250, 250, 250, 100);
		public override Color LightCastColor => Color.White;
		public override Texture2D LaserBeginTexture => ModContent.GetTexture("CalamityMod/Projectiles/Boss/AresDeathBeamStart");
		public override Texture2D LaserMiddleTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/AresDeathBeamMiddle");
		public override Texture2D LaserEndTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/AresDeathBeamEnd");

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Exo Overload Beam");
			Main.projFrames[projectile.type] = 5;
		}

		public override void SetDefaults()
		{
			projectile.width = 30;
			projectile.height = 30;
			projectile.hostile = true;
			projectile.alpha = 255;
			projectile.penetrate = -1;
			projectile.tileCollide = false;
			projectile.timeLeft = 1600;
			projectile.Calamity().canBreakPlayerDefense = true;
			cooldownSlot = 1;
		}

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write(projectile.localAI[0]);
			writer.Write(projectile.localAI[1]);
			writer.Write(InitialSpinDirection);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			projectile.localAI[0] = reader.ReadSingle();
			projectile.localAI[1] = reader.ReadSingle();
			InitialSpinDirection = reader.ReadSingle();
		}

		public override void AttachToSomething()
		{
			if (InitialSpinDirection == -100f)
				InitialSpinDirection = projectile.velocity.ToRotation();

			if (Main.npc[OwnerIndex].active && Main.npc[OwnerIndex].type == ModContent.NPCType<AresBody>() && Main.npc[OwnerIndex].Opacity > 0.35f)
			{
				projectile.velocity = (InitialSpinDirection + Main.npc[OwnerIndex].Infernum().ExtraAI[0]).ToRotationVector2();
				Vector2 fireFrom = new Vector2(Main.npc[OwnerIndex].Center.X - 1f, Main.npc[OwnerIndex].Center.Y + 23f);
				fireFrom += projectile.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(35f, 127f, projectile.scale * projectile.scale);
				projectile.Center = fireFrom;
			}

			// Die of the owner is invalid in some way.
			else
			{
				projectile.Kill();
				return;
			}
		}

		public override float DetermineLaserLength()
		{
			float[] sampledLengths = new float[10];
			Collision.LaserScan(projectile.Center, projectile.velocity, projectile.width * projectile.scale, MaxLaserLength, sampledLengths);

			float newLaserLength = sampledLengths.Average();

			// Fire laser through walls at max length if target is behind tiles.
			if (!Collision.CanHitLine(Main.npc[OwnerIndex].Center, 1, 1, Main.player[Main.npc[OwnerIndex].target].Center, 1, 1))
				newLaserLength = MaxLaserLength;

			return newLaserLength;
		}

		public override void PostAI()
		{
			// Spawn dust at the end of the beam.
			int dustType = 107;
			Vector2 dustCreationPosition = projectile.Center + projectile.velocity * (LaserLength - 14f);
			for (int i = 0; i < 2; i++)
			{
				float dustDirection = projectile.velocity.ToRotation() + Main.rand.NextBool().ToDirectionInt() * MathHelper.PiOver2;
				Vector2 dustVelocity = dustDirection.ToRotationVector2() * Main.rand.NextFloat(2f, 4f);
				Dust exoEnergy = Dust.NewDustDirect(dustCreationPosition, 0, 0, dustType, dustVelocity.X, dustVelocity.Y, 0, new Color(0, 255, 255), 1f);
				exoEnergy.noGravity = true;
				exoEnergy.scale = 1.7f;
			}

			if (Main.rand.NextBool(5))
			{
				Vector2 dustSpawnOffset = projectile.velocity.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloatDirection() * projectile.width * 0.5f;
				Dust exoEnergy = Dust.NewDustDirect(dustCreationPosition + dustSpawnOffset - Vector2.One * 4f, 8, 8, dustType, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);
				exoEnergy.velocity *= 0.5f;

				// Ensure that the dust always moves up.
				exoEnergy.velocity.Y = -Math.Abs(exoEnergy.velocity.Y);
			}

			// Determine frames.
			projectile.frameCounter++;
			if (projectile.frameCounter % 5f == 0f)
				projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			// This should never happen, but just in case-
			if (projectile.velocity == Vector2.Zero)
				return false;

			Color beamColor = LaserOverlayColor;
			Rectangle startFrameArea = LaserBeginTexture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
			Rectangle middleFrameArea = LaserMiddleTexture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
			Rectangle endFrameArea = LaserEndTexture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);

			// Start texture drawing.
			spriteBatch.Draw(LaserBeginTexture,
							 projectile.Center - Main.screenPosition,
							 startFrameArea,
							 beamColor,
							 projectile.rotation,
							 LaserBeginTexture.Size() / 2f,
							 projectile.scale,
							 SpriteEffects.None,
							 0f);

			// Prepare things for body drawing.
			float laserBodyLength = LaserLength + middleFrameArea.Height;
			Vector2 centerOnLaser = projectile.Center;

			// Body drawing.
			Rectangle screenArea = new Rectangle((int)(Main.screenPosition.X - 100f), (int)(Main.screenPosition.Y - 100f), Main.screenWidth + 200, Main.screenHeight + 200);
			if (laserBodyLength > 0f)
			{
				float laserOffset = middleFrameArea.Height * projectile.scale;
				float incrementalBodyLength = 0f;
				while (incrementalBodyLength + 1f < laserBodyLength)
				{
					if (!screenArea.Intersects(new Rectangle((int)centerOnLaser.X, (int)centerOnLaser.Y, 1, 1)))
					{
						centerOnLaser += projectile.velocity * laserOffset;
						incrementalBodyLength += laserOffset;
						continue;
					}

					spriteBatch.Draw(LaserMiddleTexture,
									 centerOnLaser - Main.screenPosition,
									 middleFrameArea,
									 beamColor,
									 projectile.rotation,
									 LaserMiddleTexture.Size() * 0.5f,
									 projectile.scale,
									 SpriteEffects.None,
									 0f);
					incrementalBodyLength += laserOffset;
					centerOnLaser += projectile.velocity * laserOffset;
					middleFrameArea.Y += LaserMiddleTexture.Height / Main.projFrames[projectile.type];
					if (middleFrameArea.Y + middleFrameArea.Height > LaserMiddleTexture.Height)
						middleFrameArea.Y = 0;
				}
			}

			Vector2 laserEndCenter = centerOnLaser - Main.screenPosition;
			spriteBatch.Draw(LaserEndTexture,
							 laserEndCenter,
							 endFrameArea,
							 beamColor,
							 projectile.rotation,
							 LaserEndTexture.Size() * 0.5f,
							 projectile.scale,
							 SpriteEffects.None,
							 0f);
			return false;
		}

		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			target.AddBuff(BuffID.OnFire, 360);
			target.AddBuff(BuffID.Frostburn, 360);
		}

		public override bool CanHitPlayer(Player target) => projectile.scale >= 0.5f;

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
		{
			target.Calamity().lastProjectileHit = projectile;
		}
	}
}
