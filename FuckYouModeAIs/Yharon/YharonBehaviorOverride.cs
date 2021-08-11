using CalamityMod.Dusts;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Yharon;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

using YharonBoss = CalamityMod.NPCs.Yharon.Yharon;

namespace InfernumMode.FuckYouModeAIs.Yharon
{
	public class YharonBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<YharonBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        #region Enumerations
        public enum YharonAttackType
        {
            SpawnEffects,
            Charge,
            FastCharge,
            FireMouthBreath,
            FlarenadoAndDetonatingFlameSpawn,
            SpinCharge,
            InfernadoAndFireShotgunBreath,
            MassiveInfernadoSummon,
            TeleportingCharge,
            RingOfFire,
            SplittingMeteors,
            PhoenixSupercharge,
            HeatFlash,
            VortexOfFlame,
            FinalDyingRoar
        }

        public enum YharonFrameDrawingType
        {
            None,
            FlapWings,
            IdleWings,
            Roar,
            OpenMouth,
        }
        #endregion

        #region Pattern Lists
        public static readonly YharonAttackType[] Subphase1Pattern = new YharonAttackType[]
        {
            YharonAttackType.Charge,
            YharonAttackType.Charge,
            YharonAttackType.FastCharge,
            YharonAttackType.FireMouthBreath,
            YharonAttackType.Charge,
            YharonAttackType.FireMouthBreath,
            YharonAttackType.Charge,
            YharonAttackType.Charge,
            YharonAttackType.FastCharge,
            YharonAttackType.Charge,
        };

        public static readonly YharonAttackType[] Subphase2Pattern = new YharonAttackType[]
        {
            YharonAttackType.FastCharge,
            YharonAttackType.Charge,
            YharonAttackType.FlarenadoAndDetonatingFlameSpawn,
            YharonAttackType.FastCharge,
            YharonAttackType.Charge,
            YharonAttackType.Charge,
            YharonAttackType.FireMouthBreath,
            YharonAttackType.Charge,
            YharonAttackType.Charge,
            YharonAttackType.FastCharge,
            YharonAttackType.Charge,
            YharonAttackType.FlarenadoAndDetonatingFlameSpawn,
            YharonAttackType.FireMouthBreath,
        };

        public static readonly YharonAttackType[] Subphase3Pattern = new YharonAttackType[]
        {
            YharonAttackType.FastCharge,
            YharonAttackType.SpinCharge,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.FireMouthBreath,
            YharonAttackType.FlarenadoAndDetonatingFlameSpawn,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.FastCharge,
            YharonAttackType.Charge,
            YharonAttackType.Charge,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.FireMouthBreath,
            YharonAttackType.SpinCharge,
            YharonAttackType.Charge,
            YharonAttackType.FastCharge,
            YharonAttackType.SpinCharge,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.FlarenadoAndDetonatingFlameSpawn,
            YharonAttackType.FireMouthBreath,
            YharonAttackType.FastCharge,
            YharonAttackType.InfernadoAndFireShotgunBreath
        };

        public static readonly YharonAttackType[] Subphase4Pattern = new YharonAttackType[]
        {
            YharonAttackType.FastCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.FastCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.FastCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.FastCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.FastCharge,
        };

        public static readonly YharonAttackType[] Subphase5Pattern = new YharonAttackType[]
        {
            YharonAttackType.RingOfFire,
            YharonAttackType.SplittingMeteors,
            YharonAttackType.SpinCharge,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.FastCharge,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.FastCharge,
            YharonAttackType.SplittingMeteors,
            YharonAttackType.RingOfFire,
            YharonAttackType.SpinCharge,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.FastCharge,
            YharonAttackType.TeleportingCharge,
            YharonAttackType.MassiveInfernadoSummon,
        };

        public static readonly YharonAttackType[] Subphase6Pattern = new YharonAttackType[]
        {
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.RingOfFire,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.FastCharge,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.RingOfFire,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.FastCharge,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.MassiveInfernadoSummon,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.RingOfFire,
            YharonAttackType.FastCharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
        };

        public static readonly YharonAttackType[] Subphase7Pattern = new YharonAttackType[]
        {
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.SplittingMeteors,
            YharonAttackType.HeatFlash,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.HeatFlash,
            YharonAttackType.SplittingMeteors,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.HeatFlash,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.SplittingMeteors,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.PhoenixSupercharge,
        };

        public static readonly YharonAttackType[] Subphase8Pattern = new YharonAttackType[]
        {
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.VortexOfFlame,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.SplittingMeteors,
            YharonAttackType.HeatFlash,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.VortexOfFlame,
            YharonAttackType.HeatFlash,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.HeatFlash,
            YharonAttackType.VortexOfFlame,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.PhoenixSupercharge,
            YharonAttackType.SplittingMeteors,
            YharonAttackType.VortexOfFlame,
            YharonAttackType.InfernadoAndFireShotgunBreath,
            YharonAttackType.PhoenixSupercharge,
        };

        public static readonly YharonAttackType[] Subphase9Pattern = new YharonAttackType[]
        {
            YharonAttackType.PhoenixSupercharge,
        };

        public static readonly YharonAttackType[] LastSubphasePattern = new YharonAttackType[]
        {
            YharonAttackType.FinalDyingRoar,
        };

        public static readonly Dictionary<YharonAttackType[], Func<NPC, bool>> SubphaseTable = new Dictionary<YharonAttackType[], Func<NPC, bool>>()
        {
            [Subphase1Pattern] = (npc) => npc.life / (float)npc.lifeMax > 0.8f && npc.Infernum().ExtraAI[2] == 0f,
            [Subphase2Pattern] = (npc) => npc.life / (float)npc.lifeMax > 0.55f && npc.life / (float)npc.lifeMax <= 0.8f && npc.Infernum().ExtraAI[2] == 0f,
            [Subphase3Pattern] = (npc) => npc.life / (float)npc.lifeMax > 0.35f && npc.life / (float)npc.lifeMax <= 0.55f && npc.Infernum().ExtraAI[2] == 0f,
            [Subphase4Pattern] = (npc) => npc.life / (float)npc.lifeMax > 0.1f && npc.life / (float)npc.lifeMax <= 0.35f && npc.Infernum().ExtraAI[2] == 0f,

            [Subphase5Pattern] = (npc) => (npc.life / (float)npc.lifeMax > 0.9f || npc.Infernum().ExtraAI[3] > 0f) && npc.Infernum().ExtraAI[2] == 1f,
            [Subphase6Pattern] = (npc) => npc.life / (float)npc.lifeMax > 0.7f && npc.life / (float)npc.lifeMax <= 0.9f && npc.Infernum().ExtraAI[2] == 1f,
            [Subphase7Pattern] = (npc) => npc.life / (float)npc.lifeMax > 0.4f && npc.life / (float)npc.lifeMax <= 0.7f && npc.Infernum().ExtraAI[2] == 1f,
            [Subphase8Pattern] = (npc) => npc.life / (float)npc.lifeMax > 0.15f && npc.life / (float)npc.lifeMax <= 0.4f && npc.Infernum().ExtraAI[2] == 1f,
            [Subphase9Pattern] = (npc) => npc.life / (float)npc.lifeMax > 0.05f && npc.life / (float)npc.lifeMax <= 0.15f && npc.Infernum().ExtraAI[2] == 1f,
            [LastSubphasePattern] = (npc) => npc.life / (float)npc.lifeMax <= 0.05f && npc.Infernum().ExtraAI[2] == 1f,
        };
        #endregion

        #region Attack Specific Constants
        public const int SpawnEffectsTime = 48;
        #endregion

        // Attack Type = npc.ai[0]
        // Attack Timer = npc.ai[1]
        // Enraged 0-1 Flag = npc.ai[2]
        // Spawned Arena 0-1 Flag = npc.ai[3]
        // Attack Type Subphase Index = npc.Infernum().ExtraAI[0]
        // Special Framing Type = npc.Infernum().ExtraAI[1]
        // Phase 2 0-1 Flag = npc.Infernum().ExtraAI[2]
        // Invincibility Timer = npc.Infernum().ExtraAI[3]

        // Subphase Specific = npc.Infernum().ExtraAI[4 through 8]
        // Phoenix Form intensity = npc.Infernum().ExtraAI[9]
        // Final Dying Roar flame illusion count = npc.Infernum().ExtraAI[10]
        // Transition phase timer = npc.Infernum().ExtraAI[11]
        // Current subphase = npc.Infernum().ExtraAI[12]

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Stop rain if it's happen so it doesn't obstruct the fight (also because Yharon is heat oriented).
            CalamityMod.CalamityMod.StopRain();

            // Reset total HP.
            if (npc.lifeMax != 3333330)
            {
                npc.lifeMax = 3333330;
                npc.life = npc.lifeMax;
                npc.netUpdate = true;
            }

            // Aquire a new target if the current one is dead or inactive.
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                npc.TargetClosest(false);

                // If no possible target was found, fly away.
                if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                {
                    npc.velocity.Y -= 0.5f;
                    npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                    if (npc.timeLeft > 240)
                        npc.timeLeft = 240;
                    return false;
                }
                npc.netUpdate = true;
            }
            else
                npc.timeLeft = 7200;

            Player player = Main.player[npc.target];

            float lifeRatio = npc.life / (float)npc.lifeMax;
            float phase2InvincibilityTime = 900f;
            float transitionTimer = 120f;

            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float specialFrameType = ref npc.Infernum().ExtraAI[1];
            ref float invincibilityTime = ref npc.Infernum().ExtraAI[3];
            ref float fireIntensity = ref npc.Infernum().ExtraAI[9];
            ref float subphaseTransitionTimer = ref npc.Infernum().ExtraAI[11];
            ref float currentSubphase = ref npc.Infernum().ExtraAI[12];

            // Go to phase 2 if at 10%.
            if (npc.Infernum().ExtraAI[2] == 0f && lifeRatio < 0.1f)
            {
                npc.Infernum().ExtraAI[2] = 1f;

                // The air is getting warmer around you...
                string key = "Mods.CalamityMod.FlameText";

                if (Main.netMode == NetmodeID.SinglePlayer)
                    Main.NewText(Language.GetTextValue(key), Color.Orange);
                else if (Main.netMode == NetmodeID.Server)
                    NetMessage.BroadcastChatMessage(NetworkText.FromKey(key), Color.Orange);

                attackType = 0; // Reset the attack type.

                // And spawn some cool sparkles.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 180; i++)
                    {
                        Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(240f, 240f);
                        Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(28f, 28f), ModContent.ProjectileType<MajesticSparkleBig>(), 0, 0f);
                    }
                }
                Mod calamityModMusic = ModLoader.GetMod("CalamityModMusic");
                if (calamityModMusic != null)
                    npc.modNPC.music = calamityModMusic.GetSoundSlot(SoundType.Music, "Sounds/Music/DragonGod");
                else npc.modNPC.music = MusicID.LunarBoss;
                invincibilityTime = phase2InvincibilityTime;
            }

            YharonAttackType[] patternToUse = SubphaseTable.First(table => table.Value(npc)).Key;
            float oldSubphase = currentSubphase;
            currentSubphase = SubphaseTable.Keys.ToList().IndexOf(patternToUse);

            if (oldSubphase != currentSubphase)
            {
                subphaseTransitionTimer = transitionTimer;
            }

            YharonAttackType nextAttackType = patternToUse[(int)((attackType + 1) % patternToUse.Length)];

            if (subphaseTransitionTimer > 0)
            {
                npc.rotation = npc.rotation.AngleTowards(0f, 0.15f);
                npc.velocity *= 0.96f;
                npc.Infernum().ExtraAI[9] = (int)MathHelper.Lerp(npc.Infernum().ExtraAI[3], 16, 0.275f);
                specialFrameType = (int)YharonFrameDrawingType.FlapWings;

                if (subphaseTransitionTimer < 18f)
                {
                    specialFrameType = (int)YharonFrameDrawingType.Roar;
                    if (subphaseTransitionTimer == 9)
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/YharonRoar"), npc.Center);
                }

                subphaseTransitionTimer--;
                return false;
            }

            // Adjust various values before doing anything else. If these need to be changed later, they will be,
            npc.dontTakeDamage = false;
            Filters.Scene["HeatDistortion"].GetShader().UseIntensity(0.5f);
            npc.Infernum().ExtraAI[10] = 0f;

            if (nextAttackType != YharonAttackType.PhoenixSupercharge && nextAttackType != YharonAttackType.HeatFlash)
                fireIntensity = MathHelper.Lerp(fireIntensity, 0f, 0.075f);

            float chargeSpeed = 31f;
            float chargeDelay = 45;
            float chargeTime = 45f;
            float fastChargeSpeedMultiplier = 1.5f;

            float fireballBreathShootDelay = 40f;
            float totalFireballBreaths = 12f;
            float fireballBreathShootRate = 5f;

            float flarenadoSpawnDelay = 30f;

            float upwardFlyTime = 120f;
            float horizontalMovementDelay = 45f;
            float totalSpins = 2f;
            float spinTime = 45f;

            int totalShotgunBursts = 3;
            float shotgunBurstFireRate = 30f;

            float infernadoAttackPowerupTime = 90f;

            int totalFlaresInRing = 6;
            float flareRingSpawnRate = 10f;

            float splittingMeteorBombingSpeed = 24f;
            float splittingMeteorRiseTime = 120f;
            float splittingMeteorBombTime = 90f;

            float heatFlashStartDelay = 60f;
            float heatFlashIdleDelay = 30f;
            float heatFlashFlashTime = 60f;
            int heatFlashTotalFlames = 24;

            int totalFlameVortices = 3;
            int totalFlameWaves = 7;
            float flameVortexSpawnDelay = 60f;

            bool phase2 = npc.Infernum().ExtraAI[2] == 1f;
            bool berserkChargeMode = phase2 && lifeRatio < 0.3f && lifeRatio >= 0.05f;

            Vector2 offsetCenter = npc.SafeDirectionTo(player.Center) * (npc.width * 0.5f + 10) + npc.Center;
            Vector2 mouthPosition = new Vector2(offsetCenter.X + 60 * npc.direction, offsetCenter.Y - 15);

            bool enraged = ArenaSpawnAndEnrageCheck(npc, player);

            switch ((YharonAttackType)(int)attackType)
            {
                case YharonAttackType.FastCharge:
                    chargeDelay = 90;
                    break;
                case YharonAttackType.PhoenixSupercharge:
                    chargeDelay = berserkChargeMode ? 30 : 60;
                    chargeTime = berserkChargeMode ? 20f : 35f;
                    fastChargeSpeedMultiplier = berserkChargeMode ? 1.4f : 1.7f;
                    break;
            }

            if (enraged)
            {
                npc.damage = npc.defDamage * 50;
                npc.dontTakeDamage = true;
                chargeSpeed += 30f;
                chargeDelay /= 2;
                chargeTime = 30f;

                fireballBreathShootDelay = 25f;
                totalFireballBreaths = 25f;
                fireballBreathShootRate = 3f;

                shotgunBurstFireRate = 15f;

                splittingMeteorBombingSpeed = 31f;
                heatFlashTotalFlames = 65;

                totalFlameVortices = 6;
                totalFlameWaves = 10;
                flameVortexSpawnDelay = 40f;
            }
            else
            {
                npc.damage = npc.defDamage; // Further npc damage manipulation can be done later if necessary.
            }

            if (phase2)
            {
                chargeSpeed += 3f;
                fastChargeSpeedMultiplier += 0.08f;
            }

            // Regenerate health over time in Phase 2
            if (invincibilityTime > 0f)
            {
                if (invincibilityTime % 5f == 0f)
                {
                    int healValue = (int)(npc.lifeMax * 0.9 / (phase2InvincibilityTime / 5f));
                    npc.life += healValue;
                    npc.HealEffect(healValue);
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(180f, 180f);
                    Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(12f, 12f), ModContent.ProjectileType<MajesticSparkle>(), 0, 0f);
                }
                npc.dontTakeDamage = true;
                invincibilityTime--;
            }

            switch ((YharonAttackType)(int)attackType)
            {
                // Only happens when Yharon spawns.
                case YharonAttackType.SpawnEffects:

                    // Idly spawn pretty sparkles.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(210f, 210f);
                        Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(18f, 18f), ModContent.ProjectileType<MajesticSparkle>(), 0, 0f);
                    }
                    if (attackTimer >= SpawnEffectsTime)
                    {
                        // Spawn a circle of fire bombs instead of flare dust.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 16; i++)
                            {
                                float angle = MathHelper.TwoPi / 16f * i;
                                Utilities.NewProjectileBetter(npc.Center + angle.ToRotationVector2() * 60f, angle.ToRotationVector2() * 15f, ModContent.ProjectileType<FlareBomb>(), 480, 0f);
                            }
                        }
                        GoToNextAttack(npc, ref attackType);
                    }
                    break;
                case YharonAttackType.Charge:
                case YharonAttackType.TeleportingCharge:
                    // Slow down and rotate towards the player.
                    if (attackTimer < chargeDelay)
                    {
                        npc.velocity *= 0.97f;
                        npc.spriteDirection = (player.Center.X - npc.Center.X < 0).ToDirectionInt();
                        npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(player.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi, 0.1f);
                        if (attackTimer == chargeDelay - 20f && (YharonAttackType)(int)attackType == YharonAttackType.TeleportingCharge)
                        {
                            int playerDirection = Math.Sign(player.velocity.X);
                            if (playerDirection == 0)
                                playerDirection = Main.rand.NextBool(2).ToDirectionInt();
                            npc.Center = player.Center + new Vector2(playerDirection * 690f, 400f * Main.rand.NextBool(2).ToDirectionInt());
                            npc.velocity = Vector2.Zero;
                            npc.netUpdate = true;

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i < 30; i++)
                                {
                                    Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(180f, 180f);
                                    Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(12f, 12f), ModContent.ProjectileType<MajesticSparkle>(), 0, 0f);
                                }
                            }

                            npc.alpha = 255;
                        }
                        if (npc.alpha > 0)
                        {
                            npc.alpha -= 28;
                            if (npc.alpha < 0)
                                npc.alpha = 0;
                        }
                        specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                    }
                    // Launch self towards the player.
                    else if (attackTimer == chargeDelay)
                    {
                        npc.velocity = npc.SafeDirectionTo(player.Center) * chargeSpeed;
                        npc.rotation = npc.AngleTo(player.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;
                        specialFrameType = (int)YharonFrameDrawingType.IdleWings;
                        npc.netUpdate = true;

                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/YharonRoarShort"), npc.Center);
                    }
                    else if (attackTimer >= chargeDelay + chargeTime)
                    {
                        GoToNextAttack(npc, ref attackType);
                    }
                    break;
                case YharonAttackType.FastCharge:
                case YharonAttackType.PhoenixSupercharge:
                    // Slow down and rotate towards the player.
                    if (attackTimer < chargeDelay)
                    {
                        npc.spriteDirection = (npc.Center.X > player.Center.X).ToDirectionInt();
                        ref float xAimOffset = ref npc.Infernum().ExtraAI[4];
                        if (xAimOffset == 0f)
                            xAimOffset = (berserkChargeMode ? 920f : 620f) * Math.Sign((npc.Center - player.Center).X);

                        if ((YharonAttackType)(int)attackType == YharonAttackType.PhoenixSupercharge)
                        {
                            // Transform into a phoenix flame form.
                            fireIntensity = MathHelper.Max(fireIntensity, Utils.InverseLerp(0f, chargeDelay - 1f, attackTimer, true));
                        }

                        Vector2 destination = player.Center + new Vector2(xAimOffset, (berserkChargeMode ? -480f : -240f)) - npc.Center;
                        Vector2 idealVelocity = Vector2.Normalize(destination - npc.velocity) * 18f;
                        npc.SimpleFlyMovement(idealVelocity, 1.1f);

                        npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(player.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi, 0.1f);
                        specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                    }
                    // Launch self towards the player and release some deadly fireballs.
                    else if (attackTimer == chargeDelay)
                    {
                        npc.velocity = npc.SafeDirectionTo(player.Center) * chargeSpeed * fastChargeSpeedMultiplier;
                        npc.rotation = npc.AngleTo(player.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;
                        specialFrameType = (int)YharonFrameDrawingType.IdleWings;

                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/YharonRoarShort"), npc.Center);

                        npc.netUpdate = true;
                    }
                    else if (attackTimer < chargeDelay + chargeTime)
                    {
                        if ((YharonAttackType)(int)attackType == YharonAttackType.PhoenixSupercharge)
                        {
                            float competionRatio = Utils.InverseLerp(chargeDelay, chargeDelay + chargeTime, attackTimer, true);
                            Filters.Scene["HeatDistortion"].GetShader().UseIntensity(0.5f + (float)Math.Sin(competionRatio * MathHelper.Pi) * 3f);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i < 2; i++)
                                {
                                    Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(240f, 240f);
                                    Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(28f, 28f), ModContent.ProjectileType<MajesticSparkleBig>(), 0, 0f);
                                }
                            }
                        }
                    }
                    if (attackTimer >= chargeDelay + chargeTime)
                    {
                        GoToNextAttack(npc, ref attackType);
                    }
                    break;
                case YharonAttackType.FireMouthBreath:
                    // Slow down quickly, rotate towards a horizontal orientation, and then spawn a bunch of fire.
                    if (attackTimer < fireballBreathShootDelay)
                    {
                        specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                        npc.velocity *= 0.955f;
                        npc.rotation = npc.rotation.AngleTowards(0f, 0.1f);
                    }
                    else
                    {
                        specialFrameType = (int)YharonFrameDrawingType.OpenMouth;

                        npc.spriteDirection = (npc.Center.X > player.Center.X).ToDirectionInt();
                        ref float xAimOffset = ref npc.Infernum().ExtraAI[4];
                        if (xAimOffset == 0f)
                            xAimOffset = 560f * Math.Sign((npc.Center - player.Center).X);

                        Vector2 destination = player.Center + new Vector2(xAimOffset, -270f) - npc.Center;
                        Vector2 idealVelocity = Vector2.Normalize(destination - npc.velocity) * 17f;
                        npc.SimpleFlyMovement(idealVelocity, 1f);

                        npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(player.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi, 0.1f);

                        if (attackTimer % fireballBreathShootRate == fireballBreathShootRate - 1)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                int fire = Utilities.NewProjectileBetter(mouthPosition, npc.SafeDirectionTo(mouthPosition) * 20f, ProjectileID.CultistBossFireBall, 450, 0f);
                                Main.projectile[fire].tileCollide = false;
                            }
                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/YharonRoarShort"), npc.Center);
                        }
                        if (attackTimer >= fireballBreathShootDelay + fireballBreathShootRate * totalFireballBreaths)
                            GoToNextAttack(npc, ref attackType);
                    }
                    break;
                case YharonAttackType.FlarenadoAndDetonatingFlameSpawn:
                    // Slow down quickly, rotate towards a horizontal orientation, and then spawn a bunch of detonating flares and some flarenado things.
                    if (attackTimer < flarenadoSpawnDelay)
                    {
                        specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                        npc.velocity *= 0.955f;
                        npc.rotation = npc.rotation.AngleTowards(0f, 0.1f);
                    }
                    else if (attackTimer == flarenadoSpawnDelay)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                float angle = MathHelper.TwoPi / 3f * i;
                                Utilities.NewProjectileBetter(mouthPosition, angle.ToRotationVector2() * 7f, ModContent.ProjectileType<BigFlare>(), 0, 0f, Main.myPlayer, 1f, npc.target + 1);
                            }
                        }
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/YharonRoar"), npc.Center);
                    }
                    else if (attackTimer < flarenadoSpawnDelay + 75)
                    {
                        specialFrameType = (int)YharonFrameDrawingType.Roar;
                        if ((attackTimer - flarenadoSpawnDelay) % 12 == 11)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.CountNPCS(ModContent.NPCType<DetonatingFlare>()) + NPC.CountNPCS(ModContent.NPCType<DetonatingFlare2>()) < 8)
                            {
                                Vector2 flareSpawnPosition = npc.Center + Main.rand.NextVector2CircularEdge(200f, 100f) * Main.rand.NextFloat(0.6f, 1f);

                                if (player.Distance(flareSpawnPosition) > 190f)
                                {
                                    NPC.NewNPC((int)flareSpawnPosition.X, (int)flareSpawnPosition.Y, ModContent.NPCType<DetonatingFlare>());
                                }
                            }
                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/YharonRoarShort"), npc.Center);
                        }
                    }
                    else GoToNextAttack(npc, ref attackType);
                    break;
                case YharonAttackType.SpinCharge:
                    // Fly upward to the side of the player.
                    if (attackTimer < 45f)
                        npc.velocity *= 0.97f;
                    if (attackTimer > 45f && attackTimer < upwardFlyTime)
                    {
                        ref float xAimOffset = ref npc.Infernum().ExtraAI[4];
                        if (xAimOffset == 0f)
                            xAimOffset = 820f * Math.Sign((npc.Center - player.Center).X);

                        Vector2 destination = player.Center + new Vector2(xAimOffset, -450f);

                        if (npc.Distance(destination) < 16f)
                            npc.Center = destination;
                        else
                            npc.velocity = npc.SafeDirectionTo(destination) * 16f;

                        npc.spriteDirection = (npc.velocity.X < 0).ToDirectionInt();
                        npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);

                        if (Vector2.Distance(destination, npc.Center) < 42f)
                            attackTimer = upwardFlyTime - 1f;
                        specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                    }
                    // And charge after a while.
                    if (attackTimer == upwardFlyTime)
                    {
                        int direction = Math.Sign(npc.SafeDirectionTo(player.Center).X);
                        npc.velocity = Vector2.UnitX * direction * 35f;
                        npc.spriteDirection = (npc.velocity.X < 0).ToDirectionInt();
                        specialFrameType = (int)YharonFrameDrawingType.IdleWings;
                    }
                    // Then do a barrel roll, and charge.
                    if (attackTimer > upwardFlyTime + horizontalMovementDelay && (attackTimer - upwardFlyTime - horizontalMovementDelay) % 90f <= spinTime)
                    {
                        ref float hasChargedYet01Flag = ref npc.Infernum().ExtraAI[5];
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(180f, 180f);
                            Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(12f, 12f), ModContent.ProjectileType<MajesticSparkle>(), 0, 0f);
                        }
                        if (attackTimer % 5f == 4f)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                int fire = Utilities.NewProjectileBetter(mouthPosition, npc.velocity * 0.667f, ProjectileID.CultistBossFireBall, 450, 0f);
                                Main.projectile[fire].tileCollide = false;
                            }
                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/YharonRoarShort"), npc.Center);
                        }

                        // Reset the attack timer if an opening for a charge is found, and charge towards the player.
                        if (hasChargedYet01Flag == 0f)
                        {
                            npc.velocity = npc.velocity.RotatedBy(MathHelper.TwoPi / spinTime);
                            bool aimedTowardsPlayer = Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(player.Center)) > 0.95f;
                            if (attackTimer >= upwardFlyTime + horizontalMovementDelay + 75f * (totalSpins - 1) && aimedTowardsPlayer)
                            {
                                npc.velocity = npc.SafeDirectionTo(player.Center) * chargeSpeed * fastChargeSpeedMultiplier;
                                attackTimer = upwardFlyTime + horizontalMovementDelay + 75f * (totalSpins - 1);
                                hasChargedYet01Flag = 1f;
                            }
                        }
                        npc.spriteDirection = (npc.velocity.X < 0).ToDirectionInt();
                        npc.rotation = npc.velocity.ToRotation() + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;
                    }
                    if (attackTimer >= upwardFlyTime + horizontalMovementDelay + totalSpins * 75f)
                        GoToNextAttack(npc, ref attackType);
                    break;
                case YharonAttackType.InfernadoAndFireShotgunBreath:
                    if (attackTimer < fireballBreathShootDelay)
                    {
                        npc.velocity *= 0.955f;
                        npc.rotation = npc.rotation.AngleTowards(0f, 0.1f);
                        specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                    }
                    else
                    {
                        npc.spriteDirection = (npc.Center.X > player.Center.X).ToDirectionInt();
                        ref float xAimOffset = ref npc.Infernum().ExtraAI[4];
                        if (xAimOffset == 0f)
                            xAimOffset = 770f * Math.Sign((npc.Center - player.Center).X);

                        Vector2 destination = player.Center + new Vector2(xAimOffset, -360f) - npc.Center;
                        Vector2 idealVelocity = Vector2.Normalize(destination - npc.velocity) * 17f;
                        npc.SimpleFlyMovement(idealVelocity, 1f);

                        npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(player.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi, 0.1f);

                        // Release an infernado flare from the mouth.
                        if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == fireballBreathShootDelay)
                        {
                            Utilities.NewProjectileBetter(mouthPosition, npc.SafeDirectionTo(mouthPosition) * 8f, ModContent.ProjectileType<BigFlare2>(), 0, 0f, Main.myPlayer, 1f, npc.target + 1);
                        }

                        // Release a shotgun spread of fireballs.
                        if ((attackTimer - fireballBreathShootDelay) % shotgunBurstFireRate == shotgunBurstFireRate - 1f)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                int fireballCount = Main.rand.Next(10, 15 + 1);
                                float angleSpread = Main.rand.NextFloat(0.3f, 0.55f);
                                for (int i = 0; i < fireballCount; i++)
                                {
                                    float offsetAngle = MathHelper.Lerp(-angleSpread, angleSpread, i / (float)fireballCount);
                                    float speed = Main.rand.NextFloat(9f, 13f);
                                    if (enraged)
                                        speed *= Main.rand.NextFloat(1.5f, 2.7f);
                                    int fire = Utilities.NewProjectileBetter(mouthPosition, npc.SafeDirectionTo(mouthPosition).RotatedBy(offsetAngle) * speed, ProjectileID.CultistBossFireBall, 500, 0f, Main.myPlayer);
                                    Main.projectile[fire].tileCollide = false;
                                }
                            }
                            if (attackTimer >= fireballBreathShootDelay + shotgunBurstFireRate * totalShotgunBursts - 1)
                                GoToNextAttack(npc, ref attackType);
                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/YharonRoarShort"), npc.Center);
                        }
                        specialFrameType = (int)YharonFrameDrawingType.Roar;
                    }
                    break;
                case YharonAttackType.MassiveInfernadoSummon:
                    // Slow down and charge up
                    if (attackTimer < infernadoAttackPowerupTime)
                    {
                        specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                        npc.velocity *= 0.955f;
                        npc.rotation = npc.rotation.AngleTowards(0f, 0.1f);
                        if (attackTimer % 4f == 3f)
                        {
                            for (int i = 0; i < 100; i++)
                            {
                                float angle = MathHelper.TwoPi * i / 100f;
                                float intensity = Main.rand.NextFloat();
                                Vector2 spawnPosition = npc.Center + angle.ToRotationVector2() * Main.rand.NextFloat(720f, 900f);
                                Vector2 velocity = (angle - MathHelper.Pi).ToRotationVector2() * (29f + 11f * intensity);
                                Dust dust = Dust.NewDustPerfect(spawnPosition, DustID.Fire, velocity);
                                dust.scale = 0.9f;
                                dust.fadeIn = 1.15f + intensity * 0.3f;
                                dust.noGravity = true;
                            }
                        }
                        specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                    }
                    // Release the energy, spawn some charging infernados, and go to the next attack phase.
                    if (attackTimer == infernadoAttackPowerupTime)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 28; i++)
                            {
                                float angle = MathHelper.TwoPi / 28f * i;
                                float speed = Main.rand.NextFloat(12f, 15f);
                                Utilities.NewProjectileBetter(npc.Center + angle.ToRotationVector2() * 40f, angle.ToRotationVector2() * speed, ModContent.ProjectileType<FlareBomb>(), 540, 0f);
                            }

                            for (int i = 0; i < 3; i++)
                            {
                                float angle = MathHelper.TwoPi / 3f * i;
                                Vector2 flareSpawnPosition = npc.Center + angle.ToRotationVector2() * 600f;
                                Utilities.NewProjectileBetter(flareSpawnPosition, angle.ToRotationVector2().RotatedByRandom(0.03f) * Vector2.Zero, ModContent.ProjectileType<ChargeFlare>(), 0, 0f, Main.myPlayer);
                            }
                        }
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/YharonRoar"), npc.Center);
                        GoToNextAttack(npc, ref attackType);
                    }
                    break;
                case YharonAttackType.RingOfFire:
                    specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                    npc.velocity *= 0.955f;
                    npc.rotation = npc.rotation.AngleTowards(0f, 0.1f);
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % flareRingSpawnRate == flareRingSpawnRate - 1)
                    {
                        Vector2 flareSpawnPosition = npc.Center + ((attackTimer - flareRingSpawnRate + 1) / flareRingSpawnRate * MathHelper.TwoPi / totalFlaresInRing).ToRotationVector2() * 665f;
                        NPC.NewNPC((int)flareSpawnPosition.X, (int)flareSpawnPosition.Y, ModContent.NPCType<DetonatingFlare>());

                        for (int i = 0; i < 4; i++)
                        {
                            float angle = MathHelper.TwoPi / 4f * i;
                            int fire = Utilities.NewProjectileBetter(flareSpawnPosition, angle.ToRotationVector2() * 8.5f, ProjectileID.CultistBossFireBall, 470, 0f, Main.myPlayer);
                            Main.projectile[fire].tileCollide = false;
                        }
                    }
                    if (attackTimer >= flareRingSpawnRate * totalFlaresInRing)
                        GoToNextAttack(npc, ref attackType);
                    break;
                case YharonAttackType.SplittingMeteors:
                    int directionToDestination = (npc.Center.X > player.Center.X).ToDirectionInt();
                    bool morePowerfulMeteors = lifeRatio < 0.7f;
                    if (attackTimer < splittingMeteorRiseTime)
                    {
                        Vector2 destination = player.Center + new Vector2(directionToDestination * 750f, -300f);
                        Vector2 idealVelocity = npc.SafeDirectionTo(destination) * splittingMeteorBombingSpeed;

                        npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.035f);
                        npc.rotation = npc.rotation.AngleTowards(0f, 0.25f);

                        npc.spriteDirection = (npc.Center.X > player.Center.X).ToDirectionInt();

                        if (Vector2.Distance(destination, npc.Center) < 32f)
                            attackTimer = splittingMeteorRiseTime - 1f;
                        specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                    }
                    else if (attackTimer == splittingMeteorRiseTime)
                    {
                        Vector2 velocity = npc.SafeDirectionTo(player.Center);
                        velocity.Y *= 0.15f;
                        velocity = velocity.SafeNormalize(Vector2.UnitX * npc.spriteDirection);

                        specialFrameType = (int)YharonFrameDrawingType.OpenMouth;

                        npc.velocity = velocity * splittingMeteorBombingSpeed;
                        if (morePowerfulMeteors)
                            npc.velocity *= 1.45f;
                    }
                    else
                    {
                        npc.position.X += npc.SafeDirectionTo(player.Center).X * 7f;
                        npc.position.Y += npc.SafeDirectionTo(player.Center + Vector2.UnitY * -400f).Y * 6f;
                        npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
                        npc.rotation = npc.velocity.ToRotation() + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;

                        int fireballReleaseRate = morePowerfulMeteors ? 4 : 7;
                        if (attackTimer % fireballReleaseRate == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                            Utilities.NewProjectileBetter(npc.Center + npc.velocity * 3f, npc.velocity, ModContent.ProjectileType<YharonFireball>(), 515, 0f, Main.myPlayer, 0f, 0f);
                    }
                    if (attackTimer >= splittingMeteorRiseTime + splittingMeteorBombTime)
                        GoToNextAttack(npc, ref attackType);
                    break;
                case YharonAttackType.HeatFlash:
                    npc.damage = 0;
                    if (attackTimer < heatFlashIdleDelay)
                    {
                        npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(player.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi, 0.1f);
                        npc.spriteDirection = 1;

                        Vector2 destinationAbovePlayer = player.Center - Vector2.UnitY * 420f - npc.Center;
                        npc.SimpleFlyMovement(Vector2.Normalize(destinationAbovePlayer - npc.velocity) * 36f, 3.5f);
                    }
                    else
                    {
                        // Teleport above the player if somewhat far away from them.
                        if (attackTimer == heatFlashIdleDelay && npc.Distance(player.Center) > 360f)
                        {
                            npc.Center = player.Center - Vector2.UnitY * 420f;
                            if (!Main.dedServ)
                            {
                                for (int j = 0; j < 30; j++)
                                {
                                    float angle = MathHelper.TwoPi * j / 30f;
                                    Dust dust = Dust.NewDustDirect(player.Center, 0, 0, ModContent.DustType<FinalFlame>(), 0f, 0f, 100, default, 3f);
                                    dust.noGravity = true;
                                    dust.noLight = true;
                                    dust.fadeIn = 1.2f;
                                    dust.velocity = angle.ToRotationVector2() * 5f;
                                }
                            }
                        }
                        npc.velocity *= 0.95f;
                        // Transform into a phoenix flame form.
                        fireIntensity = MathHelper.Max(fireIntensity, Utils.InverseLerp(0f, chargeDelay - 1f, attackTimer, true));

                        npc.rotation = npc.rotation.AngleTowards(0f, 0.7f);
                        if (attackTimer >= heatFlashIdleDelay + heatFlashStartDelay && attackTimer <= heatFlashIdleDelay + heatFlashStartDelay + heatFlashFlashTime)
                        {
                            float brightness = (float)Math.Sin(Utils.InverseLerp(heatFlashIdleDelay + heatFlashStartDelay, heatFlashIdleDelay + heatFlashStartDelay + heatFlashFlashTime, attackTimer, true) * MathHelper.Pi);
                            bool atMaximumBrightness = attackTimer == heatFlashIdleDelay + heatFlashStartDelay + heatFlashFlashTime / 2;
                            if (atMaximumBrightness)
                            {
                                float outwardness = 550f + MathHelper.Min(player.velocity.Length(), 40f) * 12f;

                                // Spawn a circle of flames.
                                for (int i = 0; i < heatFlashTotalFlames; i++)
                                {
                                    float angle = MathHelper.TwoPi * i / heatFlashTotalFlames;
                                    if (Main.netMode != NetmodeID.MultiplayerClient)
                                    {
                                        float angleFromTarget = Utilities.AngleBetween(angle.ToRotationVector2(), player.velocity);
                                        Utilities.NewProjectileBetter(player.Center + angle.ToRotationVector2() * outwardness, Vector2.Zero, ModContent.ProjectileType<YharonHeatFlashFireball>(), 595, 0f, Main.myPlayer);
                                        if (angleFromTarget <= MathHelper.TwoPi / heatFlashTotalFlames)
                                        {
                                            for (int j = 0; j < Main.rand.Next(11, 17 + 1); j++)
                                            {
                                                float newAngle = angle + Main.rand.NextFloatDirection() * angleFromTarget;
                                                Utilities.NewProjectileBetter(player.Center + newAngle.ToRotationVector2() * outwardness, Vector2.Zero, ModContent.ProjectileType<YharonHeatFlashFireball>(), 595, 0f, Main.myPlayer);
                                            }
                                        }
                                    }
                                    else if (!Main.dedServ)
                                    {
                                        for (int j = 0; j < 14; j++)
                                        {
                                            float angle2 = MathHelper.TwoPi * j / 14f;
                                            Dust dust = Dust.NewDustDirect(player.Center + angle.ToRotationVector2() * outwardness, 0, 0, ModContent.DustType<FinalFlame>(), 0f, 0f, 100, default, 2.1f);
                                            dust.noGravity = true;
                                            dust.noLight = true;
                                            dust.velocity = angle2.ToRotationVector2() * 5f;
                                        }
                                    }
                                }
                                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/YharonRoar"), npc.Center);
                            }
                            MoonlordDeathDrama.RequestLight(brightness, player.Center);
                        }
                    }

                    if (attackTimer >= heatFlashIdleDelay + heatFlashStartDelay + heatFlashFlashTime)
                        GoToNextAttack(npc, ref attackType);
                    specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                    break;
                case YharonAttackType.VortexOfFlame:
                    npc.velocity *= 0.85f;
                    npc.spriteDirection = -1;
                    npc.rotation = npc.rotation.AngleTowards(0f, 0.185f);
                    // Teleport above the player if somewhat far away from them.
                    if (attackTimer == 1f && npc.Distance(player.Center) > 360f)
                    {
                        npc.Center = player.Center - Vector2.UnitY * 480f;
                        if (!Main.dedServ)
                        {
                            for (int j = 0; j < 30; j++)
                            {
                                float angle = MathHelper.TwoPi * j / 30f;
                                Dust dust = Dust.NewDustDirect(player.Center, 0, 0, ModContent.DustType<FinalFlame>(), 0f, 0f, 100, default, 3f);
                                dust.noGravity = true;
                                dust.noLight = true;
                                dust.fadeIn = 1.2f;
                                dust.velocity = angle.ToRotationVector2() * 5f;
                            }
                        }
                        npc.netUpdate = true;
                        specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                    }

                    // Spawn vortices of doom.
                    if (attackTimer == flameVortexSpawnDelay && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < totalFlameVortices; i++)
                        {
                            float angle = MathHelper.TwoPi * i / totalFlameVortices;
                            Utilities.NewProjectileBetter(player.Center + angle.ToRotationVector2() * 1780f, Vector2.Zero, ModContent.ProjectileType<VortexOfFlame>(), 800, 0f, Main.myPlayer);
                        }
                    }

                    if (attackTimer > flameVortexSpawnDelay && attackTimer % 7 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float xOffset = (attackTimer - flameVortexSpawnDelay) / 7f * 205f + 260f;
                        Utilities.NewProjectileBetter(npc.Center + new Vector2(-xOffset, -90f), Vector2.UnitY.RotatedBy(-0.18f) * -20f, ModContent.ProjectileType<YharonFireball>(), 525, 0f, Main.myPlayer);
                        Utilities.NewProjectileBetter(npc.Center + new Vector2(xOffset, -90f), Vector2.UnitY.RotatedBy(0.18f) * -20f, ModContent.ProjectileType<YharonFireball>(), 525, 0f, Main.myPlayer);
                    }
                    if (attackTimer > flameVortexSpawnDelay + totalFlameWaves * 7)
                        GoToNextAttack(npc, ref attackType);
                    break;
                case YharonAttackType.FinalDyingRoar:
                    npc.dontTakeDamage = true;
                    Filters.Scene["HeatDistortion"].GetShader().UseIntensity(3f);
                    YharonAI_FinalDyingRoar(npc);
                    break;
            }
            attackTimer++;
            return false;
        }

        public static void YharonAI_FinalDyingRoar(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;

            Player player = Main.player[npc.target];

            int totalCharges = 6;
            float confusingChargeSpeed = 42f;

            float splittingMeteorBombingSpeed = 24f;
            float splittingMeteorRiseTime = 120f;
            float splittingMeteorBombTime = 90f;
            int totalTimeSpentPerCarpetBomb = (int)(splittingMeteorRiseTime + splittingMeteorBombTime);

            int totalBerserkCharges = 10;
            float berserkChargeSpeed = 50f;

            ref float attackTimer = ref npc.ai[1];
            ref float specialFrameType = ref npc.Infernum().ExtraAI[1];
            ref float finalAttackCompletionState = ref npc.Infernum().ExtraAI[6];
            ref float totalMeteorBomings = ref npc.Infernum().ExtraAI[7];
            ref float fireIntensity = ref npc.Infernum().ExtraAI[9];
            if (attackTimer <= totalCharges * 90)
            {
                if (finalAttackCompletionState != 1f)
                {
                    npc.life = (int)(npc.lifeMax * 0.05);
                    if (Main.netMode == NetmodeID.SinglePlayer)
                        Main.NewText("The heat is surging...", Color.Orange);
                    else if (Main.netMode == NetmodeID.Server)
                        NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("The heat is surging..."), Color.Orange);
                    finalAttackCompletionState = 1f;
                }
                if (attackTimer % 90 < 45)
                {
                    npc.spriteDirection = (npc.Center.X > player.Center.X).ToDirectionInt();
                    ref float xAimOffset = ref npc.Infernum().ExtraAI[4];
                    if (xAimOffset == 0f)
                        xAimOffset = 890f * Math.Sign((npc.Center - player.Center).X);

                    float offsetAngle = 0f;
                    if (attackTimer % 90 >= 20)
                        offsetAngle = MathHelper.Lerp(0f, MathHelper.ToRadians(25f), Utils.InverseLerp(20f, 45f, attackTimer % 90f));

                    Vector2 destination = player.Center + new Vector2(xAimOffset, -890f).RotatedBy(offsetAngle) - npc.Center;
                    Vector2 idealVelocity = Vector2.Normalize(destination - npc.velocity) * 23f;
                    npc.SimpleFlyMovement(idealVelocity, 1f);
                    npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(player.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi, 0.1f);
                    specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                }
                if (attackTimer % 90 == 45)
                {
                    npc.velocity = npc.SafeDirectionTo(player.Center) * confusingChargeSpeed;
                    npc.rotation = npc.AngleTo(player.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;
                    specialFrameType = (int)YharonFrameDrawingType.IdleWings;
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/YharonRoarShort"), npc.Center);
                }
                npc.Infernum().ExtraAI[10] = 3f; // 2 clones and original.
            }
            else if (attackTimer <= totalCharges * 90 + totalTimeSpentPerCarpetBomb)
            {
                // Start to fade into magic sparkles.
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.65f, 0.15f);
                npc.life = (int)MathHelper.Lerp(npc.life, npc.lifeMax * 0.025f, 0.025f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(180f, 180f);
                    Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(12f, 12f), ModContent.ProjectileType<MajesticSparkle>(), 0, 0f);
                }

                float adjustedAttackTimer = attackTimer - totalCharges * 90;
                float directionToDestination = (npc.Center.X > player.Center.X).ToDirectionInt();
                if (adjustedAttackTimer < splittingMeteorRiseTime)
                {
                    Vector2 destination = player.Center + new Vector2(directionToDestination * 750f, -300f);
                    Vector2 idealVelocity = npc.SafeDirectionTo(destination) * splittingMeteorBombingSpeed;

                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.035f);
                    npc.rotation = npc.rotation.AngleTowards(0f, 0.25f);

                    npc.spriteDirection = (npc.Center.X > player.Center.X).ToDirectionInt();

                    if (Vector2.Distance(destination, npc.Center) < 32f)
                        attackTimer = splittingMeteorRiseTime + totalCharges * 90 - 1f;
                    specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                }
                else if (adjustedAttackTimer == splittingMeteorRiseTime)
                {
                    Vector2 velocity = npc.SafeDirectionTo(player.Center);
                    velocity.Y *= 0.15f;
                    velocity = velocity.SafeNormalize(Vector2.UnitX * npc.spriteDirection);

                    specialFrameType = (int)YharonFrameDrawingType.OpenMouth;

                    npc.velocity = velocity * splittingMeteorBombingSpeed * 1.55f;
                }
                else
                {
                    npc.position.X += npc.SafeDirectionTo(player.Center).X * 7f;
                    npc.position.Y += npc.SafeDirectionTo(player.Center + Vector2.UnitY * -400f).Y * 6f;
                    npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
                    npc.rotation = npc.velocity.ToRotation() + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;

                    int fireballReleaseRate = 3;
                    if (adjustedAttackTimer % fireballReleaseRate == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center + npc.velocity * 3f, npc.velocity, ModContent.ProjectileType<YharonFireball>(), 565, 0f, Main.myPlayer, 0f, 0f);
                    if (adjustedAttackTimer >= splittingMeteorRiseTime + splittingMeteorBombTime && totalMeteorBomings < 3)
                    {
                        attackTimer = totalCharges * 90;
                        totalMeteorBomings++;
                    }
                }
            }
            else if (attackTimer <= totalCharges * 90 + totalTimeSpentPerCarpetBomb + totalBerserkCharges * 45)
            {
                finalAttackCompletionState = 0f;

                npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.3f, 0.15f);
                npc.life = (int)MathHelper.Lerp(npc.life, npc.lifeMax * 0.01f, 0.025f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(180f, 180f);
                        Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(12f, 12f), ModContent.ProjectileType<MajesticSparkle>(), 0, 0f);
                    }
                }
                // Longer trail and even more intense fire.
                fireIntensity = 1.5f;

                if (attackTimer % 45 < 20)
                {
                    npc.spriteDirection = (npc.Center.X > player.Center.X).ToDirectionInt();
                    ref float xAimOffset = ref npc.Infernum().ExtraAI[4];
                    if (xAimOffset == 0f)
                        xAimOffset = 870f * Math.Sign((npc.Center - player.Center).X);

                    Vector2 destination = player.Center + new Vector2(xAimOffset, -890f) - npc.Center;
                    Vector2 idealVelocity = Vector2.Normalize(destination - npc.velocity) * 23f;
                    npc.SimpleFlyMovement(idealVelocity, 1f);
                    npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(player.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi, 0.1f);
                    specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                }
                if (attackTimer % 45 == 20)
                {
                    npc.velocity = npc.SafeDirectionTo(player.Center) * berserkChargeSpeed;
                    npc.rotation = npc.AngleTo(player.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;
                    specialFrameType = (int)YharonFrameDrawingType.IdleWings;
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/YharonRoarShort"), npc.Center);
                }
            }
            else
            {
                ref float pulseDeathEffectCooldown = ref npc.Infernum().ExtraAI[5];
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.035f, 0.2f);
                if (finalAttackCompletionState != 1f)
                {
                    npc.spriteDirection = (npc.Center.X > player.Center.X).ToDirectionInt();
                    npc.velocity = npc.SafeDirectionTo(player.Center) * npc.Distance(player.Center) / 40f;
                    npc.rotation = npc.AngleTo(player.Center) + (npc.spriteDirection == 1).ToInt() * MathHelper.Pi;
                    npc.netUpdate = true;
                    finalAttackCompletionState = 1f;
                }
                npc.damage = 0;
                npc.velocity *= 0.97f;
                npc.rotation = npc.rotation.AngleTowards(0f, 0.1f);
                npc.life = (int)MathHelper.Lerp(npc.life, 0, 0.01f);

                if (npc.life <= 3100 && npc.life > 1)
                    npc.life = 1;

                specialFrameType = (int)YharonFrameDrawingType.FlapWings;
                if (lifeRatio < 0.005f && pulseDeathEffectCooldown <= 0)
                {
                    pulseDeathEffectCooldown = 8f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 80; i++)
                        {
                            Vector2 sparkleSpawnPosition = npc.Center + Main.rand.NextVector2Circular(280f, 280f);
                            Utilities.NewProjectileBetter(sparkleSpawnPosition, Main.rand.NextVector2Circular(42f, 42f), ModContent.ProjectileType<MajesticSparkleBig>(), 0, 0f);
                        }
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<YharonBoom>(), 0, 0f);
                    }
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/YharonRoarShort"), npc.Center);
                }
                else if (pulseDeathEffectCooldown > 0)
                {
                    specialFrameType = (int)YharonFrameDrawingType.OpenMouth;
                    pulseDeathEffectCooldown--;
                }

                if (npc.life <= 0)
                {
                    npc.life = 0;
                    npc.HitEffect();
                    npc.checkDead();
                    npc.NPCLoot();
                    npc.active = false;

                    // YOU SHALL HAVE HEARD MY FINAL DYINNNG ROOOOARRRRR
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/YharonRoar"), npc.Center);
                }
            }
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
                    int width = 9000;
                    npc.Infernum().arenaRectangle.X = (int)(player.Center.X - width * 0.5f);
                    npc.Infernum().arenaRectangle.Y = (int)(player.Center.Y - 160000f);
                    npc.Infernum().arenaRectangle.Width = width;
                    npc.Infernum().arenaRectangle.Height = 320000;

                    Projectile.NewProjectile(player.Center.X + width * 0.5f, player.Center.Y + 100f, 0f, 0f, ModContent.ProjectileType<SkyFlareRevenge>(), 0, 0f, Main.myPlayer, 0f, 0f);
                    Projectile.NewProjectile(player.Center.X - width * 0.5f, player.Center.Y + 100f, 0f, 0f, ModContent.ProjectileType<SkyFlareRevenge>(), 0, 0f, Main.myPlayer, 0f, 0f);
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

        public static void GoToNextAttack(NPC npc, ref float attackType)
        {
            ref float attackTypeIndex = ref npc.Infernum().ExtraAI[0];
            attackTypeIndex++;

            YharonAttackType[] patternToUse = SubphaseTable.First(table => table.Value(npc)).Key;
            attackType = (int)patternToUse[(int)(attackTypeIndex % patternToUse.Length)];

            // Reset the attack timer and subphase specific variables.
            npc.ai[1] = 0f;
            npc.Infernum().ExtraAI[4] = 0f;
            npc.Infernum().ExtraAI[5] = 0f;
            npc.Infernum().ExtraAI[6] = 0f;
            npc.Infernum().ExtraAI[7] = 0f;
            npc.Infernum().ExtraAI[8] = 0f;
            npc.netUpdate = true;
        }
        #endregion

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            if ((YharonAttackType)(int)npc.ai[0] == YharonAttackType.SpawnEffects)
            {
                // Open mouth for a little bit and roar.
                if (npc.frameCounter >= 30 &&
                    npc.frameCounter <= 40)
                {
                    npc.frame.Y = 0;
                    if (npc.frameCounter == 35)
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/YharonRoar"), npc.Center);
                }
                // Otherwise flap wings.
                else if (npc.frameCounter % 5 == 4)
                {
                    npc.frame.Y += frameHeight;

                    if (npc.frame.Y > frameHeight * Main.npcFrameCount[npc.type])
                        npc.frame.Y = 0;
                }
            }
            else
            {
                switch ((YharonFrameDrawingType)npc.Infernum().ExtraAI[1])
                {
                    case YharonFrameDrawingType.FlapWings:
                        if (npc.frameCounter % 6 == 5)
                        {
                            npc.frame.Y += frameHeight;
                            if (npc.frame.Y >= 4 * frameHeight)
                                npc.frame.Y = 0;
                        }
                        break;
                    case YharonFrameDrawingType.IdleWings:
                        npc.frame.Y = 5 * frameHeight;
                        break;
                    case YharonFrameDrawingType.Roar:
                        if (npc.frameCounter % 18 < 9)
                            npc.frame.Y = 5 * frameHeight;
                        else npc.frame.Y = 6 * frameHeight;
                        break;
                    case YharonFrameDrawingType.OpenMouth:
                        npc.frame.Y = 5 * frameHeight;
                        break;
                }
            }
            npc.frameCounter++;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            void drawYharon(Vector2 position, float rotationOffset = 0f, bool changeDirection = false)
            {
                Texture2D texture = Main.npcTexture[npc.type];
                Vector2 origin = new Vector2(texture.Width / 2, texture.Height / Main.npcFrameCount[npc.type] / 2);
                Color color = lightColor;
                YharonAttackType attackType = (YharonAttackType)(int)npc.ai[0];

                bool doingMajesticCharge = attackType == YharonAttackType.SpinCharge && npc.velocity.Length() > 32f;

                int afterimageCount = 1;
                bool phase2 = npc.Infernum().ExtraAI[2] == 1f;
                bool inLastSubphases = npc.life / (float)npc.lifeMax <= 0.3f && phase2;

                if (attackType == YharonAttackType.PhoenixSupercharge ||
                    inLastSubphases ||
                    attackType == YharonAttackType.HeatFlash ||
                    doingMajesticCharge ||
                    npc.Infernum().ExtraAI[3] > 0f ||
                    npc.Infernum().ExtraAI[11] > 0f)
                {
                    float fireIntensity = npc.Infernum().ExtraAI[9];
                    if (attackType != YharonAttackType.PhoenixSupercharge && attackType != YharonAttackType.HeatFlash)
                        fireIntensity = 0.75f;

                    if (inLastSubphases)
                        fireIntensity = MathHelper.Max(fireIntensity, 0.8f);

                    afterimageCount += (int)(fireIntensity * 20);
                    color = Color.Lerp(color, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB, 0), fireIntensity);

                    float whiteBlend = MathHelper.Lerp(0.9f, 0.5f, Utils.InverseLerp(0f, 14f, npc.velocity.Length(), true));
                    color = Color.Lerp(color, Color.White, whiteBlend);
                    if (npc.velocity.Length() < 3f)
                    {
                        color *= MathHelper.Lerp(1f, 0.5f + (float)Math.Cos(Main.GlobalTime) * 0.08f, Utils.InverseLerp(3f, 0f, npc.velocity.Length()));
                    }
                    color.A = 0;
                }
                if (doingMajesticCharge)
                {
                    color = Color.Lerp(Color.HotPink, Color.Lavender, (float)Math.Cos(Main.GlobalTime * 3f) * 0.5f + 0.5f);
                    color.A = 0;
                }

                afterimageCount = Utils.Clamp(afterimageCount, 1, 35);
                for (int i = 0; i < afterimageCount; i++)
                {
                    Vector2 drawOffset = Vector2.Zero;
                    if (npc.velocity.Length() < 7f)
                    {
                        float angle = MathHelper.TwoPi * i / afterimageCount;
                        drawOffset = angle.ToRotationVector2() * MathHelper.Lerp(0.5f, 16f + (float)Math.Cos(Main.GlobalTime) * 4f, Utils.InverseLerp(7f, 0f, npc.velocity.Length()));
                    }
                    Color afterimageColor = afterimageCount == 1 ? color : color * (i / (float)afterimageCount);
                    Vector2 drawPosition = position + drawOffset - Main.screenPosition;
                    drawPosition -= npc.velocity * 0.3f * i;
                    SpriteEffects spriteEffects = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                    if (changeDirection)
                        spriteEffects = npc.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    spriteBatch.Draw(texture, drawPosition, npc.frame, afterimageColor * npc.Opacity, npc.rotation + rotationOffset, origin, npc.scale, spriteEffects, 0f);
                }
            }
            int illusionCount = (int)npc.Infernum().ExtraAI[10];
            if (illusionCount == 0)
            {
                drawYharon(npc.Center);
            }
            else
            {
                Player player = Main.player[npc.target];
                for (int i = 0; i < illusionCount; i++)
                {
                    float offsetAngle = MathHelper.TwoPi * i / illusionCount;
                    float distanceFromPlayer = npc.Distance(player.Center);
                    Vector2 directionFromPlayer = npc.DirectionFrom(player.Center);
                    Vector2 drawPosition = Main.player[npc.target].Center + directionFromPlayer.RotatedBy(offsetAngle) * distanceFromPlayer;
                    drawYharon(drawPosition, offsetAngle, (drawPosition.X > player.Center.X).ToDirectionInt() != npc.spriteDirection);
                }
            }

            return false;
        }
        #endregion
    }
}