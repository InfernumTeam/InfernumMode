using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

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
            spinAngle += ToRadians(40f) / idealRadius;
            NPC providence = Main.npc[CalamityGlobalNPC.holyBoss];
            npc.Center = providence.Center + spinAngle.ToRotationVector2() * offsetRadius;

            // Reset things every frame.
            npc.damage = 0;
            npc.dontTakeDamage = false;
            npc.Opacity = Utils.GetLerpValue(1180f, 1000f, offsetRadius, true);

            // Only damage damage once really close to Providence.
            if (offsetRadius >= idealRadius + 100f)
                npc.dontTakeDamage = true;

            if (flyAway == 1f)
            {
                if (npc.Opacity >= 1f)
                    npc.damage = 225;

                offsetRadius += 9f;
                if (offsetRadius >= 1200f)
                    npc.active = false;
            }

            // Converge in on Providence.
            else
            {
                float incrementalRadiusChange = Utils.Remap(offsetRadius - idealRadius, 120f, 640f, 3f, 12f);
                offsetRadius = Lerp(offsetRadius, idealRadius, 0.0186f) - incrementalRadiusChange;
                if (offsetRadius <= idealRadius + 2f)
                    offsetRadius = idealRadius;
            }

            return false;
        }

        #endregion AI

        #region Drawcode

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            int npcType = (int)npc.ai[2];
            string rockVariantName = $"ProfanedRocks{npcType}";
            Texture2D texture = ModContent.Request<Texture2D>($"CalamityMod/NPCs/ProfanedGuardians/{rockVariantName}").Value;
            if (ProvidenceBehaviorOverride.IsEnraged)
            {
                rockVariantName = $"ProfanedRocksNight{npcType}";
                texture = ModContent.Request<Texture2D>($"InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/{rockVariantName}").Value;
            }

            Vector2 origin = new(texture.Width / 2, texture.Height / 2);
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Rectangle frame = new(0, 0, texture.Width, texture.Height);

            // Draw a backglow behind the rock for visual clarity reasons, along with the rock itself.
            npc.DrawBackglow(Color.White with { A = 0 }, 4f, SpriteEffects.None, frame, Main.screenPosition, texture);
            spriteBatch.Draw(texture, drawPosition, frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
        #endregion Drawcode
    }
}
