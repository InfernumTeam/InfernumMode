using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs;
using InfernumMode.FuckYouModeAIs.BoC;
using InfernumMode.InverseKinematics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

namespace InfernumMode.FuckYouModeAIs.Perforators
{
	public class Crimera : ModNPC
    {
        public Player Target => Main.player[npc.target];
        public bool InPhase2 => Main.npc[CalamityGlobalNPC.perfHive].ai[2] == 2f;
        public ref float Time => ref npc.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crimera");
            Main.npcFrameCount[npc.type] = 2;
        }

        public override void SetDefaults()
        {
            npc.width = 44;
            npc.height = 78;
            npc.aiStyle = -1;
            npc.damage = 24;
            npc.defense = 2;
            npc.lifeMax = 115;
            npc.HitSound = SoundID.NPCHit27;
            npc.DeathSound = SoundID.NPCDeath26;
            npc.knockBackResist = 0.2f;
            npc.value = 0f;
            npc.Opacity = 0f;
            npc.noGravity = true;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = false;
        }

		public override void AI()
        {
            if (npc.target < 0 || npc.target >= Main.maxPlayers || Main.player[npc.target].dead)
                npc.TargetClosest();

            // Disappear if the main boss is not present.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.perfHive) || !npc.WithinRange(Target.Center, 1700f))
            {
                npc.active = false;
                npc.netUpdate = true;
                Utils.PoofOfSmoke(npc.Center);
                return;
            }

            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.085f, 0f, 1f);

            if (Target.dead)
			{
                Vector2 idealVelocity = new Vector2(Math.Sign(npc.velocity.X) * 5f, -10f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.075f);
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                return;
            }

            if (Time % 300f > 240f)
            {
                npc.velocity *= 0.97f;
                if (Main.netMode != NetmodeID.MultiplayerClient && Time % 300f == 280f)
				{
                    for (int i = 0; i < 3; i++)
					{
                        Vector2 shootVelocity = npc.SafeDirectionTo(Target.Center).RotatedBy(i / 2f) * 5f;
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<BloodGeyser2>(), 65, 0f);
					}
				}
            }
            else
            {
                Vector2 destination = Target.Center - Vector2.UnitY * 230f;
                destination.X += (float)Math.Cos(MathHelper.TwoPi * Time / 210f) * 310f;

                if (MathHelper.Distance(destination.Y, npc.Center.Y) > 90f || npc.velocity.Length() < 4f)
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 9f, 0.1f);
            }

            npc.rotation = npc.AngleTo(Target.Center) - MathHelper.PiOver2;
            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color _)
        {
            if (InPhase2)
                PerforatorHiveAIClass.DrawEnragedEffectOnEnemy(spriteBatch, npc);

            return true;
        }

		public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.Bleeding, 240);
            target.AddBuff(ModContent.BuffType<BurningBlood>(), 90);
        }

		public override void HitEffect(int hitDirection, double damage)
        {
            if (npc.life > 0)
                return;

            for (int i = 0; i < 10; i++)
            {
                Dust blood = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(30f, 30f), DustID.Blood);
                blood.velocity = Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 2f;
                blood.noGravity = Main.rand.NextBool(3);
                blood.scale = Main.rand.NextFloat(1f, 1.35f);
            }

            Gore.NewGore(npc.position, npc.velocity, 223, npc.scale);
            Gore.NewGore(npc.position, npc.velocity, 224, npc.scale);
        }

		public override bool CheckActive() => false;
    }
}
