using CalamityMod.NPCs;
using CalamityMod.Projectiles.Enemy;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SlimeGod
{
    public class SlimeSpawnCrimson3 : ModNPC
    {
        public float spikeTimer = 60f;
        public ref float JumpTimer => ref NPC.ai[0];
        public ref float NoTileCollisionCountdown => ref NPC.ai[1];
        public Player Target => Main.player[NPC.target];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crimson Slime Spawn");
            Main.npcFrameCount[NPC.type] = 2;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.damage = 50;
            NPC.width = 40;
            NPC.height = 30;
            NPC.defense = 12;
            NPC.lifeMax = 130;
            NPC.knockBackResist = 0f;
            AnimationType = NPCID.CorruptSlime;
            NPC.alpha = 55;
            NPC.lavaImmune = false;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.canGhostHeal = false;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.buffImmune[BuffID.OnFire] = true;
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.slimeGodRed))
            {
                NPC.active = false;
                return;
            }

            NPC slimeGod = Main.npc[CalamityGlobalNPC.slimeGodRed];
            float time = slimeGod.ai[1];

            NPC.target = slimeGod.target;
            NPC.noTileCollide = !Collision.SolidCollision(NPC.position, NPC.width, NPC.height + 16) && NPC.Bottom.Y < slimeGod.Center.Y;
            if (NoTileCollisionCountdown > 0f)
            {
                NPC.noTileCollide = true;
                NoTileCollisionCountdown--;
            }

            if (time > 500f)
                NPC.active = false;

            if (spikeTimer > 0f)
                spikeTimer--;

            int type = ModContent.ProjectileType<CrimsonSpike>();
            if (!NPC.wet && NPC.velocity.Y == 0f)
            {
                NPC.velocity.X *= 0.9f;

                if (NPC.WithinRange(Target.Center, 750f) && Math.Abs(Target.Center.Y - NPC.Center.Y) > 250f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient && spikeTimer == 0f)
                    {
                        for (int n = 0; n < 2; n++)
                        {
                            Vector2 shootVelocity = new Vector2(n - 2f, -4f).SafeNormalize(Vector2.UnitY) * 26f;
                            Utilities.NewProjectileBetter(NPC.Center, shootVelocity, type, 95, 0f, Main.myPlayer);
                            spikeTimer = 125f;
                        }
                    }
                }
            }

            JumpTimer++;
            if (NPC.velocity.Y == 0f && JumpTimer >= Main.rand.NextFloat(20f, 28f) && time < 450f)
            {
                JumpTimer = 0f;
                NoTileCollisionCountdown = 10f;

                NPC.velocity.Y -= 4.5f;
                if (slimeGod.position.Y + slimeGod.height < NPC.Center.Y)
                    NPC.velocity.Y -= 1f;
                if (slimeGod.position.Y + slimeGod.height < NPC.Center.Y - 40f)
                    NPC.velocity.Y -= 1f;
                if (slimeGod.position.Y + slimeGod.height < NPC.Center.Y - 80f)
                    NPC.velocity.Y -= 1.15f;
                if (slimeGod.position.Y + slimeGod.height < NPC.Center.Y - 120f)
                    NPC.velocity.Y -= 1.5f;
                if (slimeGod.position.Y + slimeGod.height < NPC.Center.Y - 160f)
                    NPC.velocity.Y -= 2f;
                if (slimeGod.position.Y + slimeGod.height < NPC.Center.Y - 200f)
                    NPC.velocity.Y -= 3f;
                if (slimeGod.position.Y + slimeGod.height < NPC.Center.Y - 400f)
                    NPC.velocity.Y -= 4f;
                if (slimeGod.position.Y + slimeGod.height < NPC.Center.Y - 520f)
                    NPC.velocity.Y -= 4f;
                if (!Collision.CanHit(NPC.Center, 1, 1, slimeGod.Center, 1, 1))
                    NPC.velocity.Y -= 4f;

                NPC.velocity.X = (slimeGod.Center.X > NPC.Center.X).ToDirectionInt() * 9f;
                NPC.netUpdate = true;
            }

            if (time > 450f)
                NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.SafeDirectionTo(slimeGod.Center) * 20f, 0.125f);
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (projectile.penetrate > 1 || projectile.penetrate == -1)
                damage = (int)(damage * 0.35);
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
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.slimeGodRed))
                return base.CheckDead();

            Main.npc[CalamityGlobalNPC.slimeGodRed].life -= NPC.lifeMax;
            Main.npc[CalamityGlobalNPC.slimeGodRed].HitEffect(0, NPC.lifeMax);
            if (Main.npc[CalamityGlobalNPC.slimeGodRed].life <= 0)
                Main.npc[CalamityGlobalNPC.slimeGodRed].NPCLoot();

            return base.CheckDead();
        }

        public override bool PreKill() => false;

        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            player.AddBuff(BuffID.Darkness, 90, true);
        }
    }
}
