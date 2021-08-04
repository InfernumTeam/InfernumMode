using CalamityMod.Events;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Projectiles.Enemy;
using InfernumMode.Miscellaneous;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.World.Generation;

namespace InfernumMode.FuckYouModeAIs.AstrumDeus
{
	public class AstrumDeusHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum DeusAttackType
        {

        }

        public override int NPCOverrideType => ModContent.NPCType<AstrumDeusHeadSpectral>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            return base.PreAI(npc);
        }
    }
}
