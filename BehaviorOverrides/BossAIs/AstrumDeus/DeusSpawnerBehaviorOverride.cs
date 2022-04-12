using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class DeusSpawnerBehaviorOverride : ProjectileBehaviorOverride
    {
        public override int ProjectileOverrideType => ModContent.ProjectileType<DeusRitualDrama>();
        public override ProjectileOverrideContext ContentToOverride => ProjectileOverrideContext.ProjectileAI | ProjectileOverrideContext.ProjectilePreDraw;

        public override bool PreAI(Projectile projectile)
        {
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
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/AstrumDeusSpawn"), projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int deus = NPC.NewNPC(new InfernumSource(), (int)projectile.Center.X, (int)projectile.Center.Y - 3300, ModContent.NPCType<AstrumDeusHeadSpectral>());
                    CalamityMod.CalamityUtils.BossAwakenMessage(deus);
                }
            }

            if (NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHeadSpectral>()) && timer < 240f)
                timer = 240f;

            if (!NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHeadSpectral>()) && timer >= 240f)
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

            // TODO - Use a cool star texture here, after the beacon is drawn.
            return false;
        }
    }
}
