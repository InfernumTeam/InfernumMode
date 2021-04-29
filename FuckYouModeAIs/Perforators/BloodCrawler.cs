using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs;
using InfernumMode.InverseKinematics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

namespace InfernumMode.FuckYouModeAIs.Perforators
{
	public class BloodCrawler : ModNPC
    {
        public Player Target => Main.player[npc.target];
        public bool OnSolidGround
		{
			get
			{
                int totalLegsOnSolidGround = 0;
                foreach (Vector2 legEndPosition in LegEndPositions)
                {
                    Tile belowTile = Framing.GetTileSafely(legEndPosition + Vector2.UnitY * 12f);
                    if (WorldGen.SolidTile(belowTile))
                        totalLegsOnSolidGround++;
                }
                return totalLegsOnSolidGround >= 3 && npc.WithinRange(LegEndPositions[4], 120f);
            }
        }
        public Vector2[] IdealLegEndPositions = new Vector2[6];
        public Vector2[] LegEndPositions = new Vector2[6];
        public Limb[][] LegLimbs = new Limb[6][];
        public bool InPhase2 => Main.npc[CalamityGlobalNPC.perfHive].ai[2] == 2f;
        public Vector2 LegsStartingCenter => npc.Bottom + Vector2.UnitY * (npc.gfxOffY - 10f);
        public ref float StuckTimer => ref npc.ai[1];
        public ref float LungeDelay => ref npc.ai[2];
        public ref float LungeCooldown => ref npc.ai[3];
        public ref float JumpCooldown => ref npc.localAI[1];

        public const float Gravity = 0.4f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blood Crawler");
            NPCID.Sets.TrailingMode[npc.type] = 0;
            NPCID.Sets.TrailCacheLength[npc.type] = 7;
        }

        public override void SetDefaults()
        {
            npc.width = 48;
            npc.height = 22;
            npc.aiStyle = -1;
            npc.damage = 32;
            npc.defense = 4;
            npc.lifeMax = 115;
            npc.HitSound = SoundID.NPCHit27;
            npc.DeathSound = SoundID.NPCDeath21;
            npc.knockBackResist = 0.25f;
            npc.value = 0f;
            npc.Opacity = 0f;
            npc.hide = true;
            npc.noGravity = true;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = false;
        }

		public override void SendExtraAI(BinaryWriter writer)
        {
            for (int i = 0; i < LegLimbs.Length; i++)
			{
                for (int j = 0; j < LegLimbs[i].Length; j++)
                    LegLimbs[i][j].SendData(writer);
			}
        }

		public override void ReceiveExtraAI(BinaryReader reader)
        {
            for (int i = 0; i < LegLimbs.Length; i++)
            {
                for (int j = 0; j < LegLimbs[i].Length; j++)
                    LegLimbs[i][j].ReceiveData(reader);
            }
        }

		public void InitializeLimbs()
        {
            for (int i = 0; i < LegLimbs.Length; i++)
            {
                LegLimbs[i] = new Limb[2];
                for (int j = 0; j < LegLimbs[i].Length; j++)
                {
                    Vector2 direction = Main.rand.NextVector2Unit() * j / 2f * 30f;
                    LegLimbs[i][j] = new Limb(npc.Center + direction, direction.ToRotation());
                }
            }
        }

		public override void AI()
        {
            npc.gfxOffY = -20;

            npc.TargetClosest();

            // Disappear if the main boss is not present.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.perfHive) || !npc.WithinRange(Target.Center, 1100f))
            {
                npc.active = false;
                npc.netUpdate = true;
                Utils.PoofOfSmoke(npc.Center);
                return;
			}

            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.025f, 0f, 1f);
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[0] == 0f)
			{
                InitializeLimbs();
                npc.netUpdate = true;
                npc.localAI[0] = 1f;
			}

            // Update limbs.
            UpdateLimbPositions();

            // Fall downward.
            if (!OnSolidGround)
                npc.velocity.Y += Gravity;

            if (LungeDelay > 0f)
			{
                LungeDelay--;
                StuckTimer = 0f;
                npc.velocity.X *= 0.96f;

                // Jump if the delay is over.
                if (LungeDelay <= 0f)
				{
                    // Make a lunge sound.
                    Main.PlaySound(SoundID.DD2_DarkMageAttack, npc.Center);

                    float jumpSpeed = InPhase2 ? 12.5f : 10.5f;
                    npc.position.Y -= 2f;
                    npc.velocity = Utilities.GetProjectilePhysicsFiringVelocity(npc.Top, Target.Center, Gravity, jumpSpeed, out _);
                    LungeCooldown = 200f;
                    npc.netUpdate = true;
				}

                return;
            }

            // Walk if enough legs are on the ground.
            if (OnSolidGround)
                DoWalkMovement();

            if (JumpCooldown > 0f)
                JumpCooldown--;

            // Jump if necessary.
            if ((ShouldJump() && OnSolidGround && LungeCooldown < 100f && JumpCooldown <= 0f) || StuckTimer >= 150f)
            {
                npc.velocity.Y = -9f;
                npc.position.Y -= StuckTimer >= 50f ? 10f : 6f;
                if (Math.Abs(npc.velocity.X) < 2f)
                    npc.velocity.X = npc.SafeDirectionTo(Target.Center).X * 3f;

                StuckTimer = 0f;
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

                // Make a lunge sound.
                Main.PlaySound(SoundID.DD2_DarkMageAttack, npc.Center);

                JumpCooldown = 60f;

                npc.netUpdate = true;
            }

            // Prepare to lunge at the target if they're close.
            // Play a sound before doing so.
            if (LungeCooldown > 0f)
                LungeCooldown--;
            else if (LungeDelay <= 0f && MathHelper.Distance(Target.Center.X, npc.Center.X) < 150f && OnSolidGround)
			{
                LungeDelay = 35f;
                npc.velocity.X *= 0.5f;
                Main.PlaySound(SoundID.Zombie, npc.Center, 21);

                npc.netUpdate = true;
            }

            if (MathHelper.Distance(npc.position.X, npc.oldPosition.X) < 2f)
            {
                StuckTimer++;
                npc.position.Y -= 1.65f;
            }
            else
                StuckTimer = 0f;
        }

        public void UpdateLimbPositions()
		{
            for (int i = 0; i < LegLimbs.Length; i++)
            {
                int direction = (i > LegLimbs.Length / 2).ToDirectionInt();
                Vector2 legOffset = (MathHelper.TwoPi * 2f * i / LegLimbs.Length + npc.position.X / 180f + npc.position.Y / 100f - 0.4f).ToRotationVector2() * 56f;
                legOffset.Y = Math.Abs(legOffset.Y);

                if (Math.Abs(legOffset.X) < 8f)
                    legOffset.X += -direction * 20f;

                if (!OnSolidGround)
                {
                    IdealLegEndPositions[i].X += Math.Abs(legOffset.X * 0.7f) * -direction;
                    IdealLegEndPositions[i].Y += Math.Abs((float)Math.Sin(MathHelper.TwoPi * i / LegLimbs.Length)) * 56f;
                    IdealLegEndPositions[i] = LegsStartingCenter + (IdealLegEndPositions[i] - LegsStartingCenter).ClampMagnitude(0f, 46f).RotatedBy(npc.rotation);
                    IdealLegEndPositions[i].X += npc.velocity.X * 2f;

                    LegEndPositions[i].Y = MathHelper.Clamp(LegEndPositions[i].Y, npc.Top.Y - 30f + npc.gfxOffY, npc.Bottom.Y + 65f - npc.gfxOffY);
                }
                else
                {
                    IdealLegEndPositions[i] = LegsStartingCenter + legOffset;

                    IdealLegEndPositions[i].X += npc.velocity.X * 6f;

                    if (WorldUtils.Find(
                        new Vector2(IdealLegEndPositions[i].X, npc.Top.Y).ToTileCoordinates(),
                        Searches.Chain(new Searches.Down(50), new Conditions.IsSolid()),
                        out Point result))
					{
                        IdealLegEndPositions[i].Y = result.Y * 16f;
					}
                }
            }

            // Ensure no leg positions are stuck in the ground.
            for (int i = 0; i < LegLimbs.Length; i++)
            {
                LegLimbs[i][0].StartingPoint = LegsStartingCenter;
                while (Collision.SolidCollision(IdealLegEndPositions[i], 2, 2))
                {
                    IdealLegEndPositions[i].X += Math.Sign(LegsStartingCenter.X - IdealLegEndPositions[i].X) * 2f;
                    IdealLegEndPositions[i].Y--;
                }

                IdealLegEndPositions[i].Y += 6f;

                // And move leg positions to their destination.
                LegEndPositions[i] = Vector2.Lerp(LegEndPositions[i], IdealLegEndPositions[i], OnSolidGround ? 0.3f : 0.1f);

                float moveSpeed = OnSolidGround ? 14f : 40f;
                float distanceToIdeal = Vector2.Distance(LegEndPositions[i], IdealLegEndPositions[i]);
                if (distanceToIdeal < moveSpeed)
                    moveSpeed = distanceToIdeal;
                LegEndPositions[i] += (IdealLegEndPositions[i] - IdealLegEndPositions[i]).ClampMagnitude(0f, moveSpeed);

                int direction = (i > LegLimbs.Length / 2).ToDirectionInt();

                float horizontalTotalDistance = LegEndPositions[i].X - LegLimbs[i][0].StartingPoint.X;
                float verticalTotalDistance = LegEndPositions[i].Y - LegLimbs[i][0].StartingPoint.Y;
                float firstLimbEndAngleOffset = Math.Abs((float)Math.Atan(verticalTotalDistance / horizontalTotalDistance)) * -direction;
                firstLimbEndAngleOffset += MathHelper.Pi + MathHelper.PiOver2 * direction;

                LegLimbs[i][1].StartingPoint = LegLimbs[i][0].StartingPoint + firstLimbEndAngleOffset.ToRotationVector2() * new Vector2(28f, 16f) * direction;
                LegLimbs[i][1].StartingPoint.Y -= 16f;
            }
        }

        public void DoWalkMovement()
		{
            int idealDirection = (Target.Center.X - npc.Center.X > 0f).ToDirectionInt();
            float runAcceleration = InPhase2 ? 0.15f : 0.1f;
            float maxRunSpeed = InPhase2 ? 7f : 5f;

            // Accelerate much faster if decelerating to make the effect more smooth.
            if (idealDirection != Math.Sign(npc.velocity.X))
                runAcceleration *= 2.4f;

            // Run towards the target.
            if (MathHelper.Distance(npc.Center.X, Target.Center.X) > 40f)
                npc.velocity.X = MathHelper.Clamp(npc.velocity.X + idealDirection * runAcceleration, -maxRunSpeed, maxRunSpeed);
            else if (Math.Abs(npc.velocity.X) < maxRunSpeed)
                npc.velocity *= 1.02f;

            npc.spriteDirection = (npc.velocity.X > 0).ToDirectionInt();
        }

        public bool ShouldJump()
        {
            Tile tileAheadBelowTarget = Framing.GetTileSafely(npc.Bottom + new Vector2(npc.spriteDirection * 36f, 16f));

            // If the next tile below the ninja's feet is inactive or actuated, jump.
            if (!tileAheadBelowTarget.active() && Main.tileSolid[tileAheadBelowTarget.type])
                return true;

            return false;
		}

        public override void DrawBehind(int index) => Main.instance.DrawCacheNPCProjectiles.Add(index);


        public override bool PreDraw(SpriteBatch spriteBatch, Color _)
        {
            if (InPhase2)
                PerforatorHiveAIClass.DrawEnragedEffectOnEnemy(spriteBatch, npc);

            if (npc.Opacity < 0.75f)
                return true;

            for (int i = 0; i < LegLimbs.Length; i++)
			{
                if (!npc.WithinRange(LegEndPositions[i], 300f))
                    continue;

                for (int j = 0; j < LegLimbs[i].Length; j++)
                {
                    Vector2 start = LegLimbs[i][j].StartingPoint;
                    Vector2 end = j == LegLimbs[i].Length - 1 ? LegEndPositions[i] : LegLimbs[i][j + 1].StartingPoint;

                    spriteBatch.DrawLineBetter(start, end, new Color(55, 14, 10) * npc.Opacity, 3f);
                }
			}

            return true;
        }

		public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.Bleeding, 240);
            target.AddBuff(ModContent.BuffType<BurningBlood>(), 90);
        }

		public override void HitEffect(int hitDirection, double damage)
        {
            if (npc.life > 0)
                return;

            for (int i = 0; i < 10; i++)
            {
                Dust blood = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(30f, 30f), DustID.Blood);
                blood.velocity = Main.rand.NextVector2Circular(4f, 4f);
                blood.noGravity = Main.rand.NextBool(3);
                blood.scale = Main.rand.NextFloat(1f, 1.35f);
            }

            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/BloodCrawler1"), npc.scale);
            Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/BloodCrawler2"), npc.scale);
        }

		public override bool CheckActive() => false;
    }
}
