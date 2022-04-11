using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.QueenBee
{
    public class HornetHive : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Hive");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
            Projectile.scale = 1f;
            Projectile.alpha = 255;
            Projectile.tileCollide = true;
            Projectile.friendly = false;
            Projectile.hostile = true;
        }

        public override void AI()
        {
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 20, 0, 255);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Projectile.velocity.Y += 0.15f;
            if (Projectile.wet)
                Projectile.Kill();
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(BuffID.Poisoned, 120);

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.NPCDeath1, Projectile.Center);
            for (int i = 0; i < 30; i++)
            {
                Dust honey = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 147, 0f, 0f, 0, default, 1f);
                if (Main.rand.NextBool(2))
                    honey.scale *= 1.4f;

                Projectile.velocity *= 1.9f;
            }

            // Don't spawn things client-side.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int hornetType = Utils.SelectRandom(Main.rand, NPCID.HornetFatty, NPCID.HornetHoney, NPCID.HornetStingy);
            int hornet = NPC.NewNPC(new InfernumSource(), (int)Projectile.Center.X, (int)Projectile.Center.Y, hornetType, 1, 0f, 0f, 0f, 0f, 255);
            Main.npc[hornet].velocity = Main.rand.NextVector2CircularEdge(2.5f, 2.5f);
        }
    }
}
