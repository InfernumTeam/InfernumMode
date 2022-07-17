using CalamityMod;
using CalamityMod.NPCs.AstrumDeus;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class AstrumDeusTailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AstrumDeusTail>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            if (!Main.npc.IndexInRange((int)npc.ai[0]) || !Main.npc[(int)npc.ai[0]].active)
            {
                npc.life = 0;
                npc.HitEffect(0, 10.0);
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            NPC aheadSegment = Main.npc[(int)npc.ai[0]];
            npc.target = aheadSegment.target;
            npc.alpha = aheadSegment.alpha;

            npc.defense = aheadSegment.defense;
            npc.dontTakeDamage = aheadSegment.dontTakeDamage;
            npc.damage = npc.alpha > 40 ? 0 : npc.defDamage;

            npc.Calamity().DR = MathHelper.Min(npc.Calamity().DR, 0.65f);

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.075f);
            if (aheadSegment.Infernum().ExtraAI[7] == 1f)
                npc.HitSound = SoundID.NPCHit1;

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;

            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/AstrumDeus/AstrumDeusTail").Value;
            if (Main.npc[npc.realLife].Infernum().ExtraAI[7] == 1f)
            {
                texture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/AstrumDeus/AstrumDeusTailExposed").Value;
                lightColor = Color.White;
            }
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            lightColor = Color.Lerp(lightColor, Color.White, 0.6f);
            Main.spriteBatch.Draw(texture, drawPosition, null, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, 0, 0f);
            return false;
        }
    }
}
