using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityMod.Items.Potions;
using System;
using Terraria.Graphics.Shaders;

namespace InfernumMode.FuckYouModeAIs.Draedon
{
	[AutoloadBossHead]
    public class DraedonMechBoss : ModNPC
	{
		public override bool Autoload(ref string name) => false;
		#region Enumerations
		public enum FrameDrawState
		{
			Standing,
			Flying,
			Running,
			Rocket
		}

		public enum AttackState
		{
			BattleStartDelay,
			RocketCharges,
			RegislashDashes,
			Count
		}

		public int FrameX = 0;
		public int FrameY = 0;
		public int CurrentFrame
		{
			get => FrameY + FrameX * 7;
			set
			{
				FrameX = value / 7;
				FrameY = value % 7;
			}
		}
		#endregion

		#region Fields and Properties

		public PrimitiveTrail RocketTrail;

		public AttackState CurrentAttackState
		{
			get => (AttackState)(int)npc.ai[0];
			set => npc.ai[0] = (int)value;
		}

		public int AttackTimer
		{
			get => (int)npc.ai[1];
			set => npc.ai[1] = value;
		}

		public FrameDrawState CurrentFrameDrawState
		{
			get => (FrameDrawState)(int)npc.localAI[0];
			set => npc.localAI[0] = (int)value;
		}

		public ref float ArmorIntensity => ref npc.localAI[1];

		public ref float ArmorVibrancy => ref npc.localAI[2];

		public Player Target => Main.player[npc.target];
		#endregion

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Annihil MK3");
            Main.npcFrameCount[npc.type] = 7;
			NPCID.Sets.TrailCacheLength[npc.type] = 20;
			NPCID.Sets.TrailingMode[npc.type] = 1;
		}
		
		public override void SetDefaults()
		{
			npc.width = 154;
			npc.height = 200;
			npc.aiStyle = aiType = -1;
			npc.damage = 500;
			npc.defense = 120;
			npc.lifeMax = 4900000;
			npc.lifeMax += (int)(npc.lifeMax * CalamityConfig.Instance.BossHealthBoost * 0.01);
			npc.knockBackResist = 0f;
            npc.noTileCollide = false;
            npc.noGravity = false;
            npc.npcSlots = 50f;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath37;
			npc.value = Item.buyPrice(2, 0, 0, 0);
			npc.boss = true;
            npc.netAlways = true;
            npc.timeLeft = NPC.activeTime * 30;
			music = MusicID.Boss3;
            for (int k = 0; k < npc.buffImmune.Length; k++)
                npc.buffImmune[k] = true;
		}

		#region AI
		internal void ResetHitboxBounds()
		{
			npc.width = CurrentFrameDrawState == FrameDrawState.Rocket ? 46 : 154;
			npc.height = CurrentFrameDrawState == FrameDrawState.Rocket ? 46 : 200;
		}
		public override void AI()
        {
			npc.TargetClosest();

			// Adjust hitbox bounds depending on the current draw state.
			ResetHitboxBounds();

			// Readjust damage every frame.
			npc.damage = npc.defDamage;

			// Attack AIs.
			switch (CurrentAttackState)
			{
				case AttackState.BattleStartDelay:
					int startupTime = 240;
					int lungeTime = 120;
					int fadeinTime = 60;
					float lungeSpeed = 40f;

					// Fade in and generate a shield.
					if (AttackTimer <= fadeinTime)
					{
						npc.Opacity = AttackTimer / (float)fadeinTime;
						ArmorIntensity = npc.Opacity * 0.35f;
						ArmorVibrancy = npc.Opacity * 1.4f;
					}

					// Leap and hover in the air for a bit.
					if (AttackTimer == lungeTime)
					{
						npc.velocity = Vector2.UnitY * -lungeSpeed;
						npc.noGravity = true;
						npc.noTileCollide = true;
						npc.netUpdate = true;
						Collision.HitTiles(npc.Bottom - npc.velocity, npc.velocity, 500, 40);

						CurrentFrameDrawState = FrameDrawState.Flying;
					}

					// Slow-down effects.
					if (AttackTimer > lungeTime)
						npc.velocity *= 0.965f;

					if (AttackTimer > startupTime)
					{
						AttackTimer = 0;
						CurrentAttackState = AttackState.RegislashDashes;
						ArmorIntensity = 0f;
						ArmorVibrancy = 0f;
						npc.netUpdate = true;
						return;
					}
					break;

				case AttackState.RocketCharges:
					CurrentFrameDrawState = FrameDrawState.Rocket;
					int chargeRate = 120;
					int redirectTime = 80;
					if (AttackTimer % chargeRate == 1)
					{
						float oldSpeed = npc.velocity.Length();
						npc.velocity = Vector2.Lerp(npc.velocity, npc.DirectionTo(Target.Center) * 30f, npc.velocity.Length() < 5f ? 1f : 0.2f);
						npc.velocity = npc.velocity.SafeNormalize(-Vector2.UnitY) * MathHelper.Clamp(oldSpeed * 1.7f, 23f, 32f);

						if (Main.netMode != NetmodeID.MultiplayerClient)
						{
							for (int i = 0; i < 12; i++)
							{
								Vector2 shootVelocity = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 16f;
								Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<CalamityMod.Projectiles.Boss.DoGDeath>(), 500, 0f);
							}
						}

						npc.netUpdate = true;
					}
					
					// Redirecting movement.
					if (AttackTimer % chargeRate >= chargeRate - redirectTime && !npc.WithinRange(Target.Center, 320f))
					{
						// Slow down and aim towards the target if target isn't in the line of sight of the rocket.
						if (Vector2.Dot(npc.DirectionTo(Target.Center), npc.velocity.SafeNormalize(Vector2.Zero)) < 0.8f)
						{
							npc.velocity *= 0.985f;
							npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(Target.Center), MathHelper.Pi / 30f);
						}
						// Otherwise adjust speed to fly towards the target.
						else
							npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), 26f, 0.06f);

						// Quickly fly towards the target if they're nearby.
						if (npc.WithinRange(Target.Center, 560f))
							npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(Target.Center), MathHelper.Pi / 10f);

						if (npc.velocity.Length() > 27f)
							npc.velocity *= 0.996f;
					}

					npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
					break;

				case AttackState.RegislashDashes:
					int movementRedirectTime = 60;
					break;
			}
			AttackTimer++;
		}
		#endregion

		#region Expert Stats
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = 5697500;
			npc.damage = 600;
		}
		#endregion

		#region Drawing
		public override void FindFrame(int frameHeight)
		{
			switch (CurrentFrameDrawState)
			{
				case FrameDrawState.Standing:
					CurrentFrame = 0;
					break;
				case FrameDrawState.Flying:
                    // Falling
                    if (npc.velocity.Y > 0f)
                    {
                        CurrentFrame = 1;
                        return;
                    }
                    // Quickly go to flying frames
                    if (CurrentFrame == 0)
                    {
                        if (npc.frameCounter >= 4)
                        {
                            CurrentFrame++;
                            npc.frameCounter = 0;
                        }
                    }
                    // Flying frames
                    else if (npc.frameCounter % 4 == 3)
                    {
                        CurrentFrame++;
                        if (CurrentFrame >= 6)
                            CurrentFrame = 2;
                    }

                    // Clamp frames
                    if (CurrentFrame >= 6)
                        CurrentFrame = 0;
                    break;
				case FrameDrawState.Running:
					int stepRate = (int)MathHelper.Clamp(8 - Math.Abs(npc.velocity.X) / 3.5f, 1, 8);
					if (npc.velocity.X == 0f)
					{
						CurrentFrame = 0;
					}
					else if (npc.frameCounter >= stepRate)
					{
						npc.frameCounter = 0;
						CurrentFrame++;
						if (CurrentFrame >= 13)
						{
							CurrentFrame = 6;
						}
					}
					// Clamp frames
					if (CurrentFrame >= 14 || CurrentFrame <= 6)
						CurrentFrame = 6;
					break;
			}
		}

		internal Color ColorFunction(float completionRatio)
		{
			return Color.Cyan * (float)Math.Pow(1f - completionRatio, 3f);
		}

		internal float WidthFunction(float completionRatio)
		{
			return 10f - 10f * (1f - (float)Math.Cos(MathHelper.Pi * completionRatio)) / 2f;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
			Texture2D robotTexture = ModContent.GetTexture(Texture);
			Texture2D robotGlowTexture = ModContent.GetTexture($"{Texture}Glow");
			Rectangle frame = new Rectangle(FrameX * robotTexture.Width / 3, FrameY * robotTexture.Height / 7, robotTexture.Width / 3, robotTexture.Height / 7);

			spriteBatch.EnterShaderRegion();

			// Apply a super special shader.
			MiscShaderData gradientShader = GameShaders.Misc["Infernum:PristineArmorShader"];
			gradientShader.SetShaderTexture(ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Providence/ProvidenceShaderTexture"));
			gradientShader.Shader.Parameters["uIntensity"].SetValue(ArmorIntensity);
			gradientShader.Shader.Parameters["uVibrancy"].SetValue(ArmorVibrancy);
			gradientShader.UseSaturation(0.9f);
			gradientShader.Apply();

			if (CurrentFrameDrawState != FrameDrawState.Rocket)
			{
				spriteBatch.Draw(robotTexture,
							 npc.Center - Vector2.UnitY * 6f - Main.screenPosition,
							 frame,
							 npc.GetAlpha(Color.White),
							 npc.rotation,
							 robotTexture.Frame(3, 7, FrameX, FrameY).Size() * 0.5f,
							 npc.scale,
							 npc.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
							 0f);

				spriteBatch.ExitShaderRegion();
			}
			else
			{
				spriteBatch.ExitShaderRegion();
				if (RocketTrail is null)
					RocketTrail = new PrimitiveTrail(WidthFunction, ColorFunction);

				if (AttackTimer > 25)
					RocketTrail.Draw(npc.oldPos, npc.Size * 0.5f + npc.velocity.SafeNormalize(Vector2.Zero) * 15f - Main.screenPosition, 200);

				Texture2D rocketTexture = ModContent.GetTexture("CalamityMod/ExtraTextures/AndromedaBolt");
				spriteBatch.Draw(rocketTexture,
							 npc.Center - Main.screenPosition,
							 rocketTexture.Frame(1, 4, 0, (int)(Main.GlobalTime * 12f) % 4),
							 npc.GetAlpha(Color.White),
							 npc.rotation,
							 rocketTexture.Frame(1, 4, 0, 0).Size() * 0.5f,
							 npc.scale,
							 SpriteEffects.None,
							 0f);
			}

			if (CurrentFrameDrawState != FrameDrawState.Rocket)
			{
				spriteBatch.Draw(robotGlowTexture,
							 npc.Center - Vector2.UnitY * 6f - Main.screenPosition,
							 frame,
							 npc.GetAlpha(Color.White),
							 npc.rotation,
							 robotTexture.Frame(3, 7, FrameX, FrameY).Size() * 0.5f,
							 npc.scale,
							 npc.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
							 0f);
			}
			return false;
        }
		#endregion

		#region Damage Manipulation

		public override bool StrikeNPC(ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit)
		{
			damage *= 1f - Utils.InverseLerp(0f, 0.35f, ArmorIntensity, true);
			if (ArmorIntensity >= 0.34f)
				damage = 0D;

			if (damage <= 0D)
			{
				crit = false;
				return false;
			}
			
			return true;
		}

		public override bool CanHitPlayer(Player target, ref int cooldownSlot)
		{
			cooldownSlot = 1;
			return true;
		}

		public override void OnHitPlayer(Player player, int damage, bool crit) => player.AddBuff(BuffID.Electrified, 180, true);
		#endregion

		#region Loot
		public override void BossLoot(ref string name, ref int potionType) => potionType = ModContent.ItemType<SupremeHealingPotion>();
		#endregion
	}
}
