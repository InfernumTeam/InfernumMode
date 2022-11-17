using CalamityMod.NPCs.Abyss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.AbyssAIs
{
    public class ColossalSquidBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ColossalSquid>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        #region AI and Behaviors
        public override bool PreAI(NPC npc)
        {
            ref float isHostile = ref npc.Infernum().ExtraAI[0];

            // Don't naturally despawn if sleeping.
            if (isHostile != 1f)
                npc.timeLeft = 7200;

            if (npc.justHit && npc.Infernum().ExtraAI[0] != 1f)
            {
                isHostile = 1f;
                npc.netUpdate = true;
            }

            return isHostile == 1f;
        }
        #endregion AI and Behaviors

        #region Frames and Drawcode
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/AbyssAIs/SleepingColossalSquid").Value;
            Rectangle frame = texture.Frame();
            if (npc.Infernum().ExtraAI[0] == 1f)
            {
                texture = TextureAssets.Npc[npc.type].Value;
                frame = npc.frame;
            }

            Vector2 drawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * 30f;
            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(texture, drawPosition, frame, npc.GetAlpha(lightColor), npc.rotation, frame.Size() * 0.5f, npc.scale, direction, 0f);
            return false;
        }
        #endregion Frames and Drawcode
    }
}
