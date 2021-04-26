using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Destroyer
{
	public class DestroyerAIClass
    {
        #region Enumerations
        public enum DestroyerAttackType
        {
            FlyAttack,
            DivingAttack,
            LaserBarrage,
            ProbeBombing,
            ElectricPulses
        }
        #endregion

        #region AI

        internal static readonly DestroyerAttackType[] Phase1AttackPattern = new DestroyerAttackType[]
        {
            DestroyerAttackType.FlyAttack,
        };

        internal static readonly DestroyerAttackType[] Phase2AttackPattern = new DestroyerAttackType[]
        {
            DestroyerAttackType.FlyAttack,
            DestroyerAttackType.LaserBarrage,
            DestroyerAttackType.ProbeBombing,
            DestroyerAttackType.DivingAttack,
        };

        internal static readonly DestroyerAttackType[] Phase3AttackPattern = new DestroyerAttackType[]
        {
            DestroyerAttackType.FlyAttack,
            DestroyerAttackType.DivingAttack,
            DestroyerAttackType.ProbeBombing,
            DestroyerAttackType.LaserBarrage,
            DestroyerAttackType.ElectricPulses,
            DestroyerAttackType.FlyAttack,
            DestroyerAttackType.ProbeBombing,
        };

        internal const int BodySegmentCount = 60;

        [OverrideAppliesTo(NPCID.TheDestroyer, typeof(DestroyerAIClass), "DestroyerAI", EntityOverrideContext.NPCAI)]
        public static bool DestroyerAI(NPC npc)
        {
            Player target = Main.player[npc.target];
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            npc.alpha = Utils.Clamp(npc.alpha - 20, 0, 255);

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase2 = lifeRatio < 0.65f;
            bool phase3 = lifeRatio < 0.35f;

            ref float attackTimer = ref npc.ai[2];
            ref float spawnedSegmentsFlag = ref npc.ai[3];

            if (spawnedSegmentsFlag == 0f)
            {
                SpawnDestroyerSegments(npc);
                spawnedSegmentsFlag = 1f;
                npc.netUpdate = true;
            }

            if (!target.active || target.dead || Main.dayTime)
            {
                npc.TargetClosest(false);
                target = Main.player[npc.target];
                if (!target.active || target.dead || Main.dayTime)
                {
                    npc.velocity.X *= 0.98f;
                    npc.velocity.Y += 0.22f;

                    if (npc.timeLeft > 240)
                        npc.timeLeft = 240;

                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    return false;
                }
            }

            void goToNextAIState()
            {
                // You cannot use ref locals inside of a delegate context.
                // You should be able to find most important, universal locals above, anyway.
                // Any others that don't have an explicit reference above are exclusively for
                // AI state manipulation.

                npc.ai[3]++;

                DestroyerAttackType[] patternToUse = phase2 ? Phase2AttackPattern : Phase1AttackPattern;
                if (phase3)
                    patternToUse = Phase3AttackPattern;
                DestroyerAttackType nextAttackType = patternToUse[(int)(npc.ai[3] % patternToUse.Length)];

                // Going to the next AI state.
                npc.ai[1] = (int)nextAttackType;

                // Resetting the attack timer.
                npc.ai[2] = 0f;

                // And the misc ai slots.
                for (int i = 0; i < 5; i++)
                {
                    npc.Infernum().ExtraAI[i] = 0f;
                }
            }

            switch ((DestroyerAttackType)(int)npc.ai[1])
            {
                case DestroyerAttackType.FlyAttack:
                    float idealSearchSpeed = phase2 ? 10.5f : 8.5f;
                    float idealLungeSpeed = idealSearchSpeed * 1.6f;

                    ref float fallCountdown = ref npc.Infernum().ExtraAI[0];

                    if (fallCountdown > 0f)
                    {
                        npc.velocity.X *= 0.985f;
                        if (npc.velocity.Y < 15f)
                            npc.velocity.Y += 0.3f;

                        fallCountdown--;
                    }
                    else
                    {
                        bool targetInLineOfSight = Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(target.Center)) > 0.87f;
                        if (targetInLineOfSight)
                            npc.velocity = npc.velocity.SafeNormalize(-Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), idealLungeSpeed, 0.1f);
                        else
                        {
                            float turnSpeed = MathHelper.Lerp(MathHelper.Pi * 0.008f, MathHelper.Pi * 0.018f, (float)Math.Pow(Utils.InverseLerp(600f, 60f, npc.Distance(target.Center), true), 4D));
                            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * MathHelper.Lerp(npc.velocity.Length(), idealSearchSpeed, 0.1f);
                        }

                        int totalSegmentsInAir = 0;
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            bool inAir = !Collision.SolidCollision(Main.npc[i].position, Main.npc[i].width, Main.npc[i].height);
                            inAir &= !TileID.Sets.Platforms[CalamityUtils.ParanoidTileRetrieval((int)Main.npc[i].Center.X / 16, (int)Main.npc[i].Center.Y / 16).type];
                            if (Main.npc[i].type == NPCID.TheDestroyerBody && Main.npc[i].active && inAir)
                                totalSegmentsInAir++;
                        }

                        if (totalSegmentsInAir / (float)BodySegmentCount > 0.625f)
                            fallCountdown = 120f;
                    }

                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    if (attackTimer >= 420f)
                        goToNextAIState();
                    break;
                case DestroyerAttackType.DivingAttack:
                    int diveTime = 200;
                    int ascendTime = 150;
                    float maxDiveDescendSpeed = 18f;
                    float diveAcceleration = 0.3f;
                    float maxDiveAscendSpeed = 30.5f;

                    if (attackTimer < diveTime)
                    {
                        if (Math.Abs(npc.velocity.X) > 2f)
                            npc.velocity.X *= 0.97f;
                        if (npc.velocity.Y < maxDiveDescendSpeed)
                            npc.velocity.Y += diveAcceleration;
                    }
                    else if (attackTimer < diveTime + ascendTime)
                    {
                        Vector2 idealVelocity = Vector2.Lerp(Vector2.UnitY, -Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X), 0.3f) * -maxDiveAscendSpeed;

                        npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), MathHelper.Pi * 0.016f, true) * MathHelper.Lerp(npc.velocity.Length(), maxDiveAscendSpeed, 0.1f);
                        
                        // Create shake effects for players.
                        Main.LocalPlayer.Infernum().CurrentScreenShakePower = Utils.InverseLerp(diveTime + ascendTime / 2, diveTime + ascendTime, attackTimer, true);
                        Main.LocalPlayer.Infernum().CurrentScreenShakePower = MathHelper.Lerp(Main.LocalPlayer.Infernum().CurrentScreenShakePower, 2f, 7f);
                        Main.LocalPlayer.Infernum().CurrentScreenShakePower *= Utils.InverseLerp(2000f, 1100f, npc.Distance(Main.LocalPlayer.Center), true);

                        if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= diveTime + ascendTime - 30f)
                        {
                            for (int i = 0; i < 2; i++)
                                Utilities.NewProjectileBetter(npc.Center, npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.8f) * 17f, ModContent.ProjectileType<DestroyerBomb>(), 0, 0f);
                        }
                    }

                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

                    if (attackTimer >= diveTime + ascendTime)
                        goToNextAIState();
                    break;
                case DestroyerAttackType.LaserBarrage:
                    Vector2 destination;
                    if (attackTimer <= 90f)
                    {
                        destination = target.Center + Vector2.UnitY * 400f;
                        destination.X -= Math.Sign(target.Center.X - npc.Center.X) * 2300f;
                        if (npc.WithinRange(destination, 23f))
                        {
                            npc.velocity.X = Math.Sign(target.Center.X - npc.Center.X) * MathHelper.Lerp(13f, 9f, 1f - lifeRatio);
                            npc.velocity.Y = 11f;
                            attackTimer = 90f;
                        }
                        else
                        {
                            npc.velocity = Vector2.Lerp(npc.velocity, npc.DirectionTo(destination) * 20f, 0.05f);
                            attackTimer--;
                        }
                    }
                    else
                        npc.velocity.Y *= 0.98f;

                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    if (attackTimer >= 450f)
                        goToNextAIState();
                    break;
                case DestroyerAttackType.ProbeBombing:
                    destination = target.Center + (attackTimer * MathHelper.TwoPi / 150f).ToRotationVector2() * MathHelper.Lerp(1580f, 2700f, Utils.InverseLerp(360f, 420f, attackTimer, true));
                    npc.velocity = npc.SafeDirectionTo(destination) * MathHelper.Min(MathHelper.Lerp(31f, 15f, Utils.InverseLerp(360f, 420f, attackTimer, true)), npc.Distance(destination));
                    if (npc.WithinRange(destination, 30f))
                        npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    else
                        npc.rotation = npc.rotation.AngleTowards((attackTimer + 7f) * MathHelper.TwoPi / 150f + MathHelper.PiOver2, 0.15f);

                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 60f == 59f)
                    {
                        int probeCount = (int)MathHelper.Lerp(3f, 5f, 1f - lifeRatio);
                        for (int i = 0; i < probeCount; i++)
                        {
                            int probe = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.Probe);
                            Main.npc[probe].velocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.45f) * Main.rand.NextFloat(9f, 16f);
                        }
                    }

                    if (attackTimer >= 425f)
                        goToNextAIState();
                    break;
                case DestroyerAttackType.ElectricPulses:
                    idealSearchSpeed = 9.75f;

                    fallCountdown = ref npc.Infernum().ExtraAI[0];

                    if (fallCountdown > 0f)
                    {
                        npc.velocity.X *= 0.985f;
                        if (npc.velocity.Y < 11f)
                            npc.velocity.Y += 0.2f;

                        fallCountdown--;
                    }
                    else
                    {
                        bool targetInLineOfSight = Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(target.Center)) > 0.87f;
                        if (targetInLineOfSight)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 35f == 34f)
                                Utilities.NewProjectileBetter(npc.Center, npc.velocity.SafeNormalize(Vector2.UnitY) * 4f, ModContent.ProjectileType<ElectricPulse>(), 0, 0f);
                        }
                        else
                        {
                            float turnSpeed = MathHelper.Lerp(MathHelper.Pi * 0.008f, MathHelper.Pi * 0.018f, (float)Math.Pow(Utils.InverseLerp(600f, 60f, npc.Distance(target.Center), true), 4D));
                            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * MathHelper.Lerp(npc.velocity.Length(), idealSearchSpeed, 0.1f);
                        }

                        int totalSegmentsInAir = 0;
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            bool inAir = !Collision.SolidCollision(Main.npc[i].position, Main.npc[i].width, Main.npc[i].height);
                            inAir &= !TileID.Sets.Platforms[CalamityUtils.ParanoidTileRetrieval((int)Main.npc[i].Center.X / 16, (int)Main.npc[i].Center.Y / 16).type];
                            if (Main.npc[i].type == NPCID.TheDestroyerBody && Main.npc[i].active && inAir)
                                totalSegmentsInAir++;
                        }

                        if (totalSegmentsInAir / (float)BodySegmentCount > 0.625f)
                            fallCountdown = 120f;
                    }

                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    if (attackTimer >= 420f)
                        goToNextAIState();
                    break;
            }

            attackTimer++;
            return false;
		}

        [OverrideAppliesTo(NPCID.TheDestroyerBody, typeof(DestroyerAIClass), "DestroyerBodyAI", EntityOverrideContext.NPCAI)]
        public static bool DestroyerBodyAI(NPC npc)
        {
            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            if (!aheadSegment.active)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                npc.netUpdate = true;
            }

            npc.Calamity().DR = 0.2f;

            // Inherit various attributes from the head segment.
            // This code will go upstream across every segment, until it reaches the head.
            npc.scale = aheadSegment.scale;
            npc.Opacity = aheadSegment.Opacity;

            npc.dontTakeDamage = aheadSegment.dontTakeDamage;
            npc.damage = npc.dontTakeDamage ? 0 : npc.defDamage;

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.03f);

            directionToNextSegment = directionToNextSegment.SafeNormalize(Vector2.Zero);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment * npc.width * npc.scale * 0.725f;
            npc.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();

            float segmentNumber = npc.localAI[0];

            if (!Main.npc.IndexInRange(npc.realLife) || !Main.npc[npc.realLife].active)
            {
                npc.active = false;
                return false;
            }

            float headAttackTimer = Main.npc[npc.realLife].ai[2];
            DestroyerAttackType headAttackType = (DestroyerAttackType)(int)Main.npc[npc.realLife].ai[1];

            if (headAttackType == DestroyerAttackType.LaserBarrage)
            {
                bool isMovingHorizontally = Math.Abs(Vector2.Dot(directionToNextSegment, Vector2.UnitX)) > 0.95f && headAttackTimer >= 230f;
                if (Main.netMode != NetmodeID.MultiplayerClient && isMovingHorizontally && headAttackTimer % BodySegmentCount == segmentNumber && npc.whoAmI % 3 == 0)
                {
                    for (int i = -1; i <= 1; i += 2)
                    {
                        int laser = Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY * i * 7f, ProjectileID.DeathLaser, 90, 0f);
                        Main.projectile[laser].timeLeft = 250;
                        Main.projectile[laser].tileCollide = false;
                    }
                }
            }
            return false;
        }

        [OverrideAppliesTo(NPCID.Probe, typeof(DestroyerAIClass), "ProbeAI", EntityOverrideContext.NPCAI)]
        public static bool ProbeAI(NPC npc)
        {
            npc.TargetClosest();
            Player target = Main.player[npc.target];
            Vector2 destination = target.Center - Vector2.UnitY.RotatedBy(MathHelper.Lerp(-0.97f, 0.97f, npc.whoAmI % 16f / 16f)) * 300f;

            Lighting.AddLight(npc.Center, Color.Red.ToVector3() * 1.6f);

            if (npc.ai[0] == 0f)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.DirectionTo(destination) * 11f, 0.1f);
                if (npc.WithinRange(destination, npc.velocity.Length() * 1.35f))
                {
                    npc.velocity = npc.DirectionTo(target.Center) * -7f;
                    npc.ai[0] = 1f;
                    npc.netUpdate = true;
                }
                npc.rotation = npc.AngleTo(target.Center);
            }
            
            if (npc.ai[0] == 1f)
            {
                ref float time = ref npc.ai[1];
                npc.velocity *= 0.975f;
                time++;

                if (time >= 60f)
                {
                    npc.velocity = npc.DirectionTo(target.Center) * 20f;
                    npc.ai[0] = 2f;
                    npc.netUpdate = true;
                }
                npc.rotation = npc.AngleTo(target.Center);
            }

            if (npc.ai[0] == 2f)
            {
                if ((Collision.SolidCollision(npc.position, npc.width, npc.height) || npc.justHit) && !Main.dedServ)
                {
                    for (int i = 0; i < 36; i++)
                    {
                        Dust energy = Dust.NewDustDirect(npc.position, npc.width, npc.height, 182);
                        energy.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 7f);
                        energy.noGravity = true;
                    }

                    npc.active = false;
                    npc.netUpdate = true;
                }
                npc.rotation = npc.velocity.ToRotation();
                npc.damage = 95;
            }

            npc.rotation += MathHelper.Pi;
            return false;
        }

        internal static void SpawnDestroyerSegments(NPC head)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int previousSegmentIndex = head.whoAmI;
            for (int i = 0; i < BodySegmentCount + 1; i++)
            {
                int newSegment;
                if (i >= 0 && i < BodySegmentCount)
                    newSegment = NPC.NewNPC((int)head.position.X + (head.width / 2), (int)head.position.Y + (head.height / 2), NPCID.TheDestroyerBody, head.whoAmI);
                else
                    newSegment = NPC.NewNPC((int)head.position.X + (head.width / 2), (int)head.position.Y + (head.height / 2), NPCID.TheDestroyerTail, head.whoAmI);

                Main.npc[newSegment].realLife = head.whoAmI;

                // Set the ahead segment.
                Main.npc[newSegment].ai[1] = previousSegmentIndex;
                Main.npc[previousSegmentIndex].ai[0] = newSegment;

                // And the segment number.
                Main.npc[newSegment].localAI[0] = i;

                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, newSegment, 0f, 0f, 0f, 0);

                previousSegmentIndex = newSegment;
            }
        }
        #endregion
    }
}