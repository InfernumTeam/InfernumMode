using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.AbyssAIs
{
    public class EidolistRitual : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        
        public int MainEidolistIndex => (int)Projectile.ai[1];
        
        public override void SetStaticDefaults() => DisplayName.SetDefault("Ritual");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.hide = true;
            Projectile.timeLeft = 300;
            Projectile.penetrate = -1;
        }
        
        public override void AI()
        {
            // Die if the main boss is not present.
            if (!Main.npc.IndexInRange(MainEidolistIndex) || !Main.npc[MainEidolistIndex].active)
            {
                Projectile.Kill();
                return;
            }
            
            // Fade in and release some light dust inward.
            Projectile.Opacity = Utils.GetLerpValue(0f, 22f, Time, true) * Utils.GetLerpValue(0f, 25f, Projectile.timeLeft, true);
            if (Projectile.Opacity >= 1f)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust magic = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2CircularEdge(102f, 102f), 264);
                    magic.color = Color.Cyan;
                    magic.velocity = (Projectile.Center - magic.position) * 0.05f;
                    magic.noGravity = true;
                    magic.noLight = true;
                }
            }
            Projectile.scale = Projectile.Opacity;
            Projectile.rotation += 0.018f;
            
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Texture2D outerRingTexture = TextureAssets.Projectile[Projectile.type].Value;
            Texture2D innerRingTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/AbyssAIs/EidolistRitualInnerRing").Value;

            Main.spriteBatch.Draw(outerRingTexture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, outerRingTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(innerRingTexture, drawPosition, null, Projectile.GetAlpha(Color.White), -Projectile.rotation, innerRingTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);

            Main.spriteBatch.ResetBlendState();
            return false;
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI, List<int> overWiresUI)
        {
            drawCacheProjsBehindNPCs.Add(index);
        }
    }
}
