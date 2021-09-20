using CalamityMod.Dusts;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Guardians
{
	public class DefenderGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianBoss2>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss) || !Main.npc[CalamityGlobalNPC.doughnutBoss].active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            Player target = Main.player[npc.target];
            NPC thingToDefend = Main.npc[CalamityGlobalNPC.doughnutBoss];

            // Defend the crystal guardian if the attacker can no longer take damage on its own.
            int healerIndex = NPC.FindFirstNPC(ModContent.NPCType<ProfanedGuardianBoss3>());
            if (Main.npc.IndexInRange(healerIndex))
                thingToDefend = Main.npc[healerIndex];

            // Fade out and commit altruistic suicide if there's nothing to actually defend anymore.
            if (!Main.npc.IndexInRange(healerIndex) && thingToDefend.dontTakeDamage)
            {
                npc.dontTakeDamage = true;
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * -6f, 0.4f);
                npc.alpha = Utils.Clamp(npc.alpha + 10, 0, 255);

                if (npc.alpha >= 255)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            float healAmount = Main.npc[CalamityGlobalNPC.doughnutBoss].lifeMax - Main.npc[CalamityGlobalNPC.doughnutBoss].life;
                            healAmount /= 8;

                            Vector2 shootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 9f);
                            int shot = Utilities.NewProjectileBetter(npc.Center + shootVelocity * 3f, shootVelocity, ModContent.ProjectileType<HealingCrystalShot>(), 135, 0f);
                            Main.projectile[shot].ai[0] = CalamityGlobalNPC.doughnutBoss;
                            Main.projectile[shot].ai[1] = i / 8f;
                            Main.projectile[shot].localAI[0] = healAmount;
                        }
                    }
                    npc.life = 0;
                    npc.HitEffect();
                    npc.active = false;
                }
                return false;
            }

            npc.chaseable = false;
            npc.damage = 0;
            npc.target = thingToDefend.target;
            npc.spriteDirection = thingToDefend.spriteDirection;

            Vector2 defenseCenter = thingToDefend.Center;
            Vector2 destination = defenseCenter + thingToDefend.DirectionTo(target.Center).RotatedBy(MathHelper.PiOver2) * MathHelper.Min(300f, target.Distance(defenseCenter) + 30f);

            // Determine if there are any projectiles that are close to the main boss.
            // If there are, attempt to act as a meat shield.
            float minDistance = 1000f;

            // How fast the Guardian must move to protect the attacker, if any threats exist.
            float requiredMoveSpeed = 0f;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                // Ignore projectiles that cannot harm anything.
                if (!Main.projectile[i].active || Main.projectile[i].damage <= 0 || !Main.projectile[i].friendly)
                    continue;

                Vector2 endingLocation = defenseCenter + Main.projectile[i].velocity / Main.projectile[i].Distance(defenseCenter);

                // Ignore projectiles that are not of relative threat yet or are an inevitable threat.
                bool aimingAtTarget = Vector2.Dot(Main.projectile[i].velocity.SafeNormalize(Vector2.Zero), Main.projectile[i].DirectionTo(endingLocation)) > 0.68f;
                if (!aimingAtTarget || !Main.projectile[i].WithinRange(endingLocation, minDistance) || Main.projectile[i].WithinRange(endingLocation, 60f))
                    continue;

                // If the above checks are passed, update the minimum distance, destination, and required movement speed.
                minDistance = Main.projectile[i].Distance(endingLocation);
                destination = endingLocation - Main.projectile[i].DirectionTo(endingLocation) * MathHelper.Min(300f, Main.projectile[i].Distance(endingLocation));
                requiredMoveSpeed = Main.projectile[i].Distance(endingLocation) / Main.projectile[i].velocity.Length();
            }

            // Spawn dust if defending as an indicator.
            if (requiredMoveSpeed > 4f && !Main.dedServ)
            {
                for (int i = 0; i < 15; i++)
                {
                    Dust.NewDustDirect(npc.position, npc.width, npc.height, (int)CalamityDusts.ProfanedFire);
                }
            }

            ref float slowdownTimer = ref npc.ai[0];
            if (slowdownTimer > 0)
            {
                slowdownTimer--;
                npc.velocity *= 0.8f;
            }
            else
            {
                if (npc.justHit)
                    slowdownTimer = 18f;
                npc.velocity = (npc.velocity * 2f + npc.DirectionTo(destination) * MathHelper.Max(requiredMoveSpeed * 2.3f, (thingToDefend.position - thingToDefend.oldPos[1]).Length() * 2.3f)) / 3f;
                if (npc.WithinRange(destination, npc.velocity.Length() + 10f))
                {
                    npc.Center = destination;
                    npc.velocity = Vector2.UnitY * -0.5f;
                }
            }

            npc.alpha = npc.damage == 0 ? 180 : Utils.Clamp(npc.alpha - 32, 0, 255);
            return false;
        }
    }
}