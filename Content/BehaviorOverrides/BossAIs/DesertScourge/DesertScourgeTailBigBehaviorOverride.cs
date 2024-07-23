using System;
using CalamityMod;
using CalamityMod.NPCs.DesertScourge;
using InfernumMode.Assets.BossTextures;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.DesertScourge
{
    public class DesertScourgeTailBigBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DesertScourgeTail>();

        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 104;
            npc.height = 104;
            npc.scale = 1f;
            npc.Opacity = 1f;
            npc.defense = 9;
            npc.DR_NERD(0.1f);
            npc.alpha = 255;
        }

        public override bool PreAI(NPC npc)
        {
            if (!Main.npc.IndexInRange((int)npc.ai[1]) || !Main.npc[(int)npc.ai[1]].active)
            {
                npc.life = 0;
                npc.HitEffect(0, 10.0);
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }
            NPC aheadSegment = Main.npc[(int)npc.ai[1]];

            npc.target = aheadSegment.target;
            if (aheadSegment.alpha < 128)
                npc.alpha = Utils.Clamp(npc.alpha - 42, 0, 255);

            npc.defense = aheadSegment.defense;
            npc.dontTakeDamage = aheadSegment.dontTakeDamage;

            Vector2 segmentTilePos = npc.Center;
            float playerXPos = Main.player[npc.target].Center.X;
            float playerYPos = Main.player[npc.target].Center.Y;
            playerXPos = (int)(playerXPos / 16f) * 16;
            playerYPos = (int)(playerYPos / 16f) * 16;
            segmentTilePos.X = (int)(segmentTilePos.X / 16f) * 16;
            segmentTilePos.Y = (int)(segmentTilePos.Y / 16f) * 16;
            playerXPos -= segmentTilePos.X;
            playerYPos -= segmentTilePos.Y;
            float playerDistance = (float)Math.Sqrt((double)(playerXPos * playerXPos + playerYPos * playerYPos));
            if (npc.ai[1] > 0f && npc.ai[1] < (float)Main.npc.Length)
            {
                try
                {
                    segmentTilePos = npc.Center;
                    playerXPos = Main.npc[(int)npc.ai[1]].Center.X - segmentTilePos.X;
                    playerYPos = Main.npc[(int)npc.ai[1]].Center.Y - segmentTilePos.Y;
                }
                catch
                {
                }
                npc.rotation = (float)Math.Atan2((double)playerYPos, (double)playerXPos) + MathHelper.PiOver2;
                playerDistance = (float)Math.Sqrt((double)(playerXPos * playerXPos + playerYPos * playerYPos));

                int segmentOffset = 70;
                playerDistance = (playerDistance - segmentOffset) / playerDistance;
                playerXPos *= playerDistance;
                playerYPos *= playerDistance;
                npc.velocity = Vector2.Zero;
                npc.position.X = npc.position.X + playerXPos;
                npc.position.Y = npc.position.Y + playerYPos;

                if (playerXPos < 0f)
                    npc.spriteDirection = 1;
                else if (playerXPos > 0f)
                    npc.spriteDirection = -1;
            }

            return false;
        }
    }
}
