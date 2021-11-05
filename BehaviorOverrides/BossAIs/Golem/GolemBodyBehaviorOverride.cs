using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public enum GolemAttackState
    {
        ArmBullets,
        FistSpin,
        SpikeTrapWaves,
        HeatRay,
        SpinLaser,
    }

    public class GolemBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Golem;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public static int ArenaWidth = 115;
        public static int ArenaHeight = 105;

        public override bool PreAI(NPC npc)
        {
            ref float AITimer = ref npc.ai[0];
            ref float AttackState = ref npc.ai[1];
            ref float AttackTimer = ref npc.ai[2];

            ref float LeftFistNPC = ref npc.Infernum().ExtraAI[0];
            ref float RightFistNPC = ref npc.Infernum().ExtraAI[1];
            ref float AttachedHeadNPC = ref npc.Infernum().ExtraAI[2];
            ref float FreeHeadNPC = ref npc.Infernum().ExtraAI[3];

            Vector2 attachedHeadCenterPos = new Vector2(npc.Center.X, npc.Top.Y);

            if (AITimer == 0f)
            {
                // If the NPC cap is reached, the fight will break, so just don't do anything if so
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
                npc.life = npc.lifeMax = 80000;
                npc.noGravity = true;
                npc.noTileCollide = false;
                npc.netUpdate = true;

                Vector2 leftHandCenterPos = new Vector2(npc.Left.X, npc.Left.Y);
                Vector2 rightHandCenterPos = new Vector2(npc.Right.X, npc.Right.Y);

                int freeHeadInt = NPC.NewNPC((int)npc.Center.X - 55, (int)npc.Top.Y, NPCID.GolemHeadFree);
                Main.npc[freeHeadInt].Center = attachedHeadCenterPos;
                Main.npc[freeHeadInt].dontTakeDamage = true;
                Main.npc[freeHeadInt].noGravity = true;
                Main.npc[freeHeadInt].noTileCollide = true;
                Main.npc[freeHeadInt].lifeMax = Main.npc[freeHeadInt].life = npc.lifeMax;
                Main.npc[freeHeadInt].ai[0] = npc.whoAmI;
                Main.npc[freeHeadInt].netUpdate = true;
                FreeHeadNPC = freeHeadInt;

                int attachedHeadInt = NPC.NewNPC((int)npc.Center.X - 55, (int)npc.Top.Y, NPCID.GolemHead);
                Main.npc[attachedHeadInt].Center = attachedHeadCenterPos;
                Main.npc[attachedHeadInt].lifeMax = Main.npc[attachedHeadInt].life = npc.lifeMax;
                Main.npc[attachedHeadInt].noGravity = true;
                Main.npc[attachedHeadInt].noTileCollide = true;
                Main.npc[attachedHeadInt].ai[0] = npc.whoAmI;
                Main.npc[attachedHeadInt].netUpdate = true;
                AttachedHeadNPC = attachedHeadInt;

                int leftHand = NPC.NewNPC((int)npc.Left.X, (int)npc.Left.Y, NPCID.GolemFistLeft);
                Main.npc[leftHand].lifeMax = Main.npc[leftHand].life = 1;
                Main.npc[leftHand].dontTakeDamage = true;
                Main.npc[leftHand].noGravity = true;
                Main.npc[leftHand].noTileCollide = false;
                Main.npc[leftHand].ai[0] = npc.whoAmI;
                Main.npc[leftHand].Center = leftHandCenterPos;
                Main.npc[leftHand].netUpdate = true;
                LeftFistNPC = leftHand;

                int rightHand = NPC.NewNPC((int)npc.Right.X, (int)npc.Right.Y, NPCID.GolemFistRight);
                Main.npc[rightHand].lifeMax = Main.npc[rightHand].life = 1;
                Main.npc[rightHand].dontTakeDamage = true;
                Main.npc[rightHand].noGravity = true;
                Main.npc[rightHand].noTileCollide = false;
                Main.npc[rightHand].ai[0] = npc.whoAmI;
                Main.npc[rightHand].Center = rightHandCenterPos;
                Main.npc[rightHand].netUpdate = true;
                RightFistNPC = rightHand;

                CreateGolemArena();
            }

            // Aquire a new target if the current one is dead or inactive.
            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                npc.TargetClosest(false);

                // If no possible target was found, fly away.
                if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                {
                    npc.velocity.Y += 0.5f;
                    if (npc.timeLeft > 120)
                        npc.timeLeft = 120;
                    if (!npc.WithinRange(Main.player[npc.target].Center, 4200f))
                    {
                        npc.life = 0;
                        npc.active = false;
                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);

                        DespawnNPC((int)AttachedHeadNPC);
                        DespawnNPC((int)FreeHeadNPC);
                        DespawnNPC((int)LeftFistNPC);
                        DespawnNPC((int)RightFistNPC);
                    }

                    return false;
                }
                npc.netUpdate = true;
            }
            else
                npc.timeLeft = 7200;

            ref NPC freeHead = ref Main.npc[(int)FreeHeadNPC];
            ref NPC attachedHead = ref Main.npc[(int)AttachedHeadNPC];
            ref NPC leftFist = ref Main.npc[(int)LeftFistNPC];
            ref NPC rightFist = ref Main.npc[(int)RightFistNPC];

            // Sync the heads, and end the fight if necessary
            if (!attachedHead.active || !freeHead.active || attachedHead.life <= 0 || freeHead.life <= 0)
            {
                DespawnNPC((int)AttachedHeadNPC);
                DespawnNPC((int)FreeHeadNPC);
                DespawnNPC((int)LeftFistNPC);
                DespawnNPC((int)RightFistNPC);

                npc.life = 0;
                npc.HitEffect();
                npc.checkDead();
                npc.NPCLoot();
                npc.active = false;

                DeleteGolemArena();
            }
            else
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
                if (!freeHead.dontTakeDamage)
                    freeHead.Center = attachedHeadCenterPos;
            }

            float LifeRatio = npc.life / npc.lifeMax;
            npc.dontTakeDamage = true;

            if (AITimer < 60f)
            {
                if (npc.velocity.Y == 0f)
                {
                    AITimer = 61f;
                    AttackState = (float)GolemAttackState.HeatRay;
                }
                else
                    npc.velocity.Y += 1f;

                return false;
            }

            switch ((GolemAttackState)AttackState)
            {
                case GolemAttackState.ArmBullets:
                    ArmBulletsAttack(npc);
                    break;
                case GolemAttackState.FistSpin:
                    FistSpinAttack(npc);
                    break;
                case GolemAttackState.SpikeTrapWaves:
                    SpikeTrapWavesAttack(npc);
                    break;
                case GolemAttackState.HeatRay:
                    HeatRayAttack(npc);
                    break;
                case GolemAttackState.SpinLaser:
                    SpinLaserAttack(npc);
                    break;
            }

            AITimer++;
            return false;
        }

        private void ArmBulletsAttack(NPC npc)
        {
            
        }

        private void FistSpinAttack(NPC npc)
        {

        }

        private void HeatRayAttack(NPC npc)
        {

        }

        private void SpikeTrapWavesAttack(NPC npc)
        {

        }

        private void SpinLaserAttack(NPC npc)
        {

        }

        private void GoToNextAttack(NPC npc)
        {
            ref float AttackState = ref npc.ai[1];
            ref float AttackTimer = ref npc.ai[2];

            GolemAttackState NextAttack = (GolemAttackState)Main.rand.Next(0, Enum.GetNames(typeof(GolemAttackState)).Length + 1);
            while ((float)NextAttack == AttackState)
                NextAttack = (GolemAttackState)Main.rand.Next(0, Enum.GetNames(typeof(GolemAttackState)).Length + 1);

            AttackState = (float)NextAttack;
            AttackTimer = 0f;
        }

        private void DespawnNPC(int NPCID)
        {
            Main.npc[NPCID].life = 0;
            Main.npc[NPCID].active = false;
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPCID);
        }

        private void SwapHeads(NPC npc)
        {
            ref float AttachedHeadNPC = ref npc.Infernum().ExtraAI[2];
            ref float FreeHeadNPC = ref npc.Infernum().ExtraAI[3];

            bool CurrentlyAttached = !Main.npc[(int)AttachedHeadNPC].dontTakeDamage;

            if (CurrentlyAttached)
            {
                Main.npc[(int)AttachedHeadNPC].dontTakeDamage = true;
                Main.npc[(int)FreeHeadNPC].dontTakeDamage = false;
            }
            else
            {
                Main.npc[(int)AttachedHeadNPC].dontTakeDamage = false;
                Main.npc[(int)FreeHeadNPC].dontTakeDamage = true;
            }
        }

        private void CreateGolemArena()
        {

        }

        private void DeleteGolemArena()
        {

        }
    }
}
