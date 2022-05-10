using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using CeaselessVoidBoss = CalamityMod.NPCs.CeaselessVoid.CeaselessVoid;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class CeaselessVoidBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<CeaselessVoidBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCSetDefaults;

        public const int BulletHellTime = 900;

        #region Enumerations
        public enum CeaselessVoidAttackType
        {
            ReleaseRealityTearPortals,
            DarkMagicCharge,
            DarkEnergyBolts,
            RealityCracks,
            DarkEnergyBursts,
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
            Mod calamityModMusic = ModLoader.GetMod("CalamityModMusic");
            if (calamityModMusic != null)
                npc.modNPC.music = calamityModMusic.GetSoundSlot(SoundType.Music, "Sounds/Music/ScourgeofTheUniverse");
            else
                npc.modNPC.music = MusicID.Boss3;
            if (CalamityWorld.DoGSecondStageCountdown <= 0)
            {
                npc.value = Item.buyPrice(0, 35, 0, 0);
                if (calamityModMusic != null)
                    npc.modNPC.music = calamityModMusic.GetSoundSlot(SoundType.Music, "Sounds/Music/Void");
                else
                    npc.modNPC.music = MusicID.Boss3;
            }
            npc.aiStyle = -1;
            npc.modNPC.aiType = -1;
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

        public override bool PreAI(NPC npc)
        {
            // Reset DR.
            npc.Calamity().DR = 0.2f;

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

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
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float bulletHellTimer = ref npc.ai[2];
            ref float phase = ref npc.ai[3];

            // Reset things.
            npc.damage = 0;
            npc.dontTakeDamage = target.Center.Y < Main.worldSurface * 16f && !BossRushEvent.BossRushActive;
            npc.Calamity().CurrentlyEnraged = npc.dontTakeDamage;

            // Do bullet hells.
            if (bulletHellTimer > 0f && bulletHellTimer < BulletHellTime)
            {
                DoBehavior_DarkEnergyBulletHell(npc, target, lifeRatio, ref bulletHellTimer);

                // Reset individual attack timers.
                attackTimer = 0f;

                // And use the tear portals attack.
                attackType = (int)CeaselessVoidAttackType.ReleaseRealityTearPortals;

                bulletHellTimer++;
                return false;
            }

            // Handle bullet hell triggers.
            if (phase == 0f && lifeRatio < 0.75f)
            {
                phase = 1f;
                bulletHellTimer = 1f;
                npc.netUpdate = true;
            }
            if (phase == 1f && lifeRatio < 0.4f)
            {
                phase = 2f;
                bulletHellTimer = 1f;
                npc.netUpdate = true;
            }
            if (phase == 2f && lifeRatio < 0.1f)
            {
                phase = 3f;
                bulletHellTimer = 1f;
                npc.netUpdate = true;
            }

            switch ((CeaselessVoidAttackType)(int)attackType)
            {
                case CeaselessVoidAttackType.ReleaseRealityTearPortals:
                    DoBehavior_ReleaseTearPortals(npc, target, lifeRatio, (int)phase, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.DarkMagicCharge:
                    DoBehavior_DarkMagicCharge(npc, target, lifeRatio, (int)phase, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.DarkEnergyBolts:
                    DoBehavior_DarkEnergyBolts(npc, target, (int)phase, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.RealityCracks:
                    DoBehavior_RealityCracks(npc, target, (int)phase, ref attackTimer);
                    break;
                case CeaselessVoidAttackType.DarkEnergyBursts:
                    DoBehavior_DarkEnergyBursts(npc, target, (int)phase, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_ReleaseTearPortals(NPC npc, Player target, float lifeRatio, int phase, ref float attackTimer)
        {
            int riftCreationRate = 32 - phase * 4;
            float hoverSpeed = 21f;

            if (lifeRatio < Phase2LifeRatio)
                hoverSpeed += 4.5f;

            Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 450f;

            // Fly to the side of the target.
            if (!npc.WithinRange(hoverDestination, 150f) || npc.WithinRange(target.Center, 200f))
            {
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * hoverSpeed;
                npc.SimpleFlyMovement(idealVelocity, hoverSpeed / 22f);
            }

            // Create rifts around the void.
            if (attackTimer % riftCreationRate == riftCreationRate - 1f && attackTimer < 300f)
            {
                Main.PlaySound(SoundID.Item8, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 portalSpawnPosition = npc.Center + Main.rand.NextVector2Circular(npc.width, npc.height) * 0.6f;
                    Utilities.NewProjectileBetter(portalSpawnPosition, Vector2.Zero, ModContent.ProjectileType<EnergyPortalBeam>(), 0, 0f);
                }
            }

            if (attackTimer > 375f)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_DarkMagicCharge(NPC npc, Player target, float lifeRatio, int phase, ref float attackTimer)
        {
            int chargeTime = 35;
            int chargeCount = phase >= 2 ? 2 : 3;
            float chargeSpeed = MathHelper.Lerp(23f, 29f, 1f - lifeRatio);

            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];

            switch ((int)attackState)
            {
                // Hover into position for the charge.
                case 0:
                    Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 420f, -300f);
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 22f, 0.4f);
                    npc.Center = npc.Center.MoveTowards(hoverDestination, 7.5f);

                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.WithinRange(hoverDestination, 50f))
                    {
                        npc.velocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY) * chargeSpeed;

                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 portalSpawnPosition = npc.Center + (MathHelper.TwoPi * i / 4f).ToRotationVector2() * 135f;
                            Utilities.NewProjectileBetter(portalSpawnPosition, Vector2.Zero, ModContent.ProjectileType<EnergyPortalBeam>(), 0, 0f);
                        }

                        attackTimer = 0f;
                        attackState = 1f;
                    }
                    break;

                // Do the charge.
                case 1:
                    npc.damage = npc.defDamage;
                    if (attackTimer > chargeTime)
                        npc.velocity *= 0.93f;
                    if (attackTimer > chargeTime + 25f)
                    {
                        attackTimer = 0f;
                        attackState = 0f;

                        if (chargeCounter < chargeCount)
                            chargeCounter++;
                        else
                            SelectNewAttack(npc);
                        npc.netUpdate = true;
                    }
                    break;
            }
        }

        public static void DoBehavior_DarkEnergyBolts(NPC npc, Player target, int phase, ref float attackTimer)
        {
            int burstCount = phase >= 2 ? 2 : 3;
            int projectilesPerBurst = phase >= 2 ? 11 : 8;

            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float burstCounter = ref npc.Infernum().ExtraAI[1];

            switch ((int)attackState)
            {
                // Hover into position.
                case 0:
                    Vector2 hoverDestination = target.Center - Vector2.UnitY * 370f;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 25f, 0.8f);
                    npc.Center = npc.Center.MoveTowards(hoverDestination, 10f);

                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.WithinRange(hoverDestination, 50f))
                    {
                        attackTimer = 0f;
                        attackState = 1f;
                    }
                    break;

                // Do the burst.
                case 1:
                    npc.velocity *= 0.92f;
                    if (attackTimer == 60f)
                    {
                        Main.PlaySound(SoundID.Item43, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < projectilesPerBurst; i++)
                            {
                                Vector2 shootVelocity = -Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / projectilesPerBurst) * 26f;
                                Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<DarkEnergyBolt>(), 260, 0f);
                            }
                        }
                    }

                    if (attackTimer > 105f)
                    {
                        attackTimer = 0f;
                        attackState = 0f;

                        if (burstCounter < burstCount)
                            burstCounter++;
                        else
                            SelectNewAttack(npc);
                        npc.netUpdate = true;
                    }
                    break;
            }
        }

        public static void DoBehavior_RealityCracks(NPC npc, Player target, int phase, ref float attackTimer)
        {
            int crackCount = 12 + phase * 8;

            // Hover into position.
            float moveSpeedFactor = Utils.InverseLerp(60f, 120f, attackTimer, true) * Utils.InverseLerp(240f, 180f, attackTimer, true);
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 370f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * moveSpeedFactor * 25f, moveSpeedFactor * 0.8f);
            npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero, moveSpeedFactor);
            npc.Center = npc.Center.MoveTowards(hoverDestination, moveSpeedFactor * 10f);

            // Create cracks and release a spread of dark energy.
            if (attackTimer == 150f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/FlareSound"), npc.Center);
                for (int i = 0; i < crackCount; i++)
                {
                    Vector2 crackSpawnPosition = target.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(50f, 990f);
                    Utilities.NewProjectileBetter(crackSpawnPosition, Vector2.Zero, ModContent.ProjectileType<RealityCrack>(), 0, 0f);
                }

                for (int i = 0; i < 20; i++)
                {
                    Vector2 burstVelocity = (MathHelper.TwoPi * i / 20f).ToRotationVector2() * 5f;
                    Utilities.NewProjectileBetter(npc.Center, burstVelocity, ModContent.ProjectileType<DarkEnergy>(), 260, 0f);
                }
            }

            if (attackTimer > 380f)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_DarkEnergyBursts(NPC npc, Player target, int phase, ref float attackTimer)
        {
            int burstCount = phase >= 3 ? 7 : 8;
            int projectilesPerBurst = 12;
            int burstShootRate = phase >= 3 ? 35 : 44;
            float burstSpeed = phase >= 3 ? 11f : 9f;

            ref float burstCounter = ref npc.Infernum().ExtraAI[0];

            // Hover into position.
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 370f;
            hoverDestination.Y += (float)Math.Cos(attackTimer / 20f) * 50f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 25f, 0.8f);
            npc.Center = npc.Center.MoveTowards(hoverDestination, 10f);

            // Shoot bursts after a short delay.
            if (attackTimer > 90f && attackTimer % burstShootRate == burstShootRate - 1f)
            {
                if (burstCounter >= burstCount)
                {
                    SelectNewAttack(npc);
                    return;
                }

                Main.PlaySound(SoundID.Item103, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < projectilesPerBurst; i++)
                    {
                        float offsetAngle = Main.rand.NextFloat(-0.99f, 0.99f);
                        Vector2 burstVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY).RotatedBy(offsetAngle);
                        burstVelocity *= Main.rand.NextFloat(0.6f, 1f) * burstSpeed;
                        Utilities.NewProjectileBetter(npc.Center + burstVelocity * 3f, burstVelocity, ModContent.ProjectileType<DarkEnergy>(), 260, 0f);
                    }
                    burstCounter++;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_DarkEnergyBulletHell(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int burstFireRate = (int)MathHelper.Lerp(32f, 16f, 1f - lifeRatio);
            int circleFireRate = (int)MathHelper.Lerp(72f, 50f, 1f - lifeRatio);
            int energyPerBurst = (int)MathHelper.Lerp(9f, 14f, 1f - lifeRatio);
            int energyPerCircle = (int)MathHelper.Lerp(15f, 22f, 1f - lifeRatio);
            float burstBaseSpeed = MathHelper.Lerp(6f, 10.5f, 1f - lifeRatio);

            if (BossRushEvent.BossRushActive)
                burstBaseSpeed *= 1.3f + npc.Distance(target.Center) * 0.00174f;

            // Slow down.
            npc.velocity *= 0.965f;

            // Don't take damage.
            npc.dontTakeDamage = true;

            // Make a pulse sound before firing.
            if (attackTimer == 45f)
                Main.PlaySound(SoundID.DD2_WitherBeastAuraPulse, target.Center);

            // Don't fire near the start/end of the attack.
            if (attackTimer < 90f || attackTimer > BulletHellTime - 120f)
                return;

            // Create bursts.
            if (attackTimer % burstFireRate == burstFireRate - 1f)
            {
                Main.PlaySound(SoundID.Item103, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float burstAngleOffset = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < energyPerBurst; i++)
                    {
                        float burstInterpolant = i / (float)(energyPerBurst - 1f);
                        float burstAngle = burstAngleOffset + burstInterpolant * (i + i * i) / 2f + 32f * i;
                        Vector2 burstVelocity = burstAngle.ToRotationVector2() * burstBaseSpeed * Main.rand.NextFloat(0.7f, 1f);
                        Utilities.NewProjectileBetter(npc.Center, burstVelocity, ModContent.ProjectileType<DarkEnergy>(), 260, 0f);
                    }
                }

                for (int i = 0; i < 60; i++)
                {
                    Dust magic = Dust.NewDustPerfect(npc.Center, 264);
                    magic.color = Color.Lerp(Color.Purple, Color.Pink, Main.rand.NextFloat());
                    magic.scale *= 1.45f;
                    magic.velocity = (MathHelper.TwoPi * (i + Main.rand.NextFloat(-0.5f, 0.5f)) / 60f).ToRotationVector2() * Main.rand.NextFloat(30f, 50f);
                    magic.velocity += Main.rand.NextVector2Circular(3f, 3f);
                    magic.noLight = true;
                    magic.noGravity = true;
                }
            }

            // Create circles of energy.
            if (attackTimer % circleFireRate == circleFireRate - 1f)
            {
                Main.PlaySound(SoundID.Item103, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < energyPerCircle; i++)
                    {
                        Vector2 burstVelocity = (MathHelper.TwoPi * i / energyPerCircle).ToRotationVector2() * burstBaseSpeed * 1.1f;
                        Utilities.NewProjectileBetter(npc.Center, burstVelocity, ModContent.ProjectileType<DarkEnergy>(), 260, 0f);
                    }
                }
            }
        }

        public static void SelectNewAttack(NPC npc)
        {
            // Select a new target.
            npc.TargetClosest();

            int phase = (int)npc.ai[3];
            List<CeaselessVoidAttackType> possibleAttacks = new List<CeaselessVoidAttackType>
            {
                CeaselessVoidAttackType.ReleaseRealityTearPortals,
                CeaselessVoidAttackType.DarkMagicCharge,
                CeaselessVoidAttackType.DarkEnergyBolts
            };

            if (phase >= 1)
                possibleAttacks.Add(CeaselessVoidAttackType.RealityCracks);
            if (phase >= 2)
                possibleAttacks.Add(CeaselessVoidAttackType.DarkEnergyBursts);

            if (possibleAttacks.Count > 1)
                possibleAttacks.Remove((CeaselessVoidAttackType)(int)npc.ai[0]);

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.ai[0] = (int)Main.rand.Next(possibleAttacks);
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI
    }
}
