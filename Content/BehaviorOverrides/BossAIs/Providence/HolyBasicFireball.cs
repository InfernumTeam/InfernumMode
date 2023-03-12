using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Common.Graphics.Particles;
using CalamityMod.NPCs.Providence;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;
using System.IO;
using CalamityMod.NPCs;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class HolyBasicFireball : ModProjectile
    {
        public static int Variant => (int)(ProvidenceBehaviorOverride.IsEnraged ? ProvidenceBoss.BossMode.Night : ProvidenceBoss.BossMode.Day);

        public override string Texture => "CalamityMod/Projectiles/StarProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Fireball");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.scale = 0f;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Projectile.timeLeft);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = reader.ReadInt32();

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.45f, 0.35f, 0f);

            if (Projectile.ai[1] == 1f && CalamityGlobalNPC.holyBoss != -1 && Projectile.WithinRange(Main.npc[CalamityGlobalNPC.holyBoss].Center, Projectile.velocity.Length() * 1.96f + 28f))
                Projectile.Kill();

            // Release fire particles.
            for (int i = 0; i < 3; i++)
            {
                Color fireColor = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.2f, 0.4f));
                if (ProvidenceBehaviorOverride.IsEnraged)
                    fireColor = Color.Lerp(fireColor, Color.SkyBlue, 0.7f);

                fireColor = Color.Lerp(fireColor, Color.White, Main.rand.NextFloat(0.4f));
                float angularVelocity = Main.rand.NextFloat(0.035f, 0.08f);
                FireballParticle fire = new(Projectile.Center, Projectile.velocity * 0.6f, fireColor, 10, Main.rand.NextFloat(0.52f, 0.68f) * Projectile.scale, 1f, true, Main.rand.NextBool().ToDirectionInt() * angularVelocity);
                GeneralParticleHandler.SpawnParticle(fire);
            }

            // Make the fire grow in size.
            Projectile.scale = MathHelper.Clamp(Projectile.scale + 0.067f, 0f, 1.2f);
            Vector2 newScale = Vector2.One * Projectile.scale * 36f;
            if (Projectile.Size != newScale)
                Projectile.Size = newScale;

            if (Projectile.velocity.Length() < 16f)
                Projectile.velocity *= 1.01f;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float scaleInterpolant = Utils.GetLerpValue(15f, 30f, Projectile.timeLeft, true) * Utils.GetLerpValue(240f, 200f, Projectile.timeLeft, true) * (1f + 0.1f * (float)Math.Cos(Main.GlobalTimeWrappedHourly % 30f / 0.5f * (MathHelper.Pi * 2f) * 3f)) * 0.225f;

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Color baseColor = ProvUtils.GetProjectileColor(Variant, 255);
            baseColor.A = 0;
            Color colorA = baseColor;
            Color colorB = baseColor * 0.5f;
            colorA *= scaleInterpolant;
            colorB *= scaleInterpolant;
            Vector2 origin = texture.Size() / 2f;
            Vector2 scale = new Vector2(0.5f, 2f) * Projectile.scale * scaleInterpolant;

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            float upRight = Projectile.rotation + MathHelper.PiOver4;
            float up = Projectile.rotation + MathHelper.PiOver2;
            float upLeft = Projectile.rotation + 3f * MathHelper.PiOver4;
            float left = Projectile.rotation + MathHelper.Pi;
            Main.EntitySpriteDraw(texture, drawPos, null, colorA, upLeft, origin, scale, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorA, upRight, origin, scale, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorB, upLeft, origin, scale * 0.6f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorB, upRight, origin, scale * 0.6f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorA, up, origin, scale * 0.6f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorA, left, origin, scale * 0.6f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorB, up, origin, scale * 0.36f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorB, left, origin, scale * 0.36f, spriteEffects, 0);

            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            Projectile.ExpandHitboxBy(50);
            int dustType = ProvUtils.GetDustID(Variant);
            if (ProvidenceBehaviorOverride.IsEnraged)
                dustType = 187;

            for (int d = 0; d < 5; d++)
            {
                int holy = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, 2f);
                Main.dust[holy].velocity *= 3f;
                Main.dust[holy].noGravity = true;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[holy].scale = 0.5f;
                    Main.dust[holy].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int d = 0; d < 8; d++)
            {
                int fire = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, 3f);
                Main.dust[fire].noGravity = true;
                Main.dust[fire].velocity *= 5f;
                fire = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, 2f);
                Main.dust[fire].velocity *= 2f;
                Main.dust[fire].noGravity = true;
            }
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            // If the player is dodging, don't apply debuffs.
            if (damage <= 0 || target.creativeGodMode)
                return;

            ProvUtils.ApplyHitEffects(target, Variant, 180, 0);
            Projectile.Kill();
        }
    }
}
