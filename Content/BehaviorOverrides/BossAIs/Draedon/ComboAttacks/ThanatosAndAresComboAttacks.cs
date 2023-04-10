using CalamityMod;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares.AresBodyBehaviorOverride;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.DraedonBehaviorOverride;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ExoMechManagement;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Thanatos.ThanatosHeadBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ComboAttacks
{
    public static partial class ExoMechComboAttackContent
    {
        public static bool ArmCurrentlyBeingUsed(NPC npc)
        {
            // Return false Ares is not present.
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
                return false;

            // Return false if the arm is disabled.
            if (ArmIsDisabled(npc))
                return false;

            // Locate Ares' body as an NPC.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            if (AffectedAresArms.TryGetValue((ExoMechComboAttackType)aresBody.ai[0], out int[] activeArms))
                return activeArms.Contains(npc.type);
            return false;
        }

        public static bool UseThanatosAresComboAttack(NPC npc, ref float attackTimer, ref float frame)
        {
            NPC initialMech = FindInitialMech();
            if (initialMech is null)
                return false;

            int thanatosIndex = NPC.FindFirstNPC(ModContent.NPCType<ThanatosHead>());
            int aresIndex = NPC.FindFirstNPC(ModContent.NPCType<AresBody>());
            if (thanatosIndex >= 0 && initialMech.ai[0] >= 100f)
            {
                if (Main.npc[thanatosIndex].Infernum().ExtraAI[13] < 240f)
                {
                    npc.velocity *= 0.9f;
                    npc.rotation *= 0.9f;
                    return true;
                }
            }

            // Ensure that the player has a bit of time to compose themselves after killing the third mech.
            bool secondTwoAtOncePhase = (CurrentAresPhase == 3 || CurrentThanatosPhase == 3 || CurrentTwinsPhase == 3) && TotalMechs >= 2;
            if (initialMech.Infernum().ExtraAI[23] < 180f && attackTimer >= 3f && secondTwoAtOncePhase)
            {
                initialMech.Infernum().ExtraAI[23]++;
                attackTimer = 3f;
            }

            Player target = Main.player[initialMech.target];
            switch ((ExoMechComboAttackType)initialMech.ai[0])
            {
                case ExoMechComboAttackType.ThanatosAres_LaserCircle:
                    return DoBehavior_ThanatosAres_LaserCircle(npc, target, ref attackTimer, ref frame);
                case ExoMechComboAttackType.ThanatosAres_EnergySlashesAndCharges:
                    {
                        bool result = DoBehavior_ThanatosAres_EnergySlashesAndCharges(npc, target, ref attackTimer, ref frame);
                        if (result && aresIndex >= 0)
                        {
                            Main.npc[aresIndex].Infernum().ExtraAI[13] = 0f;
                            Main.npc[aresIndex].netUpdate = true;
                        }
                        return result;
                    }
            }
            return false;
        }

        public static bool DoBehavior_ThanatosAres_LaserCircle(NPC npc, Player target, ref float attackTimer, ref float frame)
        {
            int attackDelay = 108;
            int telegraphTime = 50;
            int attackTime = 780;
            int spinTime = attackTime - attackDelay;
            int totalLasers = 8;
            ref float generalAngularOffset = ref npc.Infernum().ExtraAI[0];

            if (CurrentThanatosPhase != 4 || CurrentAresPhase != 4)
                totalLasers += 3;

            // Thanatos spins around the target with its head always open while releasing lasers inward.
            if (npc.type == ModContent.NPCType<ThanatosHead>() && CalamityGlobalNPC.draedonExoMechPrime != -1)
            {
                NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
                Vector2 spinDestination = aresBody.Center + (attackTimer * MathHelper.TwoPi / 150f).ToRotationVector2() * 2000f;

                npc.velocity = npc.SafeDirectionTo(spinDestination) * MathHelper.Min(npc.Distance(spinDestination), 34f);
                npc.Center = npc.Center.MoveTowards(spinDestination, target.velocity.Length() * 1.2f + 35f);
                if (npc.WithinRange(spinDestination, 40f))
                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                else
                    npc.rotation = npc.rotation.AngleTowards((attackTimer + 8f) * MathHelper.TwoPi / 150f + MathHelper.PiOver2, 0.25f);

                ref float totalSegmentsToFire = ref npc.Infernum().ExtraAI[0];
                ref float segmentFireTime = ref npc.Infernum().ExtraAI[1];
                ref float segmentFireCountdown = ref npc.Infernum().ExtraAI[2];

                // Select segment shoot attributes.
                int segmentShootDelay = 115;
                if (attackTimer > attackDelay && attackTimer % segmentShootDelay == segmentShootDelay - 1f)
                {
                    totalSegmentsToFire = 24f;
                    segmentFireTime = 92f;

                    segmentFireCountdown = segmentFireTime;
                    npc.netUpdate = true;
                }

                // Disable contact damage before the attack happens, to prevent cheap hits.
                if (attackTimer < attackDelay)
                    npc.damage = 0;

                if (segmentFireCountdown > 0f)
                    segmentFireCountdown--;

                // Decide frames.
                frame = (int)ThanatosFrameType.Open;
            }

            // Ares sits in place, creating five large exo overload laser bursts.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                if (attackTimer == 2f)
                {
                    // Clear away old projectiles.
                    int[] projectilesToDelete = new int[]
                    {
                        ModContent.ProjectileType<SmallPlasmaSpark>(),
                    };
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (projectilesToDelete.Contains(Main.projectile[i].type))
                            Main.projectile[i].active = false;
                    }
                }

                // Decide frames.
                frame = (int)AresBodyFrameType.Laugh;
                if (attackTimer >= attackDelay - 45f)
                {
                    frame = (int)AresBodyFrameType.Laugh;
                    if (attackTimer == attackDelay - 45f)
                        DoLaughEffect(npc, target);
                }

                // Create telegraphs.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == attackDelay - telegraphTime)
                {
                    generalAngularOffset = MathHelper.Pi / totalLasers;
                    for (int i = 0; i < totalLasers; i++)
                    {
                        Vector2 laserDirection = (MathHelper.TwoPi * i / totalLasers + generalAngularOffset).ToRotationVector2();

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(telegraph =>
                        {
                            telegraph.localAI[0] = telegraphTime;
                        });
                        Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<AresDeathBeamTelegraph>(), 0, 0f, -1, 0f, npc.whoAmI);
                    }
                    npc.netUpdate = true;
                }

                // Create laser bursts.
                if (attackTimer == attackDelay)
                {
                    SoundEngine.PlaySound(TeslaCannon.FireSound, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < totalLasers; i++)
                        {
                            Vector2 laserDirection = (MathHelper.TwoPi * i / totalLasers + generalAngularOffset).ToRotationVector2();

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(deathray =>
                            {
                                deathray.ModProjectile<AresSpinningDeathBeam>().LifetimeThing = spinTime;
                            });
                            Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<AresSpinningDeathBeam>(), PowerfulShotDamage, 0f, -1, 0f, npc.whoAmI);
                        }
                        generalAngularOffset = 0f;
                        npc.netUpdate = true;
                    }
                }

                if (attackTimer > attackDelay)
                {
                    float spinSpeed = Utils.GetLerpValue(attackDelay, attackDelay + 60f, attackTimer, true) * MathHelper.Pi / 205f;

                    target.Infernum_Camera().CurrentScreenShakePower = 1.5f;

                    // Make the lasers slower in multiplayer.
                    if (Main.netMode != NetmodeID.SinglePlayer)
                        spinSpeed *= 0.65f;

                    generalAngularOffset += spinSpeed;
                }

                // Slow down.
                if (!npc.WithinRange(target.Center, 1900f) || attackTimer < attackDelay - 75f)
                    ExoMechAIUtilities.DoSnapHoverMovement(npc, target.Center - Vector2.UnitY * 450f, 24f, 75f);
                else
                    npc.velocity *= 0.9f;
            }

            return attackTimer > attackDelay + attackTime;
        }

        public static bool DoBehavior_ThanatosAres_EnergySlashesAndCharges(NPC npc, Player target, ref float attackTimer, ref float frame)
        {
            int attackDelay = 120;
            int slashAnticipationTime = 80;
            int slashTime = 31;
            int slashCount = 3;

            int redirectTime = 22;
            int chargeTime = 84;
            float flyAcceleration = 1.022f;
            if (CurrentThanatosPhase != 4 || CurrentAresPhase != 4)
            {
                slashAnticipationTime -= 54;
                slashTime -= 7;
                redirectTime -= 7;
                chargeTime -= 21;
                slashCount += 3;
                flyAcceleration += 0.008f;
            }

            // Thanatos attempts to slam into the target and accelerate.
            if (npc.type == ModContent.NPCType<ThanatosHead>())
            {
                float wrappedAttackTimer = (attackTimer - attackDelay) % (redirectTime + chargeTime);

                // Hover near the target before the attack begins.
                if (attackTimer < attackDelay)
                {
                    // Disable contact damage before the attack happens, to prevent cheap hits.
                    npc.damage = 0;

                    if (!npc.WithinRange(target.Center, 300f))
                        npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 12f, 0.06f);
                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    return false;
                }

                // Redirect and look at the target before charging.
                // Thanatos will zip towards the target during this if necessary, to ensure that he's nearby by the time the attack begins.
                if (wrappedAttackTimer <= redirectTime)
                {
                    float flySpeed = Utils.Remap(npc.Distance(target.Center), 750f, 2700f, 8f, 32f);
                    float aimInterpolant = Utils.Remap(wrappedAttackTimer, 0f, redirectTime - 4f, 0.01f, 0.5f);
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), MathHelper.Pi * aimInterpolant, true) * flySpeed;

                    if (!npc.WithinRange(target.Center, 1100f) && Vector2.Dot(npc.velocity, npc.SafeDirectionTo(target.Center)) < 0f)
                        npc.velocity *= -0.1f;
                }

                // Accelerate.
                else
                    npc.velocity *= flyAcceleration;

                // Decide the current rotation based on velocity.
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

                // Decide frames.
                frame = (int)ThanatosFrameType.Open;
            }

            // Ares hovers above the target and slashes downward, forcing the player into a tight position momentarily.
            float wrappedAresAttackTimer = (attackTimer - attackDelay) % (slashAnticipationTime + slashTime);
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 280f;
                if (target.velocity.Y < 0f)
                    hoverDestination.Y += target.velocity.Y * 20f;

                Vector2 idealVelocity = (hoverDestination - npc.Center) * 0.072f;
                if (wrappedAresAttackTimer >= slashAnticipationTime)
                    idealVelocity.X = 0f;

                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.1f);
            }

            if (npc.type == ModContent.NPCType<AresEnergyKatana>())
            {
                ref float slashTrailFadeOut = ref npc.ModNPC<AresEnergyKatana>().SlashTrailFadeOut;

                // Dangle about like normal if waiting for the attack to start.
                if (attackTimer < attackDelay || CalamityGlobalNPC.draedonExoMechPrime == -1)
                {
                    npc.ModNPC<AresEnergyKatana>().PerformDisabledHoverMovement();
                    return false;
                }

                NPC ares = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
                float flySpeedBoost = ares.velocity.Length() * 0.72f;
                float armOffsetDirection = npc.ModNPC<AresEnergyKatana>().ArmOffsetDirection;

                // Ensure that the katana is drawn.
                npc.ModNPC<AresEnergyKatana>().KatanaIsInUse = true;

                // Anticipate the slash.
                if (wrappedAresAttackTimer <= slashAnticipationTime)
                {
                    slashTrailFadeOut = MathHelper.Clamp(slashTrailFadeOut + 0.2f, 0f, 1f);
                    float minHoverSpeed = Utils.Remap(wrappedAresAttackTimer, 7f, slashAnticipationTime * 0.5f, 2f, 42f);
                    Vector2 startingOffset = new(armOffsetDirection * 470f, 0f);
                    Vector2 endingOffset = new(armOffsetDirection * 172f, -175f);
                    Vector2 hoverOffset = Vector2.Lerp(startingOffset, endingOffset, Utils.GetLerpValue(0f, slashAnticipationTime, wrappedAresAttackTimer, true));
                    ExoMechAIUtilities.DoSnapHoverMovement(npc, ares.Center + hoverOffset.SafeNormalize(Vector2.Zero) * 450f, flySpeedBoost + minHoverSpeed, 115f);
                }

                // Perform the slash.
                else
                {
                    slashTrailFadeOut = Utils.GetLerpValue(slashAnticipationTime + slashTime - 16f, slashAnticipationTime + slashTime - 11f, wrappedAresAttackTimer, true);
                    Vector2 startingOffset = new(armOffsetDirection * 172f, -175f);
                    Vector2 endingOffset = new(armOffsetDirection * 64f, 250f);
                    Vector2 hoverOffset = Vector2.Lerp(startingOffset, endingOffset, MathF.Pow(Utils.GetLerpValue(slashAnticipationTime, slashAnticipationTime + slashTime - 16f, wrappedAresAttackTimer, true), 0.4f));
                    ExoMechAIUtilities.DoSnapHoverMovement(npc, ares.Center + hoverOffset.SafeNormalize(Vector2.Zero) * 300f, flySpeedBoost + 49f, 115f);
                }

                // Prepare the slash.
                if (wrappedAresAttackTimer == slashAnticipationTime)
                {
                    // Reset the position cache, so that the trail can be drawn with a fresh set of points.
                    npc.oldPos = new Vector2[npc.oldPos.Length];

                    // Calculate the starting position of the slash. This is used for determining the orientation of the trail.
                    npc.ModNPC<AresEnergyKatana>().SlashStart = npc.Center + ((float)npc.ModNPC<AresEnergyKatana>().Limbs[1].Rotation).ToRotationVector2() * npc.scale * 160f;
                    npc.netUpdate = true;

                    // Play a slice sound.
                    SoundEngine.PlaySound(InfernumSoundRegistry.AresSlashSound, npc.Center);
                }

                // Rotate based on the direction of the arm.
                npc.rotation = (float)npc.ModNPC<AresEnergyKatana>().Limbs[1].Rotation;
                npc.spriteDirection = (int)armOffsetDirection;
                if (armOffsetDirection == 1)
                    npc.rotation += MathHelper.Pi;
            }

            return attackTimer >= attackDelay + (slashAnticipationTime + slashTime) * slashCount;
        }
    }
}
