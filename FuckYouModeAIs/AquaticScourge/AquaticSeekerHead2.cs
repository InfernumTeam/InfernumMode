using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.AquaticScourge
{
    public class AquaticSeekerHead2 : ModNPC
    {
        public bool TailSpawned = false;
        public const int MinLength = 3;
        public const int MaxLength = 4;
        public const int Lifetime = 2670;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Aquatic Seeker");

        public override void SetDefaults()
        {
            npc.damage = 125;
            npc.width = 22;
            npc.height = 28;
            npc.defense = 5;
            npc.lifeMax = Lifetime;
            npc.aiStyle = aiType = -1;
            npc.knockBackResist = 0f;
            npc.value = 0;
            npc.behindTiles = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.netAlways = true;
        }

        public override void AI()
        {
            if (npc.ai[2] > 0f)
                npc.realLife = (int)npc.ai[2];

            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead)
                npc.TargetClosest(true);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (!TailSpawned && npc.ai[0] == 0f)
                {
                    int Previous = npc.whoAmI;
                    for (int i = 0; i < MaxLength; i++)
                    {
                        int lol;
                        if (i >= 0 && i < MinLength)
                            lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AquaticSeekerBody2>(), npc.whoAmI);
                        else
                            lol = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AquaticSeekerTail2>(), npc.whoAmI);

                        Main.npc[lol].realLife = npc.whoAmI;
                        Main.npc[lol].ai[2] = npc.whoAmI;
                        Main.npc[lol].ai[1] = Previous;
                        Main.npc[Previous].ai[0] = lol;
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, lol, 0f, 0f, 0f, 0);
                        Previous = lol;
                    }
                    TailSpawned = true;
                }
            }

            npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
            if (Main.player[npc.target].dead)
                npc.TargetClosest(false);

            npc.alpha = Utils.Clamp(npc.alpha - 20, 0, 255);

            if (!npc.WithinRange(Main.player[npc.target].Center, 5600f) || !NPC.AnyNPCs(ModContent.NPCType<AquaticSeekerTail2>()))
            {
                npc.active = false;
                npc.netUpdate = true;
            }

            if (npc.WithinRange(Main.player[npc.target].Center, 250f))
            {
                if (npc.velocity.Length() < 18f)
                    npc.velocity *= 1.024f;
            }
            else
                npc.velocity = (npc.velocity * 31f + npc.SafeDirectionTo(Main.player[npc.target].Center) * 10f) / 32f;

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale) => npc.lifeMax = Lifetime;

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 3; k++)
            {
                Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);
            }
            if (npc.life <= 0)
            {
                for (int k = 0; k < 10; k++)
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);

                Gore.NewGore(npc.position, npc.velocity, InfernumMode.CalamityMod.GetGoreSlot("Gores/AquaticScourgeGores/AquaticSeekerHead"), 1f);
            }
        }

        public override bool CheckActive()
        {
            if (npc.timeLeft <= 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int k = (int)npc.ai[0]; k > 0; k = (int)Main.npc[k].ai[0])
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
