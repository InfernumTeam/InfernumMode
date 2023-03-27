using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Ravager;
using CalamityMod.Particles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using InfernumMode.Content.Dusts;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Ravager
{
    public class RavagerBodyBehaviorOverride : NPCBehaviorOverride
    {
        public struct RavagerPhaseInfo
        {
            public bool HandsAreAlive;

            public bool LegsAreAlive;

            public bool HeadIsAttached;

            public bool FreeHeadExists;

            public float LifeRatio;

            public bool InPhase2 => !HandsAreAlive && !LegsAreAlive && !HeadIsAttached;

            public static bool ShouldBeBuffed => false;

            public RavagerPhaseInfo(bool hands, bool legs, bool head, bool freeHead, float lifeRatio)
            {
                HandsAreAlive = hands;
                LegsAreAlive = legs;
                HeadIsAttached = head;
                FreeHeadExists = freeHead;
                LifeRatio = lifeRatio;
            }
        }

        public override int NPCOverrideType => ModContent.NPCType<RavagerBody>();

        public const int AttackDelay = 135;

        public const float BaseDR = 0.325f;

        public const float ArenaBorderOffset = 1850f;

        #region Enumerations
        public enum RavagerAttackType
        {
            SingleBurstsOfBlood,
            RegularJumps,
            BarrageOfBlood,
            SingleBurstsOfUpwardDarkFlames,
            DownwardFistSlam,
            SlamAndCreateMovingFlamePillars,
            WallSlams,
            DetachedHeadCinderRain
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Ensure that the NPC always draws things, even when far away.
            // Not doing this will result in the arena not being drawn if far from the target.
            NPCID.Sets.MustAlwaysDraw[npc.type] = true;

            // Prevent natural despawns.
            npc.timeLeft = 72000;

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Create limbs.
            if (npc.localAI[0] == 0f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int leftLeg = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X - 70, (int)npc.Center.Y + 88, ModContent.NPCType<RavagerLegLeft>(), npc.whoAmI);
                int rightLeg = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X + 70, (int)npc.Center.Y + 88, ModContent.NPCType<RavagerLegRight>(), npc.whoAmI);

                int leftClaw = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X - 120, (int)npc.Center.Y + 50, ModContent.NPCType<RavagerClawLeft>(), npc.whoAmI);
                int rightClaw = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X + 120, (int)npc.Center.Y + 50, ModContent.NPCType<RavagerClawRight>(), npc.whoAmI);

                // Make claws and legs share their own distinct HP pools, instead of being separate.
                if (Main.npc.IndexInRange(leftLeg) && Main.npc.IndexInRange(rightLeg))
                    Main.npc[leftLeg].realLife = rightLeg;
                if (Main.npc.IndexInRange(leftClaw) && Main.npc.IndexInRange(rightClaw))
                    Main.npc[leftClaw].realLife = rightClaw;

                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X + 1, (int)npc.Center.Y - 20, ModContent.NPCType<RavagerHead>(), npc.whoAmI);
                npc.localAI[0] = 1f;
            }

            CalamityGlobalNPC.scavenger = npc.whoAmI;

            // Fade in.
            ref float flameJetInterpolant = ref npc.localAI[1];
            npc.alpha = Utils.Clamp(npc.alpha - 10, 0, 255);

            // Reset things every frame.
            npc.Calamity().DR = BaseDR;
            npc.damage = npc.defDamage;

            npc.noTileCollide = false;
            if (!target.active || target.dead || !npc.WithinRange(target.Center, 7200f))
            {
                flameJetInterpolant = MathHelper.Clamp(flameJetInterpolant + 0.1f, 0f, 1f);

                npc.noTileCollide = true;
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * -30f, 0.2f);
                if (!npc.WithinRange(target.Center, 1000f) || Main.rand.NextBool(90))
                {
                    npc.life = 0;
                    npc.active = false;
                    npc.netUpdate = true;
                }
                return false;
            }

            // Constantly give the target Weak Pertrification.
            if (Main.netMode != NetmodeID.Server)
            {
                if (!target.dead && target.active)
                    target.AddBuff(ModContent.BuffType<WeakPetrification>(), 15);
            }

            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float attackDelay = ref npc.Infernum().ExtraAI[5];
            ref float armsCanPunch = ref npc.Infernum().ExtraAI[6];
            ref float armsShouldSlamIntoGround = ref npc.Infernum().ExtraAI[7];
            ref float horizontalArenaCenterX = ref npc.Infernum().ExtraAI[8];

            // Determine phase information.
            bool leftLegActive = false;
            bool rightLegActive = false;
            bool leftClawActive = false;
            bool rightClawActive = false;
            bool headActive = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<RavagerHead>())
                    headActive = true;
                if (Main.npc[i].active && Main.npc[i].Infernum().ExtraAI[0] == 0f && Main.npc[i].type == ModContent.NPCType<RavagerClawRight>())
                    rightClawActive = true;
                if (Main.npc[i].active && Main.npc[i].Infernum().ExtraAI[0] == 0f && Main.npc[i].type == ModContent.NPCType<RavagerClawLeft>())
                    leftClawActive = true;
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<RavagerLegRight>())
                    rightLegActive = true;
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<RavagerLegLeft>())
                    leftLegActive = true;
            }

            float lifeRatio = npc.life / (float)npc.lifeMax;
            RavagerPhaseInfo phaseInfo = new(leftClawActive && rightClawActive, leftLegActive && rightLegActive, headActive, NPC.AnyNPCs(ModContent.NPCType<RavagerHead2>()), lifeRatio);

            float gravity = 0.625f;
            if (phaseInfo.InPhase2)
                gravity += 0.2f;

            // Reset things.
            armsCanPunch = 0f;
            flameJetInterpolant = 0f;
            armsShouldSlamIntoGround = 0f;
            npc.dontTakeDamage = !phaseInfo.InPhase2;
            npc.gfxOffY = -12;

            // Make the attack delay pass.
            attackDelay++;
            if (attackDelay < AttackDelay)
            {
                npc.damage = 0;
                npc.noGravity = false;
                npc.dontTakeDamage = true;
                return false;
            }

            // Create the horizontal walls and reset the phase cycle once in the second phase.
            if (horizontalArenaCenterX == 0f && phaseInfo.InPhase2)
            {
                horizontalArenaCenterX = target.Center.X;
                npc.Infernum().ExtraAI[9] = 0f;
                npc.netUpdate = true;
            }

            // Restrict the target's position once the arena has been decided.
            if (horizontalArenaCenterX != 0f)
            {
                float left = horizontalArenaCenterX - ArenaBorderOffset + 28f;
                float right = horizontalArenaCenterX + ArenaBorderOffset - 28f;
                target.Center = Vector2.Clamp(target.Center, new Vector2(left, -100f), new Vector2(right, Main.maxTilesY * 16f + 100f));
            }

            // Perform attacks.
            switch ((RavagerAttackType)attackType)
            {
                case RavagerAttackType.RegularJumps:
                    DoBehavior_RegularJumps(npc, target, phaseInfo, ref attackTimer, ref gravity);
                    break;
                case RavagerAttackType.SingleBurstsOfBlood:
                    armsCanPunch = 1f;
                    DoBehavior_BurstsOfBlood(npc, target, phaseInfo, false, ref attackTimer);
                    break;
                case RavagerAttackType.BarrageOfBlood:
                    armsCanPunch = 1f;
                    DoBehavior_BurstsOfBlood(npc, target, phaseInfo, true, ref attackTimer);
                    break;
                case RavagerAttackType.DownwardFistSlam:
                    DoBehavior_DownwardFistSlam(npc, target, phaseInfo, ref flameJetInterpolant, ref attackTimer, ref gravity, ref armsShouldSlamIntoGround);
                    break;
                case RavagerAttackType.SlamAndCreateMovingFlamePillars:
                    DoBehavior_SlamAndCreateMovingFlamePillars(npc, target, phaseInfo, ref flameJetInterpolant, ref attackTimer, ref gravity);
                    break;
                case RavagerAttackType.WallSlams:
                    DoBehavior_WallSlams(npc, target, phaseInfo, ref attackTimer);
                    break;
                case RavagerAttackType.DetachedHeadCinderRain:
                    DoBehavior_DetachedHeadCinderRain(npc, target, phaseInfo, ref attackTimer);
                    break;
            }
            attackTimer++;

            // Do custom gravity stuff.
            npc.noGravity = true;
            EnforceCustomGravity(npc, gravity);

            return false;
        }

        public static void DoBehavior_RegularJumps(NPC npc, Player target, RavagerPhaseInfo phaseInfo, ref float attackTimer, ref float gravity)
        {
            int telegraphTime = 130;
            int jumpCount = 3;
            int jumpDelay = 45;
            int emberBurstCount = 3;
            float jumpIntensityFactor = 1.25f;

            if (!phaseInfo.HandsAreAlive)
                jumpIntensityFactor *= 1.125f;
            if (!phaseInfo.LegsAreAlive)
                jumpIntensityFactor *= 1.125f;

            ref float jumpSubstate = ref npc.Infernum().ExtraAI[0];
            ref float jumpCounter = ref npc.Infernum().ExtraAI[1];
            ref float attackDelayTimer = ref npc.Infernum().ExtraAI[2];
            ref float tileCollisionLineY = ref npc.Infernum().ExtraAI[3];

            // Sit in place and create flame particles as a telegraph to indicate the impending jump.
            // While the player needs to be near Ravager to see the particles, it should still be fine due to
            // having more time for them to react because of the distance between them and Ravager.
            if (attackDelayTimer < telegraphTime)
            {
                int dustID = ModContent.DustType<RavagerMagicDust>();
                Vector2[] flamePillarTops = new Vector2[]
                {
                    npc.Center + new Vector2(-112f, -38f),
                    npc.Center + new Vector2(112f, -38f),
                    npc.Center + new Vector2(-46f, -82f),
                    npc.Center + new Vector2(46f, -82f),
                };

                if (attackDelayTimer < telegraphTime * 0.67f)
                {
                    foreach (Vector2 flamePillarTop in flamePillarTops)
                    {
                        // Create rising blue cinders.
                        Dust fire = Dust.NewDustPerfect(flamePillarTop, dustID);
                        fire.velocity = -Vector2.UnitY.RotatedByRandom(0.21f) * Main.rand.NextFloat(5f, 8.5f);
                        fire.scale = Main.rand.NextFloat(1f, 1.45f);
                        fire.fadeIn = Main.rand.NextFloat(0.4f);
                        fire.noGravity = true;

                        if (Main.rand.NextBool(4))
                            fire.fadeIn *= 2.4f;

                        // Create converging particles.
                        for (int i = 0; i < 2; i++)
                        {
                            if (!Main.rand.NextBool(8))
                                continue;

                            float particleScale = Main.rand.NextFloat(0.84f, 1.04f);
                            Vector2 particleSpawnPosition = flamePillarTop + Main.rand.NextVector2Unit() * Main.rand.NextFloat(120f, 145f);
                            Vector2 particleVelocity = (flamePillarTop - particleSpawnPosition) * 0.035f;
                            Color particleColor = Color.Lerp(Color.DarkBlue, Color.Cyan, Main.rand.NextFloat(0.25f, 0.75f));
                            GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(particleSpawnPosition, particleVelocity, particleScale, particleColor, 40, 1f, 3.6f));
                        }
                    }
                }
                npc.velocity.X *= 0.8f;
                attackDelayTimer++;
                return;
            }

            // Jump towards the target if they're far enough away and enough time passes.
            if (jumpSubstate == 0f && npc.velocity.Y == 0f)
            {
                if (attackTimer >= jumpDelay)
                {
                    attackTimer = 0f;
                    jumpSubstate = 1f;
                    tileCollisionLineY = target.Top.Y;

                    npc.velocity.Y -= 8f;
                    if (target.position.Y + target.height < npc.Center.Y)
                        npc.velocity.Y -= 1f;
                    if (target.position.Y + target.height < npc.Center.Y - 40f)
                        npc.velocity.Y -= 1.2f;
                    if (target.position.Y + target.height < npc.Center.Y - 80f)
                        npc.velocity.Y -= 1.4f;
                    if (target.position.Y + target.height < npc.Center.Y - 120f)
                        npc.velocity.Y -= 2f;
                    if (target.position.Y + target.height < npc.Center.Y - 160f)
                        npc.velocity.Y -= 3.2f;
                    if (target.position.Y + target.height < npc.Center.Y - 200f)
                        npc.velocity.Y -= 3.2f;
                    if (target.position.Y + target.height < npc.Center.Y - 400f)
                        npc.velocity.Y -= 8f;
                    if (target.position.Y + target.height < npc.Center.Y - 780f)
                        npc.velocity.Y -= 15f;
                    if (!Collision.CanHit(npc.Center, 1, 1, target.Center, 1, 1))
                        npc.velocity.Y -= 3.84f;

                    // Jump far higher if the target is close, to allow them to have openings and encourage close combat.
                    if (MathHelper.Distance(npc.Center.X, target.Center.X) < 425f || MathHelper.Distance(npc.Center.Y, target.Center.Y) < 240f)
                        npc.velocity.Y -= 9.5f;

                    // Release fireballs at the target if they're far enough away.
                    if (Main.netMode != NetmodeID.MultiplayerClient && !npc.WithinRange(target.Center, 540f))
                    {
                        for (int i = 0; i < emberBurstCount; i++)
                        {
                            float offsetAngle = MathHelper.Lerp(-0.51f, 0.51f, i / (float)(emberBurstCount - 1f));
                            Vector2 emberShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * 6f;
                            Utilities.NewProjectileBetter(npc.Center + emberShootVelocity * 9f, emberShootVelocity, ModContent.ProjectileType<DarkMagicFireball>(), 180, 0f);
                        }
                    }

                    npc.velocity.X = (target.Center.X > npc.Center.X).ToDirectionInt() * 18f;
                    npc.velocity *= jumpIntensityFactor * 0.9f;
                    npc.netUpdate = true;
                }
            }

            // Handle post-jump behaviors.
            if (jumpSubstate == 1f)
            {
                gravity *= jumpIntensityFactor;
                if (attackTimer < 16f)
                {
                    npc.damage = 0;
                    npc.dontTakeDamage = true;
                }

                // Make stomp sounds and particles when hitting the ground again.
                if (npc.velocity.Y == 0f)
                {
                    CreateGroundImpactEffects(npc);
                    attackTimer = 0f;
                    jumpSubstate = 0f;
                    jumpCounter++;

                    target.Infernum_Camera().CurrentScreenShakePower = 6f;
                    if (jumpCounter >= jumpCount)
                    {
                        npc.velocity.X = 0f;
                        SelectNextAttack(npc, phaseInfo);
                    }

                    npc.netUpdate = true;
                }

                // Fall through tiles in the way.
                npc.noTileCollide = npc.Bottom.Y <= tileCollisionLineY;
            }
            else
                npc.velocity.X *= 0.4f;
        }

        public static void DoBehavior_BurstsOfBlood(NPC npc, Player target, RavagerPhaseInfo phaseInfo, bool multiplePerShot, ref float attackTimer)
        {
            int shootDelay = 84;
            int bloodShootRate = 12;
            int bloodShootTime = 180;
            int totalInstancesPerShot = 1;
            int postAttackTransitionDelay = 75;
            int bloodDamage = 180;
            float destinationOffsetVariance = 200f;

            if (!phaseInfo.HandsAreAlive)
            {
                shootDelay -= 35;
                postAttackTransitionDelay -= 15;
            }

            if (!phaseInfo.LegsAreAlive || !phaseInfo.HandsAreAlive)
                destinationOffsetVariance *= 0.67f;

            if (phaseInfo.InPhase2)
            {
                shootDelay -= 10;
                bloodShootRate -= 2;
            }

            if (multiplePerShot)
            {
                bloodShootRate *= 5;
                totalInstancesPerShot = 7;
                destinationOffsetVariance *= 2.5f;
            }

            // Sit in place and prevent sliding.
            npc.velocity.X *= 0.85f;

            if (attackTimer >= shootDelay + bloodShootTime + postAttackTransitionDelay)
                SelectNextAttack(npc, phaseInfo);

            // Wait before shooting and at the end of the attack. The arms will attack during this period if they are present, however.
            if (attackTimer < shootDelay || attackTimer >= shootDelay + bloodShootTime)
                return;

            if (attackTimer % bloodShootRate == bloodShootRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item45, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 shootPosition = npc.Center - Vector2.UnitY * 40f;
                    for (int i = 0; i < totalInstancesPerShot; i++)
                    {
                        Vector2 shootDestination = target.Center + Vector2.UnitX * Main.rand.NextFloatDirection() * destinationOffsetVariance;

                        // The ideal velocity for falling can be calculated based on the horizontal range formula in the following way:
                        // First, the initial formula: R = v^2 * sin(2t) / g
                        // By assuming the angle that will yield the most distance is used, we can omit the sine entirely, since its maximum value is 1, leaving the following:
                        // R = v^2 / g
                        // We wish to find v, so rewritten, we arrive at:
                        // R * g = v^2
                        // v = sqrt(R * g), as the solution.
                        // However, to prevent weird looking angles, a clamp is performed to ensure the result stays within natural bounds.
                        float horizontalDistance = Vector2.Distance(shootPosition, shootDestination);
                        float idealShootSpeed = (float)Math.Sqrt(horizontalDistance * UnholyBloodGlob.Gravity);
                        float bloodShootSpeed = MathHelper.Clamp(idealShootSpeed, 8.4f, 24f);
                        Vector2 bloodShootVelocity = Utilities.GetProjectilePhysicsFiringVelocity(shootPosition, shootDestination, UnholyBloodGlob.Gravity, bloodShootSpeed, out _);
                        if (multiplePerShot)
                            bloodShootVelocity += Main.rand.NextVector2Circular(2f, 2f);

                        int blood = Utilities.NewProjectileBetter(shootPosition, bloodShootVelocity, ModContent.ProjectileType<UnholyBloodGlob>(), bloodDamage, 0f);
                        if (Main.projectile.IndexInRange(blood))
                            Main.projectile[blood].ai[1] = target.Center.Y;
                    }
                }
            }
        }

        public static void DoBehavior_DownwardFistSlam(NPC npc, Player target, RavagerPhaseInfo phaseInfo, ref float flameJetInterpolant, ref float attackTimer, ref float gravity, ref float armsShouldSlamIntoGround)
        {
            int hoverTime = 95;
            int sitOnGroundTime = 72;

            if (!phaseInfo.LegsAreAlive)
            {
                hoverTime = 50;
                sitOnGroundTime = 25;
            }

            int slamSlowdownTime = (int)(hoverTime * 0.32f);
            int projectileShootCount = 27;
            int slamCount = 3;
            int bloodDamage = 180;
            int spikeDamage = 185;
            float projectileAngularSpread = MathHelper.ToRadians(61f);
            float horizontalSpikeSpeed = 8.4f;

            if (phaseInfo.InPhase2)
            {
                projectileShootCount += 5;
                slamCount++;
                horizontalSpikeSpeed *= 1.3f;
            }

            ref float hasDoneGroundHitEffects = ref npc.Infernum().ExtraAI[0];
            ref float slamCounter = ref npc.Infernum().ExtraAI[1];

            // Hover in place.
            if (attackTimer < hoverTime && hasDoneGroundHitEffects == 0f)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 540f;
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 30f;
                if (npc.WithinRange(hoverDestination, 85f))
                    idealVelocity = Vector2.Zero;

                // Slow down prior slamming downward and make arms slam first.
                if (attackTimer >= hoverTime - slamSlowdownTime)
                {
                    armsShouldSlamIntoGround = 1f;
                    idealVelocity = Vector2.Zero;
                    npc.velocity.X *= 0.8f;
                }
                else
                {
                    npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.02f);
                    npc.noTileCollide = true;
                }

                // Create flame jets.
                flameJetInterpolant = Utils.GetLerpValue(0f, 8f, attackTimer, true) * Utils.GetLerpValue(hoverTime, hoverTime - 12f, attackTimer, true);

                // Disable cheap hits.
                npc.damage = 0;

                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, idealVelocity.X, 0.12f);
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, idealVelocity.Y, 0.24f);

                // Disable gravity during the hover.
                gravity = 0f;
                return;
            }

            // Keep arms in the ground.
            armsShouldSlamIntoGround = 1f;

            // Slam into the ground.
            gravity *= 1.84f;

            // Disable any tiny amounts of remaining horizontal movement.
            npc.velocity.X = 0f;

            // Make stomp sounds and particles when hitting the ground.
            // Also release an even spread of projectiles into the air. A small amount of variance is used to spice things up, but not much.
            bool hitGround = npc.velocity.Y == 0f || (npc.Center.Y >= target.Top.Y && Utilities.ActualSolidCollisionTop(npc.TopLeft, npc.width, npc.height));
            if (hitGround && hasDoneGroundHitEffects == 0f)
            {
                CreateGroundImpactEffects(npc);

                // Release blood into the air.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < projectileShootCount; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-projectileAngularSpread, projectileAngularSpread, i / (float)(projectileShootCount - 1f));
                        if (Math.Abs(offsetAngle) < 0.22f)
                            continue;

                        Vector2 bloodShootVelocity = -Vector2.UnitY.RotatedBy(offsetAngle) * Main.rand.NextFloat(19f, 21f) + Main.rand.NextVector2Circular(1.6f, 1.6f);
                        int blood = Utilities.NewProjectileBetter(npc.Center - Vector2.UnitY * 40f, bloodShootVelocity, ModContent.ProjectileType<UnholyBloodGlob>(), bloodDamage, 0f);
                        if (Main.projectile.IndexInRange(blood))
                            Main.projectile[blood].ai[1] = target.Center.Y;
                    }

                    // Create spikes that move in both horizontal directions if no arms are present.
                    if (!phaseInfo.HandsAreAlive)
                    {
                        for (int i = -1; i <= 1; i += 2)
                        {
                            Vector2 spikeVelocity = Vector2.UnitX * horizontalSpikeSpeed * i;
                            Utilities.NewProjectileBetter(npc.Bottom, spikeVelocity, ModContent.ProjectileType<GroundBloodSpikeCreator>(), spikeDamage, 0f);
                        }
                    }
                    npc.netUpdate = true;
                }

                target.Infernum_Camera().CurrentScreenShakePower = 10f;
                hasDoneGroundHitEffects = 1f;
                attackTimer = 0f;
                npc.velocity.Y = 0f;
                while (Utilities.ActualSolidCollisionTop(npc.TopLeft, npc.width, npc.height + 16))
                    npc.position.Y -= 2f;

                npc.netUpdate = true;
            }

            if (hasDoneGroundHitEffects == 1f && attackTimer >= sitOnGroundTime)
            {
                slamCounter++;
                if (slamCounter >= slamCount)
                    SelectNextAttack(npc, phaseInfo);
                else
                {
                    hasDoneGroundHitEffects = 0f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_SlamAndCreateMovingFlamePillars(NPC npc, Player target, RavagerPhaseInfo phaseInfo, ref float flameJetInterpolant, ref float attackTimer, ref float gravity)
        {
            int hoverTime = 64;
            int groundShootDelay = 38;
            int sitOnGroundTime = groundShootDelay + 180;
            int fireReleaseRate = 20;
            int spikeReleaseRate = 64;
            int slamSlowdownTime = (int)(hoverTime * 0.32f);
            int flamePillarDamage = 210;
            int spikeDamage = 185;
            float horizontalSpikeSpeed = MathHelper.Lerp(7.6f, 10f, 1f - phaseInfo.LifeRatio);
            float horizontalStepPerPillar = MathHelper.Lerp(250f, 300f, 1f - phaseInfo.LifeRatio);
            ref float hasDoneGroundHitEffects = ref npc.Infernum().ExtraAI[0];
            ref float flamePillarHorizontalOffset = ref npc.Infernum().ExtraAI[1];

            // Hover in place.
            if (attackTimer < hoverTime && hasDoneGroundHitEffects == 0f)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 540f;
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 30f;
                if (npc.WithinRange(hoverDestination, 85f))
                    idealVelocity = Vector2.Zero;

                // Slow down prior slamming downward and make arms slam first.
                if (attackTimer >= hoverTime - slamSlowdownTime)
                {
                    idealVelocity = Vector2.Zero;
                    npc.velocity.X *= 0.8f;
                }
                else
                {
                    npc.noTileCollide = true;
                    npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.02f);
                }

                // Disable cheap hits.
                npc.damage = 0;

                // Create flame jets.
                flameJetInterpolant = Utils.GetLerpValue(0f, 8f, attackTimer, true) * Utils.GetLerpValue(hoverTime, hoverTime - 12f, attackTimer, true);

                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, idealVelocity.X, 0.12f);
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, idealVelocity.Y, 0.24f);

                // Disable gravity during the hover.
                gravity = 0f;
                return;
            }

            // Slam into the ground.
            gravity *= 1.84f;

            // Disable any tiny amounts of remaining horizontal movement.
            npc.velocity.X = 0f;

            // Make stomp sounds and particles when hitting the ground.
            bool hitGround = npc.velocity.Y == 0f;
            if (hitGround && hasDoneGroundHitEffects == 0f)
            {
                CreateGroundImpactEffects(npc);
                hasDoneGroundHitEffects = 1f;
                attackTimer = 0f;
                npc.velocity.X = 0f;
                npc.netUpdate = true;

                target.Infernum_Camera().CurrentScreenShakePower = 10f;

                // Create flame pillar telegraphs.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = -15; i <= 15; i++)
                    {
                        Vector2 telegraphSpawnPosition = npc.Bottom + Vector2.UnitX * horizontalStepPerPillar * i;
                        telegraphSpawnPosition.X += 42f;
                        int telegraph = Utilities.NewProjectileBetter(telegraphSpawnPosition, Vector2.Zero, ModContent.ProjectileType<DarkFlamePillarTelegraph>(), 0, 0f);
                        if (Main.projectile.IndexInRange(telegraph))
                            Main.projectile[telegraph].ai[0] = Math.Abs(i) * fireReleaseRate + groundShootDelay;
                    }
                }
            }

            // Create flame projectiles and spikes once on the ground.
            if (hasDoneGroundHitEffects == 1f && attackTimer >= groundShootDelay)
            {
                // Create flame pillars.
                bool skipPillar = npc.WithinRange(target.Center, 500f) && flamePillarHorizontalOffset < 340f;
                if (attackTimer % fireReleaseRate == fireReleaseRate - 1f)
                {
                    if (!skipPillar)
                        SoundEngine.PlaySound(SoundID.Item74, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        flamePillarHorizontalOffset += horizontalStepPerPillar;
                        for (int i = -1; i <= 1; i += 2)
                        {
                            Vector2 fireSpawnPosition = npc.Bottom + Vector2.UnitX * flamePillarHorizontalOffset * i;
                            if (MathHelper.Distance(target.Center.Y, npc.Center.Y) > 800f)
                                fireSpawnPosition.Y = target.Bottom.Y;

                            fireSpawnPosition.Y += 36f;
                            if (!skipPillar)
                                Utilities.NewProjectileBetter(fireSpawnPosition, Vector2.Zero, ModContent.ProjectileType<DarkFlamePillar>(), flamePillarDamage, 0f);
                        }
                        npc.netUpdate = true;
                    }
                }

                // Create spikes.
                if (attackTimer % spikeReleaseRate == spikeReleaseRate - 1f && Main.netMode != NetmodeID.MultiplayerClient && !skipPillar)
                {
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 spikeVelocity = Vector2.UnitX * horizontalSpikeSpeed * i;
                        Utilities.NewProjectileBetter(npc.Bottom, spikeVelocity, ModContent.ProjectileType<GroundBloodSpikeCreator>(), spikeDamage, 0f);
                    }
                }
            }

            if (hasDoneGroundHitEffects == 1f && attackTimer >= sitOnGroundTime)
                SelectNextAttack(npc, phaseInfo);
        }

        public static void DoBehavior_WallSlams(NPC npc, Player target, RavagerPhaseInfo phaseInfo, ref float attackTimer)
        {
            int shootDelay = 64;
            int wallCreateRate = 48;
            int wallCreateTime = 360;
            int attackTransitionDelay = 70;
            int wallDamage = 200;
            float spaceBetweenWalls = MathHelper.Lerp(500f, 425f, 1f - phaseInfo.LifeRatio);

            // WHY ARE YOU SLIDING AWAY YOU MOTHERFUCKER???
            npc.velocity.X *= 0.8f;

            // Be a bit more lenient with wall creation rates if the free head is present.
            if (phaseInfo.FreeHeadExists)
                wallCreateRate += 10;

            // Wait before creating walls.
            if (attackTimer < shootDelay)
                return;

            // Create rock pillars.
            if (attackTimer < shootDelay + wallCreateTime && attackTimer % wallCreateRate == wallCreateRate - 1f)
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    Vector2 pillarSpawnPosition = target.Center + Vector2.UnitX * (i * spaceBetweenWalls + 120f);
                    pillarSpawnPosition.Y -= 640f;
                    Utilities.NewProjectileBetter(pillarSpawnPosition, Vector2.Zero, ModContent.ProjectileType<SlammingRockPillar>(), wallDamage, 0f);
                }
            }

            if (attackTimer >= shootDelay + wallCreateTime + attackTransitionDelay)
                SelectNextAttack(npc, phaseInfo);
        }

        public static void DoBehavior_DetachedHeadCinderRain(NPC npc, Player target, RavagerPhaseInfo phaseInfo, ref float attackTimer)
        {
            // The head itself does the attack.
            // The body does pretty much nothing lmao
            int wallCreateRate = 60;
            int wallDamage = 200;
            float spaceBetweenWalls = MathHelper.Lerp(500f, 425f, 1f - phaseInfo.LifeRatio);
            ref float wallCreationCounter = ref npc.Infernum().ExtraAI[0];

            // Create rock pillars.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % wallCreateRate == wallCreateRate - 1f)
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    Vector2 pillarSpawnPosition = target.Center + Vector2.UnitY * i * spaceBetweenWalls;
                    pillarSpawnPosition.X -= 640f;
                    if (wallCreationCounter % 2f == 0f)
                    {
                        pillarSpawnPosition = target.Center + Vector2.UnitX * i * spaceBetweenWalls;
                        pillarSpawnPosition.Y -= 640f;
                    }

                    Utilities.NewProjectileBetter(pillarSpawnPosition, Vector2.Zero, ModContent.ProjectileType<SlammingRockPillar>(), wallDamage, 0f, -1, 0f, wallCreationCounter % 2f);
                }
                wallCreationCounter++;
                npc.netUpdate = true;
            }

            if (attackTimer >= 420f)
                SelectNextAttack(npc, phaseInfo);
        }

        public static void SelectNextAttack(NPC npc, RavagerPhaseInfo phaseInfo)
        {
            RavagerAttackType[] pattern = new RavagerAttackType[]
            {
                RavagerAttackType.DownwardFistSlam,
                RavagerAttackType.RegularJumps,
                RavagerAttackType.SingleBurstsOfBlood,
                RavagerAttackType.RegularJumps,
                RavagerAttackType.BarrageOfBlood
            };
            if (phaseInfo.InPhase2)
            {
                pattern = new RavagerAttackType[]
                {
                    RavagerAttackType.DownwardFistSlam,
                    phaseInfo.FreeHeadExists ? RavagerAttackType.DetachedHeadCinderRain : RavagerAttackType.SlamAndCreateMovingFlamePillars,
                    RavagerAttackType.RegularJumps,
                    RavagerAttackType.SingleBurstsOfBlood,
                    RavagerAttackType.DownwardFistSlam,
                    RavagerAttackType.WallSlams,
                    RavagerAttackType.SlamAndCreateMovingFlamePillars,
                    RavagerAttackType.RegularJumps,
                    RavagerAttackType.BarrageOfBlood,
                    RavagerAttackType.WallSlams,
                };
            }

            npc.ai[0] = (int)pattern[(int)++npc.Infernum().ExtraAI[9] % pattern.Length];
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
            npc.netSpam = 0;
        }

        public static void CreateGroundImpactEffects(NPC npc)
        {
            // Play a crash sound.
            SoundEngine.PlaySound(RavagerBody.JumpSound, npc.Bottom);

            // Create dust effects.
            for (int x = (int)npc.Left.X - 30; x < (int)npc.Right.X + 30; x += 10)
            {
                for (int i = 0; i < 6; i++)
                {
                    Dust stompDust = Dust.NewDustDirect(new Vector2(x, npc.Bottom.Y), npc.width + 30, 4, DustID.Smoke, 0f, 0f, 100, default, 1.5f);
                    stompDust.velocity *= 0.2f;
                }

                if (Main.netMode != NetmodeID.Server)
                {
                    Gore stompGore = Gore.NewGoreDirect(npc.GetSource_FromAI(), new Vector2(x, npc.Bottom.Y - 12f), default, Main.rand.Next(61, 64), 1f);
                    stompGore.velocity *= 0.4f;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int stomp = Utilities.NewProjectileBetter(npc.Bottom, Vector2.UnitY, ProjectileID.DD2OgreSmash, 0, 0f, -1, 0f, 1f);
                if (Main.projectile.IndexInRange(stomp))
                    Main.projectile[stomp].Size = new(npc.width + 120, 50);
            }

            // Create the particles.
            for (int i = 0; i < 15; i++)
            {
                float horizontalOffsetInterpolant = Main.rand.NextFloat();
                Vector2 sparkSpawnPosition = Vector2.Lerp(npc.BottomLeft, npc.BottomRight, MathHelper.Lerp(0.2f, 0.8f, horizontalOffsetInterpolant));
                Vector2 sparkVelocity = -Vector2.UnitY.RotatedBy(MathHelper.Lerp(-0.75f, 0.75f, horizontalOffsetInterpolant)) * Main.rand.NextFloat(7f, 16f);
                Color sparkColor = Color.Lerp(Color.Yellow, Color.Orange, Main.rand.NextFloat(0.2f, 0.8f));
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(sparkSpawnPosition, sparkVelocity, 2f, sparkColor, 40, 1f, 4f));
            }
        }

        public static void EnforceCustomGravity(NPC npc, float gravity)
        {
            float maxFallSpeed = 38f;
            if (npc.wet)
            {
                if (npc.honeyWet)
                {
                    gravity *= 0.33f;
                    maxFallSpeed *= 0.4f;
                }
                else if (npc.lavaWet)
                {
                    gravity *= 0.66f;
                    maxFallSpeed *= 0.7f;
                }
            }

            npc.velocity.Y += gravity;
            if (npc.velocity.Y > maxFallSpeed)
                npc.velocity.Y = maxFallSpeed;
        }
        #endregion AI

        #region Drawing

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            float widthFunction(float completionRatio) => MathHelper.SmoothStep(160f, 10f, completionRatio);
            Color colorFunction(float completionRatio)
            {
                Color darkFlameColor = new(58, 107, 252);
                Color lightFlameColor = new(45, 207, 239);
                float colorShiftInterpolant = (float)Math.Sin(-Main.GlobalTimeWrappedHourly * 6.7f + completionRatio * MathHelper.TwoPi) * 0.5f + 0.5f;
                Color color = Color.Lerp(darkFlameColor, lightFlameColor, (float)Math.Pow(colorShiftInterpolant, 1.64f));
                return color * npc.Opacity;
            }

            float horizontalArenaCenterX = npc.Infernum().ExtraAI[8];
            Texture2D borderTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Ravager/RockBorder").Value;

            // Draw flame jets when hovering.
            npc.Infernum().OptionalPrimitiveDrawer ??= new PrimitiveTrailCopy(widthFunction, colorFunction, null, true, InfernumEffectsRegistry.DarkFlamePillarVertexShader);

            // Create a telegraph line upward that fades away away the pillar fades in.
            Vector2 start = npc.Bottom - Vector2.UnitY * 100f;
            Vector2 end = start + Vector2.UnitY * npc.localAI[1] * 420f;
            var oldBlendState = Main.instance.GraphicsDevice.BlendState;
            Main.instance.GraphicsDevice.BlendState = BlendState.Additive;
            InfernumEffectsRegistry.DarkFlamePillarVertexShader.UseSaturation(1.4f);
            InfernumEffectsRegistry.DarkFlamePillarVertexShader.SetShaderTexture(InfernumTextureRegistry.StreakFaded);
            Main.instance.GraphicsDevice.Textures[2] = InfernumTextureRegistry.StreakFaded.Value;

            List<Vector2> points = new();
            for (int i = 0; i <= 8; i++)
                points.Add(Vector2.Lerp(start, end, i / 8f));

            if (npc.localAI[1] >= 0.01f)
                npc.Infernum().OptionalPrimitiveDrawer.Draw(points, -Main.screenPosition, 166);
            Main.instance.GraphicsDevice.BlendState = oldBlendState;

            // Draw obstructive pillars if an arena center is defined.
            if (horizontalArenaCenterX != 0f)
            {
                for (int i = -20; i < 20; i++)
                {
                    float verticalOffset = borderTexture.Height * i;

                    for (int direction = -1; direction <= 1; direction += 2)
                    {
                        Vector2 drawPosition = new(horizontalArenaCenterX - ArenaBorderOffset * direction, Main.LocalPlayer.Center.Y + verticalOffset);
                        drawPosition.Y -= drawPosition.Y % borderTexture.Height;
                        drawPosition -= Main.screenPosition;
                        Main.spriteBatch.Draw(borderTexture, drawPosition, null, Color.White, 0f, borderTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                    }
                }
            }
            return true;
        }

        #endregion Drawing

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Some of Ravager's attacks reward you for staying close. Try not to run away!";
        }
        #endregion Tips
    }
}
