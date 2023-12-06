using System.Collections.Generic;
using InfernumMode.Common.BaseEntities;
using InfernumMode.Common.Graphics.Metaballs;
using InfernumMode.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Summoner
{
    public class PerditusProjectile : BaseSummonWhipProjectile
    {
        public override int TagBuffID => ModContent.BuffType<PerditusTagBuff>();

        public override float HitDamageModifier => 0.9f;

        public override Color LineColor => Color.Aqua;

        public ref float HitTarget => ref Projectile.ai[2];

        public override void ModifyDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.WhipSettings.Segments = 25;
            Projectile.WhipSettings.RangeMultiplier = 1f;
            Projectile.extraUpdates = 1;
        }

        public override void OnHitEffects(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (HitTarget > 0)
                return;

            HitTarget = 1;
            SoundEngine.PlaySound(SoundID.SplashWeak with { PitchVariance = 0.5f, Volume = 1.75f });

            Vector2 position = Main.rand.NextVector2FromRectangle(Utils.CenteredRectangle(target.Hitbox.Center.ToVector2(), target.Hitbox.Size() * 0.5f)) + target.velocity * 6f;
            for (int i = 0; i < 50; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(0f, 4f);
                velocity.Y -= 3f;
                float npcScaleModifier = Lerp(1f, 1.75f, Utils.GetLerpValue(720, 10000f, target.Hitbox.Width * target.Hitbox.Height, true));
                Vector2 size = new Vector2(Main.rand.NextFloat(0.9f, 1.1f), Main.rand.NextFloat(0.9f, 1.1f)) * Main.rand.NextFloat(26f, 35f) * npcScaleModifier;
                ModContent.GetInstance<WaterMetaball>().SpawnParticle(position, velocity, size, Main.rand.NextFloat(0.94f, 0.95f));
            }

            for (int i = 0; i < 10; i++)
            {
                Gore bubble = Gore.NewGorePerfect(Projectile.GetSource_FromAI(), position, Main.rand.NextVector2Unit() * Main.rand.NextFloat(0f, 4f), 411);
                bubble.timeLeft = 8 + Main.rand.Next(6);
                bubble.scale = Main.rand.NextFloat(0.7f, 1f);
                bubble.type = Main.rand.NextBool(3) ? 412 : 411;
            }
        }

        public override void WhipVFX(bool pastCrack)
        {
            if (pastCrack)
            {
                List<Vector2> points = new();
                Projectile.FillWhipControlPoints(Projectile, points);
                Vector2 position = points[^2];

                for (int i = 0; i < 2; i++)
                {
                    Gore bubble = Gore.NewGorePerfect(Projectile.GetSource_FromAI(), position, Main.rand.NextVector2Circular(1f, 1f) * Projectile.velocity.Length() * 0.2f, 411);
                    bubble.timeLeft = 4 + Main.rand.Next(6);
                    bubble.scale = Main.rand.NextFloat(0.4f, 0.6f);
                    bubble.type = Main.rand.NextBool(3) ? 412 : 411;
                }
            }
        }

        public override void DrawConnectionLine(List<Vector2> points)
        {
            // Do nothing, as the line is drawn by the water metaball.
        }

        public static void DrawWaterLine(List<Vector2> points)
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
                Color color = Lighting.GetColor(element.ToTileCoordinates(), Color.White);
                Vector2 scale = new(1.2f, (diff.Length() + 2) / frame.Height);

                Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, SpriteEffects.None, 0);

                pos += diff;
            }
        }
    }
}
