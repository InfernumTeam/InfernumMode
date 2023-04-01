using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EyeOfCthulhu
{
    public class ExplodingServant : ModNPC
    {
        public ref float Timer => ref NPC.ai[0];

        public override string Texture => $"Terraria/Images/NPC_{NPCID.ServantofCthulhu}";

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("Servant of Cthulhu");
            Main.npcFrameCount[NPC.type] = 2;
        }

        public override void SetDefaults()
        {
            NPC.noGravity = true;
            NPC.lavaImmune = true;
            NPC.noTileCollide = true;
            NPC.damage = 40;
            NPC.height = NPC.width = 28;
            NPC.defense = 2;
            NPC.lifeMax = BossRushEvent.BossRushActive ? 6500 : 24;
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.alpha = 255;
            NPC.value = Item.buyPrice(0, 0, 0, 0);
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.15f;
            NPC.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            NPC.TargetClosest();
            NPC.spriteDirection = (NPC.direction > 0).ToDirectionInt();
            NPC.noGravity = true;
            if (NPC.direction == 0)
                NPC.TargetClosest(true);

            NPC.rotation = NPC.velocity.ToRotation();
            NPC.spriteDirection = (Math.Cos(NPC.rotation) > 0).ToDirectionInt();
            NPC.rotation -= MathHelper.PiOver2;

            NPC.alpha = Utils.Clamp(NPC.alpha - 30, 0, 255);

            float maxSpeed = BossRushEvent.BossRushActive ? 34f : 14f;
            float moveAcceleration = BossRushEvent.BossRushActive ? 1.04f : 1.007f;
            if (Main.dayTime && !BossRushEvent.BossRushActive)
            {
                maxSpeed *= 1.425f;
                moveAcceleration = 1.0145f;
            }

            if (NPC.velocity == Vector2.Zero || NPC.velocity.Length() < 0.01f)
                NPC.velocity = Main.rand.NextVector2Circular(1.7f, 1.7f);

            if (NPC.velocity.Length() < maxSpeed)
                NPC.velocity *= moveAcceleration;

            Player target = Main.player[Player.FindClosest(NPC.Center, 1, 1)];
            if (target != null && !target.dead && target.active)
            {
                float squareTargetDistance = NPC.DistanceSQ(target.Center);
                if (squareTargetDistance is > (180f * 180f) and < (1000f * 1000f))
                    NPC.velocity = NPC.velocity.RotateTowards(NPC.AngleTo(target.Center), 0.0145f);
            }

            Timer++;
            if (Collision.SolidCollision(NPC.Center, 30, 30) && Timer > 90f)
            {
                NPC.life = -1;
                NPC.HitEffect();
                NPC.active = false;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter += 0.125f;
            NPC.frameCounter %= Main.npcFrameCount[NPC.type];
            int frame = (int)NPC.frameCounter;
            NPC.frame.Y = frame * frameHeight;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 3; k++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);
            }
            if (Main.netMode != NetmodeID.Server && NPC.life <= 0)
            {
                for (int k = 0; k < 7; k++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);
                }
                Gore.NewGore(NPC.GetSource_FromAI(), NPC.position, NPC.velocity, 6);
                Gore.NewGore(NPC.GetSource_FromAI(), NPC.position, NPC.velocity, 7);
            }
        }
    }
}
