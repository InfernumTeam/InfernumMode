using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.QueenBee
{
	public class QueenBeeAIClass
    {
		#region Enumerations
        internal enum QueenBeeAttackState
        {
            HorizontalCharge,
            StingerBurst,
            HoneyBlast,
            CreateMinionsFromAbdomen
		}

        internal enum QueenBeeFrameType
        {
            HorizontalCharge,
            UpwardFly,
        }
        #endregion

        #region AI

        [OverrideAppliesTo(NPCID.QueenBee, typeof(QueenBeeAIClass), "QueenBeeAI", EntityOverrideContext.NPCAI)]
        public static bool QueenBeeAI(NPC npc)
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

            npc.dontTakeDamage = !target.ZoneJungle;

            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float frameType = ref npc.localAI[0];

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
                    npc.velocity.Y *= 0.5f;
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
                if ((npc.spriteDirection == 1 && (npc.Center.X - target.Center.X) > 540f) ||
                    (npc.spriteDirection == -1 && (npc.Center.X - target.Center.X) < -540f))
                {
                    npc.velocity *= 0.5f;
                    attackState = 0f;
                    speedBoost = 0f;
                    totalChargesDone++;
                    npc.netUpdate = true;
                }
            }

            if (totalChargesDone >= 2f)
                GotoNextAttackState(npc);
        }

        internal static void DoAttack_StingerBurst(NPC npc, Player target, ref float frameType, ref float attackTimer)
		{
            int shootRate = 50;
            int totalStingersToShoot = 5;
            bool canShoot = npc.Bottom.Y < target.position.Y;
            Vector2 baseStingerSpawnPosition = new Vector2(npc.Center.X + Main.rand.Next(20) * npc.spriteDirection, npc.Center.Y + npc.height * 0.3f);

            frameType = (int)QueenBeeFrameType.UpwardFly;
            npc.spriteDirection = Math.Sign(npc.velocity.X);

            if (attackTimer % shootRate == shootRate - 1f && canShoot && Collision.CanHit(baseStingerSpawnPosition, 1, 1, target.Center, 1, 1))
            {
                // Play a shoot sound when firing.
                Main.PlaySound(SoundID.Item17, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float shootSpeed = 12f;
                    for (int i = 0; i < 12; i++)
                    {
                        float offsetAngle = (MathHelper.TwoPi * 1.61808f * i / 12f);
                        Vector2 stingerSpawnPosition = baseStingerSpawnPosition + offsetAngle.ToRotationVector2() * MathHelper.Lerp(4f, 28f, i / 12f);
                        Vector2 stingerShootVelocity = (target.Center - baseStingerSpawnPosition).SafeNormalize(Vector2.UnitY) * shootSpeed;

                        int stinger = Utilities.NewProjectileBetter(stingerSpawnPosition, stingerShootVelocity, ProjectileID.Stinger, 75, 0f);
                        if (Main.projectile.IndexInRange(stinger))
                            Main.projectile[stinger].tileCollide = false;
                    }

                    npc.velocity *= 0.5f;
                    npc.netUpdate = true;
                }
            }

            // Fly above the target.
            Vector2 flyDestination = target.Center - Vector2.UnitY * 270f;
            DoHoverMovement(npc, flyDestination, 0.09f);

            if (attackTimer >= totalStingersToShoot * shootRate)
                GotoNextAttackState(npc);
        }

        internal static void DoAttack_HoneyBlast(NPC npc, Player target, ref float frameType, ref float attackTimer)
        {
            // Fly above the target.
            Vector2 flyDestination = target.Center - new Vector2((target.Center.X - npc.Center.X > 0).ToDirectionInt() * 270f, 240f);
            DoHoverMovement(npc, flyDestination, 0.09f);

            frameType = (int)QueenBeeFrameType.UpwardFly;
            npc.spriteDirection = (target.Center.X - npc.Center.X > 0).ToDirectionInt();

            // Release blasts of honey.
            bool honeyIsPoisonous = npc.life < npc.lifeMax * 0.5f;
            int shootRate = honeyIsPoisonous ? 15 : 25;
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
            int summonRate = 25;
            if (canShootHornetHives)
                summonRate = 60;

            if (MathHelper.Distance(target.Center.X, npc.Center.X) > 60f)
                npc.spriteDirection = (target.Center.X - npc.Center.X > 0).ToDirectionInt();

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % summonRate == summonRate - 1f)
            {
                Vector2 spawnPosition = new Vector2(npc.Center.X, npc.Center.Y + npc.height * 0.325f);
                spawnPosition += Main.rand.NextVector2Circular(25f, 25f);
            }
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

        internal const float Subphase2LifeRatio = 0.8f;
        internal const float Subphase3LifeRatio = 0.45f;
        internal const float Subphase4LifeRatio = 0.2f;
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
                    newAttackType = QueenBeeAttackState.HorizontalCharge;
                    break;
            }

            npc.ai[0] = (int)newAttackType;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        #endregion AI Utility Methods

        #endregion AI

        #region Drawing and Frames

        [OverrideAppliesTo(NPCID.QueenBee, typeof(QueenBeeAIClass), "QueenBeePreDraw", EntityOverrideContext.NPCFindFrame)]
        public static void QueenBeePreDraw(NPC npc, int frameHeight)
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
