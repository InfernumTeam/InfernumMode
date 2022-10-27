using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using GreatSandSharkNPC = CalamityMod.NPCs.GreatSandShark.GreatSandShark;

namespace InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class GreatSandSharkBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<GreatSandSharkNPC>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            // Disappear if the bereft vassal is not present.
            int vassal = NPC.FindFirstNPC(ModContent.NPCType<BereftVassal>());
            if (vassal == -1)
            {
                npc.active = false;
                return false;
            }

            // Do not despawn.
            npc.timeLeft = 7200;

            // Stay inside of the world.
            npc.Center = Vector2.Clamp(npc.Center, Vector2.One * 150f, Vector2.One * new Vector2(Main.maxTilesX * 16f - 150f, Main.maxTilesY * 16f - 150f));

            // Fix vanilla FindFrame jank.
            npc.localAI[3] = 1f;

            // Stop being so FAT you SILLY shark!!!
            npc.height = 100;
            npc.width = 280;

            // Reset damage and other things.
            npc.damage = npc.defDamage;

            // Inherit attributes from the bereft vassal.
            BereftVassalComboAttackManager.InheritAttributesFromLeader(npc);

            Player target = Main.player[npc.target];
            BereftVassalComboAttackManager.DoComboAttacksIfNecessary(npc, target, ref BereftVassalComboAttackManager.Vassal.ai[1]);
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
            return false;
        }
    }
}
