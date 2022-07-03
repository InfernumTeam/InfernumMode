using CalamityMod.Events;
using CalamityMod.Sounds;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Deerclops
{
    public class DeerclopsBehaviorOverride : NPCBehaviorOverride
    {
        public enum DeerclopsAttackState
        {
            DecideArena,
            WalkToTarget,
            TallIcicles,
            WideIcicles,
            BidirectionalIcicleSlam,
            UpwardDebrisLaunch
        }

        public enum DeerclopsFrameType
        {
            FrontFacingRoar,
            DigIntoGround,
            Walking,
            RaiseArmsUp
        }

        public const float ArenaWidth = 2000f;

        public override int NPCOverrideType => NPCID.Deerclops;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        public override bool PreAI(NPC npc)
        {
            // Select a target as necessary.
            npc.TargetClosestIfTargetIsInvalid();

            Player target = Main.player[npc.target];
            ref float attackTimer = ref npc.ai[1];
            ref float frameType = ref npc.localAI[0];

            // Reset things.
            npc.noGravity = false;
            npc.noTileCollide = false;

            switch ((DeerclopsAttackState)npc.ai[0])
            {
                case DeerclopsAttackState.DecideArena:
                    DoBehavior_DecideArena(npc, target, ref attackTimer, ref frameType);
                    break;
                case DeerclopsAttackState.WalkToTarget:
                    DoBehavior_WalkToTarget(npc, target, ref attackTimer, ref frameType);
                    break;
                case DeerclopsAttackState.TallIcicles:
                    DoBehavior_CreateIcicles(npc, target, false, ref attackTimer, ref frameType);
                    break;
                case DeerclopsAttackState.WideIcicles:
                    DoBehavior_CreateIcicles(npc, target, true, ref attackTimer, ref frameType);
                    break;
                case DeerclopsAttackState.BidirectionalIcicleSlam:
                    DoBehavior_BidirectionalIcicleSlam(npc, target, ref attackTimer, ref frameType);
                    break;
                case DeerclopsAttackState.UpwardDebrisLaunch:
                    DoBehavior_UpwardDebrisLaunch(npc, target, ref attackTimer, ref frameType);
                    break;
            }
            attackTimer++;
            return false;
        }

        public static void DoBehavior_DecideArena(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            // Slow down and roar.
            npc.velocity.X *= 0.9f;
            frameType = (int)DeerclopsFrameType.FrontFacingRoar;

            if (attackTimer == 38f)
                CreateIcicles(target);

            if (attackTimer >= 54f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_WalkToTarget(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            int maxWalkTime = 210;
            float walkSpeed = MathHelper.Lerp(4.56f, 7f, 1f - npc.life / (float)npc.lifeMax);

            bool haltMovement = MathHelper.Distance(npc.Center.X, target.Center.X) < 100f;
            Rectangle hitbox = target.Hitbox;

            // Use walking frames.
            frameType = (int)DeerclopsFrameType.Walking;

            // Rest tile collision things.
            npc.noTileCollide = true;

            // Slow down and make the attack go by quicker if really close to the target.
            if (haltMovement)
            {
                npc.velocity.X *= 0.9f;
                attackTimer += 4f;
            }
            else
            {
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, Math.Sign(hitbox.Center.X - npc.Center.X) * walkSpeed, 0.2f);
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            }

            if (attackTimer >= maxWalkTime)
                SelectNextAttack(npc);

            int horizontalArea = 40;
            int verticalArea = 20;
            Vector2 checkTopLeft = new(npc.Center.X - horizontalArea / 2, npc.position.Y + npc.height - verticalArea);
            bool inHorizontalBounds = checkTopLeft.X < hitbox.X && checkTopLeft.X + npc.width > hitbox.X + hitbox.Width;
            bool inVerticalBounds = checkTopLeft.Y + verticalArea < hitbox.Y + hitbox.Height - 16;
            bool acceptTopSurfaces = npc.Bottom.Y >= hitbox.Top;
            bool riseUpward = Collision.SolidCollision(checkTopLeft, horizontalArea, verticalArea, acceptTopSurfaces);
            bool canCeaseVerticalMovement = Collision.SolidCollision(checkTopLeft, horizontalArea, verticalArea - 4, acceptTopSurfaces);
            bool shouldJump = !Collision.SolidCollision(checkTopLeft + new Vector2(horizontalArea * npc.spriteDirection, 0f), 16, 80, acceptTopSurfaces);
            if ((inHorizontalBounds || haltMovement) & inVerticalBounds)
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.8f, 0.001f, 16f);
                return;
            }
            if (riseUpward && !canCeaseVerticalMovement)
            {
                npc.velocity.Y = 0f;
                return;
            }
            if (riseUpward)
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - 0.4f, -8f, 0f);
                return;
            }
            if (npc.velocity.Y == 0f & shouldJump)
            {
                npc.velocity.Y = -8f;
                return;
            }
            npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.4f, -8f, 16f);
        }

        public static void DoBehavior_CreateIcicles(NPC npc, Player target, bool wideIcicles, ref float attackTimer, ref float frameType)
        {
            int spikeShootRate = 2;
            int spikeShootTime = 64;
            float offsetPerSpike = 35f;
            float minSpikeScale = 0.3f;
            float maxSpikeScale = 1.84f;
            if (wideIcicles)
            {
                offsetPerSpike = 56f;
                minSpikeScale = 0.7f;
                maxSpikeScale = minSpikeScale + 0.01f;
            }

            int spikeCount = spikeShootTime / spikeShootRate;
            ref float sendSpikesForward = ref npc.Infernum().ExtraAI[0];

            // Slow down and choose frames.
            npc.velocity.X *= 0.9f; 
            frameType = (int)DeerclopsFrameType.DigIntoGround;

            // Choose the current direction.
            if (sendSpikesForward == 0f)
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (npc.localAI[1] == 1f && sendSpikesForward == 0f)
            {
                SoundEngine.PlaySound(SoundID.DeerclopsIceAttack, npc.Center);
                sendSpikesForward = 1f;
                npc.netUpdate = true;
            }

            // Don't increment the attack timer until the dig effect has happened.
            bool hitGround = npc.collideY || npc.velocity.Y == 0f;
            if (sendSpikesForward == 0f || !hitGround)
                attackTimer = -1f;
            if (!hitGround)
                frameType = (int)DeerclopsFrameType.RaiseArmsUp;

            Point point = npc.Bottom.ToTileCoordinates();
            point.X += npc.spriteDirection * 3;

            // Create a screen shake on the first frame when ready to shoot.
            if (attackTimer == 1f)
            {
                PunchCameraModifier modifier = new(npc.Center, Vector2.UnitY, 20f, 6f, 30, 1000f, "Deerclops");
                Main.instance.CameraModifiers.Add(modifier);
            }

            // Create spikes.
            int spikeIndex = (int)attackTimer / spikeShootRate;
            if (spikeShootRate <= 1f || attackTimer % spikeShootRate == spikeShootRate - 1f && attackTimer < spikeShootTime)
            {
                float horizontalOffset = spikeIndex * offsetPerSpike;
                float scale = Utils.Remap(attackTimer / spikeShootTime, 0f, 0.75f, minSpikeScale, maxSpikeScale);
                TryMakingSpike(target, ref point, 90, npc.spriteDirection, spikeCount, spikeIndex, horizontalOffset, scale);
            }

            if (attackTimer >= spikeShootTime + 30f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_BidirectionalIcicleSlam(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            int jumpDelay = 42;
            int spikeShootRate = 2;
            int spikeShootTime = 44;
            float offsetPerSpike = 25f;
            float minSpikeScale = 0.5f;
            float maxSpikeScale = 1f;
            bool hitGround = npc.collideY || npc.velocity.Y == 0f;
            ref float jumpState = ref npc.Infernum().ExtraAI[0];

            // Sit in place briefly before jumping.
            if (attackTimer < jumpDelay && jumpState < 2f)
            {
                frameType = (int)DeerclopsFrameType.DigIntoGround;
                npc.velocity.X *= 0.9f;
                return;
            }

            frameType = (int)DeerclopsFrameType.RaiseArmsUp;

            // Jump once ready.
            if (hitGround && jumpState == 0f)
            {
                npc.velocity = Vector2.UnitY * -10f;
                npc.position.Y -= 8f;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                jumpState = 1f;
                npc.netUpdate = true;
                return;
            }

            // Create hit effects the ground has been hit again.
            if (hitGround && jumpState == 1f)
            {
                SoundEngine.PlaySound(SoundID.DeerclopsIceAttack, npc.Bottom);
                SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, npc.Bottom);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(npc.Bottom, Vector2.UnitX * npc.spriteDirection * 6f, ProjectileID.DD2OgreSmash, 0, 0f);
                    jumpState = 2f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Release spikes once ready again.
            if (jumpState == 2f)
            {
                frameType = (int)DeerclopsFrameType.FrontFacingRoar;

                Point point = npc.Bottom.ToTileCoordinates();
                point.X += npc.spriteDirection * 3;

                // Create spikes.
                int spikeIndex = (int)attackTimer / spikeShootRate;
                int spikeCount = spikeShootTime / spikeShootRate;
                if (spikeShootRate <= 1f || attackTimer % spikeShootRate == spikeShootRate - 1f && attackTimer < spikeShootTime)
                {
                    float horizontalOffset = spikeIndex * offsetPerSpike;
                    float scale = Utils.Remap(attackTimer / spikeShootTime, 0f, 0.75f, minSpikeScale, maxSpikeScale);
                    TryMakingSpike(target, ref point, 90, -npc.spriteDirection, spikeCount, spikeIndex, horizontalOffset, scale);
                    TryMakingSpike(target, ref point, 90, npc.spriteDirection, spikeCount, spikeIndex, horizontalOffset, scale);
                }

                if (attackTimer >= spikeShootTime + 30f)
                    SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_UpwardDebrisLaunch(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            int debrisCount = 18;
            int attackTransitionDelay = 175;
            float debrisShootSpeed = 13.75f;
            ref float readyToShootDebris = ref npc.Infernum().ExtraAI[0];

            // Slow down and choose frames.
            npc.velocity.X *= 0.9f;
            frameType = (int)DeerclopsFrameType.DigIntoGround;

            // Choose the current direction.
            if (readyToShootDebris == 0f)
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (npc.localAI[1] == 1f && readyToShootDebris == 0f)
            {
                SoundEngine.PlaySound(SoundID.DeerclopsRubbleAttack, npc.Center);
                readyToShootDebris = 1f;
                npc.netUpdate = true;
            }

            // Don't increment the attack timer until the dig effect has happened.
            bool hitGround = npc.collideY || npc.velocity.Y == 0f;
            if (readyToShootDebris == 0f || !hitGround)
                attackTimer = -1f;
            if (!hitGround)
                frameType = (int)DeerclopsFrameType.RaiseArmsUp;

            Point point = npc.Bottom.ToTileCoordinates();
            point.X += npc.spriteDirection * 3;

            // Create a screen shake on the first frame when ready to shoot.
            if (attackTimer == 1f)
            {
                PunchCameraModifier modifier = new(npc.Center, Vector2.UnitY, 20f, 6f, 30, 1000f, "Deerclops");
                Main.instance.CameraModifiers.Add(modifier);
            }

            // Create debris.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 2f)
            {
                for (int i = 0; i < debrisCount; i++)
                {
                    Vector2 shootDestination = target.Center + Vector2.UnitX * (MathHelper.Lerp(-450f, 450f, i / (float)(debrisCount - 1f)) + Main.rand.NextFloat(10f));
                    Vector2 shootVelocity = Utilities.GetProjectilePhysicsFiringVelocity(npc.Bottom, shootDestination, 0.15f, debrisShootSpeed, out _).RotatedByRandom(0.19f);
                    int debris = Utilities.NewProjectileBetter(npc.Bottom, shootVelocity, ProjectileID.DeerclopsRangedProjectile, 90, 0f);
                    if (Main.projectile.IndexInRange(debris))
                        Main.projectile[debris].ai[1] = Main.rand.Next(6, 12);

                    shootVelocity = Utilities.GetProjectilePhysicsFiringVelocity(npc.Bottom, shootDestination, 0.15f, debrisShootSpeed * 1.45f, out _).RotatedByRandom(0.13f);
                    debris = Utilities.NewProjectileBetter(npc.Bottom, shootVelocity, ProjectileID.DeerclopsRangedProjectile, 90, 0f);
                    if (Main.projectile.IndexInRange(debris))
                        Main.projectile[debris].ai[1] = Main.rand.Next(6, 12);
                }
            }

            if (attackTimer >= attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void TryMakingSpike(Player target, ref Point sourceTileCoords, int damage, int dir, int spikeCount, int spikeIndex, float horizontalOffset, float scale)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int spikeID = ModContent.ProjectileType<GroundIcicleSpike>();
            int x = sourceTileCoords.X + (int)(horizontalOffset / 16f * dir);
            int y = TryMakingSpike_FindBestY(target, ref sourceTileCoords, x);
            if (!WorldGen.ActiveAndWalkableTile(x, y))
                return;

            Vector2 position = new(x * 16 + 8, y * 16 - scale * 80f);
            Vector2 velocity = -Vector2.UnitY.RotatedBy(spikeIndex / (float)spikeCount * dir * MathHelper.Pi * 0.175f);
            int spike = Utilities.NewProjectileBetter(position, velocity, spikeID, damage, 0f, Main.myPlayer, 0f);
            if (Main.projectile.IndexInRange(spike))
                Main.projectile[spike].ai[1] = scale;
        }

        public static int TryMakingSpike_FindBestY(Player target, ref Point sourceTileCoords, int x)
        {
            int bestY = sourceTileCoords.Y;
            if (!target.dead && target.active)
            {
                int targetTileBottom = (int)(target.Bottom.Y / 16f);
                int bestYPlayerDistance = Math.Sign((int)(target.Bottom.Y / 16f) - bestY);
                int soughtY = targetTileBottom + bestYPlayerDistance * 15;
                int? result = null;
                float bestScore = float.PositiveInfinity;
                for (int y = bestY; y != soughtY; y += bestYPlayerDistance)
                {
                    if (WorldGen.ActiveAndWalkableTile(x, y))
                    {
                        float score = new Point(x, y).ToWorldCoordinates().Distance(target.Bottom);
                        if (!result.HasValue || score < bestScore)
                        {
                            result = y;
                            bestScore = score;
                        }
                    }
                }
                if (result.HasValue)
                    bestY = result.Value;
            }
            int tries = 0;
            while (tries < 20 && bestY >= 10 && WorldGen.SolidTile(x, bestY))
            {
                bestY--;
                tries++;
            }
            tries = 0;
            while (tries < 20 && bestY <= Main.maxTilesY - 10 && !WorldGen.ActiveAndWalkableTile(x, bestY))
            {
                bestY++;
                tries++;
            }
            return bestY;
        }

        public static void SelectNextAttack(NPC npc)
        {
            switch ((int)npc.ai[2] % 7)
            {
                case 0:
                    npc.ai[0] = (int)DeerclopsAttackState.TallIcicles;
                    break;
                case 1:
                    npc.ai[0] = (int)DeerclopsAttackState.WalkToTarget;
                    break;
                case 2:
                    npc.ai[0] = (int)DeerclopsAttackState.WideIcicles;
                    break;
                case 3:
                    npc.ai[0] = (int)DeerclopsAttackState.WalkToTarget;
                    break;
                case 4:
                    npc.ai[0] = (int)DeerclopsAttackState.BidirectionalIcicleSlam;
                    break;
                case 5:
                    npc.ai[0] = (int)DeerclopsAttackState.UpwardDebrisLaunch;
                    break;
                case 6:
                    npc.ai[0] = (int)DeerclopsAttackState.WalkToTarget;
                    break;
            }

            npc.localAI[0] = 0f;
            npc.localAI[1] = 0f;
            npc.ai[1] = 0f;
            npc.ai[2]++;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.netUpdate = true;
        }

        public static void CreateIcicles(Player target)
        {
            SoundEngine.PlaySound(CommonCalamitySounds.MeatySlashSound, target.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 leftIciclePosition = Utilities.GetGroundPositionFrom(target.Center - Vector2.UnitX * ArenaWidth * 0.5f) + Vector2.UnitY * 24f;
                Vector2 rightIciclePosition = Utilities.GetGroundPositionFrom(target.Center + Vector2.UnitX * ArenaWidth * 0.5f) + Vector2.UnitY * 24f;
                Utilities.NewProjectileBetter(leftIciclePosition, Vector2.Zero, ModContent.ProjectileType<ArenaIcicle>(), 160, 0f);
                Utilities.NewProjectileBetter(rightIciclePosition, Vector2.Zero, ModContent.ProjectileType<ArenaIcicle>(), 160, 0f);
            }
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Width = 218;
            npc.frame.Height = 240;
            int frame = npc.frame.Y;

            npc.localAI[1] = 0f;

            switch ((DeerclopsFrameType)npc.localAI[0])
            {
                case DeerclopsFrameType.FrontFacingRoar:
                    if (frame < 19)
                        frame = 19;
                    if (npc.frameCounter >= 5D)
                    {
                        frame++;
                        // Roar if the last frame has been reached before the frame loop.
                        if (frame == 24)
                            SoundEngine.PlaySound(SoundID.DeerclopsScream, npc.Center);

                        npc.frameCounter = 0D;
                    }

                    if (frame >= 25)
                        frame = 24;
                    break;
                case DeerclopsFrameType.DigIntoGround:
                    if (frame is < 12 or >= 19)
                        frame = 12;
                    if (npc.frameCounter >= 7D && frame < 18)
                    {
                        frame++;
                        if (frame == 17)
                            npc.localAI[1] = 1f;

                        npc.frameCounter = 0D;
                    }
                    break;
                case DeerclopsFrameType.Walking:
                    if (frame is >= 12 or < 2)
                        frame = 2;
                    if (npc.frameCounter >= 6D)
                    {
                        frame++;
                        if (frame >= 12)
                            frame = 2;

                        npc.frameCounter = 0D;
                    }

                    npc.frameCounter += Math.Abs(npc.velocity.X) * 0.1333f - 0.8f;
                    break;
                case DeerclopsFrameType.RaiseArmsUp:
                    frame = 13;
                    break;
            }

            npc.frame.Y = frame;
            npc.frameCounter++;
        }
    }
}
