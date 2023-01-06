using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
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
            if (timer < 120f)
                projectile.velocity = Vector2.UnitY * (float)Math.Sin(MathHelper.TwoPi * 2f * timer / 120f) * 3f;
            else if (timer < 210f)
                projectile.velocity = Vector2.Lerp(projectile.velocity, -Vector2.UnitY * 5f, 0.06f);
            else
                projectile.velocity *= 0.97f;

            // Summon deus from the sky after enough time has passed.
            if (timer == 235f)
            {
                SoundEngine.PlaySound(AstrumDeusHead.SpawnSound, projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int deus = NPC.NewNPC(projectile.GetSource_FromAI(), (int)projectile.Center.X, (int)projectile.Center.Y - 3300, ModContent.NPCType<AstrumDeusHead>());
                    CalamityMod.CalamityUtils.BossAwakenMessage(deus);
                }
            }

            if (NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHead>()) && timer < 240f)
                timer = 240f;

            if (!NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHead>()) && timer >= 240f)
                projectile.Kill();
            projectile.timeLeft = 5;

            timer++;
            return false;
        }

        public override bool PreDraw(Projectile projectile, SpriteBatch spriteBatch, Color lightColor)
        {
            // Draw a beacon into the sky.
            float animationTime = projectile.ai[0];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Texture2D borderTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Cultist/Border").Value;
            for (int i = 0; i < 6; i++)
            {
                float intensity = MathHelper.Clamp(Utils.GetLerpValue(120f, 210f, animationTime, true) - i / 5f, 0f, 1f);
                Vector2 origin = TextureAssets.MagicPixel.Value.Size() * 0.5f;
                Vector2 scale = new((float)Math.Sqrt(intensity) * 30f, intensity * 15f);

                // Have the beam color cycle through orange red and cyan based on time and beacon index.
                Color beamColor = Color.Lerp(Color.Lerp(Color.OrangeRed, Color.Red, 0.5f), Color.Cyan, ((float)Math.Cos(Main.GlobalTimeWrappedHourly * 2.2f + i * 0.26f) * 0.5f + 0.5f) * 0.4f + 0.3f);
                beamColor *= intensity * 0.36f;
                beamColor.A = 0;
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, drawPosition, null, beamColor, 0f, origin, scale, SpriteEffects.None, 0f);
            }

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
                float borderOutwardness = Utils.GetLerpValue(0f, 0.9f, leftBorderOpacity, true) * MathHelper.Lerp(700f, 755f, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 4.4f) * 0.5f + 0.5f);
                Color borderColor = Color.Lerp(Color.Transparent, borderColor1, leftBorderOpacity);

                for (int i = 0; i < 150; i++)
                {
                    float fade = 1f - Math.Abs(i - 75f) / 75f;
                    drawPosition = baseDrawPosition + Vector2.UnitY * (i - 75f) / 75f * borderOutwardness;
                    Main.spriteBatch.Draw(borderTexture, drawPosition, null, Color.Lerp(borderColor, borderColor2, 1f - fade) * fade, 0f, borderTexture.Size() * 0.5f, new Vector2(0.33f, 1f), SpriteEffects.None, 0f);
                }
            }

            if (rightBorderOpacity > 0f)
            {
                Vector2 baseDrawPosition = new Vector2(right, Main.LocalPlayer.Center.Y) - Main.screenPosition;
                float borderOutwardness = Utils.GetLerpValue(0f, 0.9f, rightBorderOpacity, true) * MathHelper.Lerp(700f, 755f, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 4.4f) * 0.5f + 0.5f);
                Color borderColor = Color.Lerp(Color.Transparent, borderColor1, rightBorderOpacity);

                for (int i = 0; i < 150; i++)
                {
                    float fade = 1f - Math.Abs(i - 75f) / 75f;
                    drawPosition = baseDrawPosition + Vector2.UnitY * (i - 75f) / 75f * borderOutwardness;
                    Main.spriteBatch.Draw(borderTexture, drawPosition, null, Color.Lerp(borderColor, borderColor2, 1f - fade) * fade, 0f, borderTexture.Size() * 0.5f, new Vector2(0.33f, 1f), SpriteEffects.FlipHorizontally, 0f);
                }
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            // TODO - Use a cool star texture here, after the beacon is drawn.
            return false;
        }
    }
}
