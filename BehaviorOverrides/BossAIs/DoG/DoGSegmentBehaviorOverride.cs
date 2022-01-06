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

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class DoGPhase1BodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DevourerofGodsBody>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public static void DoGSegmentAI(NPC npc)
        {
            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            NPC head = Main.npc[(int)npc.ai[2]];
            npc.life = head.life;
            if (!head.active)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                npc.netUpdate = true;
            }

            // Inherit various attributes from the head segment.
            // This code will go upstream across every segment, until it reaches the head.
            npc.scale = aheadSegment.scale;

            // Reset sizes.
            if (npc.Infernum().ExtraAI[33] == 0f && head.Infernum().ExtraAI[33] == 1f)
            {
                if (npc.type == ModContent.NPCType<DevourerofGodsBody>())
                {
                    npc.width = 120;
                    npc.height = 120;
                    npc.frame = new Rectangle(0, 0, 142, 126);
                    typeof(DevourerofGodsBody).GetField("phase2Started", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(npc.modNPC, true);
                }
                else
                {
                    npc.width = 100;
                    npc.height = 100;
                    npc.frame = new Rectangle(0, 0, 106, 200);
                    typeof(DevourerofGodsTail).GetField("phase2Started", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(npc.modNPC, true);
                }
            }
            npc.Infernum().ExtraAI[33] = head.Infernum().ExtraAI[33];

            bool headOutOfWorld = head.Center.X < -10001f || head.Center.X > Main.maxTilesX * 16f + 10001f ||
                head.Center.Y < -10001f || head.Center.Y > Main.maxTilesY * 16f + 10001f;

            if (head.Infernum().ExtraAI[33] == 1f && head.Infernum().ExtraAI[14] == 1f/* && head.Infernum().ExtraAI[2] >= 6f*/)
            {
                if (head.Infernum().ExtraAI[30] >= 0f && npc.Hitbox.Intersects(Main.projectile[(int)head.Infernum().ExtraAI[30]].Hitbox))
                {
                    npc.alpha += 70;
                    if (npc.alpha > 255)
                        npc.alpha = 255;
                }
                else
                {
                    if (head.Infernum().ExtraAI[15] % 120f > 105f)
                        npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.1f, 0f, 1f);
                    else
                    {
                        if (aheadSegment.Opacity < 0.2f)
                            npc.Opacity = 0f;
                        if (aheadSegment.Opacity > npc.Opacity)
                            npc.Opacity = MathHelper.Lerp(npc.Opacity, aheadSegment.Opacity, 0.4f);
                    }
                }
            }
            else if (head.Infernum().ExtraAI[33] == 1f && head.Infernum().ExtraAI[30] >= 0f)
            {
                if (npc.Hitbox.Intersects(Main.projectile[(int)head.Infernum().ExtraAI[30]].Hitbox))
                {
                    npc.alpha += 70;
                    if (npc.alpha > 255)
                        npc.alpha = 255;
                }
            }

            else if (head.Infernum().ExtraAI[33] == 1f && head.Infernum().ExtraAI[25] > 381f)
            {
                if (npc.Hitbox.Intersects(Main.projectile[(int)head.Infernum().ExtraAI[26]].Hitbox))
                {
                    npc.alpha += 70;
                    if (npc.alpha > 255)
                        npc.alpha = 255;
                }
            }
            else if (head.Infernum().ExtraAI[33] == 0f && head.Infernum().ExtraAI[11] >= 0f)
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
                            Main.npc[CalamityGlobalNPC.DoGHead].Infernum().ExtraAI[10] = 0f;
                            Main.npc[CalamityGlobalNPC.DoGHead].Infernum().ExtraAI[33] = 1f;
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
                npc.Opacity = aheadSegment.Opacity;

            if (npc.type == ModContent.NPCType<DevourerofGodsBody>())
                typeof(DevourerofGodsBody).GetField("invinceTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(npc.modNPC, 0);

            Vector2 size = npc.Size;
            if (npc.type == ModContent.NPCType<DevourerofGodsBody>() && head.Infernum().ExtraAI[33] == 0f)
                size = new Vector2(102f);
            if (npc.type == ModContent.NPCType<DevourerofGodsTail>() && head.Infernum().ExtraAI[33] == 0f)
                size = new Vector2(82f, 90f);

            if (npc.Size != size)
                npc.Size = size;

            npc.dontTakeDamage = head.dontTakeDamage;
            npc.damage = npc.dontTakeDamage ? 0 : npc.defDamage;
            if (head.Infernum().ExtraAI[32] > 0f)
                npc.life = npc.lifeMax;

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
            {
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.08f);
                directionToNextSegment = directionToNextSegment.MoveTowards((aheadSegment.rotation - npc.rotation).ToRotationVector2(), 1f);
            }

            float segmentOffset = 100f;
            if (head.Infernum().ExtraAI[33] == 1f)
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
            if (npc.Infernum().ExtraAI[33] == 1f)
            {
                npc.scale = 1f;
                Texture2D bodyTexture2 = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2Body");
                Texture2D glowmaskTexture2 = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2BodyGlow");
                Vector2 drawPosition2 = npc.Center - Main.screenPosition;
                Vector2 origin2 = bodyTexture2.Size() * 0.5f;
                spriteBatch.Draw(bodyTexture2, drawPosition2, null, npc.GetAlpha(lightColor), npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(glowmaskTexture2, drawPosition2, null, npc.GetAlpha(Color.White), npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                return false;
            }

            Texture2D bodyTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1Body");
            Texture2D glowmaskTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1BodyGlowmask");
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
            if (npc.Infernum().ExtraAI[33] == 1f)
            {
                npc.scale = 1f;
                Texture2D bodyTexture2 = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2Tail");
                Texture2D glowmaskTexture2 = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2TailGlow");
                Vector2 drawPosition2 = npc.Center - Main.screenPosition;
                Vector2 origin2 = bodyTexture2.Size() * 0.5f;
                spriteBatch.Draw(bodyTexture2, drawPosition2, null, npc.GetAlpha(lightColor), npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(glowmaskTexture2, drawPosition2, null, npc.GetAlpha(Color.White), npc.rotation, origin2, npc.scale, SpriteEffects.None, 0f);
                return false;
            }

            Texture2D tailTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1Tail");
            Texture2D glowmaskTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1TailGlowmask");
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = tailTexture.Size() * 0.5f;
            spriteBatch.Draw(tailTexture, drawPosition, null, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(glowmaskTexture, drawPosition, null, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
