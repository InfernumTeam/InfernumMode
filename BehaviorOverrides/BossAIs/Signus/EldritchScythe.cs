using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Signus
{
    public class EldritchScythe : ModProjectile
    {
        public ref float ShootCountdown => ref projectile.ai[0];
        public ref float AngularOffset => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Scythe");
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 26;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 420;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            cooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(projectile.localAI[0]);
            writer.Write(projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            projectile.localAI[0] = reader.ReadSingle();
            projectile.localAI[1] = reader.ReadSingle();
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.signus == -1)
            {
                projectile.active = false;
                return;
            }

            NPC signus = Main.npc[CalamityGlobalNPC.signus];
            projectile.rotation += 0.5f * projectile.direction;
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.085f, 0f, 0.9f);

            if (ShootCountdown > 0f)
            {
                ShootCountdown--;

                if (ShootCountdown == 0f)
                {
                    Main.PlaySound(SoundID.Item73, projectile.position);

                    projectile.velocity = (projectile.Center - signus.Center).SafeNormalize(Vector2.UnitY) * 34.5f;
                    projectile.netUpdate = true;
                }
                else
                {
                    projectile.Center = signus.Center + AngularOffset.ToRotationVector2() * 86f;
                    projectile.velocity = Vector2.Zero;
                }
            }
            else if (projectile.velocity.Length() < 44f)
                projectile.velocity *= 1.01f;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            if (projectile.timeLeft > 515)
            {
                projectile.localAI[1] += 1f;
                byte b2 = (byte)(((int)projectile.localAI[1]) * 3);
                byte a2 = (byte)(projectile.alpha * (b2 / 255f));
                return new Color(b2, b2, b2, a2);
            }
            return new Color(255, 255, 255, projectile.alpha);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (ShootCountdown > 0f)
            {
                Vector2 start = projectile.Center;
                Vector2 end = projectile.Center + AngularOffset.ToRotationVector2() * 5200f;
                float width = projectile.Opacity * 2.75f + 0.25f;
                spriteBatch.DrawLineBetter(start, end, Color.Fuchsia * projectile.Opacity, width);
            }
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<WhisperingDeath>(), 300);
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item74, projectile.position);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
