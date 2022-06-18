using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.QueenBee
{
    public class HornetHive : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Hive");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 32;
            projectile.ignoreWater = true;
            projectile.timeLeft = 240;
            projectile.scale = 1f;
            projectile.alpha = 255;
            projectile.tileCollide = true;
            projectile.friendly = false;
            projectile.hostile = true;
        }

        public override void AI()
        {
            projectile.alpha = Utils.Clamp(projectile.alpha - 20, 0, 255);
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
            projectile.velocity.Y += 0.15f;
            if (projectile.wet)
                projectile.Kill();
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(BuffID.Poisoned, 120);

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.NPCDeath1, projectile.Center);
            for (int i = 0; i < 30; i++)
            {
                Dust honey = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 147, 0f, 0f, 0, default, 1f);
                if (Main.rand.NextBool(2))
                    honey.scale *= 1.4f;

                projectile.velocity *= 1.9f;
            }

            // Don't spawn things client-side.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int hornetType = Utils.SelectRandom(Main.rand, NPCID.HornetFatty, NPCID.HornetHoney, NPCID.HornetStingy);
            int hornet = NPC.NewNPC((int)projectile.Center.X, (int)projectile.Center.Y, hornetType, 1, 0f, 0f, 0f, 0f, 255);
            Main.npc[hornet].velocity = Main.rand.NextVector2CircularEdge(2.5f, 2.5f);
        }
    }
}
