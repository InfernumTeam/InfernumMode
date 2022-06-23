using CalamityMod.Events;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SlimeGod
{
    public class SlimeSpawnCorrupt2 : ModNPC
    {
        public ref float RedirectCountdown => ref NPC.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Corrupt Slime Spawn");
            Main.npcFrameCount[NPC.type] = 4;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = AIType = -1;
            NPC.damage = 67;
            NPC.width = 40;
            NPC.height = 30;
            NPC.defense = 11;
            NPC.lifeMax = 320;
            NPC.knockBackResist = 0f;
            AnimationType = 121;
            NPC.alpha = 35;
            NPC.lavaImmune = true;
            NPC.noGravity = false;
            NPC.noTileCollide = true;
            NPC.canGhostHeal = false;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.buffImmune[BuffID.OnFire] = true;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(NPC.lifeMax);

        public override void ReceiveExtraAI(BinaryReader reader) => NPC.lifeMax = reader.ReadInt32();

        public override void AI()
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.slimeGodPurple))
            {
                NPC.active = false;
                return;
            }

            NPC slimeGod = Main.npc[CalamityGlobalNPC.slimeGodPurple];
            float time = slimeGod.ai[1];

            if (time > 500f)
                NPC.active = false;

            if (!NPC.WithinRange(slimeGod.Center, Main.rand.NextFloat(380f, 520f)) || time > 420f)
                RedirectCountdown = 60f;

            if (RedirectCountdown > 0f && !NPC.WithinRange(slimeGod.Center, 50f))
            {
                float flySpeed = BossRushEvent.BossRushActive ? 38f : 20.75f;
                Vector2 destinationOffset = (MathHelper.TwoPi * NPC.whoAmI / 13f).ToRotationVector2() * 12f;
                NPC.velocity = (NPC.velocity * 34f + NPC.SafeDirectionTo(slimeGod.Center + destinationOffset) * flySpeed) / 35f;
                RedirectCountdown--;
            }

            NPC.rotation = MathHelper.Clamp(NPC.velocity.X * 0.05f, -0.2f, 0.2f);
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (projectile.penetrate > 1 || projectile.penetrate == -1)
                damage = (int)(damage * 0.1);
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 5; k++)
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 4, hitDirection, -1f, 0, default, 1f);

            if (NPC.life <= 0)
            {
                for (int k = 0; k < 20; k++)
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 4, hitDirection, -1f, 0, default, 1f);
            }
        }

        public override bool CheckDead()
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.slimeGodPurple))
                return base.CheckDead();

            Main.npc[CalamityGlobalNPC.slimeGodPurple].life -= NPC.lifeMax;
            Main.npc[CalamityGlobalNPC.slimeGodPurple].HitEffect(0, NPC.lifeMax);
            if (Main.npc[CalamityGlobalNPC.slimeGodPurple].life <= 0)
                Main.npc[CalamityGlobalNPC.slimeGodPurple].NPCLoot();

            return base.CheckDead();
        }

        public override bool PreKill() => false;

        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            player.AddBuff(BuffID.Weak, 90, true);
        }
    }
}
