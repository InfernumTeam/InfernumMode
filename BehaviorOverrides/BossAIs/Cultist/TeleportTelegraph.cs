using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cultist
{
    public class TeleportTelegraph : ModProjectile
    {
        public bool CanCreateDust => Projectile.ai[0] == 0f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.timeLeft = 45;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            // Play a teleport sound.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                SoundEngine.PlaySound(SoundID.Item105, Projectile.Center);
            }

            Projectile.Opacity = Utils.GetLerpValue(0f, 12f, Projectile.timeLeft);
            Projectile.scale = Utils.GetLerpValue(45f, 5f, Projectile.timeLeft);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D telegraphTexture = TextureAssets.Projectile[Projectile.type].Value;
            Color telegraphColor = Color.White * Projectile.Opacity * 0.2f;
            telegraphColor.A = 0;

            for (int i = 0; i < 35; i++)
            {
                Vector2 drawPosition = Projectile.Center + (MathHelper.TwoPi * i / 5f + Main.GlobalTimeWrappedHourly * 3f).ToRotationVector2() * 2f;
                drawPosition -= Main.screenPosition;

                Vector2 scale = new Vector2(0.58f, 1f) * Projectile.scale;
                scale *= MathHelper.Lerp(0.015f, 1f, i / 35f);

                Main.spriteBatch.Draw(telegraphTexture, drawPosition, null, telegraphColor, 0f, telegraphTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (!CanCreateDust)
                return;

            for (int i = 0; i < 20; i++)
            {
                Dust magic = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(30f, 30f), 267);
                magic.color = Color.SkyBlue;
                magic.scale = 1.1f;
                magic.fadeIn = 0.6f;
                magic.velocity = Main.rand.NextVector2Circular(2f, 2f);
                magic.velocity = Vector2.Lerp(magic.velocity, -Vector2.UnitY * magic.velocity.Length(), Main.rand.NextFloat(0.65f, 1f));
                magic.noGravity = true;
            }
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI, List<int> overWiresUI)
        {
            drawCacheProjsBehindNPCsAndTiles.Add(index);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            
        }
    }
}
