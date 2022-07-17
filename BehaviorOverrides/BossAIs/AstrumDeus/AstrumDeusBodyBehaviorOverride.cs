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
    public class AstrumDeusBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AstrumDeusBody>();

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
            NPC headSegment = Main.npc[(int)npc.ai[1]];
            npc.target = aheadSegment.target;
            npc.alpha = headSegment.alpha;

            npc.defense = aheadSegment.defense;
            npc.dontTakeDamage = aheadSegment.dontTakeDamage;
            npc.damage = npc.alpha > 40 || headSegment.damage <= 0 ? 0 : npc.defDamage;

            npc.Calamity().DR = 0.325f;
            npc.Calamity().newAI[1] = 600f;

            // Perform segment positioning and rotation.
            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.12f);
            if (headSegment.Infernum().ExtraAI[7] == 1f)
                npc.HitSound = SoundID.NPCHit1;

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;

            // Emit particles if the head says to do so.
            if (Main.netMode != NetmodeID.MultiplayerClient && headSegment.localAI[1] == 1f && Main.rand.NextFloat() < npc.Opacity)
                Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2Circular(4f, 4f), ModContent.ProjectileType<AstralSparkle>(), 0, 0f);
            
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/AstrumDeus/AstrumDeusBody").Value;
            if (Main.npc[npc.realLife].Infernum().ExtraAI[7] == 1f)
            {
                texture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/AstrumDeus/AstrumDeusBodyExposed").Value;
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
