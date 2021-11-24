using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    
    public class GolemHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.GolemHead;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        private static Dictionary<GolemAttackState, Color> AttackEyeColorPairs = new Dictionary<GolemAttackState, Color>
        {
            [GolemAttackState.ArmBullets] = Color.AntiqueWhite,
            [GolemAttackState.FistSpin] = Color.Red,
            [GolemAttackState.HeatRay] = Color.Orange,
            [GolemAttackState.SpikeTrapWaves] = Color.LightBlue,
            [GolemAttackState.SpinLaser] = Color.Firebrick,
        };

        public override bool PreAI(NPC npc)
        {
            npc.chaseable = !npc.dontTakeDamage;
            npc.Opacity = npc.dontTakeDamage ? 0f : 1f;
            return false;
        }

        /*public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            DoEyeDrawing(npc);
            return false;
        }

        public static void DoEyeDrawing(NPC npc)
        {
            Color drawColor = AttackEyeColorPairs[(GolemAttackState)Main.npc[(int)npc.ai[0]].ai[1]];
        }*/
    }
}
