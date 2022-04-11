using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class AquaticParasite2 : ModNPC
    {
        public Player Target => Main.player[NPC.target];
        public ref float Time => ref NPC.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Aquatic Parasite");

        public override void SetDefaults()
        {
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.damage = 120;
            NPC.width = 28;
            NPC.height = 28;
            NPC.defense = 5;
            NPC.lifeMax = 900;
            NPC.aiStyle = aiType = -1;
            NPC.value = 0;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale) => NPC.lifeMax = 900;

        public override void AI()
        {
            NPC.TargetClosest();
            float speedFactor = BossRushEvent.BossRushActive ? 1.7f : 1f;

            if (Time > 420f)
                NPC.velocity *= 0.985f;
            else if (!NPC.WithinRange(Target.Center, 170f))
            {
                Vector2 destinationOffset = (MathHelper.TwoPi * Time / 24f + NPC.whoAmI * 1.156f).ToRotationVector2() * 40f;
                NPC.velocity = (NPC.velocity * 70f + NPC.SafeDirectionTo(Target.Center + destinationOffset) * speedFactor * 13f) / 71f;
                if (!NPC.WithinRange(Target.Center, 275f))
                    NPC.velocity = (NPC.velocity * 31f + NPC.SafeDirectionTo(Target.Center + destinationOffset) * speedFactor * 14f) / 32f;
            }

            NPC.rotation += NPC.velocity.Length() * Math.Sign(NPC.velocity.X) * 0.02f;

            if (Time >= 600f)
            {
                NPC.life = 0;
                NPC.HitEffect(0, 10.0);
                NPC.checkDead();
                NPC.active = false;
                return;
            }

            Time++;
        }

        public override bool CheckDead()
        {
            SoundEngine.PlaySound(SoundID.Item14, NPC.position);

            for (int i = 0; i < 15; i++)
            {
                Dust blood = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Blood, 0f, 0f, 100, default, 2f);
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
                Dust acid = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, (int)CalamityDusts.SulfurousSeaAcid, 0f, 0f, 100, default, 3f);
                acid.noGravity = true;
                acid.velocity *= 5f;

                acid = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, (int)CalamityDusts.SulfurousSeaAcid, 0f, 0f, 100, default, 2f);
                acid.velocity *= 2f;
                acid.noGravity = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int k = 0; k < 8; k++)
                {
                    Vector2 velocity = (MathHelper.TwoPi * k / 8f + Main.rand.NextFloat(-0.3f, 0.3f)).ToRotationVector2() * Main.rand.NextFloat(4f, 7f);
                    Utilities.NewProjectileBetter(NPC.Center, velocity, ModContent.ProjectileType<SandBlast>(), 110, 0f, Main.myPlayer, 0f, 0f);
                }
                Utilities.NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<SandPoisonCloud>(), 115, 0f, Main.myPlayer, 0f, 0f);
            }

            return true;
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter += 0.15f;
            NPC.frameCounter %= Main.npcFrameCount[NPC.type];
            int frame = (int)NPC.frameCounter;
            NPC.frame.Y = frame * frameHeight;
        }

        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            player.AddBuff(ModContent.BuffType<Irradiated>(), 120, true);
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 3; k++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);
            }
            if (NPC.life <= 0)
            {
                for (int k = 0; k < 15; k++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);
                }
                Gore.NewGore(NPC.Center, NPC.velocity, Utilities.GetGoreID("AquaticParasite1", InfernumMode.CalamityMod), 1f);
                Gore.NewGore(NPC.Center, NPC.velocity, Utilities.GetGoreID("AquaticParasite2", InfernumMode.CalamityMod), 1f);
            }
        }
    }
}
