using CalamityMod;
using CalamityMod.NPCs.AstrumDeus;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.AstrumDeus
{
	public class AstrumDeusBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AstrumDeusBodySpectral>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

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
            npc.damage = npc.alpha > 40 ? 0 : npc.defDamage;

            npc.Calamity().DR = 0.55f;
            npc.Calamity().newAI[1] = 600f;

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.075f);
            
            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;

            // Emit particles if the head says to do so.
            if (Main.netMode != NetmodeID.MultiplayerClient && headSegment.localAI[1] == 1f && Main.rand.NextFloat() < npc.Opacity)
                Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2Circular(4f, 4f), ModContent.ProjectileType<AstralSparkle>(), 0, 0f);

            // Grow any nearby stars.
            int starGrowChance = (int)MathHelper.Lerp(230f, 135f, headSegment.Infernum().ExtraAI[6]);
            List<Projectile> stars = Utilities.AllProjectilesByID(ModContent.ProjectileType<GiantAstralStar>()).ToList();
            if (stars.Count > 0 && stars.First().scale < 7f && Main.rand.NextBool(starGrowChance))
                Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 8f), ModContent.ProjectileType<StellarEnergy>(), 0, 0f);

            return false;
        }
    }
}
