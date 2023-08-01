using CalamityMod.NPCs;
using CalamityMod.NPCs.SlimeGod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SlimeGod
{
    public enum SlimeGodFightState
    {
        SingleLargeSlime,
        BothLargeSlimes,
        AloneSingleLargeSlimeEnraged,
        CorePhase
    }

    public enum BigSlimeGodAttackType
    {
        LongJumps,
        GroundedGelSlam,
        CoreSpinBursts
    }

    public enum SlimeGodComboAttackType
    {
        MutualStomps = 100,
        TeleportAndFireBlobs,
        SplitFormCharges
    }

    public static class SlimeGodComboAttackManager
    {
        public static int GroundSlimeDamage => 95;

        public static int SlimeGlobDamage => 95;

        public static int FirstSlimeToSummonIndex => WorldGen.crimson ? CalamityGlobalNPC.slimeGodRed : CalamityGlobalNPC.slimeGodPurple;

        public static int SecondSlimeToSummonIndex => WorldGen.crimson ? CalamityGlobalNPC.slimeGodPurple : CalamityGlobalNPC.slimeGodRed;

        public static int DelayBeforeSoloEnrageAttacksBegin => 90;

        public static NPC LeaderOfFight
        {
            get
            {
                if (CalamityGlobalNPC.slimeGodPurple != -1)
                    return Main.npc[CalamityGlobalNPC.slimeGodPurple];
                return Main.npc[CalamityGlobalNPC.slimeGodRed];
            }
        }

        public static SlimeGodFightState FightState
        {
            get
            {
                NPC firstSlime = FirstSlimeToSummonIndex >= 0 ? Main.npc[FirstSlimeToSummonIndex] : null;
                NPC secondSlime = SecondSlimeToSummonIndex >= 0 ? Main.npc[SecondSlimeToSummonIndex] : null;

                bool eitherSlimeIsAlive = firstSlime != null || secondSlime != null;
                if (eitherSlimeIsAlive && !(firstSlime != null && secondSlime != null))
                {
                    NPC remainingSlime = firstSlime is null ? secondSlime : firstSlime;
                    if (remainingSlime.Infernum().ExtraAI[5] == 1f)
                        return SlimeGodFightState.AloneSingleLargeSlimeEnraged;
                }

                if (secondSlime != null && firstSlime != null)
                    return SlimeGodFightState.BothLargeSlimes;
                if (secondSlime is null && firstSlime != null)
                    return SlimeGodFightState.SingleLargeSlime;

                return SlimeGodFightState.CorePhase;
            }
        }

        public const float SummonSecondSlimeLifeRatio = 0.6f;

        public const float BigSlimeBaseScale = 1.5f;

        public const float CoreBaseScale = 1.3f;

        public static void InheritAttributesFromLeader(NPC npc)
        {
            bool needsToPickNewAttack = false;

            // Pick a new attack if a combo attack is not in use but both bosses are present.
            if (npc.ai[0] < 100f && npc == LeaderOfFight && FightState == SlimeGodFightState.BothLargeSlimes)
                needsToPickNewAttack = true;

            // Pick a new attack if alone, exiting combo attack states.
            if (npc.ai[0] >= 100f && FightState != SlimeGodFightState.BothLargeSlimes)
            {
                npc.Opacity = 1f;
                npc.scale = BigSlimeBaseScale;

                int splitSlimeID = ModContent.NPCType<SplitBigSlime>();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == splitSlimeID)
                        Main.npc[i].active = false;
                }

                needsToPickNewAttack = true;
            }

            if (needsToPickNewAttack)
                BigSlimeGodAttacks.SelectNextAttack(npc);

            bool alone = FightState == SlimeGodFightState.AloneSingleLargeSlimeEnraged;
            if (npc == LeaderOfFight || alone)
                return;

            // Inherit the attack state and timer. Also sync if the leader decides to.
            npc.ai[0] = LeaderOfFight.ai[0];
            npc.ai[1] = LeaderOfFight.ai[1];
            npc.target = LeaderOfFight.target;
            if (LeaderOfFight.netUpdate)
                npc.netUpdate = true;
        }

        public static void DoAttacks(NPC npc, Player target, ref float attackTimer)
        {
            bool red = npc.type == ModContent.NPCType<CrimulanPaladin>();
            bool alone = FightState == SlimeGodFightState.AloneSingleLargeSlimeEnraged;

            // Wait a bit before attacking in the alone and enraged phase.
            if (alone && npc.ai[2] >= 1f)
            {
                npc.defense = 999999;
                npc.ai[0] = (int)BigSlimeGodAttackType.LongJumps;
                npc.ai[2]--;
                attackTimer = 0f;
                return;
            }

            switch ((int)npc.ai[0])
            {
                case (int)BigSlimeGodAttackType.LongJumps:
                    BigSlimeGodAttacks.DoBehavior_LongJumps(npc, target, red, alone, ref attackTimer);
                    break;
                case (int)BigSlimeGodAttackType.GroundedGelSlam:
                    BigSlimeGodAttacks.DoBehavior_GroundedGelSlam(npc, target, red, alone, ref attackTimer);
                    break;

                // This community does not tolerate winks of respect.
                case (int)BigSlimeGodAttackType.CoreSpinBursts:
                    BigSlimeGodAttacks.DoBehavior_CoreSpinBursts(npc, target, ref attackTimer);
                    break;
            }
            DoComboAttacks(npc, target, ref attackTimer);

            if (LeaderOfFight.whoAmI == npc.whoAmI)
                attackTimer++;
        }

        public static void DoComboAttacks(NPC npc, Player target, ref float attackTimer)
        {
            if (FightState != SlimeGodFightState.BothLargeSlimes)
                return;

            bool red = npc.type == ModContent.NPCType<CrimulanPaladin>();
            switch ((int)npc.ai[0])
            {
                case (int)SlimeGodComboAttackType.MutualStomps:
                    DoBehavior_MutualStomps(npc, target, red, ref attackTimer);
                    break;
                case (int)SlimeGodComboAttackType.TeleportAndFireBlobs:
                    DoBehavior_TeleportAndFireBlobs(npc, target, red, ref attackTimer);
                    break;
                case (int)SlimeGodComboAttackType.SplitFormCharges:
                    DoBehavior_SplitFormCharges(npc, target, red, ref attackTimer);
                    break;
            }
        }

        public static void DoBehavior_MutualStomps(NPC npc, Player target, bool red, ref float attackTimer)
        {
            int maxHoverTime = 210;
            int maxSlamTime = 150;
            int sitTime = 42;
            int globCount = 12;
            float globSpeed = 5.4f;
            ref float hasSlammed = ref npc.Infernum().ExtraAI[0];
            ref float chargeOffsetDirection = ref npc.Infernum().ExtraAI[1];
            ref float slamCounter = ref npc.Infernum().ExtraAI[2];

            if (npc.type != LeaderOfFight.whoAmI)
                chargeOffsetDirection = LeaderOfFight.Infernum().ExtraAI[1];

            NPC crimulanSlime = Main.npc[CalamityGlobalNPC.slimeGodRed];
            NPC ebonianSlime = Main.npc[CalamityGlobalNPC.slimeGodPurple];

            Vector2 getHoverDestination(NPC n)
            {
                float offsetDirection = (n.type == ModContent.NPCType<CrimulanPaladin>()).ToDirectionInt() * n.Infernum().ExtraAI[1];
                Vector2 destination = target.Center + new Vector2(offsetDirection * 540f, -440f);
                if (n.WithinRange(target.Center, 360f) && Distance(target.Center.Y, n.Center.Y) < 200f)
                    destination.X -= offsetDirection * 700f;
                return destination;
            }

            // Hover into position.
            if (attackTimer < maxHoverTime)
            {
                // Initialize the offset direction.
                if (chargeOffsetDirection == 0f)
                    chargeOffsetDirection = 1f;

                float hoverSpeed = Utils.Remap(attackTimer, 0f, maxHoverTime, 23.5f, 38.5f);
                Vector2 hoverDestination = getHoverDestination(npc);
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero.MoveTowards(hoverDestination - npc.Center, hoverSpeed), 0.2f);
                npc.noTileCollide = true;
                npc.damage = 0;

                // Make both slimes slam downward if they are sufficiently close to their hover destination.
                bool bothSlimesAreClose = crimulanSlime.WithinRange(getHoverDestination(crimulanSlime), 85f) && ebonianSlime.WithinRange(getHoverDestination(ebonianSlime), 85f);
                if (bothSlimesAreClose)
                {
                    attackTimer = maxHoverTime;
                    npc.netUpdate = true;
                }

                if (attackTimer >= maxHoverTime - 1f)
                    npc.velocity.Y *= 0.4f;

                return;
            }

            // Slam downward.
            if (attackTimer < maxHoverTime + maxSlamTime)
            {
                float gravity = Utils.Remap(attackTimer - maxHoverTime, 0f, 45f, 1.3f, 2.6f);
                npc.noGravity = true;
                npc.velocity.X *= 0.8f;
                npc.noTileCollide = npc.Bottom.Y < target.Bottom.Y;
                npc.velocity.Y = Clamp(npc.velocity.Y + gravity, -12f, 21f);
                if (Collision.SolidCollision(npc.TopLeft, npc.width, npc.height + 4, true) && !npc.noTileCollide)
                {
                    bool bothSlimesHasSlammed = crimulanSlime.Infernum().ExtraAI[0] == 1f && ebonianSlime.Infernum().ExtraAI[0] == 1f;

                    if (hasSlammed == 0f)
                    {
                        hasSlammed = 1f;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int globID = red ? ModContent.ProjectileType<DeceleratingCrimulanGlob>() : ModContent.ProjectileType<DeceleratingEbonianGlob>();
                            float shootOffsetAngle = Main.rand.NextBool().ToInt() * Pi / globCount;
                            for (int i = 0; i < globCount; i++)
                            {
                                Vector2 globVelocity = (TwoPi * i / globCount + shootOffsetAngle).ToRotationVector2() * globSpeed;
                                Utilities.NewProjectileBetter(npc.Bottom, globVelocity, globID, SlimeGlobDamage, 0f);
                            }

                            // Shoot one glob directly at the target to prevent sitting in place.
                            Utilities.NewProjectileBetter(npc.Bottom, npc.SafeDirectionTo(target.Center) * globSpeed * 0.8f, globID, SlimeGlobDamage, 0f);
                        }

                        SoundEngine.PlaySound(SoundID.Item167, npc.Bottom);
                        npc.netUpdate = true;
                    }

                    // Do collision effects when both slimes have slammed.
                    if (bothSlimesHasSlammed)
                    {
                        chargeOffsetDirection *= -1f;
                        attackTimer = maxHoverTime + maxSlamTime;
                        npc.velocity.Y = 0f;
                        npc.netUpdate = true;
                    }
                }
            }
            else
                hasSlammed = 1f;

            if (attackTimer >= maxHoverTime + maxSlamTime + sitTime)
            {
                attackTimer = 0f;
                hasSlammed = 0f;
                npc.netUpdate = true;

                if (npc.whoAmI == LeaderOfFight.whoAmI)
                {
                    slamCounter++;
                    if (slamCounter >= 3f)
                        BigSlimeGodAttacks.SelectNextAttack(npc);
                }
            }
        }

        public static void DoBehavior_TeleportAndFireBlobs(NPC npc, Player target, bool red, ref float attackTimer)
        {
            int teleportTime = 78;
            int blobShootRate = 60;
            int groundBlobCountPerShot = 2;
            int acceleratingGlobPerShot = 3;
            int blobShootTime = blobShootRate * 3 - 8;
            float globSpeed = 6f;

            // Disable contact damage to prevent telefrags.
            npc.damage = 0;

            // Do teleport animation effects.
            if (attackTimer < teleportTime)
            {
                npc.scale = Utils.GetLerpValue(teleportTime / 2 - 5f, 0f, attackTimer, true);
                if (attackTimer >= teleportTime / 2)
                    npc.scale += Utils.GetLerpValue(teleportTime / 2, teleportTime - 10f, attackTimer, true);
                npc.scale *= BigSlimeBaseScale;

                if (npc.scale <= 0f)
                    npc.scale = 0.0001f;
                npc.dontTakeDamage = true;

                // Fuck.
                npc.TopLeft += npc.Size * 0.5f;
                npc.Size = new Vector2(150f, 92f) * npc.scale;
                npc.TopLeft -= npc.Size * 0.5f;
                npc.velocity.X *= 0.8f;
                while (Collision.SolidCollision(npc.TopLeft, npc.width, npc.height, true))
                    npc.position.Y--;

                // Find a place to teleport to.
                if (attackTimer == teleportTime / 2)
                {
                    for (int i = 0; i < 8000; i++)
                    {
                        int dx = Main.rand.Next(25, i / 25 + 40) * red.ToDirectionInt();
                        int dy = Main.rand.Next(-50, 50);
                        Vector2 teleportBottom = target.Center + new Vector2(dx, dy).ToWorldCoordinates(8f, 0f);

                        // Ignore positions that are midair.
                        if (!Collision.SolidCollision(teleportBottom, 150, 32, true))
                            continue;

                        // Ignore positions that are in the ground.
                        if (Collision.SolidCollision(teleportBottom - Vector2.UnitY * 92, 150, 92 - 16))
                            continue;

                        // Ignore positions that have no opening to the target.
                        if (!Collision.CanHit(target.TopLeft, target.width, target.height, teleportBottom, 1, 1))
                            continue;

                        npc.Bottom = teleportBottom;
                        npc.netUpdate = true;
                        break;
                    }
                }

                // Release slime dust to accompany the teleport.
                Color slimeColor = red ? Color.Crimson : Color.Purple;
                slimeColor.A = 135;
                for (int i = 0; i < 12; i++)
                {
                    Dust slime = Dust.NewDustDirect(npc.position + Vector2.UnitX * -20f, npc.width + 40, npc.height, DustID.TintableDust, npc.velocity.X, npc.velocity.Y, npc.alpha, slimeColor, 2f);
                    slime.noGravity = true;
                    slime.velocity *= 0.5f;
                }
                return;
            }

            // Shoot blobs at the target and in the air.
            if (attackTimer % blobShootRate == blobShootRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item171, npc.Bottom);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 shootPosition = npc.Center - Vector2.UnitY * 24f;
                    for (int i = 0; i < groundBlobCountPerShot; i++)
                    {
                        Vector2 shootDestination = target.Center + Vector2.UnitX * Lerp(-200f, 200f, i / (float)(groundBlobCountPerShot - 1f));
                        shootDestination.X += Main.rand.NextFloatDirection() * 30f;

                        // The ideal velocity for falling can be calculated based on the horizontal range formula in the following way:
                        // First, the initial formula: R = v^2 * sin(2t) / g
                        // By assuming the angle that will yield the most distance is used, we can omit the sine entirely, since its maximum value is 1, leaving the following:
                        // R = v^2 / g
                        // We wish to find v, so rewritten, we arrive at:
                        // R * g = v^2
                        // v = sqrt(R * g), as the solution.
                        // However, to prevent weird looking angles, a clamp is performed to ensure the result stays within natural bounds.
                        float horizontalDistance = Vector2.Distance(shootPosition, shootDestination);
                        float idealShootSpeed = Sqrt(horizontalDistance * GroundSlimeGlob.Gravity);
                        float slimeShootSpeed = Clamp(idealShootSpeed, 7.6f, 20f);
                        Vector2 slimeShootVelocity = Utilities.GetProjectilePhysicsFiringVelocity(shootPosition, shootDestination, GroundSlimeGlob.Gravity, slimeShootSpeed, out _);
                        Utilities.NewProjectileBetter(shootPosition, slimeShootVelocity, ModContent.ProjectileType<GroundSlimeGlob>(), GroundSlimeDamage, 0f, -1, 0f, target.Center.Y);
                    }

                    // Shoot accelerating blobs if far away enough to the target.
                    if (!npc.WithinRange(target.Center, 336f))
                    {
                        int globID = red ? ModContent.ProjectileType<DeceleratingCrimulanGlob>() : ModContent.ProjectileType<DeceleratingEbonianGlob>();
                        for (int i = 0; i < acceleratingGlobPerShot; i++)
                        {
                            float shootOffsetAngle = Lerp(-0.62f, 0.62f, i / (float)(acceleratingGlobPerShot - 1f));
                            Vector2 globVelocity = Vector2.UnitX.RotatedBy(shootOffsetAngle) * globSpeed;
                            if (target.Center.X < npc.Center.X)
                                globVelocity *= -1f;

                            Utilities.NewProjectileBetter(npc.Bottom, globVelocity, globID, SlimeGlobDamage, 0f);
                        }

                        // Shoot one glob directly at the target to prevent sitting in place.
                        Utilities.NewProjectileBetter(npc.Bottom, npc.SafeDirectionTo(target.Center) * globSpeed * 0.8f, globID, SlimeGlobDamage, 0f);
                    }
                }
            }

            if (attackTimer >= teleportTime + blobShootTime)
                BigSlimeGodAttacks.SelectNextAttack(npc);
        }

        public static void DoBehavior_SplitFormCharges(NPC npc, Player target, bool red, ref float attackTimer)
        {
            int swarmTime = 420;
            int reformTime = 120;
            int acceleratingGlobPerShot = 4;
            float chargeSpeed = 16.5f;
            float globSpeed = 7f;
            ref float splitState = ref npc.Infernum().ExtraAI[1];

            // Determine tile collision stuff and disable contact damage.
            npc.noTileCollide = true;
            npc.noGravity = true;
            npc.damage = 0;

            // Do the split.
            if (attackTimer == 1f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int totalSlimesToSpawn = (int)Lerp(7f, 12f, 1f - npc.life / (float)npc.lifeMax);
                    int lifePerSlime = (int)Math.Ceiling(npc.life / (float)totalSlimesToSpawn);

                    for (int i = 0; i < totalSlimesToSpawn; i++)
                    {
                        int slime = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SplitBigSlime>(), npc.whoAmI, 0f, npc.whoAmI);
                        if (Main.npc.IndexInRange(slime))
                        {
                            Main.npc[slime].velocity = Main.rand.NextVector2CircularEdge(6f, 6f);
                            Main.npc[slime].Center += Main.rand.NextVector2Circular(15f, 15f);
                            Main.npc[slime].lifeMax = Main.npc[slime].life = lifePerSlime;
                            Main.npc[slime].netUpdate = true;
                        }
                    }
                }

                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/SlimeGodPossession"), npc.Center);
                for (int k = 0; k < 50; k++)
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.TintableDust, Main.rand.NextFloatDirection() * 3f, -1f, 0, default, 1f);
            }

            // Circle around the target, sometimes taking time to dash inward at them.
            // Both slimes have different yet dependent timers for this cycle.
            if (attackTimer < swarmTime)
            {
                float effectiveTimer = attackTimer;
                if (red)
                    effectiveTimer += 42f;
                float chargeWrappedAttackTimer = effectiveTimer % 150f;

                Vector2 flyDestination = target.Center + (TwoPi * effectiveTimer / 150f).ToRotationVector2() * 465f;
                if (chargeWrappedAttackTimer > 90f)
                {
                    // Slow down.
                    if (chargeWrappedAttackTimer < 105f)
                        npc.velocity *= 0.94f;
                    else
                        npc.velocity *= 1.01f;

                    // Charge.
                    if (chargeWrappedAttackTimer == 105f)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.netUpdate = true;

                        if (Main.netMode != NetmodeID.MultiplayerClient && !npc.WithinRange(target.Center, 200f))
                        {
                            int globID = red ? ModContent.ProjectileType<DeceleratingCrimulanGlob>() : ModContent.ProjectileType<DeceleratingEbonianGlob>();
                            for (int i = 0; i < acceleratingGlobPerShot; i++)
                            {
                                float shootOffsetAngle = Lerp(-0.32f, 0.32f, i / (float)(acceleratingGlobPerShot - 1f));
                                Vector2 globVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(shootOffsetAngle) * globSpeed;
                                Utilities.NewProjectileBetter(npc.Bottom, globVelocity, globID, SlimeGlobDamage, 0f);
                            }
                        }
                    }
                }

                // Spin around.
                else if (!npc.WithinRange(flyDestination, 110f))
                    npc.velocity = (npc.velocity * 15f + npc.SafeDirectionTo(flyDestination) * 15f) / 16f;
            }
            else
            {
                npc.velocity.X *= 0.925f;
                npc.noGravity = false;

                splitState = 1f;
            }

            // Handle opacity and clear away split slimes once close to attack termination.
            if (attackTimer > swarmTime + reformTime - 40)
            {
                splitState = 2f;
                npc.Opacity = 1f;
                npc.scale = Clamp(npc.scale + 0.075f, 0f, BigSlimeBaseScale);
            }
            else
                npc.Opacity = 0f;

            if (attackTimer > swarmTime + reformTime)
                BigSlimeGodAttacks.SelectNextAttack(npc);
        }

        public static void SelectNextAttackSpecific(NPC npc)
        {
            if (FightState != SlimeGodFightState.BothLargeSlimes || npc != LeaderOfFight)
                return;

            npc.Infernum().ExtraAI[6]++;
            switch ((int)(npc.Infernum().ExtraAI[6] % 3))
            {
                case 0:
                    npc.ai[0] = (int)SlimeGodComboAttackType.MutualStomps;
                    break;
                case 1:
                    npc.ai[0] = (int)SlimeGodComboAttackType.TeleportAndFireBlobs;
                    break;
                case 2:
                    npc.ai[0] = (int)SlimeGodComboAttackType.SplitFormCharges;
                    break;
            }
        }
    }
}
