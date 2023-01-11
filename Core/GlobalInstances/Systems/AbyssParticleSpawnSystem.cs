using InfernumMode.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Systems
{
    public class AbyssParticleSpawnSystem : ModSystem
    {
        public override void PreUpdatePlayers()
        {
            if (!WorldSaveSystem.InPostAEWUpdateWorld)
                return;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead && Main.player[i].Infernum().InLayer3HadalZone)
                {
                    int tries = 0;
                    do
                    {
                        tries++;

                        Vector2 particleSpawnPosition = Main.player[i].Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(200f, 600f);
                        if (Collision.SolidCollision(particleSpawnPosition, 1, 1))
                            continue;

                        Utilities.NewProjectileBetter(particleSpawnPosition, Main.rand.NextVector2Circular(2f, 2f) - Vector2.UnitY, ModContent.ProjectileType<AbyssParticle>(), 0, 0f);
                        break;
                    }
                    while (tries < 100);
                }
            }
        }
    }
}