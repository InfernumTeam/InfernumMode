using CalamityMod.NPCs.AdultEidolonWyrm;
using CalamityMod.Particles;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm.AEWHeadBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
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
            npc.Opacity = head.Opacity;
            npc.hide = npc.Opacity <= 0f;

            // Decide segment offset stuff.
            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
            {
                directionToNextSegment = directionToNextSegment.RotatedBy(WrapAngle(aheadSegment.rotation - npc.rotation) * 0.08f);
                directionToNextSegment = directionToNextSegment.MoveTowards((aheadSegment.rotation - npc.rotation).ToRotationVector2(), 1f);
            }

            // Decide segment offset stuff.
            float segmentOffset = 66f;
            npc.rotation = directionToNextSegment.ToRotation() + PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.scale * segmentOffset;
            npc.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();

            // Emit light effects before disappearing.
            float fadeCompletionInterpolant = head.Infernum().ExtraAI[DeathAnimationFadeCompletionInterpolantIndex];
            int segmentToFadeAway = (int)Math.Round(SegmentCount * (1f - fadeCompletionInterpolant));
            if (fadeCompletionInterpolant > 0f && fadeCompletionInterpolant < 1f && npc.ai[3] == segmentToFadeAway)
            {
                for (int i = 0; i < 3; i++)
                {
                    Color lightColor = Color.Lerp(Color.HotPink, Color.Red, Main.rand.NextFloat(0.1f, 0.7f));
                    Vector2 lightSpawnPosition = npc.Center + Main.rand.NextVector2Circular(60f, 60f);
                    Vector2 lightVelocity = -directionToNextSegment.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1f, 3f);
                    SquishyLightParticle light = new(lightSpawnPosition, lightVelocity, Main.rand.NextFloat(0.4f, 0.5f), lightColor, 64, 1.6f);
                    GeneralParticleHandler.SpawnParticle(light);
                }
            }
        }

        public override bool PreAI(NPC npc)
        {
            SegmentAI(npc);
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            DrawSegment(npc, lightColor);
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

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            DrawSegment(npc, lightColor);
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

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            DrawSegment(npc, lightColor);
            return false;
        }
    }
}
