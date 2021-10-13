using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.WallOfFlesh
{
	public class WallOfFleshEyeBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.WallofFleshEye;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        #region AI

        public override bool PreAI(NPC npc)
        {
            ref float time = ref npc.ai[1];

            if (!Main.npc.IndexInRange(Main.wof))
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            Player target = Main.player[Main.npc[Main.wof].target];
            float destinationOffset = MathHelper.Clamp(npc.Distance(target.Center), 60f, 210f);
            destinationOffset += MathHelper.Lerp(0f, 215f, (float)Math.Sin(npc.whoAmI % 4f / 4f * MathHelper.Pi + time / 16f) * 0.5f + 0.5f);
            destinationOffset += npc.Distance(target.Center) * 0.1f;

            float destinationAngularOffset = MathHelper.Lerp(-1.5f, 1.5f, npc.ai[0]);
            destinationAngularOffset += (float)Math.Sin(time / 32f + npc.whoAmI % 4f / 4f * MathHelper.Pi) * 0.16f;

            Vector2 destination = Main.npc[Main.wof].Center;
            destination += Main.npc[Main.wof].velocity.SafeNormalize(Vector2.UnitX).RotatedBy(destinationAngularOffset) * destinationOffset;

            float maxSpeed = Utilities.AnyProjectiles(ModContent.ProjectileType<FireBeamWoF>()) ? 4f : 15f;

            npc.velocity = (destination - npc.Center).SafeNormalize(Vector2.Zero) * MathHelper.Min(npc.Distance(destination) * 0.5f, maxSpeed);
            if (!npc.WithinRange(Main.npc[Main.wof].Center, 750f))
                npc.Center = Main.npc[Main.wof].Center + Main.npc[Main.wof].SafeDirectionTo(npc.Center) * 750f;

            npc.spriteDirection = 1;
            npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center), MathHelper.Pi * 0.1f);

            time++;

            int beamShootRate = 1600;
            if (time % beamShootRate == (beamShootRate + npc.whoAmI * 300) % beamShootRate)
                WallOfFleshMouthBehaviorOverride.PrepareFireBeam(npc, target);

            return false;
        }

        #endregion

        #region Drawing

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            ref float verticalOffsetFactor = ref npc.ai[0];
            float yStart = MathHelper.Lerp(Main.wofB, Main.wofT, verticalOffsetFactor);
            Vector2 start = new Vector2(Main.npc[Main.wof].Center.X, yStart);

            Texture2D fleshRopeTexture = Main.chain12Texture;
            void drawChainFrom(Vector2 startingPosition)
            {
                Vector2 drawPosition = startingPosition;
                float rotation = npc.AngleFrom(drawPosition) - MathHelper.PiOver2;
                while (Vector2.Distance(drawPosition, npc.Center) > 40f)
                {
                    drawPosition += npc.DirectionFrom(drawPosition) * fleshRopeTexture.Height;
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 drawOffset = Vector2.UnitX.RotatedBy(rotation) * (float)Math.Cos(MathHelper.TwoPi * i / 4f) * 4f;
                        Color color = Lighting.GetColor((int)(drawPosition + drawOffset).X / 16, (int)(drawPosition + drawOffset).Y / 16);
                        spriteBatch.Draw(fleshRopeTexture, drawPosition + drawOffset - Main.screenPosition, null, color, rotation, fleshRopeTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                    }
                }
            }

            drawChainFrom(start);
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type != NPCID.WallofFleshEye || !Main.npc[i].active || Main.npc[i].whoAmI == npc.whoAmI)
                    continue;

                // Draw order depends on index. Therefore, if the other index is greater than this one, that means it will draw
                // a chain of its own. This is done to prevent duplicates.
                if (Main.npc[i].whoAmI < npc.whoAmI)
                    drawChainFrom(Main.npc[i].Center);
            }
            return true;
        }
        #endregion
    }
}
