using CalamityMod.NPCs.Abyss;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class BloatfishBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<Bloatfish>();

        public override bool PreAI(NPC npc)
        {
            // These things are useless anyway. Disable their damage.
            npc.Infernum().IsAbyssPrey = true;
            npc.damage = 0;

            // Emit light.
            Lighting.AddLight(npc.Center, Color.Blue.ToVector3());

            NPCID.Sets.CountsAsCritter[npc.type] = true;
            NPCID.Sets.TakesDamageFromHostilesWithoutBeingFriendly[npc.type] = false;
            return true;
        }
    }
}
