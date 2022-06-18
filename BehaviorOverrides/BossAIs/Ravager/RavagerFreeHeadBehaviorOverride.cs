using CalamityMod.NPCs;
using CalamityMod.NPCs.Ravager;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class RavagerFreeHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<RavagerHead2>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            // Die if the main body does not exist anymore.
            if (CalamityGlobalNPC.scavenger < 0 || !Main.npc[CalamityGlobalNPC.scavenger].active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            // Inherit attributes from the main body.
            NPC ravager = Main.npc[CalamityGlobalNPC.scavenger];
            npc.target = ravager.target;

            Player target = Main.player[npc.target];
            ref float attackTimer = ref npc.ai[1];

            // Circle around the player slowly, releasing bolts when at the cardinal directions.
            int laserShootRate = 120;
            float wallAttackTimer = Main.npc[Main.wof].ai[3];
            float hoverSpeedFactor = 1f;
            bool doCircleAttack = wallAttackTimer % 1200f < 600f || Main.npc[Main.wof].life > Main.npc[Main.wof].lifeMax * 0.45f;
            Vector2 hoverOffset = (MathHelper.TwoPi * (attackTimer / laserShootRate) / 4f).ToRotationVector2() * 360f;
            Vector2 hoverDestination = target.Center + hoverOffset;

            // Look at the player.
            Vector2 aimDirection = npc.SafeDirectionTo(target.Center, -Vector2.UnitY);
            Vector2 laserShootPosition = npc.Center + aimDirection * 36f;
            npc.rotation = aimDirection.ToRotation() - MathHelper.PiOver2;

            // Create a dust telegraph prior to releasing lasers.
            if (attackTimer % laserShootRate > laserShootRate - 40f)
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust laser = Dust.NewDustPerfect(laserShootPosition + Main.rand.NextVector2Circular(25f, 25f), 182);
                    laser.velocity = (laserShootPosition - laser.position).SafeNormalize(Vector2.UnitY) * -Main.rand.NextFloat(2f, 8f);
                    laser.noGravity = true;
                }
            }

            attackTimer++;
            return false;
        }
    }
}
