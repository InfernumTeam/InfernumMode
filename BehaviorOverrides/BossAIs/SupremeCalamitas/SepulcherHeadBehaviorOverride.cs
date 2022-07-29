using CalamityMod;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Typeless;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

using SCalNPC = CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SepulcherHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum SepulcherAttackType
        {
            ErraticCharges,
            PerpendicularBoneCharges,
            SoulBarrages
        }

        public enum SCalBrotherAnimationType
        {
            HoverInPlace,
            AttackAnimation
        }

        public override int NPCOverrideType => ModContent.NPCType<SupremeCataclysm>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            return false;
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.netUpdate = true;
        }
    }
}