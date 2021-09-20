using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.QueenBee
{
    public class QueenBeeBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.QueenBee;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        #region Enumerations
        internal enum QueenBeeAttackState
        {
            HorizontalCharge,
            StingerBurst,
            HoneyBlast,
            CreateMinionsFromAbdomen,
            SummonBeesFromBelow
        }

        internal enum QueenBeeFrameType
        {
            HorizontalCharge,
            UpwardFly,
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            // If none was found or it was too far away, despawn.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead ||
                !Main.player[npc.target].active || !npc.WithinRange(Main.player[npc.target].Center, 3400f))
            {
                DoDespawnEffects(npc);
                return false;
            }

            Player target = Main.player[npc.target];

            npc.defense = target.ZoneJungle ? npc.defDefense : 65;

            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float finalPhaseTransitionTimer = ref npc.ai[2];
            ref float frameType = ref npc.localAI[0];
            ref float hasBegunFinalPhaseTransition = ref npc.localAI[1];

            if (npc.life < npc.lifeMax * 0.1f && Main.netMode != NetmodeID.MultiplayerClient && hasBegunFinalPhaseTransition == 0f)
            {
                hasBegunFinalPhaseTransition = 1f;
                finalPhaseTransitionTimer = 150f;
            }

            if (finalPhaseTransitionTimer > 0f)
            {
                attackTimer = 0f;
                npc.dontTakeDamage = true;
                frameType = (int)QueenBeeFrameType.UpwardFly;
                finalPhaseTransitionTimer--;
                if (finalPhaseTransitionTimer == 0f)
                    GotoNextAttackState(npc);

                npc.velocity *= 0.93f;

                return false;
            }

            npc.dontTakeDamage = false;

            switch ((QueenBeeAttackState)(int)attackType)
            {
                case QueenBeeAttackState.HorizontalCharge:
                    DoAttack_HorizontalCharge(npc, target, ref frameType);
                    break;
                case QueenBeeAttackState.StingerBurst:
                    DoAttack_StingerBurst(npc, target, ref frameType, ref attackTimer);
                    break;
                case QueenBeeAttackState.HoneyBlast:
                    DoAttack_HoneyBlast(npc, target, ref frameType, ref attackTimer);
                    break;
                case QueenBeeAttackState.CreateMinionsFromAbdomen:
                    DoAttack_CreateMinionsFromAbdomen(npc, target, ref frameType, ref attackTimer);
                    break;
                case QueenBeeAttackState.SummonBeesFromBelow:
                    DoAttack_SummonBeesFromBelow(npc, target, ref frameType, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        #region Specific Attacks
        internal static void DoDespawnEffects(NPC npc)
        {
            npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 17f, 0.1f);
            npc.damage = 0;
            if (npc.timeLeft > 180)
                npc.timeLeft = 180;
        }

        internal static void DoAttack_HorizontalCharge(NPC npc, Player target, ref float frameType)
        {
            int chargesToDo = 2;
            if (npc.life < npc.lifeMax * 0.33)
                chargesToDo = 4;

            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float speedBoost = ref npc.Infernum().ExtraAI[1];
            ref float totalChargesDone = ref npc.Infernum().ExtraAI[2];

            // Line up.
            if (attackState == 0f)
            {
                Vector2 destination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 320f;
                npc.velocity += npc.SafeDirectionTo(destination) * 0.5f;

                frameType = (int)QueenBeeFrameType.UpwardFly;
                if (npc.WithinRange(destination, 40f) || Math.Abs(target.Center.Y - npc.Center.Y) < 10f)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center, Vector2.UnitX) * 18.5f;
                    if (npc.life < npc.lifeMax * 0.1)
                        npc.velocity *= 1.3f;

                    if (npc.life < npc.lifeMax * 0.1 && Math.Abs(target.velocity.Y) > 3f)
                        npc.velocity.Y += target.velocity.Y * 0.5f;
                    attackState = 1f;
                    frameType = (int)QueenBeeFrameType.HorizontalCharge;

                    Main.PlaySound(SoundID.Roar, npc.Center, 0);

                    npc.netUpdate = true;
                }
                npc.spriteDirection = Math.Sign(npc.velocity.X);
            }

            // Do the charge.
            else
            {
                speedBoost += 0.004f;
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitX) * (npc.velocity.Length() + speedBoost);

                frameType = (int)QueenBeeFrameType.HorizontalCharge;
                float destinationOffset = MathHelper.Lerp(540f, 450f, 1f - npc.life / (float)npc.lifeMax);

                if ((npc.spriteDirection == 1 && (npc.Center.X - target.Center.X) > destinationOffset) ||
                    (npc.spriteDirection == -1 && (npc.Center.X - target.Center.X) < -destinationOffset))
                {
                    npc.velocity *= 0.5f;
                    attackState = 0f;
                    speedBoost = 0f;
                    totalChargesDone++;
                    npc.netUpdate = true;
                }
            }

            if (totalChargesDone >= chargesToDo)
                GotoNextAttackState(npc);
        }

        internal static void DoAttack_StingerBurst(NPC npc, Player target, ref float frameType, ref float attackTimer)
        {
            ref float flyDestinationX = ref npc.Infernum().ExtraAI[0];
            ref float flyDestinationY = ref npc.Infernum().ExtraAI[1];
            Vector2 currentFlyDestination = new Vector2(flyDestinationX, flyDestinationY);

            if (flyDestinationX == 0f || flyDestinationY == 0f || flyDestinationY > target.Center.Y - 40f)
                currentFlyDestination = target.Center - Vector2.UnitY * 270f;

            if (!target.WithinRange(currentFlyDestination, 500f))
			{
                currentFlyDestination = target.Center - Vector2.UnitY * 200f;
                npc.netUpdate = true;
			}

            int shootRate = 50;
            int totalStingersToShoot = 5;
            float shootSpeed = 12f;
            if (npc.life < npc.lifeMax * 0.5)
            {
                shootRate = 28;
                totalStingersToShoot = 8;
                shootSpeed = 15f;
            }

            bool canShoot = npc.Bottom.Y < target.position.Y;
            Vector2 baseStingerSpawnPosition = new Vector2(npc.Center.X + Main.rand.Next(20) * npc.spriteDirection, npc.Center.Y + npc.height * 0.3f);

            frameType = (int)QueenBeeFrameType.UpwardFly;

            if (attackTimer % shootRate == shootRate - 1f && canShoot && Collision.CanHit(baseStingerSpawnPosition, 1, 1, target.Center, 1, 1))
            {
                // Play a shoot sound when firing.
                Main.PlaySound(SoundID.Item17, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        float offsetAngle = MathHelper.TwoPi * 1.61808f * i / 12f;
                        Vector2 stingerSpawnPosition = baseStingerSpawnPosition + offsetAngle.ToRotationVector2() * MathHelper.Lerp(4f, 28f, i / 12f);
                        Vector2 stingerShootVelocity = (target.Center - baseStingerSpawnPosition).SafeNormalize(Vector2.UnitY) * shootSpeed;
                        float burstOutwardness = MathHelper.Lerp(0.04f, 0.12f, 1f - npc.life / (float)npc.lifeMax);
                        stingerShootVelocity = stingerShootVelocity.RotatedBy(MathHelper.Lerp(-burstOutwardness, burstOutwardness, i / 11f));

                        int stinger = Utilities.NewProjectileBetter(stingerSpawnPosition, stingerShootVelocity, ProjectileID.Stinger, 75, 0f);
                        if (Main.projectile.IndexInRange(stinger))
                            Main.projectile[stinger].tileCollide = false;
                    }

                    // Determine a new position to fly at.
                    Matrix offsetRotationMatrix = Matrix.CreateRotationX(attackTimer / shootRate * 2.8f + 0.56f);
                    offsetRotationMatrix *= Matrix.CreateRotationY((attackTimer / shootRate * 5.3f + 0.66f) * 0.41f);
                    offsetRotationMatrix *= Matrix.CreateRotationZ(attackTimer / shootRate * MathHelper.Pi * 0.33f);

                    Vector3 tansformedRotationData = Vector3.Transform(Vector3.UnitY, offsetRotationMatrix);
                    Vector2 flyDestinationOffset = new Vector2(tansformedRotationData.X, tansformedRotationData.Y) * new Vector2(210f, 125f);
                    currentFlyDestination = target.Center - Vector2.UnitY * 260f + flyDestinationOffset;
                    flyDestinationOffset.X += target.velocity.X * 30f;
                    if (target.WithinRange(currentFlyDestination, 400f))
                        currentFlyDestination = target.Center + target.SafeDirectionTo(currentFlyDestination) * 400f;

                    npc.netUpdate = true;
                }
            }

            // Fly above the target.
            if (npc.WithinRange(currentFlyDestination, 35f))
            {
                npc.velocity *= 0.85f;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            }
            else
            {
                DoHoverMovement(npc, currentFlyDestination, 0.25f);
                if (Math.Abs(npc.velocity.X) > 2f)
                    npc.spriteDirection = Math.Sign(npc.velocity.X);
            }

            flyDestinationX = currentFlyDestination.X;
            flyDestinationY = currentFlyDestination.Y;

            if (attackTimer >= totalStingersToShoot * shootRate)
                GotoNextAttackState(npc);
        }

        internal static void DoAttack_HoneyBlast(NPC npc, Player target, ref float frameType, ref float attackTimer)
        {
            // Fly above the target.
            Vector2 flyDestination = target.Center - new Vector2((target.Center.X - npc.Center.X > 0).ToDirectionInt() * 270f, 240f);
            DoHoverMovement(npc, flyDestination, 0.14f);

            frameType = (int)QueenBeeFrameType.UpwardFly;
            npc.spriteDirection = (target.Center.X - npc.Center.X > 0).ToDirectionInt();

            // Release blasts of honey.
            bool honeyIsPoisonous = npc.life < npc.lifeMax * 0.5f;
            int shootRate = honeyIsPoisonous ? 10 : 25;
            int totalBlastsToShoot = 18;
            bool canShoot = npc.Bottom.Y < target.position.Y;
            if (attackTimer % shootRate == shootRate - 1f && canShoot)
            {
                // Play a shoot sound when firing.
                Main.PlaySound(SoundID.Item17, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 honeySpawnPosition = new Vector2(npc.Center.X, npc.Center.Y + npc.height * 0.325f);
                    Vector2 honeyShootVelocity = (target.Center - honeySpawnPosition).SafeNormalize(Vector2.UnitY) * 10f;
                    int honeyBlast = Utilities.NewProjectileBetter(honeySpawnPosition, honeyShootVelocity, ModContent.ProjectileType<HoneyBlast>(), 65, 0f);
                    if (Main.projectile.IndexInRange(honeyBlast))
                        Main.projectile[honeyBlast].ai[0] = honeyIsPoisonous.ToInt();
                }
            }

            if (attackTimer >= shootRate * totalBlastsToShoot)
                GotoNextAttackState(npc);
        }

        internal static void DoAttack_CreateMinionsFromAbdomen(NPC npc, Player target, ref float frameType, ref float attackTimer)
        {
            Vector2 destination = target.Center - Vector2.UnitY * 210f;
            DoHoverMovement(npc, destination, 0.1f);

            frameType = (int)QueenBeeFrameType.UpwardFly;

            bool canShootHornetHives = npc.life < npc.lifeMax * 0.75f;
            int totalThingsToSummon = canShootHornetHives ? 2 : 7;

            // Shoot 3 hives instead of 2 once below 33% life.
            if (npc.life < npc.lifeMax * 0.33f)
                totalThingsToSummon = 3;

            int summonRate = 25;
            if (canShootHornetHives)
                summonRate = 60;

            if (MathHelper.Distance(target.Center.X, npc.Center.X) > 60f)
                npc.spriteDirection = (target.Center.X - npc.Center.X > 0).ToDirectionInt();

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % summonRate == summonRate - 1f)
            {
                Vector2 spawnPosition = new Vector2(npc.Center.X, npc.Center.Y + npc.height * 0.325f);
                spawnPosition += Main.rand.NextVector2Circular(25f, 25f);

                if (canShootHornetHives)
                {
                    Vector2 hiveShootVelocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitY) * 11.5f;
                    spawnPosition += hiveShootVelocity * 2f;
                    Projectile.NewProjectile(spawnPosition, hiveShootVelocity, ModContent.ProjectileType<HornetHive>(), 55, 0f);
                }
                else
                {
                    int bee = NPC.NewNPC((int)spawnPosition.X, (int)spawnPosition.Y, NPCID.Bee);
                    Main.npc[bee].velocity = Main.npc[bee].SafeDirectionTo(target.Center, Vector2.UnitY).RotatedByRandom(0.37f) * 4f;
                }
            }

            if (attackTimer >= summonRate * totalThingsToSummon)
                GotoNextAttackState(npc);
        }

        internal static void DoAttack_SummonBeesFromBelow(NPC npc, Player target, ref float frameType, ref float attackTimer)
        {
            frameType = (int)QueenBeeFrameType.UpwardFly;

            int hoverTime = 75;
            if (attackTimer < hoverTime)
            {
                Vector2 flyDestination = target.Center - new Vector2((target.Center.X - npc.Center.X > 0).ToDirectionInt() * 270f, 240f);
                DoHoverMovement(npc, flyDestination, 0.15f);
            }
            else if (attackTimer < 775f)
            {
                npc.velocity *= 0.9785f;

                // Roar and make a circle of honey dust as an indicator before release the bees.
                if (attackTimer == hoverTime + 1f)
                {
                    Main.PlaySound(SoundID.Roar, target.Center, 0);
                    for (int i = 0; i < 30; i++)
                    {
                        Vector2 honeyDustVelocity = (MathHelper.TwoPi * i / 30f).ToRotationVector2() * 5f;
                        Dust honey = Dust.NewDustPerfect(npc.Center, 153);
                        honey.scale = Main.rand.NextFloat(1f, 1.85f);
                        honey.velocity = honeyDustVelocity;
                        honey.noGravity = true;
                    }
                }

                int shootRate = npc.life < npc.lifeMax * 0.1f ? 16 : 20;
                if (attackTimer % shootRate == shootRate - 1f && attackTimer < 600f)
                {
                    Vector2 beeSpawnPosition = target.Center + new Vector2(Main.rand.NextBool(2).ToDirectionInt() * 1200f, Main.rand.NextFloat(-900f, 0f));
                    Vector2 beeVelocity = (target.Center - beeSpawnPosition).SafeNormalize(Vector2.UnitY) * new Vector2(4f, 20f);
                    Utilities.NewProjectileBetter(beeSpawnPosition, beeVelocity, ModContent.ProjectileType<TinyBee>(), 65, 0f);
                }
            }

            if (attackTimer >= 805f)
                GotoNextAttackState(npc);

            npc.spriteDirection = (target.Center.X - npc.Center.X > 0).ToDirectionInt();
        }

        internal static void DoHoverMovement(NPC npc, Vector2 destination, float flyAcceleration)
        {
            Vector2 idealVelocity = npc.SafeDirectionTo(destination) * 33f;
            if (npc.velocity.X < idealVelocity.X)
            {
                npc.velocity.X += flyAcceleration;
                if (npc.velocity.X < 0f && idealVelocity.X > 0f)
                    npc.velocity.X += flyAcceleration * 2f;
            }
            else if (npc.velocity.X > idealVelocity.X)
            {
                npc.velocity.X -= flyAcceleration;
                if (npc.velocity.X > 0f && idealVelocity.X < 0f)
                    npc.velocity.X -= flyAcceleration * 2f;
            }
            if (npc.velocity.Y < idealVelocity.Y)
            {
                npc.velocity.Y += flyAcceleration;
                if (npc.velocity.Y < 0f && idealVelocity.Y > 0f)
                    npc.velocity.Y += flyAcceleration * 2f;
            }
            else if (npc.velocity.Y > idealVelocity.Y)
            {
                npc.velocity.Y -= flyAcceleration;
                if (npc.velocity.Y > 0f && idealVelocity.Y < 0f)
                    npc.velocity.Y -= flyAcceleration * 2f;
            }
        }
        #endregion Specific Attacks

        #region AI Utility Methods

        internal static void GotoNextAttackState(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;

            QueenBeeAttackState oldAttackType = (QueenBeeAttackState)(int)npc.ai[0];
            QueenBeeAttackState newAttackType = QueenBeeAttackState.HorizontalCharge;
            switch (oldAttackType)
            {
                case QueenBeeAttackState.HorizontalCharge:
                    newAttackType = QueenBeeAttackState.StingerBurst;
                    break;
                case QueenBeeAttackState.StingerBurst:
                    newAttackType = QueenBeeAttackState.HoneyBlast;
                    break;
                case QueenBeeAttackState.HoneyBlast:
                    newAttackType = QueenBeeAttackState.CreateMinionsFromAbdomen;
                    break;
                case QueenBeeAttackState.CreateMinionsFromAbdomen:
                    newAttackType = lifeRatio < 0.5f ? QueenBeeAttackState.SummonBeesFromBelow : QueenBeeAttackState.HorizontalCharge;
                    break;
            }

            if (lifeRatio < 0.1f)
                newAttackType = oldAttackType == QueenBeeAttackState.HorizontalCharge ? QueenBeeAttackState.SummonBeesFromBelow : QueenBeeAttackState.HorizontalCharge;

            npc.ai[0] = (int)newAttackType;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        #endregion AI Utility Methods

        #endregion AI

        #region Drawing and Frames

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            if (npc.frameCounter % 5 == 4)
                npc.frame.Y += frameHeight;
            switch ((QueenBeeFrameType)(int)npc.localAI[0])
            {
                case QueenBeeFrameType.UpwardFly:
                    if (npc.frame.Y < frameHeight * 4)
                        npc.frame.Y = frameHeight * 4;
                    if (npc.frame.Y >= frameHeight * Main.npcFrameCount[npc.type])
                        npc.frame.Y = frameHeight * 4;
                    break;
                case QueenBeeFrameType.HorizontalCharge:
                    if (npc.frame.Y >= frameHeight * 4)
                        npc.frame.Y = 0;
                    break;
            }
        }
        #endregion
    }
}
