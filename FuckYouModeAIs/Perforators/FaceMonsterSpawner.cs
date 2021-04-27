using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Perforators
{
    public class FaceMonsterSpawner : ModProjectile
    {
        internal ref float Time => ref projectile.ai[0];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Spawner");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 4;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.netImportant = true;
            projectile.timeLeft = 120;
        }

        public override void AI()
        {
            // Create dust visuals to indicate movement unground.
            // The monster will rise out of the ground.
            if (Main.dedServ)
                return;

            for (int i = 0; i < 2; i++)
            {
                Dust blood = Dust.NewDustPerfect(projectile.Top, DustID.Blood);
                blood.velocity = -Vector2.UnitY.RotatedByRandom(0.87f) * Main.rand.NextFloat(1.8f, 3f);
                blood.scale = Main.rand.NextFloat(1.05f, 1.3f);
                blood.noGravity = Main.rand.NextBool(3);
                blood.fadeIn = 0.3f;
            }
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int faceMonster = NPC.NewNPC((int)projectile.Center.X, (int)projectile.Center.Y, ModContent.NPCType<FaceMonster>());
            if (Main.npc.IndexInRange(faceMonster))
                Main.npc[faceMonster].Bottom = projectile.Bottom + Vector2.UnitY * 50f;
        }
    }
}
