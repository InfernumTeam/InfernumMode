using CalamityMod.NPCs.Abyss;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class OarfishBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<OarfishBody>();

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
            npc.damage = head.damage <= 0 ? 0 : npc.defDamage;

            // Perform segment connection code.
            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.03f);

            directionToNextSegment = directionToNextSegment.SafeNormalize(Vector2.Zero);

            // Move hit registrations up to the head.
            if (npc.justHit)
                head.justHit = true;

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment * npc.width * npc.scale;
            return false;
        }

        public override bool PreAI(NPC npc) => DoSegmentingAI(npc);

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor) => true;
    }

    public class OarfishTailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<OarfishTail>();

        public override bool PreAI(NPC npc) => OarfishBodyBehaviorOverride.DoSegmentingAI(npc);

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor) => true;
    }
}