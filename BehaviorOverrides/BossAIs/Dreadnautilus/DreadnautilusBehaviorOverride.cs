using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Items.Placeables.Banners;
using InfernumMode.BehaviorOverrides.BossAIs.DesertScourge;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;

namespace InfernumMode.BehaviorOverrides.BossAIs.Dreadnautilus
{
    public class DreadnautilusBehaviorOverride : NPCBehaviorOverride
    {
        public enum DreadnautilusAttackState
        {
            InitialSummonDelay
        }

        public override int NPCOverrideType => NPCID.BloodNautilus;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCSetDefaults | NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override void SetDefaults(NPC npc)
        {
            NPCID.Sets.TrailCacheLength[npc.type] = 8;
            NPCID.Sets.TrailingMode[npc.type] = 1;

            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.npcSlots = 15f;
            npc.damage = 130;
            npc.width = 100;
            npc.height = 100;
            npc.defense = 20;
            npc.DR_NERD(0.25f);
            npc.LifeMaxNERB(33000, 33000);
            npc.lifeMax /= 2;
            npc.aiStyle = -1;
            npc.knockBackResist = 0f;
            npc.value = Item.buyPrice(0, 10, 0, 0);
            npc.rarity = 1;
            for (int k = 0; k < npc.buffImmune.Length; k++)
                npc.buffImmune[k] = true;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.timeLeft = NPC.activeTime * 30;
            npc.Calamity().canBreakPlayerDefense = true;
        }

        public override bool PreAI(NPC npc)
        {
            return false;
        }

        public static void SelectNextAttack(NPC npc)
        {

        }
    }
}
