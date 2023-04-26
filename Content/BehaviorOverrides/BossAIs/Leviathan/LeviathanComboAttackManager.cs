using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

using LeviathanNPC = CalamityMod.NPCs.Leviathan.Leviathan;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Leviathan
{
    public enum LeviAnahitaFightState
    {
        AnahitaFirstPhase,
        LeviathanAlone,
        AnahitaLeviathanTogether,
        AloneEnraged
    }

    public enum LeviathanComboAttackType
    {
        UpwardRedirectingWaterSpears = 100,
        ExoTwinsBasicShotsPrecursor,
        AngeringSong
    }

    public static class LeviathanComboAttackManager
    {
        public static int WaterSpearDamage => 170;

        public static int LullabyDamage => 170;

        public static int FrostMistDamage => 175;

        public static int LeviathanVomitDamage => 175;

        public static int AquaticAberrationDamage => 190;

        public static int AtlantisSpearDamage => 195;

        public static int LeviathanMeteorDamage => 195;

        public static NPC LeaderOfFight
        {
            get
            {
                if (CalamityGlobalNPC.siren != -1)
                    return Main.npc[CalamityGlobalNPC.siren];
                return Main.npc[CalamityGlobalNPC.leviathan];
            }
        }

        public static LeviAnahitaFightState FightState
        {
            get
            {
                NPC siren = CalamityGlobalNPC.siren >= 0 ? Main.npc[CalamityGlobalNPC.siren] : null;
                NPC leviathan = CalamityGlobalNPC.leviathan >= 0 ? Main.npc[CalamityGlobalNPC.leviathan] : null;
                float leviathanLifeRatio = 0f;
                if (leviathan != null)
                    leviathanLifeRatio = leviathan.life / (float)leviathan.lifeMax;

                if (Utilities.AnyProjectiles(ModContent.ProjectileType<LeviathanSpawner>()))
                    return LeviAnahitaFightState.LeviathanAlone;

                if (siren != null && leviathan != null)
                    return leviathanLifeRatio <= AnahitaReturnLifeRatio ? LeviAnahitaFightState.AnahitaLeviathanTogether : LeviAnahitaFightState.LeviathanAlone;
                if (siren != null && siren.ai[3] == 0f)
                    return LeviAnahitaFightState.AnahitaFirstPhase;
                return LeviAnahitaFightState.AloneEnraged;
            }
        }

        public const float LeviathanSummonLifeRatio = 0.5f;

        public const float AnahitaReturnLifeRatio = 0.5f;

        public static void InheritAttributesFromLeader(NPC npc)
        {
            bool needsToPickNewAttack = false;

            // Pick a new attack if a combo attack is not in use but both bosses are present.
            if (npc.ai[0] < 100f && npc == LeaderOfFight && FightState == LeviAnahitaFightState.AnahitaLeviathanTogether)
                needsToPickNewAttack = true;

            // Pick a new attack if alone, exiting combo attack states.
            if (npc.ai[0] >= 100f && FightState != LeviAnahitaFightState.AnahitaLeviathanTogether)
                needsToPickNewAttack = true;

            if (needsToPickNewAttack)
                SelectNextAttackBase(npc);

            bool alone = FightState == LeviAnahitaFightState.LeviathanAlone;
            if (npc == LeaderOfFight || alone)
                return;

            // Inherit the attack state and timer. Also sync if the leader decides to.
            npc.ai[0] = LeaderOfFight.ai[0];
            npc.ai[1] = LeaderOfFight.ai[1];
            npc.target = LeaderOfFight.target;
            if (LeaderOfFight.netUpdate)
                npc.netUpdate = true;
        }

        public static void DoComboAttacks(NPC npc, Player target, ref float attackTimer)
        {
            switch ((int)npc.ai[0])
            {
                case (int)LeviathanComboAttackType.UpwardRedirectingWaterSpears:
                    DoBehavior_UpwardRedirectingWaterSpears(npc, target, ref attackTimer);
                    break;
                case (int)LeviathanComboAttackType.ExoTwinsBasicShotsPrecursor:
                    DoBehavior_ExoTwinsBasicShotsPrecursor(npc, target, ref attackTimer);
                    break;
                case (int)LeviathanComboAttackType.AngeringSong:
                    DoBehavior_AngeringSong(npc, target, ref attackTimer);
                    break;
            }
        }

        public static void DoBehavior_UpwardRedirectingWaterSpears(NPC npc, Player target, ref float attackTimer)
        {
            int redirectTime = 96;
            int spearReleaseRate = 9;
            int chargeTime = 64;
            int slowdownTime = 30;
            int chargeCount = 3;
            float chargeSpeed = 35f;
            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];

            // Have the Leviathan hover a bit above the side of the target and have Anahita move towards riding on her back.
            Vector2 backOfLeviathan = Main.npc[CalamityGlobalNPC.leviathan].Center + new Vector2(npc.spriteDirection * 120f, -225f);
            if (attackTimer <= redirectTime)
            {
                // Determine direction.
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                float flySpeedInterpolant = 0.04f;
                Vector2 hoverDestination = target.Center + new Vector2(Math.Sign(target.Center.X - npc.Center.X) * -1000f, -100f);
                if (npc.type == ModContent.NPCType<Anahita>())
                {
                    hoverDestination = backOfLeviathan;
                    flySpeedInterpolant = 0.05f;
                    npc.rotation = npc.velocity.X * 0.015f;
                }
                else
                {
                    // Have the Leviathan roar before the charge.
                    if (attackTimer == redirectTime - 36)
                        SoundEngine.PlaySound(LeviathanNPC.RoarMeteorSound, npc.Center);
                    npc.damage = 0;
                }

                float hoverSpeed = BossRushEvent.BossRushActive ? 29f : 19f;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 60f);
                if (flySpeedInterpolant > 0f)
                    npc.Center = Vector2.Lerp(npc.Center, hoverDestination, flySpeedInterpolant);

                // Charge.
                if (attackTimer == redirectTime)
                {
                    npc.velocity = Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * chargeSpeed;
                    npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
                    npc.netUpdate = true;
                }
                return;
            }

            // Glue Anahita's position to the back of the Leviathan and fire redirecting spears upward.
            if (npc.type == ModContent.NPCType<Anahita>())
            {
                npc.Center = backOfLeviathan;
                npc.rotation = 0f;
                npc.velocity = Vector2.Zero;
                npc.spriteDirection = Main.npc[CalamityGlobalNPC.leviathan].spriteDirection;

                // Release spears.
                if (attackTimer % spearReleaseRate == spearReleaseRate - 1f && attackTimer <= redirectTime + chargeTime)
                {
                    SoundEngine.PlaySound(SoundID.Item66, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 shootVelocity = -Vector2.UnitY * 15.5f;
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 4f, shootVelocity, ModContent.ProjectileType<RedirectingWaterBolt>(), WaterSpearDamage, 0f);
                    }
                }
            }

            if (attackTimer >= redirectTime + chargeTime)
                npc.velocity *= 0.95f;

            if (attackTimer >= redirectTime + chargeTime + slowdownTime)
            {
                chargeCounter++;
                attackTimer = 0f;
                if (chargeCounter >= chargeCount)
                    SelectNextAttackBase(npc);
                npc.netUpdate = true;
            }
        }

        // Well this method name didn't age well, huh!
        public static void DoBehavior_ExoTwinsBasicShotsPrecursor(NPC npc, Player target, ref float attackTimer)
        {
            int redirectTime = 96;
            int shootRate = 65;
            int shootTime = 325;
            Vector2 hoverDestination = target.Center + Vector2.UnitX * -1000f;
            if (npc.type == ModContent.NPCType<Anahita>())
                hoverDestination = target.Center + Vector2.UnitX * 550f;

            // Have Anahita use rotation.
            if (npc.type == ModContent.NPCType<Anahita>())
                npc.rotation = npc.velocity.X * 0.015f;

            // Determine direction.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            // Hover.
            float hoverSpeed = BossRushEvent.BossRushActive ? 29f : 19f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 80f);

            if (attackTimer <= redirectTime)
                return;

            // Have Anahita shoot frost mist and have the Leviathan shoot an exploding meteor.
            bool canShoot = attackTimer < redirectTime + shootTime && !npc.WithinRange(target.Center, 250f);
            if (canShoot)
            {
                bool shootInterval = attackTimer % shootRate == shootRate - 1f;
                if (npc.type == ModContent.NPCType<Anahita>())
                {
                    if (!shootInterval)
                        return;

                    Vector2 headPosition = npc.Center + new Vector2(npc.spriteDirection * 16f, -42f);
                    SoundEngine.PlaySound(SoundID.Item60, headPosition);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 shootVelocity = (target.Center - headPosition).SafeNormalize(Vector2.UnitY) * 13.5f;
                        Utilities.NewProjectileBetter(headPosition, shootVelocity, ModContent.ProjectileType<FrostMist>(), FrostMistDamage, 0f);
                    }
                }

                else
                {
                    npc.localAI[0] = 1f;
                    npc.damage = 0;
                    if (!shootInterval)
                        return;

                    Vector2 mouthPosition = npc.Center + new Vector2(npc.spriteDirection * 380f, -45f);
                    SoundEngine.PlaySound(SoundID.Item73, mouthPosition);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 shootVelocity = (target.Center - mouthPosition).SafeNormalize(Vector2.UnitY) * 16f;
                        Utilities.NewProjectileBetter(mouthPosition, shootVelocity, ModContent.ProjectileType<LeviathanMeteor>(), LeviathanMeteorDamage, 0f);
                    }
                }
            }

            if (attackTimer >= redirectTime + shootTime + 50f)
                SelectNextAttackBase(npc);
        }

        public static void DoBehavior_AngeringSong(NPC npc, Player target, ref float attackTimer)
        {
            int slowdownTime = 25;
            int redirectTime = 35;
            int chargeTime = 28;
            int aberrationSpawnRate = 10;
            int chargeCount = 3;
            int attackTime = (redirectTime + chargeTime + slowdownTime) * chargeCount;

            // Have the Leviathan charge back and forth and summon redirecting aberrations.
            if (npc.type == ModContent.NPCType<LeviathanNPC>())
            {
                float wrappedAttackTimer = attackTimer % (redirectTime + chargeTime + slowdownTime);
                if (wrappedAttackTimer < redirectTime)
                {
                    npc.damage = 0;

                    Vector2 destination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * 1000f;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 14f, 0.27f);
                    npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, npc.SafeDirectionTo(destination).Y * 30f, 0.18f);
                    npc.spriteDirection = npc.direction;

                    // Roar before charging.
                    if (wrappedAttackTimer == redirectTime / 2)
                        SoundEngine.PlaySound(LeviathanNPC.RoarChargeSound, npc.Center);
                }

                // Initiate the charge.
                if (wrappedAttackTimer == redirectTime)
                {
                    float chargeSpeed = 32.5f;
                    if (BossRushEvent.BossRushActive)
                        chargeSpeed *= 1.3f;
                    npc.velocity = Vector2.UnitX * npc.direction * chargeSpeed;
                }

                // Summon aberrations while charging.
                if (wrappedAttackTimer >= redirectTime && attackTimer < redirectTime + chargeTime && attackTimer % aberrationSpawnRate == aberrationSpawnRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.Zombie54, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 aberrationSpawnPosition = npc.Center + Main.rand.NextVector2Circular(50f, 16f);
                        Vector2 aberrationVelocity = (target.Center - aberrationSpawnPosition).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.43f) * 13.6f;
                        Utilities.NewProjectileBetter(aberrationSpawnPosition, aberrationVelocity, ModContent.ProjectileType<AquaticAberrationProj>(), AquaticAberrationDamage, 0f);
                    }
                }

                // Slow down after charging.
                if (wrappedAttackTimer >= redirectTime + chargeTime)
                    npc.velocity *= 0.95f;
            }

            // Have Anahita hover above the target, releasing occasional song notes.
            else
            {
                int noteReleaseRate = 32;
                float songShootSpeed = 10f;
                Vector2 headPosition = npc.Center + new Vector2(npc.spriteDirection * 16f, -42f);
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 175f, -400f) - npc.velocity;
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 16f;
                npc.SimpleFlyMovement(idealVelocity, 0.3f);

                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

                if (!npc.WithinRange(target.Center, 250f) && attackTimer % noteReleaseRate == noteReleaseRate - 1f)
                {
                    Main.musicPitch = Main.rand.NextFloatDirection() * 0.25f;
                    SoundEngine.PlaySound(SoundID.Item26, target.position);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 songShootVelocity = (target.Center - headPosition).SafeNormalize(Vector2.UnitY) * songShootSpeed;
                        Utilities.NewProjectileBetter(headPosition, songShootVelocity, ModContent.ProjectileType<HeavenlyLullaby>(), LullabyDamage, 0f);
                    }
                }
            }

            if (attackTimer >= attackTime)
                SelectNextAttackBase(npc);
        }

        public static void SelectNextAttackBase(NPC npc)
        {
            if (npc.whoAmI == CalamityGlobalNPC.siren)
                AnahitaBehaviorOverride.SelectNextAttack(npc);
            if (npc.whoAmI == CalamityGlobalNPC.leviathan)
                LeviathanBehaviorOverride.SelectNextAttack(npc);
        }

        public static void SelectNextAttackSpecific(NPC npc)
        {
            if (FightState != LeviAnahitaFightState.AnahitaLeviathanTogether || npc != LeaderOfFight)
                return;

            npc.Infernum().ExtraAI[6]++;
            switch ((int)(npc.Infernum().ExtraAI[6] % 3))
            {
                case 0:
                    npc.ai[0] = (int)LeviathanComboAttackType.ExoTwinsBasicShotsPrecursor;
                    break;
                case 1:
                    npc.ai[0] = (int)LeviathanComboAttackType.UpwardRedirectingWaterSpears;
                    break;
                case 2:
                    npc.ai[0] = (int)LeviathanComboAttackType.AngeringSong;
                    break;
            }
        }
    }
}
