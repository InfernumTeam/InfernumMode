using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

namespace InfernumMode.FuckYouModeAIs.KingSlime
{
    public class Ninja : ModNPC
    {
        public PrimitiveTrailCopy FireDrawer;
        public Player Target => Main.player[npc.target];
        public ref float CurrentTeleportDirection => ref Main.npc[NPC.FindFirstNPC(NPCID.KingSlime)].Infernum().ExtraAI[6];
        public ref float Time => ref npc.ai[0];
        public ref float ShurikenShootCountdown => ref npc.ai[1];
        public ref float TimeOfFlightCountdown => ref npc.ai[2];
        public ref float TeleportCountdown => ref npc.ai[3];
        public ref float StuckTimer => ref npc.localAI[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ninja");
            Main.npcFrameCount[npc.type] = 9;
			NPCID.Sets.TrailingMode[npc.type] = 3;
            NPCID.Sets.TrailCacheLength[npc.type] = 9;
        }

        public override void SetDefaults()
        {
			npc.npcSlots = 1f;
            npc.aiStyle = aiType = -1;
            npc.width = npc.height = 26;
            npc.damage = 5;
            npc.lifeMax = 100;
            npc.knockBackResist = 0f;
            npc.dontTakeDamage = true;
            npc.noGravity = false;
            npc.noTileCollide = false;
            npc.netAlways = true;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(StuckTimer);

        public override void ReceiveExtraAI(BinaryReader reader) => StuckTimer = reader.ReadSingle();

        public override void AI()
        {
            // Disappear if the main boss is not present.
            if (!NPC.AnyNPCs(NPCID.KingSlime))
			{
                Utils.PoofOfSmoke(npc.Center);
				npc.active = false;
				npc.netUpdate = true;
				return;
			}

            if (MathHelper.Distance(npc.position.X, npc.oldPosition.X) < 2f)
                StuckTimer += 2f;

            npc.TargetClosest();

            float horizontalDistanceFromTarget = MathHelper.Distance(Target.Center.X, npc.Center.X);

            if (ShurikenShootCountdown > 0f)
            {
                // Shoot 3 shurikens before the timer resets.
                if (ShurikenShootCountdown == 1f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 shurikenVelocity = npc.SafeDirectionTo(Target.Center).RotatedBy(MathHelper.Lerp(-0.45f, 0.45f, i / 2f)) * 5.8f;
                            Utilities.NewProjectileBetter(npc.Center + shurikenVelocity, shurikenVelocity, ModContent.ProjectileType<Shuriken>(), 45, 0f);
                        }
                    }

                    Main.PlaySound(SoundID.Item1, npc.Center);
                }

                ShurikenShootCountdown--;
            }

            if (TimeOfFlightCountdown > 0f)
            {
                if (npc.velocity.X != 0f)
                {
                    npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

                    // Spin when going upward.
                    if (npc.velocity.Y < 0f)
                        npc.rotation += npc.spriteDirection * 0.3f;
                    // And aim footfirst when going downward.
                    // Unless it's April 1st. In which case he becomes a goddamn bouncy ball lmao
                    else if (!Utilities.IsAprilFirst())
                        npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                }
                else
                    npc.rotation = 0f;

                TimeOfFlightCountdown--;
                if (npc.collideY && TimeOfFlightCountdown < 35f)
                    TimeOfFlightCountdown = 0f;

                return;
            }
            else
            {
                npc.rotation = 0f;
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
            }

            if (Time % 150f > 130f)
                npc.velocity.X *= 0.96f;
            else
                DoRunEffects();

            // Teleport if far from the target or it is typically possible to do so.
            bool canDashTeleport = Time % 360f == 359f && Collision.CanHit(npc.Center, 2, 2, npc.Center + Vector2.UnitX * npc.spriteDirection * 80f, 2, 2);
            canDashTeleport |= !npc.WithinRange(Target.Center, 820f) || StuckTimer >= 150f;
            canDashTeleport &= npc.collideY || StuckTimer >= 150f;

            if (TeleportCountdown > 0f)
            {
                DoTeleportEffects();
                TeleportCountdown--;
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && canDashTeleport)
            {
                StuckTimer = 0f;
                TeleportCountdown = 70f;
                npc.netUpdate = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && horizontalDistanceFromTarget > 320f && Time % 60f == 59f && npc.collideY)
            {
                float jumpSpeed = (float)Math.Sqrt(horizontalDistanceFromTarget) * 0.7f;
                if (jumpSpeed >= 13f)
                    jumpSpeed = 13f;
                jumpSpeed *= Main.rand.NextFloat(1.15f, 1.4f);
                DoJump(jumpSpeed);

                npc.netUpdate = true;
            }

            Time++;
        }

        public void DoJump(float jumpSpeed)
        {
            float gravity = 0.3f;
            npc.velocity = Utilities.GetProjectilePhysicsFiringVelocity(npc.Center, Target.Center, gravity, jumpSpeed, out _);
            ShurikenShootCountdown = 24f;

            // Use the Time of Flight formula to determine how long the jump will last.
            TimeOfFlightCountdown = (int)Math.Ceiling(Math.Abs(npc.velocity.Y * 2f / gravity));
            npc.spriteDirection = (Target.Center.X - npc.Center.X > 0f).ToDirectionInt();

            npc.netUpdate = true;
        }

        public void DoRunEffects()
        {
            if (TeleportCountdown > 0)
                return;

            int idealDirection = (Target.Center.X - npc.Center.X > 0f).ToDirectionInt();
            float runAcceleration = 0.18f;
            float maxRunSpeed = 6f;

            // Accelerate much faster if decelerating to make the effect more smooth.
            if (idealDirection != Math.Sign(npc.velocity.X))
                runAcceleration *= 4f;

            // Run towards the target.
            if (MathHelper.Distance(npc.Center.X, Target.Center.X) > 40f)
                npc.velocity.X = MathHelper.Clamp(npc.velocity.X + idealDirection * runAcceleration, -maxRunSpeed, maxRunSpeed);
            else
                npc.velocity *= 1.02f;

            bool onSolidGround = WorldGen.SolidTile(Framing.GetTileSafely(npc.Bottom + Vector2.UnitY * 16f));
            Tile tileAheadAboveTarget = Framing.GetTileSafely(npc.Bottom + new Vector2(npc.spriteDirection * 16f, -16f));
            Tile tileAheadBelowTarget = Framing.GetTileSafely(npc.Bottom + new Vector2(npc.spriteDirection * 16f, 16f));

            // Jump if there's an impending obstacle.
            if (onSolidGround && tileAheadAboveTarget.active() && Main.tileSolid[tileAheadAboveTarget.type])
            {
                DoJump(8f);
                npc.netUpdate = true;
            }

            // If the next tile below the ninja's feet is inactive or actuated, jump.
            if (onSolidGround && !tileAheadBelowTarget.active() && Main.tileSolid[tileAheadBelowTarget.type])
            {
                DoJump(10f);
                npc.netUpdate = true;
            }

            // Jump if is stuck somewhat on the X axis.
            if (MathHelper.Distance(npc.position.X, npc.oldPosition.X) < 2f)
            {
                DoJump(13f);
                npc.netUpdate = true;
            }
            else
                StuckTimer = 0f;
        }

        public void DoTeleportEffects()
        {
            // Do the teleport dash.
            if (TeleportCountdown > 35f)
            {
                npc.velocity.X = MathHelper.SmoothStep(0f, npc.spriteDirection * 10f, Utils.InverseLerp(35f, 70f, TeleportCountdown, true));
                npc.Opacity = Utils.InverseLerp(35f, 45f, TeleportCountdown, true);
            }
            
            // Decide where to teleport to.
            else if (TeleportCountdown == 35f)
            {
                Vector2 teleportPoint = Vector2.Zero;

                Vector2 top = Target.Center - Vector2.UnitY * 800f;
                if (top.Y < 100f)
                    top.Y = 100f;

                CurrentTeleportDirection *= -1f;
                npc.spriteDirection = (int)CurrentTeleportDirection;

                WorldUtils.Find(top.ToTileCoordinates(), Searches.Chain(new Searches.Down(400), new Conditions.IsSolid()), out Point ground);
                Vector2 groundedTargetPosition = ground.ToWorldCoordinates();

                for (int tries = 0; tries < 10000; tries++)
                {
                    Vector2 potentialSpawnPoint = groundedTargetPosition + new Vector2(Main.rand.NextFloat(-400f - tries * 0.04f, 400f + tries * 0.04f), Main.rand.NextFloat(-400f - tries * 0.025f, 100f));
                    Vector2 potentialEndPoint = potentialSpawnPoint + Vector2.UnitX * npc.spriteDirection * 150f;

                    // Ignore a position is too close to the target.
                    if (Target.WithinRange(potentialSpawnPoint, 180f) || Target.WithinRange(potentialEndPoint, 180f))
                        continue;

                    // If it's close to the original position.
                    if (npc.WithinRange(potentialSpawnPoint, 180f))
                        continue;

                    // If there would be a wall in the way.
                    if (!Collision.CanHit(potentialSpawnPoint - npc.Size * 0.5f, npc.width, npc.height, potentialEndPoint, 2, 2))
                        continue;

                    // If the area would result in the ninja being stuck.
                    if (Collision.SolidCollision(potentialSpawnPoint - Vector2.One * 38f, 50, 50))
                        continue;

                    // If the side is incorrect.
                    if (Math.Sign(potentialSpawnPoint.X - npc.Center.X) != npc.spriteDirection)
                        continue;

                    // Or if there's no ground near the position.
                    Point teleportPointTileBottom = potentialSpawnPoint.ToTileCoordinates();
                    if (!WorldGen.SolidTile(teleportPointTileBottom.X, teleportPointTileBottom.Y + 1))
                        continue;

                    teleportPoint = potentialSpawnPoint.ToTileCoordinates().ToWorldCoordinates(8, -8);
                    break;
                }

                if (teleportPoint != Vector2.Zero)
                    npc.Center = teleportPoint;
                npc.netUpdate = true;
            }
            else
            {
                npc.velocity.X = MathHelper.SmoothStep(0f, npc.spriteDirection * 10f, Utils.InverseLerp(0f, 35f, TeleportCountdown, true));
                npc.Opacity = Utils.InverseLerp(35f, 25f, TeleportCountdown, true);
            }

            // Spawn no dust if fading out a good amount.
            if (npc.Opacity < 0.5f)
                return;

            // Release ninja dodge dust.
            if (TeleportCountdown % 3f == 2f)
            {
                for (int i = 0; i < 6; i++)
                {
                    Dust ninjaDodgeDust = Dust.NewDustDirect(npc.position, npc.width, npc.height, 31, 0f, 0f, 100, default, 2f);
                    ninjaDodgeDust.position += Main.rand.NextVector2Square(-20f, 20f);
                    ninjaDodgeDust.velocity *= 0.4f;
                    ninjaDodgeDust.scale *= Main.rand.NextFloat(1f, 1.4f);
                    if (Main.rand.NextBool(2))
                    {
                        ninjaDodgeDust.scale *= Main.rand.NextFloat(1f, 1.4f);
                        ninjaDodgeDust.noGravity = true;
                    }
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            frameHeight = 48;
            if (TimeOfFlightCountdown > 0f || !npc.collideY)
            {
                npc.frame.Y = frameHeight * 8;
                return;
            }

            if (TeleportCountdown > 0f)
            {
                npc.frame.Y = frameHeight * 3;
                return;
            }

            npc.frameCounter++;
            if (npc.frameCounter % 3f == 2f && npc.collideY)
                npc.frame.Y += frameHeight;

            if (npc.frame.Y >= frameHeight * 8)
                npc.frame.Y = 0;
        }

        public override bool CheckActive() => false;
    }
}
