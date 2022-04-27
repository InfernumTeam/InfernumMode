using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.Signus
{
	public class EldritchScythe : ModProjectile
    {
        public ref float ShootCountdown => ref Projectile.ai[0];
        public ref float AngularOffset => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Scythe");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 420;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            CooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.signus == -1)
            {
                Projectile.active = false;
                return;
            }

            NPC signus = Main.npc[CalamityGlobalNPC.signus];
            Projectile.rotation += 0.5f * Projectile.direction;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.085f, 0f, 0.9f);

            if (ShootCountdown > 0f)
            {
                ShootCountdown--;

                if (ShootCountdown == 0f)
                {
                    SoundEngine.PlaySound(SoundID.Item73, Projectile.position);

                    Projectile.velocity = (Projectile.Center - signus.Center).SafeNormalize(Vector2.UnitY) * 34.5f;
                    Projectile.netUpdate = true;
                }
                else
                {
                    Projectile.Center = signus.Center + AngularOffset.ToRotationVector2() * 86f;
                    Projectile.velocity = Vector2.Zero;
                }
            }
            else if (Projectile.velocity.Length() < 44f)
                Projectile.velocity *= 1.01f;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            if (Projectile.timeLeft > 515)
            {
                Projectile.localAI[1] += 1f;
                byte b2 = (byte)(((int)Projectile.localAI[1]) * 3);
                byte a2 = (byte)(Projectile.alpha * (b2 / 255f));
                return new Color(b2, b2, b2, a2);
            }
            return new Color(255, 255, 255, Projectile.alpha);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (ShootCountdown > 0f)
            {
                Vector2 start = Projectile.Center;
                Vector2 end = Projectile.Center + AngularOffset.ToRotationVector2() * 5200f;
                float width = Projectile.Opacity * 2.75f + 0.25f;
                Main.spriteBatch.DrawLineBetter(start, end, Color.Fuchsia * Projectile.Opacity, width);
            }
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<WhisperingDeath>(), 300);
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item74, Projectile.position);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
