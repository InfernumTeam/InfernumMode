using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Cultist
{
    public class Ritual : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public int MainCultistIndex => (int)projectile.ai[1];
        public Color RitualColor => Color.White;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Ritual");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 2;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.netImportant = true;
            projectile.hide = true;
            projectile.timeLeft = 325;
            projectile.penetrate = -1;
        }

        public static int GetWaitTime(bool phase2) => phase2 ? 200 : 290;

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(projectile.timeLeft);

        public override void ReceiveExtraAI(BinaryReader reader) => projectile.timeLeft = reader.ReadInt32();

        public override void AI()
        {
            // Die if the main boss is not present.
            if (!Main.npc.IndexInRange(MainCultistIndex) || !Main.npc[MainCultistIndex].active)
            {
                projectile.Kill();
                return;
            }

            int waitTime = GetWaitTime(Main.npc[MainCultistIndex].ai[2] >= 2f);
            if (Main.netMode != NetmodeID.MultiplayerClient && projectile.timeLeft > waitTime)
            {
                projectile.timeLeft = waitTime;
                projectile.netUpdate = true;
            }

            // Fade in and release some light dust inward.
            projectile.Opacity = Utils.InverseLerp(0f, 22f, Time, true) * Utils.InverseLerp(0f, 25f, projectile.timeLeft, true);
            if (projectile.Opacity >= 1f)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust magic = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2CircularEdge(102f, 102f), 264);
                    magic.color = Color.Yellow;
                    magic.velocity = (projectile.Center - magic.position) * 0.05f;
                    magic.noGravity = true;
                    magic.noLight = true;
                }
            }
            projectile.scale = projectile.Opacity;
            projectile.rotation += 0.018f;

            // Play initial sounds.
            if (projectile.localAI[0] == 0f)
            {
                projectile.localAI[0] = 1f;
                Main.PlaySound(SoundID.Item123, projectile.position);
            }
            Time++;
        }

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Texture2D outerRingTexture = Main.projectileTexture[projectile.type];
            Texture2D innerRingTexture = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Cultist/RitualInnerRing");
            Texture2D auraTexture = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/Cultist/LightBurst");
            float pulse = Main.GlobalTime * 0.67f % 1f;
            float auraScale = projectile.scale * MathHelper.SmoothStep(0.85f, 1.2f, 1f - pulse);
            Color auraColor = Color.White * 0.25f;
            auraColor *= pulse;

            spriteBatch.Draw(auraTexture, drawPosition, null, auraColor, projectile.rotation, auraTexture.Size() * 0.5f, auraScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(outerRingTexture, drawPosition, null, RitualColor, projectile.rotation, outerRingTexture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(innerRingTexture, drawPosition, null, RitualColor, -projectile.rotation, innerRingTexture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);

            spriteBatch.ResetBlendState();
            return false;
        }

		public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
		{
            drawCacheProjsBehindNPCs.Add(index);
        }
    }
}
