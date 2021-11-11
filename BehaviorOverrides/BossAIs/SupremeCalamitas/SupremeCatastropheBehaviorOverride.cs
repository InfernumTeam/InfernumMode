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
    public class SupremeCatastropheBehaviorOverride : NPCBehaviorOverride
    {
        public enum SupremeCatastropheAttackState
		{

		}

        public override int NPCOverrideType => ModContent.NPCType<SupremeCatastrophe>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
		{
            return false;
		}
    }
}
