using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Items.Weapons.Typeless;
using CalamityMod.NPCs;
using CalamityMod.NPCs.CeaselessVoid;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

using CeaselessVoidBoss = CalamityMod.NPCs.CeaselessVoid.CeaselessVoid;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class CeaselessVoidBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<CeaselessVoidBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCSetDefaults | NPCOverrideContext.NPCPreDraw;
        
        #region Enumerations
        public enum CeaselessVoidAttackType
        {
            DarkEnergySwirl,
            HorizontalRealityRendCharge,
            ConvergingEnergyBarrages,
            SlowEnergySpirals,
            BlackHoleSuck
        }
        #endregion

        #region Set Defaults
        public override void SetDefaults(NPC npc)
        {
            npc.npcSlots = 36f;
            npc.width = 100;
            npc.height = 100;
            npc.defense = 0;
            npc.lifeMax = 363000;
            npc.value = Item.buyPrice(0, 35, 0, 0);
            
            if (ModLoader.TryGetMod("CalamityModMusic", out Mod calamityModMusic))
                npc.ModNPC.Music = MusicLoader.GetMusicSlot(calamityModMusic, "Sounds/Music/Void");
            else
                npc.ModNPC.Music = MusicID.Boss3;
            npc.aiStyle = -1;
            npc.ModNPC.AIType = -1;
            npc.knockBackResist = 0f;
            for (int k = 0; k < npc.buffImmune.Length; k++)
                npc.buffImmune[k] = true;

            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.boss = true;
            npc.DeathSound = SoundID.NPCDeath14;
        }
        #endregion Set Defaults

        #region AI

        public const float Phase2LifeRatio = 0.65f;

        public const float Phase3LifeRatio = 0.3f;

        public const float DarkEnergyOffsetRadius = 1120f;

        public override bool PreAI(NPC npc)
        {
            // Reset DR.
            npc.Calamity().DR = 0.2f;

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Set the global whoAmI variable.
            CalamityGlobalNPC.voidBoss = npc.whoAmI;

            if (!target.active || target.dead || !npc.WithinRange(target.Center, 7200f))
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 18f, 0.08f);
                if (!npc.WithinRange(target.Center, 1450f))
                {
                    npc.life = 0;
                    npc.active = false;
                    npc.netUpdate = true;
                }
                return false;
            }

            npc.timeLeft = 3600;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase2 = lifeRatio < Phase2LifeRatio;
            bool phase3 = lifeRatio < Phase3LifeRatio;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float currentPhase = ref npc.ai[2];

            // Do phase transitions.
            if (currentPhase == 0f && phase2)
            {
                currentPhase = 1f;
                SelectNewAttack(npc);
                attackType = (int)CeaselessVoidAttackType.DarkEnergySwirl;
            }
            if (currentPhase == 1f && phase3)
            {
                currentPhase = 2f;
                SelectNewAttack(npc);
                attackType = (int)CeaselessVoidAttackType.BlackHoleSuck;
            }

            // This debuff is not fun.
            if (target.HasBuff(BuffID.VortexDebuff))
                target.ClearBuff(BuffID.VortexDebuff);

            // Reset things.
            npc.damage = 0;
            npc.dontTakeDamage = target.Center.Y < Main.worldSurface * 16f && !BossRushEvent.BossRushActive;
            npc.Calamity().CurrentlyEnraged = npc.dontTakeDamage;
            
            switch ((CeaselessVoidAttackType)(int)attackType)
            {
                case CeaselessVoidAttackType.DarkEnergySwirl:
                    DoBehavior_DarkEnergySwirl(npc, phase2, phase3, target, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.HorizontalRealityRendCharge:
                    DoBehavior_HorizontalRealityRendCharge(npc, phase2, phase3, target, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.ConvergingEnergyBarrages:
                    DoBehavior_ConvergingEnergyBarrages(npc, phase2, phase3, target, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.SlowEnergySpirals:
                    DoBehavior_SlowEnergySpirals(npc, phase2, phase3, target, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.BlackHoleSuck:
                    DoBehavior_BlackHoleSuck(npc, target, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_DarkEnergySwirl(NPC npc, bool phase2, bool phase3, Player target, ref float attackTimer)
        {
            int totalRings = 4;
            int energyCountPerRing = 7;
            int hoverRedirectDelay = 300;
            int darkEnergyID = ModContent.NPCType<DarkEnergy>();

            if (phase2)
                energyCountPerRing += 2;
            if (phase3)
            {
                energyCountPerRing++;
                totalRings++;
            }

            ref float hasCreatedDarkEnergy = ref npc.Infernum().ExtraAI[0];
            ref float hoverOffsetAngle = ref npc.Infernum().ExtraAI[1];

            // Initialize by creating the dark energy ring.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasCreatedDarkEnergy == 0f)
            {
                for (int i = 0; i < totalRings; i++)
                {
                    float spinMovementSpeed = MathHelper.Lerp(1f, 2.3f, i / (float)(totalRings - 1f));
                    for (int j = 0; j < energyCountPerRing; j++)
                    {
                        float offsetAngle = MathHelper.TwoPi * j / energyCountPerRing;
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, darkEnergyID, npc.whoAmI, offsetAngle, spinMovementSpeed);
                    }
                }
                hasCreatedDarkEnergy = 1f;
            }

            // Redirect to a different offset after a sufficient amount of time has passed.
            if (attackTimer >= hoverRedirectDelay)
            {
                hoverOffsetAngle += MathHelper.PiOver4;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            // Disable damage.
            npc.dontTakeDamage = true;

            // Fly towards the hover destination.
            float flySpeedInterpolant = Utils.GetLerpValue(-24f, 105f, attackTimer, true);
            Vector2 hoverDestination = target.Center - Vector2.UnitY.RotatedBy(hoverOffsetAngle) * 250f;
            npc.velocity = Vector2.Zero.MoveTowards(hoverDestination - npc.Center, flySpeedInterpolant * 12f);

            // Calculate the life ratio of all dark energy combined.
            // If it is sufficiently low then all remaining dark energy fades away and CV goes to the next attack.
            int darkEnergyTotalLife = 0;
            int darkEnergyTotalMaxLife = 0;
            List<NPC> darkEnergies = new();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == darkEnergyID)
                {
                    darkEnergyTotalLife += Main.npc[i].life;
                    darkEnergyTotalMaxLife = Main.npc[i].lifeMax;
                    darkEnergies.Add(Main.npc[i]);
                }
            }
            darkEnergyTotalMaxLife *= totalRings * energyCountPerRing;

            float darkEnergyLifeRatio = darkEnergyTotalLife / (float)darkEnergyTotalMaxLife;
            if (darkEnergyTotalMaxLife <= 0)
                darkEnergyLifeRatio = 0f;

            if (darkEnergyLifeRatio <= 0.3f)
            {
                foreach (NPC darkEnergy in darkEnergies)
                {
                    if (darkEnergy.Infernum().ExtraAI[1] == 0f)
                    {
                        darkEnergy.Infernum().ExtraAI[1] = 1f;
                        darkEnergy.netUpdate = true;
                    }
                }
                
                SelectNewAttack(npc);
            }
        }

        public static void DoBehavior_HorizontalRealityRendCharge(NPC npc, bool phase2, bool phase3, Player target, ref float attackTimer)
        {
            int chargeTime = 42;
            int repositionTime = 210;
            int chargeCount = 3;
            float hoverOffset = 640f;
            float chargeDistance = hoverOffset + 975f;
            if (phase2)
            {
                chargeTime -= 6;
                repositionTime -= 20;
            }
            if (phase3)
            {
                chargeTime -= 8;
                chargeCount++;
            }

            float chargeSpeed = chargeDistance / chargeTime;
            ref float tearProjectileIndex = ref npc.Infernum().ExtraAI[0];
            ref float attackState = ref npc.Infernum().ExtraAI[1];
            ref float chargeDirection = ref npc.Infernum().ExtraAI[2];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[3];

            switch ((int)attackState)
            {
                // Get into position for the horizontal charge.
                case 0:
                    Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * hoverOffset;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 23f, 0.9f);

                    // Begin the charge if either enough time has passed or within sufficient range of the hover destination.
                    if ((attackTimer >= repositionTime || npc.WithinRange(hoverDestination, 85f)) && attackTimer >= 30f)
                    {
                        attackTimer = 0f;
                        attackState = 1f;
                        chargeDirection = Math.Sign(target.Center.X - npc.Center.X);
                        npc.velocity.Y *= 0.372f;
                        npc.netUpdate = true;

                        // Create the reality tear.
                        SoundEngine.PlaySound(YanmeisKnife.HitSound, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            tearProjectileIndex = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<RealityTear>(), 0, 0f);
                    }
                    break;

                // Do the charge.
                case 1:
                    npc.damage = npc.defDamage;
                    npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitX * chargeDirection * chargeSpeed, 0.1f);
                    if (attackTimer >= chargeTime)
                    {
                        attackState = 0f;
                        attackTimer = 0f;
                        tearProjectileIndex = -1f;
                        chargeCounter++;
                        npc.netUpdate = true;

                        if (chargeCounter >= chargeCount)
                            SelectNewAttack(npc);
                    }
                    break;
            }
        }

        public static void DoBehavior_ConvergingEnergyBarrages(NPC npc, bool phase2, bool phase3, Player target, ref float attackTimer)
        {
            int hoverTime = 20;
            int barrageBurstCount = 4;
            int barrageTelegraphTime = 20;
            int barrageShootRate = 32;
            int barrageCount = 13;
            int attackTransitionDelay = 40;
            float maxShootOffsetAngle = 1.26f;
            float initialBarrageSpeed = 10.5f;
            if (phase2)
                initialBarrageSpeed += 1.8f;
            if (phase3)
            {
                initialBarrageSpeed += 2f;
                barrageShootRate -= 4;
                barrageTelegraphTime -= 4;
            }

            ref float hoverOffsetAngle = ref npc.Infernum().ExtraAI[0];
            ref float playerShootDirection = ref npc.Infernum().ExtraAI[1];
            ref float barrageBurstCounter = ref npc.Infernum().ExtraAI[2];
            if (barrageBurstCounter == 0f)
                hoverTime += 120;

            // Hover before firing.
            if (attackTimer < hoverTime)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY.RotatedBy(hoverOffsetAngle) * 540f;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 25f, 1.9f);
                if (npc.WithinRange(hoverDestination, 100f))
                    npc.velocity *= 0.85f;
            }
            else
                npc.velocity *= 0.9f;

            // Prepare dust line telegraphs.
            if (attackTimer == hoverTime + barrageShootRate - barrageTelegraphTime)
            {
                SoundEngine.PlaySound(SoundID.Item8, npc.Center);

                playerShootDirection = npc.AngleTo(target.Center);
                for (int i = 0; i < barrageCount; i++)
                {
                    float offsetAngle = MathHelper.Lerp(-maxShootOffsetAngle, maxShootOffsetAngle, i / (float)(barrageCount - 1f));

                    for (int frames = 8; frames < 75; frames += 4)
                    {
                        Vector2 linePosition = ConvergingCelestialBarrage.SimulateMotion(npc.Center, (offsetAngle + playerShootDirection).ToRotationVector2() * initialBarrageSpeed, playerShootDirection, frames);
                        Dust magic = Dust.NewDustPerfect(linePosition, 267, -Vector2.UnitY);
                        magic.fadeIn = 0.4f;
                        magic.scale = 1.1f;
                        magic.color = Color.Lerp(Color.Cyan, Color.Fuchsia, frames / 75f);
                        magic.noGravity = true;
                    }
                }
                npc.netUpdate = true;
            }

            // Shoot.
            if (attackTimer == hoverTime + barrageShootRate)
            {
                SoundEngine.PlaySound(SoundID.Item28, npc.Center);
                for (int i = 0; i < barrageCount; i++)
                {
                    float offsetAngle = MathHelper.Lerp(-maxShootOffsetAngle, maxShootOffsetAngle, i / (float)(barrageCount - 1f));
                    Vector2 shootVelocity = (offsetAngle + playerShootDirection).ToRotationVector2() * initialBarrageSpeed;
                    int barrage = Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<ConvergingCelestialBarrage>(), 250, 0f);
                    if (Main.projectile.IndexInRange(barrage))
                        Main.projectile[barrage].ai[1] = playerShootDirection;
                }
            }

            if (attackTimer >= hoverTime + barrageShootRate + attackTransitionDelay)
            {
                attackTimer = 0f;
                hoverOffsetAngle += MathHelper.TwoPi / barrageBurstCount;
                barrageBurstCounter++;
                if (barrageBurstCounter >= barrageBurstCount)
                    SelectNewAttack(npc);

                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_SlowEnergySpirals(NPC npc, bool phase2, bool phase3, Player target, ref float attackTimer)
        {
            int shootDelay = 96;
            int burstShootRate = 36;
            int laserBurstCount = 12;
            if (phase2)
                burstShootRate -= 6;
            if (phase3)
                laserBurstCount += 4;

            ref float moveTowardsTarget = ref npc.Infernum().ExtraAI[0];

            // Redirect quickly towards the target if necessary.
            if (moveTowardsTarget == 1f)
            {
                attackTimer--;
                if (npc.WithinRange(target.Center, 360f))
                {
                    npc.velocity *= 0.8f;
                    npc.damage = 0;
                    if (npc.velocity.Length() < 1f)
                    {
                        npc.velocity = Vector2.Zero;
                        moveTowardsTarget = 0f;
                        npc.netUpdate = true;
                    }
                    return;
                }

                CalamityUtils.SmoothMovement(npc, 0f, target.Center - Vector2.UnitY * 200f - npc.Center, 40f, 0.75f, true);
                return;
            }

            // Make Ceaseless Void move quickly towards the target if they go too far away.
            if (!npc.WithinRange(target.Center, 800f))
            {
                moveTowardsTarget = 1f;
                npc.netUpdate = true;
                return;
            }

            // Slow down.
            npc.velocity *= 0.9f;

            // Release lasers.
            if (attackTimer % burstShootRate == burstShootRate - 1f && attackTimer >= shootDelay && attackTimer < 400f)
            {
                SoundEngine.PlaySound(SoundID.Item28, npc.Center);
                float shootOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                for (int i = 0; i < laserBurstCount; i++)
                {
                    for (int j = -1; j <= 1; j += 2)
                    {
                        Vector2 shootVelocity = (MathHelper.TwoPi * i / laserBurstCount + shootOffsetAngle).ToRotationVector2() * 7.5f;
                        Vector2 laserSpawnPosition = npc.Center + shootVelocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * j * 8f;
                        int laser = Utilities.NewProjectileBetter(laserSpawnPosition, shootVelocity, ModContent.ProjectileType<SpiralEnergyLaser>(), 250, 0f);
                        if (Main.projectile.IndexInRange(laser))
                            Main.projectile[laser].localAI[1] = j * 0.5f;
                    }
                }
            }

            if (attackTimer >= 480f)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_BlackHoleSuck(NPC npc, Player target, ref float attackTimer)
        {
            ref float moveTowardsTarget = ref npc.Infernum().ExtraAI[0];
            ref float hasCreatedBlackHole = ref npc.Infernum().ExtraAI[1];

            // Create the black hole on the first frame.
            if (hasCreatedBlackHole == 0f)
            {
                Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<AllConsumingBlackHole>(), 360, 0f);
                hasCreatedBlackHole = 1f;
            }

            // Disable damage.
            npc.dontTakeDamage = true;

            // Redirect quickly towards the target if necessary.
            if (moveTowardsTarget == 1f)
            {
                if (npc.WithinRange(target.Center, 360f))
                {
                    npc.velocity *= 0.8f;
                    npc.damage = 0;
                    if (npc.velocity.Length() < 1f)
                    {
                        npc.velocity = Vector2.Zero;
                        moveTowardsTarget = 0f;
                        npc.netUpdate = true;
                    }
                    return;
                }

                CalamityUtils.SmoothMovement(npc, 0f, target.Center - Vector2.UnitY * 200f - npc.Center, 40f, 0.75f, true);
                return;
            }

            // Make Ceaseless Void move quickly towards the target if they go too far away.
            if (!npc.WithinRange(target.Center, 1200f))
            {
                moveTowardsTarget = 1f;
                npc.netUpdate = true;
                return;
            }

            // Slow down.
            npc.velocity *= 0.9f;

            if (attackTimer >= 600f)
                SelectNewAttack(npc);
        }

        public static void SelectNewAttack(NPC npc)
        {
            // Select a new target.
            npc.TargetClosest();
            
            List<CeaselessVoidAttackType> possibleAttacks = new()
            {
                CeaselessVoidAttackType.HorizontalRealityRendCharge,
                CeaselessVoidAttackType.ConvergingEnergyBarrages,
                CeaselessVoidAttackType.SlowEnergySpirals
            };
            
            if (possibleAttacks.Count >= 2)
                possibleAttacks.Remove((CeaselessVoidAttackType)(int)npc.ai[0]);

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.ai[0] = (int)Main.rand.Next(possibleAttacks);
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/CeaselessVoid/CeaselessVoidGlow").Value;
            Texture2D voidTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/CeaselessVoid/CeaselessVoidVoidStuff").Value;
            Main.spriteBatch.Draw(texture, npc.Center - Main.screenPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0f);
            Main.spriteBatch.Draw(glowmask, npc.Center - Main.screenPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0f);

            Main.spriteBatch.EnterShaderRegion();

            DrawData drawData = new(voidTexture, npc.Center - Main.screenPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0);
            GameShaders.Misc["Infernum:RealityTear2"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/Stars"));
            GameShaders.Misc["Infernum:RealityTear2"].Apply(drawData);
            drawData.Draw(Main.spriteBatch);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        #endregion Drawing
    }
}
