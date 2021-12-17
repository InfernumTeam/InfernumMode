using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using DraedonNPC = CalamityMod.NPCs.ExoMechs.Draedon;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ApolloBehaviorOverride;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ThanatosHeadBehaviorOverride;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.AresBodyBehaviorOverride;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ExoMechManagement;
using System;
using InfernumMode.BehaviorOverrides.BossAIs.Twins;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public static class ExoMechComboAttackContent
    {
        public enum ExoMechComboAttackType
        {
            AresTwins_PressureLaser = 100
        }

        public static bool ShouldSelectComboAttack(NPC npc, out ExoMechComboAttackType newAttack)
        {
            // Use a fallback for the attack.
            newAttack = 0;

            // If the initial mech is not present for some reason, stop attack selections.
            NPC initialMech = FindInitialMech();
            if (initialMech is null)
                return false;

            int complementMechIndex = (int)initialMech.Infernum().ExtraAI[ComplementMechIndexIndex];
            NPC complementMech = complementMechIndex >= 0 && Main.npc[complementMechIndex].active ? Main.npc[complementMechIndex] : null;

            // If the complement mech isn't present, stop attack seletions.
            if (complementMech is null)
                return false;

            bool aresAndTwins = initialMech.type == ModContent.NPCType<Apollo>() && complementMech.type == ModContent.NPCType<AresBody>();
            bool thanatosAndAres = (initialMech.type == ModContent.NPCType<ThanatosHead>() && complementMech.type == ModContent.NPCType<AresBody>()) ||
                (initialMech.type == ModContent.NPCType<AresBody>() && complementMech.type == ModContent.NPCType<ThanatosHead>());

            if (aresAndTwins)
            {
                newAttack = ExoMechComboAttackType.AresTwins_PressureLaser;
                return true;
            }
            if (thanatosAndAres)
            {
                return true;
            }

            return false;
        }

        public static bool DoBehavior_AresTwins_PressureLaser(NPC npc, Player target, float twinsHoverSide, ref float attackTimer, ref float frame)
        {
            int attackDelay = 120;
            int pressureTime = 240;
            int laserTelegraphTime = AresBeamTelegraph.Lifetime;
            int laserReleaseTime = AresDeathray.Lifetime;

            // Inherit the attack timer from the initial mech.
            attackTimer = FindInitialMech()?.ai[1] ?? attackTimer;

            // Have Artemis hover below the player, release a laserbeam, and rise upward.
            if (npc.type == ModContent.NPCType<Artemis>())
            {
                // Play a charge sound as a telegraph.
                if (attackTimer == 1f)
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/CrystylCharge"), target.Center);

                // And create some fire dust on the eye crystal as a telegraph.
                if (attackTimer > 30f && attackTimer < attackDelay)
                {
                    int dustCount = attackTimer > attackDelay * 0.65f ? 3 : 1;
                    Vector2 dustSpawnCenter = npc.Center + (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * 80f;
                    for (int i = 0; i < dustCount; i++)
                    {
                        float scale = Main.rand.NextFloat(1f, 1.425f);
                        Vector2 dustSpawnPosition = dustSpawnCenter + Main.rand.NextVector2Unit() * Main.rand.NextFloat(16f, 56f);
                        Vector2 dustVelocity = (dustSpawnCenter - dustSpawnPosition) / scale * 0.1f;

                        Dust fire = Dust.NewDustPerfect(dustSpawnPosition, 267);
                        fire.scale = scale;
                        fire.velocity = dustVelocity + Main.rand.NextVector2Circular(0.06f, 0.06f);
                        fire.color = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.6f));
                        fire.noGravity = true;
                    }
                }

                // Hover in place.
                float verticalOffset = MathHelper.Lerp(540f, 80f, Utils.InverseLerp(50f, attackDelay + 50f, attackTimer, true));
                Vector2 hoverDestination = target.Center + new Vector2(twinsHoverSide * 600f, verticalOffset);
                DoHoverMovement(npc, hoverDestination, 17f, 60f);
                if (attackTimer >= attackDelay + 50f)
                {
                    npc.velocity.Y *= 0.5f;

                    // Only move downward if above the player.
                    bool abovePlayer = npc.Center.Y < target.Center.Y;
                    if (npc.velocity.Y > 0f && !abovePlayer)
                        npc.velocity.Y = 0f;
                }

                // Release the laserbeam when ready.
                // If the player attempts to sneakily teleport below Artemis they will descend and damage them with the laser.
                if (attackTimer == attackDelay)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int type = ModContent.ProjectileType<ArtemisPressureLaser>();
                        int laser = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, type, 1100, 0f, Main.myPlayer, npc.whoAmI);
                        if (Main.npc.IndexInRange(laser))
                        {
                            Main.projectile[laser].ai[0] = npc.whoAmI;
                            Main.projectile[laser].ai[1] = 1f;
                        }
                    }
                }

                // Decide rotation.
                float idealRotation = npc.AngleTo(target.Center);
                if (npc.WithinRange(hoverDestination, 60f) || attackTimer > attackDelay * 0.65f)
                    idealRotation = twinsHoverSide == -1f ? 0f : MathHelper.Pi;
                idealRotation += MathHelper.PiOver2;
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.3f).AngleLerp(idealRotation, 0.075f);

                // Handle frames.
                npc.frameCounter++;
                if (attackTimer < attackDelay)
                    frame = (int)Math.Round(MathHelper.Lerp(70f, 79f, (float)npc.frameCounter / 36f % 1f));
                else
                    frame = (int)Math.Round(MathHelper.Lerp(80f, 89f, (float)npc.frameCounter / 36f % 1f));
            }

            // Have Apollo hover in place and release bursts of plasma bolts.
            if (npc.type == ModContent.NPCType<Apollo>())
            {
                ref float hoverOffsetX = ref npc.Infernum().ExtraAI[0];
                ref float hoverOffsetY = ref npc.Infernum().ExtraAI[1];

                // Hover in place.
                Vector2 hoverDestination = target.Center + new Vector2(twinsHoverSide * 600f + hoverOffsetX, hoverOffsetY - 550f);
                DoHoverMovement(npc, hoverDestination, 37f, 75f);

                // Decide rotation.
                int shootRate = 50;
                float plasmaShootSpeed = 10f;

                if (CalamityGlobalNPC.draedonExoMechPrime >= 0 && target.Center.Y < Main.npc[CalamityGlobalNPC.draedonExoMechPrime].Center.Y)
                {
                    shootRate /= 6;
                    plasmaShootSpeed += 8.5f;
                }

                Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * 12f);
                npc.rotation = aimDirection.ToRotation() + MathHelper.PiOver2;

                // Periodically release plasma.
                // If the player attempts to go above Ares the shoot rate is dramatically faster as as punishment.
                if (attackTimer > attackDelay && attackTimer % shootRate == shootRate - 1f)
                {
                    Vector2 plasmaSpawnPosition = npc.Center + aimDirection * 70f;
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaCasterFire"), npc.Center);
                    
                    for (int i = 0; i < 40; i++)
                    {
                        Vector2 dustVelocity = aimDirection.RotatedByRandom(0.35f) * Main.rand.NextFloat(1.8f, 3f);
                        int randomDustType = Main.rand.NextBool() ? 107 : 110;

                        Dust plasma = Dust.NewDustDirect(npc.position, npc.width, npc.height, randomDustType, dustVelocity.X, dustVelocity.Y, 200, default, 1.7f);
                        plasma.position = plasmaSpawnPosition + Main.rand.NextVector2Circular(48f, 48f);
                        plasma.noGravity = true;
                        plasma.velocity *= 3f;

                        plasma = Dust.NewDustDirect(npc.position, npc.width, npc.height, randomDustType, dustVelocity.X, dustVelocity.Y, 100, default, 0.8f);
                        plasma.position = plasmaSpawnPosition + Main.rand.NextVector2Circular(48f, 48f);
                        plasma.velocity *= 2f;

                        plasma.noGravity = true;
                        plasma.fadeIn = 1f;
                        plasma.color = Color.Green * 0.5f;
                    }

                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 dustVelocity = npc.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.35f) * Main.rand.NextFloat(1.8f, 3f);
                        int randomDustType = Main.rand.NextBool() ? 107 : 110;

                        Dust plasma = Dust.NewDustDirect(npc.position, npc.width, npc.height, randomDustType, dustVelocity.X, dustVelocity.Y, 0, default, 2f);
                        plasma.position = plasmaSpawnPosition + Vector2.UnitX.RotatedByRandom(MathHelper.Pi).RotatedBy(npc.velocity.ToRotation()) * 16f;
                        plasma.noGravity = true;
                        plasma.velocity *= 0.5f;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        hoverOffsetX = Main.rand.NextFloat(-30f, 30f);
                        hoverOffsetY = Main.rand.NextFloat(-85f, 85f);
                        for (int i = 0; i < 6; i++)
                        {
                            Vector2 plasmaShootVelocity = aimDirection.RotatedByRandom(0.25f) * plasmaShootSpeed * Main.rand.NextFloat(0.6f, 1f);
                            Utilities.NewProjectileBetter(plasmaSpawnPosition, plasmaShootVelocity, ModContent.ProjectileType<TypicalPlasmaSpark>(), 580, 0f);
                        }
                        npc.netUpdate = true;
                    }
                }

                // Handle frames.
                npc.frameCounter++;
                frame = (int)Math.Round(MathHelper.Lerp(70f, 79f, (float)npc.frameCounter / 36f % 1f));
            }

            // Have Ares linger above the player, charge up, and eventually release a laserbeam.
            // Ares' arms will enforce a border and attempt to punish the player if they attempt to leave.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                ref float laserDirection = ref npc.Infernum().ExtraAI[0];

                // Hover in place.
                bool slowDownInPreparation = attackTimer > attackDelay + pressureTime * 0.35f;
                if (!slowDownInPreparation)
                {
                    Vector2 hoverDestination = target.Center - Vector2.UnitY * 410f;
                    DoHoverMovement(npc, hoverDestination, 24f, 75f);
                }
                else
                    npc.velocity = npc.velocity.ClampMagnitude(0f, 22f) * 0.92f;

                // Release a border of lasers to prevent from the player from just RoD-ing away.
                float minHorizontalOffset = MathHelper.Lerp(650f, 400f, Utils.InverseLerp(0f, attackDelay + 90f, attackTimer, true));
                for (int i = -1; i <= 1; i += 2)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        float horizontalOffset = Main.rand.NextFloat(minHorizontalOffset, 1900f) * i;
                        if (Main.rand.NextFloat() < 0.6f)
                            horizontalOffset = minHorizontalOffset * i + Main.rand.NextFloat(0f, 30f) * -i;
                        Vector2 laserSpawnPosition = new Vector2(npc.Center.X + horizontalOffset, target.Center.Y + Main.rand.NextBool().ToDirectionInt() * 1600f);
                        Vector2 laserShootVelocity = Vector2.UnitY * Math.Sign(target.Center.Y - laserSpawnPosition.Y) * Main.rand.NextFloat(7f, 8f);
                        if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(5))
                        {
                            int lightning = Utilities.NewProjectileBetter(laserSpawnPosition, laserShootVelocity, ModContent.ProjectileType<RedLightning>(), 700, 0f);
                            if (Main.projectile.IndexInRange(lightning))
                            {
                                Main.projectile[lightning].ai[0] = Main.projectile[lightning].velocity.ToRotation();
                                Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                            }
                        }
                    }
                }

                // Create chargeup visuals before firing.
                if (slowDownInPreparation)
                {
                    float chargeupPower = Utils.InverseLerp(attackDelay + pressureTime * 0.35f, attackDelay + pressureTime, attackTimer, true);
                    for (int i = 0; i < 1f + chargeupPower * 3f; i++)
                    {
                        Vector2 laserDustSpawnPosition = npc.Center + Vector2.UnitY * 26f + Main.rand.NextVector2CircularEdge(20f, 20f);
                        Dust laser = Dust.NewDustPerfect(laserDustSpawnPosition, 182);
                        laser.velocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 3.5f) * MathHelper.Lerp(0.35f, 1f, chargeupPower);
                        laser.scale = MathHelper.Lerp(0.8f, 1.5f, chargeupPower) * Main.rand.NextFloat(0.75f, 1f);
                        laser.noGravity = true;

                        Dust.CloneDust(laser).velocity = (npc.Center - laser.position).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.4f) * -laser.velocity.Length() * (1f + chargeupPower * 1.56f);
                    }
                }

                // Create telegraphs prior to firing.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == attackDelay + pressureTime - laserTelegraphTime)
                {
                    // Point the lasers at the player.
                    laserDirection = npc.AngleTo(target.Center);

                    int type = ModContent.ProjectileType<AresBeamTelegraph>();
                    for (int b = 0; b < 7; b++)
                    {
                        int beam = Projectile.NewProjectile(npc.Center, Vector2.Zero, type, 0, 0f, 255, npc.whoAmI);

                        // Determine the initial offset angle of telegraph. It will be smoothened to give a "stretch" effect.
                        if (Main.projectile.IndexInRange(beam))
                        {
                            float squishedRatio = (float)Math.Pow((float)Math.Sin(MathHelper.Pi * b / 7f), 2D);
                            float smoothenedRatio = MathHelper.SmoothStep(0f, 1f, squishedRatio);
                            Main.projectile[beam].ai[0] = npc.whoAmI;
                            Main.projectile[beam].ai[1] = MathHelper.Lerp(-0.55f, 0.55f, smoothenedRatio) + laserDirection;
                            Main.projectile[beam].localAI[0] = laserDirection;
                        }
                    }
                }

                // Release the laserbeam.
                if (attackTimer == attackDelay + pressureTime)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int type = ModContent.ProjectileType<AresDeathray>();
                        int beam = Utilities.NewProjectileBetter(npc.Center, laserDirection.ToRotationVector2(), type, 1200, 0f);
                        if (Main.projectile.IndexInRange(beam))
                            Main.projectile[beam].ai[1] = npc.whoAmI;
                    }
                }

                // Rise upward a little bit after the laserbeam is released.
                if (attackTimer > attackDelay + pressureTime)
                    npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 6f, 0.06f);

                // Handle frames.
                frame = (int)AresBodyFrameType.Normal;
                if (slowDownInPreparation)
                    frame = (int)AresBodyFrameType.Laugh;
            }

            bool attackAboutToConclude = attackTimer > attackDelay + pressureTime + laserReleaseTime + 30f;

            // Clear lasers when the attack should end.
            if (attackAboutToConclude)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].type == ModContent.ProjectileType<ArtemisPressureLaser>())
                        Main.projectile[i].Kill();
                }
            }
            return attackAboutToConclude;
        }
    }
}
