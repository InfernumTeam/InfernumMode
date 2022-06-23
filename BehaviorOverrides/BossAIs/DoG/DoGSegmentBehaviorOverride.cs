using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using static InfernumMode.BehaviorOverrides.BossAIs.DoG.DoGPhase2HeadBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class DoGPhase1BodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DevourerofGodsBody>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        private static readonly FieldInfo invincibilityTimeField = typeof(DevourerofGodsBody).GetField("invinceTime", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo bodyPhase2StartedField = typeof(DevourerofGodsBody).GetField("phase2Started", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo tailPhase2StartedField = typeof(DevourerofGodsTail).GetField("phase2Started", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo bodyPhase2StartedField2 = typeof(DevourerofGodsBody).GetField("Phase2Started", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo tailPhase2StartedField2 = typeof(DevourerofGodsTail).GetField("Phase2Started", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void DoGSegmentAI(NPC npc)
        {
            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            NPC head = Main.npc[(int)npc.ai[2]];
            npc.life = head.life;
            npc.lifeMax = head.lifeMax;
            npc.defense = 0;
            if (!head.active || CalamityGlobalNPC.DoGHead < 0)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                npc.netUpdate = true;
                return;
            }

            // Inherit various attributes from the head segment.
            // This code will go upstream across every segment, until it reaches the head.
            npc.scale = aheadSegment.scale;

            // Reset sizes if the head has transitioned to phase 2 but this segment has yet to inherit that property.
            if (npc.Infernum().ExtraAI[33] == 0f && InPhase2)
            {
                if (npc.type == ModContent.NPCType<DevourerofGodsBody>())
                {
                    npc.width = 120;
                    npc.height = 120;
                    npc.frame = new Rectangle(0, 0, 142, 126);
                    bodyPhase2StartedField?.SetValue(npc.ModNPC, true);
                    bodyPhase2StartedField2?.SetValue(npc.ModNPC, true);
                }
                else
                {
                    npc.width = 100;
                    npc.height = 100;
                    npc.frame = new Rectangle(0, 0, 106, 200);
                    tailPhase2StartedField?.SetValue(npc.ModNPC, true);
                    tailPhase2StartedField2?.SetValue(npc.ModNPC, true);
                }
            }

            // Inherit the phase 2 state from the head.
            npc.Infernum().ExtraAI[33] = head.Infernum().ExtraAI[33];

            bool headOutOfWorld = head.Center.X < -10001f || head.Center.X > Main.maxTilesX * 16f + 10001f ||
                head.Center.Y < -10001f || head.Center.Y > Main.maxTilesY * 16f + 10001f;

            // Enter phase two once the tail enters the transition portal.
            if (!InPhase2 && head.Infernum().ExtraAI[DoGPhase1HeadBehaviorOverride.Phase2PortalProjectileIndexAIIndex] >= 0f)
            {
                if (npc.Hitbox.Intersects(Main.projectile[(int)head.Infernum().ExtraAI[DoGPhase1HeadBehaviorOverride.Phase2PortalProjectileIndexAIIndex]].Hitbox) || headOutOfWorld)
                {
                    npc.alpha += 140;
                    if (npc.alpha > 255)
                    {
                        npc.alpha = 255;

                        int tailType = ModContent.NPCType<DevourerofGodsTail>();
                        if (npc.type == tailType)
                        {
                            InPhase2 = true;
                            Main.npc[CalamityGlobalNPC.DoGHead].Infernum().ExtraAI[10] = 0f;
                            Main.npc[CalamityGlobalNPC.DoGHead].netUpdate = true;
                        }
                    }
                }
            }
            else
            {
                // Do what the head says in regards to opacity inheritance if not doing phase two transition stuff.
                switch ((BodySegmentFadeType)(int)head.Infernum().ExtraAI[DoGPhase1HeadBehaviorOverride.BodySegmentFadeTypeAIIndex])
                {
                    case BodySegmentFadeType.EnterPortal:
                        int portalIndex = (int)head.Infernum().ExtraAI[DoGPhase1HeadBehaviorOverride.Phase2PortalProjectileIndexAIIndex];
                        if (portalIndex >= 0f && npc.Hitbox.Intersects(Main.projectile[portalIndex].Hitbox))
                            npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.275f, 0f, 1f);

                        break;

                    case BodySegmentFadeType.InhertHeadOpacity:
                        npc.Opacity = head.Opacity;
                        break;

                    case BodySegmentFadeType.ApproachAheadSegmentOpacity:
                        if (aheadSegment.Opacity < 0.2f)
                            npc.Opacity = 0f;
                        if (aheadSegment.Opacity > npc.Opacity)
                        {
                            npc.Opacity = MathHelper.Lerp(npc.Opacity, aheadSegment.Opacity, 0.67f);
                            if (aheadSegment.Opacity >= 1f)
                                npc.Opacity = MathHelper.Lerp(npc.Opacity, aheadSegment.Opacity, 0.67f);
                        }
                        break;
                }
            }

            // Reset the invicibility time variable used in the vanilla AI.
            if (npc.type == ModContent.NPCType<DevourerofGodsBody>())
                invincibilityTimeField.SetValue(npc.ModNPC, 0);

            // Decide segment size stuff.
            Vector2 size = npc.Size;
            if (npc.type == ModContent.NPCType<DevourerofGodsBody>() && InPhase2)
                size = new Vector2(102f);
            if (npc.type == ModContent.NPCType<DevourerofGodsTail>() && InPhase2)
                size = new Vector2(82f, 90f);

            if (npc.Size != size)
                npc.Size = size;

            npc.dontTakeDamage = head.dontTakeDamage || npc.Opacity < 0.1f;
            npc.damage = npc.dontTakeDamage ? 0 : npc.defDamage;
            npc.life = npc.lifeMax;

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
            {
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.08f);
                directionToNextSegment = directionToNextSegment.MoveTowards((aheadSegment.rotation - npc.rotation).ToRotationVector2(), 1f);
            }

            // Decide segment offset stuff.
            float segmentOffset = 100f;
            if (InPhase2)
            {
                if (npc.type == ModContent.NPCType<DevourerofGodsBody>())
                    segmentOffset = 80f;
                if (npc.type == ModContent.NPCType<DevourerofGodsTail>())
                    segmentOffset = 120f;
            }
            else
            {
                if (npc.type == ModContent.NPCType<DevourerofGodsBody>())
                    segmentOffset = 100f;
                if (npc.type == ModContent.NPCType<DevourerofGodsTail>())
                    segmentOffset = 98f;
            }
            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.scale * segmentOffset;
            npc.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();
        }

        public override bool PreAI(NPC npc)
        {
            DoGSegmentAI(npc);
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            if (InPhase2)
            {
                npc.scale = 1f;

                float antimatterFade = FadeToAntimatterForm;
                Texture2D bodyTexture2 = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2Body").Value;
                Texture2D glowmaskTexture2 = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2BodyGlow").Value;
                Texture2D bodyTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2BodyAntimatter").Value;
                Texture2D glowmaskTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2BodyGlowAntimatter").Value;
                Vector2 drawPosition2 = npc.Center - Main.screenPosition;
                Vector2 origin2 = bodyTexture2.Size() * 0.5f;

                // Draw back afterimages when the antimatter effect is ongoing.
                float backTexturePulse1 = (Main.GlobalTimeWrappedHourly * 0.43f + npc.whoAmI * 0.13f) % 1f;
                float backTexturePulse2 = (Main.GlobalTimeWrappedHourly * 0.31f + npc.whoAmI * 0.09f) % 1f;
                Color c1 = Color.Cyan;
                Color c2 = Color.Fuchsia;
                c1.A = 84;
                c2.A = 92;
                spriteBatch.Draw(bodyTexture2Antimatter, drawPosition2, null, npc.GetAlpha(c1) * antimatterFade * (1f - backTexturePulse1) * 0.84f, npc.rotation, origin2, npc.scale * (1f + backTexturePulse1 * 0.4f), SpriteEffects.None, 0f);
                spriteBatch.Draw(bodyTexture2Antimatter, drawPosition2, null, npc.GetAlpha(c2) * antimatterFade * (1f - backTexturePulse2) * 0.6f, npc.rotation, origin2, npc.scale * (1f + backTexturePulse2 * 1.2f), SpriteEffects.None, 0f);

                spriteBatch.Draw(bodyTexture2, drawPosition2, null, npc.GetAlpha(lightColor) * (1f - antimatterFade), npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(glowmaskTexture2, drawPosition2, null, npc.GetAlpha(Color.White) * (1f - antimatterFade), npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(bodyTexture2Antimatter, drawPosition2, null, npc.GetAlpha(Color.White) * antimatterFade, npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(glowmaskTexture2Antimatter, drawPosition2, null, npc.GetAlpha(Color.White) * antimatterFade, npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                return false;
            }

            Texture2D bodyTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1Body").Value;
            Texture2D glowmaskTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1BodyGlowmask").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = bodyTexture.Size() * 0.5f;

            spriteBatch.Draw(bodyTexture, drawPosition, null, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(glowmaskTexture, drawPosition, null, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    public class DoGPhase1TailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DevourerofGodsTail>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            DoGPhase1BodyBehaviorOverride.DoGSegmentAI(npc);
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            if (InPhase2)
            {
                npc.scale = 1f;

                float antimatterFade = FadeToAntimatterForm;
                Texture2D tailTexture2 = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2Tail").Value;
                Texture2D glowmaskTexture2 = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2TailGlow").Value;
                Texture2D tailTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2TailAntimatter").Value;
                Texture2D glowmaskTexture2Antimatter = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2TailGlowAntimatter").Value;
                Vector2 drawPosition2 = npc.Center - Main.screenPosition;
                Vector2 origin2 = tailTexture2.Size() * 0.5f;

                // Draw back afterimages when the antimatter effect is ongoing.
                float backTexturePulse1 = (Main.GlobalTimeWrappedHourly * 0.43f + npc.whoAmI * 0.13f) % 1f;
                float backTexturePulse2 = (Main.GlobalTimeWrappedHourly * 0.31f + npc.whoAmI * 0.09f) % 1f;
                Color c1 = Color.Cyan;
                Color c2 = Color.Fuchsia;
                c1.A = 84;
                c2.A = 92;
                spriteBatch.Draw(tailTexture2Antimatter, drawPosition2, null, npc.GetAlpha(c1) * antimatterFade * (1f - backTexturePulse1) * 0.84f, npc.rotation, origin2, npc.scale * (1f + backTexturePulse1 * 0.4f), SpriteEffects.None, 0f);
                spriteBatch.Draw(tailTexture2Antimatter, drawPosition2, null, npc.GetAlpha(c2) * antimatterFade * (1f - backTexturePulse2) * 0.6f, npc.rotation, origin2, npc.scale * (1f + backTexturePulse2 * 1.2f), SpriteEffects.None, 0f);

                spriteBatch.Draw(tailTexture2, drawPosition2, null, npc.GetAlpha(lightColor) * (1f - antimatterFade), npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(glowmaskTexture2, drawPosition2, null, npc.GetAlpha(Color.White) * (1f - antimatterFade), npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(tailTexture2Antimatter, drawPosition2, null, npc.GetAlpha(Color.White) * antimatterFade, npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(glowmaskTexture2Antimatter, drawPosition2, null, npc.GetAlpha(Color.White) * antimatterFade, npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                return false;
            }

            Texture2D tailTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1Tail").Value;
            Texture2D glowmaskTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1TailGlowmask").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = tailTexture.Size() * 0.5f;

            spriteBatch.Draw(tailTexture, drawPosition, null, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(glowmaskTexture, drawPosition, null, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
