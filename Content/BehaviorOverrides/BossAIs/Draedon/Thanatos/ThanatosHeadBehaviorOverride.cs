using CalamityMod;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.Particles;
using CalamityMod.Skies;
using CalamityMod.Sounds;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.DoG;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ComboAttacks;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.DraedonBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Thanatos
{
    public class ThanatosHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum ThanatosFrameType
        {
            Closed,
            Open
        }

        public enum ThanatosHeadAttackType
        {
            AggressiveCharge,
            ExoBomb,
            ExoLightBarrage,
            RefractionRotorRays,

            // Ultimate attack. Only happens when in the final phase.
            MaximumOverdrive
        }

        public override int NPCOverrideType => ModContent.NPCType<ThanatosHead>();

        public const int SegmentCount = 100;

        public const int TransitionSoundDelay = 80;

        public const float OpenSegmentDR = 0f;

        public const float ClosedSegmentDR = 0.98f;

        public const float FlatDamageBoostFactor = 1.66f;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            ExoMechManagement.Phase4LifeRatio
        };

        #region Loading
        public override void Load()
        {
            GlobalNPCOverrides.StrikeNPCEvent += AddDamageMultiplier;
        }

        private bool AddDamageMultiplier(NPC npc, ref NPC.HitModifiers modifiers)
        {
            // Make Thanatos' head take a flat multiplier in terms of final damage, as a means of allowing direct hits to be effective.
            if (npc.type == ModContent.NPCType<ThanatosHead>())
            {
                modifiers.FinalDamage.Base *= FlatDamageBoostFactor;
                if (npc.Calamity().DR > 0.999f)
                {
                    modifiers.FinalDamage.Base *= 0f;
                    return false;
                }
            }

            bool isThanatos = npc.type == ModContent.NPCType<ThanatosHead>() || npc.type == ModContent.NPCType<ThanatosBody1>() || npc.type == ModContent.NPCType<ThanatosBody2>() || npc.type == ModContent.NPCType<ThanatosTail>();
            if (isThanatos)
            {
                NPC head = npc.realLife >= 0 ? Main.npc[npc.realLife] : npc;

                // Disable damage and start the death animation if the hit would kill Thanatos.
                if (head.life - modifiers.FinalDamage.Base <= 1)
                {
                    head.life = 0;
                    head.checkDead();
                    modifiers.FinalDamage.Base *= 0f;
                    npc.dontTakeDamage = true;
                    return false;
                }
            }

            return true;
        }
        #endregion Loading

        #region Netcode Syncs

        public override void SendExtraData(NPC npc, ModPacket writer) => writer.Write(npc.Opacity);

        public override void ReceiveExtraData(NPC npc, BinaryReader reader) => npc.Opacity = reader.ReadSingle();

        #endregion Netcode Syncs

        #region AI and Behaviors
        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 164;
            npc.height = 164;
            npc.scale = 1f;
            npc.Opacity = 0f;
            npc.defense = 80;
            npc.DR_NERD(0.9999f);
        }

        public override bool PreAI(NPC npc)
        {
            // Define the life ratio.
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Define the whoAmI variable.
            CalamityGlobalNPC.draedonExoMechWorm = npc.whoAmI;

            // Reset frame states.
            ref float frameType = ref npc.localAI[0];
            frameType = (int)ThanatosFrameType.Closed;

            // Reset damage.
            npc.defDamage = ThanatosHeadDamage;
            npc.damage = npc.defDamage;

            // Define attack variables.
            bool performingDeathAnimation = ExoMechAIUtilities.PerformingDeathAnimation(npc);
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float segmentsSpawned = ref npc.ai[2];
            ref float hasSummonedComplementMech = ref npc.Infernum().ExtraAI[ExoMechManagement.HasSummonedComplementMechIndex];
            ref float complementMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.ComplementMechIndexIndex];
            ref float wasNotInitialSummon = ref npc.Infernum().ExtraAI[ExoMechManagement.WasNotInitialSummonIndex];
            ref float finalMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.FinalMechIndexIndex];
            ref float attackDelay = ref npc.Infernum().ExtraAI[ExoMechManagement.Thanatos_AttackDelayIndex];
            ref float finalPhaseAnimationTime = ref npc.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex];
            ref float secondComboPhaseResistanceBoostFlag = ref npc.Infernum().ExtraAI[17];
            ref float deathAnimationTimer = ref npc.Infernum().ExtraAI[ExoMechManagement.DeathAnimationTimerIndex];

            NPC initialMech = ExoMechManagement.FindInitialMech();
            NPC complementMech = complementMechIndex >= 0 && Main.npc[(int)complementMechIndex].active && Utilities.IsExoMech(Main.npc[(int)complementMechIndex]) ? Main.npc[(int)complementMechIndex] : null;
            NPC finalMech = ExoMechManagement.FindFinalMech();

            // Define rotation and direction.
            int oldDirection = npc.direction;
            npc.rotation = npc.velocity.ToRotation() + PiOver2;
            npc.direction = npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
            if (oldDirection != npc.direction)
                npc.netUpdate = true;

            // Create segments.
            if (Main.netMode != NetmodeID.MultiplayerClient && (NPC.CountNPCS(ModContent.NPCType<ThanatosBody1>()) <= 0 || segmentsSpawned == 0f))
            {
                int previous = npc.whoAmI;
                for (int i = 0; i < SegmentCount; i++)
                {
                    int lol;
                    if (i < SegmentCount - 1)
                    {
                        if (i % 2 == 0)
                            lol = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ThanatosBody1>(), npc.whoAmI);
                        else
                            lol = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ThanatosBody2>(), npc.whoAmI);
                    }
                    else
                        lol = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ThanatosTail>(), npc.whoAmI);

                    Main.npc[lol].realLife = npc.whoAmI;
                    Main.npc[lol].ai[0] = i;
                    Main.npc[lol].ai[1] = previous;
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, lol, 0f, 0f, 0f, 0);
                    previous = lol;
                }

                npc.ai[0] = (int)ThanatosHeadAttackType.AggressiveCharge;
                finalMechIndex = -1f;
                complementMechIndex = -1f;
                segmentsSpawned++;
                npc.netUpdate = true;
            }

            // Summon the complement mech and reset things once ready.
            if (hasSummonedComplementMech == 0f && lifeRatio < ExoMechManagement.Phase4LifeRatio)
            {
                ExoMechManagement.SummonComplementMech(npc);
                hasSummonedComplementMech = 1f;
                attackTimer = 0f;
                SelectNextAttack(npc);
                npc.netUpdate = true;
            }

            // Summon the final mech once ready.
            if (wasNotInitialSummon == 0f && finalMechIndex == -1f && complementMech != null && complementMech.life / (float)complementMech?.lifeMax < ExoMechManagement.ComplementMechInvincibilityThreshold)
            {
                ExoMechManagement.SummonFinalMech(npc);
                npc.netUpdate = true;
            }

            // Get a target.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Become invincible if the complement mech is at high enough health or if in the middle of a death animation.
            npc.dontTakeDamage = ExoMechAIUtilities.PerformingDeathAnimation(npc);
            npc.Calamity().unbreakableDR = false;
            bool dontResetDR = false;
            if (complementMechIndex >= 0 && Main.npc[(int)complementMechIndex].active && Main.npc[(int)complementMechIndex].life > Main.npc[(int)complementMechIndex].lifeMax * ExoMechManagement.ComplementMechInvincibilityThreshold)
            {
                dontResetDR = true;
                npc.dontTakeDamage = false;
                npc.Calamity().DR = 0.9999999f;
                npc.Calamity().unbreakableDR = true;
            }

            // Become invincible and disappear if necessary.
            npc.Calamity().newAI[1] = 0f;
            if (ExoMechAIUtilities.ShouldExoMechVanish(npc))
            {
                npc.Opacity = Clamp(npc.Opacity - 0.08f, 0f, 1f);
                if (npc.Opacity <= 0f)
                    npc.Center = target.Center + Vector2.UnitY * 1600f;

                attackTimer = 0f;
                attackState = (int)ThanatosHeadAttackType.AggressiveCharge;
                npc.Calamity().newAI[1] = (int)ThanatosHead.SecondaryPhase.PassiveAndImmune;
                npc.Calamity().ShouldCloseHPBar = true;
                npc.dontTakeDamage = true;
            }
            else
                npc.Opacity = Clamp(npc.Opacity + 0.08f, 0f, 1f);

            // Kill debuffs.
            DoGPhase1BodyBehaviorOverride.KillUnbalancedDebuffs(npc);

            // Despawn if the target is gone.
            if (!target.active || target.dead)
            {
                npc.TargetClosest(false);
                target = Main.player[npc.target];
                if (!target.active || target.dead)
                    npc.active = false;
            }

            // Have a brief period of immortality before attacking to allow for time to uncoil.
            if (attackDelay < 270f && !performingDeathAnimation)
            {
                if (attackDelay < 240f)
                {
                    npc.dontTakeDamage = true;
                    npc.damage = 0;
                }
                npc.rotation = npc.velocity.ToRotation() + PiOver2;
                attackDelay++;
                DoProjectileShootInterceptionMovement(npc, target, Utils.GetLerpValue(330f, 100f, attackDelay, true) * 1.8f);
                return false;
            }

            // Handle the final phase transition.
            if (finalPhaseAnimationTime <= ExoMechManagement.FinalPhaseTransitionTime && ExoMechManagement.CurrentThanatosPhase >= 6 && !ExoMechManagement.ExoMechIsPerformingDeathAnimation)
            {
                frameType = (int)ThanatosFrameType.Closed;
                attackState = (int)ThanatosHeadAttackType.AggressiveCharge;
                finalPhaseAnimationTime++;
                npc.damage = 0;
                npc.dontTakeDamage = true;
                DoBehavior_DoFinalPhaseTransition(npc, target, finalPhaseAnimationTime);

                if (finalPhaseAnimationTime >= ExoMechManagement.FinalPhaseTransitionTime)
                {
                    npc.Infernum().ExtraAI[ExoMechManagement.Thanatos_FinalPhaseAttackCounter] = 0f;
                    SelectNextAttack(npc);
                }

                // The delay before returning is to ensure that DR code is executed that reflects the fact that Thanatos' head segment is closed.
                if (finalPhaseAnimationTime >= 3f)
                    return false;
            }

            // Use combo attacks as necessary.
            if (ExoMechManagement.TotalMechs >= 2 && (int)attackState < 100)
            {
                attackTimer = 0f;

                if (initialMech.whoAmI == npc.whoAmI)
                    SelectNextAttack(npc);

                attackState = initialMech.ai[0];
                npc.netUpdate = true;
            }

            if ((finalMech != null && finalMech.Opacity > 0f || ExoMechManagement.CurrentThanatosPhase >= 6) && attackState >= 100f)
            {
                attackTimer = 0f;
                attackState = 0f;
                npc.netUpdate = true;
            }

            // Handle smoke venting and open/closed DR.
            if (!dontResetDR)
                npc.Calamity().DR = ClosedSegmentDR;
            npc.Calamity().unbreakableDR = true;
            npc.chaseable = false;
            npc.defense = 0;
            npc.takenDamageMultiplier = 1f;

            // Become vulnerable on the map.
            typeof(ThanatosHead).GetField("vulnerable", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(npc.ModNPC, frameType == (int)ThanatosFrameType.Open);

            if (!performingDeathAnimation)
            {
                switch ((ThanatosHeadAttackType)(int)attackState)
                {
                    case ThanatosHeadAttackType.AggressiveCharge:
                        DoBehavior_AggressiveCharge(npc, target, ref attackTimer, ref frameType);
                        break;
                    case ThanatosHeadAttackType.ExoBomb:
                        DoBehavior_ExoBomb(npc, target, ref attackTimer, ref frameType);
                        break;
                    case ThanatosHeadAttackType.RefractionRotorRays:
                        DoBehavior_RefractionRotorRays(npc, target, ref attackTimer, ref frameType);
                        break;
                    case ThanatosHeadAttackType.ExoLightBarrage:
                        DoBehavior_ExoLightBarrage(npc, target, ref attackTimer, ref frameType);
                        break;
                    case ThanatosHeadAttackType.MaximumOverdrive:
                        DoBehavior_MaximumOverdrive(npc, target, ref attackTimer, ref frameType);
                        break;
                }

                if (ExoMechComboAttackContent.UseThanatosAresComboAttack(npc, ref attackTimer, ref frameType))
                    SelectNextAttack(npc);
                if (ExoMechComboAttackContent.UseTwinsThanatosComboAttack(npc, 1f, ref attackTimer, ref frameType))
                    SelectNextAttack(npc);
            }
            else
            {
                DoBehavior_DeathAnimation(npc, target, ref deathAnimationTimer, ref frameType);
                deathAnimationTimer++;
            }

            if (frameType == (int)ThanatosFrameType.Open)
            {
                // Emit light.
                Lighting.AddLight(npc.Center, 0.35f * npc.Opacity, 0.05f * npc.Opacity, 0.05f * npc.Opacity);

                npc.takenDamageMultiplier = 103.184f;
                if (!dontResetDR)
                    npc.Calamity().DR = OpenSegmentDR - 0.125f;
                npc.Calamity().unbreakableDR = false;
                npc.chaseable = true;
            }
            // Emit light.
            else
                Lighting.AddLight(npc.Center, 0.05f * npc.Opacity, 0.2f * npc.Opacity, 0.2f * npc.Opacity);

            secondComboPhaseResistanceBoostFlag = 0f;
            if (ExoMechManagement.ShouldHaveSecondComboPhaseResistance(npc) && npc == complementMech)
            {
                secondComboPhaseResistanceBoostFlag = 1f;
                npc.takenDamageMultiplier *= 0.5f;
            }

            attackTimer++;

            return false;
        }

        public static void DoBehavior_DeathAnimation(NPC npc, Player target, ref float deathAnimationTimer, ref float frameType)
        {
            // Clear away projectiles.
            ExoMechManagement.ClearAwayTransitionProjectiles();

            bool isHead = npc.type == ModContent.NPCType<ThanatosHead>();
            bool closeToDying = deathAnimationTimer >= 180f;

            // Close the HP bar.
            npc.Calamity().ShouldCloseHPBar = true;

            // Use close to the minimum HP.
            npc.life = 50000;

            // Disable contact damage.
            npc.dontTakeDamage = true;
            npc.damage = 0;

            // Slowly attempt to approach the target.
            if (isHead && !closeToDying)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 6f, 0.056f);
                if (npc.velocity.Length() > 10f)
                    npc.velocity *= 0.96f;
            }

            // Release bursts of energy sometimes.
            if (deathAnimationTimer >= 35f && !closeToDying && Main.rand.NextBool(800))
            {
                SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, npc.Center);
                GeneralParticleHandler.SpawnParticle(new PulseRing(npc.Center, Vector2.Zero, Color.Red, 0f, 3.5f, 50));

                for (int i = 0; i < 8; i++)
                {
                    Vector2 sparkVelocity = -Vector2.UnitY.RotatedByRandom(1.23f) * Main.rand.NextFloat(6f, 14f);
                    GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(npc.Center, sparkVelocity, 1f, Color.Red, 55, 1f, 5f));
                }
            }

            if (deathAnimationTimer == 240f && isHead)
            {
                int thanatosHeadID = ModContent.NPCType<ThanatosHead>();
                int thanatosBodyID = ModContent.NPCType<ThanatosBody2>();

                // Play an explosion sound.
                SoundEngine.PlaySound(InfernumSoundRegistry.WyrmChargeSound with { Volume = 1.75f }, npc.Center);

                Color[] explosionColorPalette = (Color[])CalamityUtils.ExoPalette.Clone();
                for (int j = 0; j < explosionColorPalette.Length; j++)
                    explosionColorPalette[j] = Color.Lerp(explosionColorPalette[j], Color.Red, 0.3f);

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (!Main.npc[i].active)
                        continue;

                    if (Main.npc[i].type == thanatosBodyID && i % 3 == 0 || Main.npc[i].type == thanatosHeadID)
                        GeneralParticleHandler.SpawnParticle(new ElectricExplosionRing(Main.npc[i].Center, Vector2.Zero, explosionColorPalette, 2.1f, 90, 0.4f));
                }
            }

            if (deathAnimationTimer >= 260f)
            {
                npc.life = 0;
                npc.HitEffect();
                NPC.HitInfo hit = new()
                {
                    Damage = 10,
                    Knockback = 0f,
                    HitDirection = 0
                };
                npc.StrikeNPC(hit);
                npc.checkDead();
            }

            // Open vents (pretty sus ngl).
            frameType = (int)ThanatosFrameType.Open;
        }

        public static void DoBehavior_AggressiveCharge(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            // Decide frames.
            frameType = (int)ThanatosFrameType.Open;

            // Handle movement.
            DoAggressiveChargeMovement(npc, target, attackTimer, 1f);

            // Play a sound prior to switching attacks.
            if (attackTimer == 540f - TransitionSoundDelay)
                SoundEngine.PlaySound(InfernumSoundRegistry.ThanatosTransitionSound with { Volume = 2f }, target.Center);

            if (attackTimer > 540f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ExoBomb(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            // Decide frames.
            frameType = (int)ThanatosFrameType.Open;

            int initialRedirectTime = 360;
            int spinBufferTime = 300;
            int intendedSpinTime = 105;
            int postSpinChargeTime = 165;
            float chargeSpeed = 37f;
            float spinSpeed = 51f;
            float totalRotations = 1f;

            if (ExoMechManagement.CurrentThanatosPhase >= 2)
            {
                intendedSpinTime -= 15;
                chargeSpeed += 6f;
                spinSpeed += 5f;
            }
            if (ExoMechManagement.CurrentThanatosPhase >= 3)
            {
                intendedSpinTime -= 15;
                chargeSpeed += 4f;
                spinSpeed += 5f;
            }
            if (ExoMechManagement.CurrentThanatosPhase >= 5)
            {
                intendedSpinTime -= 15;
                spinSpeed += 5f;
            }
            if (ExoMechManagement.CurrentThanatosPhase >= 6)
            {
                intendedSpinTime -= 25;
                chargeSpeed += 8f;
                spinSpeed += 5f;
            }

            Vector2 hoverOffset = new((target.Center.X < npc.Center.X).ToDirectionInt() * 450f, -270f);
            Vector2 hoverDestination = target.Center + hoverOffset;
            ref float spinTime = ref npc.Infernum().ExtraAI[0];

            // Initialize the spin time. This is done via a variable because it's possible that the spin time will otherwise switch if Thanatos changes subphases mid-attack, potentially
            // resulting in strange behaviors.
            if (spinTime == 0f)
            {
                spinTime = intendedSpinTime;
                npc.netUpdate = true;
            }

            // Disable contact damage.
            npc.damage = 0;

            // Attempt to get into position for a charge.
            if (attackTimer < initialRedirectTime)
            {
                float idealHoverSpeed = Lerp(43.5f, 72.5f, attackTimer / initialRedirectTime);
                idealHoverSpeed *= Utils.GetLerpValue(35f, 300f, npc.Distance(target.Center), true);

                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * Lerp(npc.velocity.Length(), idealHoverSpeed, 0.135f);
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.045f, true) * idealVelocity.Length();
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, 3f);

                // Stop hovering if close to the hover destination and prepare the spin.
                if (npc.WithinRange(hoverDestination, 90f) && attackTimer > 45f)
                {
                    attackTimer = initialRedirectTime;
                    npc.velocity = npc.SafeDirectionTo(target.Center) * -spinSpeed;
                    npc.netUpdate = true;
                }
            }

            // Create the exo bomb.
            if (attackTimer == initialRedirectTime + 1f)
            {
                Vector2 bombSpawnPosition = npc.Center + npc.velocity.RotatedBy(PiOver2) * spinTime / totalRotations / TwoPi;

                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(bomb =>
                {
                    bomb.ModProjectile<ExolaserBomb>().GrowTime = (int)npc.Infernum().ExtraAI[0];
                });
                Utilities.NewProjectileBetter(bombSpawnPosition, Vector2.Zero, ModContent.ProjectileType<ExolaserBomb>(), PowerfulShotDamage, 0f);
            }

            // Spin.
            if (attackTimer >= initialRedirectTime && attackTimer < initialRedirectTime + spinBufferTime)
            {
                npc.velocity = npc.velocity.RotatedBy(TwoPi * totalRotations / spinTime);

                if (attackTimer >= initialRedirectTime + spinTime && npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < 0.1f)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * npc.velocity.Length();

                    float bombSpeed = Lerp(15f, 35f, Utils.GetLerpValue(750f, 1500f, npc.Distance(target.Center), true));
                    foreach (Projectile exoBomb in Utilities.AllProjectilesByID(ModContent.ProjectileType<ExolaserBomb>()))
                    {
                        exoBomb.velocity = exoBomb.SafeDirectionTo(target.Center + target.velocity * 25f) * bombSpeed;
                        exoBomb.netUpdate = true;
                    }
                    attackTimer = initialRedirectTime + spinBufferTime;
                    npc.netUpdate = true;
                }
            }

            // Charge.
            if (attackTimer >= initialRedirectTime + spinBufferTime)
            {
                npc.damage = npc.defDamage;
                npc.velocity *= npc.velocity.Length() > chargeSpeed * 0.56f ? 0.98f : 1.02f;
                if (!npc.WithinRange(target.Center, 1200f))
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.05f);
            }

            // Play a sound prior to switching attacks.
            if (attackTimer == initialRedirectTime + spinBufferTime + postSpinChargeTime - TransitionSoundDelay)
                SoundEngine.PlaySound(InfernumSoundRegistry.ThanatosTransitionSound with { Volume = 2f }, target.Center);

            if (attackTimer == initialRedirectTime + spinBufferTime + postSpinChargeTime)
            {
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<ExolaserSpark>());
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_RefractionRotorRays(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            // Decide frames.
            frameType = (int)ThanatosFrameType.Open;

            int slowdownTime = 100;
            int chargePreparationTime = 60;
            int redirectTime = 90;
            int chargeTime = 45;
            int attackShiftDelay = 120;
            int lasersPerRotor = 5;
            int rotorReleaseRate = 7;
            int chargeCount = 2;
            float initialChargeSpeed = 13f;
            float finalChargeSpeed = 23f;
            float rotorSpeed = 25f;
            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];

            if (ExoMechManagement.CurrentThanatosPhase >= 2)
            {
                slowdownTime -= 15;
                redirectTime -= 5;
            }

            if (ExoMechManagement.CurrentThanatosPhase >= 3)
            {
                slowdownTime -= 10;
                chargePreparationTime -= 8;
                redirectTime -= 5;
                chargeTime -= 5;
                finalChargeSpeed += 3f;
            }

            if (ExoMechManagement.CurrentThanatosPhase >= 5)
            {
                slowdownTime -= 12;
                chargePreparationTime -= 8;
                redirectTime -= 5;
                lasersPerRotor++;
                rotorReleaseRate--;
                chargeCount++;
                finalChargeSpeed += 3f;
            }

            if (ExoMechManagement.CurrentThanatosPhase >= 6)
            {
                slowdownTime -= 12;
                chargePreparationTime -= 8;
                redirectTime -= 5;
                chargeTime -= 8;
                lasersPerRotor++;
                finalChargeSpeed += 3f;
            }

            // Don't deal damage because its apparently really annoying to dodge even though its half the damn attack.
            npc.damage = 0;

            // Approach the player at an increasingly slow speed.
            if (attackTimer < slowdownTime)
            {
                float speedMultiplier = Lerp(0.75f, 0.385f, attackTimer / slowdownTime);
                DoAggressiveChargeMovement(npc, target, attackTimer, speedMultiplier);
            }

            // Continue moving in the current direction, but continue slowing down.
            if (attackTimer >= slowdownTime && attackTimer < slowdownTime + chargePreparationTime)
                npc.velocity = (npc.velocity * 0.96f).ClampMagnitude(8f, 27f);

            // Play a telegraph sound to alert the player of the impending charge.
            if (attackTimer == slowdownTime + chargePreparationTime / 2)
            {
                SoundEngine.PlaySound(ScorchedEarth.ShootSound, target.Center);
                target.Infernum_Camera().CurrentScreenShakePower = 6f;
            }

            // Begin the charge.
            if (attackTimer >= slowdownTime + chargePreparationTime && attackTimer < slowdownTime + chargePreparationTime + redirectTime)
            {
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * initialChargeSpeed;
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.075f).MoveTowards(idealVelocity, 2.75f);

                // Sometimes charge early if aimed at the target and the redirect is more than halfway done.
                if (attackTimer >= chargePreparationTime + redirectTime * 0.5f && npc.velocity.AngleBetween(idealVelocity) < 0.15f)
                {
                    npc.velocity = idealVelocity;
                    attackTimer = slowdownTime + chargePreparationTime + redirectTime;
                    npc.netUpdate = true;
                }
            }

            // Charge and release refraction rotors.
            if (attackTimer >= slowdownTime + chargePreparationTime + redirectTime)
            {
                npc.velocity = npc.velocity.MoveTowards(npc.velocity.SafeNormalize(Vector2.UnitY) * finalChargeSpeed, 2f);
                if (!npc.WithinRange(target.Center, 1600f))
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.05f);

                if (attackTimer % rotorReleaseRate == rotorReleaseRate - 1f)
                {
                    // Randomly pick a segment that's decently far away from the target but not too far away to release a rotor from.
                    var segments = (from n in Main.npc.Take(Main.maxNPCs)
                                    where n.active && n.type == ModContent.NPCType<ThanatosBody1>() && !n.WithinRange(target.Center, 400f) && n.WithinRange(target.Center, 1200f)
                                    orderby n.Distance(target.Center)
                                    select n).ToList();

                    if (Main.netMode != NetmodeID.MultiplayerClient && segments.Count > 1)
                    {
                        NPC segmentToFireFrom = segments[Main.rand.Next(0, segments.Count / 3)];
                        Vector2 rotorShootVelocity = segmentToFireFrom.SafeDirectionTo(target.Center).RotatedByRandom(1.6f) * rotorSpeed;
                        Utilities.NewProjectileBetter(segmentToFireFrom.Center, rotorShootVelocity, ModContent.ProjectileType<RefractionRotor>(), 0, 0f, -1, lasersPerRotor);
                    }
                }
            }

            // Play a sound prior to switching attacks.
            if (attackTimer == slowdownTime + chargePreparationTime + redirectTime + chargeTime + attackShiftDelay - TransitionSoundDelay && chargeCounter >= chargeCount - 1f)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center) * 14f;
                npc.netUpdate = true;

                SoundEngine.PlaySound(InfernumSoundRegistry.ThanatosTransitionSound with { Volume = 2f }, target.Center);
            }

            // Perform the attack again if necessary.
            if (attackTimer >= slowdownTime + chargePreparationTime + redirectTime + chargeTime + attackShiftDelay)
            {
                chargeCounter++;
                attackTimer = 0f;
                if (chargeCounter >= chargeCount)
                {
                    for (int i = 0; i < 2; i++)
                        Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<RefractionRotor>(), ModContent.ProjectileType<ExolaserSpark>());

                    SelectNextAttack(npc);
                }
            }
        }

        public static void DoBehavior_ExoLightBarrage(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            // Decide frames.
            frameType = (int)ThanatosFrameType.Open;

            // Disable contact damage.
            npc.damage = 0;

            int initialRedirectTime = 360;
            int lightTelegraphTime = 95;
            int lightLaserFireDelay = 20;
            int lightLaserShootTime = LightOverloadRay.Lifetime;
            int redirectCount = 3;
            float pointAtTargetSpeed = 2f;
            float lightRaySpreadDegrees = 125f;
            ref float hoverOffsetDirection = ref npc.Infernum().ExtraAI[0];
            ref float rayTelegraphSoundSlot = ref npc.Infernum().ExtraAI[1];
            ref float redirectCounter = ref npc.Infernum().ExtraAI[2];

            if (ExoMechManagement.CurrentThanatosPhase >= 6)
                redirectCount = 2;

            // Initialize a hover offset direction.
            if (hoverOffsetDirection == 0f)
            {
                hoverOffsetDirection = Main.rand.Next(4) * TwoPi / 4f + PiOver4;
                npc.netUpdate = true;
            }

            int totalLightRays = (int)(lightRaySpreadDegrees * 0.257f);
            float lightRaySpread = ToRadians(lightRaySpreadDegrees);
            Vector2 outerHoverOffset = hoverOffsetDirection.ToRotationVector2() * 1200f;
            Vector2 outerHoverDestination = target.Center + outerHoverOffset;

            // Clamp Thanatos' position to stay in the world.
            // This is very important, as the telegraph might simply not appear if Thanatos is too high up.
            if (npc.position.Y < 600f)
                npc.position.Y = 600f;

            // Update the sound telegraph's position to account for Thanatos drifting.
            if (SoundEngine.TryGetActiveSound(SlotId.FromFloat(rayTelegraphSoundSlot), out var t) && t.IsPlaying)
                t.Position = npc.Center;

            // Attempt to get into position for the light attack.
            if (attackTimer < initialRedirectTime)
            {
                float idealHoverSpeed = Lerp(43.5f, 72.5f, attackTimer / initialRedirectTime);
                idealHoverSpeed *= Utils.GetLerpValue(35f, 300f, npc.Distance(target.Center), true);

                Vector2 idealVelocity = npc.SafeDirectionTo(outerHoverDestination) * Lerp(npc.velocity.Length(), idealHoverSpeed, 0.135f);
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.045f, true) * idealVelocity.Length();
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, 8f);

                // Stop hovering if close to the hover destination and prepare to move towards the target.
                if (npc.WithinRange(outerHoverDestination, 90f) && attackTimer > 45f)
                {
                    attackTimer = initialRedirectTime;
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * npc.velocity.Length(), 0.85f);
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * Lerp(npc.velocity.Length(), pointAtTargetSpeed, 0.4f);
                    npc.netUpdate = true;
                }
            }

            // Slow down, move towards the target (while maintaining the current direction) and create a laser telegraph.
            if (attackTimer >= initialRedirectTime && attackTimer < initialRedirectTime + lightTelegraphTime)
            {
                // Create light telegraphs.
                if (attackTimer == initialRedirectTime + 1f)
                {
                    rayTelegraphSoundSlot = SoundEngine.PlaySound(InfernumSoundRegistry.ThanatosLightRay with { Volume = 3f }, npc.Center).ToFloat();
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < totalLightRays; i++)
                        {
                            float lightRayAngularOffset = Lerp(-lightRaySpread, lightRaySpread, i / (float)(totalLightRays - 1f));

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(lightRayTelegraph =>
                            {
                                lightRayTelegraph.ModProjectile<LightRayTelegraph>().Lifetime = lightTelegraphTime;
                            });
                            Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<LightRayTelegraph>(), 0, 0f, -1, i / (float)(totalLightRays - 1f), lightRayAngularOffset);
                        }
                    }
                }
                // Approach the ideal position.
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * Lerp(npc.velocity.Length(), pointAtTargetSpeed, 0.05f);
            }

            // Create a massive laser.
            if (attackTimer == initialRedirectTime + lightTelegraphTime + lightLaserFireDelay)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<OverloadBoom>(), 0, 0f);
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<LightOverloadRay>(), PowerfulShotDamage, 0f, -1, 0f, lightRaySpread * 0.53f);
                }

                ScreenEffectSystem.SetBlurEffect(npc.Center, 1.7f, 45);
                target.Infernum_Camera().CurrentScreenShakePower = 7f;
            }

            // Create explosions that make sparks after the lasers are fired.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= initialRedirectTime + lightTelegraphTime + lightLaserFireDelay && attackTimer % 12f == 11f)
            {
                Vector2 targetDirection = target.velocity.SafeNormalize(Main.rand.NextVector2Unit());
                Vector2 spawnPosition = target.Center - targetDirection.RotatedByRandom(1.1f) * Main.rand.NextFloat(325f, 650f) * new Vector2(1f, 0.6f);
                Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<AresBeamExplosion>(), StrongerNormalShotDamage, 0f);
            }

            // Play a sound prior to switching attacks.
            if (attackTimer == initialRedirectTime + lightTelegraphTime + lightLaserShootTime + lightLaserFireDelay - TransitionSoundDelay && redirectCounter >= redirectCount - 1f) // 
                SoundEngine.PlaySound(InfernumSoundRegistry.ThanatosTransitionSound with { Volume = 2f }, target.Center);

            if (attackTimer >= initialRedirectTime + lightTelegraphTime + lightLaserShootTime + lightLaserFireDelay)
            {
                attackTimer = 0f;
                hoverOffsetDirection += PiOver2;
                redirectCounter++;
                if (redirectCounter >= redirectCount)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<AresBeamExplosion>(), ModContent.ProjectileType<ExoburstSpark>());
                    }
                    SelectNextAttack(npc);
                }
            }
        }

        public static void DoBehavior_MaximumOverdrive(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            // Dash or die.
            npc.damage = ThanatosHeadDamageMaximumOverdrive;

            int chargeDelay = 270;
            int attackTime = 600;
            int cooloffTime = 360;
            bool dontAttackYet = attackTimer <= chargeDelay;
            bool firstTimeAttacking = npc.Infernum().ExtraAI[ExoMechManagement.Thanatos_FinalPhaseAttackCounter] <= 3f;
            if (!firstTimeAttacking)
                chargeDelay = 30;

            float chargeSpeedInterpolant = Utils.GetLerpValue(chargeDelay - 16f, chargeDelay + 25f, attackTimer, true) * Utils.GetLerpValue(attackTime, attackTime - 45f, attackTimer - chargeDelay, true);
            float chargeSpeedFactor = Lerp(0.3f, 1.2f, chargeSpeedInterpolant);

            ref float coolingOff = ref npc.Infernum().ExtraAI[0];

            if (attackTimer == chargeDelay / 2 && firstTimeAttacking)
                CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.ExoMechDesperationThanatos1", ThanatosTextColor);

            if (attackTimer == chargeDelay - 16f && firstTimeAttacking)
                CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.ExoMechDesperationThanatos2", ThanatosTextColor);

            // Play a danger sound before the attack begins.
            if (attackTimer == chargeDelay - (firstTimeAttacking ? 90f : 12f))
                SoundEngine.PlaySound(InfernumSoundRegistry.ExoMechImpendingDeathSound with { Volume = 3f });

            // Decide frames.
            frameType = (int)ThanatosFrameType.Open;

            // Decide whether to cool off or not.
            coolingOff = (attackTimer > attackTime + chargeDelay - 12f).ToInt();

            // Handle movement.
            DoAggressiveChargeMovement(npc, target, attackTimer, chargeSpeedFactor);

            // Periodically release lasers from the sides.
            if (Main.netMode != NetmodeID.MultiplayerClient && coolingOff == 0f && attackTimer % 60f == 59f && !dontAttackYet)
            {
                SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, target.Center);
                for (int i = 0; i < 3; i++)
                {
                    int type = ModContent.ProjectileType<DetatchedThanatosLaser>();
                    float shootSpeed = 12f;
                    Vector2 projectileDestination = target.Center;
                    Vector2 spawnPosition = target.Center + Main.rand.NextVector2CircularEdge(1500f, 1500f);

                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(laser =>
                    {
                        laser.ModProjectile<DetatchedThanatosLaser>().InitialDestination = projectileDestination;
                    });
                    Utilities.NewProjectileBetter(spawnPosition, npc.SafeDirectionTo(projectileDestination) * shootSpeed, type, StrongerNormalShotDamage, 0f, npc.target, 0f, npc.whoAmI);
                }
            }

            // Create lightning bolts in the sky.
            if (Main.netMode != NetmodeID.Server && attackTimer % 3f == 2f)
                ExoMechsSky.CreateLightningBolt();

            // Play a sound prior to switching attacks.
            if (attackTimer == chargeDelay + attackTime + cooloffTime - TransitionSoundDelay)
                SoundEngine.PlaySound(InfernumSoundRegistry.ThanatosTransitionSound with { Volume = 2f }, target.Center);

            if (attackTimer > chargeDelay + attackTime + cooloffTime)
                SelectNextAttack(npc);
        }

        public static void DoProjectileShootInterceptionMovement(NPC npc, Player target, float speedMultiplier = 1f)
        {
            // Attempt to intercept the target.
            Vector2 hoverDestination = target.Center + target.velocity.SafeNormalize(Vector2.UnitX * target.direction) * new Vector2(675f, 550f);
            hoverDestination.Y -= 550f;

            float idealFlySpeed = 19.5f;

            if (ExoMechManagement.CurrentThanatosPhase == 4)
                idealFlySpeed *= 0.7f;
            else
            {
                if (ExoMechManagement.CurrentThanatosPhase >= 2)
                    idealFlySpeed *= 1.2f;
            }
            if (ExoMechManagement.CurrentThanatosPhase >= 3)
                idealFlySpeed *= 1.2f;
            if (ExoMechManagement.CurrentThanatosPhase >= 5)
                idealFlySpeed *= 1.225f;

            idealFlySpeed += npc.Distance(target.Center) * 0.004f;
            idealFlySpeed *= speedMultiplier;

            // Move towards the target if far away from them.
            if (!npc.WithinRange(target.Center, 1600f))
                hoverDestination = target.Center;

            if (!npc.WithinRange(hoverDestination, 210f))
            {
                float flySpeed = Lerp(npc.velocity.Length(), idealFlySpeed, 0.05f);
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(hoverDestination), flySpeed / 580f, true) * flySpeed;
            }
            else
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(hoverDestination) * idealFlySpeed, idealFlySpeed / 24f);
        }

        public static void DoAggressiveChargeMovement(NPC npc, Player target, float attackTimer, float speedMultiplier = 1f)
        {
            speedMultiplier *= 1.1f;
            if (!target.HasShieldBash())
                speedMultiplier *= 0.67f;
            else
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.ThanatosChargeTip");

            float lifeRatio = npc.life / (float)npc.lifeMax;
            float flyAcceleration = Lerp(0.045f, 0.037f, lifeRatio);
            float idealFlySpeed = Lerp(13f, 9.6f, lifeRatio);
            float generalSpeedFactor = Utils.GetLerpValue(0f, 35f, attackTimer, true) * 0.825f + 1f;

            Vector2 destination = target.Center;

            float distanceFromDestination = npc.Distance(destination);
            if (!npc.WithinRange(destination, 550f))
            {
                distanceFromDestination = npc.Distance(destination);
                flyAcceleration *= 1.2f;
            }

            // Charge if the player is far away.
            // Don't do this at the start of the fight though. Doing so might lead to an unfair
            // charge.
            if (distanceFromDestination > 1750f && attackTimer > 90f)
                idealFlySpeed = 22f;

            if (ExoMechManagement.CurrentThanatosPhase == 4)
            {
                generalSpeedFactor *= 0.75f;
                flyAcceleration *= 0.5f;
            }
            else
            {
                if (ExoMechManagement.CurrentThanatosPhase <= 2)
                    generalSpeedFactor *= 1.08f;
                if (ExoMechManagement.CurrentThanatosPhase >= 3)
                    generalSpeedFactor *= 1.1f;
                if (ExoMechManagement.CurrentThanatosPhase >= 5)
                {
                    generalSpeedFactor *= 1.1f;
                    flyAcceleration *= 1.1f;
                }
            }

            // Enforce a lower bound on the speed factor.
            if (generalSpeedFactor < 1f)
                generalSpeedFactor = 1f;
            generalSpeedFactor *= speedMultiplier * 0.825f;
            flyAcceleration *= 1f + (speedMultiplier - 1f) * 1.3f;

            float directionToPlayerOrthogonality = Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(destination));

            // Adjust the speed based on how the direction towards the target compares to the direction of the
            // current velocity. This check is unnecessary once close to the target, which would prompt a snap/charge.
            if (!npc.WithinRange(destination, 250f))
            {
                float flySpeed = npc.velocity.Length();
                if (flySpeed < 14f)
                    flySpeed += 0.06f;

                if (flySpeed > 16.5f)
                    flySpeed -= 0.065f;

                if (directionToPlayerOrthogonality is < 0.85f and > 0.5f)
                    flySpeed += 0.16f;

                if (directionToPlayerOrthogonality is < 0.5f and > (-0.7f))
                    flySpeed -= 0.1f;

                flySpeed = Clamp(flySpeed, 14f, 20.5f) * generalSpeedFactor;
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination), flyAcceleration, true) * flySpeed;
            }

            if (!npc.WithinRange(target.Center, 200f))
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(target.Center) * npc.velocity.Length(), generalSpeedFactor);

            // Lunge if near the player.
            bool canCharge = ExoMechManagement.CurrentThanatosPhase != 4 && directionToPlayerOrthogonality > 0.75f && distanceFromDestination < 400f;
            if (canCharge && npc.velocity.Length() < idealFlySpeed * generalSpeedFactor * 1.8f)
                npc.velocity *= 1.2f;
        }

        public static void DoBehavior_DoFinalPhaseTransition(NPC npc, Player target, float phaseTransitionAnimationTime)
        {
            // Clear away projectiles.
            ExoMechManagement.ClearAwayTransitionProjectiles();

            DoProjectileShootInterceptionMovement(npc, target, 0.6f);

            // Heal HP.
            ExoMechAIUtilities.HealInFinalPhase(npc, phaseTransitionAnimationTime);

            // Play the transition sound at the start.
            if (phaseTransitionAnimationTime == 3f)
                SoundEngine.PlaySound(InfernumSoundRegistry.ExoMechFinalPhaseSound, target.Center);
        }

        public static void SelectNextAttack(NPC npc)
        {
            ref float previousSpecialAttack = ref npc.Infernum().ExtraAI[18];

            ThanatosHeadAttackType oldAttackType = (ThanatosHeadAttackType)(int)npc.ai[0];
            bool wasCharging = oldAttackType is ThanatosHeadAttackType.AggressiveCharge or ThanatosHeadAttackType.MaximumOverdrive;

            if (ExoMechComboAttackContent.ShouldSelectComboAttack(npc, out ExoMechComboAttackContent.ExoMechComboAttackType newAttack))
                npc.ai[0] = (int)newAttack;

            else if (wasCharging || Main.rand.NextBool())
            {
                do
                {
                    if (Main.rand.NextBool())
                        npc.ai[0] = (int)ThanatosHeadAttackType.RefractionRotorRays;
                    if (Main.rand.NextBool(3))
                        npc.ai[0] = (int)ThanatosHeadAttackType.ExoBomb;
                    if (Main.rand.NextBool(3) && ExoMechManagement.CurrentThanatosPhase >= 5)
                        npc.ai[0] = (int)ThanatosHeadAttackType.ExoLightBarrage;
                }
                while (npc.ai[0] == previousSpecialAttack);
                previousSpecialAttack = npc.ai[0];
            }
            else
                npc.ai[0] = (int)ThanatosHeadAttackType.AggressiveCharge;

            if (oldAttackType == ThanatosHeadAttackType.RefractionRotorRays)
                npc.ai[0] = (int)ThanatosHeadAttackType.ExoLightBarrage;

            // In the final phase a preset order is established, ending with the ultimate attack.
            if (ExoMechManagement.CurrentThanatosPhase >= 6)
            {
                ref float attackCounter = ref npc.Infernum().ExtraAI[ExoMechManagement.Thanatos_FinalPhaseAttackCounter];
                npc.ai[0] = (int)attackCounter switch
                {
                    0 => (int)ThanatosHeadAttackType.AggressiveCharge,
                    1 => (int)ThanatosHeadAttackType.ExoLightBarrage,
                    _ => (float)(int)ThanatosHeadAttackType.MaximumOverdrive,
                };
                attackCounter++;
            }

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI and Behaviors

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            // Swap between venting and non-venting frames.
            npc.frameCounter++;
            if (npc.localAI[0] == (int)ThanatosFrameType.Closed)
            {
                if (npc.frameCounter >= 6D)
                {
                    npc.frame.Y -= frameHeight;
                    npc.frameCounter = 0D;
                }
                if (npc.frame.Y < 0)
                    npc.frame.Y = 0;
            }
            else
            {
                if (npc.frameCounter >= 6D)
                {
                    npc.frame.Y += frameHeight;
                    npc.frameCounter = 0D;
                }
                int finalFrame = Main.npcFrameCount[npc.type] - 1;

                // Play a vent sound (sus).
                if (Main.netMode != NetmodeID.Server && npc.frame.Y == frameHeight * (finalFrame - 1))
                    SoundEngine.PlaySound(ThanatosHead.VentSound with { Volume = 0.4f }, npc.Center);

                if (npc.frame.Y >= frameHeight * finalFrame)
                    npc.frame.Y = frameHeight * finalFrame;
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 origin = npc.frame.Size() * 0.5f;

            Vector2 center = npc.Center - Main.screenPosition;

            ExoMechAIUtilities.DrawFinalPhaseGlow(npc, texture, center, npc.frame, origin);
            Main.spriteBatch.Draw(texture, center, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Thanatos/ThanatosHeadGlow").Value;
            Main.spriteBatch.Draw(texture, center, npc.frame, Color.White * npc.Opacity, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            return false;
        }
        #endregion Frames and Drawcode

        #region Death Effects
        public override bool CheckDead(NPC npc) => ExoMechManagement.HandleDeathEffects(npc);
        #endregion Death Effects
    }
}
