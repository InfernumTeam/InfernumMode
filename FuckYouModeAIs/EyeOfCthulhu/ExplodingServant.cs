using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.EyeOfCthulhu
{
    public class ExplodingServant : ModNPC
    {
        public ref float Timer => ref npc.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Servant of Cthulhu");
            Main.npcFrameCount[npc.type] = 2;
        }

        public override void SetDefaults()
        {
            npc.noGravity = true;
            npc.lavaImmune = true;
            npc.damage = 40;
            npc.height = npc.width = 28;
            npc.defense = 2;
            npc.lifeMax = 40;
            npc.aiStyle = -1;
            aiType = -1;
            npc.alpha = 255;
            npc.value = Item.buyPrice(0, 0, 0, 0);
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.knockBackResist = 0f;
        }

        public override void AI()
        {
            npc.TargetClosest();
            npc.spriteDirection = (npc.direction > 0).ToDirectionInt();
            npc.noGravity = true;
            if (npc.direction == 0)
            {
                npc.TargetClosest(true);
            }
            npc.rotation = npc.velocity.ToRotation();
            npc.spriteDirection = (Math.Cos(npc.rotation) > 0).ToDirectionInt();
            npc.rotation -= MathHelper.PiOver2;

            npc.alpha = Utils.Clamp(npc.alpha - 30, 0, 255);

            if (npc.velocity.Length() < 14f)
                npc.velocity *= 1.0075f;

            Player target = Main.player[Player.FindClosest(npc.Center, 1, 1)];
            if (target != null && !target.dead && target.active)
			{
                float squareTargetDistance = npc.DistanceSQ(target.Center);
                if (squareTargetDistance > 180f * 180f && squareTargetDistance < 1000f * 1000f)
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.0145f);
			}

            npc.noTileCollide = Timer++ < 90;

            if (Collision.SolidCollision(npc.Center, 30, 30) && !npc.noTileCollide)
            {
                npc.life = -1;
                npc.HitEffect();
                npc.active = false;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            npc.frameCounter += 0.125f;
            npc.frameCounter %= Main.npcFrameCount[npc.type];
            int frame = (int)npc.frameCounter;
            npc.frame.Y = frame * frameHeight;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 3; k++)
            {
                Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);
            }
            if (npc.life <= 0)
            {
                for (int k = 0; k < 7; k++)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);
                }
                Gore.NewGore(npc.position, npc.velocity, 6);
                Gore.NewGore(npc.position, npc.velocity, 7);
            }
        }
    }
}
