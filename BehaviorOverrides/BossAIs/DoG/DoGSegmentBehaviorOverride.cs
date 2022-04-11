using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
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

            // Reset sizes.
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
            npc.Infernum().ExtraAI[33] = head.Infernum().ExtraAI[33];

            bool headOutOfWorld = head.Center.X < -10001f || head.Center.X > Main.maxTilesX * 16f + 10001f ||
                head.Center.Y < -10001f || head.Center.Y > Main.maxTilesY * 16f + 10001f;

            // Enter phase two once the tail enters the transition portal.
            if (!InPhase2 && head.Infernum().ExtraAI[11] >= 0f)
            {
                if (npc.Hitbox.Intersects(Main.projectile[(int)head.Infernum().ExtraAI[11]].Hitbox) || headOutOfWorld)
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

                        CalamityWorld.DoGSecondStageCountdown = 305;

                        if (Main.netMode == NetmodeID.Server)
                        {
                            var netMessage = InfernumMode.CalamityMod.GetPacket();
                            netMessage.Write((byte)CalamityModMessageType.DoGCountdownSync);
                            netMessage.Write(CalamityWorld.DoGSecondStageCountdown);
                            netMessage.Send();
                        }
                    }
                }
            }
            else
            {
                // Do what the head says if not doing phase two transition stuff.
                switch ((BodySegmentFadeType)(int)head.Infernum().ExtraAI[DoGPhase1HeadBehaviorOverride.BodySegmentFadeTypeAIIndex])
                {
                    case BodySegmentFadeType.EnterPortal:
                        int portalIndex = (int)head.Infernum().ExtraAI[DoGPhase1HeadBehaviorOverride.PortalProjectileIndexAIIndex];
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
                Texture2D bodyTexture2 = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2Body").Value;
                Texture2D glowmaskTexture2 = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2BodyGlow").Value;
                Vector2 drawPosition2 = npc.Center - Main.screenPosition;
                Vector2 origin2 = bodyTexture2.Size() * 0.5f;

                // Draw head backimages as a telegraph.
                float aggressiveFade2 = GetAggressiveFade;
                float passiveFade2 = GetPassiveFade;
                if (aggressiveFade2 > 0f || passiveFade2 > 0f)
                {
                    Color afterimageColor = Color.Transparent;
                    if (aggressiveFade2 > 0f)
                        afterimageColor = Color.Lerp(afterimageColor, AggressiveFadeColor, (float)Math.Sqrt(aggressiveFade2));
                    if (passiveFade2 > 0f)
                        afterimageColor = Color.Lerp(afterimageColor, PassiveFadeColor, (float)Math.Sqrt(passiveFade2));
                    afterimageColor.A = 50;
                    float afterimageOffsetFactor = MathHelper.Max(aggressiveFade2, passiveFade2) * 24f;

                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 afterimageOffset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * afterimageOffsetFactor;
                        spriteBatch.Draw(bodyTexture2, drawPosition2 + afterimageOffset, npc.frame, npc.GetAlpha(afterimageColor) * 0.45f, npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                    }
                }

                spriteBatch.Draw(bodyTexture2, drawPosition2, null, npc.GetAlpha(lightColor), npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(glowmaskTexture2, drawPosition2, null, npc.GetAlpha(Color.White), npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                return false;
            }

            Texture2D bodyTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1Body").Value;
            Texture2D glowmaskTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1BodyGlowmask").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = bodyTexture.Size() * 0.5f;

            // Draw head backimages as a telegraph.
            float aggressiveFade = GetAggressiveFade;
            float passiveFade = GetPassiveFade;
            if (aggressiveFade > 0f || passiveFade > 0f)
            {
                Color afterimageColor = Color.Transparent;
                if (aggressiveFade > 0f)
                    afterimageColor = Color.Lerp(afterimageColor, AggressiveFadeColor, (float)Math.Sqrt(aggressiveFade));
                if (passiveFade > 0f)
                    afterimageColor = Color.Lerp(afterimageColor, PassiveFadeColor, (float)Math.Sqrt(passiveFade));
                afterimageColor.A = 50;
                float afterimageOffsetFactor = MathHelper.Max(aggressiveFade, passiveFade) * 24f;

                for (int i = 0; i < 12; i++)
                {
                    Vector2 afterimageOffset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * afterimageOffsetFactor;
                    spriteBatch.Draw(bodyTexture, drawPosition + afterimageOffset, null, npc.GetAlpha(afterimageColor) * 0.45f, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
                }
            }
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
                Texture2D tailTexture2 = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2Tail").Value;
                Texture2D glowmaskTexture2 = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2TailGlow").Value;
                Vector2 drawPosition2 = npc.Center - Main.screenPosition;
                Vector2 origin2 = tailTexture2.Size() * 0.5f;

                // Draw head backimages as a telegraph.
                float aggressiveFade2 = GetAggressiveFade;
                float passiveFade2 = GetPassiveFade;
                if (aggressiveFade2 > 0f || passiveFade2 > 0f)
                {
                    Color afterimageColor = Color.Transparent;
                    if (aggressiveFade2 > 0f)
                        afterimageColor = Color.Lerp(afterimageColor, AggressiveFadeColor, (float)Math.Sqrt(aggressiveFade2));
                    if (passiveFade2 > 0f)
                        afterimageColor = Color.Lerp(afterimageColor, PassiveFadeColor, (float)Math.Sqrt(passiveFade2));
                    afterimageColor.A = 50;
                    float afterimageOffsetFactor = MathHelper.Max(aggressiveFade2, passiveFade2) * 24f;

                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 afterimageOffset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * afterimageOffsetFactor;
                        spriteBatch.Draw(tailTexture2, drawPosition2 + afterimageOffset, npc.frame, npc.GetAlpha(afterimageColor) * 0.45f, npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                    }
                }

                spriteBatch.Draw(tailTexture2, drawPosition2, null, npc.GetAlpha(lightColor), npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(glowmaskTexture2, drawPosition2, null, npc.GetAlpha(Color.White), npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                return false;
            }

            Texture2D tailTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1Tail").Value;
            Texture2D glowmaskTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1TailGlowmask").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = tailTexture.Size() * 0.5f;

            // Draw head backimages as a telegraph.
            float aggressiveFade = GetAggressiveFade;
            float passiveFade = GetPassiveFade;
            if (aggressiveFade > 0f || passiveFade > 0f)
            {
                Color afterimageColor = Color.Transparent;
                if (aggressiveFade > 0f)
                    afterimageColor = Color.Lerp(afterimageColor, AggressiveFadeColor, (float)Math.Sqrt(aggressiveFade));
                if (passiveFade > 0f)
                    afterimageColor = Color.Lerp(afterimageColor, PassiveFadeColor, (float)Math.Sqrt(passiveFade));
                afterimageColor.A = 50;
                float afterimageOffsetFactor = MathHelper.Max(aggressiveFade, passiveFade) * 24f;

                for (int i = 0; i < 12; i++)
                {
                    Vector2 afterimageOffset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * afterimageOffsetFactor;
                    spriteBatch.Draw(tailTexture, drawPosition + afterimageOffset, null, npc.GetAlpha(afterimageColor) * 0.45f, npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
                }
            }
            spriteBatch.Draw(tailTexture, drawPosition, null, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(glowmaskTexture, drawPosition, null, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
