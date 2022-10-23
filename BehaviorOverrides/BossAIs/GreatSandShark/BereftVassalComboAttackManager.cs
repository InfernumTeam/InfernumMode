using CalamityMod;
using CalamityMod.Projectiles.Summon;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using GreatSandSharkNPC = CalamityMod.NPCs.GreatSandShark.GreatSandShark;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public enum BereftVassalFightState
    {
        StartOfFightBereftVassal,
        BereftVassalAndGSS,
        EnragedBereftVassal
    }
    
    public enum BereftVassalComboAttackType
    {
        ParabolicLeaps = 100,
        HorizontalChargesAndLightningSpears
    }

    public static class BereftVassalComboAttackManager
    {
        public static BereftVassalFightState FightState
        {
            get
            {
                int gssIndex = NPC.FindFirstNPC(ModContent.NPCType<GreatSandSharkNPC>());
                NPC gss = gssIndex >= 0 ? Main.npc[gssIndex] : null;

                if (gss is null && Vassal.ModNPC<BereftVassal>().HasBegunSummoningGSS)
                    return BereftVassalFightState.EnragedBereftVassal;

                if (gss is not null)
                    return BereftVassalFightState.BereftVassalAndGSS;

                return BereftVassalFightState.StartOfFightBereftVassal;
            }
        }

        public static NPC Vassal => Main.npc[NPC.FindFirstNPC(ModContent.NPCType<BereftVassal>())];

        public static NPC GreatSandShark => Main.npc[NPC.FindFirstNPC(ModContent.NPCType<GreatSandSharkNPC>())];

        public static void InheritAttributesFromLeader(NPC npc)
        {
            // Inherit the attack state and timer. Also sync if the leader decides to.
            npc.ai[0] = Vassal.ai[0];
            npc.ai[1] = Vassal.ai[1];
            npc.target = Vassal.target;
            if (Vassal.netUpdate)
                npc.netUpdate = true;
        }

        public static void DoComboAttacksIfNecessary(NPC npc, Player target, ref float attackTimer)
        {
            if (FightState != BereftVassalFightState.BereftVassalAndGSS)
                return;

            // Ensure that a combo attack is always used.
            if (npc.ai[0] < 100f)
                SelectNextComboAttack(npc);
            
            switch ((int)npc.ai[0])
            {
                case (int)BereftVassalComboAttackType.ParabolicLeaps:
                    DoBehavior_ParabolicLeaps(npc, target, ref attackTimer);
                    break;
                case (int)BereftVassalComboAttackType.HorizontalChargesAndLightningSpears:
                    DoBehavior_HorizontalChargesAndLightningSpears(npc, target, ref attackTimer);
                    break;
            }
        }

        public static void DoBehavior_ParabolicLeaps(NPC npc, Player target, ref float attackTimer)
        {
            int leapTime = 96;
            int sandVomitCount = 8;
            int leapCount = 3;
            int waterSpearReleaseRate = 5;
            float hoverVerticalOffset = 336f;
            float hoverRedirectSpeed = 22f;
            float hoverRedirectAcceleration = 0.8f;
            float leapHorizontalSpeed = 23f;
            float leapVerticalSpeed = 30f;
            float leapGravity = 0.65f;
            ref float hasLeapedYet = ref Vassal.Infernum().ExtraAI[0];
            ref float leapCounter = ref Vassal.Infernum().ExtraAI[1];
            ref float spearRotation = ref Vassal.ModNPC<BereftVassal>().SpearRotation;
            ref float spearOpacity = ref Vassal.ModNPC<BereftVassal>().SpearOpacity;

            // The bereft vassal rides atop the great sand shark at all times, releasing water spears into the air.
            if (npc.type == ModContent.NPCType<BereftVassal>())
            {
                // Ride the great sand shark.
                RideGreatSandShark(npc);

                // Reset water spears into the air.
                if (hasLeapedYet == 1f && attackTimer % waterSpearReleaseRate == waterSpearReleaseRate - 1f && !Collision.SolidCollision(npc.TopLeft, npc.width, npc.height))
                {
                    SoundEngine.PlaySound(SoundID.AbigailAttack, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 shootPosition = npc.Center + (spearRotation - MathHelper.PiOver4).ToRotationVector2() * 12f;
                        Vector2 spearShootVelocity = new(npc.spriteDirection * -4f, -9f);

                        int spearIndex = Utilities.NewProjectileBetter(shootPosition, spearShootVelocity, ModContent.ProjectileType<WaterSpear>(), 160, 0f);
                        if (Main.projectile.IndexInRange(spearIndex))
                            Main.projectile[spearIndex].ModProjectile<WaterSpear>().StartingYPosition = target.Bottom.Y;
                    }                
                }

                // Adjust the spear direction and rotation.
                if (hasLeapedYet == 1f)
                {
                    spearOpacity = Utils.GetLerpValue(0f, 20f, attackTimer) * Utils.GetLerpValue(leapTime, leapTime - 12f, attackTimer, true);
                    spearRotation = GreatSandShark.velocity.ToRotation() + MathHelper.PiOver4;
                }

                // Transition to the next attack once enough time has passed and within blocks.
                if (hasLeapedYet == 1f && attackTimer >= leapTime && npc.Center.Y >= Main.maxTilesY * 16f - 1000f)
                {
                    GreatSandShark.velocity *= 0.3f;
                    leapCounter++;
                    hasLeapedYet = 0f;

                    if (leapCounter >= leapCount)
                        SelectNextComboAttack(npc);
                }

                return;
            }

            // The great sand shark hovers to the bottom left/right of the target in anticipation of a leap.
            if (hasLeapedYet == 0f)
            {
                attackTimer = 0f;

                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 600f, hoverVerticalOffset);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverRedirectSpeed, hoverRedirectAcceleration);
                npc.rotation = npc.velocity.X * 0.012f;
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                // Begin the leap and have the sand shark vomit sand once sufficiently close to the hover destination.
                if (npc.WithinRange(hoverDestination, 56f))
                {
                    // Release the sand vomit.
                    SoundEngine.PlaySound(InfernumSoundRegistry.GreatSandSharkSuddenRoarSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < sandVomitCount; i++)
                        {
                            Vector2 sandVelocity = new(npc.spriteDirection * -MathHelper.Lerp(4f, 11f, i / (float)(sandVomitCount - 1f)), -16f - i * 1.36f);
                            Vector2 mouthPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.UnitX * -npc.spriteDirection) * 108f;
                            sandVelocity += Main.rand.NextVector2Circular(1.4f, 1.4f);

                            int blobIndex = Utilities.NewProjectileBetter(mouthPosition, sandVelocity, ModContent.ProjectileType<SandBlob>(), 160, 0f);
                            if (Main.projectile.IndexInRange(blobIndex))
                                Main.projectile[blobIndex].ModProjectile<SandBlob>().StartingYPosition = target.Bottom.Y + 300f;
                        }
                    }

                    hasLeapedYet = 1f;
                    npc.velocity = new Vector2(npc.spriteDirection * -leapHorizontalSpeed, -leapVerticalSpeed);
                    npc.netUpdate = true;
                }
                return;
            }

            npc.velocity.X *= 0.99f;
            npc.rotation = npc.velocity.ToRotation();
            if (npc.spriteDirection == 1)
                npc.rotation += MathHelper.Pi;
            npc.velocity.Y += leapGravity;
        }

        public static void DoBehavior_HorizontalChargesAndLightningSpears(NPC npc, Player target, ref float attackTimer)
        {
            int chargeCount = 5;
            int chargeDelay = 23;
            int chargeTime = 26;
            int spearReleaseRate = 56;
            float chargeSpeed = 30f;
            float maxChargeSpeed = 50f;
            float hoverRedirectSpeed = 25f;
            float hoverRedirectAcceleration = 0.975f;
            float chargeHoverOffset = 540f;
            float spearShootSpeed = 13.5f;
            ref float hasReachedChargeDestination = ref Vassal.Infernum().ExtraAI[0];
            ref float chargeCounter = ref Vassal.Infernum().ExtraAI[1];
            ref float spearFadeBuffer = ref Vassal.Infernum().ExtraAI[2];
            ref float spearShootTimer = ref Vassal.Infernum().ExtraAI[3];
            ref float spearRotation = ref Vassal.ModNPC<BereftVassal>().SpearRotation;
            ref float spearOpacity = ref Vassal.ModNPC<BereftVassal>().SpearOpacity;

            // The bereft vassal rides atop the great sand shark at all times, shooting their spear at the ground.
            if (npc.type == ModContent.NPCType<BereftVassal>())
            {
                // Ride the great sand shark.
                RideGreatSandShark(npc);

                // Aim the spear at the target.
                spearRotation = npc.AngleTo(target.Center) + MathHelper.PiOver4;
                if (spearFadeBuffer > 0f)
                {
                    spearOpacity = 0f;
                    spearFadeBuffer--;
                }
                else
                    spearOpacity = MathHelper.Clamp(spearOpacity + 0.036f, 0f, 1f);

                // Release tridents. After enough time has passed, they will release lightning from the sky.
                spearShootTimer++;
                if (spearShootTimer >= spearReleaseRate && chargeCounter < chargeCount - 1f && !npc.WithinRange(target.Center, 336f) && !Collision.SolidCollision(npc.TopLeft, npc.width, npc.height))
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.VassalSlashSound, npc.Center);

                    // Make the spear disappear the frame after it's fired.
                    spearFadeBuffer = 19f;
                    spearOpacity = 0f;
                    spearShootTimer = 0f;
                    npc.netUpdate = true;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(target.Center) * spearShootSpeed, ModContent.ProjectileType<BereftVassalSpear>(), 175, 0f);
                }

                if (attackTimer >= chargeDelay + chargeTime)
                {
                    attackTimer = 0f;
                    chargeCounter++;
                    hasReachedChargeDestination = 0f;
                    if (chargeCounter >= chargeCount)
                    {
                        Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<BereftVassalSpear>());
                        SelectNextComboAttack(npc);
                    }
                    npc.netUpdate = true;
                }

                return;
            }

            // The ground sand shark charges horizontally at the target.
            // Hover into position.
            if (hasReachedChargeDestination == 0f)
            {
                // Hover into position and slow down once ready to charge.
                Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * chargeHoverOffset;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverRedirectSpeed, hoverRedirectAcceleration);
                npc.rotation = npc.velocity.X * 0.012f;
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                if (npc.WithinRange(hoverDestination, 60f))
                {
                    hasReachedChargeDestination = 1f;
                    npc.velocity *= 0.4f;
                    npc.netUpdate = true;
                }

                // Disable natural time progression and contact damage.
                Vassal.damage = 0;
                npc.damage = 0;
                attackTimer = 0f;
                return;
            }

            // Prepare for the charge.
            if (attackTimer < chargeDelay)
            {
                npc.velocity *= 0.92f;
                npc.rotation = npc.velocity.X * 0.012f;

                // Roar before charging.
                if (attackTimer == chargeDelay - 25)
                    SoundEngine.PlaySound(InfernumSoundRegistry.GreatSandSharkChargeRoarSound, npc.Center);
            }

            // Accelerate after the charge has happened.
            else if (npc.velocity.Length() < maxChargeSpeed)
                npc.velocity *= 1.03f;

            // Do the charge.
            if (attackTimer == chargeDelay)
            {
                SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);
                npc.velocity = Vector2.UnitX * (target.Center.X > npc.Center.X).ToDirectionInt() * chargeSpeed;
            }
        }

        public static void RideGreatSandShark(NPC npc)
        {
            Vector2 gssDirection = GreatSandShark.rotation.ToRotationVector2() * -GreatSandShark.spriteDirection;

            npc.spriteDirection = GreatSandShark.spriteDirection;
            npc.Center = GreatSandShark.Center + gssDirection * 76f + gssDirection.RotatedBy(MathHelper.PiOver2) * GreatSandShark.spriteDirection * 42f;
            npc.velocity = Vector2.Zero;
            npc.rotation = GreatSandShark.rotation;
            npc.noGravity = true;
            npc.noTileCollide = true;
        }

        public static void SelectNextComboAttack(NPC npc)
        {
            if (FightState != BereftVassalFightState.BereftVassalAndGSS || npc != Vassal)
                return;

            npc.Opacity = 1f;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.Infernum().ExtraAI[6]++;
            switch ((int)(npc.Infernum().ExtraAI[6] % 3))
            {
                case 0:
                    npc.ai[0] = (int)BereftVassalComboAttackType.ParabolicLeaps;
                    break;
                case 1:
                    npc.ai[0] = (int)BereftVassalComboAttackType.HorizontalChargesAndLightningSpears;
                    break;
                case 2:
                    npc.ai[0] = (int)BereftVassalComboAttackType.HorizontalChargesAndLightningSpears;
                    break;
            }
            npc.netUpdate = true;
        }
    }
}
