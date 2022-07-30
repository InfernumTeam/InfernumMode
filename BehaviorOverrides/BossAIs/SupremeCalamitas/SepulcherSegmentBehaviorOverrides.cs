using CalamityMod;
using CalamityMod.NPCs.SupremeCalamitas;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SepulcherBody1BehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SepulcherBody>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCSetDefaults;

        public override void SetDefaults(NPC npc) => DoSetDefaults(npc);

        public static void DoSetDefaults(NPC npc)
        {
            npc.damage = 0;
            npc.npcSlots = 5f;
            npc.width = npc.height = 48;
            if (npc.type == ModContent.NPCType<SepulcherBodyEnergyBall>())
                npc.width = npc.height = 20;

            npc.defense = 0;
            npc.lifeMax = 312000;
            npc.aiStyle = -1;
            npc.knockBackResist = 0f;
            npc.scale *= 1.2f;
            npc.alpha = 255;
            npc.behindTiles = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.canGhostHeal = false;
            npc.chaseable = true;
            npc.netAlways = true;
            npc.dontCountMe = true;
            npc.HitSound = SoundID.DD2_SkeletonHurt with { Volume = 0.925f };
            npc.Calamity().DR = 0.56f;
        }

        public static bool DoAI(NPC npc)
        {
            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            if (!aheadSegment.active || npc.realLife <= -1)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    return false;

                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                npc.netUpdate = true;
            }
            
            // Inherit various attributes from the ahead segment.
            // This code will go upstream across every segment, until it reaches the head.
            npc.Opacity = aheadSegment.Opacity;
            npc.chaseable = true;
            npc.friendly = false;
            npc.dontTakeDamage = aheadSegment.dontTakeDamage;
            npc.damage = npc.dontTakeDamage ? 0 : npc.defDamage;
            npc.defense = 20;
            
            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.04f);

            directionToNextSegment = directionToNextSegment.SafeNormalize(Vector2.Zero);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment * npc.width * npc.scale * 0.725f;
            npc.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();
            return false;
        }
    }

    public class SepulcherBody2BehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SepulcherBodyEnergyBall>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCSetDefaults;

        public override void SetDefaults(NPC npc) => SepulcherBody1BehaviorOverride.DoSetDefaults(npc);
    }

    public class SepulcherTailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SepulcherTail>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCSetDefaults;

        public override void SetDefaults(NPC npc) => SepulcherBody1BehaviorOverride.DoSetDefaults(npc);
    }
}