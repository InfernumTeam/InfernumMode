using CalamityMod;
using CalamityMod.NPCs.DesertScourge;
using InfernumMode.Assets.BossTextures;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.DesertScourge
{
    public class DesertScourgeBodyBigBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DesertScourgeBody>();

        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 104;
            npc.height = 104;
            npc.scale = 1f;
            npc.Opacity = 1f;
            npc.defense = 6;
            npc.DR_NERD(0.05f);
            npc.alpha = 255;
        }

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

            npc.target = aheadSegment.target;
            if (aheadSegment.alpha < 128)
                npc.alpha = Utils.Clamp(npc.alpha - 42, 0, 255);

            npc.defense = aheadSegment.defense;
            npc.dontTakeDamage = aheadSegment.dontTakeDamage;


            if (npc.ai[3] > 0f)
            {
                switch ((int)npc.ai[3])
                {
                    default:
                        break;

                    case 10:

                        npc.ai[3] = 1f;

                        npc.position = npc.Center;
                        npc.position -= npc.Size * 0.5f;
                        npc.frame = new Rectangle(0, 0, DesertScourgeBody.BodyTexture2 is null ? 0 : DesertScourgeBody.BodyTexture2.Width(), DesertScourgeBody.BodyTexture2 is null ? 0 : DesertScourgeBody.BodyTexture2.Height());

                        npc.netUpdate = true;

                        // Prevent netUpdate from being blocked by the spam counter.
                        npc.netSpam = 0;

                        break;

                    case 20:

                        npc.ai[3] = 2f;

                        npc.position = npc.Center;
                        npc.position -= npc.Size * 0.5f;
                        npc.frame = new Rectangle(0, 0, DesertScourgeBody.BodyTexture3 is null ? 0 : DesertScourgeBody.BodyTexture3.Width(), DesertScourgeBody.BodyTexture3 is null ? 0 : DesertScourgeBody.BodyTexture3.Height());

                        npc.netUpdate = true;

                        // Prevent netUpdate from being blocked by the spam counter.
                        npc.netSpam = 0;

                        break;

                    case 30:

                        npc.ai[3] = 3f;

                        npc.position = npc.Center;
                        npc.position -= npc.Size * 0.5f;
                        npc.frame = new Rectangle(0, 0, DesertScourgeBody.BodyTexture4 is null ? 0 : DesertScourgeBody.BodyTexture4.Width(), DesertScourgeBody.BodyTexture4 is null ? 0 : DesertScourgeBody.BodyTexture4.Height());

                        npc.netUpdate = true;

                        // Prevent netUpdate from being blocked by the spam counter.
                        npc.netSpam = 0;

                        break;
                }
            }
            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
            {
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.08f);
                directionToNextSegment = directionToNextSegment.MoveTowards((aheadSegment.rotation - npc.rotation).ToRotationVector2(), 1f);
            }
            int segmentOffset = 70;
            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.scale * segmentOffset;
            npc.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();
            /*
            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(WrapAngle(aheadSegment.rotation - npc.rotation) * 0.075f);

            npc.rotation = directionToNextSegment.ToRotation() + PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;
            */
            return false;
        }
    }
}
