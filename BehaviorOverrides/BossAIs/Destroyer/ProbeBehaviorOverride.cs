using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Destroyer
{
    public class ProbeBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Probe;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            npc.TargetClosest();
            Player target = Main.player[npc.target];
            Vector2 destination = target.Center - Vector2.UnitY.RotatedBy(MathHelper.Lerp(-0.97f, 0.97f, npc.whoAmI % 16f / 16f)) * 300f;

            ref float generalTimer = ref npc.ai[2];
            Lighting.AddLight(npc.Center, Color.Red.ToVector3() * 1.6f);

            // Have a brief moment of no damage.
            npc.damage = generalTimer > 60f ? npc.defDamage : 0;

            if (npc.ai[0] == 0f)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * 14f, 0.1f);
                if (npc.WithinRange(destination, npc.velocity.Length() * 1.35f))
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * -7f;
                    npc.ai[0] = 1f;
                    npc.netUpdate = true;
                }
                npc.rotation = npc.AngleTo(target.Center);
            }

            if (npc.ai[0] == 1f)
            {
                ref float time = ref npc.ai[1];
                npc.velocity *= 0.975f;
                time++;

                if (time >= 60f)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * 22f;
                    npc.ai[0] = 2f;
                    npc.netUpdate = true;
                }
                npc.rotation = npc.AngleTo(target.Center);
            }

            if (npc.ai[0] == 2f)
            {
                if ((Collision.SolidCollision(npc.position, npc.width, npc.height) || npc.justHit) && !Main.dedServ)
                {
                    Main.PlaySound(SoundID.DD2_KoboldExplosion, npc.Center);
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