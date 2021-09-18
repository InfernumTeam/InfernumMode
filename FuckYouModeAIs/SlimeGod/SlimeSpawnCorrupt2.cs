using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.SlimeGod
{
    public class SlimeSpawnCorrupt2 : ModNPC
    {
        public ref float RedirectCountdown => ref npc.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Corrupt Slime Spawn");
            Main.npcFrameCount[npc.type] = 4;
        }

        public override void SetDefaults()
        {
            npc.aiStyle = aiType = -1;
            npc.damage = 67;
			npc.width = 40;
            npc.height = 30;
            npc.defense = 5;
            npc.lifeMax = 320;
            npc.knockBackResist = 0f;
            animationType = 121;
            npc.alpha = 35;
            npc.lavaImmune = true;
            npc.noGravity = false;
            npc.noTileCollide = true;
            npc.canGhostHeal = false;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.buffImmune[BuffID.OnFire] = true;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(npc.lifeMax);

        public override void ReceiveExtraAI(BinaryReader reader) => npc.lifeMax = reader.ReadInt32();

        public override void AI()
		{
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.slimeGodPurple))
            {
                npc.active = false;
                return;
            }

            NPC slimeGod = Main.npc[CalamityGlobalNPC.slimeGodPurple];
            float time = slimeGod.ai[1];

            if (time > 500f)
                npc.active = false;

            if (!npc.WithinRange(slimeGod.Center, Main.rand.NextFloat(380f, 520f)) || time > 420f)
                RedirectCountdown = 60f;

            if (RedirectCountdown > 0f && !npc.WithinRange(slimeGod.Center, 50f))
            {
                Vector2 destinationOffset = (MathHelper.TwoPi * npc.whoAmI / 13f).ToRotationVector2() * 12f;
                npc.velocity = (npc.velocity * 29f + npc.SafeDirectionTo(slimeGod.Center + destinationOffset) * 23f) / 30f;
                RedirectCountdown--;
            }

            npc.rotation = MathHelper.Clamp(npc.velocity.X * 0.05f, -0.2f, 0.2f);
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (projectile.penetrate > 1 || projectile.penetrate == -1)
                damage = (int)(damage * 0.35);
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 5; k++)
                Dust.NewDust(npc.position, npc.width, npc.height, 4, hitDirection, -1f, 0, default, 1f);

            if (npc.life <= 0)
            {
                for (int k = 0; k < 20; k++)
                    Dust.NewDust(npc.position, npc.width, npc.height, 4, hitDirection, -1f, 0, default, 1f);
            }
        }

        public override bool CheckDead()
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.slimeGodPurple))
                return base.CheckDead();

            Main.npc[CalamityGlobalNPC.slimeGodPurple].life -= npc.lifeMax;
            Main.npc[CalamityGlobalNPC.slimeGodPurple].HitEffect(0, npc.lifeMax);
            if (Main.npc[CalamityGlobalNPC.slimeGodPurple].life <= 0)
                Main.npc[CalamityGlobalNPC.slimeGodPurple].NPCLoot();

            return base.CheckDead();
        }

        public override bool PreNPCLoot() => false;

        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            player.AddBuff(BuffID.Weak, 90, true);
        }
    }
}
