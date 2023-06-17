using CalamityMod;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Common.Graphics;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class DeusSpawnerBehaviorOverride : ProjectileBehaviorOverride
    {
        public override int ProjectileOverrideType => ModContent.ProjectileType<DeusRitualDrama>();
        public override ProjectileOverrideContext ContentToOverride => ProjectileOverrideContext.ProjectileAI | ProjectileOverrideContext.ProjectilePreDraw;

        public override bool PreAI(Projectile projectile)
        {
            ProjectileID.Sets.DrawScreenCheckFluff[projectile.type] = 999999;

            ref float timer = ref projectile.ai[0];

            // Rise into the sky a bit after oscillating. After even more time has passed, slow down.
            if (timer is >= 60f and < 350f)
                projectile.velocity.Y = Lerp(projectile.velocity.Y, -3f, 0.06f);
            else
                projectile.velocity *= 0.97f;

            // Summon deus from the sky after enough time has passed.
            if (timer == 374f)
            {
                SoundEngine.PlaySound(AstrumDeusHead.SpawnSound, projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int deus = NPC.NewNPC(projectile.GetSource_FromAI(), (int)projectile.Center.X, (int)projectile.Center.Y - 1900, ModContent.NPCType<AstrumDeusHead>());
                    CalamityUtils.BossAwakenMessage(deus);
                }

                Color[] explosionColors = new Color[]
                {
                    new(250, 90, 74, 127),
                    new(76, 255, 194, 127)
                };
                GeneralParticleHandler.SpawnParticle(new ElectricExplosionRing(projectile.Center, Vector2.Zero, explosionColors, 3f, 180, 1.9f));
                ScreenEffectSystem.SetBlurEffect(projectile.Center, 0.5f, 20);
            }

            if (NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHead>()) && timer < 375f)
                timer = 375f;

            if (!NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHead>()) && timer >= 375f)
                projectile.Kill();
            projectile.timeLeft = 5;

            timer++;
            return false;
        }

        public override bool PreDraw(Projectile projectile, SpriteBatch spriteBatch, Color lightColor)
        {
            // Draw a beacon into the sky.
            Texture2D borderTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Cultist/Border").Value;

            float left = projectile.Center.X - AstrumDeusHeadBehaviorOverride.EnrageStartDistance;
            float right = projectile.Center.X + AstrumDeusHeadBehaviorOverride.EnrageStartDistance;
            float leftBorderOpacity = Utils.GetLerpValue(left + 350f, left, Main.LocalPlayer.Center.X, true) * 0.6f;
            float rightBorderOpacity = Utils.GetLerpValue(right - 350f, right, Main.LocalPlayer.Center.X, true) * 0.6f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Color borderColor1 = Color.OrangeRed;
            Color borderColor2 = Color.Cyan;
            if (leftBorderOpacity > 0f)
            {
                Vector2 baseDrawPosition = new Vector2(left, Main.LocalPlayer.Center.Y) - Main.screenPosition;
                float borderOutwardness = Utils.GetLerpValue(0f, 0.9f, leftBorderOpacity, true) * Lerp(700f, 755f, Cos(Main.GlobalTimeWrappedHourly * 4.4f) * 0.5f + 0.5f);
                Color borderColor = Color.Lerp(Color.Transparent, borderColor1, leftBorderOpacity);

                for (int i = 0; i < 150; i++)
                {
                    float fade = 1f - Math.Abs(i - 75f) / 75f;
                    Vector2 drawPosition = baseDrawPosition + Vector2.UnitY * (i - 75f) / 75f * borderOutwardness;
                    Main.spriteBatch.Draw(borderTexture, drawPosition, null, Color.Lerp(borderColor, borderColor2, 1f - fade) * fade, 0f, borderTexture.Size() * 0.5f, new Vector2(0.33f, 1f), SpriteEffects.None, 0f);
                }
            }

            if (rightBorderOpacity > 0f)
            {
                Vector2 baseDrawPosition = new Vector2(right, Main.LocalPlayer.Center.Y) - Main.screenPosition;
                float borderOutwardness = Utils.GetLerpValue(0f, 0.9f, rightBorderOpacity, true) * Lerp(700f, 755f, Cos(Main.GlobalTimeWrappedHourly * 4.4f) * 0.5f + 0.5f);
                Color borderColor = Color.Lerp(Color.Transparent, borderColor1, rightBorderOpacity);

                for (int i = 0; i < 150; i++)
                {
                    float fade = 1f - Math.Abs(i - 75f) / 75f;
                    Vector2 drawPosition = baseDrawPosition + Vector2.UnitY * (i - 75f) / 75f * borderOutwardness;
                    Main.spriteBatch.Draw(borderTexture, drawPosition, null, Color.Lerp(borderColor, borderColor2, 1f - fade) * fade, 0f, borderTexture.Size() * 0.5f, new Vector2(0.33f, 1f), SpriteEffects.FlipHorizontally, 0f);
                }
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return projectile.ai[0] <= 374f;
        }
    }
}
