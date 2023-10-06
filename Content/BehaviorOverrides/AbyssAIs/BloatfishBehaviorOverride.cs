using CalamityMod.NPCs.Abyss;
using InfernumMode.Common.Worldgen;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

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

            // Avoid being too close to ground.
            float minHeight = Lerp(100f, 302f, npc.whoAmI / 8f % 1f);
            if (WorldUtils.Find(npc.Center.ToTileCoordinates(), Searches.Chain(new Searches.Down((int)(minHeight / 16f)), new Conditions.IsSolid(), new CustomTileConditions.ActiveAndNotActuated()), out _))
                npc.position.Y -= 0.8f;

            // Swim away if a major thing is around.
            if (AbyssMinibossSpawnSystem.MajorAbyssEnemyExists)
                npc.velocity.Y -= 0.9f;

            // Emit light.
            Lighting.AddLight(npc.Center, Color.Blue.ToVector3());

            NPCID.Sets.CountsAsCritter[npc.type] = true;
            NPCID.Sets.TakesDamageFromHostilesWithoutBeingFriendly[npc.type] = false;
            return true;
        }
    }
}
