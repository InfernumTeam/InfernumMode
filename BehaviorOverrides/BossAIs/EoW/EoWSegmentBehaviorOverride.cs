using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

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
            npc.Calamity().newAI[1] = head.Calamity().newAI[1];

            // Fuck!
            for (int k = 0; k < npc.buffImmune.Length; k++)
                npc.buffImmune[k] = true;

            // Inherit various attributes from the head segment.
            // This code will go upstream across every segment, until it reaches the head.
            npc.realLife = head.realLife >= 0 ? head.realLife : head.whoAmI;
            npc.scale = aheadSegment.scale;
            npc.life = head.life;
            npc.lifeMax = head.lifeMax;
            npc.damage = npc.defDamage + 8;
            npc.dontTakeDamage = head.dontTakeDamage;
            npc.defense = 7;
            npc.Opacity = head.Opacity;
            aheadSegment.ai[0] = npc.whoAmI;

            if (head.damage == 0)
                npc.damage = 0;

            // What the actual fuck why is this needed.
            if (npc.life <= 0)
                npc.active = false;

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.05f);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;
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
            npc.dontTakeDamage = true;
            return false;
        }
    }
}
