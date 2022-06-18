using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SupremeCataclysmBehaviorOverride : NPCBehaviorOverride
    {
        public enum SCalBrotherAttackType
        {
            AttackDelay,
            SinusoidalBobbing,
            ProjectileShooting,
            Hyperdashes
        }

        public enum SCalBrotherAnimationType
        {
            HoverInPlace,
            AttackAnimation
        }

        public override int NPCOverrideType => ModContent.NPCType<SupremeCataclysm>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        #region AI
        public override bool PreAI(NPC npc)
        {
            DoAI(npc);
            return false;
        }

        // In this AI, Cataclysm is considered the leader, managing things such as attack states, timers, and life.
        // Catastrophe, on the other hand, simply inherits these properties from Cataclysm. Together, the two share an HP pool,
        // so the concern of Catastrophe having to attack on his own is eliminated.
        public static void DoAI(NPC npc)
        {
            int cataclysmIndex = NPC.FindFirstNPC(ModContent.NPCType<SupremeCataclysm>());
            int catastropheIndex = NPC.FindFirstNPC(ModContent.NPCType<SupremeCatastrophe>());
            bool isCataclysm = npc.type == ModContent.NPCType<SupremeCataclysm>();
            bool isCatastrophe = npc.type == ModContent.NPCType<SupremeCatastrophe>();
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float currentFrame = ref npc.localAI[0];
            ref float attackSpecificTimer = ref npc.Infernum().ExtraAI[5];
            ref float firingFromRight = ref npc.Infernum().ExtraAI[6];

            // Die if the either brother is missing.
            if (cataclysmIndex == -1 || catastropheIndex == -1)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.netUpdate = true;
                return;
            }

            if (isCatastrophe)
            {
                // Shamelessly steal variables from Cataclysm.
                NPC cataclysm = Main.npc[cataclysmIndex];

                // Sync if Catastrophe changed attack states or there's a noticeable discrepancy between attack timers.
                if (attackState != cataclysm.ai[0] || MathHelper.Distance(attackTimer, cataclysm.ai[1]) > 20f)
                    npc.netUpdate = true;

                npc.ai = cataclysm.ai;
                npc.target = cataclysm.target;
                npc.life = cataclysm.life;
                npc.lifeMax = cataclysm.lifeMax;
                npc.realLife = cataclysm.whoAmI;
                attackState = ref cataclysm.ai[0];
                attackTimer = ref cataclysm.ai[1];

                // Use a fallback target if Cataclysm doesn't have one at the moment. This will not care about large distances.
                npc.TargetClosestIfTargetIsInvalid(1000000f);
            }

            // Have Cataclysm increment the attack timer and handle targeting.
            else if (isCataclysm)
            {
                npc.TargetClosestIfTargetIsInvalid();
                attackTimer++;
            }

            Player target = Main.player[npc.target];

            // Perform attacks.
            switch ((SCalBrotherAttackType)attackState)
            {
                case SCalBrotherAttackType.AttackDelay:
                    DoBehavior_AttackDelay(npc, target, isCataclysm, ref currentFrame, ref attackTimer);
                    break;
                case SCalBrotherAttackType.SinusoidalBobbing:
                    DoBehavior_SinusoidalBobbing(npc, target, isCataclysm, ref attackSpecificTimer, ref currentFrame, ref firingFromRight, ref attackTimer);
                    break;
                case SCalBrotherAttackType.ProjectileShooting:
                    DoBehavior_ProjectileShooting(npc, target, isCataclysm, ref attackSpecificTimer, ref currentFrame, ref firingFromRight, ref attackTimer);
                    break;
            }
        }

        public static void DoFastHoverMovement(NPC npc, Vector2 hoverDestination)
        {
            float distanceFromDestination = npc.Distance(hoverDestination);
            Vector2 closeMoveVelocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(distanceFromDestination, 24f);
            npc.velocity = Vector2.Lerp(closeMoveVelocity, (hoverDestination - npc.Center) * 0.0125f, Utils.InverseLerp(360f, 1080f, distanceFromDestination, true));
            npc.rotation = MathHelper.Clamp(npc.velocity.X * 0.026f, -0.2f, 0.2f);
        }

        public static void DoBehavior_AttackDelay(NPC npc, Player target, bool isCataclysm, ref float currentFrame, ref float attackTimer)
        {
            int transitionDelay = 150;

            // Reset rotation to zero.
            npc.rotation = 0f;

            // Define the direction and animation type.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            currentFrame = (int)SCalBrotherAnimationType.HoverInPlace;

            // Hover to the side of the target.
            float acceleration = 0.925f;
            Vector2 idealVelocity = npc.SafeDirectionTo(target.Center + Vector2.UnitX * isCataclysm.ToDirectionInt() * -720f) * 30f;
            npc.SimpleFlyMovement(idealVelocity, acceleration);

            if (attackTimer >= transitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_SinusoidalBobbing(NPC npc, Player target, bool isCataclysm, ref float attackSpecificTimer, ref float currentFrame, ref float firingFromRight, ref float attackTimer)
        {
            int shootTime = 420;
            int projectileFireThreshold = isCataclysm ? 60 : 45;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float shootIncrement = MathHelper.Lerp(1.85f, 3f, 1f - lifeRatio);

            // Define the direction and animation type.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            currentFrame = (int)SCalBrotherAnimationType.AttackAnimation;

            float acceleration = 0.95f;
            float sinusoidalOffset = (float)Math.Sin(attackTimer * MathHelper.TwoPi / shootTime) * (!isCataclysm).ToDirectionInt() * 400f;
            Vector2 hoverDestination = target.Center + new Vector2(isCataclysm.ToDirectionInt() * -720f, sinusoidalOffset);
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 30f;
            npc.SimpleFlyMovement(idealVelocity, acceleration);

            // Increment the attack timer and shoot.
            attackSpecificTimer += shootIncrement;
            if (attackSpecificTimer >= projectileFireThreshold)
            {
                attackSpecificTimer = 0f;
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/SCalSounds/BrimstoneHellblastSound"), npc.Center);

                int type = ModContent.ProjectileType<SupremeCataclysmFist>();
                Vector2 projectileSpawnPosition = npc.Center + Vector2.UnitX * npc.spriteDirection * 74f;
                if (!isCataclysm)
                {
                    type = ModContent.ProjectileType<SupremeCatastropheSlash>();
                    projectileSpawnPosition = npc.Center + Vector2.UnitX * npc.spriteDirection * 125f;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int proj = Utilities.NewProjectileBetter(projectileSpawnPosition, Vector2.UnitX * npc.spriteDirection * 11f, type, 550, 0f);
                    if (Main.projectile.IndexInRange(proj))
                        Main.projectile[proj].ai[1] = firingFromRight;
                }
                firingFromRight = firingFromRight == 0f ? 1f : 0f;
            }

            if (attackTimer >= shootTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ProjectileShooting(NPC npc, Player target, bool isCataclysm, ref float attackSpecificTimer, ref float currentFrame, ref float firingFromRight, ref float attackTimer)
        {
            // Define attack values when the other brother is alive.
            int attackShiftDelay = 0;
            int hoverTime = 60;
            int shootTime = 240;
            int fireBurstCount = 4;
            int projectileFireThreshold = isCataclysm ? 60 : 45;
            float fireShootSpeed = 17.5f;

            int attackCycleTime = hoverTime + shootTime;
            int attackTime = (hoverTime + shootTime) * fireBurstCount;
            float wrappedTimer = attackTimer % attackCycleTime;

            if (attackTimer >= attackTime + attackShiftDelay)
                SelectNextAttack(npc);

            // Define the direction and animation type.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            currentFrame = (int)(isCataclysm || wrappedTimer < hoverTime ? SCalBrotherAnimationType.HoverInPlace : SCalBrotherAnimationType.AttackAnimation);

            // Slow down and do nothing prior to the attack ending.
            if (attackTimer >= attackTime)
            {
                npc.velocity *= 0.95f;
                npc.rotation *= 0.95f;
                return;
            }

            // Increment the attack timer.
            attackSpecificTimer += isCataclysm ? 8f : 3f;

            float hoverOffsetDirection = isCataclysm.ToDirectionInt() * ((int)(attackTimer / attackCycleTime) % 2 == 0).ToDirectionInt();
            Vector2 hoverDestination = target.Center + new Vector2(hoverOffsetDirection * 550f, isCataclysm.ToInt() * -255f);
            if (wrappedTimer < hoverTime)
            {
                // Slow down right before firing.
                if (wrappedTimer > hoverTime * 0.5f)
                {
                    npc.velocity *= 0.9f;
                    npc.rotation *= 0.9f;
                }

                // Otherwise, do typical hover behavior, towards the upper right of the target.
                else
                {
                    DoFastHoverMovement(npc, hoverDestination);
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.rotation = MathHelper.Clamp(npc.velocity.X * 0.026f, -0.2f, 0.2f);
                }
            }
            else
            {
                if (isCataclysm)
                    npc.velocity = Vector2.Zero;
                else
                {
                    DoFastHoverMovement(npc, hoverDestination);
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.rotation = MathHelper.Clamp(npc.velocity.X * 0.026f, -0.2f, 0.2f);
                }

                // Rapidly approach a 0 rotation.
                npc.rotation = npc.rotation.AngleLerp(0f, 0.1f).AngleTowards(0f, 0.15f);

                if (attackSpecificTimer >= projectileFireThreshold)
                {
                    // Play a firing sound.
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/SCalSounds/BrimstoneShoot"), npc.Center);

                    // And shoot the projectile serverside.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int projectileType = ModContent.ProjectileType<RedirectingHellfireSCal>();
                        Vector2 shootVelocity = -Vector2.UnitY.RotatedByRandom(MathHelper.Pi / 9f) * fireShootSpeed * Main.rand.NextFloat(0.9f, 1.125f);
                        Vector2 projectileSpawnPosition = npc.Center + shootVelocity * 5.4f;
                        if (!isCataclysm)
                        {
                            projectileType = ModContent.ProjectileType<SupremeCatastropheSlash>();
                            projectileSpawnPosition = npc.Center + Vector2.UnitX * npc.spriteDirection * 125f;
                            shootVelocity = Vector2.UnitX * npc.spriteDirection * 17.5f;
                            firingFromRight = firingFromRight == 0f ? 1f : 0f;
                        }

                        Utilities.NewProjectileBetter(projectileSpawnPosition, shootVelocity, projectileType, 500, 0f);
                        attackSpecificTimer = 0f;
                        npc.netUpdate = true;
                    }
                }
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            // Catastrophe does not have control over when attack switches happen.
            bool isCatastrophe = npc.type == ModContent.NPCType<SupremeCataclysm>();
            if (isCatastrophe)
                return;

            // The 6 instead of 5 is intentional in the loop below. It's intended to clear the attack specific timer.
            npc.ai[0] = (int)SCalBrotherAttackType.ProjectileShooting;
            npc.ai[1] = 0f;
            for (int i = 0; i < 6; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.netUpdate = true;
        }

        #endregion AI

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            ref float currentFrame = ref npc.localAI[1];
            ref float attackSpecificTimer = ref npc.Infernum().ExtraAI[5];
            ref float firingFromRight = ref npc.Infernum().ExtraAI[6];
            switch ((SCalBrotherAnimationType)npc.localAI[0])
            {
                case SCalBrotherAnimationType.HoverInPlace:
                    npc.frameCounter += 0.15;
                    if (npc.frameCounter >= 1D)
                    {
                        currentFrame = (currentFrame + 1f) % 12f;
                        npc.frameCounter = 0D;
                    }
                    break;
                case SCalBrotherAnimationType.AttackAnimation:
                    float punchInterpolant = Utils.InverseLerp(10f, SupremeCataclysm.PunchCounterLimit * 2f, attackSpecificTimer + (firingFromRight != 0f ? 0f : SupremeCataclysm.PunchCounterLimit), true);
                    currentFrame = (int)Math.Round(MathHelper.Lerp(12f, 21f, punchInterpolant));
                    break;
            }

            int xFrame = (int)currentFrame / Main.npcFrameCount[npc.type];
            int yFrame = (int)currentFrame % Main.npcFrameCount[npc.type];

            npc.frame.Width = 212;
            npc.frame.Height = 208;
            npc.frame.X = xFrame * npc.frame.Width;
            npc.frame.Y = yFrame * npc.frame.Height;
        }
        #endregion Frames and Drawcode
    }
}