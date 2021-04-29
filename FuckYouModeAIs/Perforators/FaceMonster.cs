using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Perforators
{
	public class FaceMonster : ModNPC
    {
        public Player Target => Main.player[npc.target];
        public bool OnSolidGround
		{
			get
			{
                Tile belowTile = Framing.GetTileSafely(npc.Bottom + Vector2.UnitY * 8);
                return WorldGen.SolidTile(belowTile);
            }
		}
        public bool InPhase2 => Main.npc[CalamityGlobalNPC.perfHive].ai[2] == 2f;
        public bool HasRisenOutOfGroundCompletely
		{
            get => npc.ai[0] == 1f;
            set => npc.ai[0] = value.ToInt();
		}
        public ref float AttackTimer => ref npc.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Face Monster");
            Main.npcFrameCount[npc.type] = 16;
            NPCID.Sets.TrailingMode[npc.type] = 0;
            NPCID.Sets.TrailCacheLength[npc.type] = 7;
        }

        public override void SetDefaults()
        {
            npc.width = 34;
            npc.height = 60;
            npc.aiStyle = -1;
            npc.damage = 25;
            npc.defense = 10;
            npc.lifeMax = 140;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath2;
            npc.knockBackResist = 0.25f;
            npc.value = 0f;
            npc.Opacity = 0f;
            npc.hide = true;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = false;
        }

		public override void AI()
        {
            npc.TargetClosest();

            // Disappear if the main boss is not present.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.perfHive) || !npc.WithinRange(Target.Center, 1100f))
            {
                npc.active = false;
                npc.netUpdate = true;
                Utils.PoofOfSmoke(npc.Center);
                return;
			}

            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.05f, 0f, 1f);
            npc.noTileCollide = !HasRisenOutOfGroundCompletely;
            npc.noGravity = !HasRisenOutOfGroundCompletely;
            if (!HasRisenOutOfGroundCompletely)
			{
                npc.position.Y -= 4f;
                npc.velocity = Vector2.Zero;
                if (Main.netMode != NetmodeID.MultiplayerClient && !Collision.SolidCollision(npc.Bottom, 2, 2))
				{
                    HasRisenOutOfGroundCompletely = true;
                    npc.netUpdate = true;
				}
                return;
            }

            if (InPhase2)
                AttackTimer++;

            // Slow down and release blood spit at the player from time to time.
            if (AttackTimer % 150f > 90f)
			{
                npc.velocity *= 0.92f;
                npc.spriteDirection = (Target.Center.X - npc.Center.X > 0).ToDirectionInt();

                // Make a belch sound and shoot.
                if (AttackTimer % 150f == 120f)
				{
                    Main.PlaySound(SoundID.NPCDeath13, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
					{
                        Vector2 shootingPosition = npc.Top + new Vector2(npc.spriteDirection * 6f, 20f);
                        Vector2 baseVelocity = Utilities.GetProjectilePhysicsFiringVelocity(shootingPosition, Target.Center, 0.06f, 5f, out _);
                        for (int i = 0; i < 5; i++)
						{
                            Vector2 bloodVelocity = baseVelocity.RotatedByRandom(0.56f) * Main.rand.NextFloat(0.8f, 1.2f);
                            Utilities.NewProjectileBetter(shootingPosition, bloodVelocity, ModContent.ProjectileType<BloodGeyser>(), 65, 0f);
						}
					}
				}
                return;
			}

            if (OnSolidGround)
            {
                DoWalkMovement();
                if (ShouldJump())
                {
                    npc.velocity.Y = -10f;
                    npc.position.Y -= 8f;
                    npc.netUpdate = true;
                }
            }
        }

        public void DoWalkMovement()
		{
            int idealDirection = (Target.Center.X - npc.Center.X > 0f).ToDirectionInt();
            float runAcceleration = InPhase2 ? 0.1f : 0.06f;
            float maxRunSpeed = InPhase2 ? 7f : 5f;

            // Accelerate much faster if decelerating to make the effect more smooth.
            if (idealDirection != Math.Sign(npc.velocity.X))
                runAcceleration *= 2.4f;

            // Run towards the target.
            if (MathHelper.Distance(npc.Center.X, Target.Center.X) > 40f)
                npc.velocity.X = MathHelper.Clamp(npc.velocity.X + idealDirection * runAcceleration, -maxRunSpeed, maxRunSpeed);
            else
                npc.velocity *= 1.02f;

            npc.spriteDirection = (npc.velocity.X > 0).ToDirectionInt();
        }

        public bool ShouldJump()
        {
            Tile aheadTileInFront = Framing.GetTileSafely(npc.Bottom + new Vector2(npc.spriteDirection * 36, -8));
            if (WorldGen.SolidTile(aheadTileInFront))
                return true;

            Tile aheadTileBelow = Framing.GetTileSafely(npc.Bottom + new Vector2(npc.spriteDirection * 36, 8));
            if (!WorldGen.SolidTile(aheadTileBelow))
                return true;

            return false;
		}

        public override void DrawBehind(int index) => Main.instance.DrawCacheNPCProjectiles.Add(index);


        public override bool PreDraw(SpriteBatch spriteBatch, Color _)
        {
            if (InPhase2)
                PerforatorHiveAIClass.DrawEnragedEffectOnEnemy(spriteBatch, npc);

            return true;
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

            Gore.NewGore(npc.position, npc.velocity, 237, npc.scale);
        }

        public override void FindFrame(int frameHeight)
		{
            npc.frameCounter += MathHelper.Clamp(npc.velocity.X * 0.4f, 1f, 4.5f);
            if (npc.frameCounter >= 5f)
			{
                npc.frame.Y += frameHeight;
                npc.frameCounter = 0;
			}

            if (npc.frame.Y >= Main.npcFrameCount[npc.type] * frameHeight)
                npc.frame.Y = 2 * frameHeight;
            if (npc.frame.Y < 2 * frameHeight)
                npc.frame.Y = 2 * frameHeight;

            if (!OnSolidGround)
                npc.frame.Y = frameHeight;
            else if (Math.Abs(npc.velocity.X) < 1f)
                npc.frame.Y = 0;
		}

		public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.Bleeding, 240);
            target.AddBuff(ModContent.BuffType<BurningBlood>(), 90);
        }

        public override bool CheckActive() => false;
    }
}
