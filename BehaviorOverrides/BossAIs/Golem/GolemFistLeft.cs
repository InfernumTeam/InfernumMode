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
            NPC.lifeMax = 1;
            NPC.defDamage = NPC.damage = 75;
            NPC.dontTakeDamage = true;
            NPC.width = 40;
            NPC.height = 40;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
        }

        public override bool PreAI() => DoFistAI(NPC, true);

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor) => DrawFist(NPC, spriteBatch, drawColor, true);

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
                Rectangle frame = new(0, 0, armTexture.Width, moveDistance);
                spriteBatch.Draw(armTexture, FistCenterPos - Main.screenPosition, frame, color, armRotation, armTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                FistCenterPos += (npc.Center - FistCenterPos).SafeNormalize(Vector2.Zero) * moveDistance;
            }

            SpriteEffects effect = leftFist ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Texture2D texture = Main.projectileTexture[ModContent.ProjectileType<FistBullet>()];
            Rectangle rect = new(0, 0, texture.Width, texture.Height);
            Main.spriteBatch.Draw(texture, npc.Center - Main.screenPosition, rect, lightColor * npc.Opacity, npc.rotation, rect.Size() * 0.5f, 1f, effect, 0f);
            return false;
        }
    }
}
