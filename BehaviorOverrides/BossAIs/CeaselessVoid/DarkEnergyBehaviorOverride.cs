using CalamityMod.NPCs;
using CalamityMod.NPCs.CeaselessVoid;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class DarkEnergyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DarkEnergy>();

        public override bool PreAI(NPC npc)
        {
            bool shouldFadeAway = npc.Infernum().ExtraAI[1] == 1f;
            ref float spinAngle = ref npc.ai[0];
            ref float spinMovementSpeed = ref npc.ai[1];
            ref float spinRadius = ref npc.ai[2];
            ref float radiusUpdateDirection = ref npc.ai[3];
            ref float timer = ref npc.Infernum().ExtraAI[0];

            // Initialize the radius update direction.
            if (radiusUpdateDirection == 0f)
                radiusUpdateDirection = 1f;

            // Vanish if the Ceaseless Void is not present.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.voidBoss))
            {
                npc.active = false;
                return false;
            }
            NPC ceaselessVoid = Main.npc[CalamityGlobalNPC.voidBoss];

            // Disable damage universally for a short period of time to both give the target time to react and to prevent all energy being killed at once.
            npc.dontTakeDamage = false;
            npc.damage = npc.defDamage;
            if (shouldFadeAway || timer < 180f || !Main.player[Player.FindClosest(ceaselessVoid.Center, 1, 1)].WithinRange(ceaselessVoid.Center, CeaselessVoidBehaviorOverride.DarkEnergyOffsetRadius + 200f))
            {
                npc.damage = 0;
                npc.dontTakeDamage = true;
            }
            npc.Opacity = MathHelper.Clamp(npc.Opacity - shouldFadeAway.ToDirectionInt() * 0.03f, 0f, 1f);

            // Spin around.
            spinRadius += radiusUpdateDirection * spinMovementSpeed * 1.6f;
            spinAngle += radiusUpdateDirection * spinMovementSpeed * MathHelper.ToRadians(0.27f);
            if (spinRadius >= CeaselessVoidBehaviorOverride.DarkEnergyOffsetRadius)
                radiusUpdateDirection = -1f;
            if (spinRadius <= 0f)
                radiusUpdateDirection = 1f;

            // Stick to the Ceaseless Void.
            npc.Center = ceaselessVoid.Center + spinAngle.ToRotationVector2() * spinRadius;
            timer++;
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            NPCID.Sets.TrailingMode[npc.type] = 0;
            NPCID.Sets.TrailCacheLength[npc.type] = 4;

            Texture2D baseTexture = TextureAssets.Npc[npc.type].Value;
            for (int i = npc.oldPos.Length - 1; i >= 1; i--)
            {
                Vector2 drawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                Color illusionColor = new Color(78, 244, 197) * npc.Opacity * ((npc.oldPos.Length - i) / (float)npc.oldPos.Length);
                illusionColor.A = 0;
                Main.spriteBatch.Draw(baseTexture, drawPosition, npc.frame, illusionColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0f);
            }
            Main.spriteBatch.Draw(baseTexture, npc.Center - Main.screenPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0f);
            return false;
        }
    }
}
