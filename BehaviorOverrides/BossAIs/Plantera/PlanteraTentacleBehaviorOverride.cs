using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Plantera
{
    public class PlanteraTentacleBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.PlanterasTentacle;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            // Die if Plantera is absent or not using tentacles.
            if (!Main.npc.IndexInRange(NPC.plantBoss) || Main.npc[NPC.plantBoss].ai[0] != (int)PlanteraBehaviorOverride.PlanteraAttackState.TentacleSnap)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.checkDead();
                npc.netUpdate = true;
                return false;
            }

            // Ensure that the tentacle always draws, even when far offscreen.
            NPCID.Sets.MustAlwaysDraw[npc.type] = true;

            float attachAngle = npc.ai[0];
            ref float attachOffset = ref npc.ai[1];
            ref float time = ref npc.ai[2];

            // Reel inward prior to snapping.
            if (time > -20f && time < 5f)
                attachOffset = MathHelper.Lerp(attachOffset, 45f, 0.05f);

            // Reach outward swiftly in hopes of hitting a target.
            if (time > 30f)
                attachOffset = MathHelper.Lerp(attachOffset, 3900f, 0.021f);

            if (time == 30f)
                Main.PlaySound(SoundID.Item74, npc.Center);

            if (time > 70f)
            {
                npc.scale *= 0.85f;

                // Die once small enough.
                npc.Opacity = npc.scale;
                if (npc.scale < 0.01f)
                {
                    npc.life = 0;
                    npc.HitEffect();
                    npc.checkDead();
                    npc.active = false;
                }
            }

            npc.Center = Main.npc[NPC.plantBoss].Center + attachAngle.ToRotationVector2() * attachOffset;
            npc.rotation = attachAngle + MathHelper.Pi;
            npc.dontTakeDamage = true;

            time++;
            return false;
        }
    }
}
