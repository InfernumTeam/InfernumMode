using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class AquaticParasite2 : ModNPC
    {
        public Player Target => Main.player[npc.target];
        public ref float Time => ref npc.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Aquatic Parasite");

        public override void SetDefaults()
        {
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.damage = 120;
            npc.width = 28;
            npc.height = 28;
            npc.defense = 5;
            npc.lifeMax = 900;
            npc.aiStyle = aiType = -1;
            npc.value = 0;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale) => npc.lifeMax = 900;

        public override void AI()
        {
            npc.TargetClosest();
            float speedFactor = BossRushEvent.BossRushActive ? 1.7f : 1f;

            if (Time > 420f)
                npc.velocity *= 0.985f;
            else if (!npc.WithinRange(Target.Center, 170f))
            {
                Vector2 destinationOffset = (MathHelper.TwoPi * Time / 24f + npc.whoAmI * 1.156f).ToRotationVector2() * 40f;
                npc.velocity = (npc.velocity * 70f + npc.SafeDirectionTo(Target.Center + destinationOffset) * speedFactor * 13f) / 71f;
                if (!npc.WithinRange(Target.Center, 275f))
                    npc.velocity = (npc.velocity * 31f + npc.SafeDirectionTo(Target.Center + destinationOffset) * speedFactor * 14f) / 32f;
            }

            npc.rotation += npc.velocity.Length() * Math.Sign(npc.velocity.X) * 0.02f;

            if (Time >= 600f)
            {
                npc.life = 0;
                npc.HitEffect(0, 10.0);
                npc.checkDead();
                npc.active = false;
                return;
            }

            Time++;
        }

        public override bool CheckDead()
        {
            Main.PlaySound(SoundID.Item14, npc.position);

            for (int i = 0; i < 15; i++)
            {
                Dust blood = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Blood, 0f, 0f, 100, default, 2f);
                blood.velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    blood.scale = 0.5f;
                    blood.fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
                blood.noGravity = true;
            }

            for (int i = 0; i < 15; i++)
            {
                Dust acid = Dust.NewDustDirect(npc.position, npc.width, npc.height, (int)CalamityDusts.SulfurousSeaAcid, 0f, 0f, 100, default, 3f);
                acid.noGravity = true;
                acid.velocity *= 5f;

                acid = Dust.NewDustDirect(npc.position, npc.width, npc.height, (int)CalamityDusts.SulfurousSeaAcid, 0f, 0f, 100, default, 2f);
                acid.velocity *= 2f;
                acid.noGravity = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int k = 0; k < 8; k++)
                {
                    Vector2 velocity = (MathHelper.TwoPi * k / 8f + Main.rand.NextFloat(-0.3f, 0.3f)).ToRotationVector2() * Main.rand.NextFloat(4f, 7f);
                    Utilities.NewProjectileBetter(npc.Center, velocity, ModContent.ProjectileType<SandBlast>(), 110, 0f, Main.myPlayer, 0f, 0f);
                }
                Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<SandPoisonCloud>(), 115, 0f, Main.myPlayer, 0f, 0f);
            }

            return true;
        }

        public override void FindFrame(int frameHeight)
        {
            npc.frameCounter += 0.15f;
            npc.frameCounter %= Main.npcFrameCount[npc.type];
            int frame = (int)npc.frameCounter;
            npc.frame.Y = frame * frameHeight;
        }

        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            player.AddBuff(ModContent.BuffType<Irradiated>(), 120, true);
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 3; k++)
            {
                Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);
            }
            if (npc.life <= 0)
            {
                for (int k = 0; k < 15; k++)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);
                }
                Gore.NewGore(npc.Center, npc.velocity, InfernumMode.CalamityMod.GetGoreSlot("Gores/AquaticScourgeGores/AquaticParasite1"), 1f);
                Gore.NewGore(npc.Center, npc.velocity, InfernumMode.CalamityMod.GetGoreSlot("Gores/AquaticScourgeGores/AquaticParasite2"), 1f);
            }
        }
    }
}
