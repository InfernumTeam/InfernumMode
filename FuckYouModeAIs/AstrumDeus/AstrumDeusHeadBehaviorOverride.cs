using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode.FuckYouModeAIs.AstrumDeus
{
    public class AstrumDeusHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum DeusAttackType
        {
            AstralBombs,
            StellarCrash,
            CelestialLights,
            WarpCharge,
            StarWeave,
            InfectedPlasmaVomit
        }

        public const float Phase2LifeThreshold = 0.55f;

        public override int NPCOverrideType => ModContent.NPCType<AstrumDeusHeadSpectral>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            // Emit a pale white light idly.
            Lighting.AddLight(npc.Center, 0.3f, 0.3f, 0.3f);

            // Select a new target if an old one was lost.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            // Reset damage. Do none by default if somewhat transparent.
            npc.damage = npc.alpha > 40 ? 0 : npc.defDamage;

            // If none was found or it was too far away, despawn.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || Main.dayTime || !Main.player[npc.target].active || !npc.WithinRange(Main.player[npc.target].Center, 7800f))
            {
                DoBehavior_Despawn(npc);
                return false;
            }

            // Create a beacon if none exists.
            List<Projectile> beacons = Utilities.AllProjectilesByID(ModContent.ProjectileType<DeusRitualDrama>()).ToList();
            if (beacons.Count == 0)
            {
                Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<DeusRitualDrama>(), 0, 0f);
                beacons = Utilities.AllProjectilesByID(ModContent.ProjectileType<DeusRitualDrama>()).ToList();
            }

            Player target = Main.player[npc.target];

            float lifeRatio = npc.life / (float)npc.lifeMax;
            float beaconAngerFactor = Utils.InverseLerp(2400f, 5600f, target.Distance(beacons.First().Center), true);
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float hasCreatedSegments = ref npc.localAI[0];
            ref float releasingParticlesFlag = ref npc.localAI[1];

            // Save the beacon anger factor in a variable.
            npc.Infernum().ExtraAI[6] = beaconAngerFactor;

            // Create segments and initialize on the first frame.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasCreatedSegments == 0f)
            {
                CreateSegments(npc, 64, ModContent.NPCType<AstrumDeusBodySpectral>(), ModContent.NPCType<AstrumDeusTailSpectral>());
                attackType = (int)DeusAttackType.AstralBombs;
                hasCreatedSegments = 1f;
                npc.netUpdate = true;
            }

            // Clamp position into the world.
            npc.position.Y = MathHelper.Clamp(npc.position.Y, 600f, Main.maxTilesY * 16f - 600f);

            // Quickly fade in.
            npc.alpha = Utils.Clamp(npc.alpha - 16, 0, 255);

            switch ((DeusAttackType)(int)attackType)
            {
                case DeusAttackType.AstralBombs:
                    DoBehavior_AstralBombs(npc, target, beaconAngerFactor, lifeRatio, attackTimer);
                    break;
                case DeusAttackType.StellarCrash:
                    DoBehavior_StellarCrash(npc, target, beaconAngerFactor, lifeRatio, ref attackTimer);
                    break;
                case DeusAttackType.CelestialLights:
                    DoBehavior_CelestialLights(npc, target, beaconAngerFactor, lifeRatio, attackTimer);
                    break;
                case DeusAttackType.WarpCharge:
                    DoBehavior_WarpCharge(npc, target, beaconAngerFactor, lifeRatio, attackTimer, ref releasingParticlesFlag);
                    break;
                case DeusAttackType.StarWeave:
                    DoBehavior_StarWeave(npc, target, beaconAngerFactor, ref attackTimer);
                    break;
                case DeusAttackType.InfectedPlasmaVomit:
                    DoBehavior_InfectedPlasmaVomit(npc, target, beaconAngerFactor, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        #region Custom Behaviors

        public static void DoBehavior_Despawn(NPC npc)
        {
            // Ascend into the sky and disappear.
            npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 26f, 0.08f);
            npc.velocity.X *= 0.975f;
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Cap the despawn timer so that the boss can swiftly disappear.
            npc.timeLeft = Utils.Clamp(npc.timeLeft - 1, 0, 120);

            if (npc.timeLeft <= 0)
            {
                npc.life = 0;
                npc.active = false;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_AstralBombs(NPC npc, Player target, float beaconAngerFactor, float lifeRatio, float attackTimer)
        {
            int shootRate = (int)MathHelper.Lerp(10f, 7f, 1f - lifeRatio);
            int totalBombsToShoot = lifeRatio < Phase2LifeThreshold ? 56 : 45;
            float flySpeed = MathHelper.Lerp(12f, 16f, 1f - lifeRatio) + beaconAngerFactor * 10f;
            float flyAcceleration = MathHelper.Lerp(0.028f, 0.034f, 1f - lifeRatio) + beaconAngerFactor * 0.036f;
            int shootTime = shootRate * totalBombsToShoot;
            int attackSwitchDelay = lifeRatio < Phase2LifeThreshold ? 45 : 105;

            // Initialize movement if it is too low.
            if (npc.velocity == Vector2.Zero || npc.velocity.Length() < 5f)
                npc.velocity = npc.velocity.SafeNormalize(-Vector2.UnitY) * 5.25f;

            // Drift towards the player. Contact damage is possible, but should be of little threat.
            if (!npc.WithinRange(target.Center, 325f))
            {
                float newSpeed = MathHelper.Lerp(npc.velocity.Length(), flySpeed, 0.075f);
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), flyAcceleration, true) * newSpeed;
            }

            // Release astral bomb mines from the sky.
            // They become faster/closer at specific life thresholds.
            if (attackTimer % shootRate == 0 && attackTimer < shootTime)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float bombShootOffset = lifeRatio < Phase2LifeThreshold ? 850f : 1050f;
                    Vector2 bombShootPosition = target.Center - Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2) * bombShootOffset;
                    Vector2 bombShootVelocity = (target.Center - bombShootPosition).SafeNormalize(Vector2.UnitY) * 11f;
                    if (lifeRatio < Phase2LifeThreshold)
                        bombShootVelocity *= 1.4f;

                    Utilities.NewProjectileBetter(bombShootPosition, bombShootVelocity, ModContent.ProjectileType<DeusMine2>(), 155, 0f);
                }
            }

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (attackTimer > shootTime + attackSwitchDelay)
                GotoNextAttackState(npc);
        }

        public static void DoBehavior_StellarCrash(NPC npc, Player target, float beaconAngerFactor, float lifeRatio, ref float attackTimer)
        {
            int totalCrashes = lifeRatio < Phase2LifeThreshold ? 4 : 3;
            int crashRiseTime = lifeRatio < Phase2LifeThreshold ? 215 : 275;
            int crashChargeTime = lifeRatio < Phase2LifeThreshold ? 100 : 95;
            float crashSpeed = MathHelper.Lerp(32.5f, 37f, 1f - lifeRatio) + beaconAngerFactor * 22f;
            float wrappedTime = attackTimer % (crashRiseTime + crashChargeTime);

            // Rise upward and release redirecting astral bombs.
            if (wrappedTime < crashRiseTime)
            {
                Vector2 riseDestination = target.Center - Vector2.UnitY * 1250f;
                if (!npc.WithinRange(riseDestination, 65f))
                {
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(riseDestination), 0.035f);
                    npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(riseDestination) * (27.25f + beaconAngerFactor * 14f), 0.6f);
                }
                else
                    attackTimer += crashRiseTime - wrappedTime;

                if (npc.WithinRange(target.Center, 105f))
                    npc.Center = target.Center + target.SafeDirectionTo(npc.Center, -Vector2.UnitY) * 105f;

                if (attackTimer % 90f == 89f)
                {
                    Main.PlaySound(SoundID.Item73, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 shootVelocity = Vector2.Lerp(npc.velocity.SafeNormalize(-Vector2.UnitY), -Vector2.UnitY, 0.75f) * Main.rand.NextFloat(9f, 12f);
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<AstralFlame2>(), 155, 0f);
                    }
                }
            }

            // Attempt to crash into the target from above after releasing flames as a dive-bomb.
            else
            {
                if (wrappedTime - crashRiseTime < 16)
                {
                    npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(target.Center + target.velocity * 20f) * crashSpeed, crashSpeed * 0.11f);
                    if (Main.netMode != NetmodeID.MultiplayerClient && wrappedTime % 6f == 0f)
                    {
                        Vector2 shootVelocity = Vector2.Lerp(npc.velocity.SafeNormalize(-Vector2.UnitY), npc.SafeDirectionTo(target.Center), 0.9f) * Main.rand.NextFloat(12f, 14f);
                        shootVelocity = shootVelocity.RotatedByRandom(0.46f);
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<AstralFlame2>(), 155, 0f);
                    }
                }

                if (wrappedTime == crashRiseTime + 8f)
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/AstrumDeusSplit"), target.Center);
            }

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (attackTimer > totalCrashes * (crashRiseTime + crashChargeTime) + 40)
                GotoNextAttackState(npc);
        }

        public static void DoBehavior_CelestialLights(NPC npc, Player target, float beaconAngerFactor, float lifeRatio, float attackTimer)
        {
            float flySpeed = MathHelper.Lerp(12.5f, 17f, 1f - lifeRatio);
            float flyAcceleration = MathHelper.Lerp(0.03f, 0.038f, 1f - lifeRatio);
            int shootRate = lifeRatio < Phase2LifeThreshold ? 3 : 7;

            ref float starCounter = ref npc.Infernum().ExtraAI[0];

            // Initialize movement if it is too low.
            if (npc.velocity == Vector2.Zero || npc.velocity.Length() < 5f)
                npc.velocity = npc.velocity.SafeNormalize(-Vector2.UnitY) * 5.25f;

            // Drift towards the player. Contact damage is possible, but should be of little threat.
            if (!npc.WithinRange(target.Center, 325f))
            {
                float newSpeed = MathHelper.Lerp(npc.velocity.Length(), flySpeed, 0.08f);
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), flyAcceleration, true) * newSpeed;
            }

            if (attackTimer > 60 && attackTimer % shootRate == shootRate - 1f)
            {
                int bodyType = ModContent.NPCType<AstrumDeusBodySpectral>();
                int shootIndex = (int)(starCounter * 2f);
                Vector2 starSpawnPosition = Vector2.Zero;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].ai[2] != shootIndex || Main.npc[i].type != bodyType || !Main.npc[i].active)
                        continue;

                    starSpawnPosition = Main.npc[i].Center;
                    break;
                }

                Main.PlaySound(SoundID.Item9, starSpawnPosition);
                if (Main.netMode != NetmodeID.MultiplayerClient && starSpawnPosition != Vector2.Zero)
                {
                    Vector2 starVelocity = -Vector2.UnitY.RotatedByRandom(0.37f) * Main.rand.NextFloat(5f, 6f);
                    starVelocity *= MathHelper.Lerp(1f, 1.8f, beaconAngerFactor);
                    Utilities.NewProjectileBetter(starSpawnPosition, starVelocity, ModContent.ProjectileType<AstralStar>(), 160, 0f);
                }

                starCounter++;
            }

            if (attackTimer >= 60f + shootRate * 30f + 320f)
                GotoNextAttackState(npc);

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public static void DoBehavior_WarpCharge(NPC npc, Player target, float beaconAngerFactor, float lifeRatio, float attackTimer, ref float releasingParticlesFlag)
        {
            int fadeInTime = lifeRatio < Phase2LifeThreshold ? 67 : 85;
            int chargeTime = lifeRatio < Phase2LifeThreshold ? 38 : 45;
            int fadeOutTime = fadeInTime / 2 + 10;
            int chargeCount = lifeRatio < Phase2LifeThreshold ? 5 : 4;
            float teleportOutwardness = MathHelper.Lerp(1360f, 1175f, 1f - lifeRatio) - beaconAngerFactor * 400f;
            float chargeSpeed = MathHelper.Lerp(35f, 40f, 1f - lifeRatio) + beaconAngerFactor * 10f;
            float wrappedTimer = attackTimer % (fadeInTime + chargeTime + fadeOutTime);

            if (wrappedTimer < fadeInTime)
            {
                // Drift towards the player. Contact damage is possible, but should be of little threat.
                if (!npc.WithinRange(target.Center, 375f) && wrappedTimer < 45f)
                {
                    float newSpeed = MathHelper.Lerp(npc.velocity.Length(), 16f, 0.085f);
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.032f, true) * newSpeed;
                }
                else if (wrappedTimer >= 45f)
                {
                    if (npc.velocity.Length() < 26f)
                        npc.velocity *= 1.015f;
                }

                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.13f, 0f, 1f);
                if (wrappedTimer == fadeInTime - 1f)
                {
                    Vector2 teleportOffsetDirection;
                    do
                        teleportOffsetDirection = Main.rand.NextVector2Unit();
                    while (teleportOffsetDirection.AngleBetween(target.velocity.SafeNormalize(Vector2.UnitY)) > MathHelper.Pi * 0.35f);

                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/AstrumDeusSplit"), target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.Center = target.Center + Main.rand.NextVector2CircularEdge(teleportOutwardness, teleportOutwardness);
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.netUpdate = true;

                        for (int i = 0; i < 5; i++)
                        {
                            Vector2 fireVelocity = (MathHelper.TwoPi * i / 5f).ToRotationVector2() * 7f;
                            Utilities.NewProjectileBetter(npc.Center, fireVelocity, ModContent.ProjectileType<AstralFlame2>(), 155, 0f);
                        }

                        BringAllSegmentsToNPCPosition(npc);
                    }
                }
            }
            else if (wrappedTimer < fadeInTime + chargeTime)
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.15f, 0f, 1f);
            else if (wrappedTimer < fadeInTime + chargeTime + fadeOutTime)
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.06f, 0f, 1f);
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.05f);

                // Teleport near the player again at the end of the cycle.
                if (wrappedTimer == fadeInTime + chargeTime + fadeOutTime)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.Center = target.Center + Main.rand.NextVector2CircularEdge(teleportOutwardness, teleportOutwardness) * 1.75f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.netUpdate = true;

                        BringAllSegmentsToNPCPosition(npc);
                    }
                }
            }

            // Release particles when disappearing.
            releasingParticlesFlag = (npc.Opacity < 0.5f && npc.Opacity > 0f).ToInt();

            if (attackTimer >= chargeCount * (fadeInTime + chargeTime + fadeOutTime) + 3)
                GotoNextAttackState(npc);

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public static void DoBehavior_StarWeave(NPC npc, Player target, float beaconAngerFactor, ref float attackTimer)
        {
            // Apply the extreme gravity debuff.
            if (Main.netMode != NetmodeID.Server)
                target.AddBuff(ModContent.BuffType<ExtremeGrav>(), 25);

            float spinSpeed = 34f;
            ref float cantKeepSpinningFlag = ref npc.Infernum().ExtraAI[0];

            if (attackTimer < 60f && !npc.WithinRange(target.Center, 1200f))
            {
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(target.Center) * 23f, 1.5f);
                attackTimer = 30f;
            }
            else if (npc.velocity.Length() < spinSpeed && attackTimer < 90f)
                npc.velocity = npc.velocity.SafeNormalize(-Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), spinSpeed, 0.075f);

            if (cantKeepSpinningFlag == 0f)
                npc.velocity = npc.velocity.RotatedBy(MathHelper.TwoPi / 125f);

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 60f)
            {
                Vector2 starSpawnPosition = npc.Center + npc.velocity.RotatedBy(MathHelper.PiOver2) * 136f / MathHelper.TwoPi;
                int star = Utilities.NewProjectileBetter(starSpawnPosition, Vector2.Zero, ModContent.ProjectileType<GiantAstralStar>(), 250, 0f);
                if (Main.projectile.IndexInRange(star))
                    Main.projectile[star].localAI[0] = beaconAngerFactor;
            }

            List<Projectile> stars = Utilities.AllProjectilesByID(ModContent.ProjectileType<GiantAstralStar>()).ToList();
            if (attackTimer > 60f)
            {
                if (stars.Count > 0 && stars.First().scale < 7f)
                {
                    cantKeepSpinningFlag = 0f;
                    attackTimer = 60f;
                }
            }

            // If an opening is found to hit the target and the star is fully charged, fly towards the target and send the star towards them too.
            bool aimedTowardsPlayer = Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(target.Center)) > 0.95f;
            if (aimedTowardsPlayer && stars.Count > 0 && stars.First().velocity == Vector2.Zero && attackTimer > 90f)
            {
                stars.First().velocity = stars.First().SafeDirectionTo(target.Center) * MathHelper.Lerp(4f, 8f, beaconAngerFactor);
                stars.First().netUpdate = true;

                cantKeepSpinningFlag = 1f;
                npc.velocity = npc.SafeDirectionTo(target.Center) * MathHelper.Lerp(24f, 38f, beaconAngerFactor);
                npc.netUpdate = true;
            }

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (attackTimer > 90f && !npc.WithinRange(target.Center, 270f))
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(target.Center) * (15f + beaconAngerFactor * 5f), 0.7f);

            if (attackTimer > 270f)
                GotoNextAttackState(npc);
        }

        public static void DoBehavior_InfectedPlasmaVomit(NPC npc, Player target, float beaconAngerFactor, ref float attackTimer)
        {
            int shootRate = (int)MathHelper.Lerp(85f, 45f, beaconAngerFactor);
            ref float shootTimer = ref npc.Infernum().ExtraAI[0];

            // Drift towards the player.
            if (!npc.WithinRange(target.Center, 530f))
            {
                float newSpeed = MathHelper.Lerp(npc.velocity.Length(), 17f, 0.085f);
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.036f, true) * newSpeed;
            }

            // Release bursts of plasma.
            if (shootTimer >= shootRate)
            {
                Main.PlaySound(SoundID.Item74, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 plasmaVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.56f) * Main.rand.NextFloat(14f, 18f);
                        Utilities.NewProjectileBetter(npc.Center + plasmaVelocity * 2f, plasmaVelocity, ModContent.ProjectileType<InfectiousPlasma>(), 160, 0f);
                    }
                    shootTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            if (attackTimer >= 360f)
                GotoNextAttackState(npc);

            shootTimer++;
        }
        #endregion Custom Behaviors

        #region Misc AI Operations
        public static void GotoNextAttackState(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            DeusAttackType oldAttackState = (DeusAttackType)(int)npc.ai[0];
            DeusAttackType secondLastAttackState = (DeusAttackType)(int)npc.ai[3];
            DeusAttackType newAttackState;
            WeightedRandom<DeusAttackType> attackSelector = new WeightedRandom<DeusAttackType>();
            attackSelector.Add(DeusAttackType.AstralBombs, lifeRatio < Phase2LifeThreshold ? 0.5f : 0.8f);
            attackSelector.Add(DeusAttackType.StellarCrash, 1f);
            attackSelector.Add(DeusAttackType.CelestialLights, 1.15f);
            attackSelector.Add(DeusAttackType.WarpCharge, 1f);
            if (lifeRatio < Phase2LifeThreshold)
            {
                attackSelector.Add(DeusAttackType.StarWeave, 0.9f);
                attackSelector.Add(DeusAttackType.InfectedPlasmaVomit, 1.1f);
            }

            do
                newAttackState = attackSelector.Get();
            while (newAttackState == oldAttackState || newAttackState == secondLastAttackState);

            npc.ai[0] = (int)newAttackState;
            npc.ai[1] = 0f;
            npc.ai[3] = (int)oldAttackState;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        public static void CreateSegments(NPC npc, int wormLength, int bodyType, int tailType)
        {
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < wormLength; i++)
            {
                int nextIndex;
                if (i < wormLength - 1)
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, bodyType, npc.whoAmI);
                else
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI);

                Main.npc[nextIndex].realLife = npc.whoAmI;
                Main.npc[nextIndex].ai[2] = i;
                Main.npc[nextIndex].ai[1] = npc.whoAmI;
                Main.npc[nextIndex].ai[0] = previousIndex;
                if (i < wormLength - 1)
                    Main.npc[nextIndex].localAI[3] = i % 2;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

        public static void BringAllSegmentsToNPCPosition(NPC npc)
        {
            int segmentCount = 0;
            int bodyType = ModContent.NPCType<AstrumDeusBodySpectral>();
            int tailType = ModContent.NPCType<AstrumDeusTailSpectral>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && (Main.npc[i].type == bodyType || Main.npc[i].type == tailType))
                {
                    Main.npc[i].Center = npc.Center - (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * segmentCount;
                    Main.npc[i].netUpdate = true;
                    segmentCount++;
                }
            }
        }

        #endregion Misc AI Operations

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            return true;
        }
    }
}
