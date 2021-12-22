using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using SCalBoss = CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SepulcherHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum SepulcherAttackState
        {
            // TODO - Needs a glow effect. Can probably wait for now though
            SnapCharges,
            SoulCast,
            HellblastSlam,
            BrimstoneFlameBurst
        }

        public static List<SepulcherAttackState> AttackCycle => new List<SepulcherAttackState>()
        {
            SepulcherAttackState.SnapCharges,
            SepulcherAttackState.SoulCast,
            SepulcherAttackState.HellblastSlam,
            SepulcherAttackState.SnapCharges,
            SepulcherAttackState.BrimstoneFlameBurst,
            SepulcherAttackState.SoulCast,
            SepulcherAttackState.HellblastSlam,
            SepulcherAttackState.BrimstoneFlameBurst,
        };

        public override int NPCOverrideType => ModContent.NPCType<SCalWormHead>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCSetDefaults;

        public override void SetDefaults(NPC npc)
        {
            npc.damage = 550;
            npc.npcSlots = 5f;
            npc.width = npc.height = 64;
            npc.defense = 0;
            npc.lifeMax = 166400;
            npc.aiStyle = npc.modNPC.aiType = -1;
            npc.knockBackResist = 0f;
            npc.scale = 1.3f;
            npc.alpha = 255;
            for (int k = 0; k < npc.buffImmune.Length; k++)
                npc.buffImmune[k] = true;

            npc.behindTiles = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.HitSound = SoundID.DD2_SkeletonHurt;
            npc.DeathSound = SoundID.NPCDeath52;
            npc.netAlways = true;
        }

        public override bool PreAI(NPC npc)
        {
            ref float attackCycleCounter = ref npc.ai[1];
            ref float attackTimer = ref npc.ai[2];
            ref float attackDelay = ref npc.ai[3];
            ref float bodySegmentsSpawnedFlag = ref npc.localAI[0];
            ref float heartState = ref npc.Infernum().ExtraAI[5];
            ref float immunityTimer = ref npc.Infernum().ExtraAI[6];

            // Die if SCal is not present.
            if (!NPC.AnyNPCs(ModContent.NPCType<SCalBoss>()))
            {
                npc.active = false;
                return false;
            }

            npc.dontTakeDamage = false;
            npc.Calamity().DR = 0.2f;
            npc.Calamity().unbreakableDR = false;
            npc.Calamity().CanHaveBossHealthBar = true;
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Create body segments.
            if (Main.netMode != NetmodeID.MultiplayerClient && bodySegmentsSpawnedFlag == 0f)
            {
                int segmentCount = 30;
                int previousSegment = npc.whoAmI;
                float rotationalOffset = 0f;
                for (int i = 0; i < segmentCount; i++)
                {
                    int lol;
                    if (i < segmentCount - 1)
                    {
                        lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SCalWormBody>(), npc.whoAmI);
                        Main.npc[lol].localAI[3] = i;
                    }
                    else
                        lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SCalWormTail>(), npc.whoAmI);

                    // Create arms.
                    if (i >= 3 && i % 4 == 0)
                    {
                        NPC segment = Main.npc[lol];
                        int arm = NPC.NewNPC((int)segment.Center.X, (int)segment.Center.Y, ModContent.NPCType<SCalWormArm>(), lol);
                        if (Main.npc.IndexInRange(arm))
                        {
                            Main.npc[arm].ai[0] = lol;
                            Main.npc[arm].direction = 1;
                            Main.npc[arm].rotation = rotationalOffset;
                        }

                        rotationalOffset += MathHelper.Pi / 6f;

                        arm = NPC.NewNPC((int)segment.Center.X, (int)segment.Center.Y, ModContent.NPCType<SCalWormArm>(), lol);
                        if (Main.npc.IndexInRange(arm))
                        {
                            Main.npc[arm].ai[0] = lol;
                            Main.npc[arm].direction = -1;
                            Main.npc[arm].rotation = rotationalOffset + MathHelper.Pi;
                        }

                        rotationalOffset += MathHelper.Pi / 6f;
                        rotationalOffset = MathHelper.WrapAngle(rotationalOffset);
                    }

                    Main.npc[lol].realLife = npc.whoAmI;
                    Main.npc[lol].ai[2] = npc.whoAmI;
                    Main.npc[lol].ai[1] = previousSegment;
                    Main.npc[previousSegment].ai[0] = lol;
                    previousSegment = lol;
                }
                bodySegmentsSpawnedFlag = 1f;
            }

            // Set the whoAmI index.
            CalamityGlobalNPC.SCalWorm = npc.whoAmI;

            // Periodically summon hearts.
            if (lifeRatio < 1f - (heartState + 1f) / 6f)
            {
                Main.PlaySound(SoundID.DD2_SkyDragonsFuryShot, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<BrimstoneHeart>());

                heartState++;
                npc.netUpdate = true;
            }

            // Define rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Define whether this NPC can be homed in on.
            npc.canGhostHeal = npc.chaseable = !NPC.AnyNPCs(ModContent.NPCType<BrimstoneHeart>());

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);

            // Wait a little bit before attacking, so that the target has time to prepare.
            if (attackDelay < 60f)
            {
                npc.dontTakeDamage = true;
                attackDelay++;
                return false;
            }

            npc.target = Main.npc[CalamityGlobalNPC.SCal].target;
            Player target = Main.player[npc.target];

            switch (AttackCycle[(int)attackCycleCounter % AttackCycle.Count])
            {
                case SepulcherAttackState.SnapCharges:
                    DoBehavior_SnapCharges(npc, target, lifeRatio, ref attackTimer);
                    break;
                case SepulcherAttackState.SoulCast:
                    DoBehavior_SoulCast(npc, target, ref attackTimer);
                    break;
                case SepulcherAttackState.HellblastSlam:
                    DoBehavior_HellblastSlam(npc, target, lifeRatio, ref attackTimer);
                    break;
                case SepulcherAttackState.BrimstoneFlameBurst:
                    DoBehavior_BrimstoneFlameBurst(npc, target, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        public static void DoBehavior_SnapCharges(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            float idealRotation = npc.AngleTo(target.Center);
            float movementSpeed = MathHelper.Lerp(24.5f, 29f, Utils.InverseLerp(1f, 0.15f, lifeRatio, true));
            movementSpeed *= Utils.InverseLerp(-5f, 60f, attackTimer, true);
            float acceleration = movementSpeed / 550f;
            acceleration *= Utils.InverseLerp(240f, 150f, npc.Distance(target.Center), true) + 1f;

            if (!npc.WithinRange(target.Center, 160f))
            {
                float newSpeed = MathHelper.Lerp(npc.velocity.Length(), movementSpeed, acceleration * 3.2f);
                npc.velocity = npc.velocity.RotateTowards(idealRotation, acceleration, true);
                npc.velocity = Vector2.Lerp(npc.velocity * newSpeed, npc.SafeDirectionTo(target.Center) * newSpeed, 0.018f);
            }
            else
                npc.velocity *= 1.0175f;

            if (attackTimer > 360f)
                GotoNextAttack(npc);
        }

        public static void DoBehavior_SoulCast(NPC npc, Player target, ref float attackTimer)
        {
            float idealRotation = npc.AngleTo(target.Center);
            float movementSpeed = 14f;
            float acceleration = movementSpeed / 400f;
            acceleration *= Utils.InverseLerp(240f, 150f, npc.Distance(target.Center), true) * 1.25f + 1f;

            if (!npc.WithinRange(target.Center, 160f))
            {
                float newSpeed = MathHelper.Lerp(npc.velocity.Length(), movementSpeed, acceleration * 3.2f);
                npc.velocity = npc.velocity.RotateTowards(idealRotation, acceleration, true);
                npc.velocity = Vector2.Lerp(npc.velocity * newSpeed, npc.SafeDirectionTo(target.Center) * newSpeed, 0.018f);
            }
            else
                npc.velocity *= 1.01f;

            // Hover near the target, but don't worry about physical attacking.
            float wrappedAttackTimer = attackTimer % 100f;
            bool canFire = wrappedAttackTimer > 40f && wrappedAttackTimer < 90f && attackTimer < 300f;
            if (Main.netMode != NetmodeID.MultiplayerClient && canFire)
            {
                // Pick a random segment to fire a soul from.
                List<NPC> segments = Main.npc.Where(n => n.active && n.type == ModContent.NPCType<SCalWormBody>()).ToList();
                if (segments.Count > 0)
                {
                    NPC segmentToFireFrom = Main.rand.Next(segments);
                    Vector2 soulShootVelocity = (segmentToFireFrom.rotation - MathHelper.PiOver2).ToRotationVector2();
                    soulShootVelocity = Vector2.Lerp(soulShootVelocity, segmentToFireFrom.SafeDirectionTo(target.Center + target.velocity * 50f), 0.75f) * 9f;
                    int spirit = Utilities.NewProjectileBetter(segmentToFireFrom.Center, soulShootVelocity, ModContent.ProjectileType<SepulcherSpirit2>(), 540, 0f);

                    if (Main.projectile.IndexInRange(spirit))
                    {
                        Main.projectile[spirit].localAI[0] = Main.rand.NextFloat(0.92f, 1.08f) % 1f;
                        Main.projectile[spirit].owner = target.whoAmI;
                    }
                }
            }

            if (attackTimer >= 340f)
                GotoNextAttack(npc);
        }

        public static void DoBehavior_HellblastSlam(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int hoverRedirectTime = 240;
            Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt(), -1f) * 435f;
            Vector2 hoverDestination = target.Center + hoverOffset;
            int chargeRedirectTime = 40;
            int chargeTime = 45;
            int chargeSlowdownTime = 25;
            int chargeCount = 3;
            ref float idealChargeVelocityX = ref npc.Infernum().ExtraAI[0];
            ref float idealChargeVelocityY = ref npc.Infernum().ExtraAI[1];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[2];

            // Attempt to get into position for a charge.
            if (attackTimer < hoverRedirectTime)
            {
                float idealHoverSpeed = MathHelper.Lerp(28f, 44f, attackTimer / hoverRedirectTime);
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Lerp(npc.velocity.Length(), idealHoverSpeed, 0.08f);
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.064f, true) * idealVelocity.Length();
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, 3f);

                // Stop hovering if close to the hover destination
                if (npc.WithinRange(hoverDestination, 80f))
                {
                    attackTimer = hoverRedirectTime;
                    if (npc.velocity.Length() > 29f)
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 29f;

                    npc.netUpdate = true;
                }
            }

            // Determine a charge velocity to adjust to.
            if (attackTimer == hoverRedirectTime)
            {
                Vector2 idealChargeVelocity = npc.SafeDirectionTo(target.Center + target.velocity * 10f) * MathHelper.Lerp(33.5f, 39f, 1f - lifeRatio);
                idealChargeVelocityX = idealChargeVelocity.X;
                idealChargeVelocityY = idealChargeVelocity.Y;
                npc.netUpdate = true;
            }

            // Move into the charge.
            if (attackTimer > hoverRedirectTime && attackTimer <= hoverRedirectTime + chargeRedirectTime)
            {
                Vector2 idealChargeVelocity = new Vector2(idealChargeVelocityX, idealChargeVelocityY);
                npc.velocity = npc.velocity.RotateTowards(idealChargeVelocity.ToRotation(), 0.08f, true) * MathHelper.Lerp(npc.velocity.Length(), idealChargeVelocity.Length(), 0.15f);
                npc.velocity = npc.velocity.MoveTowards(idealChargeVelocity, 5f);
            }

            // Release hellblasts charge has begun.
            if (attackTimer == hoverRedirectTime + chargeRedirectTime / 3)
            {
                Main.PlaySound(SoundID.DD2_FlameburstTowerShot, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 17; i++)
                    {
                        Vector2 hellblastVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.Lerp(-1.1f, 1.1f, i / 16f)) * 6f;
                        Utilities.NewProjectileBetter(npc.Center, hellblastVelocity, ModContent.ProjectileType<BrimstoneHellblast>(), 540, 0f);
                    }
                }
            }

            // Slow down after charging.
            if (attackTimer > hoverRedirectTime + chargeRedirectTime + chargeTime)
                npc.velocity *= 0.92f;

            // Prepare the next charge. If all charges are done, go to the next attack.
            if (attackTimer > hoverRedirectTime + chargeRedirectTime + chargeTime + chargeSlowdownTime)
            {
                chargeCounter++;
                idealChargeVelocityX = 0f;
                idealChargeVelocityY = 0f;
                attackTimer = 0f;
                if (chargeCounter >= chargeCount)
                    GotoNextAttack(npc);

                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_BrimstoneFlameBurst(NPC npc, Player target, ref float attackTimer)
        {
            float idealRotation = npc.AngleTo(target.Center);
            float movementSpeed = 15f;
            float acceleration = movementSpeed / 550f;

            if (!npc.WithinRange(target.Center, 100f))
            {
                float newSpeed = MathHelper.Lerp(npc.velocity.Length(), movementSpeed, acceleration * 3.2f);
                npc.velocity = npc.velocity.RotateTowards(idealRotation, acceleration, true);
                npc.velocity = Vector2.Lerp(npc.velocity * newSpeed, npc.SafeDirectionTo(target.Center) * newSpeed, 0.018f);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 35f == 34f && !npc.WithinRange(target.Center, 100f) && attackTimer < 400f && attackTimer > 60f)
            {
                Vector2 shootVelocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 17f;
                Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<DarkMagicBurst>(), 540, 0f);
            }

            if (attackTimer > 460f)
                GotoNextAttack(npc);
        }

        public static void GotoNextAttack(NPC npc)
        {
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.ai[1]++;
            npc.ai[2] = 0f;
            npc.netUpdate = true;
        }
    }
}
