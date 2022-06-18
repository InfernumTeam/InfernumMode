using CalamityMod.Events;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeCannonBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.PrimeCannon;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float hoverDirection = npc.ai[0];
            float ownerIndex = npc.ai[1];
            ref float attackTimer = ref npc.ai[2];

            if (!Main.npc.IndexInRange((int)ownerIndex) || !Main.npc[(int)ownerIndex].active)
            {
                npc.life = 0;
                npc.StrikeNPCNoInteraction(9999, 0f, 0);
                npc.netUpdate = true;
                return false;
            }

            NPC owner = Main.npc[(int)ownerIndex];
            npc.target = owner.target;

            Player target = Main.player[npc.target];

            // Disable contact damage.
            npc.damage = 0;

            bool shouldBeInactive = PrimeHeadBehaviorOverride.ShouldBeInactive(npc.type, owner.ai[2]);
            Vector2 hoverDestination = owner.Center + new Vector2(hoverDirection * -180f, shouldBeInactive ? 260f : -150f);
            if (shouldBeInactive)
                hoverDestination += owner.velocity * 4f;
            if (!npc.WithinRange(hoverDestination, 40f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 20f, shouldBeInactive ? 0.07f : 0.18f);
            if (!npc.WithinRange(hoverDestination, 450f))
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 20f, 0.1f);
                npc.Center = npc.Center.MoveTowards(hoverDestination, 2f);
            }

            if (shouldBeInactive)
            {
                attackTimer = 0f;
                PrimeHeadBehaviorOverride.ArmHoverAI(npc);
                return false;
            }

            attackTimer++;
            npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) - MathHelper.PiOver2, 0.08f);

            int shootRate = 145 - (4 - PrimeHeadBehaviorOverride.RemainingArms) * 28;
            bool canShootNukes = lifeRatio < 0.5f || PrimeHeadBehaviorOverride.RemainingArms <= 2;
            if (canShootNukes)
                shootRate += PrimeHeadBehaviorOverride.RemainingArms == 1 ? 75 : 220;

            if (attackTimer >= shootRate)
            {
                Main.PlaySound(SoundID.Item38, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (Main.rand.NextBool(2) && canShootNukes)
                    {
                        Vector2 nukeShootVelocity = npc.SafeDirectionTo(target.Center).RotatedByRandom(1.58f) * 8f;
                        Utilities.NewProjectileBetter(npc.Center + nukeShootVelocity * 7f, nukeShootVelocity, ModContent.ProjectileType<PrimeNuke>(), 180, 0f);
                    }
                    else
                    {
                        for (int i = 0; i < (canShootNukes ? 12 : 4); i++)
                        {
                            Vector2 rocketShootVelocity = npc.SafeDirectionTo(target.Center).RotatedByRandom(0.78f) * 11f;
                            if (BossRushEvent.BossRushActive)
                                rocketShootVelocity *= 2.15f;
                            Utilities.NewProjectileBetter(npc.Center + rocketShootVelocity * 6f, rocketShootVelocity, ProjectileID.SaucerMissile, 135, 0f);
                        }
                    }
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            return false;
        }
    }
}