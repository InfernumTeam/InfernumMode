using CalamityMod.Events;
using CalamityMod.Particles;
using InfernumMode.Common.Graphics.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Cryogen
{
    public class IcicleSpike : ModProjectile
    {
        public float InwardRadiusOffset
        {
            get;
            set;
        }

        public static float SpeedPower => BossRushEvent.BossRushActive ? 1.122f : 0.66f;

        public ref float Time => ref Projectile.localAI[0];

        public ref float OffsetRotation => ref Projectile.ai[0];

        public NPC Owner => Main.npc[(int)Projectile.ai[1]];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Icicle Spike");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
            Projectile.alpha = 255;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Time);
            writer.Write(InwardRadiusOffset);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Time = reader.ReadSingle();
            InwardRadiusOffset = reader.ReadSingle();
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange((int)Projectile.ai[1]) || !Owner.active)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.alpha > 0)
                Projectile.alpha -= 12;

            if (Time < 65f)
                OffsetRotation += MathHelper.TwoPi * 2f / 55f * Utils.GetLerpValue(30f, 60f, Time, true);
            if (Time == 80f)
                Projectile.velocity = Owner.SafeDirectionTo(Projectile.Center) * SpeedPower * 9f;
            if (Time > 80f && Projectile.velocity.Length() < SpeedPower * 33f)
                Projectile.velocity *= 1f + SpeedPower * 0.03f;

            if (Time <= 80f)
                Projectile.Center = Owner.Center + OffsetRotation.ToRotationVector2() * (MathHelper.Lerp(110f, 72f, SpeedPower) - InwardRadiusOffset);
            else if (Time % 10 == 0)
            {
                // Leave a trail of particles.
                Particle iceParticle = new SnowyIceParticle(Projectile.Center, Projectile.velocity * 0.5f, Color.White, Main.rand.NextFloat(0.75f, 0.95f), 30);
                GeneralParticleHandler.SpawnParticle(iceParticle);
            }

            Projectile.rotation = Time > 80f ? Projectile.velocity.ToRotation() : Owner.AngleTo(Projectile.Center);
            Projectile.rotation -= MathHelper.PiOver2;

            Time++;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.alpha < 20;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

            // Draw backglow effects.
            for (int i = 0; i < 12; i++)
            {
                Vector2 afterimageOffset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 4f;
                Color afterimageColor = new Color(46, 188, 234, 0f) * 0.2f * Projectile.Opacity;
                Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + afterimageOffset, null, Projectile.GetAlpha(afterimageColor), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, 1, 0, 0);
            return false;
        }
    }
}
