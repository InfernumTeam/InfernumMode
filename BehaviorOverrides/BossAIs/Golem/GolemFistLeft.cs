using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class GolemFistLeft : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Golem Fist");
        }

        public override void SetDefaults()
        {
            npc.lifeMax = 1;
            npc.defDamage = npc.damage = 75;
            npc.dontTakeDamage = true;
            npc.width = 40;
            npc.height = 40;
            npc.lavaImmune = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
        }

        public override bool PreAI() => DoFistAI(npc, true);

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor) => DrawFist(npc, spriteBatch, drawColor, true);

        public static bool DoFistAI(NPC npc, bool leftFist)
        {
            if (!Main.npc[(int)npc.ai[0]].active || Main.npc[(int)npc.ai[0]].type != NPCID.Golem)
            {
                GolemBodyBehaviorOverride.DespawnNPC(npc.whoAmI);
                return false;
            }
            npc.dontTakeDamage = true;
            npc.chaseable = false;
            return false;
        }

        public static bool DrawFist(NPC npc, SpriteBatch spriteBatch, Color lightColor, bool leftFist)
        {
            if (npc.Opacity == 0f)
                return false;

            NPC body = Main.npc[(int)npc.ai[0]];
            float telegraphRotation = body.Infernum().ExtraAI[11];
            float telegraphInterpolant = body.Infernum().ExtraAI[19];
            bool isCorrectArmForTelegraph = Math.Cos(telegraphRotation) > 0f ? npc.type == ModContent.NPCType<GolemFistRight>() : npc.type == ModContent.NPCType<GolemFistLeft>();

            // Draw the telegraph as necessary.
            if (telegraphInterpolant > 0f && isCorrectArmForTelegraph)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                Texture2D line = ModContent.GetTexture("InfernumMode/ExtraTextures/BloomLine");
                Color outlineColor = Color.Lerp(Color.OrangeRed, Color.White, telegraphInterpolant);
                Vector2 origin = new Vector2(line.Width / 2f, line.Height);
                Vector2 beamScale = new Vector2(telegraphInterpolant * 0.5f, 2.4f);

                Vector2 drawPosition = npc.Center - Main.screenPosition;
                Vector2 beamDirection = -telegraphRotation.ToRotationVector2();
                float beamRotation = beamDirection.ToRotation() - MathHelper.PiOver2;
                Main.spriteBatch.Draw(line, drawPosition, null, outlineColor, beamRotation, origin, beamScale, 0, 0f);

                Main.spriteBatch.ResetBlendState();
            }

            Vector2 FistCenterPos = leftFist ? new Vector2(body.Left.X, body.Left.Y) : new Vector2(body.Right.X, body.Right.Y);
            float armRotation = npc.AngleFrom(FistCenterPos) + MathHelper.PiOver2;
            bool continueDrawing = true;
            while (continueDrawing)
            {
                int moveDistance = 16;
                if (npc.Distance(FistCenterPos) < moveDistance)
                {
                    moveDistance = (int)npc.Distance(FistCenterPos);
                    continueDrawing = false;
                }
                Color color = Lighting.GetColor((int)(FistCenterPos.X / 16f), (int)(FistCenterPos.Y / 16f));
                Texture2D armTexture = Main.chain21Texture;
                Rectangle frame = new Rectangle(0, 0, armTexture.Width, moveDistance);
                spriteBatch.Draw(armTexture, FistCenterPos - Main.screenPosition, frame, color, armRotation, armTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                FistCenterPos += (npc.Center - FistCenterPos).SafeNormalize(Vector2.Zero) * moveDistance;
            }

            SpriteEffects effect = leftFist ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Texture2D texture = Main.projectileTexture[ModContent.ProjectileType<FistBullet>()];
            Rectangle rect = new Rectangle(0, 0, texture.Width, texture.Height);
            Main.spriteBatch.Draw(texture, npc.Center - Main.screenPosition, rect, lightColor * npc.Opacity, npc.rotation, rect.Size() * 0.5f, 1f, effect, 0f);
            return false;
        }
    }
}
