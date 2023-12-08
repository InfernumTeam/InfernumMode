using InfernumMode.Core.GlobalInstances;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;

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
        /// <param name="desiredTypes">The projectile type to check for.</param>
        public static IEnumerable<Projectile> AllProjectilesByID(params int[] desiredTypes)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && desiredTypes.Contains(Main.projectile[i].type))
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

                Projectile p = Main.projectile[i];

                // Make the projectile fade away instead of dying if it's marked for such behavior.
                if (p.Infernum().FadesAwayWhenManuallyKilled)
                {
                    p.Infernum().FadeAwayTimer = GlobalProjectileOverrides.FadeAwayTime;
                    p.netSpam = 0;
                    p.netUpdate = true;
                    continue;
                }

                if (setToInactive)
                {
                    p.active = false;
                    p.netUpdate = true;
                }
                else
                    p.Kill();
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
        /// <param name="ai2">An optional <see cref="NPC.ai"/>[2] fill value. Defaults to 0.</param>
        public static int NewProjectileBetter(float spawnX, float spawnY, float velocityX, float velocityY, int type, int damage, float knockback, int owner = -1, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f)
        {
            if (owner == -1)
                owner = Main.myPlayer;
            damage = (int)(damage * 0.5);
            if (Main.expertMode)
                damage = (int)(damage * 0.5);
            int index = Projectile.NewProjectile(new EntitySource_WorldEvent(), spawnX, spawnY, velocityX, velocityY, type, damage, knockback, owner, ai0, ai1, ai2);
            if (index >= 0 && index < Main.maxProjectiles)
                Main.projectile[index].netUpdate = true;

            return index;
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
        /// <param name="ai2">An optional <see cref="NPC.ai"/>[2] fill value. Defaults to 0.</param>
        public static int NewProjectileBetter(Vector2 center, Vector2 velocity, int type, int damage, float knockback, int owner = -1, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f)
        {
            return NewProjectileBetter(center.X, center.Y, velocity.X, velocity.Y, type, damage, knockback, owner, ai0, ai1, ai2);
        }

        public static void DrawBackglow(Projectile projectile, Color backglowColor, float backglowArea, Rectangle? frame = null)
        {
            Texture2D texture = TextureAssets.Projectile[projectile.type].Value;

            // Use a fallback for the frame.
            frame ??= texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);

            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Value.Size() * 0.5f;
            Color backAfterimageColor = backglowColor * projectile.Opacity;
            SpriteEffects direction = projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            for (int i = 0; i < 10; i++)
            {
                Vector2 drawOffset = (TwoPi * i / 10f).ToRotationVector2() * backglowArea;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, frame, backAfterimageColor, projectile.rotation, origin, projectile.scale, direction, 0f);
            }
        }

        public static void DrawProjectileWithBackglowTemp(this Projectile projectile, Color backglowColor, Color lightColor, float backglowArea, Rectangle? frame = null, Texture2D texture = null)
        {
            texture ??= TextureAssets.Projectile[projectile.type].Value;

            // Use a fallback for the frame.
            frame ??= texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);

            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Value.Size() * 0.5f;

            DrawBackglow(projectile, backglowColor, backglowArea, frame);
            SpriteEffects direction = projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(texture, drawPosition, frame, projectile.GetAlpha(lightColor), projectile.rotation, origin, projectile.scale, direction, 0f);
        }

        public static Projectile FindProjectileByIdentity(int identity, int ownerIndex)
        {
            // If in singleplayer, simply return the projectile at the designated index, as singleplayer will never have mismatching indices.
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                return identity <= (-1) || identity >= Main.maxProjectiles ? null : Main.projectile[identity];
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].identity != identity || Main.projectile[i].owner != ownerIndex || !Main.projectile[i].active)
                    continue;

                return Main.projectile[i];
            }
            return null;
        }
    }
}
