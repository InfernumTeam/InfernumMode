using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace InfernumMode
{
    public static partial class Utilities
    {
        /// <summary>
        /// Checks if any projectiles of a specific type are present.
        /// </summary>
        /// <param name="desiredType">The projectile type to check for.</param>
        public static bool AnyProjectiles(int desiredType)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == desiredType)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns all projectiles present of a specific type.
        /// </summary>
        /// <param name="desiredType">The projectile type to check for.</param>
        public static IEnumerable<Projectile> AllProjectilesByID(int desiredType)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == desiredType)
                    yield return Main.projectile[i];
            }
        }

        /// <summary>
        /// Deletes all projectiles of certain IDs.
        /// </summary>
        /// <param name="setToInactive">Whether to set the active bool to false directly instead of killing the projectile.</param>
        /// <param name="projectileIDs">The projectiles to kill.</param>
        public static void DeleteAllProjectiles(bool setToInactive, params int[] projectileIDs)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!Main.projectile[i].active || !projectileIDs.Contains(Main.projectile[i].type))
                    continue;

                if (setToInactive)
                    Main.projectile[i].active = false;
                else
                    Main.projectile[i].Kill();
            }
        }

        /// <summary>
        /// Summons a projectile of a specific type while also adjusting damage for vanilla spaghetti regarding hostile projectiles.
        /// </summary>
        /// <param name="spawnX">The x spawn position of the projectile.</param>
        /// <param name="spawnY">The y spawn position of the projectile.</param>
        /// <param name="velocityX">The x velocity of the projectile.</param>
        /// <param name="velocityY">The y velocity of the projectile</param>
        /// <param name="type">The id of the projectile type that should be spawned.</param>
        /// <param name="damage">The damage of the projectile.</param>
        /// <param name="knockback">The knockback of the projectile.</param>
        /// <param name="owner">The owner index of the projectile.</param>
        /// <param name="ai0">An optional <see cref="NPC.ai"/>[0] fill value. Defaults to 0.</param>
        /// <param name="ai1">An optional <see cref="NPC.ai"/>[1] fill value. Defaults to 0.</param>
        public static int NewProjectileBetter(float spawnX, float spawnY, float velocityX, float velocityY, int type, int damage, float knockback, int owner = -1, float ai0 = 0f, float ai1 = 0f)
        {
            if (owner == -1)
                owner = Main.myPlayer;
            damage = (int)(damage * 0.5);
            if (Main.expertMode)
                damage = (int)(damage * 0.5);
            return Projectile.NewProjectile(spawnX, spawnY, velocityX, velocityY, type, damage, knockback, owner, ai0, ai1);
        }

        /// <summary>
        /// Summons a projectile of a specific type while also adjusting damage for vanilla spaghetti regarding hostile projectiles.
        /// </summary>
        /// <param name="center">The spawn position of the projectile.</param>
        /// <param name="velocity">The velocity of the projectile</param>
        /// <param name="type">The id of the projectile type that should be spawned.</param>
        /// <param name="damage">The damage of the projectile.</param>
        /// <param name="knockback">The knockback of the projectile.</param>
        /// <param name="owner">The owner index of the projectile.</param>
        /// <param name="ai0">An optional <see cref="NPC.ai"/>[0] fill value. Defaults to 0.</param>
        /// <param name="ai1">An optional <see cref="NPC.ai"/>[1] fill value. Defaults to 0.</param>
        public static int NewProjectileBetter(Vector2 center, Vector2 velocity, int type, int damage, float knockback, int owner = -1, float ai0 = 0f, float ai1 = 0f)
        {
            return NewProjectileBetter(center.X, center.Y, velocity.X, velocity.Y, type, damage, knockback, owner, ai0, ai1);
        }
    }
}
