using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.SlimeGod
{
    public class SlimeSpawnCorrupt2 : ModNPC
    {
        public Player Target => Main.player[npc.target];
        public ref float Time => ref npc.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Corrupt Slime Spawn");
            Main.npcFrameCount[npc.type] = 4;
        }

        public override void SetDefaults()
        {
            npc.aiStyle = aiType = -1;
            npc.damage = 115;
			npc.width = 40;
            npc.height = 30;
            npc.defense = 6;
            npc.lifeMax = 320;
            npc.knockBackResist = 0f;
            animationType = 121;
            npc.alpha = 55;
            npc.lavaImmune = false;
            npc.noGravity = false;
            npc.noTileCollide = true;
            npc.canGhostHeal = false;
            npc.dontTakeDamage = true;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.buffImmune[BuffID.OnFire] = true;
        }

		public override void AI()
		{
            npc.TargetClosest();

            if (Time < 70f)
                npc.velocity = -Vector2.UnitY * MathHelper.Lerp(1f, 6f, Time / 70f);
            else if (Time < 135f)
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(Target.Center) * -4.5f, 0.2f);
            else if (Time == 135f)
			{
                npc.velocity = npc.SafeDirectionTo(Target.Center) * 15f;
                npc.netUpdate = true;
			}

            if (Time > 135)
			{
                if (npc.velocity.Length() < 20.5f)
                    npc.velocity *= 1.02f;

                if (Time > 360f || Collision.SolidCollision(npc.position, npc.width, npc.height))
                    npc.StrikeNPCNoInteraction(9999, 0f, 0);
			}
            Time++;
		}

		public override bool PreNPCLoot() => false;

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 5; k++)
                Dust.NewDust(npc.position, npc.width, npc.height, 4, hitDirection, -1f, 0, default, 1f);
        }

        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            player.AddBuff(BuffID.Weak, 90, true);
        }
    }
}
