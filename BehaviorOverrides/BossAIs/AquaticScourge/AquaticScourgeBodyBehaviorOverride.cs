using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.AquaticScourge;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class AquaticScourgeBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AquaticScourgeBody>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            if (!Main.npc.IndexInRange((int)npc.ai[1]) || !Main.npc[(int)npc.ai[1]].active)
            {
                npc.life = 0;
                npc.HitEffect(0, 10.0);
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            NPC headSegment = Main.npc[npc.realLife];
            npc.target = aheadSegment.target;

            if (aheadSegment.alpha < 128)
                npc.alpha = Utils.Clamp(npc.alpha - 42, 0, 255);

            npc.defense = aheadSegment.defense;
            npc.damage = 50;
            npc.dontTakeDamage = aheadSegment.dontTakeDamage;
            npc.chaseable = headSegment.chaseable;
            npc.Calamity().newAI[0] = npc.chaseable.ToInt();

            npc.Calamity().DR = MathHelper.Min(npc.Calamity().DR, 0.4f);

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.075f);
            ref float attackTimer = ref npc.ai[3];

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;

            attackTimer++;
            float lifeRatio = headSegment.life / (float)headSegment.lifeMax;
            bool canShoot = !npc.WithinRange(Main.player[npc.target].Center, 380f) && lifeRatio < 0.25f;
            if (canShoot && attackTimer > Main.rand.NextFloat(320f, 415f) && Utilities.AllProjectilesByID(ModContent.ProjectileType<SlowerSandTooth>()).Count() < 5)
            {
                Main.PlaySound(SoundID.Item17, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 toothVelocity = Main.rand.NextVector2CircularEdge(7.2f, 7.2f);
                    if (BossRushEvent.BossRushActive)
                        toothVelocity *= 2.4f;

                    Utilities.NewProjectileBetter(npc.Center + toothVelocity * 3f, toothVelocity, ModContent.ProjectileType<SlowerSandTooth>(), 115, 0f);
                    attackTimer = 0f;
                }
            }

            return false;
        }
    }
}
