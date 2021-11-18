using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
	public class AresBodyBehaviorOverride : NPCBehaviorOverride
	{
		public enum AresBodyFrameType
		{
			Normal,
			Laugh
		}

		public enum AresBodyAttackType
		{
			IdleHover
		}

		public override int NPCOverrideType => ModContent.NPCType<AresBody>();

		public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

		#region AI
		public override bool PreAI(NPC npc)
		{
			// Define the life ratio.
			float lifeRatio = npc.life / (float)npc.lifeMax;

			// Define the whoAmI variable.
			CalamityGlobalNPC.draedonExoMechPrime = npc.whoAmI;

			// Reset frame states.
			ref float frameType = ref npc.localAI[0];
			frameType = (int)AresBodyFrameType.Normal;

			// Define attack variables.
			ref float attackState = ref npc.ai[0];
			ref float attackTimer = ref npc.ai[1];
			ref float armsHasBeenSummoned = ref npc.ai[3];

			if (armsHasBeenSummoned == 0f)
			{
				int totalArms = 4;
				int previous = npc.whoAmI;
				for (int i = 0; i < totalArms; i++)
				{
					int lol = 0;
					switch (i)
					{
						case 0:
							lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AresLaserCannon>(), npc.whoAmI);
							break;
						case 1:
							lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AresPlasmaFlamethrower>(), npc.whoAmI);
							break;
						case 2:
							lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AresTeslaCannon>(), npc.whoAmI);
							break;
						case 3:
							lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AresGaussNuke>(), npc.whoAmI);
							break;
						default:
							break;
					}

					Main.npc[lol].realLife = npc.whoAmI;
					Main.npc[previous].netUpdate = true;
					previous = lol;
				}
				armsHasBeenSummoned = 1f;
			}

			// Get a target.
			npc.TargetClosest(false);
			Player target = Main.player[npc.target];

			// Perform specific behaviors.
			switch ((AresBodyAttackType)(int)attackState)
			{
				case AresBodyAttackType.IdleHover:
					DoBehavior_IdleHover(npc, target);
					break;
			}

			return false;
		}

		public static void DoBehavior_IdleHover(NPC npc, Player target)
		{
			// Fade in.
			npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);

			Vector2 hoverDestination = target.Center - Vector2.UnitY * 400f;
			DoHoverMovement(npc, hoverDestination, 24f, 75f);
		}

		public static void DoHoverMovement(NPC npc, Vector2 destination, float flySpeed, float hyperSpeedCap)
		{
			float distanceFromDestination = npc.Distance(destination);
			float hyperSpeedInterpolant = Utils.InverseLerp(50f, 2400f, distanceFromDestination, true);

			// Scale up velocity over time if too far from destination.
			float speedUpFactor = Utils.InverseLerp(50f, 1600f, npc.Distance(destination), true) * 1.76f;
			flySpeed *= 1f + speedUpFactor;

			// Reduce speed when very close to the destination, to prevent swerving movement.
			if (flySpeed > distanceFromDestination)
				flySpeed = distanceFromDestination;

			// Define the max velocity.
			Vector2 maxVelocity = (destination - npc.Center) / 24f;
			if (maxVelocity.Length() > hyperSpeedCap)
				maxVelocity = maxVelocity.SafeNormalize(Vector2.Zero) * hyperSpeedCap;

			npc.velocity = Vector2.Lerp(npc.SafeDirectionTo(destination) * flySpeed, maxVelocity, hyperSpeedInterpolant);
		}
		#endregion AI

		#region Frames and Drawcode
		public override void FindFrame(NPC npc, int frameHeight)
		{
			int framesInNormalState = 11;
			ref float currentFrame = ref npc.localAI[2];

			npc.frameCounter++;
			switch ((AresBodyFrameType)(int)npc.localAI[0])
			{
				case AresBodyFrameType.Normal:
					if (npc.frameCounter >= 6D)
					{
						// Reset the frame counter.
						npc.frameCounter = 0D;

						// Increment the frame.
						currentFrame++;

						// Reset the frames to frame 0 after the animation cycle for the normal phase has concluded.
						if (currentFrame > framesInNormalState)
							currentFrame = 0;
					}
					break;
				case AresBodyFrameType.Laugh:
					if (currentFrame <= 35 || currentFrame > 47)
						currentFrame = 36f;

					if (npc.frameCounter >= 6D)
					{
						// Reset the frame counter.
						npc.frameCounter = 0D;

						// Increment the frame.
						currentFrame++;
					}
					break;
			}

			npc.frame = new Rectangle(npc.width * (int)(currentFrame / 8), npc.height * (int)(currentFrame % 8), npc.width, npc.height);
		}

		public static MethodInfo DrawArmFunction = typeof(AresBody).GetMethod("DrawArm", BindingFlags.Public | BindingFlags.Instance);

		public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
		{
			// Draw arms.
			int laserArm = NPC.FindFirstNPC(ModContent.NPCType<AresLaserCannon>());
			int gaussArm = NPC.FindFirstNPC(ModContent.NPCType<AresGaussNuke>());
			int teslaArm = NPC.FindFirstNPC(ModContent.NPCType<AresTeslaCannon>());
			int plasmaArm = NPC.FindFirstNPC(ModContent.NPCType<AresPlasmaFlamethrower>());
			Color afterimageBaseColor = Color.White;
			Color armGlowmaskColor = afterimageBaseColor;
			armGlowmaskColor.A = 184;

			(int, bool)[] armProperties = new (int, bool)[]
			{
				// Gauss arm.
				(1, true),

				// Laser arm.
				(-1, true),

				// Telsa arm.
				(-1, false),

				// Plasma arm.
				(1, false),
			};

			if (laserArm != -1)
				DrawArmFunction.Invoke(npc.modNPC, new object[] { spriteBatch, Main.npc[laserArm].Center, armGlowmaskColor, armProperties[0].Item1, armProperties[0].Item2 });
			if (gaussArm != -1)
				DrawArmFunction.Invoke(npc.modNPC, new object[] { spriteBatch, Main.npc[gaussArm].Center, armGlowmaskColor, armProperties[1].Item1, armProperties[1].Item2 });
			if (teslaArm != -1)
				DrawArmFunction.Invoke(npc.modNPC, new object[] { spriteBatch, Main.npc[teslaArm].Center, armGlowmaskColor, armProperties[2].Item1, armProperties[2].Item2 });
			if (plasmaArm != -1)
				DrawArmFunction.Invoke(npc.modNPC, new object[] { spriteBatch, Main.npc[plasmaArm].Center, armGlowmaskColor, armProperties[3].Item1, armProperties[3].Item2 });

			Texture2D texture = Main.npcTexture[npc.type];
			Rectangle frame = npc.frame;
			Vector2 origin = frame.Size() * 0.5f;
			int numAfterimages = 5;

			if (CalamityConfig.Instance.Afterimages)
			{
				for (int i = 1; i < numAfterimages; i += 2)
				{
					Color afterimageColor = lightColor;
					afterimageColor = Color.Lerp(afterimageColor, afterimageBaseColor, 0.5f);
					afterimageColor = npc.GetAlpha(afterimageColor);
					afterimageColor *= (numAfterimages - i) / 15f;
					Vector2 afterimageCenter = npc.oldPos[i] + npc.frame.Size() * 0.5f - Main.screenPosition;
					spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, SpriteEffects.None, 0f);
				}
			}

			Vector2 center = npc.Center - Main.screenPosition;
			spriteBatch.Draw(texture, center, frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);

			texture = ModContent.GetTexture("CalamityMod/NPCs/ExoMechs/Ares/AresBodyGlow");

			if (CalamityConfig.Instance.Afterimages)
			{
				for (int i = 1; i < numAfterimages; i += 2)
				{
					Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
					Vector2 afterimageCenter = npc.oldPos[i] + npc.frame.Size() * 0.5f - Main.screenPosition;
					spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, SpriteEffects.None, 0f);
				}
			}

			spriteBatch.Draw(texture, center, frame, afterimageBaseColor * npc.Opacity, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);

			return false;
		}
		#endregion Frames and Drawcode
	}
}
