using System;
using CalamityMod;
using InfernumMode.Content.Items.Relics;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.Ogre
{
    public class OgreBehaviorOverride2 : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.DD2OgreT3;

        public override bool PreAI(NPC npc) => OgreBehaviorOverride.DoAI(npc);

        #region Death Effects

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs, ModContent.ItemType<OgreRelic>());
        }

        #endregion Death Effects

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Width = 80;
            npc.frame.Y = (int)Math.Round(npc.localAI[0]);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            if (npc.IsABestiaryIconDummy)
                return base.PreDraw(npc, spriteBatch, screenPos, lightColor);
            return OgreBehaviorOverride.DoDrawing(npc, Main.spriteBatch, lightColor);
        }
    }
}
