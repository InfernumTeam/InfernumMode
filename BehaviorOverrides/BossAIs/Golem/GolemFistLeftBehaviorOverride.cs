using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class GolemFistLeftBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.GolemFistLeft;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc) => DoFistAI(npc, true);

        public static bool DoFistAI(NPC npc, bool leftFist)
        {
            NPC golemBody = Main.npc[(int)npc.ai[0]];
            Vector2 leftHandCenterPos = new Vector2(golemBody.Left.X, golemBody.Left.Y);
            Vector2 rightHandCenterPos = new Vector2(golemBody.Right.X, golemBody.Right.Y);

            float attackState = golemBody.ai[1];

            // if not doing anything do nothing
            if (attackState != (float)GolemAttackState.ArmBullets && attackState != (float)GolemAttackState.FistSpin)
            {
                if (leftFist)
                    npc.Center = leftHandCenterPos;
                else
                    npc.Center = rightHandCenterPos;

                return false;
            }

            switch ((GolemAttackState)attackState)
            {
                case GolemAttackState.ArmBullets:
                    ArmBulletsFistAttack(npc, leftFist);
                    break;
                case GolemAttackState.FistSpin:
                    FistSpinFistAttack(npc, leftFist);
                    break;
            }

            return false;
        }

        private static void ArmBulletsFistAttack(NPC npc, bool leftFist)
        {

        }

        private static void FistSpinFistAttack(NPC npc, bool leftFist)
        {

        }
    }
}
