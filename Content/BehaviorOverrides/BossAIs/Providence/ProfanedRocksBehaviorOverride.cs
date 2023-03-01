using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using InfernumMode.Core.OverridingSystem;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework.Graphics;
using CalamityMod;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class ProfanedRocksBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ProfanedRocks>();

        #region AI

        public override bool PreAI(NPC npc)
        {
            float idealRadius = 104f;
            ref float offsetRadius = ref npc.ai[0];
            ref float spinAngle = ref npc.ai[1];
            ref float flyAway = ref npc.Infernum().ExtraAI[0];

            // Disappear if Providence is not present.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.holyBoss))
            {
                npc.active = false;
                return false;
            }

            // Spin around Providence.
            spinAngle += MathHelper.ToRadians(40f) / idealRadius;
            NPC providence = Main.npc[CalamityGlobalNPC.holyBoss];
            npc.Center = providence.Center + spinAngle.ToRotationVector2() * offsetRadius;

            // Reset things every frame.
            npc.damage = 0;
            npc.dontTakeDamage = false;
            npc.Opacity = Utils.GetLerpValue(1260f, 1000f, offsetRadius, true);

            // Only damage damage once really close to Providence.
            if (offsetRadius >= idealRadius + 100f)
                npc.dontTakeDamage = true;

            if (flyAway == 1f)
            {
                npc.damage = 225;

                offsetRadius += 11f;
                if (offsetRadius >= 1275f)
                    npc.active = false;
            }
            
            // Converge in on Providence.
            else
            {
                float incrementalRadiusChange = Utils.Remap(offsetRadius - idealRadius, 120f, 640f, 3f, 12f);
                offsetRadius = MathHelper.Lerp(offsetRadius, idealRadius, 0.0186f) - incrementalRadiusChange;
                if (offsetRadius <= idealRadius + 2f)
                    offsetRadius = idealRadius;
            }

            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            int npcType = (int)npc.ai[2];
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ProfanedGuardians/ProfanedRocks" + npcType.ToString()).Value;
            Vector2 drawOrigin = new(texture.Width / 2, texture.Height / 2);
            Vector2 drawPos = npc.Center - Main.screenPosition;
            drawPos -= new Vector2(texture.Width, texture.Height) * npc.scale / 2f;
            drawPos += drawOrigin * npc.scale + new Vector2(0f, npc.gfxOffY);
            Rectangle frame = new(0, 0, texture.Width, texture.Height);
            npc.DrawBackglow(Color.White with { A = 0 }, 4f, SpriteEffects.None, frame, Main.screenPosition, texture);
            spriteBatch.Draw(texture, drawPos, frame, npc.GetAlpha(lightColor), npc.rotation, drawOrigin, npc.scale, SpriteEffects.None, 0f);
            return false;
        }

        #endregion
    }
}
