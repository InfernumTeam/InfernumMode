using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class GolemHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.GolemHead;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public static Dictionary<GolemAttackState, Color> AttackEyeColorPairs => new Dictionary<GolemAttackState, Color>
        {
            [GolemAttackState.SummonDelay] = Color.Transparent,
            [GolemAttackState.BIGSHOT] = Color.Transparent,
            [GolemAttackState.BadTime] = Color.Transparent,

            [GolemAttackState.FloorFire] = Color.AntiqueWhite,
            [GolemAttackState.FistSpin] = Color.Orange,
            [GolemAttackState.HeatRay] = Color.Aquamarine,
            [GolemAttackState.SpikeTrapWaves] = Color.LightBlue,
            [GolemAttackState.SpinLaser] = Color.Firebrick,
            [GolemAttackState.Slingshot] = Color.MediumPurple,
            [GolemAttackState.SpikeRush] = Color.Green
        };

        public override bool PreAI(NPC npc)
        {
            if (!Main.npc[(int)npc.ai[0]].active || Main.npc[(int)npc.ai[0]].type != NPCID.Golem)
            {
                GolemBodyBehaviorOverride.DespawnNPC(npc.whoAmI);
                return false;
            }
            NPCID.Sets.MustAlwaysDraw[NPCID.GolemHead] = true;
            npc.chaseable = !npc.dontTakeDamage;
            if (Main.npc[(int)npc.ai[0]].ai[0] > 242f)
                npc.Opacity = npc.dontTakeDamage ? 0f : 1f;
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Only draw eyes on summon animation
            if (Main.npc[(int)npc.ai[0]].ai[0] < 182f)
            {
                DoEyeDrawing(npc);
                return false;
            }

            if (npc.Opacity == 0f && Main.npc[(int)npc.ai[0]].ai[0] > 242)
                return false;

            Texture2D texture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Golem/AttachedHead");
            Texture2D glowMask = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Golem/AttachedHeadGlow");
            Rectangle rect = new Rectangle(0, 0, texture.Width, texture.Height);
            spriteBatch.Draw(texture, npc.Center - Main.screenPosition, rect, lightColor * npc.Opacity, npc.rotation, rect.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            spriteBatch.Draw(glowMask, npc.Center - Main.screenPosition, rect, Color.White * npc.Opacity, npc.rotation, rect.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            DoEyeDrawing(npc);
            return false;
        }

        public static void DoEyeDrawing(NPC npc)
        {
            Texture2D texture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Golem/GolemEyeGlow");
            Rectangle rect = new Rectangle(0, 0, texture.Width, texture.Height);
            float rotation = MathHelper.Lerp(0f, MathHelper.TwoPi, npc.ai[1] / 240f);
            float rotation2 = MathHelper.Lerp(MathHelper.TwoPi, 0f, npc.ai[1] / 240f);
            Color nextColor = Color.Transparent;
            if (AttackEyeColorPairs.TryGetValue((GolemAttackState)Main.npc[(int)npc.ai[0]].ai[1], out Color fuckYou))
                nextColor = fuckYou;
            nextColor.A = 0;
            Vector2 drawPos = npc.type == NPCID.GolemHead ? new Vector2(npc.Center.X - 15f, npc.Bottom.Y - 33f) : new Vector2(npc.Center.X - 15f, npc.Bottom.Y - 57f);
            Vector2 drawPos2 = new Vector2(drawPos.X + 30f, drawPos.Y);

            NPC body = Main.npc[(int)npc.ai[0]];
            float laserRayTelegraphInterpolant = body.Infernum().ExtraAI[10];

            if ((GolemAttackState)body.ai[1] == GolemAttackState.BIGSHOT || (GolemAttackState)body.ai[1] == GolemAttackState.BadTime || (GolemAttackState)body.ai[1] == GolemAttackState.SummonDelay)
            {
                float DarknessRatio = body.Infernum().ExtraAI[9];
                Color leftColor;
                Color rightColor;

                if ((GolemAttackState)body.ai[1] == GolemAttackState.BIGSHOT)
                {
                    leftColor = Color.LightPink;
                    rightColor = Color.Yellow;
                }
                else if ((GolemAttackState)body.ai[1] == GolemAttackState.BadTime)
                {
                    leftColor = Color.Transparent;
                    rightColor = Color.Cyan;
                }
                else
                {
                    leftColor = Color.Red;
                    rightColor = Color.Red;
                }

                float colorRatio;
                if (body.ai[0] > 120f)
                    colorRatio = 1 - MathHelper.Clamp((body.ai[0] - 121f) / 30f, 0f, 1f);
                else
                    colorRatio = 0f;

                Texture2D black = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Golem/BlackPixel");
                float blackScale = Math.Max(Main.screenWidth, Main.screenHeight);
                Vector2 blackPos = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
                Main.spriteBatch.Draw(black, blackPos, new Rectangle(0, 0, 1, 1), Color.White * DarknessRatio, 0f, new Vector2(.5f, .5f), blackScale, SpriteEffects.None, 0f);

                for (float i = 4; i > 0; i--)
                {
                    float scale = i / 4f;
                    Main.spriteBatch.Draw(texture, drawPos - Main.screenPosition, rect, leftColor * colorRatio, rotation, rect.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(texture, drawPos2 - Main.screenPosition, rect, rightColor * colorRatio, rotation2, rect.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(texture, drawPos - Main.screenPosition, rect, leftColor * colorRatio, rotation + MathHelper.PiOver2, rect.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(texture, drawPos2 - Main.screenPosition, rect, rightColor * colorRatio, rotation2 + MathHelper.PiOver2, rect.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                }
                return;
            }

            float AttackCooldownRatio = GolemBodyBehaviorOverride.ConstAttackCooldown - body.Infernum().ExtraAI[7];
            Color prevColor = AttackEyeColorPairs[(GolemAttackState)body.Infernum().ExtraAI[8]];
            float ratio = MathHelper.Clamp(AttackCooldownRatio / (GolemBodyBehaviorOverride.ConstAttackCooldown * 0.67f), 0f, 1f);
            Color drawColor = body.Infernum().ExtraAI[5] == 1f || body.Infernum().ExtraAI[6] == 1f ? Color.Red * 0.25f : Color.Lerp(prevColor, nextColor, ratio) * 0.25f;

            for (float i = 4; i > 0; i--)
            {
                float scale = i / 4f;
                Main.spriteBatch.Draw(texture, drawPos - Main.screenPosition, rect, drawColor, rotation, rect.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(texture, drawPos2 - Main.screenPosition, rect, drawColor, rotation2, rect.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(texture, drawPos - Main.screenPosition, rect, drawColor, rotation + MathHelper.PiOver2, rect.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(texture, drawPos2 - Main.screenPosition, rect, drawColor, rotation2 + MathHelper.PiOver2, rect.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }

            // Draw laser ray telegraphs.
            if (laserRayTelegraphInterpolant > 0f)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                Texture2D line = ModContent.GetTexture("InfernumMode/ExtraTextures/BloomLine");
                Color outlineColor = Color.Lerp(Color.OrangeRed, Color.White, laserRayTelegraphInterpolant);
                Vector2 origin = new Vector2(line.Width / 2f, line.Height);
                Vector2 beamScale = new Vector2(laserRayTelegraphInterpolant * 0.5f, 2.4f);

                Vector2 drawPosition = drawPos - Main.screenPosition;
                Vector2 beamDirection = -Vector2.UnitX;
                float beamRotation = beamDirection.ToRotation() - MathHelper.PiOver2;
                Main.spriteBatch.Draw(line, drawPosition, null, outlineColor, beamRotation, origin, beamScale, 0, 0f);

                drawPosition = drawPos2 - Main.screenPosition;
                beamDirection = Vector2.UnitX;
                beamRotation = beamDirection.ToRotation() - MathHelper.PiOver2;
                Main.spriteBatch.Draw(line, drawPosition, null, outlineColor, beamRotation, origin, beamScale, 0, 0f);

                Main.spriteBatch.ResetBlendState();
            }
        }
    }
}
