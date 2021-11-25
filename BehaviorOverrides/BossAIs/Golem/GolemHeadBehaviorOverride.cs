using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        private static Dictionary<GolemAttackState, Color> AttackEyeColorPairs = new Dictionary<GolemAttackState, Color>
        {
            [GolemAttackState.ArmBullets] = Color.AntiqueWhite,
            [GolemAttackState.FistSpin] = Color.Red,
            [GolemAttackState.HeatRay] = Color.Orange,
            [GolemAttackState.SpikeTrapWaves] = Color.LightBlue,
            [GolemAttackState.SpinLaser] = Color.Firebrick,
            [GolemAttackState.Slingshot] = Color.MediumPurple,
        };

        public override bool PreAI(NPC npc)
        {
            npc.chaseable = !npc.dontTakeDamage;
            npc.Opacity = npc.dontTakeDamage ? 0f : 1f;
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            if (npc.dontTakeDamage)
                return false;

            Texture2D texture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Golem/AttachedHead");
            Rectangle rect = new Rectangle(0, 0, texture.Width, texture.Height);
            Main.spriteBatch.Draw(texture, npc.Center - Main.screenPosition, rect, lightColor, npc.rotation, npc.Center, 1f, SpriteEffects.None, 0f);
            DoEyeDrawing(npc);
            return false;
        }

        public static void DoEyeDrawing(NPC npc)
        {
            Texture2D texture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Golem/GolemEyeGlow");
            Rectangle rect = new Rectangle(0, 0, texture.Width, texture.Height);
            float rotation = MathHelper.Lerp(0f, MathHelper.TwoPi, npc.ai[1] / 240f);
            float rotation2 = Utils.InverseLerp(0f, MathHelper.TwoPi, npc.ai[1] / 240f);
            Color drawColor = AttackEyeColorPairs[(GolemAttackState)Main.npc[(int)npc.ai[0]].ai[1]] * 0.75f;
            drawColor.A = 0;
            Vector2 drawPos = npc.type == NPCID.GolemHead ? new Vector2(npc.Center.X - 15f, npc.Bottom.Y + 50f) : new Vector2(npc.Center.X - 15f, npc.Bottom.Y + 78f);
            Vector2 drawPos2 = new Vector2(drawPos.X + 30f, drawPos.Y);

            for (float i = 4; i > 0; i--)
            {
                float scale = i / 4f;
                Main.spriteBatch.Draw(texture, drawPos, rect, drawColor, rotation, drawPos, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(texture, drawPos2, rect, drawColor, rotation2, drawPos2, scale, SpriteEffects.None, 0f);
            }
        }
    }
}
