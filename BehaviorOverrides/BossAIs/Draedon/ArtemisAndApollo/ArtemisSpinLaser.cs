using CalamityMod;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo
{
	public class ArtemisSpinLaser : BaseLaserbeamProjectile
	{
		public int OwnerIndex
		{
			get => (int)projectile.ai[0];
			set => projectile.ai[0] = value;
		}

		public const int LaserLifetime = 135;
		public override float MaxScale => 1f;
		public override float MaxLaserLength => 3600f;
		public override float Lifetime => LaserLifetime;
		public override Color LaserOverlayColor => new Color(250, 180, 100, 100);
		public override Color LightCastColor => Color.White;
		public override Texture2D LaserBeginTexture => Main.projectileTexture[projectile.type];
		public override Texture2D LaserMiddleTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/AresLaserBeamMiddle");
		public override Texture2D LaserEndTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/AresLaserBeamEnd");
		public override string Texture => "CalamityMod/Projectiles/Boss/AresLaserBeamStart";

		// Dude
		// Dude
		// Dude
		// You are going to Ohio
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Ohio Beam");
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
			if (Main.npc[OwnerIndex].active && Main.npc[OwnerIndex].type == ModContent.NPCType<Artemis>())
			{
				Vector2 fireFrom = Main.npc[OwnerIndex].Center + Vector2.UnitY * Main.npc[OwnerIndex].gfxOffY;
				fireFrom += projectile.velocity.SafeNormalize(Vector2.UnitY) * 78f;
				projectile.Center = fireFrom;
			}

			// Die of the owner is invalid in some way.
			else
			{
				projectile.Kill();
				return;
			}

			bool notUsingReleventAttack = Main.npc[OwnerIndex].ai[0] != (int)ApolloBehaviorOverride.TwinsAttackType.SpecialAttack_LaserRayScarletBursts;
			if (Main.npc[OwnerIndex].Opacity <= 0f || notUsingReleventAttack)
			{
				projectile.Kill();
				return;
			}

			Time = Main.npc[OwnerIndex].ai[1];
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

		public override void UpdateLaserMotion()
		{
			projectile.rotation = Main.npc[OwnerIndex].rotation;
			projectile.velocity = (projectile.rotation - MathHelper.PiOver2).ToRotationVector2();
		}

		public override void PostAI()
		{
			// Determine frames.
			projectile.frameCounter++;
			if (projectile.frameCounter % 5f == 0f)
				projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			// This should never happen, but just in case.
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
							 SpriteEffects.FlipVertically,
							 0f);

			// Prepare things for body drawing.
			float laserBodyLength = LaserLength + middleFrameArea.Height;
			Vector2 centerOnLaser = projectile.Center + projectile.velocity * projectile.scale * 5f;

			// Body drawing.
			if (laserBodyLength > 0f)
			{
				float laserOffset = middleFrameArea.Height * projectile.scale;
				float incrementalBodyLength = 0f;
				while (incrementalBodyLength + 1f < laserBodyLength)
				{
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
							 SpriteEffects.FlipVertically,
							 0f);
			return false;
		}

		public override bool CanHitPlayer(Player target) => projectile.scale >= 0.5f;

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
		{
			target.Calamity().lastProjectileHit = projectile;
		}
	}
}
