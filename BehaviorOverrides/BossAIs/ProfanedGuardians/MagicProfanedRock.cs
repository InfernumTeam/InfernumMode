using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs.ProfanedGuardians;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class MagicProfanedRock : ModProjectile
    {
        public ref float GeneralTimer => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Profaned Rock");
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
        }

        public override void AI()
        {
            // Disappear if the defender guardian is gone.
            int defenderGuardianIndex = NPC.FindFirstNPC(ModContent.NPCType<ProfanedGuardianBoss2>());
            if (defenderGuardianIndex == -1)
            {
                Projectile.Kill();
                return;
            }

            NPC defenderGuardian = Main.npc[defenderGuardianIndex];
            Player target = Main.player[defenderGuardian.target];

            // Emit light.
            Lighting.AddLight(Projectile.Center, Color.Yellow.ToVector3() * 0.45f);

            if (GeneralTimer < 120f)
            {
                // Orbit around the guardian.
                float hoverOffset = (float)Math.Sin(GeneralTimer / 300f + Projectile.identity) * 40f + 120f;
                Vector2 hoverDestination = defenderGuardian.Center;
                hoverDestination += (GeneralTimer / 120f * MathHelper.Pi * 4f + Projectile.identity).ToRotationVector2() * hoverOffset;
                Vector2 idealVelocity = Projectile.SafeDirectionTo(hoverDestination) * MathHelper.Min(Projectile.Distance(hoverDestination), 30f);
                Projectile.velocity = (Projectile.velocity * 19f + idealVelocity) / 20f;
                Projectile.velocity = Projectile.velocity.MoveTowards(idealVelocity, 3.5f);
                Projectile.Center = Projectile.Center.MoveTowards(hoverDestination, 7f);

                bool aimedAtTarget = Projectile.velocity.AngleBetween(Projectile.SafeDirectionTo(target.Center)) < 0.2f;

                // Fling the rock at the target if aimed at them.
                if ((aimedAtTarget && GeneralTimer > 65f) || GeneralTimer >= 115f)
                {
                    SoundEngine.PlaySound(SoundID.Item73, Projectile.Center);

                    GeneralTimer = 120f;
                    Projectile.velocity = Projectile.SafeDirectionTo(target.Center) * 11f;
                    Projectile.netUpdate = true;
                }
            }

            // Accelerate after being launched.
            else if (Projectile.velocity.Length() < 30f)
                Projectile.velocity *= 1.025f;

            Projectile.rotation += Projectile.velocity.X * 0.006f;
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 30, 0, 255);
            GeneralTimer++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 baseDrawPosition = Projectile.Center - Main.screenPosition;
            Color afterimageColor = Projectile.GetAlpha(Color.Lerp(Color.Orange, Color.Yellow, 0.56f)) * 0.4f;
            afterimageColor.A = 24;

            for (int i = 0; i < 12; i++)
            {
                float drawOffset = MathHelper.Lerp(16f, 1f, Projectile.Opacity);
                drawOffset = MathHelper.Lerp(drawOffset, 6f, Utils.GetLerpValue(120f, 135f, GeneralTimer, true));
                Vector2 drawPosition = baseDrawPosition + (MathHelper.TwoPi * i / 12f + Main.GlobalTimeWrappedHourly * 0.47f).ToRotationVector2() * drawOffset;
                spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(afterimageColor), Projectile.rotation, origin, Projectile.scale, 0, 0f);
            }

            spriteBatch.Draw(texture, baseDrawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, 0, 0f);
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<HolyFlames>(), 120);
    }
}
