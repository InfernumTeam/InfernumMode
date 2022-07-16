using CalamityMod;
using CalamityMod.NPCs.AstrumDeus;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class AstrumDeusBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AstrumDeusBody>();

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
            npc.damage = npc.alpha > 40 || headSegment.damage <= 0 ? 0 : npc.defDamage;

            npc.Calamity().DR = 0.325f;
            npc.Calamity().newAI[1] = 600f;

            Player target = Main.player[npc.target];
            float headAngerFactor = headSegment.Infernum().ExtraAI[6];

            // Perform segment positioning and rotation.
            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.12f);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;

            // Emit particles if the head says to do so.
            if (Main.netMode != NetmodeID.MultiplayerClient && headSegment.localAI[1] == 1f && Main.rand.NextFloat() < npc.Opacity)
                Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2Circular(4f, 4f), ModContent.ProjectileType<AstralSparkle>(), 0, 0f);

            /*
            List<Projectile> stars = Utilities.AllProjectilesByID(ModContent.ProjectileType<GiantAstralStar>()).ToList();

            bool growingStar = stars.Count > 0 && stars.First().scale < 7f;
            if (growingStar)
            {
                int starGrowChance = (int)MathHelper.Lerp(230f, 135f, headAngerFactor);
                int laserShootChance = (int)MathHelper.Lerp(285f, 160f, headAngerFactor);

                // Grow any nearby stars.
                if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(starGrowChance))
                    Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 8f), ModContent.ProjectileType<StellarEnergy>(), 0, 0f);

                // And fire lasers at the target from time to time while doing so.
                // This does not happen if the target is noticably close.
                if (!npc.WithinRange(target.Center, 200f) && Main.rand.NextBool(laserShootChance))
                {
                    SoundEngine.PlaySound(SoundID.Item12, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float shootSpeed = npc.Distance(target.Center) / 135f + 18.5f;
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center) * shootSpeed;
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<AstralShot2>(), 160, 0f);
                    }
                }
            }
            */

            return false;
        }
    }
}
