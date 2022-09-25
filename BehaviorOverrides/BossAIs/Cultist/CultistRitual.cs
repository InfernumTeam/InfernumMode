using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cultist
{
    public class CultistRitual : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        
        public int MainCultistIndex => (int)Projectile.ai[1];

        public static Color RitualColor => Color.White;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Ritual");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.hide = true;
            Projectile.timeLeft = 325;
            Projectile.penetrate = -1;
        }

        public static int GetWaitTime(bool phase2) => phase2 ? 290 : 250;

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Projectile.timeLeft);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = reader.ReadInt32();

        public override void AI()
        {
            // Die if the main boss is not present.
            if (!Main.npc.IndexInRange(MainCultistIndex) || !Main.npc[MainCultistIndex].active)
            {
                Projectile.Kill();
                return;
            }

            int waitTime = GetWaitTime(Main.npc[MainCultistIndex].ai[2] >= 2f);
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.timeLeft > waitTime)
            {
                Projectile.timeLeft = waitTime;
                Projectile.netUpdate = true;
            }

            // Fade in and release some light dust inward.
            Projectile.Opacity = Utils.GetLerpValue(0f, 22f, Time, true) * Utils.GetLerpValue(0f, 25f, Projectile.timeLeft, true);
            if (Projectile.Opacity >= 1f)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust magic = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2CircularEdge(102f, 102f), 264);
                    magic.color = Color.Yellow;
                    magic.velocity = (Projectile.Center - magic.position) * 0.05f;
                    magic.noGravity = true;
                    magic.noLight = true;
                }
            }
            Projectile.scale = Projectile.Opacity;
            Projectile.rotation += 0.018f;

            // Play initial sounds.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                SoundEngine.PlaySound(SoundID.Item123, Projectile.position);
            }
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Texture2D outerRingTexture = TextureAssets.Projectile[Projectile.type].Value;
            Texture2D innerRingTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Cultist/CultistRitualInnerRing").Value;
            Texture2D auraTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Cultist/LightBurst").Value;
            float pulse = Main.GlobalTimeWrappedHourly * 0.67f % 1f;
            float auraScale = Projectile.scale * MathHelper.SmoothStep(0.85f, 1.2f, 1f - pulse);
            Color auraColor = Color.White * 0.25f;
            auraColor *= pulse;

            Main.spriteBatch.Draw(auraTexture, drawPosition, null, auraColor, Projectile.rotation, auraTexture.Size() * 0.5f, auraScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(outerRingTexture, drawPosition, null, RitualColor, Projectile.rotation, outerRingTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(innerRingTexture, drawPosition, null, RitualColor, -Projectile.rotation, innerRingTexture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);

            Main.spriteBatch.ResetBlendState();
            return false;
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI, List<int> overWiresUI)
        {
            drawCacheProjsBehindNPCs.Add(index);
        }
    }
}
