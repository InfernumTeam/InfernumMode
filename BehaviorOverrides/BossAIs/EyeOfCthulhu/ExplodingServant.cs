using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EyeOfCthulhu
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
            npc.noTileCollide = true;
            npc.damage = 48;
            npc.height = npc.width = 28;
            npc.defense = 2;
            npc.lifeMax = BossRushEvent.BossRushActive ? 6500 : 24;
            npc.aiStyle = -1;
            aiType = -1;
            npc.alpha = 255;
            npc.value = Item.buyPrice(0, 0, 0, 0);
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.knockBackResist = 0.15f;
            npc.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            npc.TargetClosest();
            npc.spriteDirection = (npc.direction > 0).ToDirectionInt();
            npc.noGravity = true;
            if (npc.direction == 0)
                npc.TargetClosest(true);

            npc.rotation = npc.velocity.ToRotation();
            npc.spriteDirection = (Math.Cos(npc.rotation) > 0).ToDirectionInt();
            npc.rotation -= MathHelper.PiOver2;

            npc.alpha = Utils.Clamp(npc.alpha - 30, 0, 255);

            float maxSpeed = BossRushEvent.BossRushActive ? 34f : 14f;
            float moveAcceleration = BossRushEvent.BossRushActive ? 1.04f : 1.0075f;
            if (Main.dayTime && !BossRushEvent.BossRushActive)
            {
                maxSpeed *= 1.425f;
                moveAcceleration = 1.0145f;
            }

            if (npc.velocity == Vector2.Zero || npc.velocity.Length() < 0.01f)
                npc.velocity = Main.rand.NextVector2Circular(1.7f, 1.7f);

            if (npc.velocity.Length() < maxSpeed)
                npc.velocity *= moveAcceleration;

            Player target = Main.player[Player.FindClosest(npc.Center, 1, 1)];
            if (target != null && !target.dead && target.active)
            {
                float squareTargetDistance = npc.DistanceSQ(target.Center);
                if (squareTargetDistance > 180f * 180f && squareTargetDistance < 1000f * 1000f)
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.0145f);
            }

            Timer++;
            if (Collision.SolidCollision(npc.Center, 30, 30) && Timer > 90f)
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
