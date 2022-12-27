using CalamityMod.NPCs.AdultEidolonWyrm;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AEWBody1BehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AdultEidolonWyrmBody>();

        public static void SegmentAI(NPC npc)
        {
            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            NPC head = Main.npc[(int)npc.ai[2]];
            npc.life = head.life;
            npc.lifeMax = head.lifeMax;

            // Disappear if the head is not present for some reason.
            if (Main.netMode != NetmodeID.MultiplayerClient && !head.active)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                npc.netUpdate = true;
                return;
            }

            // Inherit various attributes from the head segment.
            npc.scale = head.scale;
            npc.life = head.life;
            npc.lifeMax = head.lifeMax;
            npc.damage = 0;

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.05f, 0f, 1f);

            // Decide segment offset stuff.
            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
            {
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.08f);
                directionToNextSegment = directionToNextSegment.MoveTowards((aheadSegment.rotation - npc.rotation).ToRotationVector2(), 1f);
            }

            // Decide segment offset stuff.
            float segmentOffset = 66f;
            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.scale * segmentOffset;
            npc.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();
        }
        
        public override bool PreAI(NPC npc)
        {
            SegmentAI(npc);
            return false;
        }
    }

    public class AEWBody2BehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AdultEidolonWyrmBodyAlt>();

        public override bool PreAI(NPC npc)
        {
            AEWBody1BehaviorOverride.SegmentAI(npc);
            return false;
        }
    }

    public class AEWTailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AdultEidolonWyrmTail>();

        public override bool PreAI(NPC npc)
        {
            AEWBody1BehaviorOverride.SegmentAI(npc);
            return false;
        }
    }
}
