using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EoW
{
    public class EoWBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.EaterofWorldsBody;

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
            }

            // Fuck.
            npc.Calamity().newAI[1] = 720f;

            // Inherit various attributes from the head segment.
            // This code will go upstream across every segment, until it reaches the head.
            npc.scale = aheadSegment.scale;
            npc.life = head.life;
            npc.lifeMax = head.lifeMax;

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.05f);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;

            // Make segments immune to crush depth.
            npc.buffImmune[ModContent.BuffType<CrushDepth>()] = true;
        }

        public override bool PreAI(NPC npc)
        {
            SegmentAI(npc);
            return false;
        }
    }
    public class EoWTailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.EaterofWorldsTail;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            EoWBodyBehaviorOverride.SegmentAI(npc);
            return false;
        }
    }
}
