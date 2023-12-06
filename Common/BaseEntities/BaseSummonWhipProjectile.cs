using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Common.BaseEntities
{
    public abstract class BaseSummonWhipProjectile : ModProjectile
    {
        #region Fields/Properties
        public Asset<Texture2D> SegmentTexture
        {
            get;
            private set;
        }

        public Asset<Texture2D> TipTexture
        {
            get;
            private set;
        }

        public Player Owner => Main.player[Projectile.owner];

        public int SwingTime => Owner.itemAnimationMax * Projectile.MaxUpdates;

        public ref float Timer => ref Projectile.ai[0];
        #endregion

        #region Overrides
        public sealed override void SetStaticDefaults()
        {
            ProjectileID.Sets.IsAWhip[Type] = true;
        }

        public sealed override void SetDefaults()
        {
            Projectile.DefaultToWhip();

            SegmentTexture ??= ModContent.Request<Texture2D>("InfernumMode/Content/Projectiles/Summoner/" + Name + "_Segment", AssetRequestMode.ImmediateLoad);
            TipTexture ??= ModContent.Request<Texture2D>("InfernumMode/Content/Projectiles/Summoner/" + Name + "_Tip", AssetRequestMode.ImmediateLoad);

            ModifyDefaults();
        }

        public sealed override bool PreAI()
        {
            WhipAI();
            return false;
        }

        public sealed override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Inflict the target with the tag buff.
            target.buffImmune[TagBuffID] = false;

            if (!target.HasBuff(TagBuffID))
                target.AddBuff(TagBuffID, TagDuration);

            // Reduce the damage with each hit, for balancing against things such as worms. Ensure it stays above 1.
            int newDamage = (int)(Projectile.damage * HitDamageModifier);
            Projectile.damage = newDamage < 1 ? 1 : newDamage;

            // Set this target as the player's minion target.
            Owner.MinionAttackTargetNPC = target.whoAmI;

            // Perform any additional on hit effects.
            OnHitEffects(target, hit, damageDone);
        }

        public sealed override bool PreDraw(ref Color lightColor)
        {
            List<Vector2> points = new();
            Projectile.FillWhipControlPoints(Projectile, points);

            DrawConnectionLine(points);
            SpriteEffects direction = Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;

            Vector2 worldPosition = points[0];
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 position = points[i];
                Vector2 fromNextPosition = points[i + 1] - position;

                float scale = 1f;
                float rotation = fromNextPosition.ToRotation();

                if (i == 0)
                    DrawHandle(worldPosition - Main.screenPosition, rotation, scale, direction);
                else if (i == points.Count - 2)
                {
                    Projectile.GetWhipSettings(Projectile, out var timeToFlyOut, out _, out _);
                    float interpolant = Timer / timeToFlyOut;

                    // Scale the tip related to its extension for impact.
                    scale = Lerp(0.5f, 1.5f, Utils.GetLerpValue(0.1f, 0.7f, interpolant, true) * Utils.GetLerpValue(0.9f, 0.7f, interpolant, true));

                    DrawTip(worldPosition - Main.screenPosition, rotation, scale, direction);
                }
                else
                    DrawSegment(worldPosition - Main.screenPosition, rotation, scale, direction);

                worldPosition += fromNextPosition;
            }

            return false;
        }
        #endregion

        #region Abstracts/Virtuals
        public abstract int TagBuffID { get; }

        public virtual int TagDuration => 240;

        public virtual float HitDamageModifier => 0.8f;

        public virtual SoundStyle? CrackSound => SoundID.Item153;

        public virtual Color LineColor => Color.White;

        public virtual bool UseLightingForLine => true;

        public virtual void ModifyDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.WhipSettings.Segments = 30;
            Projectile.WhipSettings.RangeMultiplier = 1.5f;
        }

        public virtual void WhipAI()
        {
            //// Set the center to the tip of the whip.
            ////Projectile.Center = Main.GetPlayerArmPosition(Projectile) + Projectile.velocity * Timer;//WhipPoints[^1];
            ////Projectile.spriteDirection = Sign(Projectile.velocity.X);
            ////Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;

            //// Handle playing the whip sound, if it exists.
            //bool pastCrack = Timer >= SwingTime * 0.5f;
            ////if (CrackSound != null && pastCrack)
            ////    SoundEngine.PlaySound(CrackSound, WhipPoints[^2]);

            //WhipVFX(pastCrack);

            //// Die when appropriate.
            ////if (Timer >= SwingTime || Owner.itemAnimation <= 0)
            ////    Projectile.Kill();
            ///
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2; // Without PiOver2, the rotation would be off by 90 degrees counterclockwise.

            Projectile.Center = Main.GetPlayerArmPosition(Projectile) + Projectile.velocity * Timer;
            // Vanilla uses Vector2.Dot(Projectile.velocity, Vector2.UnitX) here. Dot Product returns the difference between two vectors, 0 meaning they are perpendicular.
            // However, the use of UnitX basically turns it into a more complicated way of checking if the projectile's velocity is above or equal to zero on the X axis.
            Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;


            Timer++;

            float swingTime = Owner.itemAnimationMax * Projectile.MaxUpdates;
            if (Timer >= swingTime || Owner.itemAnimation <= 0)
            {
                Projectile.Kill();
                return;
            }

            Owner.heldProj = Projectile.whoAmI;
            if (Timer == swingTime / 2)
            {
                // Plays a whipcrack sound at the tip of the whip.
                List<Vector2> points = Projectile.WhipPointsForCollision;
                Projectile.FillWhipControlPoints(Projectile, points);
                SoundEngine.PlaySound(SoundID.Item153, points[^1]);
            }
            WhipVFX(Timer >= swingTime / 2);
        }

        /// <summary>
        /// Perform visual effects in here.
        /// </summary>
        /// <param name="pastCrack">Whether the whip has reached the climax of the swing</param>
        public virtual void WhipVFX(bool pastCrack)
        {

        }

        /// <summary>
        /// Perform any extra on hit effects here.
        /// </summary>
        /// <param name="target">The target that was hit</param>
        /// <param name="hit">The information about the hit</param>
        /// <param name="damageDone">The damage dealt to the target</param>
        public virtual void OnHitEffects(NPC target, NPC.HitInfo hit, int damageDone)
        {

        }

        /// <summary>
        /// By default just calls the drawing method. Use this to apply shaders etc.
        /// </summary>
        public virtual void DrawConnectionLine(List<Vector2> points) => DrawLine(points);

        /// <summary>
        /// By default, draws the handle of the whip with the provided parameters
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        /// <param name="direction"></param>
        public virtual void DrawHandle(Vector2 position, float rotation, float scale, SpriteEffects direction)
        {
            Texture2D handle = TextureAssets.Projectile[Type].Value;
            Vector2 origin = handle.Size() * 0.5f;
            Main.spriteBatch.Draw(handle, position, null, Lighting.GetColor((position + Main.screenPosition).ToTileCoordinates()), rotation, origin, scale, direction, 0f);
        }

        /// <summary>
        /// By default, draws a segment of the whip with the provided parameters.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        /// <param name="direction"></param>
        public virtual void DrawSegment(Vector2 position, float rotation, float scale, SpriteEffects direction)
        {
            Vector2 origin = SegmentTexture.Size() * 0.5f;
            Main.spriteBatch.Draw(SegmentTexture.Value, position, null, Lighting.GetColor((position + Main.screenPosition).ToTileCoordinates()), rotation, origin, scale, direction, 0f);
        }

        /// <summary>
        /// By default, draws the tip of the whip with the provided parameters.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        /// <param name="direction"></param>
        public virtual void DrawTip(Vector2 position, float rotation, float scale, SpriteEffects direction)
        {
            Vector2 origin = TipTexture.Size() * 0.5f;
            Main.spriteBatch.Draw(TipTexture.Value, position, null, Lighting.GetColor((position + Main.screenPosition).ToTileCoordinates()), rotation, origin, scale, direction, 0f);
        }
        #endregion

        #region Methods
        public void DrawLine(List<Vector2> points)
        {
            Texture2D texture = TextureAssets.FishingLine.Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = new(frame.Width / 2, 2);

            Vector2 pos = points[0];
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 element = points[i];
                Vector2 diff = points[i + 1] - element;

                float rotation = diff.ToRotation() - PiOver2;
                Color color = Lighting.GetColor(element.ToTileCoordinates(), LineColor);
                Vector2 scale = new(0.5f, (diff.Length() + 2) / frame.Height);

                Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, SpriteEffects.None, 0);

                pos += diff;
            }
        }
        #endregion
    }
}
