using System.IO;
using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SlimeGod
{
    public class SplitBigSlimeAnimation : ModNPC
    {
        public static int OwnerIndex => CalamityGlobalNPC.slimeGod;

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            // DisplayName.SetDefault("Unstable Slime Spawn");
            Main.npcFrameCount[NPC.type] = 4;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = AIType = -1;
            NPC.damage = 0;
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
            NPC.dontTakeDamage = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.buffImmune[BuffID.OnFire] = true;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(NPC.lifeMax);

        public override void ReceiveExtraAI(BinaryReader reader) => NPC.lifeMax = reader.ReadInt32();

        public override void AI()
        {
            if (!Main.npc.IndexInRange(OwnerIndex))
            {
                NPC.active = false;
                return;
            }

            NPC slimeGod = Main.npc[OwnerIndex];
            float flySpeed = 14f;
            Vector2 destinationOffset = (TwoPi * NPC.whoAmI / 13f).ToRotationVector2() * 32f;
            NPC.velocity = (NPC.velocity * 41f + NPC.SafeDirectionTo(slimeGod.Center + destinationOffset) * flySpeed) / 42f;

            NPC.Opacity = Utils.Remap(NPC.Distance(slimeGod.Center), 240f, 80f, 1f, 0.1f);
            if (NPC.Opacity <= 0.1f)
                NPC.active = false;

            NPC.rotation = Clamp(NPC.velocity.X * 0.05f, -0.2f, 0.2f);
            NPC.spriteDirection = (NPC.velocity.X < 0f).ToDirectionInt();
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.penetrate is > 1 or (-1))
                modifiers.FinalDamage.Base *= 0.1f;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int k = 0; k < 5; k++)
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.TintableDust, hit.HitDirection, -1f, 0, default, 1f);

            if (NPC.life <= 0)
            {
                for (int k = 0; k < 20; k++)
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.TintableDust, hit.HitDirection, -1f, 0, default, 1f);
            }
        }

        public override bool CheckDead()
        {
            if (!Main.npc.IndexInRange(OwnerIndex))
                return base.CheckDead();

            Main.npc[OwnerIndex].life -= NPC.lifeMax;
            Main.npc[OwnerIndex].HitEffect(0, NPC.lifeMax);
            if (Main.npc[OwnerIndex].life <= 0)
                Main.npc[OwnerIndex].NPCLoot();

            return base.CheckDead();
        }

        public override bool PreKill() => false;
    }
}
