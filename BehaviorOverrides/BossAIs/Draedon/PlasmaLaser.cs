using CalamityMod;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
	public class PlasmaLaser : ModProjectile
	{
		public int ExplodeCountdown;
		public ref float TelegraphDelay => ref projectile.ai[0];
		public ref float PulseFlash => ref projectile.localAI[0];
		public ref float InitialSpeed => ref projectile.localAI[1];
		public NPC ThingToAttachTo => Main.npc.IndexInRange((int)projectile.ai[1]) ? Main.npc[(int)projectile.ai[1]] : null;

		public Vector2 InitialDestination;
		public Vector2 Destination;
		public Vector2 Velocity;
		public const float TelegraphTotalTime = 55f;
		public const float TelegraphFadeTime = 15f;
		public const float TelegraphWidth = 2800f;
		public const float LaserVelocity = 10f;

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Plasma Laser");
			Main.projFrames[projectile.type] = 3;
		}

		public override void SetDefaults()
		{
			projectile.width = 22;
			projectile.height = 22;
			projectile.hostile = true;
			projectile.ignoreWater = true;
			projectile.tileCollide = false;
			projectile.alpha = 255;
			projectile.penetrate = -1;
			projectile.extraUpdates = 1;
			projectile.timeLeft = 600;
			projectile.Calamity().canBreakPlayerDefense = true;
			cooldownSlot = 1;
		}

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write(ExplodeCountdown);
			writer.Write(InitialSpeed);
			writer.WriteVector2(Destination);
			writer.WriteVector2(Velocity);
			writer.WriteVector2(InitialDestination);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			ExplodeCountdown = reader.ReadInt32();
			InitialSpeed = reader.ReadSingle();
			Destination = reader.ReadVector2();
			Velocity = reader.ReadVector2();
			InitialDestination = reader.ReadVector2();
		}

		public override void AI()
		{
			projectile.frameCounter++;
			if (projectile.frameCounter > 12)
			{
				projectile.frame++;
				projectile.frameCounter = 0;
			}
			if (projectile.frame >= Main.projFrames[projectile.type])
				projectile.frame = 0;

			Lighting.AddLight(projectile.Center, 0.6f, 0f, 0f);

			// Die if the thing to attach to disappears.
			if (ThingToAttachTo is null || !ThingToAttachTo.active)
			{
				projectile.Kill();
				return;
			}

			if (InitialSpeed == 0f)
				InitialSpeed = projectile.velocity.Length();

			// Fade in after telegraphs have faded.
			if (TelegraphDelay > TelegraphTotalTime)
			{
				if (ExplodeCountdown == 0)
				{
					ExplodeCountdown = Main.rand.Next(45, 70);
					projectile.netUpdate = true;
				}
				else if (projectile.FinalExtraUpdate())
				{
					ExplodeCountdown--;
					if (Main.netMode != NetmodeID.MultiplayerClient && ExplodeCountdown <= 0)
					{
						// Release plasma bolts.
						int totalProjectiles = 3;
						int type = ModContent.ProjectileType<AresPlasmaBolt>();
						float offsetAngle = Main.rand.NextBool() ? 0f : MathHelper.Pi / totalProjectiles;
						Vector2 spinningPoint = Main.rand.NextVector2Circular(0.5f, 0.5f);
						for (int i = 0; i < totalProjectiles; i++)
						{
							Vector2 shootVelocity = spinningPoint.RotatedBy(MathHelper.TwoPi / totalProjectiles * i + offsetAngle);
							Projectile.NewProjectile(projectile.Center, shootVelocity, type, projectile.damage, 0f, Main.myPlayer);
						}
						projectile.Kill();
					}
				}

				if (ExplodeCountdown > 35)
					projectile.alpha -= 25;
				else if (projectile.FinalExtraUpdate())
					projectile.alpha += 4;

				projectile.alpha = Utils.Clamp(projectile.alpha, 0, 255);

				// If a velocity is in reserve, set the true velocity to it and make it as "taken" by setting it to <0,0>
				if (Velocity != Vector2.Zero)
				{
					projectile.extraUpdates = 3;
					projectile.velocity = Velocity;
					Velocity = Vector2.Zero;
					projectile.netUpdate = true;
				}

				// Direction and rotation.
				if (projectile.velocity.X < 0f)
				{
					projectile.spriteDirection = -1;
					projectile.rotation = projectile.velocity.ToRotation() + MathHelper.Pi;
				}
				else
				{
					projectile.spriteDirection = 1;
					projectile.rotation = projectile.velocity.ToRotation();
				}
			}
			else if (Destination == Vector2.Zero)
			{
				// Set start of telegraph to the npc center.
				projectile.Center = ThingToAttachTo.Center;

				// Set destination of the laser, the target's center.
				Destination = InitialDestination;

				// Calculate and store the velocity that will be used for laser telegraph rotation and beam firing.
				Vector2 projectileDestination = Destination - ThingToAttachTo.Center;
				Velocity = Vector2.Normalize(projectileDestination) * InitialSpeed;

				// Set velocity to zero.
				projectile.velocity = Vector2.Zero;
				projectile.netUpdate = true;

				// Direction and rotation.
				if (projectile.velocity.X < 0f)
				{
					projectile.spriteDirection = -1;
					projectile.rotation = projectile.velocity.ToRotation() + MathHelper.Pi;
				}
				else
				{
					projectile.spriteDirection = 1;
					projectile.rotation = projectile.velocity.ToRotation();
				}
			}
			else
			{
				// Set start of telegraph to the npc center.
				projectile.Center = ThingToAttachTo.Center;

				// Calculate and store the velocity that will be used for laser telegraph rotation and beam firing.
				Vector2 projectileDestination = Destination - ThingToAttachTo.Center;
				Velocity = Vector2.Normalize(projectileDestination) * InitialSpeed;

				// Direction and rotation.
				if (projectile.velocity.X < 0f)
				{
					projectile.spriteDirection = -1;
					projectile.rotation = projectile.velocity.ToRotation() + MathHelper.Pi;
				}
				else
				{
					projectile.spriteDirection = 1;
					projectile.rotation = projectile.velocity.ToRotation();
				}
			}

			TelegraphDelay++;
		}

		public override bool CanHitPlayer(Player target) => TelegraphDelay > TelegraphTotalTime;

		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			if (TelegraphDelay > TelegraphTotalTime)
				target.AddBuff(BuffID.CursedInferno, 180);
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
		{
			target.Calamity().lastProjectileHit = projectile;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			return CalamityUtils.CircularHitboxCollision(projHitbox.Center(), projectile.Size.Length() * 0.5f, targetHitbox);
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			if (TelegraphDelay >= TelegraphTotalTime)
			{
				lightColor.R = (byte)(255 * projectile.Opacity);
				lightColor.G = (byte)(255 * projectile.Opacity);
				lightColor.B = (byte)(255 * projectile.Opacity);
				lightColor = Color.Lerp(lightColor, new Color(1f, 1f, 1f, 0f), (float)Math.Sin(PulseFlash) * 0.5f + 0.5f);
				Vector2 drawOffset = projectile.velocity.SafeNormalize(Vector2.Zero) * -30f;
				projectile.Center += drawOffset;
				CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor, 1);
				projectile.Center -= drawOffset;
				return false;
			}

			Texture2D laserTelegraph = ModContent.GetTexture("CalamityMod/ExtraTextures/LaserWallTelegraphBeam");

			float yScale = 2f;
			if (TelegraphDelay < TelegraphFadeTime)
				yScale = MathHelper.Lerp(0f, 2f, TelegraphDelay / 15f);
			if (TelegraphDelay > TelegraphTotalTime - TelegraphFadeTime)
				yScale = MathHelper.Lerp(2f, 0f, (TelegraphDelay - (TelegraphTotalTime - TelegraphFadeTime)) / 15f);

			Vector2 scaleInner = new Vector2(TelegraphWidth / laserTelegraph.Width, yScale);
			Vector2 origin = laserTelegraph.Size() * new Vector2(0f, 0.5f);
			Vector2 scaleOuter = scaleInner * new Vector2(1f, 2.2f);

			Color colorOuter = Color.Lerp(Color.Lime, Color.LimeGreen, TelegraphDelay / TelegraphTotalTime * 2f % 1f);
			colorOuter = Color.Lerp(colorOuter, new Color(1f, 1f, 1f, 0f), (float)Math.Sin(PulseFlash) * 0.5f + 0.5f);
			Color colorInner = Color.Lerp(colorOuter, Color.White, 0.75f);

			colorOuter *= 0.6f;
			colorInner *= 0.6f;

			spriteBatch.Draw(laserTelegraph, projectile.Center - Main.screenPosition, null, colorInner, Velocity.ToRotation(), origin, scaleInner, SpriteEffects.None, 0f);
			spriteBatch.Draw(laserTelegraph, projectile.Center - Main.screenPosition, null, colorOuter, Velocity.ToRotation(), origin, scaleOuter, SpriteEffects.None, 0f);
			return false;
		}
	}
}
