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
            npc.width = 32;
            npc.height = 48;
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

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(WrapAngle(aheadSegment.rotation - npc.rotation) * 0.075f);

            npc.rotation = directionToNextSegment.ToRotation() + PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.Draw(BossTextureRegistry.DesertScourgeTail.Value, npc.Center - Main.screenPosition, null, npc.GetAlpha(lightColor), npc.rotation, BossTextureRegistry.DesertScourgeTail.Value.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
