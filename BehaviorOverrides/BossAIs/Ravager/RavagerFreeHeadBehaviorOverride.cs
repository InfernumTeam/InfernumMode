using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Ravager;
using InfernumMode.Dusts;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
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
            var currentAttack = (RavagerBodyBehaviorOverride.RavagerAttackType)ravager.ai[0];
            npc.target = ravager.target;

            Player target = Main.player[npc.target];
            ref float attackTimer = ref npc.ai[1];

            // Circle around the player slowly, releasing bolts when at the cardinal directions.
            int cinderShootRate = 180;
            Vector2 hoverOffset = (MathHelper.TwoPi * (attackTimer / cinderShootRate) / 4f).ToRotationVector2() * 360f;
            Vector2 hoverDestination = target.Center + hoverOffset;

            // Look at the player.
            Vector2 aimDirection = npc.SafeDirectionTo(target.Center, -Vector2.UnitY);
            Vector2 cinderShootPosition = npc.Center + aimDirection * 36f;
            npc.rotation = aimDirection.ToRotation() - MathHelper.PiOver2;

            // Create a dust telegraph prior to releasing cinders.
            if (currentAttack != RavagerBodyBehaviorOverride.RavagerAttackType.DetachedHeadCinderRain)
            {
                if (attackTimer % cinderShootRate > cinderShootRate - 40f)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Dust fire = Dust.NewDustPerfect(cinderShootPosition + Main.rand.NextVector2Circular(25f, 25f), ModContent.DustType<RavagerMagicDust>());
                        fire.velocity = (fire.position - cinderShootPosition).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2f, 8f);
                        fire.scale = 1.3f;
                        fire.noGravity = true;
                    }
                }

                // Shoot the cinder.
                if (attackTimer % cinderShootRate == cinderShootRate - 1f)
                {
                    Main.PlaySound(SoundID.Item72, cinderShootPosition);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(cinderShootPosition, aimDirection * 15f, ModContent.ProjectileType<DarkMagicCinder>(), 185, 0f);
                        npc.netUpdate = true;
                    }
                }
            }
            else
            {
                cinderShootRate = 60;
                hoverDestination = target.Center - Vector2.UnitY * 450f;

                if (attackTimer % cinderShootRate == cinderShootRate - 1f)
                {
                    Main.PlaySound(SoundID.Item72, cinderShootPosition);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 cinderShootVelocity = aimDirection.RotatedBy(MathHelper.Lerp(-0.49f, 0.49f, i / 2f)) * 14f;
                            Utilities.NewProjectileBetter(cinderShootPosition, cinderShootVelocity, ModContent.ProjectileType<DarkMagicCinder>(), 185, 0f);
                        }
                        npc.netUpdate = true;
                    }
                }
            }

            // Hover into position.
            float acceleration = 0.7f;
            Item heldItem = target.ActiveItem();
            if (heldItem.melee && (heldItem.shoot == ProjectileID.None || heldItem.Calamity().trueMelee))
                acceleration = 0.2f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 20f, acceleration);

            attackTimer++;
            return false;
        }
    }
}
