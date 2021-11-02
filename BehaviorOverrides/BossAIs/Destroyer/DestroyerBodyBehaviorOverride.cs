using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
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
            npc.chaseable = true;
            npc.friendly = false;
            npc.dontTakeDamage = aheadSegment.dontTakeDamage;
            npc.damage = npc.dontTakeDamage ? 0 : npc.defDamage;
            npc.Calamity().DR = 0.5f;
            npc.defense = 12;

            npc.buffImmune[ModContent.BuffType<CrushDepth>()] = true;

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.03f);

            directionToNextSegment = directionToNextSegment.SafeNormalize(Vector2.Zero);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment * npc.width * npc.scale * 0.725f;
            npc.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();

            if (!Main.npc.IndexInRange(npc.realLife) || !Main.npc[npc.realLife].active)
            {
                npc.active = false;
                return false;
            }

            float segmentNumber = npc.localAI[0];
            float headAttackTimer = Main.npc[npc.realLife].ai[2];
            Player target = Main.player[Main.npc[npc.realLife].target];
            if (Main.npc[npc.realLife].ai[1] == (int)DestroyerAttackType.EnergyBlasts && Main.npc[npc.realLife].Infernum().ExtraAI[0] == 2f && headAttackTimer - 45f == segmentNumber)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(target.Center) * 23.5f, ModContent.ProjectileType<EnergySpark2>(), 130, 0f);
            }
            return false;
        }
    }
}