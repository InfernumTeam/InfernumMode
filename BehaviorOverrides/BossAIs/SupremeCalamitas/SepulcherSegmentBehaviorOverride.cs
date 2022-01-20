using CalamityMod;
using CalamityMod.NPCs.SupremeCalamitas;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SepulcherBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SCalWormBody>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCSetDefaults;

        public override void SetDefaults(NPC npc)
        {
            npc.damage = 450;
            npc.npcSlots = 5f;
            npc.width = npc.height = 24;
            npc.defense = 0;
            npc.lifeMax = 166400;
            npc.aiStyle = npc.modNPC.aiType = -1;
            npc.knockBackResist = 0f;
            npc.scale = 1.3f;
            npc.alpha = 255;
            for (int k = 0; k < npc.buffImmune.Length; k++)
                npc.buffImmune[k] = true;

            npc.behindTiles = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.HitSound = SoundID.DD2_SkeletonHurt;
            npc.DeathSound = SoundID.NPCDeath52;
            npc.netAlways = true;
        }

        public static void SegmentBehavior(NPC npc)
        {
            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            if (!aheadSegment.active)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                npc.netUpdate = true;
            }

            // Inherit various attributes from the head segment.
            // This code will go upstream across every segment, until it reaches the head.
            npc.scale = aheadSegment.scale;
            npc.Opacity = aheadSegment.Opacity;
            npc.chaseable = aheadSegment.chaseable;
            npc.canGhostHeal = aheadSegment.canGhostHeal;
            npc.friendly = false;
            npc.dontTakeDamage = aheadSegment.dontTakeDamage;
            npc.damage = npc.dontTakeDamage ? 0 : npc.defDamage;
            npc.Calamity().DR = npc.type == ModContent.NPCType<SCalWormBody>() ? 0.99999f : 0.2f;
            if (npc.Calamity().DR > 0.99f)
                npc.Calamity().unbreakableDR = true;

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.03f);

            directionToNextSegment = directionToNextSegment.SafeNormalize(Vector2.Zero);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment * npc.scale * 60f;
            npc.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();
        }

        public override bool PreAI(NPC npc)
        {
            SegmentBehavior(npc);
            return false;
        }
    }
    public class SepulcherTailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SCalWormTail>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCSetDefaults;

		public override void SetDefaults(NPC npc)
        {
            npc.damage = 0;
            npc.npcSlots = 5f;
            npc.width = npc.height = 24;
            npc.defense = 0;
            npc.lifeMax = 166400;
            npc.aiStyle = npc.modNPC.aiType = -1;
            npc.knockBackResist = 0f;
            npc.scale = 1.3f;
            npc.alpha = 255;
            for (int k = 0; k < npc.buffImmune.Length; k++)
                npc.buffImmune[k] = true;

            npc.behindTiles = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.HitSound = SoundID.DD2_SkeletonHurt;
            npc.DeathSound = SoundID.NPCDeath52;
            npc.netAlways = true;
        }

		public override bool PreAI(NPC npc)
        {
            SepulcherBodyBehaviorOverride.SegmentBehavior(npc);
            return base.PreAI(npc);
		}
	}
}
