using CalamityMod.Events;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Destroyer
{
    public class ProbeBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Probe;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            if (npc.scale != 1f)
            {
                npc.Size /= npc.scale;
                npc.scale = 1f;
            }

            npc.TargetClosest();
            Player target = Main.player[npc.target];

            Vector2 spawnOffset = Vector2.UnitY.RotatedBy(MathHelper.Lerp(-0.97f, 0.97f, npc.whoAmI % 16f / 16f)) * 300f;
            if (npc.whoAmI * 113 % 2 == 1)
                spawnOffset *= -1f;

            Vector2 destination = target.Center + spawnOffset;

            ref float generalTimer = ref npc.ai[2];
            Lighting.AddLight(npc.Center, Color.Red.ToVector3() * 1.6f);

            // Have a brief moment of no damage.
            npc.damage = npc.ai[0] == 2f ? npc.defDamage : 0;

            float hoverSpeed = 22f;
            if (BossRushEvent.BossRushActive)
                hoverSpeed *= 1.5f;
            ref float attackTimer = ref npc.ai[1];

            // Hover into position and look at the target. Once reached, reel back.
            if (npc.ai[0] == 0f)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * hoverSpeed, 0.1f);
                if (npc.WithinRange(destination, npc.velocity.Length() * 1.35f))
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * -7f;
                    npc.ai[0] = 1f;
                    npc.netUpdate = true;
                }
                npc.rotation = npc.AngleTo(target.Center);
            }

            // Reel back and decelerate.
            if (npc.ai[0] == 1f)
            {
                npc.velocity *= 0.975f;
                attackTimer++;

                int reelBackTime = BossRushEvent.BossRushActive ? 30 : 60;
                if (attackTimer >= reelBackTime)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * hoverSpeed;

                    npc.ai[0] = 2f;
                    npc.netUpdate = true;
                }
                npc.rotation = npc.AngleTo(target.Center);
            }

            // Charge at the target and explode once a tile is hit.
            if (npc.ai[0] == 2f)
            {
                npc.knockBackResist = 0f;
                if (Collision.SolidCollision(npc.position, npc.width, npc.height) && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, npc.Center);
                    for (int i = 0; i < 36; i++)
                    {
                        Dust energy = Dust.NewDustDirect(npc.position, npc.width, npc.height, 182);
                        energy.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 7f);
                        energy.noGravity = true;
                    }

                    npc.active = false;
                    npc.netUpdate = true;
                }
                npc.rotation = npc.velocity.ToRotation();
                npc.damage = 95;
            }

            npc.rotation += MathHelper.Pi;
            generalTimer++;
            return false;
        }
    }
}