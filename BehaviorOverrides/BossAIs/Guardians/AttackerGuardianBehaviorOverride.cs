using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Guardians
{
    public class AttackerGuardianBehaviorOverride : NPCBehaviorOverride
    {
        internal enum Phase2GuardianAttackState
        {
            ReelBackSpin,
            FireCast,
            RayZap
        }

        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            CalamityGlobalNPC.doughnutBoss = npc.whoAmI;

            // Summoning donuts.
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[1] == 0f)
            {
                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ProfanedGuardianBoss3>());
                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ProfanedGuardianBoss2>());
                npc.localAI[1] = 1f;
            }

            int remainingGuardians = NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianBoss2>()).ToInt() + NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianBoss3>()).ToInt();
            npc.dontTakeDamage = npc.life < npc.lifeMax * 0.55f && remainingGuardians > 0;

            npc.TargetClosest();

            // Despawn if no valid target exists.
            Player target = Main.player[npc.target];
            if (!target.active || target.dead)
            {
                npc.TargetClosest();
                target = Main.player[npc.target];
                if (!target.active || target.dead)
                {
                    npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - 0.4f, -20f, 6f);
                    if (npc.timeLeft < 180)
                        npc.timeLeft = 180;
                    return false;
                }
            }

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float phase2TransitionTimer = ref npc.ai[3];

            if (remainingGuardians != 0)
            {
                DoCoordinatedMovement(npc, remainingGuardians, ref attackState, ref attackTimer);
                npc.alpha = npc.damage == 0 ? 180 : Utils.Clamp(npc.alpha - 32, 0, 255);
            }
            else
                DoPhase2Movement(npc, ref attackState, ref attackTimer, ref phase2TransitionTimer);

            return false;
        }

        internal static void DoCoordinatedMovement(NPC npc, int remainingGuardians, ref float attackState, ref float attackTimer)
        {
            Player target = Main.player[npc.target];

            npc.spriteDirection = (npc.velocity.X > 0).ToDirectionInt();
            npc.damage = npc.defDamage;

            if (attackState == 0f)
            {
                npc.damage = 0;

                int xOffsetDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                Vector2 destination = target.Center + Vector2.UnitX * 540f * xOffsetDirection;

                float distanceFromDestination = npc.Distance(destination);
                Vector2 linearVelocityToDestination = npc.SafeDirectionTo(destination) * MathHelper.Min(15f + target.velocity.Length() * 0.5f, distanceFromDestination);
                npc.velocity = Vector2.Lerp(linearVelocityToDestination, (destination - npc.Center) / 15f, Utils.InverseLerp(180f, 420f, distanceFromDestination, true));

                if (Main.netMode != NetmodeID.MultiplayerClient && npc.WithinRange(destination, 12f + target.velocity.Length() * 0.5f))
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * 9f) * 19f;

                    // Release a burst of spears outward.
                    int spearBurstCount = remainingGuardians == 1 ? 6 : 3;
                    float spearBurstSpread = remainingGuardians == 1 ? MathHelper.ToRadians(12f) : MathHelper.ToRadians(21f);
                    for (int i = 0; i < spearBurstCount; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-spearBurstSpread, spearBurstSpread, i / (float)spearBurstCount);
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center + target.velocity.Y * new Vector2(8f, 36f)).RotatedBy(offsetAngle) * 9f;
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<ProfanedSpear>(), 150, 0f);
                    }

                    attackState = 1f;
                    npc.netUpdate = true;
                }
            }

            // Charge.
            if (attackState == 1f)
            {
                attackTimer++;
                npc.velocity *= 1.015f;
                if (attackTimer >= 60f)
                {
                    attackState = 0f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }
        }

        internal static void DoPhase2Movement(NPC npc, ref float attackState, ref float attackTimer, ref float phase2TransitionTimer)
        {
            Player target = Main.player[npc.target];

            float lifeRatio = npc.life / (float)npc.lifeMax;
            float phase2TransitionTime = 180f;
            if (phase2TransitionTimer < phase2TransitionTime)
            {
                npc.velocity *= 0.9f;

                if (Main.netMode != NetmodeID.MultiplayerClient && phase2TransitionTimer % 45 == 0)
                {
                    float shootSpeed = BossRushEvent.BossRushActive ? 17f : 12f;
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / 11f) * shootSpeed;
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<ProfanedSpear>(), 170, 0f);
                    }
                }

                attackState = attackTimer = 0f;
                phase2TransitionTimer++;

                if (Main.netMode != NetmodeID.MultiplayerClient && phase2TransitionTimer == phase2TransitionTime - 45)
                {
                    for (int i = -1; i <= 1; i += 2)
                        NPC.NewNPC((int)npc.Center.X + i * 160, (int)npc.Center.Y, ModContent.NPCType<EtherealHand>(), 0, i);
                }

                return;
            }

            ref float shouldHandsBeInvisibleFlag = ref npc.localAI[2];
            shouldHandsBeInvisibleFlag = 0f;
            switch ((Phase2GuardianAttackState)(int)attackState)
            {
                case Phase2GuardianAttackState.ReelBackSpin:
                    ref float arcDirection = ref npc.ai[2];
                    shouldHandsBeInvisibleFlag = (attackTimer > 45f).ToInt();

                    // Fade out.
                    if (attackTimer <= 30f)
                        npc.velocity *= 0.96f;

                    // Reel back.
                    if (attackTimer == 30f)
                    {
                        npc.Center = target.Center + (MathHelper.PiOver2 * Main.rand.Next(4)).ToRotationVector2() * 600f;
                        npc.velocity = -npc.SafeDirectionTo(target.Center);
                        npc.rotation = npc.AngleTo(target.Center);
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        if (npc.spriteDirection == -1)
                            npc.rotation += MathHelper.Pi;

                        npc.netUpdate = true;
                    }

                    // Move back and re-appear.
                    if (attackTimer > 30f && attackTimer < 75f)
                    {
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(1f, 6f, Utils.InverseLerp(30f, 75, attackTimer, true));
                        npc.alpha = Utils.Clamp(npc.alpha - 15, 0, 255);
                    }

                    // Charge and fire a spear.
                    if (attackTimer == 75f)
                    {
                        arcDirection = (Math.Cos(npc.AngleTo(target.Center)) > 0).ToDirectionInt();
                        npc.velocity = npc.SafeDirectionTo(target.Center) * 24f;
                        if (BossRushEvent.BossRushActive)
                            npc.velocity *= 1.5f;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 spawnPosition = npc.Center - npc.velocity.SafeNormalize(Vector2.Zero) * 40f;
                            Utilities.NewProjectileBetter(spawnPosition, npc.velocity.SafeNormalize(Vector2.Zero) * 40f, ModContent.ProjectileType<ProfanedSpear>(), 210, 0f);
                            Utilities.NewProjectileBetter(spawnPosition, npc.velocity.SafeNormalize(Vector2.Zero) * 47f, ModContent.ProjectileType<ProfanedSpear>(), 200, 0f);
                        }

                        npc.netUpdate = true;
                    }

                    // Arc around a bit.
                    if (attackTimer >= 75f && attackTimer < 150f)
                    {
                        npc.velocity = npc.velocity.RotatedBy(arcDirection * MathHelper.TwoPi / 75f);

                        if (!npc.WithinRange(target.Center, 180f))
                            npc.Center += npc.SafeDirectionTo(target.Center) * (12f + target.velocity.Length() * 0.15f);

                        npc.rotation = npc.velocity.ToRotation();
                        if (npc.spriteDirection == -1)
                            npc.rotation += MathHelper.Pi;

                        int lightReleaseTime = lifeRatio < 0.45f ? 12 : 25;
                        // Release crystal lights when spinning.
                        if (attackTimer % lightReleaseTime == 0)
                        {
                            Main.PlaySound(SoundID.Item101, npc.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.PiOver2 * Main.rand.NextFloatDirection()) * 3f;
                                int shot = Utilities.NewProjectileBetter(npc.Center + shootVelocity * 3f, shootVelocity, ModContent.ProjectileType<CrystalShot>(), 160, 0f);
                                Main.projectile[shot].ai[0] = npc.target;
                                Main.projectile[shot].ai[1] = Main.rand.NextFloat();
                            }
                        }

                        if (Main.netMode != NetmodeID.MultiplayerClient && lifeRatio < 0.45f && attackTimer % 50 == 0)
                        {
                            for (int i = 0; i < 18; i++)
                            {
                                Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / 18f) * 19f;
                                Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<ProfanedSpear>(), 170, 0f);
                            }
                        }
                    }

                    // Slow down and fade out again.
                    if (attackTimer >= 150f)
                    {
                        npc.velocity *= 0.94f;
                        npc.alpha = Utils.Clamp(npc.alpha + 30, 0, 255);
                    }

                    // Prepare for the ritual attack.
                    if (attackTimer >= 180f)
                    {
                        attackTimer = 0f;
                        npc.Center = target.Center - Vector2.UnitY * 400f;

                        attackState = (int)Phase2GuardianAttackState.FireCast;
                        arcDirection = 0f;

                        npc.alpha = 0;
                        npc.rotation = 0f;
                        npc.velocity = Vector2.Zero;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;
                    }

                    break;
                case Phase2GuardianAttackState.FireCast:
                    if (attackTimer < 45f)
                    {
                        shouldHandsBeInvisibleFlag = 1f;
                        npc.velocity *= 0.935f;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 45f)
                        Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<FireCastRitual>(), 0, 0f);

                    if (attackTimer >= 100f && attackTimer <= 130f && Lighting.NotRetro)
                    {
                        float lightPower = Utils.InverseLerp(100f, 120f, attackTimer, true);
                        lightPower = MathHelper.Clamp(lightPower * 2f, 0f, 1f);
                        MoonlordDeathDrama.RequestLight(lightPower, target.Center);

                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    }

                    int totalShots = 13;
                    int shootRate = 8;
                    int lightSpawnChance = lifeRatio < 0.45f ? 3 : 5;
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > 130f && attackTimer <= 130f + totalShots * shootRate && attackTimer % shootRate == 0)
                    {
                        int shotType = Main.rand.NextBool(lightSpawnChance) ? ModContent.ProjectileType<CrystalShot>() : ProjectileID.CultistBossFireBall;
                        Vector2 spawnPosition = npc.Center;

                        float shootSpeed = BossRushEvent.BossRushActive ? 21f : 13f;
                        Vector2 shootVelocity = Main.rand.NextVector2CircularEdge(shootSpeed, shootSpeed);
                        shootVelocity = Vector2.Lerp(shootVelocity, npc.SafeDirectionTo(target.Center) * shootSpeed, Main.rand.NextFloat(0.6f, 0.7f));
                        shootVelocity = shootVelocity.SafeNormalize(Vector2.UnitY) * shootSpeed;

                        spawnPosition += shootVelocity * 5f;

                        // Make the ritual fade.
                        int ritualType = ModContent.ProjectileType<FireCastRitual>();
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            Projectile proj = Main.projectile[i];
                            if (!proj.active || proj.type != ritualType)
                                continue;
                            proj.timeLeft = 15;
                        }

                        int shot = Utilities.NewProjectileBetter(spawnPosition, shootVelocity, shotType, 170, 0f);
                        Main.projectile[shot].tileCollide = false;
                    }

                    if (attackTimer >= 130f + totalShots * shootRate + 20f)
                    {
                        attackTimer = 0f;
                        attackState = (int)Phase2GuardianAttackState.RayZap;

                        npc.alpha = 0;
                        npc.rotation = 0f;
                        npc.velocity = Vector2.Zero;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;
                    }
                    break;
                case Phase2GuardianAttackState.RayZap:
                    ref float xOffset = ref npc.ai[2];
                    if (xOffset == 0f)
                        xOffset = Math.Sign((npc.Center - target.Center).X);

                    Vector2 destination = target.Center + new Vector2(xOffset * 450f, -300f);
                    Vector2 flyVelocity = (destination - npc.Center - npc.velocity).SafeNormalize(Vector2.UnitY) * 17f;

                    if (npc.Distance(destination) > 12f)
                        npc.velocity = Vector2.Lerp(npc.velocity, flyVelocity, 0.14f);
                    else
                        npc.Center = destination;

                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 60 == 0)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / 6f) * 10f;
                            Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<ProfanedSpear>(), 180, 0f);
                        }
                    }

                    if (attackTimer >= 325f)
                    {
                        attackTimer = 0f;
                        attackState = (int)Phase2GuardianAttackState.ReelBackSpin;

                        npc.alpha = 0;
                        npc.rotation = 0f;
                        npc.velocity = Vector2.Zero;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;
                    }
                    break;
            }
            attackTimer++;
        }
    }
}