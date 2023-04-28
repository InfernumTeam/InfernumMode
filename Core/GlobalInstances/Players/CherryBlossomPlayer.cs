using InfernumMode.Content.Projectiles.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class CherryBlossomPlayer : ModPlayer
    {
        public bool CreatingCherryBlossoms
        {
            get;
            set;
        }

        public override void ResetEffects()
        {
            CreatingCherryBlossoms = false;
        }

        public override void UpdateDead()
        {
            CreatingCherryBlossoms = false;
        }

        public override void PostUpdate()
        {
            // Create a bunch of blossoms.
            if (!CreatingCherryBlossoms || Main.myPlayer != Player.whoAmI || !Main.rand.NextBool(4) || Player.dead)
                return;

            Vector2 blossomSpawnPosition = Player.Center + new Vector2(Main.rand.NextFloatDirection() * 1000f, -600f);
            Vector2 blossomVelocity = Vector2.UnitY.RotatedByRandom(1.23f) * Main.rand.NextFloat(0.3f, 4f);
            Projectile.NewProjectile(Player.GetSource_FromThis(), blossomSpawnPosition, blossomVelocity, ModContent.ProjectileType<CherryBlossomPetal>(), 0, 0f, Player.whoAmI);
        }
    }
}
