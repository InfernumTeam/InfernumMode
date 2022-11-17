using CalamityMod.NPCs.Abyss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.AbyssAIs
{
    public class GulperEelBody1BehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<GulperEelBodyAlt>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public static bool DoSegmentingAI(NPC npc)
        {
            if (!Main.npc.IndexInRange(npc.realLife))
            {
                npc.active = false;
                return false;
            }

            // Disable water distortion effects messing up the eel drawcode.
            npc.wet = false;

            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            NPC head = Main.npc[npc.realLife];
            if (!aheadSegment.active || npc.realLife <= -1 || !head.active)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    return false;

                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                npc.netUpdate = true;
            }

            // Inherit various attributes from the ahead segment.
            // This code will go upstream across every segment, until it reaches the head.
            npc.Opacity = aheadSegment.Opacity;
            npc.chaseable = true;
            npc.friendly = false;
            npc.dontTakeDamage = aheadSegment.dontTakeDamage;
            npc.damage = npc.dontTakeDamage ? 0 : npc.defDamage;

            // Perform segment connection code.
            float segmentIndex = npc.Infernum().ExtraAI[0];
            float slitherTimer = head.Infernum().ExtraAI[0];
            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.03f);
            if (segmentIndex <= 4f)
                directionToNextSegment = (aheadSegment.rotation - MathHelper.PiOver2).ToRotationVector2();

            directionToNextSegment = directionToNextSegment.SafeNormalize(Vector2.Zero);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment * npc.width * npc.scale * 0.725f;
            npc.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();

            // Slither around.
            float maxSlitherOffset = Utils.Remap(head.velocity.Length(), 1f, 13f, 20f, 1f);
            float slitherOffset = (float)Math.Sin(-slitherTimer + segmentIndex * 0.4113f) * Utils.Remap(segmentIndex, 2f, 7f, 0f, maxSlitherOffset);
            npc.Center += npc.SafeDirectionTo(head.Center).RotatedBy(MathHelper.PiOver2) * Utils.GetLerpValue(npc.Distance(head.Center), 145f, 190f, true) * slitherOffset;
            return false;
        }

        public override bool PreAI(NPC npc) => DoSegmentingAI(npc);

        // Drawing is handled by the head.
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            npc.Opacity = 0f;
            return false;
        }
    }

    public class GulperEelBody2BehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<GulperEelBody>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;
        
        public override bool PreAI(NPC npc) => GulperEelBody1BehaviorOverride.DoSegmentingAI(npc);

        // Drawing is handled by the head.
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            npc.Opacity = 0f;
            return false;
        }
    }

    public class GulperEelTailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<GulperEelTail>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc) => GulperEelBody1BehaviorOverride.DoSegmentingAI(npc);

        // Drawing is handled by the head.
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            npc.Opacity = 0f;
            return false;
        }
    }
}