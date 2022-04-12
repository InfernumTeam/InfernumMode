using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class AquaticSeekerHead2 : ModNPC
    {
        public bool TailSpawned = false;
        public const int MinLength = 11;
        public const int MaxLength = 12;
        public const int TotalLife = 2670;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Aquatic Seeker");

        public override void SetDefaults()
        {
            NPC.damage = 95;
            NPC.width = 22;
            NPC.height = 28;
            NPC.defense = 5;
            NPC.lifeMax = TotalLife;
            NPC.aiStyle = AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.value = 0;
            NPC.behindTiles = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.Calamity().canBreakPlayerDefense = true;
            NPC.netAlways = true;
        }

        public override void AI()
        {
            if (NPC.ai[2] > 0f)
                NPC.realLife = (int)NPC.ai[2];

            if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead)
                NPC.TargetClosest(true);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (!TailSpawned && NPC.ai[0] == 0f)
                {
                    int Previous = NPC.whoAmI;
                    for (int i = 0; i < MaxLength; i++)
                    {
                        int lol;
                        if (i is >= 0 and < MinLength)
                            lol = NPC.NewNPC(new InfernumSource(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<AquaticSeekerBody2>(), NPC.whoAmI);
                        else
                            lol = NPC.NewNPC(new InfernumSource(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<AquaticSeekerTail2>(), NPC.whoAmI);

                        Main.npc[lol].realLife = NPC.whoAmI;
                        Main.npc[lol].ai[2] = NPC.whoAmI;
                        Main.npc[lol].ai[1] = Previous;
                        Main.npc[Previous].ai[0] = lol;
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, lol, 0f, 0f, 0f, 0);
                        Previous = lol;
                    }
                    TailSpawned = true;
                }
            }

            NPC.spriteDirection = (NPC.velocity.X < 0f).ToDirectionInt();
            if (Main.player[NPC.target].dead)
                NPC.TargetClosest(false);

            NPC.alpha = Utils.Clamp(NPC.alpha - 20, 0, 255);

            if (!NPC.WithinRange(Main.player[NPC.target].Center, 5600f) || !NPC.AnyNPCs(ModContent.NPCType<AquaticSeekerTail2>()))
            {
                NPC.active = false;
                NPC.netUpdate = true;
            }

            float flySpeed = BossRushEvent.BossRushActive ? 20f : 12f;
            if (NPC.WithinRange(Main.player[NPC.target].Center, 280f))
            {
                if (NPC.velocity.Length() < 18f)
                    NPC.velocity *= 1.024f;
            }
            else
                NPC.velocity = (NPC.velocity * 31f + NPC.SafeDirectionTo(Main.player[NPC.target].Center) * flySpeed) / 32f;

            NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale) => NPC.lifeMax = TotalLife;

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 3; k++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);
            }
            if (NPC.life <= 0)
            {
                for (int k = 0; k < 10; k++)
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);

                Gore.NewGore(NPC.position, NPC.velocity, Utilities.GetGoreID("AquaticSeekerHead", InfernumMode.CalamityMod), 1f);
            }
        }

        public override bool CheckActive()
        {
            if (NPC.timeLeft <= 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int k = (int)NPC.ai[0]; k > 0; k = (int)Main.npc[k].ai[0])
                {
                    if (Main.npc[k].active)
                    {
                        Main.npc[k].active = false;
                        if (Main.netMode == NetmodeID.Server)
                        {
                            Main.npc[k].life = 0;
                            Main.npc[k].netSkip = -1;
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, k, 0f, 0f, 0f, 0, 0, 0);
                        }
                    }
                }
            }
            return true;
        }

        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            player.AddBuff(ModContent.BuffType<Irradiated>(), 120, true);
        }
    }
}
