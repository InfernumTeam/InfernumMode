using CalamityMod.NPCs.StormWeaver;
using InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode.BehaviorOverrides.BossAIs.Signus
{
    public class SignusBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<StormWeaverHead>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        public enum SignusAttackType
        {
            Test
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // Regularly ade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.2f, 0f, 1f);

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            switch ((SignusAttackType)(int)attackState)
            {
                case SignusAttackType.Test:
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void SelectNewAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 4; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            ref float attackState = ref npc.ai[1];
            float oldAttackState = npc.ai[1];
            WeightedRandom<float> newStatePicker = new WeightedRandom<float>(Main.rand);
            newStatePicker.Add((int)SignusAttackType.Test);

            do
                attackState = newStatePicker.Get();
            while (attackState == oldAttackState);

            npc.ai[1] = (int)attackState;
            npc.ai[2] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI
    }
}
