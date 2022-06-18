using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Ravager;
using CalamityMod.Particles;
using CalamityMod.World;
using InfernumMode.Dusts;
using InfernumMode.OverridingSystem;
using InfernumMode.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class RavagerBodyBehaviorOverride : NPCBehaviorOverride
    {
        public struct RavagerPhaseInfo
        {
            public bool HandsAreAlive;

            public bool LegsAreAlive;

            public bool HeadIsAttached;

            public bool InPhase2 => !HandsAreAlive && !LegsAreAlive && !HeadIsAttached;

            public RavagerPhaseInfo(bool hands, bool legs, bool head)
            {
                HandsAreAlive = hands;
                LegsAreAlive = legs;
                HeadIsAttached = head;
            }
        }

        public override int NPCOverrideType => ModContent.NPCType<RavagerBody>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public const int AttackDelay = 135;
        public const float BaseDR = 0.325f;

        #region Enumerations
        public enum RavagerAttackType
        {
            SingleBurstsOfBlood,
            RegularJumps,
            BarrageOfBlood,
            SingleBurstsOfUpwardDarkFlames,
            DownwardFistSlam,
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Ensure that the NPC always draws things, even when far away.
            // Not doing this will result in the arena not being drawn if far from the target.
            NPCID.Sets.MustAlwaysDraw[npc.type] = true;

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Create limbs.
            if (npc.localAI[0] == 0f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int leftLeg = NPC.NewNPC((int)npc.Center.X - 70, (int)npc.Center.Y + 88, ModContent.NPCType<RavagerLegLeft>(), npc.whoAmI);
                int rightLeg = NPC.NewNPC((int)npc.Center.X + 70, (int)npc.Center.Y + 88, ModContent.NPCType<RavagerLegRight>(), npc.whoAmI);

                int leftClaw = NPC.NewNPC((int)npc.Center.X - 120, (int)npc.Center.Y + 50, ModContent.NPCType<RavagerClawLeft>(), npc.whoAmI);
                int rightClaw = NPC.NewNPC((int)npc.Center.X + 120, (int)npc.Center.Y + 50, ModContent.NPCType<RavagerClawRight>(), npc.whoAmI);

                // Make claws and legs share their own distinct HP pools, instead of being separate.
                if (Main.npc.IndexInRange(leftLeg) && Main.npc.IndexInRange(rightLeg))
                    Main.npc[leftLeg].realLife = rightLeg;
                if (Main.npc.IndexInRange(leftClaw) && Main.npc.IndexInRange(rightClaw))
                    Main.npc[leftClaw].realLife = rightClaw;

                NPC.NewNPC((int)npc.Center.X + 1, (int)npc.Center.Y - 20, ModContent.NPCType<RavagerHead>(), npc.whoAmI);
                npc.localAI[0] = 1f;
            }

            CalamityGlobalNPC.scavenger = npc.whoAmI;

            // Fade in.
            npc.alpha = Utils.Clamp(npc.alpha - 10, 0, 255);

            // Reset things every frame.
            npc.Calamity().DR = BaseDR;
            npc.damage = npc.defDamage;

            npc.noTileCollide = false;
            if (!target.active || target.dead || !npc.WithinRange(target.Center, 7200f))
            {
                npc.noTileCollide = true;
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * -30f, 0.2f);
                if (!npc.WithinRange(target.Center, 1000f) || Main.rand.NextBool(45))
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
            RavagerPhaseInfo phaseInfo = new RavagerPhaseInfo(leftClawActive && rightClawActive, leftLegActive && rightLegActive, headActive);

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool shouldBeBuffed = CalamityWorld.downedProvidence && !BossRushEvent.BossRushActive;

            float gravity = 0.625f;
            if (phaseInfo.InPhase2)
                gravity += 0.25f;

            // Reset things.
            armsCanPunch = 0f;
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
                    DoBehavior_DownwardFistSlam(npc, target, phaseInfo, ref attackTimer, ref gravity, ref armsShouldSlamIntoGround);
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
            int telegraphTime = 96;
            int jumpCount = 3;
            int jumpDelay = 45;
            int emberBurstCount = 3;
            float jumpIntensityFactor = 1.27f;

            if (!phaseInfo.HandsAreAlive)
                jumpIntensityFactor *= 1.15f;
            if (!phaseInfo.LegsAreAlive)
                jumpIntensityFactor *= 1.15f;

            ref float jumpSubstate = ref npc.Infernum().ExtraAI[0];
            ref float jumpCounter = ref npc.Infernum().ExtraAI[1];
            ref float attackDelayTimer = ref npc.Infernum().ExtraAI[2];

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
                        npc.velocity.Y -= 7.2f;
                    if (!Collision.CanHit(npc.Center, 1, 1, target.Center, 1, 1))
                        npc.velocity.Y -= 3.84f;

                    // Jump far higher if the target is close, to allow them to have openings and encourage close combat.
                    if (MathHelper.Distance(npc.Center.X, target.Center.X) < 425f || MathHelper.Distance(npc.Center.Y, target.Center.Y) < 240f)
                        npc.velocity.Y -= 9.5f;

                    // Release fireballs at the target.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < emberBurstCount; i++)
                        {
                            float offsetAngle = MathHelper.Lerp(-0.51f, 0.51f, i / (float)(emberBurstCount - 1f));
                            Vector2 emberShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * 9f;
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

                // Make stomp sounds and particles when hitting the ground again.
                if (npc.velocity.Y == 0f)
                {
                    CreateGroundImpactEffects(npc);
                    attackTimer = 0f;
                    jumpSubstate = 0f;
                    jumpCounter++;
                    if (jumpCounter >= jumpCount)
                        SelectNextAttack(npc);

                    npc.netUpdate = true;
                }

                // Fall through tiles in the way.
                if (!target.dead)
                {
                    if ((target.position.Y > npc.Bottom.Y && npc.velocity.Y > 0f) || (target.position.Y < npc.Bottom.Y && npc.velocity.Y < 0f))
                        npc.noTileCollide = true;
                    else if ((npc.velocity.Y > 0f && npc.Bottom.Y > target.Top.Y) || (Collision.CanHit(npc.position, npc.width, npc.height, target.Center, 1, 1) && !Collision.SolidCollision(npc.position, npc.width, npc.height)))
                        npc.noTileCollide = false;
                }
            }
            else
                npc.velocity.X *= 0.8f;
        }

        public static void DoBehavior_BurstsOfBlood(NPC npc, Player target, RavagerPhaseInfo phaseInfo, bool multiplePerShot, ref float attackTimer)
        {
            int shootDelay = 84;
            int bloodShootRate = 10;
            int bloodShootTime = 180;
            int totalInstancesPerShot = 1;
            int postAttackTransitionDelay = 75;
            float destinationOffsetVariance = 200f;

            if (!phaseInfo.HandsAreAlive)
            {
                shootDelay -= 35;
                postAttackTransitionDelay -= 15;
            }
            
            if (!phaseInfo.LegsAreAlive || !phaseInfo.HandsAreAlive)
                destinationOffsetVariance *= 0.67f;

            if (multiplePerShot)
            {
                bloodShootRate *= 5;
                totalInstancesPerShot = 7;
                destinationOffsetVariance *= 2.5f;
            }
            
            // Sit in place and prevent sliding.
            npc.velocity.X *= 0.85f;

            if (attackTimer >= shootDelay + bloodShootTime + postAttackTransitionDelay)
                SelectNextAttack(npc);

            // Wait before shooting and at the end of the attack. The arms will attack during this period if they are present, however.
            if (attackTimer < shootDelay || attackTimer >= shootDelay + bloodShootTime)
                return;

            if (attackTimer % bloodShootRate == bloodShootRate - 1f)
            {
                Main.PlaySound(SoundID.Item45, npc.Center);

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

                        int blood = Utilities.NewProjectileBetter(shootPosition, bloodShootVelocity, ModContent.ProjectileType<UnholyBloodGlob>(), 185, 0f);
                        if (Main.projectile.IndexInRange(blood))
                            Main.projectile[blood].ai[1] = target.Center.Y;
                    }
                }
            }
        }

        public static void DoBehavior_DownwardFistSlam(NPC npc, Player target, RavagerPhaseInfo phaseInfo, ref float attackTimer, ref float gravity, ref float armsShouldSlamIntoGround)
        {
            int hoverTime = 95;
            int sitOnGroundTime = 72;

            if (phaseInfo.LegsAreAlive)
            {
                hoverTime = 50;
                sitOnGroundTime = 25;
            }

            int slamSlowdownTime = (int)(hoverTime * 0.32f);
            int projectileShootCount = 30;
            int totalCrystalsPerProj = 13;
            int slamCount = 3;
            float projectileAngularSpread = MathHelper.ToRadians(61f);
            float horizontalCrystalSpeed = 8.4f;
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
                    npc.noTileCollide = true;

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
            if (npc.velocity.Y == 0f && hasDoneGroundHitEffects == 0f)
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
                        int blood = Utilities.NewProjectileBetter(npc.Center - Vector2.UnitY * 40f, bloodShootVelocity, ModContent.ProjectileType<UnholyBloodGlob>(), 185, 0f);
                        if (Main.projectile.IndexInRange(blood))
                            Main.projectile[blood].ai[1] = target.Center.Y;
                    }

                    // Create crystals that move in both horizontal directions if no arms are present.
                    if (!phaseInfo.HandsAreAlive)
                    {
                        for (int i = -1; i <= 1; i += 2)
                        {
                            Vector2 crystalVelocity = Vector2.UnitX * horizontalCrystalSpeed * i;
                            int crystal = Utilities.NewProjectileBetter(npc.Bottom, crystalVelocity, ModContent.ProjectileType<GroundBloodCrystal>(), 200, 0f);
                            if (Main.projectile.IndexInRange(crystal))
                                Main.projectile[crystal].ai[0] = totalCrystalsPerProj;
                        }
                    }
                }

                hasDoneGroundHitEffects = 1f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            if (hasDoneGroundHitEffects == 1f && attackTimer >= sitOnGroundTime)
            {
                slamCounter++;
                if (slamCounter >= slamCount)
                    SelectNextAttack(npc);
                else
                {
                    hasDoneGroundHitEffects = 0f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            RavagerAttackType[] pattern = new RavagerAttackType[]
            {
                RavagerAttackType.DownwardFistSlam,
                RavagerAttackType.RegularJumps,
                RavagerAttackType.SingleBurstsOfBlood,
                RavagerAttackType.RegularJumps
            };
            npc.ai[0] = (int)pattern[(int)++npc.Infernum().ExtraAI[8] % pattern.Length];
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
            npc.netSpam = 0;
        }

        public static void CreateGroundImpactEffects(NPC npc)
        {
            // Play a crash sound.
            // TODO -- Perhaps try to find something stronger for this?
            Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 14, 1.25f, -0.25f);

            // Create dust effects.
            for (int x = (int)npc.Left.X - 30; x < (int)npc.Right.X + 30; x += 10)
            {
                for (int i = 0; i < 6; i++)
                {
                    Dust stompDust = Dust.NewDustDirect(new Vector2(x, npc.Bottom.Y), npc.width + 30, 4, 31, 0f, 0f, 100, default, 1.5f);
                    stompDust.velocity *= 0.2f;
                }

                Gore stompGore = Gore.NewGoreDirect(new Vector2(x, npc.Bottom.Y - 12f), default, Main.rand.Next(61, 64), 1f);
                stompGore.velocity *= 0.4f;
            }

            // Create the particles.
            GeneralParticleHandler.SpawnParticle(new GroundImpactParticle(npc.Bottom, Vector2.UnitY, Color.Lerp(Color.Yellow, Color.Orange, 0.45f), 32, 1.1f));
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
    }
}
