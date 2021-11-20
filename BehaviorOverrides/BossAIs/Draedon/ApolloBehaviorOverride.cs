using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
	public class ApolloBehaviorOverride : NPCBehaviorOverride
	{
		public enum ApolloFrameType
		{
			IdleAnimation,
			ChargeupAnimation,
			AttackAnimation,
			IdleAnimationPhase2,
			ChargeupAnimationPhase2,
			AttackAnimationPhase2,
		}

		public enum TwinsAttackType
		{
			VanillaShots,
			FireCharge
		}

		public override int NPCOverrideType => ModContent.NPCType<Apollo>();

		public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

		#region AI
		public override bool PreAI(NPC npc)
		{
			// Define the life ratio.
			float lifeRatio = npc.life / (float)npc.lifeMax;

			// Define the whoAmI variable.
			CalamityGlobalNPC.draedonExoMechTwinGreen = npc.whoAmI;

			// Reset frame states.
			ref float frameType = ref npc.localAI[0];
			frameType = (int)ApolloFrameType.IdleAnimation;

			// Define attack variables.
			ref float attackState = ref npc.ai[0];
			ref float attackTimer = ref npc.ai[1];
			ref float hoverSide = ref npc.ai[2];
			ref float frame = ref npc.localAI[0];

			// Fade in.
			npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);

			// Define the initial hover side.
			if (hoverSide == 0f)
			{
				hoverSide = 1f;
				npc.netUpdate = true;
			}

			// Get a target.
			npc.TargetClosest(false);
			Player target = Main.player[npc.target];

			// Despawn if the target is gone.
			if (!target.active || target.dead)
			{
				npc.TargetClosest(false);
				target = Main.player[npc.target];
				if (!target.active || target.dead)
					npc.active = false;
			}

			switch ((TwinsAttackType)(int)attackState)
			{
				case TwinsAttackType.VanillaShots:
					DoBehavior_ReleaseSplittingPlasmaShots(npc, target, hoverSide, ref frame, ref attackTimer);
					break;
				case TwinsAttackType.FireCharge:
					DoBehavior_FireCharge(npc, target, hoverSide, ref frame, ref attackTimer);
					break;
			}

			attackTimer++;
			return false;
		}

		public static void DoBehavior_ReleaseSplittingPlasmaShots(NPC npc, Player target, float hoverSide, ref float frame, ref float attackTimer)
		{
			int totalShots = 8;
			float shootRate = 60f;
			float plasmaShootSpeed = 13.5f;
			float predictivenessFactor = 18.5f;
			Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * predictivenessFactor);
			ref float hoverOffsetX = ref npc.Infernum().ExtraAI[0];
			ref float hoverOffsetY = ref npc.Infernum().ExtraAI[1];
			ref float shootCounter = ref npc.Infernum().ExtraAI[2];
			Vector2 hoverDestination = target.Center + Vector2.UnitX * hoverSide * 780f;
			hoverDestination.X += hoverOffsetX;
			hoverDestination.Y += hoverOffsetY;

			// Determine rotation.
			npc.rotation = aimDirection.ToRotation() + MathHelper.PiOver2;

			// Fire a plasma burst and select a new offset.
			AresBodyBehaviorOverride.DoHoverMovement(npc, hoverDestination, 30f, 84f);
			if (attackTimer >= shootRate)
			{
				Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaCasterFire"), npc.Center);

				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					Vector2 plasmaShootVelocity = aimDirection * plasmaShootSpeed;
					int plasma = Utilities.NewProjectileBetter(npc.Center + aimDirection * 70f, plasmaShootVelocity, ModContent.ProjectileType<ApolloPlasmaFireball>(), 550, 0f);
					if (Main.projectile.IndexInRange(plasma))
						Main.projectile[plasma].ai[0] = shootCounter % 2f;
				}

				hoverOffsetX = Main.rand.NextFloat(-50f, 50f);
				hoverOffsetY = Main.rand.NextFloat(-250f, 250f);
				attackTimer = 0f;
				shootCounter++;
				npc.netUpdate = true;
			}

			// Calculate frames.
			frame = (int)Math.Round(MathHelper.Lerp(20f, 29f, attackTimer / shootRate));

			if (shootCounter >= totalShots)
				SelectNextAttack(npc);
		}

		public static void DoBehavior_FireCharge(NPC npc, Player target, float hoverSide, ref float frame, ref float attackTimer)
		{
			int waitTime = 16;
			int chargeTime = 45;
			float chargeSpeed = 37f;
			ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
			Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 560f, -450f);

			switch ((int)attackSubstate)
			{
				// Hover into position.
				case 0:
					npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

					// Hover to the top left/right of the target.
					AresBodyBehaviorOverride.DoHoverMovement(npc, hoverDestination, 30f, 84f);

					// Once sufficiently close, go to the next attack substate.
					if (npc.WithinRange(hoverDestination, 20f))
					{
						npc.velocity = Vector2.Zero;
						attackSubstate = 1f;
						attackTimer = 0f;
						npc.netUpdate = true;
					}
					break;

				// Wait in place for a short period of time.
				case 1:
					npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;
					if (attackTimer >= waitTime)
					{
						npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
						attackSubstate = 1f;
						attackTimer = 0f;
						npc.netUpdate = true;
					}
					break;

				// Release fire.
				case 2:
					break;
			}
		}

		public static void SelectNextAttack(NPC npc)
		{
			TwinsAttackType oldAttackType = (TwinsAttackType)(int)npc.ai[0];
			npc.ai[0] = (int)TwinsAttackType.VanillaShots;
			if (oldAttackType == TwinsAttackType.VanillaShots)
				npc.ai[0] = (int)TwinsAttackType.FireCharge;

			npc.ai[1] = 0f;
			for (int i = 0; i < 5; i++)
				npc.Infernum().ExtraAI[i] = 0f;

			npc.netUpdate = true;
		}

		#endregion AI

		#region Frames and Drawcode
		public override void FindFrame(NPC npc, int frameHeight)
		{
			int frameX = (int)npc.localAI[0] / 9;
			int frameY = (int)npc.localAI[0] % 9;
			npc.frame = new Rectangle(npc.width * frameX, npc.height * frameY, npc.width, npc.height);
		}

		public static float FlameTrailWidthFunction(NPC npc, float completionRatio) => MathHelper.SmoothStep(21f, 8f, completionRatio) * npc.ModNPC<Apollo>().ChargeComboFlash;

		public static float FlameTrailWidthFunctionBig(NPC npc, float completionRatio) => MathHelper.SmoothStep(34f, 12f, completionRatio) * npc.ModNPC<Apollo>().ChargeComboFlash;

		public float RibbonTrailWidthFunction(float completionRatio)
		{
			float baseWidth = Utils.InverseLerp(1f, 0.54f, completionRatio, true) * 5f;
			float endTipWidth = CalamityUtils.Convert01To010(Utils.InverseLerp(0.96f, 0.89f, completionRatio, true)) * 2.4f;
			return baseWidth + endTipWidth;
		}

		public static Color FlameTrailColorFunction(NPC npc, float completionRatio)
		{
			float trailOpacity = Utils.InverseLerp(0.8f, 0.27f, completionRatio, true) * Utils.InverseLerp(0f, 0.067f, completionRatio, true);
			Color startingColor = Color.Lerp(Color.White, Color.Cyan, 0.27f);
			Color middleColor = Color.Lerp(Color.Orange, Color.ForestGreen, 0.74f);
			Color endColor = Color.Lime;
			return CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * npc.ModNPC<Apollo>().ChargeComboFlash * trailOpacity;
		}

		public static Color FlameTrailColorFunctionBig(NPC npc, float completionRatio)
		{
			float trailOpacity = Utils.InverseLerp(0.8f, 0.27f, completionRatio, true) * Utils.InverseLerp(0f, 0.067f, completionRatio, true) * 0.56f;
			Color startingColor = Color.Lerp(Color.White, Color.Cyan, 0.25f);
			Color middleColor = Color.Lerp(Color.Blue, Color.White, 0.35f);
			Color endColor = Color.Lerp(Color.DarkBlue, Color.White, 0.47f);
			Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * npc.ModNPC<Apollo>().ChargeComboFlash * trailOpacity;
			color.A = 0;
			return color;
		}

		public static Color RibbonTrailColorFunction(NPC npc, float completionRatio)
		{
			Color startingColor = new Color(34, 40, 48);
			Color endColor = new Color(40, 160, 32);
			return Color.Lerp(startingColor, endColor, (float)Math.Pow(completionRatio, 1.5D)) * npc.Opacity;
		}

		public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
		{
			// Declare the trail drawers if they have yet to be defined.
			if (npc.ModNPC<Apollo>().ChargeFlameTrail is null)
				npc.ModNPC<Apollo>().ChargeFlameTrail = new PrimitiveTrail(c => FlameTrailWidthFunction(npc, c), c => FlameTrailColorFunction(npc, c), null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

			if (npc.ModNPC<Apollo>().ChargeFlameTrailBig is null)
				npc.ModNPC<Apollo>().ChargeFlameTrailBig = new PrimitiveTrail(c => FlameTrailWidthFunctionBig(npc, c), c => FlameTrailColorFunctionBig(npc, c), null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

			if (npc.ModNPC<Apollo>().RibbonTrail is null)
				npc.ModNPC<Apollo>().RibbonTrail = new PrimitiveTrail(RibbonTrailWidthFunction, c => RibbonTrailColorFunction(npc, c));

			// Prepare the flame trail shader with its map texture.
			GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.GetTexture("CalamityMod/ExtraTextures/ScarletDevilStreak"));

			int numAfterimages = npc.ModNPC<Apollo>().ChargeComboFlash > 0f ? 0 : 5;
			Texture2D texture = Main.npcTexture[npc.type];
			Rectangle frame = npc.frame;
			Vector2 origin = npc.Size * 0.5f;
			Vector2 center = npc.Center - Main.screenPosition;
			Color afterimageBaseColor = Color.White;

			// Draws a single instance of a regular, non-glowmask based Artemis.
			// This is created to allow easy duplication of them when drawing the charge.
			void drawInstance(Vector2 drawOffset, Color baseColor)
			{
				if (CalamityConfig.Instance.Afterimages)
				{
					for (int i = 1; i < numAfterimages; i += 2)
					{
						Color afterimageColor = npc.GetAlpha(Color.Lerp(baseColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
						Vector2 afterimageCenter = npc.oldPos[i] + frame.Size() * 0.5f - Main.screenPosition;
						spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
					}
				}

				spriteBatch.Draw(texture, center + drawOffset, frame, npc.GetAlpha(baseColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
			}

			// Draw ribbons near the main thruster
			for (int direction = -1; direction <= 1; direction += 2)
			{
				Vector2 ribbonOffset = -Vector2.UnitY.RotatedBy(npc.rotation) * 14f;
				ribbonOffset += Vector2.UnitX.RotatedBy(npc.rotation) * direction * 26f;

				float currentSegmentRotation = npc.rotation;
				List<Vector2> ribbonDrawPositions = new List<Vector2>();
				for (int i = 0; i < 12; i++)
				{
					float ribbonCompletionRatio = i / 12f;
					float wrappedAngularOffset = MathHelper.WrapAngle(npc.oldRot[i + 1] - currentSegmentRotation) * 0.3f;
					float segmentRotationOffset = MathHelper.Clamp(wrappedAngularOffset, -0.12f, 0.12f);

					// Add a sinusoidal offset that goes based on time and completion ratio to create a waving-flag-like effect.
					// This is dampened for the first few points to prevent weird offsets. It is also dampened by high velocity.
					float sinusoidalRotationOffset = (float)Math.Sin(ribbonCompletionRatio * 2.22f + Main.GlobalTime * 3.4f) * 1.36f;
					float sinusoidalRotationOffsetFactor = Utils.InverseLerp(0f, 0.37f, ribbonCompletionRatio, true) * direction * 24f;
					sinusoidalRotationOffsetFactor *= Utils.InverseLerp(24f, 16f, npc.velocity.Length(), true);

					Vector2 sinusoidalOffset = Vector2.UnitY.RotatedBy(npc.rotation + sinusoidalRotationOffset) * sinusoidalRotationOffsetFactor;
					Vector2 ribbonSegmentOffset = Vector2.UnitY.RotatedBy(currentSegmentRotation) * ribbonCompletionRatio * 540f + sinusoidalOffset;
					ribbonDrawPositions.Add(npc.Center + ribbonSegmentOffset + ribbonOffset);

					currentSegmentRotation += segmentRotationOffset;
				}
				npc.ModNPC<Apollo>().RibbonTrail.Draw(ribbonDrawPositions, -Main.screenPosition, 66);
			}

			int instanceCount = (int)MathHelper.Lerp(1f, 15f, npc.ModNPC<Apollo>().ChargeComboFlash);
			Color baseInstanceColor = Color.Lerp(lightColor, Color.White, npc.ModNPC<Apollo>().ChargeComboFlash);
			baseInstanceColor.A = (byte)(int)(255f - npc.ModNPC<Apollo>().ChargeComboFlash * 255f);

			spriteBatch.EnterShaderRegion();

			drawInstance(Vector2.Zero, baseInstanceColor);
			if (instanceCount > 1)
			{
				baseInstanceColor *= 0.04f;
				float backAfterimageOffset = MathHelper.SmoothStep(0f, 2f, npc.ModNPC<Apollo>().ChargeComboFlash);
				for (int i = 0; i < instanceCount; i++)
				{
					Vector2 drawOffset = (MathHelper.TwoPi * i / instanceCount + Main.GlobalTime * 0.8f).ToRotationVector2() * backAfterimageOffset;
					drawInstance(drawOffset, baseInstanceColor);
				}
			}

			texture = ModContent.GetTexture("CalamityMod/NPCs/ExoMechs/Apollo/ApolloGlow");
			if (CalamityConfig.Instance.Afterimages)
			{
				for (int i = 1; i < numAfterimages; i += 2)
				{
					Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
					Vector2 afterimageCenter = npc.oldPos[i] + frame.Size() * 0.5f - Main.screenPosition;
					spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
				}
			}

			spriteBatch.Draw(texture, center, frame, Color.White * npc.Opacity, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);

			spriteBatch.ExitShaderRegion();

			// Draw a flame trail on the thrusters if needed. This happens during charges.
			if (npc.ModNPC<Apollo>().ChargeComboFlash > 0f)
			{
				for (int direction = -1; direction <= 1; direction++)
				{
					Vector2 baseDrawOffset = new Vector2(0f, direction == 0f ? 18f : 60f).RotatedBy(npc.rotation);
					baseDrawOffset += new Vector2(direction * 64f, 0f).RotatedBy(npc.rotation);

					float backFlameLength = direction == 0f ? 700f : 190f;
					Vector2 drawStart = npc.Center + baseDrawOffset;
					Vector2 drawEnd = drawStart - (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * npc.ModNPC<Apollo>().ChargeComboFlash * backFlameLength;
					Vector2[] drawPositions = new Vector2[]
					{
						drawStart,
						drawEnd
					};

					if (direction == 0)
					{
						for (int i = 0; i < 4; i++)
						{
							Vector2 drawOffset = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * 8f;
							npc.ModNPC<Apollo>().ChargeFlameTrailBig.Draw(drawPositions, drawOffset - Main.screenPosition, 70);
						}
					}
					else
						npc.ModNPC<Apollo>().ChargeFlameTrail.Draw(drawPositions, -Main.screenPosition, 70);
				}
			}

			return false;
		}
		#endregion Frames and Drawcode
	}
}
