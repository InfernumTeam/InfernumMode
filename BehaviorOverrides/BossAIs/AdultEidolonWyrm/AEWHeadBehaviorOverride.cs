using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.AdultEidolonWyrm;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AEWHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum AEWAttackType
        {
            AbyssalCrash,
            HadalSpirits,
            PsychicBlasts,
            UndynesTail,
            StormCharge,
            ImpactTail,
            LightningCage
        }

        public static List<AEWAttackType[]> Phase1AttackCycles = new()
        {
            new [] { AEWAttackType.UndynesTail, AEWAttackType.AbyssalCrash, AEWAttackType.UndynesTail, AEWAttackType.PsychicBlasts, AEWAttackType.HadalSpirits },
            new [] { AEWAttackType.UndynesTail, AEWAttackType.AbyssalCrash, AEWAttackType.PsychicBlasts, AEWAttackType.HadalSpirits, AEWAttackType.UndynesTail },
            new [] { AEWAttackType.PsychicBlasts, AEWAttackType.HadalSpirits, AEWAttackType.PsychicBlasts, AEWAttackType.UndynesTail, AEWAttackType.AbyssalCrash },
            new [] { AEWAttackType.PsychicBlasts, AEWAttackType.HadalSpirits, AEWAttackType.UndynesTail, AEWAttackType.AbyssalCrash, AEWAttackType.PsychicBlasts },
        };

        public static List<AEWAttackType[]> Phase2AttackCycles = new()
        {
            new [] { AEWAttackType.StormCharge, AEWAttackType.ImpactTail },
        };

        public static List<AEWAttackType[]> CurrentAttackCycles => Phase2AttackCycles;

        public const int ShieldHP = 102000;
        public const float Phase2LifeRatio = 0.8f;

        public override int NPCOverrideType => InfernumMode.CalamityMod.Find<ModNPC>("EidolonWyrmHeadHuge").Type;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public static Color CalculateEyeColor(NPC npc)
        {
            float hue = npc.Infernum().ExtraAI[5] / CurrentAttackCycles.Count % 1f;
            Color c = Main.hslToRgb(hue, 1f, 0.55f);
            return c;
        }

        public override bool PreAI(NPC npc)
        {
            // Use the default AI if SCal and Draedon are not both dead.
            if (!DownedBossSystem.downedExoMechs || !DownedBossSystem.downedSCal)
                return true;

            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // Disappear if the target is gone.
            if (!target.active || target.dead)
            {
                npc.TargetClosest();
                target = Main.player[npc.target];
                if (!target.active || target.dead)
                {
                    npc.active = false;
                    return false;
                }
            }

            // Set the whoAmI variable.
            CalamityGlobalNPC.adultEidolonWyrmHead = npc.whoAmI;

            // Do enrage checks.
            bool enraged = ArenaSpawnAndEnrageCheck(npc, target);
            npc.Calamity().CurrentlyEnraged = enraged;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            float generalDamageFactor = enraged ? 40f : 1f;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float hasInitialized = ref npc.localAI[0];
            ref float etherealnessFactor = ref npc.localAI[1];

            // Do initializations.
            if (hasInitialized == 0f)
            {
                npc.Opacity = 1f;

                int Previous = npc.whoAmI;
                for (int i = 0; i < 41; i++)
                {
                    int lol;
                    if (i is >= 0 and < 40)
                    {
                        if (i % 2 == 0)
                            lol = NPC.NewNPC(new InfernumSource(), (int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), InfernumMode.CalamityMod.Find<ModNPC>("EidolonWyrmBodyHuge").Type, npc.whoAmI + 1);
                        else
                            lol = NPC.NewNPC(new InfernumSource(), (int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), InfernumMode.CalamityMod.Find<ModNPC>("EidolonWyrmBodyAltHuge").Type, npc.whoAmI + 1);
                    }
                    else
                        lol = NPC.NewNPC(new InfernumSource(), (int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), InfernumMode.CalamityMod.Find<ModNPC>("EidolonWyrmTailHuge").Type, npc.whoAmI + 1);

                    Main.npc[lol].realLife = npc.whoAmI;
                    Main.npc[lol].ai[2] = npc.whoAmI;
                    Main.npc[lol].ai[1] = Previous;

                    if (i > 0)
                        Main.npc[Previous].ai[0] = lol;
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, lol, 0f, 0f, 0f, 0);
                    Previous = lol;
                    Main.npc[Previous].ai[3] = i / 2;
                }
                hasInitialized = 1f;
            }

            // Make the etherealness effect naturally dissipate.
            etherealnessFactor = MathHelper.Clamp(etherealnessFactor - 0.025f, 0f, 1f);

            // Reset damage and other things.
            npc.damage = (int)(npc.defDamage * generalDamageFactor);
            npc.dontTakeDamage = false;

            switch ((AEWAttackType)(int)attackType)
            {
                case AEWAttackType.AbyssalCrash:
                    DoBehavior_AbyssalCrash(npc, target, generalDamageFactor, ref attackTimer);
                    break;
                case AEWAttackType.HadalSpirits:
                    DoBehavior_HadalSpirits(npc, target, lifeRatio, generalDamageFactor, ref etherealnessFactor, ref attackTimer);
                    break;
                case AEWAttackType.PsychicBlasts:
                    DoBehavior_PsychicBlasts(npc, target, lifeRatio, generalDamageFactor, ref etherealnessFactor, ref attackTimer);
                    break;
                case AEWAttackType.UndynesTail:
                    DoBehavior_UndynesTail(npc, target, lifeRatio, generalDamageFactor, ref etherealnessFactor, ref attackTimer);
                    break;
                case AEWAttackType.StormCharge:
                    DoBehavior_StormCharge(npc, target, lifeRatio, generalDamageFactor, ref etherealnessFactor, ref attackTimer);
                    break;
                case AEWAttackType.ImpactTail:
                    DoBehavior_ImpactTail(npc, target, lifeRatio, generalDamageFactor, ref etherealnessFactor, ref attackTimer);
                    break;
                case AEWAttackType.LightningCage:
                    DoBehavior_LightningCage(npc, target, lifeRatio, generalDamageFactor, ref etherealnessFactor, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        public static void DoBehavior_AbyssalCrash(NPC npc, Player target, float generalDamageFactor, ref float attackTimer)
        {
            // Define attack variables.
            int waterShootRate = 45;
            int waterPerBurst = 3;
            int attackChangeDelay = 90;
            int attackTime = 480;

            // Periodically release streams of abyssal water.
            bool readyToShootJet = attackTimer % waterShootRate == waterShootRate - 1f && attackTimer < attackTime - attackChangeDelay;
            if (!npc.WithinRange(target.Center, 240f) && readyToShootJet)
            {
                SoundEngine.PlaySound(SoundID.Item73, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int jetDamage = (int)(generalDamageFactor * 640f);
                    for (int i = 0; i < waterPerBurst; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-0.17f, 0.17f, i / (float)(waterPerBurst - 1f));
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * 10f;
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 3f, shootVelocity, ModContent.ProjectileType<AbyssalWaterJet>(), jetDamage, 0f);
                    }
                }
            }

            // Do movement.
            DoDefaultSwimMovement(npc, target);

            // Decide rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (attackTimer > attackTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_HadalSpirits(NPC npc, Player target, float lifeRatio, float generalDamageFactor, ref float etherealnessFactor, ref float attackTimer)
        {
            int attackShootDelay = 60;
            int attackShootRate = (int)MathHelper.Lerp(15f, 10f, 1f - lifeRatio);
            int homingSpiritShootRate = attackShootRate * 4;
            int attackChangeDelay = 90;
            int attackTime = 600;
            int psychicBlastCircleShootRate = 90;
            ref float spiritSpawnOffsetAngle = ref npc.Infernum().ExtraAI[0];

            // Initialize the spawn offset angle.
            if (spiritSpawnOffsetAngle == 0f)
            {
                spiritSpawnOffsetAngle = Main.rand.NextFloatDirection() * 0.36f;
                npc.netUpdate = true;
            }

            // Reset damage to 0.
            npc.damage = 0;

            // Do movement.
            DoDefaultSwimMovement(npc, target, 0.625f);

            // Decide rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Decide the etherealness factor.
            etherealnessFactor = Utils.GetLerpValue(0f, 60f, attackTimer, true) * Utils.GetLerpValue(attackTime, attackTime - attackChangeDelay, attackTimer, true);

            if (attackTimer > attackTime)
                SelectNextAttack(npc);

            if (attackTimer > attackTime - attackChangeDelay)
                return;

            // Periodically release blasts of psychic energy.
            if (attackTimer % psychicBlastCircleShootRate == psychicBlastCircleShootRate - 1f)
            {
                // Play a bolt sound.
                SoundEngine.PlaySound(SoundID.Item75, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int blastDamage = (int)(generalDamageFactor * 640f);
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 blastShootVelocity = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * 15f;
                        Projectile.NewProjectile(new InfernumSource(), npc.Center, blastShootVelocity, ModContent.ProjectileType<PsionicRay>(), blastDamage, 0f);
                    }
                }
            }

            // Release souls below the target.
            int spiritDamage = (int)(generalDamageFactor * 640f);
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > attackShootDelay && attackTimer % attackShootRate == attackShootRate - 1f)
            {
                Vector2 spiritSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 800f, 1080f);
                Vector2 spiritVelocity = -Vector2.UnitY.RotatedBy(spiritSpawnOffsetAngle) * 17f;
                Utilities.NewProjectileBetter(spiritSpawnPosition, spiritVelocity, ModContent.ProjectileType<HadalSpirit>(), spiritDamage, 0f);
            }

            // Release homing souls around the target.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > attackShootDelay && attackTimer % homingSpiritShootRate == homingSpiritShootRate - 1f)
            {
                Vector2 spiritSpawnPosition = target.Center + Main.rand.NextVector2Circular(1200f, 1200f);
                Vector2 spiritVelocity = (target.Center - spiritSpawnPosition).SafeNormalize(Vector2.UnitY) * 12f;
                Utilities.NewProjectileBetter(spiritSpawnPosition, spiritVelocity, ModContent.ProjectileType<HomingHadalSpirit>(), spiritDamage, 0f);
            }
        }

        public static void DoBehavior_PsychicBlasts(NPC npc, Player target, float lifeRatio, float generalDamageFactor, ref float etherealnessFactor, ref float attackTimer)
        {
            int attackShootDelay = 60;
            int orbCreationRate = (int)MathHelper.Lerp(16f, 10f, 1f - lifeRatio);
            int attackChangeDelay = 90;
            int attackTime = 900;

            // Reset damage to 0.
            npc.damage = 0;

            // Do movement.
            DoDefaultSwimMovement(npc, target, 0.7f);

            // Decide rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Decide the etherealness factor.
            etherealnessFactor = Utils.GetLerpValue(0f, 60f, attackTimer, true) * Utils.GetLerpValue(attackTime, attackTime - attackChangeDelay, attackTimer, true);

            if (attackTimer > attackTime)
                SelectNextAttack(npc);

            if (attackTimer > attackTime - attackChangeDelay)
                return;

            // Release psychic fields around the head.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > attackShootDelay && attackTimer % orbCreationRate == orbCreationRate - 1f)
            {
                int fieldBoltDamage = (int)(generalDamageFactor * 640f);
                Vector2 fieldSpawnPosition = npc.Center + Main.rand.NextVector2Circular(660f, 150f).RotatedBy(npc.rotation);
                Utilities.NewProjectileBetter(fieldSpawnPosition, Vector2.Zero, ModContent.ProjectileType<PsychicEnergyField>(), fieldBoltDamage, 0f);
            }
        }

        public static void DoBehavior_UndynesTail(NPC npc, Player target, float lifeRatio, float generalDamageFactor, ref float etherealnessFactor, ref float attackTimer)
        {
            int totalCharges = 5;
            int redirectTime = 45;
            int chargeTime = 48;
            float chargeSpeed = MathHelper.Lerp(48f, 65f, 1f - lifeRatio);

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float chargeDirection = ref npc.Infernum().ExtraAI[1];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[2];

            // Decide the etherealness factor.
            etherealnessFactor = MathHelper.Lerp(etherealnessFactor, npc.Opacity, MathHelper.Lerp(0.25f, 0.08f, npc.Opacity));

            // Decide rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            switch ((int)attackSubstate)
            {
                // Line up in preparation for the charge.
                case 0:
                    Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 500f, -350f);
                    Vector2 idealHoverVelocity = npc.SafeDirectionTo(hoverDestination) * 45f;
                    npc.velocity = npc.velocity.RotateTowards(idealHoverVelocity.ToRotation(), 0.045f).MoveTowards(idealHoverVelocity, 4f);

                    // Fade out.
                    npc.Opacity = Utils.GetLerpValue(redirectTime * 0.5f, redirectTime * 0.5f - 12f, attackTimer, true);

                    // Disable conct damage.
                    npc.damage = 0;

                    // Determine whether to be invulnerable.
                    npc.dontTakeDamage = npc.Opacity < 0.65f;

                    // Begin the charge.
                    if (attackTimer > redirectTime * 0.5f && (npc.WithinRange(hoverDestination, 60f) || attackTimer > redirectTime))
                    {
                        chargeDirection = npc.AngleTo(target.Center + target.velocity * 15f);
                        attackSubstate = 1f;
                        attackTimer = 0f;
                        npc.velocity = npc.velocity.AngleDirectionLerp(chargeDirection.ToRotationVector2(), 0.33f).SafeNormalize(Vector2.Zero) * chargeSpeed * 0.6f;
                        npc.netUpdate = true;
                    }
                    break;

                // Move into the charge.
                case 1:
                    float newSpeed = MathHelper.Lerp(npc.velocity.Length(), chargeSpeed, 0.18f);
                    npc.velocity = npc.velocity.RotateTowards(chargeDirection, MathHelper.Pi / 3f, true) * newSpeed;

                    // Fade in.
                    npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.05f, 0f, 1f);

                    // Release water spears from the tail of the wyrm.
                    int tail = NPC.FindFirstNPC(ModContent.NPCType<EidolonWyrmTailHuge>());
                    if (tail != -1 && !Main.npc[tail].WithinRange(target.Center, 200f) && attackTimer % 6f == 5f)
                    {
                        SoundEngine.PlaySound(SoundID.Item66, Main.npc[tail].Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int spearDamage = (int)(generalDamageFactor * 640f);
                            Vector2 spearShootVelocity = Main.rand.NextVector2CircularEdge(27f, 27f);
                            Utilities.NewProjectileBetter(Main.npc[tail].Center, spearShootVelocity, ModContent.ProjectileType<HomingWaterSpear>(), spearDamage, 0f);
                        }
                    }
                    
                    if (attackTimer > chargeTime)
                    {
                        chargeDirection = 0f;
                        attackSubstate = 0f;
                        attackTimer = 0f;
                        chargeCounter++;
                        npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 24f, 0.4f);
                        npc.netUpdate = true;

                        if (chargeCounter >= totalCharges)
                            SelectNextAttack(npc);
                    }
                    break;
            }
        }

        public static void DoBehavior_StormCharge(NPC npc, Player target, float lifeRatio, float generalDamageFactor, ref float etherealnessFactor, ref float attackTimer)
        {
            int teleportFadeTime = 32;
            int telegraphTime = 50;
            int chargeTime = 54;
            int chargeCount = 4;
            int lighningCloudCreationRate = 4;
            float chargeSpeed = 72f;
            float chargeOffset = MathHelper.Lerp(1750f, 1400f, 1f - lifeRatio);
            ref float aimDirection = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];

            // Fade out prior to teleporting.
            if (attackTimer <= teleportFadeTime)
            {
                npc.Opacity = MathHelper.Lerp(1f, 0f, attackTimer / teleportFadeTime);
                npc.dontTakeDamage = npc.Opacity < 0.65f;
                npc.damage = 0;
                DoDefaultSwimMovement(npc, target, MathHelper.Lerp(0.45f, 1f, npc.Opacity));
            }

            // Decide a position to teleport to and create a telegraph.
            if (attackTimer == teleportFadeTime)
            {
                SoundEngine.PlaySound(SoundID.Item105, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 offsetDirection = Main.rand.NextVector2Unit();
                    offsetDirection = (offsetDirection * new Vector2(1f, 0.13f)).SafeNormalize(Vector2.UnitY);

                    Vector2 teleportPosition = target.Center + offsetDirection * chargeOffset;
                    Vector2 telegraphDirection = (target.Center - teleportPosition).SafeNormalize(Vector2.UnitY);
                    npc.Center = teleportPosition;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].realLife == npc.whoAmI)
                        {
                            Main.npc[i].Center = npc.Center - npc.SafeDirectionTo(target.Center) * i * 0.1f;
                            Main.npc[i].netUpdate = true;
                        }
                    }

                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;

                    int telegraph = Utilities.NewProjectileBetter(teleportPosition, telegraphDirection, ModContent.ProjectileType<StormChargeTelegraph>(), 0, 0f);
                    if (Main.projectile.IndexInRange(telegraph))
                        Main.projectile[telegraph].ai[1] = telegraphTime;
                }
            }

            // Remain invisible while the telegraph is being made.
            if (attackTimer >= teleportFadeTime && attackTimer < teleportFadeTime + telegraphTime)
            {
                npc.damage = 0;
                npc.dontTakeDamage = true;
                npc.Opacity = 0f;
            }

            // Charge.
            if (attackTimer == teleportFadeTime + telegraphTime)
            {
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.Instance, "Sounds/Custom/WyrmElectricCharge"), target.Center);
                npc.velocity = aimDirection.ToRotationVector2() * chargeSpeed;
                npc.Opacity = 1f;
                npc.netUpdate = true;
            }

            // Apply post-charge effects.
            if (attackTimer > teleportFadeTime + telegraphTime)
            {
                // Create electric sparks.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 2f == 1f)
                {
                    Vector2 sparkOffsetDirection = Main.rand.NextVector2Unit();
                    Vector2 sparkSpawnPosition = npc.Center + sparkOffsetDirection * 64f;
                    Vector2 sparkVelocity = sparkOffsetDirection * 7f;
                    Utilities.NewProjectileBetter(sparkSpawnPosition, sparkVelocity, ModContent.ProjectileType<ElectricSparkParticle>(), 0, 0f);
                }

                // Create lightning clouds that release lightning from the sky.
                bool readyToCreateCloud = npc.WithinRange(target.Center, 1100f) && attackTimer % lighningCloudCreationRate == lighningCloudCreationRate - 1f;
                if (Main.netMode != NetmodeID.MultiplayerClient && readyToCreateCloud)
                    Utilities.NewProjectileBetter(npc.Center + Main.rand.NextVector2Circular(10f, 10f), Vector2.Zero, ModContent.ProjectileType<StormLightningCloud>(), 0, 0f);

                // Become ethereal-looking.
                etherealnessFactor = 1f;
            }

            // Define rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Go to the next attack.
            if (attackTimer >= teleportFadeTime + telegraphTime + chargeTime)
            {
                attackTimer = 0f;
                chargeCounter++;
                if (chargeCounter >= chargeCount)
                    SelectNextAttack(npc);
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_ImpactTail(NPC npc, Player target, float lifeRatio, float generalDamageFactor, ref float etherealnessFactor, ref float attackTimer)
        {
            int timeNeededToDestroyShield = 630;
            int postShieldBreakTransitionDelay = 125;
            int pissedOffChargeDelay = 20;
            int shieldExplosionDelay = 54;
            int pissedOffChargeTime = 50;
            NPC tail = Main.npc[NPC.FindFirstNPC(InfernumMode.CalamityMod.Find<ModNPC>("EidolonWyrmTailHuge").Type)];

            float pissedOffChargeSpeed = MathHelper.Lerp(54f, 66f, 1f - lifeRatio);
            ref float totalShieldDamage = ref npc.Infernum().ExtraAI[0];
            ref float shieldHasExploded = ref npc.Infernum().ExtraAI[1];
            ref float shieldLifetime = ref npc.Infernum().ExtraAI[2];
            ref float hasRoaredYet = ref npc.Infernum().ExtraAI[3];
            ref float pissedOff = ref npc.Infernum().ExtraAI[4];
            shieldLifetime = timeNeededToDestroyShield;

            // Circle around the target.
            if (hasRoaredYet == 0f)
            {
                npc.damage = 0;
                Vector2 hoverDestination = target.Center + (MathHelper.TwoPi * attackTimer / 105f).ToRotationVector2() * new Vector2(3000f, 1750f);
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 37f;
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.03f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.03f).MoveTowards(idealVelocity, 3f);
            }
            
            // Move slowly near the target if the shield was destroyed.
            else if (pissedOff == 0f)
            {
                if (attackTimer < timeNeededToDestroyShield + postShieldBreakTransitionDelay)
                    DoDefaultSwimMovement(npc, target, 0.32f);
                else
                    SelectNextAttack(npc);
            }
            else
            {
                etherealnessFactor = Utils.GetLerpValue(0f, pissedOffChargeDelay, attackTimer - timeNeededToDestroyShield, true);
                if (attackTimer < timeNeededToDestroyShield + pissedOffChargeDelay)
                {
                    Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * 27f;

                    // Approach the ideal velocity magnitude.
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), idealVelocity.Length(), 0.08f);

                    // Approach the ideal velocity direction.
                    npc.velocity = npc.velocity.MoveTowards(idealVelocity, 3.5f).SafeNormalize(Vector2.UnitY) * npc.velocity.Length();
                }

                // Charge at the target.
                if (attackTimer == timeNeededToDestroyShield + pissedOffChargeDelay)
                {
                    Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center) * pissedOffChargeSpeed;
                    npc.velocity = Vector2.Lerp(npc.velocity, chargeVelocity, 0.9f);
                    npc.netUpdate = true;
                }

                // Make the shield explode.
                if (attackTimer == timeNeededToDestroyShield + shieldExplosionDelay)
                {
                    shieldHasExploded = 1f;
                    npc.netUpdate = true;

                    int explosionDamage = (int)(generalDamageFactor * 900f);
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/LargeMechGaussRifle"), tail.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(tail.Center, Vector2.Zero, ModContent.ProjectileType<EidolicExplosion>(), explosionDamage, 0f);
                }

                if (attackTimer == timeNeededToDestroyShield + pissedOffChargeDelay + pissedOffChargeTime)
                    SelectNextAttack(npc);
            }

            bool shieldIsDestroyed = totalShieldDamage >= ShieldHP;

            // Get very angry if the shield was not destroyed in time.
            if (attackTimer >= timeNeededToDestroyShield && !shieldIsDestroyed)
            {
                if (hasRoaredYet == 0f)
                {
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/WyrmScream"), target.Center);
                    hasRoaredYet = 1f;
                    pissedOff = 1f;
                    npc.netUpdate = true;
                }
            }

            // Roar and take damage once the shield is destroyed.
            if (shieldIsDestroyed)
            {
                if (hasRoaredYet == 0f)
                {
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/WyrmScream"), target.Center);

                    // Take damage after the shield is destroyed.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        npc.StrikeNPC(ShieldHP / 2, 0f, 0);

                    hasRoaredYet = 1f;
                    npc.netUpdate = true;
                }
            }

            // Release psychic fields around the tail.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 24f == 23f && attackTimer > 90f && hasRoaredYet == 0f)
            {
                int fieldBoltDamage = (int)(generalDamageFactor * 640f);
                Vector2 fieldSpawnPosition = tail.Center + Main.rand.NextVector2Circular(50f, 50f);
                Utilities.NewProjectileBetter(fieldSpawnPosition, Vector2.Zero, ModContent.ProjectileType<PsychicEnergyField>(), fieldBoltDamage, 0f);
            }
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public static void DoBehavior_LightningCage(NPC npc, Player target, float lifeRatio, float generalDamageFactor, ref float etherealnessFactor, ref float attackTimer)
        {
            int fieldCreationDelay = 15;
            int spinTime = 90;
            int chargePreparationTime = 20;
            int droneSummonCount = 6;
            float chargeSpeed = MathHelper.Lerp(54f, 66f, 1f - lifeRatio);
            float chargeSparkSpeed = MathHelper.Lerp(22.5f, 28f, 1f - lifeRatio);
            ref float hasChargedYet = ref npc.Infernum().ExtraAI[0];
            ref float fieldCenterX = ref npc.Infernum().ExtraAI[1];
            ref float fieldCenterY = ref npc.Infernum().ExtraAI[2];

            // Circle around the target.
            if (hasChargedYet == 0f)
            {
                npc.damage = 0;
                Vector2 hoverDestination = target.Center + (MathHelper.TwoPi * attackTimer / 210f).ToRotationVector2() * 1900f;
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 50f;
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.08f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.08f).MoveTowards(idealVelocity, 8f);
            }

            // Create energy fields.
            if (attackTimer == fieldCreationDelay)
            {
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/EidolonWyrmRoarClose"), target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int spinDirection = Main.rand.NextBool().ToDirectionInt();
                    float angularOffsetPerIncrement = Main.rand.NextFloat();
                    List<int> drones = new();
                    for (int i = 0; i < droneSummonCount; i++)
                    {
                        int drone = NPC.NewNPC(new InfernumSource(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<EnergyFieldLaserProjector>());
                        Main.npc[drone].target = npc.target;
                        drones.Add(drone);
                    }

                    for (int i = 0; i < drones.Count; i++)
                    {
                        Main.npc[drones[i]].ai[0] = -2f;
                        Main.npc[drones[i]].ai[1] = drones[(i + 1) % drones.Count];
                        Main.npc[drones[i]].ai[2] = MathHelper.TwoPi * (i + angularOffsetPerIncrement) / drones.Count;
                        Main.npc[drones[i]].ModNPC<EnergyFieldLaserProjector>().SpinDirection = spinDirection;
                    }
                    fieldCenterX = target.Center.X;
                    fieldCenterY = target.Center.Y;
                    npc.netUpdate = true;
                }
            }

            // Prepare to charge towards the field center.
            if (attackTimer >= spinTime && attackTimer < spinTime + chargePreparationTime)
            {
                hasChargedYet = 1f;
                Vector2 chargeVelocity = npc.SafeDirectionTo(new Vector2(fieldCenterX, fieldCenterY)) * chargeSpeed;
                npc.velocity = Vector2.Lerp(npc.velocity, chargeVelocity, 0.05f).MoveTowards(chargeVelocity, 2f);
                if (attackTimer == spinTime + chargePreparationTime - 1f)
                {
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/WyrmScream"), target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Release sparks at the target.
                        for (int i = 0; i < 12; i++)
                        {
                            int sparkDamage = (int)(generalDamageFactor * 640f);
                            Vector2 sparkVelocity = npc.SafeDirectionTo(target.Center) * chargeSparkSpeed + Main.rand.NextVector2Circular(5f, 5f);
                            Utilities.NewProjectileBetter(npc.Center, sparkVelocity, ModContent.ProjectileType<EidolicSpark>(), sparkDamage, 0f);
                        }
                    }

                    npc.velocity = chargeVelocity;
                    npc.netUpdate = true;
                }
            }

            if (attackTimer >= 200f)
                SelectNextAttack(npc);

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public static void DoDefaultSwimMovement(NPC npc, Player target, float generalSpeedFactor = 1f)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float idealFlySpeed = MathHelper.Lerp(27f, 34f, 1f - lifeRatio) * generalSpeedFactor;
            float flyAcceleration = MathHelper.Lerp(0.03f, 0.0425f, 1f - lifeRatio) * generalSpeedFactor;
            float newSpeed = MathHelper.Lerp(npc.velocity.Length(), idealFlySpeed, 0.08f);
            Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * idealFlySpeed;

            // Fly towards the target.
            if (!npc.WithinRange(target.Center, 240f))
            {
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), flyAcceleration, true) * newSpeed;
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, flyAcceleration * 25f);
            }

            // Accelerate if close to the target.
            else
                npc.velocity = (npc.velocity * 1.025f).ClampMagnitude(10f, 50f);
        }

        public static void SelectNextAttack(NPC npc)
        {
            ref float attackCycleType = ref npc.Infernum().ExtraAI[5];
            ref float attackCycleIndex = ref npc.Infernum().ExtraAI[6];
            List<AEWAttackType[]> attackCycles = CurrentAttackCycles;
            int oldAttackCycle = (int)attackCycleType;

            attackCycleIndex++;

            // Shift the attack cycle once the current one's end has been reached.
            if (attackCycleIndex >= attackCycles[(int)attackCycleType].Length)
            {
                attackCycleIndex = 0f;
                do
                    attackCycleType = Main.rand.Next(attackCycles.Count);
                while (attackCycleType == oldAttackCycle && attackCycles.Count > 1);
            }

            npc.ai[0] = (int)attackCycles[(int)attackCycleType][(int)attackCycleIndex];
            npc.ai[0] = (int)AEWAttackType.LightningCage;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        public static bool ArenaSpawnAndEnrageCheck(NPC npc, Player player)
        {
            ref float enraged01Flag = ref npc.ai[2];
            ref float spawnedArena01Flag = ref npc.ai[3];

            // Create the arena, but not as a multiplayer client.
            // In single player, the arena gets created and never gets synced because it's single player.
            if (spawnedArena01Flag == 0f)
            {
                spawnedArena01Flag = 1f;
                enraged01Flag = 0f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int width = 9600;
                    npc.Infernum().arenaRectangle.X = (int)(player.Center.X - width * 0.5f);
                    npc.Infernum().arenaRectangle.Y = (int)(player.Center.Y - 160000f);
                    npc.Infernum().arenaRectangle.Width = width;
                    npc.Infernum().arenaRectangle.Height = 320000;
                    Vector2 spawnPosition = player.Center + new Vector2(width * 0.5f, 100f);
                    Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<TornadoBorder>(), 10000, 0f, Main.myPlayer, 0f, 0f);
                    spawnPosition = player.Center + new Vector2(width * -0.5f, 100f);
                    Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<TornadoBorder>(), 10000, 0f, Main.myPlayer, 0f, 0f);
                }

                // Force Yharon to send a sync packet so that the arena gets sent immediately
                npc.netUpdate = true;
            }
            // Enrage code doesn't run on frame 1 so that Yharon won't be enraged for 1 frame in multiplayer
            else
            {
                var arena = npc.Infernum().arenaRectangle;
                enraged01Flag = (!player.Hitbox.Intersects(arena)).ToInt();
                if (enraged01Flag == 1f)
                    return true;
            }
            return false;
        }

        public static void DrawSegment(SpriteBatch spriteBatch, Color lightColor, NPC npc)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            float etherealnessFactor = npc.localAI[1];
            if (npc.realLife >= 0)
                etherealnessFactor = Main.npc[npc.realLife].localAI[1];
            float opacity = MathHelper.Lerp(1f, 0.75f, etherealnessFactor) * npc.Opacity;
            Color color = Color.Lerp(lightColor, Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.7f % 1f, 1f, 0.74f), etherealnessFactor * 0.85f);
            color.A = (byte)(int)(255 - etherealnessFactor * 84f);

            if (etherealnessFactor > 0f)
            {
                float etherealOffsetPulse = etherealnessFactor * 16f;

                for (int i = 0; i < 32; i++)
                {
                    Color baseColor = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 1.7f + i / 32f) % 1f, 1f, 0.8f);
                    Color etherealAfterimageColor = Color.Lerp(lightColor, baseColor, etherealnessFactor * 0.85f) * 0.24f;
                    etherealAfterimageColor.A = (byte)(int)(255 - etherealnessFactor * 255f);
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 32f).ToRotationVector2() * etherealOffsetPulse;
                    Main.spriteBatch.Draw(texture, drawPosition + drawOffset, npc.frame, etherealAfterimageColor * opacity, npc.rotation, origin, npc.scale, 0, 0f);
                }
            }

            for (int i = 0; i < (int)Math.Round(1f + etherealnessFactor); i++)
                Main.spriteBatch.Draw(texture, drawPosition, npc.frame, color * opacity, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);

            // Create the shield for the tail in the Impact Tail attack.
            if (npc.type == InfernumMode.CalamityMod.Find<ModNPC>("EidolonWyrmTailHuge").Type)
            {
                NPC head = Main.npc[npc.realLife];
                if (head.ai[0] == (int)AEWAttackType.ImpactTail && head.Infernum().ExtraAI[1] == 0f)
                {
                    Main.spriteBatch.SetBlendState(BlendState.Additive);
                    Vector2 shieldDrawPosition = npc.Center - Main.screenPosition;

                    for (int i = 0; i < 50; i++)
                    {
                        float fadeToWhite = 0f;
                        Texture2D shieldTexture = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/AEWTailShield").Value;
                        if (i < 8)
                        {
                            shieldTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/PlaguebringerGoliath/PlagueNuclearExplosion").Value;
                            fadeToWhite = 1f - i / 7f;
                        }

                        float rotation = (float)Math.Cos(Main.GlobalTimeWrappedHourly * 3f + i) * MathHelper.Pi * (1f - i / 50f);
                        float hpBasedShieldOpacity = 1f - (float)Math.Pow(Utils.GetLerpValue(0f, ShieldHP, head.Infernum().ExtraAI[0], true), 2D);
                        float shieldScaleFactor = Utils.GetLerpValue(0f, 45f, head.ai[1], true);
                        shieldScaleFactor += (float)Math.Cos(Main.GlobalTimeWrappedHourly * -1.9f + i * 2f) * 0.8f;

                        Vector2 shieldScale = Vector2.One / shieldTexture.Size() * MathHelper.Max(npc.frame.Width, npc.frame.Height) * shieldScaleFactor * 0.8f;
                        shieldScale.Y *= 1.2f;

                        Color shieldColor = Color.Lerp(Color.Cyan, Color.White * 0.5f, fadeToWhite) * shieldScaleFactor * (1f - (i + 1f) / 33f) * hpBasedShieldOpacity;
                        Main.spriteBatch.Draw(shieldTexture, shieldDrawPosition, null, shieldColor, rotation, shieldTexture.Size() * 0.5f, shieldScale, 0, 0f);
                    }
                    Main.spriteBatch.ExitShaderRegion();
                }
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            DrawSegment(spriteBatch, lightColor, npc);
            Texture2D eyeTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/AdultEidolonWyrm/EidolonWyrmEyes").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * 1.5f;
            Vector2 origin = eyeTexture.Size() * 0.5f;
            Color eyeColor = npc.GetAlpha(CalculateEyeColor(npc));
            eyeColor.A = 0;
            for (int i = 0; i < 10; i++)
            {
                Vector2 eyeOffset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * 4f;
                Main.spriteBatch.Draw(eyeTexture, drawPosition + eyeOffset, npc.frame, eyeColor * 0.5f, npc.rotation, origin, npc.scale, 0, 0f);
            }

            Main.spriteBatch.Draw(eyeTexture, drawPosition, npc.frame, eyeColor, npc.rotation, origin, npc.scale, 0, 0f);
            return false;
        }
    }
}
