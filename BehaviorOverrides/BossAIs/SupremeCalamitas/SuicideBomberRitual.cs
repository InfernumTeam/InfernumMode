using InfernumMode.ILEditingStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SuicideBomberRitual : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];

        public const int Lifetime = 84;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Ritual");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 34;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = Lifetime;
            projectile.hide = true;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(0f, 60f, Time, true);
            projectile.scale = projectile.Opacity;
            projectile.direction = (projectile.identity % 2 == 0).ToDirectionInt();
            projectile.rotation += projectile.direction * 0.18f;

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.DD2_ExplosiveTrapExplode, projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Utilities.NewProjectileBetter(projectile.Center, Vector2.Zero, ModContent.ProjectileType<BrimstoneDemonSummonExplosion>(), 0, 0f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/SupremeCalamitas/SuicideBomberRitual");
            Texture2D innerCircle = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/SupremeCalamitas/SuicideBomberRitualCircleInner");
            Color color = projectile.GetAlpha(Color.Lerp(Color.Red, Color.Blue, projectile.identity / 6f % 1f));
            Color color2 = projectile.GetAlpha(Color.Lerp(Color.Red, Color.Blue, (projectile.identity / 6f + 0.27f) % 1f));
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(texture, drawPosition, null, color, projectile.rotation, texture.Size() * 0.5f, projectile.scale, 0, 0f);
            Main.spriteBatch.Draw(innerCircle, drawPosition, null, color2, -projectile.rotation, innerCircle.Size() * 0.5f, projectile.scale, 0, 0f);
            return false;
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            DrawBlackEffectHook.DrawCacheAdditiveLighting.Add(index);
        }
    }
}
