using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.Projectiles.Enemy;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.SlimeGod
{
    public class SlimeSpawnCrimson3 : ModNPC
    {
        public float spikeTimer = 60f;
        public ref float JumpTimer => ref npc.ai[0];
        public ref float NoTileCollisionCountdown => ref npc.ai[1];
        public Player Target => Main.player[npc.target];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crimson Slime Spawn");
            Main.npcFrameCount[npc.type] = 2;
        }

        public override void SetDefaults()
        {
            npc.aiStyle = -1;
            aiType = -1;
            npc.damage = 50;
			npc.width = 40;
            npc.height = 30;
            npc.defense = 6;
            npc.lifeMax = 130;
            npc.knockBackResist = 0f;
            animationType = NPCID.CorruptSlime;
            npc.alpha = 55;
            npc.lavaImmune = false;
            npc.noGravity = false;
            npc.noTileCollide = false;
            npc.canGhostHeal = false;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.slimeGodRed))
            {
                npc.active = false;
                return;
            }

            NPC slimeGod = Main.npc[CalamityGlobalNPC.slimeGodRed];
            float time = slimeGod.ai[1];

            npc.target = slimeGod.target;
            npc.noTileCollide = !Collision.SolidCollision(npc.position, npc.width, npc.height + 16) && npc.Bottom.Y < slimeGod.Center.Y;
            if (NoTileCollisionCountdown > 0f)
            {
                npc.noTileCollide = true;
                NoTileCollisionCountdown--;
            }

            if (time > 500f)
                npc.active = false;

            if (spikeTimer > 0f)
                spikeTimer--;

            int type = ModContent.ProjectileType<CrimsonSpike>();
            if (!npc.wet && npc.velocity.Y == 0f)
            {
                npc.velocity.X *= 0.9f;

                if (npc.WithinRange(Target.Center, 750f) && Math.Abs(Target.Center.Y - npc.Center.Y) > 250f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient && spikeTimer == 0f)
                    {
                        for (int n = 0; n < 2; n++)
                        {
                            Vector2 shootVelocity = new Vector2(n - 2f, -4f).SafeNormalize(Vector2.UnitY) * 26f;
                            Utilities.NewProjectileBetter(npc.Center, shootVelocity, type, 95, 0f, Main.myPlayer);
                            spikeTimer = 125f;
                        }
                    }
                }
            }

            JumpTimer++;
            if (npc.velocity.Y == 0f && JumpTimer >= Main.rand.NextFloat(20f, 28f) && time < 450f)
            {
                JumpTimer = 0f;
                NoTileCollisionCountdown = 10f;

                npc.velocity.Y -= 4.5f;
                if (slimeGod.position.Y + slimeGod.height < npc.Center.Y)
                    npc.velocity.Y -= 1f;
                if (slimeGod.position.Y + slimeGod.height < npc.Center.Y - 40f)
                    npc.velocity.Y -= 1f;
                if (slimeGod.position.Y + slimeGod.height < npc.Center.Y - 80f)
                    npc.velocity.Y -= 1.15f;
                if (slimeGod.position.Y + slimeGod.height < npc.Center.Y - 120f)
                    npc.velocity.Y -= 1.5f;
                if (slimeGod.position.Y + slimeGod.height < npc.Center.Y - 160f)
                    npc.velocity.Y -= 2f;
                if (slimeGod.position.Y + slimeGod.height < npc.Center.Y - 200f)
                    npc.velocity.Y -= 3f;
                if (slimeGod.position.Y + slimeGod.height < npc.Center.Y - 400f)
                    npc.velocity.Y -= 4f;
                if (slimeGod.position.Y + slimeGod.height < npc.Center.Y - 520f)
                    npc.velocity.Y -= 4f;
                if (!Collision.CanHit(npc.Center, 1, 1, slimeGod.Center, 1, 1))
                    npc.velocity.Y -= 4f;

                npc.velocity.X = (slimeGod.Center.X > npc.Center.X).ToDirectionInt() * 9f;
                npc.netUpdate = true;
            }

            if (time > 450f)
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(slimeGod.Center) * 20f, 0.125f);
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
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.slimeGodRed))
                return base.CheckDead();

            Main.npc[CalamityGlobalNPC.slimeGodRed].life -= npc.lifeMax;
            Main.npc[CalamityGlobalNPC.slimeGodRed].HitEffect(0, npc.lifeMax);
            if (Main.npc[CalamityGlobalNPC.slimeGodRed].life <= 0)
                Main.npc[CalamityGlobalNPC.slimeGodRed].NPCLoot();

            return base.CheckDead();
        }

        public override bool PreNPCLoot() => false;

        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
			player.AddBuff(BuffID.Darkness, 90, true);
		}
    }
}
