using CalamityMod.NPCs;
using CalamityMod.NPCs.CeaselessVoid;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
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
            ref float spinDirection = ref npc.ai[3];
            ref float timer = ref npc.Infernum().ExtraAI[0];

            // Initialize the radius update direction.
            if (spinDirection == 0f)
                spinDirection = 1f;

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
            npc.Opacity = Clamp(npc.Opacity - shouldFadeAway.ToDirectionInt() * 0.03f, 0f, 1f);

            // Spin around.
            spinAngle += spinDirection * spinMovementSpeed * ToRadians(0.27f);
            npc.rotation = spinAngle;

            // Stick to the Ceaseless Void.
            npc.Center = ceaselessVoid.Center + spinAngle.ToRotationVector2() * spinRadius;
            timer++;
            return false;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            npc.frame.Width = 64;
            npc.frame.Height = 76;
            npc.frame.Y = (int)(npc.frameCounter / 5 + npc.whoAmI) % 8 * npc.frame.Height;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            NPCID.Sets.TrailingMode[npc.type] = 0;
            NPCID.Sets.TrailCacheLength[npc.type] = 4;

            Texture2D baseTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CeaselessVoid/DarkEnergyBright").Value;
            for (int i = npc.oldPos.Length - 1; i >= 1; i--)
            {
                Vector2 drawPosition = Vector2.Lerp(npc.oldPos[i] + npc.Size * 0.5f, npc.Center, 0.5f) - Main.screenPosition;
                Color illusionColor = Color.White * npc.Opacity * ((npc.oldPos.Length - i) / (float)npc.oldPos.Length);
                illusionColor.A = 0;
                Main.spriteBatch.Draw(baseTexture, drawPosition, npc.frame, illusionColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0f);
            }
            Main.spriteBatch.Draw(baseTexture, npc.Center - Main.screenPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0f);
            return false;
        }
    }
}
