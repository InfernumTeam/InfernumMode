using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using static InfernumMode.BehaviorOverrides.BossAIs.Destroyer.DestroyerHeadBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.BossAIs.Destroyer
{
	public class DestroyerBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.TheDestroyerBody;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            if (!aheadSegment.active)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                npc.netUpdate = true;
            }

            npc.Calamity().DR = 0.2f;

            // Inherit various attributes from the head segment.
            // This code will go upstream across every segment, until it reaches the head.
            npc.scale = aheadSegment.scale;
            npc.Opacity = aheadSegment.Opacity;

            npc.dontTakeDamage = aheadSegment.dontTakeDamage;
            npc.damage = npc.dontTakeDamage ? 0 : npc.defDamage;

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.03f);

            directionToNextSegment = directionToNextSegment.SafeNormalize(Vector2.Zero);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment * npc.width * npc.scale * 0.725f;
            npc.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();

            float segmentNumber = npc.localAI[0];

            if (!Main.npc.IndexInRange(npc.realLife) || !Main.npc[npc.realLife].active)
            {
                npc.active = false;
                return false;
            }

            float headAttackTimer = Main.npc[npc.realLife].ai[2];
            DestroyerAttackType headAttackType = (DestroyerAttackType)(int)Main.npc[npc.realLife].ai[1];

            if (headAttackType == DestroyerAttackType.LaserBarrage)
            {
                bool isMovingHorizontally = Math.Abs(Vector2.Dot(directionToNextSegment, Vector2.UnitX)) > 0.95f && headAttackTimer >= 230f;
                if (Main.netMode != NetmodeID.MultiplayerClient && isMovingHorizontally && headAttackTimer % BodySegmentCount == segmentNumber && npc.whoAmI % 3 == 0)
                {
                    for (int i = -1; i <= 1; i += 2)
                    {
                        int laser = Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY * i * 7f, ProjectileID.DeathLaser, 90, 0f);
                        Main.projectile[laser].timeLeft = 250;
                        Main.projectile[laser].tileCollide = false;
                    }
                }
            }
            return false;
        }
    }
}