using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.BehaviorOverrides.MinibossAIs.CorruptionMimic.CorruptionMimicBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.CrimsonMimic
{
    public class CrimsonMimicBehaviorOverride : NPCBehaviorOverride
    {
        public enum CrimsonMimicAttackState
        {
            Inactive,
            RapidJumps,
            GroundPound,
            SummonLifeDrainingField,
            IchorDartJumps,
            BaghknahsCharges
        }

        public override int NPCOverrideType => NPCID.BigMimicCrimson;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        public override bool PreAI(NPC npc)
        {
            // Pick a target.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Reset things.
            npc.defense = 10;
            npc.npcSlots = 16f;
            npc.knockBackResist = 0f;
            npc.noTileCollide = false;
            npc.noGravity = false;

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float isHostile = ref npc.ai[2];
            ref float currentFrame = ref npc.localAI[0];
            
            if ((npc.justHit || target.WithinRange(npc.Center, 200f)) && isHostile == 0f)
            {
                isHostile = 1f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            switch ((CrimsonMimicAttackState)(int)attackState)
            {
                case CrimsonMimicAttackState.Inactive:
                    if (DoBehavior_Inactive(npc, target, isHostile == 1f, ref attackTimer, ref currentFrame))
                        SelectNextAttack(npc);
                    break;
                case CrimsonMimicAttackState.RapidJumps:
                    if (DoBehavior_RapidJumps(npc, target, false, ref attackTimer, ref currentFrame))
                        SelectNextAttack(npc);
                    break;
                case CrimsonMimicAttackState.GroundPound:
                    if (DoBehavior_GroundPound(npc, target, ref attackTimer, ref currentFrame))
                        SelectNextAttack(npc);
                    break;
                case CrimsonMimicAttackState.SummonLifeDrainingField:
                    DoBehavior_SummonLifeDrainingField(npc, target, ref attackTimer, ref currentFrame);
                    break;
                case CrimsonMimicAttackState.IchorDartJumps:
                    DoBehavior_RapidIchorDartJumps(npc, target, ref attackTimer, ref currentFrame);
                    break;
                case CrimsonMimicAttackState.BaghknahsCharges:
                    DoBehavior_BaghknahsCharges(npc, target, ref attackTimer, ref currentFrame);
                    break;
            }
            attackTimer++;

            return false;
        }

        public static void DoBehavior_SummonLifeDrainingField(NPC npc, Player target, ref float attackTimer, ref float currentFrame)
        {
            int animationTime = 70;
            int fieldAttackTime = LifeDrainingField.Lifetime;

            if (npc.velocity.Y != 0f && attackTimer < 3f)
            {
                currentFrame = 13f;
                attackTimer = 0f;
                return;
            }

            npc.velocity.X *= 0.8f;
            currentFrame = (int)Math.Round(Utils.Remap(attackTimer, 0f, animationTime, 12f, 7f));

            // Do some dust animation stuff around the target prior to summoning the field.
            if (attackTimer < animationTime)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust blood = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(800f, 400f), 267);
                    blood.velocity = -Vector2.UnitY * Main.rand.NextFloat(0.4f, 4f);
                    blood.color = Color.Lerp(Color.Red, Color.IndianRed, Main.rand.NextFloat());
                    blood.noGravity = true;
                }
            }

            // Summon the life draining field.
            if (attackTimer == animationTime)
            {
                SoundEngine.PlaySound(SoundID.DD2_DrakinShot, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(target.Center - Vector2.UnitY * 270f, Vector2.Zero, ModContent.ProjectileType<LifeDrainingField>(), 120, 0f);
            }

            if (attackTimer >= animationTime + fieldAttackTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_RapidIchorDartJumps(NPC npc, Player target, ref float attackTimer, ref float currentFrame)
        {
            int jumpCount = 4;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float jumpDelay = MathHelper.Lerp(35f, 20f, 1f - lifeRatio);

            ref float jumpCounter = ref npc.Infernum().ExtraAI[0];

            if (npc.velocity.Y == 0f)
            {
                currentFrame = (int)(npc.frameCounter / 7 % 3 + 10f);

                // Slow down when touching the floor.
                npc.velocity.X *= 0.8f;

                // Look at the target.
                if (attackTimer >= jumpDelay * 0.5f)
                {
                    npc.TargetClosest();
                    npc.spriteDirection = npc.direction;
                }

                if (attackTimer >= jumpDelay)
                {
                    jumpCounter++;
                    if (jumpCounter >= jumpCount + 1f)
                        SelectNextAttack(npc);

                    npc.velocity.X = npc.spriteDirection * 14f;
                    npc.velocity.Y = -3f;
                    if (target.Bottom.Y < npc.Center.Y)
                        npc.velocity.Y -= 1.25f;
                    if (target.Bottom.Y < npc.Center.Y - 40f)
                        npc.velocity.Y -= 1.5f;
                    if (target.Bottom.Y < npc.Center.Y - 80f)
                        npc.velocity.Y -= 1.75f;
                    if (target.Bottom.Y < npc.Center.Y - 120f)
                        npc.velocity.Y -= 2f;
                    if (target.Bottom.Y < npc.Center.Y - 160f)
                        npc.velocity.Y -= 2.25f;
                    if (target.Bottom.Y < npc.Center.Y - 200f)
                        npc.velocity.Y -= 2.5f;
                    if (!Collision.CanHit(npc, target))
                        npc.velocity.Y -= 2f;
                    SoundEngine.PlaySound(SoundID.Item102, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(target.Center) * 10f, ModContent.ProjectileType<IchorDart>(), 120, 0f);

                    npc.netUpdate = true;
                }
            }
            else
            {
                npc.velocity.X *= 0.99f;
                attackTimer = 0f;
                currentFrame = 13f;
            }
        }

        public static void DoBehavior_BaghknahsCharges(NPC npc, Player target, ref float attackTimer, ref float currentFrame)
        {
            int jumpCount = 5;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float jumpDelay = MathHelper.Lerp(24f, 12f, 1f - lifeRatio);

            ref float jumpCounter = ref npc.Infernum().ExtraAI[0];

            if (npc.velocity.Y == 0f)
            {
                currentFrame = (int)(npc.frameCounter / 7 % 3 + 10f);

                // Slow down when touching the floor.
                npc.velocity.X *= 0.8f;

                // Look at the target.
                if (attackTimer >= jumpDelay * 0.5f)
                {
                    npc.TargetClosest();
                    npc.spriteDirection = npc.direction;
                }

                if (attackTimer >= jumpDelay)
                {
                    jumpCounter++;
                    if (jumpCounter >= jumpCount + 1f)
                    {
                        int projID = ModContent.ProjectileType<FetidBaghnakhs>();
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            if (Main.projectile[i].type != projID || !Main.projectile[i].active || Main.projectile[i].ai[1] != npc.whoAmI)
                                continue;

                            Main.projectile[i].damage = 0;
                            Main.projectile[i].timeLeft = 30;
                            Main.projectile[i].netUpdate = true;
                        }
                        SelectNextAttack(npc);
                    }
                    else if (Main.netMode != NetmodeID.MultiplayerClient && jumpCounter == 1f)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            int baghnakhs = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<FetidBaghnakhs>(), 120, 0f);
                            if (Main.projectile.IndexInRange(baghnakhs))
                            {
                                Main.projectile[baghnakhs].ai[1] = npc.whoAmI;
                                Main.projectile[baghnakhs].ModProjectile<FetidBaghnakhs>().SpinOffsetAngle = MathHelper.TwoPi * i / 8f;
                                Main.projectile[baghnakhs].netUpdate = true;
                            }
                        }
                    }

                    npc.velocity.X = npc.spriteDirection * 14f;
                    npc.velocity.Y = -3f;
                    if (target.Bottom.Y < npc.Center.Y)
                        npc.velocity.Y -= 1.25f;
                    if (target.Bottom.Y < npc.Center.Y - 40f)
                        npc.velocity.Y -= 1.5f;
                    if (target.Bottom.Y < npc.Center.Y - 80f)
                        npc.velocity.Y -= 1.75f;
                    if (target.Bottom.Y < npc.Center.Y - 120f)
                        npc.velocity.Y -= 2f;
                    if (target.Bottom.Y < npc.Center.Y - 160f)
                        npc.velocity.Y -= 2.25f;
                    if (target.Bottom.Y < npc.Center.Y - 200f)
                        npc.velocity.Y -= 2.5f;
                    if (!Collision.CanHit(npc, target))
                        npc.velocity.Y -= 2f;
                    npc.velocity *= 1.25f;
                    npc.netUpdate = true;
                }
            }
            else
            {
                npc.velocity.X *= 0.99f;
                attackTimer = 0f;
                currentFrame = 13f;
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            switch ((CrimsonMimicAttackState)npc.ai[0])
            {
                case CrimsonMimicAttackState.Inactive:
                    npc.ai[0] = (int)CrimsonMimicAttackState.RapidJumps;
                    break;
                case CrimsonMimicAttackState.RapidJumps:
                    npc.ai[0] = (int)CrimsonMimicAttackState.GroundPound;
                    break;
                case CrimsonMimicAttackState.GroundPound:
                    npc.ai[0] = (int)CrimsonMimicAttackState.IchorDartJumps;
                    break;
                case CrimsonMimicAttackState.IchorDartJumps:
                    npc.ai[0] = (int)CrimsonMimicAttackState.SummonLifeDrainingField;
                    break;
                case CrimsonMimicAttackState.SummonLifeDrainingField:
                    npc.ai[0] = (int)CrimsonMimicAttackState.BaghknahsCharges;
                    break;
                case CrimsonMimicAttackState.BaghknahsCharges:
                    npc.ai[0] = (int)CrimsonMimicAttackState.RapidJumps;
                    break;
            }

            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            npc.frame.Y = (int)(frameHeight * Math.Round(npc.localAI[0]));
        }
    }
}
