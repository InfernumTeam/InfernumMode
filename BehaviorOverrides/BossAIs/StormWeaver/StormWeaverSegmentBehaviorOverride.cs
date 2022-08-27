using CalamityMod;
using CalamityMod.NPCs.StormWeaver;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.StormWeaver
{
    public class StormWeaverBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<StormWeaverBody>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public static void SegmentAI(NPC npc)
        {
            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            NPC head = Main.npc[(int)npc.ai[2]];
            if (!aheadSegment.active)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                npc.netUpdate = true;
                return;
            }

            if (head.Infernum().ExtraAI[20] == 1f)
            {
                if (npc.type == ModContent.NPCType<StormWeaverBody>())
                    npc.frame = new Rectangle(0, 0, 54, 52);
                npc.HitSound = head.HitSound;
            }

            // Inherit various attributes from the head segment.
            // This code will go upstream across every segment, until it reaches the head.
            npc.scale = aheadSegment.scale;
            npc.life = head.life;
            npc.lifeMax = head.lifeMax;
            npc.Opacity = head.Opacity;
            npc.Calamity().DR = head.Calamity().DR;
            npc.Calamity().unbreakableDR = head.Calamity().unbreakableDR;
            npc.damage = head.damage > 0 ? npc.defDamage : 0;

            if (npc.type == ModContent.NPCType<StormWeaverTail>())
            {
                npc.Calamity().DR = 0f;
                npc.Calamity().unbreakableDR = false;
            }

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.05f);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * 44f;
        }

        public override bool PreAI(NPC npc)
        {
            SegmentAI(npc);
            return false;
        }
    }

    public class StormWeaverTailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<StormWeaverTail>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            StormWeaverBodyBehaviorOverride.SegmentAI(npc);
            return false;
        }
    }
}
