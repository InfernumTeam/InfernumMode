using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Guardians
{
    public class HealerGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianBoss3>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss) || !Main.npc[CalamityGlobalNPC.doughnutBoss].active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            NPC attacker = Main.npc[CalamityGlobalNPC.doughnutBoss];
            Player target = Main.player[attacker.target];

            npc.damage = 0;
            npc.target = attacker.target;
            npc.spriteDirection = (npc.velocity.X > 0).ToDirectionInt();

            bool defenderAlive = NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianBoss2>());
            ref float shootTimer = ref npc.ai[0];
            ref float initialRotationalOffset = ref npc.ai[1];

            int totalCrystalShots = !defenderAlive ? 7 : 4;
            int shootRate = !defenderAlive ? 10 : 17;
            float shootWaitTime = 160f;

            // Try to be at the opposite side of the attacker relative to the player at all times.
            Vector2 destination = target.Center - target.DirectionTo(attacker.Center) * MathHelper.Max(160f, target.Distance(attacker.Center));

            if (shootTimer >= shootWaitTime)
            {
                destination = target.Center + (MathHelper.TwoPi * Utils.InverseLerp(shootWaitTime, shootWaitTime + totalCrystalShots * shootRate, shootTimer) + initialRotationalOffset).ToRotationVector2() * 720f;
                npc.Center = Vector2.Lerp(npc.Center, destination, 0.2f);
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.velocity = Vector2.Zero;
            }

            if (!npc.WithinRange(destination, npc.velocity.Length() * 3f + attacker.velocity.Length() * 5f + 8f))
            {
                // Ensure the healer does not move super slowly.
                if (npc.velocity.Length() < 2f)
                    npc.velocity = Vector2.UnitY * -2.4f;

                float flySpeed = MathHelper.Lerp(9f, 23f, Utils.InverseLerp(50f, 270f, npc.Distance(destination), true));
                npc.velocity = npc.velocity * 0.85f + npc.DirectionTo(destination) * flySpeed * 0.15f;
            }
            else
            {
                shootTimer += shootTimer >= shootWaitTime ? 1 : 7;
                if (initialRotationalOffset == 0f)
                    initialRotationalOffset = npc.AngleFrom(target.Center);

                if (shootTimer >= shootWaitTime && shootTimer % shootRate == 0)
                {
                    Main.PlaySound(SoundID.Item101, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float shotNumber = (shootTimer - shootWaitTime) / shootRate;
                        Vector2 shootVelocity = npc.DirectionTo(target.Center).RotatedBy(MathHelper.PiOver2 * Main.rand.NextFloatDirection()) * 3f;
                        int shot = Utilities.NewProjectileBetter(npc.Center + shootVelocity * 3f, shootVelocity, ModContent.ProjectileType<CrystalShot>(), 135, 0f);
                        Main.projectile[shot].ai[0] = npc.target;
                        Main.projectile[shot].ai[1] = shotNumber / totalCrystalShots;
                    }

                    if (shootTimer - shootWaitTime > shootRate * totalCrystalShots)
                    {
                        initialRotationalOffset = 0f;
                        shootTimer = 0f;
                        npc.netUpdate = true;
                    }
                }
                npc.velocity *= 0.7f;

                npc.alpha = npc.damage == 0 ? 180 : Utils.Clamp(npc.alpha - 32, 0, 255);
            }

            return false;
        }
    }
}