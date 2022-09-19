using CalamityMod;
using CalamityMod.Items.Tools;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Sounds;
using InfernumMode.GlobalInstances;
using InfernumMode.OverridingSystem;
using InfernumMode.Projectiles;
using InfernumMode.Sounds;
using InfernumMode.Systems;
using InfernumMode.Tiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public enum GolemAttackState
    {
        FloorFire,
        FistSpin,
        SpikeTrapWaves,
        HeatRay,
        SpinLaser,
        Slingshot,
        SpikeRush,

        LandingState,
        SummonDelay,
        BIGSHOT,
        BadTime,
    }

    public class GolemBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Golem;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public static int ArenaWidth = 115;

        public static int ArenaHeight = 105;

        public const int AttacksNotToPool = 4; // The last X states in GolemAttackState should not be selected as attacks during the fight

        public const int Phase2TransitionAnimationTime = 180;

        public const float ConstAttackCooldown = 125f;

        public const float Phase2LifeRatio = 0.6f;

        public const float Phase3LifeRatio = 0.3f;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio
        };

        public static NPC GetLeftFist(NPC npc) => Main.npc[(int)npc.Infernum().ExtraAI[0]];

        public static NPC GetRightFist(NPC npc) => Main.npc[(int)npc.Infernum().ExtraAI[1]];

        public static NPC GetAttachedHead(NPC npc) => Main.npc[(int)npc.Infernum().ExtraAI[2]];

        public static NPC GetFreeHead(NPC npc) => Main.npc[(int)npc.Infernum().ExtraAI[3]];

        public static Vector2 GetLeftFistAttachmentPosition(NPC npc) => new(npc.Left.X - 24f, npc.Left.Y - 6f);

        public static Vector2 GetRightFistAttachmentPosition(NPC npc) => new(npc.Right.X + 24f, npc.Right.Y - 6f);

        public override bool PreAI(NPC npc)
        {
            // Set the whoAmI variable.
            NPC.golemBoss = npc.whoAmI;

            ref float AITimer = ref npc.ai[0];
            ref float AttackState = ref npc.ai[1];
            ref float AttackTimer = ref npc.ai[2];
            ref float FightStarted = ref npc.ai[3];

            ref float LeftFistNPC = ref npc.Infernum().ExtraAI[0];
            ref float RightFistNPC = ref npc.Infernum().ExtraAI[1];
            ref float AttachedHeadNPC = ref npc.Infernum().ExtraAI[2];
            ref float FreeHeadNPC = ref npc.Infernum().ExtraAI[3];
            ref float HeadState = ref npc.Infernum().ExtraAI[4];
            ref float EnrageState = ref npc.Infernum().ExtraAI[5];
            ref float ReturnFromEnrageState = ref npc.Infernum().ExtraAI[6];
            ref float AttackCooldown = ref npc.Infernum().ExtraAI[7];
            ref float PreviousAttackState = ref npc.Infernum().ExtraAI[8];
            // ref float DarknessRatio = ref npc.Infernum().ExtraAI[9]; (later in code)
            ref float eyeLaserRayInterpolant = ref npc.Infernum().ExtraAI[10];
            ref float slingshotRotation = ref npc.Infernum().ExtraAI[11];
            ref float fistSlamDestinationX = ref npc.Infernum().ExtraAI[12];
            ref float fistSlamDestinationY = ref npc.Infernum().ExtraAI[13];
            ref float attackCounter = ref npc.Infernum().ExtraAI[14];
            ref float jumpState = ref npc.Infernum().ExtraAI[15];
            ref float phase2TransitionTimer = ref npc.Infernum().ExtraAI[16];
            ref float coreLaserRayInterpolant = ref npc.Infernum().ExtraAI[17];
            ref float coreLaserRayDirection = ref npc.Infernum().ExtraAI[18];
            ref float slingshotArmToCharge = ref npc.Infernum().ExtraAI[19];
            ref float phase3TransitionTimer = ref npc.Infernum().ExtraAI[21];

            bool headIsFree = HeadState == 1;

            Vector2 attachedHeadCenterPos = new(npc.Center.X, npc.Top.Y);
            Vector2 leftHandCenterPos = GetLeftFistAttachmentPosition(npc);
            Vector2 rightHandCenterPos = GetRightFistAttachmentPosition(npc);

            if (AITimer == 0f)
            {
                npc.TargetClosest();

                // If the NPC cap is reached, the fight will break, so just don't do anything
                int npcCount = Main.npc.Count(n => n.active);
                if (npcCount > Main.maxNPCs - 4)
                {
                    npc.life = 0;
                    npc.active = false;
                    npc.netUpdate = true;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);

                    return false;
                }

                // Otherwise prepare the fight
                int maxHP = 156000;
                GlobalNPCOverrides.AdjustMaxHP(ref maxHP);
                npc.life = npc.lifeMax = maxHP;
                npc.noGravity = true;
                npc.noTileCollide = false;
                npc.chaseable = false;
                npc.Opacity = 0f;
                AttackState = Utilities.IsAprilFirst() ? (Main.rand.NextBool() ? (float)GolemAttackState.BIGSHOT : (float)GolemAttackState.BadTime) : (float)GolemAttackState.SummonDelay;
                PreviousAttackState = (float)GolemAttackState.LandingState;
                CreateGolemArena(npc);
                attachedHeadCenterPos = new Vector2(npc.Center.X, npc.Top.Y);
                npc.netUpdate = true;

                int freeHeadInt = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X - 55, (int)npc.Top.Y, NPCID.GolemHeadFree);
                Main.npc[freeHeadInt].Center = attachedHeadCenterPos;
                Main.npc[freeHeadInt].dontTakeDamage = true;
                Main.npc[freeHeadInt].noGravity = true;
                Main.npc[freeHeadInt].noTileCollide = true;
                Main.npc[freeHeadInt].lifeMax = Main.npc[freeHeadInt].life = npc.lifeMax;
                Main.npc[freeHeadInt].ai[0] = npc.whoAmI;
                Main.npc[freeHeadInt].netUpdate = true;
                FreeHeadNPC = freeHeadInt;

                int attachedHeadInt = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X - 55, (int)npc.Top.Y, NPCID.GolemHead);
                Main.npc[attachedHeadInt].Center = attachedHeadCenterPos;
                Main.npc[attachedHeadInt].lifeMax = Main.npc[attachedHeadInt].life = npc.lifeMax;
                Main.npc[attachedHeadInt].noGravity = true;
                Main.npc[attachedHeadInt].noTileCollide = true;
                Main.npc[attachedHeadInt].ai[0] = npc.whoAmI;
                Main.npc[attachedHeadInt].netUpdate = true;
                AttachedHeadNPC = attachedHeadInt;

                int leftHand = NPC.NewNPC(npc.GetSource_FromAI(), (int)leftHandCenterPos.X, (int)leftHandCenterPos.Y, ModContent.NPCType<GolemFistLeft>());
                Main.npc[leftHand].ai[0] = npc.whoAmI;
                Main.npc[leftHand].netUpdate = true;
                LeftFistNPC = leftHand;

                int rightHand = NPC.NewNPC(npc.GetSource_FromAI(), (int)rightHandCenterPos.X, (int)rightHandCenterPos.Y, ModContent.NPCType<GolemFistRight>());
                Main.npc[rightHand].ai[0] = npc.whoAmI;
                Main.npc[rightHand].netUpdate = true;
                RightFistNPC = rightHand;

                AITimer++;

                return false;
            }

            // Aquire a new target if the current one is dead or inactive.
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                npc.TargetClosest(false);

                // If no possible target was found, die.
                if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                {
                    DespawnNPC((int)AttachedHeadNPC);
                    DespawnNPC((int)FreeHeadNPC);
                    DespawnNPC((int)LeftFistNPC);
                    DespawnNPC((int)RightFistNPC);

                    npc.life = 0;
                    npc.active = false;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);

                    DeleteGolemArena();

                    return false;
                }
                npc.netUpdate = true;
            }
            else
                npc.timeLeft = 7200;
            
            Rectangle arena = npc.Infernum().Arena;
            
            // 0 is normal. 1 is enraged.
            EnrageState = (!Main.player[npc.target].Hitbox.Intersects(arena)).ToInt();
            npc.TargetClosest(false);

            // Reset telegraph interpolants.
            eyeLaserRayInterpolant = 0f;
            coreLaserRayInterpolant = 0f;

            bool Enraged = EnrageState == 1f;

            ref NPC body = ref npc;
            ref NPC freeHead = ref Main.npc[(int)FreeHeadNPC];
            ref NPC attachedHead = ref Main.npc[(int)AttachedHeadNPC];
            ref NPC leftFist = ref Main.npc[(int)LeftFistNPC];
            ref NPC rightFist = ref Main.npc[(int)RightFistNPC];
            ref Player target = ref Main.player[npc.target];

            // Reset head DR.
            freeHead.Calamity().DR = 0.3f;
            attachedHead.Calamity().DR = 0.3f;

            // Sync the heads, and end the fight if necessary
            if (!attachedHead.active || !freeHead.active || attachedHead.life <= 0 || freeHead.life <= 0)
            {
                DespawnNPC(attachedHead.whoAmI);
                DespawnNPC(freeHead.whoAmI);
                DespawnNPC(leftFist.whoAmI);
                DespawnNPC(rightFist.whoAmI);

                npc.life = 0;
                npc.HitEffect();
                npc.checkDead();
                npc.active = false;
                npc.netUpdate = true;

                DeleteGolemArena();
                return false;
            }
            else if (ReturnFromEnrageState == 0f)
            {
                // Sync head HP
                if (freeHead.life > attachedHead.life)
                    freeHead.life = attachedHead.life;
                else
                    attachedHead.life = freeHead.life;

                npc.life = attachedHead.life;

                // Sync positions of NPCs
                attachedHead.Center = attachedHeadCenterPos;

                // Only sync free head if it's not in the middle of doing something
                if (freeHead.dontTakeDamage)
                    freeHead.Center = attachedHeadCenterPos;
            }

            if (!rightFist.active)
                rightFist.active = true;

            if (!leftFist.active)
                leftFist.active = true;

            freeHead.ai[1]++;
            if (freeHead.ai[1] >= 240f)
                freeHead.ai[1] = 0f;

            attachedHead.ai[1]++;
            if (attachedHead.ai[1] >= 240f)
                attachedHead.ai[1] = 0f;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool inPhase2 = phase2TransitionTimer >= Phase2TransitionAnimationTime && lifeRatio < Phase2LifeRatio;
            bool inPhase3 = inPhase2 && lifeRatio < Phase3LifeRatio;

            // Create a boom as a phase transition for phase 3.
            if (Main.netMode != NetmodeID.MultiplayerClient && inPhase3 && phase3TransitionTimer < 90f)
            {
                phase3TransitionTimer++;
                if (phase3TransitionTimer == 45f && HatGirlTipsManager.ShouldUseJokeText)
                    HatGirl.SayThingWhileOwnerIsAlive(target, "You're gonna have a bad time...");
            }

            // Reset things.
            npc.dontTakeDamage = true;
            freeHead.damage = AITimer < 120f ? 0 : freeHead.defDamage;

            if (FightStarted == 0f)
            {
                if (npc.Bottom.Y > npc.Infernum().Arena.Bottom)
                {
                    npc.Bottom = new Vector2(npc.Center.X, npc.Infernum().Arena.Bottom);
                    npc.velocity = Vector2.Zero;
                }

                leftFist.Center = leftHandCenterPos;
                rightFist.Center = rightHandCenterPos;

                // Fade the screen to black
                // Starting by setting opacities to 0 and having golem fall invisibly
                if (AITimer == 1f || (AITimer > 1f && npc.velocity.Y != 0f))
                {
                    npc.Opacity = 0f;
                    leftFist.Opacity = 0f;
                    rightFist.Opacity = 0f;
                    freeHead.Opacity = 0f;
                    attachedHead.Opacity = 0f;
                    npc.damage = 0;
                    leftFist.damage = 0;
                    rightFist.damage = 0;
                    freeHead.damage = 0;
                    attachedHead.damage = 0;
                    attachedHead.dontTakeDamage = true;

                    npc.velocity.Y += 0.5f;
                    AITimer++;
                    return false;
                }

                // Start the epic once golem lands
                else if (PreviousAttackState == (float)GolemAttackState.LandingState)
                {
                    PreviousAttackState = AttackState;
                    AITimer = 2f;
                }

                ref float DarknessRatio = ref npc.Infernum().ExtraAI[9];
                float Timer = AITimer - 2f;

                // Fade in for the first 60 frames
                // Hold black for the second 60 frames
                if (Timer < 180f)
                    DarknessRatio = MathHelper.Clamp(Timer / 60f, 0f, 1f);

                // Fade out for the last 60 frames
                else if (Timer < 240f)
                {
                    DarknessRatio = 1f - MathHelper.Clamp((Timer - 180f) / 60f, 0f, 1f);
                    npc.Opacity = 1f;
                    freeHead.Opacity = 1f;
                    leftFist.Opacity = 1f;
                    rightFist.Opacity = 1f;
                    attachedHead.Opacity = 1f;
                }
                else
                {
                    DarknessRatio = 0f;
                    AITimer++;
                    FightStarted = 1f;
                    AttackCooldown = ConstAttackCooldown;
                    PreviousAttackState = (float)GolemAttackState.SummonDelay;
                    AttackState = (float)GolemAttackState.FistSpin;
                    npc.damage = npc.defDamage + 20;
                    leftFist.damage = leftFist.defDamage;
                    rightFist.damage = rightFist.defDamage;
                    attachedHead.damage = attachedHead.defDamage;
                    attachedHead.dontTakeDamage = false;
                    return false;
                }

                // Play the sound
                if (Timer == 120f)
                {
                    if (AttackState == (float)GolemAttackState.BadTime)
                        SoundEngine.PlaySound(InfernumSoundRegistry.GolemSansSound, target.Center);
                    else
                        SoundEngine.PlaySound(InfernumSoundRegistry.GolemSpamtonSound, target.Center);
                }

                AITimer++;
                return false;
            }

            // Enter the second phase.
            if (lifeRatio < Phase2LifeRatio && phase2TransitionTimer < Phase2TransitionAnimationTime)
            {
                // Make the head return to the body before doing anything else.
                // This negates the enrage state.
                ReturnFromEnrageState = 0f;
                if (headIsFree)
                {
                    phase2TransitionTimer = 0f;
                    ReAttachHead(npc);
                }

                // Make fists return.
                leftFist.rotation = 0f;
                rightFist.rotation = 0f;
                leftFist.Center = Vector2.Lerp(leftFist.Center, leftHandCenterPos, 0.8f);
                rightFist.Center = Vector2.Lerp(rightFist.Center, rightHandCenterPos, 0.8f);

                // Obey gravity and tile collision.
                npc.noGravity = false;
                npc.noTileCollide = false;
                if (Math.Abs(npc.velocity.Y) < 0.5f)
                    npc.velocity.X *= 0.8f;

                freeHead.Calamity().DR = 0.9f;
                attachedHead.Calamity().DR = 0.9f;
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<GolemEyeLaserRay>());
                DoBehavior_EnterSecondPhase(npc, phase2TransitionTimer);
                if (phase2TransitionTimer == 2f)
                {
                    fistSlamDestinationX = fistSlamDestinationY = 0f;
                    jumpState = 0f;
                    slingshotRotation = 0f;
                    slingshotArmToCharge = 0f;
                    attackCounter = 0f;
                    SelectNextAttackState(npc);
                    npc.netUpdate = true;
                }

                phase2TransitionTimer++;
                return false;
            }

            if (Enraged)
            {
                npc.Calamity().CurrentlyEnraged = true;

                // Swap to the free head so that death can ensue
                if (!headIsFree)
                    SwapHeads(npc);

                // Invincibility is lame
                freeHead.defense = 9999;

                // Accelerate the X and Y separately for sporadic movement
                if (freeHead.velocity.Length() < 2f)
                    freeHead.velocity = freeHead.SafeDirectionTo(target.Center) * 2f;

                if (Math.Abs((freeHead.Center + freeHead.velocity).X - target.Center.X) > Math.Abs(freeHead.Center.X - target.Center.X))
                    freeHead.velocity.X += freeHead.Center.X > target.Center.X ? -1f : 1f;
                else
                    freeHead.velocity.X *= 1.1f;

                if (Math.Abs((freeHead.Center + freeHead.velocity).Y - target.Center.Y) > Math.Abs(freeHead.Center.Y - target.Center.Y))
                    freeHead.velocity.Y += freeHead.Center.Y > target.Center.Y ? -1f : 1f;
                else
                    freeHead.velocity.Y *= 1.1f;

                freeHead.velocity = freeHead.velocity.ClampMagnitude(0f, 25f);

                // Shoot a lot of powerful fire at the target.
                AITimer++;
                if (Main.netMode != NetmodeID.MultiplayerClient && AITimer % 32f == 31f)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 shootVelocity = freeHead.SafeDirectionTo(target.Center + target.velocity * 20f).RotatedBy(MathHelper.Lerp(-0.72f, 0.72f, i / 9f)) * 10f;
                        Utilities.NewProjectileBetter(freeHead.Center, shootVelocity, ProjectileID.Fireball, 250, 0f);
                    }
                }

                // Mark this so that if the player re-enters the arena then the AI will know to resync
                ReturnFromEnrageState = 1f;
                return false;
            }
            else if (ReturnFromEnrageState == 1f)
            {
                // If it will pass, reattach
                // It's fine if the head was unattached before enraging, the attack will continue like normal
                if (freeHead.Distance(attachedHeadCenterPos) < 50f)
                {
                    freeHead.defense = freeHead.defDefense;
                    SwapHeads(npc);
                    freeHead.velocity = Vector2.Zero;
                    ReturnFromEnrageState = 0f;
                    return false;
                }
                else if (attachedHead.Distance(freeHead.Center + freeHead.velocity) > attachedHead.Distance(freeHead.Center))
                    freeHead.velocity = freeHead.SafeDirectionTo(attachedHeadCenterPos) * 18f;

                return false;
            }

            // Return to the arena if stuck.
            Rectangle deflatedArena = npc.Infernum().Arena;
            deflatedArena.Inflate(-80, -32);
            if (Collision.SolidCollision(npc.Center - Vector2.One * 15f, 30, 30) || !npc.Hitbox.Intersects(deflatedArena))
            {
                if (!npc.Hitbox.Intersects(npc.Infernum().Arena))
                    npc.velocity = Vector2.Zero;
                npc.Center = npc.Center.MoveTowards(npc.Infernum().Arena.Center.ToVector2(), 14f);
            }

            if (AttackCooldown <= 0f)
            {
                switch ((GolemAttackState)AttackState)
                {
                    case GolemAttackState.FloorFire:
                        if (headIsFree)
                        {
                            ReAttachHead(npc);
                            break;
                        }
                        DoBehavior_FloorFire(npc, target, inPhase2, inPhase3, ref AttackTimer, ref AttackCooldown, ref jumpState, ref attackCounter);
                        break;
                    case GolemAttackState.FistSpin:
                        if (headIsFree)
                        {
                            ReAttachHead(npc);
                            break;
                        }
                        DoBehavior_FistSpin(npc, target, inPhase2, inPhase3, ref AttackTimer, ref AttackCooldown);
                        break;
                    case GolemAttackState.SpikeTrapWaves:
                        if (headIsFree)
                        {
                            ReAttachHead(npc);
                            break;
                        }
                        DoBehavior_SpiketrapWaves(npc, inPhase2, inPhase3, ref AttackTimer, ref AttackCooldown);
                        break;
                    case GolemAttackState.HeatRay:
                        DoBehavior_HeatRay(npc, target, inPhase2, inPhase3, headIsFree, ref AttackTimer, ref AttackCooldown, ref eyeLaserRayInterpolant);
                        break;
                    case GolemAttackState.SpinLaser:
                        if (headIsFree)
                        {
                            ReAttachHead(npc);
                            break;
                        }
                        DoBehavior_SpinLaser(npc, target, inPhase3, ref AttackTimer, ref coreLaserRayInterpolant, ref coreLaserRayDirection, ref AttackCooldown);
                        break;
                    case GolemAttackState.Slingshot:
                        if (headIsFree)
                        {
                            ReAttachHead(npc);
                            break;
                        }
                        DoBehavior_Slingshot(npc, target, inPhase2, inPhase3, ref AttackTimer, ref fistSlamDestinationX, ref fistSlamDestinationY, ref slingshotArmToCharge, ref slingshotRotation, ref attackCounter, ref AttackCooldown);
                        break;
                    case GolemAttackState.SpikeRush:
                        if (headIsFree)
                        {
                            ReAttachHead(npc);
                            break;
                        }
                        DoBehavior_SpikeRush(npc, ref AttackTimer, ref AttackCooldown);
                        break;
                }
            }
            else
            {
                // Attack swapping
                freeHead.velocity *= 0.9f;
                if (freeHead.velocity.Length() < 0.25f)
                    freeHead.velocity = Vector2.Zero;

                rightFist.Center = Vector2.Lerp(rightFist.Center, rightHandCenterPos, 0.3f);
                leftFist.Center = Vector2.Lerp(leftFist.Center, leftHandCenterPos, 0.3f);
                AttackCooldown--;
            }

            if (Math.Abs(npc.velocity.Y) < 0.5f && !npc.noTileCollide)
                npc.velocity.X *= 0.8f;

            AITimer++;
            return false;
        }

        public static void DoBehavior_FloorFire(NPC npc, Player target, bool inPhase2, bool inPhase3, ref float attackTimer, ref float attackCooldown, ref float jumpState, ref float attackCounter)
        {
            int jumpDelay = 25;
            int jumpCount = 3;
            int postJumpSitTime = 90;
            int platformReleaseRate = 0;
            float fireCrystalSpacing = 120f;
            float lifeRatio = npc.life / (float)npc.lifeMax;

            if (inPhase2)
                fireCrystalSpacing -= 20f;
            if (inPhase3)
            {
                platformReleaseRate += 72;
                fireCrystalSpacing -= 15f;
            }

            npc.noTileCollide = false;

            // Attach hands.
            NPC leftFist = GetLeftFist(npc);
            NPC rightFist = GetRightFist(npc);
            leftFist.Center = GetLeftFistAttachmentPosition(npc);
            rightFist.Center = GetRightFistAttachmentPosition(npc);
            leftFist.rotation = 0f;
            rightFist.rotation = 0f;

            // Sit in place and await the next jump.
            if (jumpState == 0f)
            {
                npc.velocity.X *= 0.8f;
                if (attackTimer >= jumpDelay && Math.Abs(npc.velocity.X) < 0.35f)
                {
                    attackTimer = 0f;
                    jumpState = 1f;
                    npc.velocity.X = MathHelper.Lerp(5f, 16f, lifeRatio) * (target.Center.X > npc.Center.X).ToDirectionInt();

                    if (target.Top.Y < npc.Bottom.Y)
                        npc.velocity.Y = -12.5f;
                    else
                        npc.velocity.Y = 1f;
                    npc.netUpdate = true;
                }
            }

            // Fall.
            else
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.5f, -20f, 20f);
                if (Math.Abs(npc.velocity.Y) <= 0.85f)
                {
                    npc.velocity.X *= 0.8f;
                    if (jumpState == 1f)
                    {
                        SoundEngine.PlaySound(SoundID.Item14, npc.position);
                        for (int i = (int)npc.position.X - 20; i < (int)npc.position.X + npc.width + 40; i += 20)
                        {
                            for (int j = 0; j < 4; j++)
                            {
                                Dust smokeDust = Dust.NewDustDirect(new Vector2(npc.position.X - 20f, npc.position.Y + npc.height), npc.width + 20, 4, 31, 0f, 0f, 100, default, 1.5f);
                                smokeDust.velocity *= 0.2f;
                            }

                            if (Main.netMode != NetmodeID.Server)
                            {
                                Gore smoke = Gore.NewGoreDirect(npc.GetSource_FromAI(), new Vector2(i - 20, npc.position.Y + npc.height - 8f), default, Main.rand.Next(61, 64), 1f);
                                smoke.velocity *= 0.4f;
                            }
                        }

                        // Summon crystals on the floor that accelerate upward.
                        float horizontalOffset = Main.rand.NextFloat(120f);
                        for (float x = npc.Infernum().Arena.Left + 20f; x < npc.Infernum().Arena.Right - 20f; x += fireCrystalSpacing)
                        {
                            float y = npc.Infernum().Arena.Center.Y;
                            Vector2 crystalSpawnPosition = Utilities.GetGroundPositionFrom(new Vector2(x + horizontalOffset, y));

                            // Create puffs of fire at the crystal's position.
                            for (int i = 0; i < 6; i++)
                            {
                                Dust fire = Dust.NewDustPerfect(crystalSpawnPosition + Main.rand.NextVector2Square(-24f, 24f), 6);
                                fire.velocity = Main.rand.NextVector2Circular(5f, 5f) - Vector2.UnitY * 2.5f;
                                fire.scale = 1.3f;
                                fire.fadeIn = 0.75f;
                                fire.noGravity = true;
                            }

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                Utilities.NewProjectileBetter(crystalSpawnPosition, -Vector2.UnitY * 0.004f, ModContent.ProjectileType<GroundFireCrystal>(), 200, 0f);
                        }

                        // As well as on the sides of the arena in the third phase.
                        if (inPhase3)
                        {
                            for (float y = npc.Infernum().Arena.Top + 20f; y < npc.Infernum().Arena.Bottom - 20f; y += fireCrystalSpacing * 2f)
                            {
                                float x = npc.Infernum().Arena.Center.X;
                                Vector2 crystalSpawnPosition = Utilities.GetGroundPositionFrom(new Vector2(x, y + horizontalOffset), new Searches.Left(9001));

                                // Create puffs of fire at the crystal's position.
                                for (int i = 0; i < 6; i++)
                                {
                                    Dust fire = Dust.NewDustPerfect(crystalSpawnPosition + Main.rand.NextVector2Square(-24f, 24f), 6);
                                    fire.velocity = Main.rand.NextVector2Circular(5f, 5f) + Vector2.UnitX * 2.5f;
                                    fire.scale = 1.3f;
                                    fire.fadeIn = 0.75f;
                                    fire.noGravity = true;
                                }

                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                    Utilities.NewProjectileBetter(crystalSpawnPosition, Vector2.UnitX * 0.004f, ModContent.ProjectileType<GroundFireCrystal>(), 200, 0f);
                            }
                        }

                        jumpState = 2f;
                        npc.netUpdate = true;
                    }

                    if (attackTimer >= postJumpSitTime)
                    {
                        attackTimer = 0f;
                        attackCounter++;
                        jumpState = 0f;

                        if (attackCounter >= jumpCount)
                        {
                            attackCounter = 0f;
                            attackCooldown = ConstAttackCooldown;
                            SelectNextAttackState(npc);
                        }

                        npc.velocity = Vector2.Zero;
                        npc.netUpdate = true;
                    }
                }
            }

            // Create platforms below the target in the third phase.
            if (attackTimer % platformReleaseRate == 0f && inPhase3)
            {
                Vector2 platformSpawnPosition = new(target.Center.X, npc.Infernum().Arena.Bottom - 16f);
                CreatePlatform(npc, platformSpawnPosition, -Vector2.UnitY * 1.5f);
            }


            attackTimer++;
        }

        public static void DoBehavior_FistSpin(NPC npc, Player target, bool inPhase2, bool inPhase3, ref float attackTimer, ref float attackCooldown)
        {
            int platformReleaseRate = 95;
            int fistShootRate = 18;

            if (inPhase2)
                fistShootRate -= 4;
            if (inPhase3)
                fistShootRate -= 2;

            NPC leftFist = GetLeftFist(npc);
            NPC rightFist = GetRightFist(npc);

            if (attackTimer <= 300f)
            {
                // Rotate the fists around the body over the course of 3 seconds, spawning projectiles every so often
                float rotation = MathHelper.Lerp(0f, MathHelper.TwoPi * 2, attackTimer / 240f);
                float distance = 145f;
                rightFist.Center = GetRightFistAttachmentPosition(npc) + rotation.ToRotationVector2() * distance;
                rightFist.rotation = rotation;
                leftFist.Center = GetLeftFistAttachmentPosition(npc) + MathHelper.WrapAngle(rotation + MathHelper.Pi).ToRotationVector2() * distance;
                leftFist.rotation = rotation;

                // Release fist bullets.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % fistShootRate == 0f)
                {
                    int type = ModContent.ProjectileType<FistBullet>();
                    int bullet = Utilities.NewProjectileBetter(rightFist.Center, Vector2.Zero, type, 200, 0);
                    if (Main.projectile.IndexInRange(bullet))
                    {
                        Main.projectile[bullet].Infernum().ExtraAI[0] = 0f;
                        Main.projectile[bullet].Infernum().ExtraAI[2] = target.whoAmI;
                        Main.projectile[bullet].rotation = rotation;
                        Main.projectile[bullet].netUpdate = true;
                    }

                    bullet = Utilities.NewProjectileBetter(leftFist.Center, Vector2.Zero, type, 200, 0);
                    if (Main.projectile.IndexInRange(bullet))
                    {
                        Main.projectile[bullet].Infernum().ExtraAI[0] = 0f;
                        Main.projectile[bullet].Infernum().ExtraAI[2] = target.whoAmI;
                        Main.projectile[bullet].rotation = MathHelper.WrapAngle(rotation + MathHelper.Pi);
                        Main.projectile[bullet].netUpdate = true;
                    }
                }
            }
            else
            {
                leftFist.rotation = 0f;
                rightFist.rotation = 0f;
                leftFist.Center = Vector2.Lerp(leftFist.Center, GetLeftFistAttachmentPosition(npc), 0.8f);
                rightFist.Center = Vector2.Lerp(rightFist.Center, GetRightFistAttachmentPosition(npc), 0.8f);
                if (attackTimer >= 360f)
                {
                    attackCooldown = ConstAttackCooldown;
                    SelectNextAttackState(npc);
                }
            }

            // Create platforms below the target in the second phase.
            if (attackTimer % platformReleaseRate == 0f && inPhase2)
            {
                Vector2 platformSpawnPosition = new(target.Center.X, npc.Infernum().Arena.Bottom - 16f);
                CreatePlatform(npc, platformSpawnPosition, -Vector2.UnitY * 1.5f);
            }

            attackTimer++;
        }

        public static void DoBehavior_SpiketrapWaves(NPC npc, bool inPhase2, bool inPhase3, ref float attackTimer, ref float attackCooldown)
        {
            int platformReleaseRate = 80;
            int fistSlamTime = 45;
            int spikeWaveCreationTime = 420;
            int spikeTrapCreationRate = 50;
            int fireCrystalReleaseRate = 72;
            float fistSlamInterpolant = 1f;

            if (inPhase2)
            {
                spikeTrapCreationRate -= 7;
                platformReleaseRate -= 4;
            }
            if (inPhase3)
                spikeTrapCreationRate -= 3;

            NPC leftFist = GetLeftFist(npc);
            NPC rightFist = GetRightFist(npc);

            // Make fists slam into the wall.
            if (attackTimer < fistSlamTime)
            {
                fistSlamInterpolant = (float)Math.Pow(attackTimer / fistSlamTime, 2D);

                // Create impact effects.
                if (attackTimer == fistSlamTime - 1f)
                {
                    Vector2 leftImpactPoint = new(npc.Infernum().Arena.Left, leftFist.Center.Y);
                    Vector2 rightImpactPoint = new(npc.Infernum().Arena.Right, leftFist.Center.Y);

                    SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, leftImpactPoint);
                    SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, rightImpactPoint);
                    Utils.PoofOfSmoke(leftImpactPoint);
                    Utils.PoofOfSmoke(rightImpactPoint);
                    Collision.HitTiles(leftImpactPoint, -Vector2.UnitX, 40, 40);
                    Collision.HitTiles(rightImpactPoint, Vector2.UnitX, 40, 40);
                }

                // Create dust on the walls where spikes will appear.
                for (int i = 0; i < 6; i++)
                {
                    Vector2 trapSpawnPosition = npc.Infernum().Arena.TopLeft();
                    Dust.NewDustDirect(trapSpawnPosition, 8, 600, 6);

                    trapSpawnPosition = npc.Infernum().Arena.BottomRight();
                    Dust.NewDustDirect(trapSpawnPosition - new Vector2(-8f, 600f), 8, 600, 6);
                }
            }

            // Summon waves of spikes.
            else if (attackTimer < fistSlamTime + spikeWaveCreationTime)
            {
                if (inPhase3 && Main.netMode != NetmodeID.MultiplayerClient && attackTimer % fireCrystalReleaseRate == fireCrystalReleaseRate - 1f)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        float[] samples = new float[16];
                        Vector2 crystalSpawnPosition = npc.Infernum().Arena.Center.ToVector2();
                        Vector2 crystalSpawnOffsetDirection = (MathHelper.TwoPi * i / 10f).ToRotationVector2();
                        Collision.LaserScan(crystalSpawnPosition, crystalSpawnOffsetDirection, 24f, 10000f, samples);
                        crystalSpawnPosition += crystalSpawnOffsetDirection * samples.Average();
                        Utilities.NewProjectileBetter(crystalSpawnPosition, crystalSpawnOffsetDirection * -0.004f, ModContent.ProjectileType<GroundFireCrystal>(), 200, 0f);
                    }
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % spikeTrapCreationRate == spikeTrapCreationRate - 1f)
                {
                    Vector2 trapSpawnPosition = npc.Infernum().Arena.TopLeft();
                    Vector2 trapVelocity = Vector2.UnitX * 8f;
                    int spike = Utilities.NewProjectileBetter(trapSpawnPosition, trapVelocity, ModContent.ProjectileType<SpikeTrap>(), 200, 0f);
                    if (Main.projectile.IndexInRange(spike))
                        Main.projectile[spike].ai[1] = 1f;

                    trapSpawnPosition = npc.Infernum().Arena.BottomRight();
                    trapVelocity = Vector2.UnitX * -8f;
                    spike = Utilities.NewProjectileBetter(trapSpawnPosition, trapVelocity, ModContent.ProjectileType<SpikeTrap>(), 200, 0f);
                    if (Main.projectile.IndexInRange(spike))
                        Main.projectile[spike].ai[1] = -1f;
                }
            }
            else
            {
                leftFist.rotation = 0f;
                rightFist.rotation = 0f;
                if (attackTimer >= fistSlamTime + spikeTrapCreationRate + 270f)
                {
                    attackCooldown = ConstAttackCooldown;
                    Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<SpikeTrap>());
                    SelectNextAttackState(npc);
                }
                return;
            }

            // Create platforms in the middle section of the arena.
            if (attackTimer >= fistSlamTime && attackTimer % platformReleaseRate == platformReleaseRate - 1f && inPhase2)
            {
                Vector2 platformSpawnPosition = new(npc.Infernum().Arena.Left + 12f, npc.Infernum().Arena.Center.Y - 40f);
                CreatePlatform(npc, platformSpawnPosition, Vector2.UnitX * 3f);

                platformSpawnPosition = new Vector2(npc.Infernum().Arena.Right - 12f, npc.Infernum().Arena.Center.Y + 40f);
                CreatePlatform(npc, platformSpawnPosition, Vector2.UnitX * -3f);
            }

            // Do the slam animation.
            leftFist.Center = GetLeftFistAttachmentPosition(npc);
            leftFist.position.X = MathHelper.Lerp(leftFist.position.X, npc.Infernum().Arena.Left + 8f, fistSlamInterpolant) - leftFist.width * 0.5f;
            rightFist.Center = GetRightFistAttachmentPosition(npc);
            rightFist.position.X = MathHelper.Lerp(rightFist.position.X, npc.Infernum().Arena.Right - 8f, fistSlamInterpolant) - rightFist.width * 0.5f;
            leftFist.rotation = 0f;
            rightFist.rotation = 0f;

            attackTimer++;
        }

        public static void DoBehavior_HeatRay(NPC npc, Player target, bool inPhase2, bool inPhase3, bool headIsFree, ref float attackTimer, ref float attackCooldown, ref float eyeLaserRayInterpolant)
        {
            int platformReleaseRate = 90;
            int hoverTelegraphTime = 90;
            int fireReleaseRate = 27;
            int fireCircleCount = 9;

            if (inPhase2)
            {
                fireReleaseRate -= 6;
                fireCircleCount++;
            }
            if (inPhase3)
                fireCircleCount += 2;

            NPC leftFist = GetLeftFist(npc);
            NPC rightFist = GetRightFist(npc);
            NPC freeHead = GetFreeHead(npc);

            if (attackTimer >= hoverTelegraphTime + 120f)
            {
                if (headIsFree)
                {
                    freeHead.Center = freeHead.Center.MoveTowards(GetAttachedHead(npc).Center, 12f);
                    ReAttachHead(npc);
                }
            }
            else if (!headIsFree)
                SwapHeads(npc);

            // Reset the rotations of the fists.
            leftFist.rotation = 0f;
            rightFist.rotation = 0f;
            freeHead.damage = 0;

            // Have the head hover in place and perform the telegraph prior to firing.
            if (attackTimer < hoverTelegraphTime)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 300f - freeHead.velocity * 4f;
                float movementSpeed = MathHelper.Lerp(33f, 4.5f, Utils.GetLerpValue(hoverTelegraphTime / 2, hoverTelegraphTime - 5f, attackTimer, true));
                freeHead.velocity = (freeHead.velocity * 7f + freeHead.SafeDirectionTo(hoverDestination) * MathHelper.Min(freeHead.Distance(hoverDestination), movementSpeed)) / 8f;

                // Calculate the telegraph interpolant.
                eyeLaserRayInterpolant = Utils.GetLerpValue(0f, hoverTelegraphTime - 20f, attackTimer, true);

                // Play a telegraph sound prior to firing.
                if (attackTimer == 5f)
                    SoundEngine.PlaySound(CrystylCrusher.ChargeSound, target.Center);
            }

            // Release the lasers from eyes.
            if (attackTimer == hoverTelegraphTime)
            {
                SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    freeHead.velocity *= 0.2f;
                    freeHead.netUpdate = true;

                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 beamSpawnPosition = freeHead.Center + new Vector2(-i * 16f, -7f);
                        Vector2 beamDirection = Vector2.UnitX * i;

                        int beam = Utilities.NewProjectileBetter(beamSpawnPosition, beamDirection, ModContent.ProjectileType<GolemEyeLaserRay>(), 290, 0f);
                        if (Main.projectile.IndexInRange(beam))
                        {
                            Main.projectile[beam].ai[0] = i * MathHelper.PiOver2 / 120f * 0.46f;
                            Main.projectile[beam].ai[1] = freeHead.whoAmI;
                            Main.projectile[beam].netUpdate = true;
                        }
                    }
                }
            }

            // Create platforms below the target in the second phase.
            if (attackTimer % platformReleaseRate == 0f && inPhase2)
            {
                Vector2 platformSpawnPosition = new(target.Center.X, npc.Infernum().Arena.Bottom - 16f);
                CreatePlatform(npc, platformSpawnPosition, -Vector2.UnitY * 1.5f);
            }

            // Release bursts of fire after firing.
            if (attackTimer >= hoverTelegraphTime && attackTimer % fireReleaseRate == fireReleaseRate - 1f && attackTimer < hoverTelegraphTime + 120f)
            {
                SoundEngine.PlaySound(SoundID.Item12, freeHead.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float shootOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < fireCircleCount; i++)
                    {
                        Vector2 fireShootVelocity = (MathHelper.TwoPi * i / fireCircleCount + shootOffsetAngle).ToRotationVector2() * 6.7f;
                        Utilities.NewProjectileBetter(freeHead.Center, fireShootVelocity, ProjectileID.EyeBeam, 190, 0f);
                    }
                }
            }

            attackTimer++;

            if (attackTimer >= hoverTelegraphTime + 210f)
            {
                attackCooldown = ConstAttackCooldown;
                SelectNextAttackState(npc);
            }
        }

        public static void DoBehavior_SpinLaser(NPC npc, Player target, bool inPhase3, ref float attackTimer, ref float coreLaserRayInterpolant, ref float coreLaserRayDirection, ref float attackCooldown)
        {
            int platformReleaseRate = 95;
            int laserTelegraphTime = 75;
            int laserLifetime = 160;
            int coreLaserFireRate = 40;
            float laserArc = MathHelper.Pi * 0.44f;
            if (inPhase3)
            {
                coreLaserFireRate -= 10;
                laserArc *= 1.1f;
            }

            float angularVelocity = laserArc / laserLifetime;

            // Play a telegraph sound prior to firing.
            if (attackTimer == 5f)
                SoundEngine.PlaySound(CrystylCrusher.ChargeSound, target.Center);

            // Create a laser ray telegraph.
            if (attackTimer <= laserTelegraphTime)
            {
                coreLaserRayInterpolant = Utils.GetLerpValue(0f, laserTelegraphTime - 32f, attackTimer, true);
                if (coreLaserRayInterpolant < 1f)
                    coreLaserRayDirection = npc.AngleTo(target.Center).AngleLerp(-MathHelper.PiOver2, 0.84f);
            }

            // Create platforms.
            if (attackTimer % platformReleaseRate == 0f)
            {
                float platformVerticalInterpolant = attackTimer / platformReleaseRate % 3f / 3f;
                Vector2 platformSpawnOffset = new(0f, npc.Infernum().Arena.Height * MathHelper.Lerp(0.2f, 0.8f, platformVerticalInterpolant));
                CreatePlatform(npc, npc.Infernum().Arena.TopLeft() + platformSpawnOffset, Vector2.UnitX * 3f);

                platformSpawnOffset = new Vector2(0f, npc.Infernum().Arena.Height * MathHelper.Lerp(0.2f, 0.8f, 1f - platformVerticalInterpolant));
                CreatePlatform(npc, npc.Infernum().Arena.TopRight() + platformSpawnOffset, Vector2.UnitX * -3f);
            }

            // Cast the laser from the core.
            if (attackTimer == laserTelegraphTime)
            {
                SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int laser = Utilities.NewProjectileBetter(npc.Center, coreLaserRayDirection.ToRotationVector2(), ModContent.ProjectileType<ThermalDeathray>(), 320, 0f);
                    if (Main.projectile.IndexInRange(laser))
                    {
                        Main.projectile[laser].ModProjectile<ThermalDeathray>().AngularVelocity = (MathHelper.WrapAngle(npc.AngleTo(target.Center) - coreLaserRayDirection) > 0f).ToDirectionInt() * angularVelocity;
                        Main.projectile[laser].ModProjectile<ThermalDeathray>().OwnerIndex = npc.whoAmI + 1;
                        Main.projectile[laser].ModProjectile<ThermalDeathray>().Lifetime = laserLifetime;
                        Main.projectile[laser].netUpdate = true;
                    }
                }
            }

            // Create lasers from the core after firing.
            if (attackTimer > laserTelegraphTime && attackTimer % coreLaserFireRate == coreLaserFireRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item12, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 shootVelocity = npc.SafeDirectionTo(target.Center) * 8f;
                    Utilities.NewProjectileBetter(npc.Center, shootVelocity, ProjectileID.EyeBeam, 200, 0f);
                }
            }

            // Select the next attack shortly after the laser goes away.
            if (attackTimer >= laserTelegraphTime + laserLifetime + 60f)
            {
                attackCooldown = ConstAttackCooldown;
                SelectNextAttackState(npc);
            }

            attackTimer++;
        }

        public static void DoBehavior_Slingshot(NPC npc, Player target, bool inPhase2, bool inPhase3, ref float attackTimer, ref float fistSlamDestinationX, ref float fistSlamDestinationY, ref float slingshotArmToCharge,
            ref float slingshotRotation, ref float attackCounter, ref float attackCooldown)
        {
            int dustTelegraphTime = 25;
            int fistSlamTime = 15;
            int bodyReelTime = 25;
            int shootTime = 150;
            int slingshotCount = 2;
            int fistShootRate = 16;
            int platformReleaseRate = 90;
            float armSlamInterpolant = 0f;
            float bodySlamInterpolant = 0f;

            if (inPhase2)
                fistShootRate -= 3;
            if (inPhase3)
            {
                fistShootRate -= 4;
                shootTime -= 45;
            }

            npc.noTileCollide = false;
            NPC leftFist = GetLeftFist(npc);
            NPC rightFist = GetRightFist(npc);
            NPC freeHead = GetFreeHead(npc);
            NPC attachedHead = GetAttachedHead(npc);
            Vector2 leftFistAttachmentPosition = GetLeftFistAttachmentPosition(npc);
            Vector2 rightFistAttachmentPosition = GetRightFistAttachmentPosition(npc);

            // Determine the initial slingshot rotation.
            if (attackTimer == 1f)
            {
                slingshotRotation = (-Vector2.UnitY).ToRotation();
                npc.netUpdate = true;
            }

            NPC fistToChargeWith = slingshotArmToCharge == 1f ? rightFist : leftFist;
            NPC otherFist = fistToChargeWith.whoAmI == leftFist.whoAmI ? rightFist : leftFist;
            fistToChargeWith.rotation = slingshotRotation;
            if (fistToChargeWith.whoAmI == leftFist.whoAmI)
                fistToChargeWith.rotation += MathHelper.Pi;

            otherFist.rotation = 0f;

            float[] samples = new float[24];
            Vector2 fistStart = fistToChargeWith.whoAmI == leftFist.whoAmI ? leftFistAttachmentPosition : rightFistAttachmentPosition;
            Vector2 offsetDirection = -Vector2.UnitY;
            Collision.LaserScan(fistStart, offsetDirection, 30f, 10000f, samples);
            Vector2 fistEnd = fistStart + offsetDirection * samples.Average();

            // Determine the initial fist destination.

            if ((fistSlamDestinationX == 0f || fistSlamDestinationY == 0f) && attackTimer <= 5f)
            {
                fistSlamDestinationX = fistEnd.X;
                fistSlamDestinationY = fistEnd.Y;
                npc.netUpdate = true;
            }
            else
            {
                if (fistSlamDestinationX == 0f || fistSlamDestinationY == 0f)
                {
                    fistSlamDestinationX = fistStart.X;
                    fistSlamDestinationY = fistStart.Y;
                }
                fistEnd = new Vector2(fistSlamDestinationX, fistSlamDestinationY);
            }

            Rectangle deflatedArena = npc.Infernum().Arena;
            deflatedArena.Inflate(-20, 16);
            while (!Utils.CenteredRectangle(new Vector2(fistSlamDestinationX, fistSlamDestinationY), Vector2.One).Intersects(deflatedArena))
            {
                Vector2 directionToCenter = (npc.Infernum().Arena.Center.ToVector2() - new Vector2(fistSlamDestinationX, fistSlamDestinationY)).SafeNormalize(Vector2.UnitY);
                fistSlamDestinationX += directionToCenter.X * 4f;
                fistSlamDestinationY += directionToCenter.Y * 4f;
            }

            // Create platforms.
            if (attackTimer % platformReleaseRate == 0f && inPhase2)
            {
                float platformVerticalInterpolant = attackTimer / platformReleaseRate % 3f / 3f;
                Vector2 platformSpawnOffset = new(0f, npc.Infernum().Arena.Height * MathHelper.Lerp(0.2f, 0.8f, platformVerticalInterpolant));
                CreatePlatform(npc, npc.Infernum().Arena.TopLeft() + platformSpawnOffset, Vector2.UnitX * 3f);

                platformSpawnOffset = new Vector2(0f, npc.Infernum().Arena.Height * MathHelper.Lerp(0.2f, 0.8f, 1f - platformVerticalInterpolant));
                CreatePlatform(npc, npc.Infernum().Arena.TopRight() + platformSpawnOffset, Vector2.UnitX * -3f);
            }

            // Create fire sparks as a telegraph that indicates which fist will charge.
            if (Main.netMode != NetmodeID.Server && attackTimer < dustTelegraphTime)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 fireSpawnVelocity = offsetDirection * Main.rand.NextFloat(6f, 27f) + Main.rand.NextVector2Circular(4f, 4f);
                    float scale = Main.rand.NextFloat(0.4f, 1f);
                    GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(fistStart, fireSpawnVelocity, scale, Color.Orange, 50));
                }
            }

            // Make arms do a slam effect.
            else if (attackTimer <= dustTelegraphTime + fistSlamTime)
            {
                armSlamInterpolant = (float)Math.Pow(Utils.GetLerpValue(0f, fistSlamTime, attackTimer - dustTelegraphTime, true), 2D);

                if (attackTimer == dustTelegraphTime + fistSlamTime)
                {
                    // Create impact effects.
                    if (attackTimer == fistSlamTime - 1f)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, fistEnd);
                        Utils.PoofOfSmoke(fistEnd);
                        Collision.HitTiles(fistEnd, offsetDirection, 40, 40);
                    }
                }
            }

            // Make the body lunge into position.
            else if (attackTimer <= dustTelegraphTime + fistSlamTime + bodyReelTime)
            {
                armSlamInterpolant = 1f;
                bodySlamInterpolant = (float)Math.Pow(Utils.GetLerpValue(0f, bodyReelTime, attackTimer - dustTelegraphTime - fistSlamTime, true), 2D);

                // Play a launch sound.
                if (attackTimer == dustTelegraphTime + fistSlamTime + 4f)
                    SoundEngine.PlaySound(ScorchedEarth.ShootSound, npc.Center);

                otherFist.Center = otherFist.whoAmI == leftFist.whoAmI ? leftFistAttachmentPosition : rightFistAttachmentPosition;
            }

            // Have the other arm release fist rockets at the target.
            else if (attackTimer < dustTelegraphTime + fistSlamTime + bodyReelTime + shootTime)
            {
                armSlamInterpolant = 1f;
                bodySlamInterpolant = 1f;

                // Rotate the fists around the body over the course of 3 seconds, spawning projectiles every so often
                float rotation = npc.AngleTo(target.Center) + (float)Math.Cos(attackTimer / 13f) * 0.31f;
                otherFist.Center = npc.Center + rotation.ToRotationVector2() * 180f;
                otherFist.rotation = rotation;
                if (otherFist.whoAmI == leftFist.whoAmI)
                    otherFist.rotation += MathHelper.Pi;

                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % fistShootRate == 0f)
                {
                    int type = ModContent.ProjectileType<FistBullet>();
                    int bullet = Utilities.NewProjectileBetter(otherFist.Center, Vector2.Zero, type, 200, 0);
                    if (Main.projectile.IndexInRange(bullet))
                    {
                        Main.projectile[bullet].Infernum().ExtraAI[0] = 0f;
                        Main.projectile[bullet].Infernum().ExtraAI[2] = target.whoAmI;
                        Main.projectile[bullet].rotation = rotation;
                        Main.projectile[bullet].netUpdate = true;
                    }
                }

                // Release bursts of fire after firing.
                if (attackTimer % 35f == 34f)
                {
                    SoundEngine.PlaySound(SoundID.Item12, freeHead.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float shootOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                        for (int i = 0; i < 9; i++)
                        {
                            Vector2 fireShootVelocity = (MathHelper.TwoPi * i / 9f + shootOffsetAngle).ToRotationVector2() * 6.7f;
                            Utilities.NewProjectileBetter(attachedHead.Center, fireShootVelocity, ProjectileID.EyeBeam, 200, 0f);
                        }
                    }
                }
            }

            // Jump back to the center of the arena.
            else
            {
                npc.noGravity = false;
                fistSlamDestinationX = fistSlamDestinationY = 0f;
                armSlamInterpolant = Utils.GetLerpValue(15f, 0f, attackTimer - (dustTelegraphTime + fistSlamTime + bodyReelTime + shootTime), true);
                bodySlamInterpolant = 0f;
                leftFist.rotation = 0f;
                rightFist.rotation = 0f;
                otherFist.Center = Vector2.Lerp(otherFist.Center, otherFist.whoAmI == leftFist.whoAmI ? leftFistAttachmentPosition : rightFistAttachmentPosition, 0.15f);

                if (attackTimer >= dustTelegraphTime + fistSlamTime + bodyReelTime + shootTime + 100f)
                {
                    // Slow down dramatically on the X axis when there's little Y movement, to prevent sliding.
                    if (Math.Abs(npc.velocity.Y) <= 0.35f)
                        npc.velocity.X *= 0.8f;
                    else
                        npc.position.Y += 5f;
                }

                if (attackTimer == dustTelegraphTime + fistSlamTime + bodyReelTime + shootTime + 30f)
                    npc.velocity.X = Utilities.GetProjectilePhysicsFiringVelocity(npc.Center, npc.Infernum().Arena.Center.ToVector2(), 0.3f, 21f, out _).X;

                if (attackTimer >= dustTelegraphTime + fistSlamTime + bodyReelTime + shootTime + 180f)
                {
                    npc.velocity.X = 0f;

                    if (attackCounter < slingshotCount)
                    {
                        attackTimer = 0f;
                        attackCounter++;
                    }
                    else
                    {
                        attackCounter = 0f;
                        attackCooldown = ConstAttackCooldown;
                        slingshotRotation = 0f;
                        SelectNextAttackState(npc);
                    }
                    npc.netUpdate = true;
                }
            }

            fistToChargeWith.Center = Vector2.Lerp(fistStart, fistEnd, armSlamInterpolant);
            Vector2 bodyDestination = fistEnd - offsetDirection * 100f;
            npc.Center = Vector2.Lerp(npc.Center, bodyDestination, (float)Math.Pow(bodySlamInterpolant, 8.4D));

            if (bodySlamInterpolant > 0f)
                npc.velocity = Vector2.Zero;

            attackTimer++;
        }

        public static void DoBehavior_SpikeRush(NPC npc, ref float attackTimer, ref float attackCooldown)
        {
            int platformReleaseRate = 82;
            int laserReleaseRate = 10;
            int rushTime = 420;
            float platformRiseSpeed = 7.5f;

            // Destroy all old platforms and create a few new ones in their place
            if (attackTimer == 25f)
            {
                DestroyAllPlatforms();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int platformX = (int)MathHelper.Lerp(npc.Infernum().Arena.Left + 60f, npc.Infernum().Arena.Right - 60f, i / 2f);
                        int platformY = npc.Infernum().Arena.Bottom - 16;
                        CreatePlatform(npc, new Vector2(platformX, platformY), Vector2.UnitY * -platformRiseSpeed);
                    }
                }
            }

            // Create lasers from the sides of the arena.
            if (attackTimer >= 25f && attackTimer % laserReleaseRate == laserReleaseRate - 1f)
            {
                Vector2 laserSpawnOffsetFactors = new(Main.rand.NextBool().ToDirectionInt() * 0.95f, Main.rand.NextFloat(-0.85f, 0.85f));
                Vector2 laserSpawnPosition = npc.Infernum().Arena.Center.ToVector2() + npc.Infernum().Arena.Size() * laserSpawnOffsetFactors * 0.5f;
                SoundEngine.PlaySound(SoundID.Item12, laserSpawnPosition);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 laserShootVelocity = Vector2.UnitX * Math.Sign(npc.Infernum().Arena.Center.X - laserSpawnPosition.X) * 5f;
                    Utilities.NewProjectileBetter(laserSpawnPosition, laserShootVelocity, ProjectileID.EyeBeam, 200, 0f);
                }
            }

            // Create new platforms afterwards.
            if (attackTimer % platformReleaseRate == platformReleaseRate - 1f)
            {
                int platformX = (int)MathHelper.Lerp(npc.Infernum().Arena.Left + 150f, npc.Infernum().Arena.Right - 150f, Main.rand.NextFloat());
                int platformY = npc.Infernum().Arena.Bottom - 16;
                CreatePlatform(npc, new Vector2(platformX, platformY), Vector2.UnitY * -platformRiseSpeed);
            }

            attackTimer++;

            if (attackTimer >= rushTime + 25f)
            {
                attackCooldown = ConstAttackCooldown;
                SelectNextAttackState(npc);
            }
        }

        public static void CreatePlatform(NPC npc, Vector2 spawnPosition, Vector2 velocity)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int platform = NPC.NewNPC(npc.GetSource_FromAI(), (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<GolemArenaPlatform>());
            if (Main.npc.IndexInRange(platform))
            {
                Main.npc[platform].velocity = velocity;
                Main.npc[platform].netUpdate = true;
            }
        }

        public static void DestroyAllPlatforms()
        {
            int platformID = ModContent.NPCType<GolemArenaPlatform>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == platformID)
                {
                    for (int j = 0; j < 12; j++)
                        Dust.NewDust(Main.npc[i].position, Main.npc[i].width, Main.npc[i].height, 148);

                    Main.npc[i].active = false;
                    Main.npc[i].netUpdate = true;
                }
            }
        }

        public static void DoBehavior_EnterSecondPhase(NPC npc, float phase2TransitionTimer)
        {
            // Create spikes throughout the arena at first. This will activate soon afterwards.
            if (phase2TransitionTimer == 1f)
            {
                for (float i = npc.Infernum().Arena.Left + 16f; i < npc.Infernum().Arena.Right; i += 16f)
                {
                    Vector2 top = Utilities.GetGroundPositionFrom(new Vector2(i, npc.Infernum().Arena.Center.Y), new Searches.Up(9001)).Floor();
                    top.Y -= 16f;
                    Vector2 bottom = Utilities.GetGroundPositionFrom(new Vector2(i, npc.Infernum().Arena.Center.Y)).Floor();

                    if (Collision.CanHit(top, 1, 1, top + Vector2.UnitY * 100f, 1, 1))
                    {
                        int topSpike = Utilities.NewProjectileBetter(top, Vector2.Zero, ModContent.ProjectileType<StationarySpikeTrap>(), 230, 0f);
                        if (Main.projectile.IndexInRange(topSpike))
                        {
                            Main.projectile[topSpike].ModProjectile<StationarySpikeTrap>().SpikeDirection = 1f;
                            Main.projectile[topSpike].netUpdate = true;
                        }
                    }

                    if (Collision.CanHit(bottom, 1, 1, bottom - Vector2.UnitY * 100f, 1, 1))
                    {
                        int bottomSpike = Utilities.NewProjectileBetter(bottom, Vector2.Zero, ModContent.ProjectileType<StationarySpikeTrap>(), 230, 0f);
                        if (Main.projectile.IndexInRange(bottomSpike))
                        {
                            Main.projectile[bottomSpike].ModProjectile<StationarySpikeTrap>().SpikeDirection = -1f;
                            Main.projectile[bottomSpike].netUpdate = true;
                        }
                    }
                }
            }

            // Create a rumble effect.
            if (phase2TransitionTimer > 30f && phase2TransitionTimer < 75f && Main.LocalPlayer.WithinRange(npc.Center, 5000f))
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = 10f;

            // Create some platforms before the spike traps are released.
            if (Main.netMode != NetmodeID.MultiplayerClient && phase2TransitionTimer == 35f)
            {
                for (int i = 0; i < 7; i++)
                {
                    int platformX = (int)MathHelper.Lerp(npc.Infernum().Arena.Left + 60f, npc.Infernum().Arena.Right - 60f, i / 6f);
                    int platformY = npc.Infernum().Arena.Bottom - 16;
                    CreatePlatform(npc, new Vector2(platformX, platformY), Vector2.UnitY * -2.5f);
                }
            }

            // Make all spike traps release their spears.
            if (phase2TransitionTimer == 75f)
            {
                SoundEngine.PlaySound(CommonCalamitySounds.MeatySlashSound);
                foreach (Projectile spear in Utilities.AllProjectilesByID(ModContent.ProjectileType<StationarySpikeTrap>()))
                {
                    spear.ModProjectile<StationarySpikeTrap>().SpikesShouldExtendOutward = true;
                    spear.netUpdate = true;
                }
            }

            // Release a burst of fireballs outward. This happens after some platforms have spawned, and serves to teach the player about the platforms by
            // forcing them to dodge the burst while utilizing them.
            if (phase2TransitionTimer == 120f)
            {
                SoundEngine.PlaySound(HolyBlast.ImpactSound);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float shootOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < 16; i++)
                    {
                        Vector2 fireShootVelocity = (MathHelper.TwoPi * i / 16f + shootOffsetAngle).ToRotationVector2() * 8f;
                        Utilities.NewProjectileBetter(npc.Center, fireShootVelocity, ProjectileID.EyeBeam, 200, 0f);
                    }
                }
            }
        }

        public static void ReAttachHead(NPC npc)
        {
            NPC FreeHeadNPC = Main.npc[(int)npc.Infernum().ExtraAI[3]];
            NPC AttachedHeadNPC = Main.npc[(int)npc.Infernum().ExtraAI[2]];

            // If the free head is close enough or it will pass the correct position, reattach it
            if (FreeHeadNPC.Distance(AttachedHeadNPC.Center) < 70f)
            {
                SwapHeads(npc);
                return;
            }

            // Otherwise accelerate towards the proper position
            FreeHeadNPC.velocity += FreeHeadNPC.SafeDirectionTo(AttachedHeadNPC.Center) * 0.35f;
        }

        public static void SwapHeads(NPC npc)
        {
            ref float AttachedHeadNPC = ref npc.Infernum().ExtraAI[2];
            ref float FreeHeadNPC = ref npc.Infernum().ExtraAI[3];
            ref float FreeHead = ref npc.Infernum().ExtraAI[4];

            bool CurrentlyAttached = !Main.npc[(int)AttachedHeadNPC].dontTakeDamage;

            if (CurrentlyAttached)
            {
                Main.npc[(int)AttachedHeadNPC].dontTakeDamage = true;
                Main.npc[(int)FreeHeadNPC].dontTakeDamage = false;
                FreeHead = 1f;
            }
            else
            {
                Main.npc[(int)AttachedHeadNPC].dontTakeDamage = false;
                Main.npc[(int)FreeHeadNPC].dontTakeDamage = true;
                FreeHead = 0f;
            }
        }

        public static void SelectNextAttackState(NPC npc)
        {
            bool inRealTemple = false;
            bool inPhase2 = npc.life < npc.lifeMax * Phase2LifeRatio;
            bool inPhase3 = npc.life < npc.lifeMax * Phase3LifeRatio;
            ref float AttackState = ref npc.ai[1];
            ref float AttackTimer = ref npc.ai[2];
            ref float PreviousAttackState = ref npc.Infernum().ExtraAI[8];
            ref float PreviousAttackState2 = ref npc.Infernum().ExtraAI[20];

            int x = (int)(npc.Center.X / 16f);
            int y = (int)(npc.Center.Y / 16f);
            for (int i = x - 10; i < x + 10; i++)
            {
                for (int j = y - 10; j < y + 10; j++)
                {
                    if (!inRealTemple && Main.tile[i, j].WallType == WallID.LihzahrdBrickUnsafe)
                    {
                        inRealTemple = true;
                        goto LeaveLoop;
                    }
                }
            }
        LeaveLoop:

            List<GolemAttackState> possibleAttacks = new()
            {
                GolemAttackState.FloorFire,
                GolemAttackState.SpikeTrapWaves,
                GolemAttackState.HeatRay,
            };

            if (inRealTemple)
                possibleAttacks.Add(GolemAttackState.Slingshot);
            if (inPhase2)
            {
                possibleAttacks.Add(GolemAttackState.SpinLaser);
                possibleAttacks.Add(GolemAttackState.SpikeRush);
            }
            if (!inPhase3)
                possibleAttacks.Add(GolemAttackState.FistSpin);

            int failsafeCounter = 0;
            GolemAttackState NextAttack;
            do
            {
                NextAttack = Main.rand.Next(possibleAttacks);
                failsafeCounter++;

                if (failsafeCounter >= 100)
                    break;
            }
            while ((float)NextAttack == AttackState || (float)NextAttack == PreviousAttackState2);

            PreviousAttackState2 = PreviousAttackState;
            PreviousAttackState = AttackState;
            AttackState = (float)NextAttack;
            AttackTimer = 0f;
        }

        public static void DespawnNPC(int NPCID)
        {
            Main.npc[NPCID].life = 0;
            Main.npc[NPCID].active = false;
            Main.npc[NPCID].netUpdate = true;

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPCID);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Golem/GolemBody").Value;
            Texture2D glowMask = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Golem/BodyGlow").Value;
            Rectangle rect = new(0, 0, texture.Width, texture.Height);
            Vector2 drawPos = npc.Center - Main.screenPosition;
            drawPos += new Vector2(4, -12);

            // Draw the second phase back afterimage if applicable.
            float backAfterimageInterpolant = Utils.GetLerpValue(0f, 90f, npc.Infernum().ExtraAI[21], true) * npc.Opacity;
            if (backAfterimageInterpolant > 0f)
            {
                for (int i = 0; i < 6; i++)
                {
                    float colorInterpolant = (float)Math.Cos(MathHelper.SmoothStep(0f, MathHelper.TwoPi, i / 6f) + Main.GlobalTimeWrappedHourly * 10f) * 0.5f + 0.5f;
                    Color backAfterimageColor = Color.Lerp(Color.Yellow, Color.Orange, colorInterpolant);
                    backAfterimageColor *= backAfterimageInterpolant;
                    backAfterimageColor.A /= 8;
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * backAfterimageInterpolant * 8f;
                    Main.spriteBatch.Draw(texture, drawPos + drawOffset, npc.frame, backAfterimageColor, npc.rotation, rect.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                }
            }

            Main.spriteBatch.Draw(texture, drawPos, rect, lightColor * npc.Opacity, npc.rotation, rect.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowMask, drawPos, rect, Color.White * npc.Opacity, npc.rotation, rect.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            // Draw laser ray telegraphs.
            float laserRayTelegraphInterpolant = npc.Infernum().ExtraAI[17];
            if (laserRayTelegraphInterpolant > 0f)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                Texture2D line = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/BloomLine").Value;
                Color outlineColor = Color.Lerp(Color.OrangeRed, Color.White, laserRayTelegraphInterpolant);
                Vector2 origin = new(line.Width / 2f, line.Height);
                Vector2 beamScale = new(laserRayTelegraphInterpolant * 0.5f, 2.4f);

                Vector2 drawPosition = drawPos + Vector2.UnitY * 20f;
                Vector2 beamDirection = -npc.Infernum().ExtraAI[18].ToRotationVector2();
                float beamRotation = beamDirection.ToRotation() - MathHelper.PiOver2;
                Main.spriteBatch.Draw(line, drawPosition, null, outlineColor, beamRotation, origin, beamScale, 0, 0f);

                Main.spriteBatch.ResetBlendState();
            }

            return false;
        }

        private static void CreateGolemArena(NPC npc)
        {
            DeleteGolemArena();

            if (!Main.player.IndexInRange(npc.target))
                return;

            Player closest = Main.player[npc.target];

            int num = (int)closest.Center.X / 16;
            int num2 = (int)closest.Center.Y / 16;
            int altarX = 0;
            int altarY = 0;
            bool inRealTemple = false;
            for (int i = num - 20; i < num + 20; i++)
            {
                for (int j = num2 - 20; j < num2 + 20; j++)
                {
                    if (!inRealTemple)
                        inRealTemple = Main.tile[i, j].WallType == WallID.LihzahrdBrickUnsafe;

                    if (Main.tile[i, j].HasTile && Main.tile[i, j].TileType == TileID.LihzahrdAltar)
                    {
                        altarX = i;
                        altarY = j;
                    }
                }
            }
            if (altarX == 0 || altarY == 0)
            {
                altarX = num;
                altarY = num2;
            }

            int arenaBottom = altarY + 15;
            Vector2 arenaCenter = new(altarX, arenaBottom - (ArenaHeight / 2) - 5);
            Vector2 arenaArea = new(ArenaWidth, ArenaHeight);
            npc.Infernum().Arena = Utils.CenteredRectangle(arenaCenter * 16f, arenaArea * 16f);
            npc.Center = npc.Infernum().Arena.Center.ToVector2();

            int left = (int)(npc.Infernum().Arena.Center().X / 16 - arenaArea.X * 0.5f);
            int right = (int)(npc.Infernum().Arena.Center().X / 16 + arenaArea.X * 0.5f);
            int top = (int)(npc.Infernum().Arena.Center().Y / 16 - arenaArea.Y * 0.5f);
            int bottom = (int)(npc.Infernum().Arena.Center().Y / 16 + arenaArea.Y * 0.5f);
            int arenaTileType = ModContent.TileType<GolemArena>();
            for (int i = left; i <= right; i++)
            {
                for (int j = top; j <= bottom; j++)
                {
                    if (!WorldGen.InWorld(i, j))
                        continue;

                    // Break existing tiles if inside of a real temple.
                    // This is done to ensure that there are no unexpected tiles that may trivialize the platforming aspect of the fight.
                    // However, this is not done if the fight is done outside of the temple, as it's possible that in that circumstance
                    // the player might be fighting Golem near their base, which could result in serious damage happening.
                    // If the player is fighting Golem outside of the temple it is likely that they have already beaten him anyways.
                    if (inRealTemple)
                    {
                        Tile tile = CalamityUtils.ParanoidTileRetrieval(i, j);
                        if (tile.HasTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]))
                        {
                            if (tile.TileType != TileID.LihzahrdBrick && tile.TileType != TileID.LihzahrdAltar && tile.TileType != TileID.Traps && tile.TileType != TileID.WoodenSpikes && tile.TileType != arenaTileType)
                                WorldGen.KillTile(i, j);
                        }
                    }

                    // Create arena tiles.
                    if ((i == left || i == right || j == top || j == bottom) && !Main.tile[i, j].HasTile)
                    {
                        Main.tile[i, j].TileType = (ushort)arenaTileType;
                        Main.tile[i, j].Get<TileWallWireStateData>().HasTile = true;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendTileSquare(-1, i, j, 1, TileChangeType.None);
                        else
                            WorldGen.SquareTileFrame(i, j, true);
                    }

                    // Erase old arena tiles.
                    else if (Framing.GetTileSafely(i, j).TileType == arenaTileType)
                        Main.tile[i, j].Get<TileWallWireStateData>().HasTile = false;
                }
            }

        }

        private static void DeleteGolemArena()
        {
            int surface = (int)Main.worldSurface;
            for (int i = 0; i < Main.maxTilesX; i++)
            {
                for (int j = 0; j < surface; j++)
                {
                    if (Main.tile[i, j] != null)
                    {
                        if (Main.tile[i, j].TileType == ModContent.TileType<Tiles.GolemArena>())
                        {
                            Main.tile[i, j].ClearEverything();
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendTileSquare(-1, i, j, 1, TileChangeType.None);
                            }
                            else
                            {
                                WorldGen.SquareTileFrame(i, j, true);
                            }
                        }
                    }
                }
            }
        }

        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Golem's eye color will change depending on it's attack. Keep your own eyes peeled!";
        }
    }
}
