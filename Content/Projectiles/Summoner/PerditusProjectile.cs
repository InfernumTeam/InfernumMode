using System.Collections.Generic;
using InfernumMode.Common.BaseEntities;
using InfernumMode.Common.Graphics.Metaballs;
using InfernumMode.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Summoner
{
    public class PerditusProjectile : BaseSummonWhipProjectile
    {
        public override int TagBuffID => ModContent.BuffType<PerditusTagBuff>();

        public override float HitDamageModifier => 0.9f;

        public override Color LineColor => Color.Aqua;


        public override void ModifyDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.WhipSettings.Segments = 30;
            Projectile.WhipSettings.RangeMultiplier = 1f;
            Projectile.extraUpdates = 1;
        }

        public override void OnHitEffects(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.SplashWeak with { PitchVariance = 0.2f });

            NPC.HitInfo info = new()
            {
                Damage = (int)(damageDone * 1.2f),
                Crit = true,
                HitDirection = hit.HitDirection,
                Knockback = hit.Knockback
            };

            target.StrikeNPC(info);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendStrikeNPC(target, info);

            for (int i = 0; i < 50; i++)
            {
                Vector2 position = Main.rand.NextVector2FromRectangle(target.Hitbox);
                Vector2 velocity = target.SafeDirectionTo(position).RotatedBy(Main.rand.NextFloat(-0.1f, 0.1f)) * Main.rand.NextFloat(1f, 3f);
                velocity.Y *= 1.5f;
                Vector2 size = new Vector2(Main.rand.NextFloat(0.9f, 1.1f), Main.rand.NextFloat(0.9f, 1.1f)) * Main.rand.NextFloat(15f, 25f);
                ModContent.GetInstance<WaterMetaball>().SpawnParticle(position, velocity, size, Main.rand.NextFloat(0.955f, 0.975f));
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
                    bubble.scale = Main.rand.NextFloat(0.3f, 0.6f);
                    bubble.type = Main.rand.NextBool(3) ? 412 : 411;
                }
            }
        }
    }
}
