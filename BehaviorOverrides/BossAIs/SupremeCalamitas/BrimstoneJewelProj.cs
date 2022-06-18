using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class BrimstoneJewelProj : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];

        public const int ChargeupTime = 75;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Brimstone Jewel");

        public override void SetDefaults()
        {
            projectile.width = 30;
            projectile.height = 30;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.netImportant = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 90000;
            projectile.Opacity = 0f;
        }

        // Ensure that rotation is synced. It is very important for SCal's attacks.
        public override void SendExtraAI(BinaryWriter writer) => writer.Write(projectile.rotation);

        public override void ReceiveExtraAI(BinaryReader reader) => projectile.rotation = reader.ReadSingle();

        // Projectile spawning and rotation code are done in SCal's AI.
        public override void AI()
        {
            // Die if SCal is gone.
            if (CalamityGlobalNPC.SCal == -1)
            {
                projectile.Kill();
                return;
            }

            // Stay glued to SCal's hand.
            projectile.Center = Main.npc[CalamityGlobalNPC.SCal].Center + projectile.rotation.ToRotationVector2() * 20f;

            // Fade in. While this happens the projectile emits large amounts of flames.
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.05f, 0f, 1f);

            // Frequently sync.
            if (Main.netMode != NetmodeID.MultiplayerClient && projectile.timeLeft % 12 == 11)
            {
                projectile.netUpdate = true;
                projectile.netSpam = 0;
            }

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.GetTexture(Texture);
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 start = drawPosition + Main.screenPosition;
            Vector2 end = start + projectile.rotation.ToRotationVector2() * 4000f;

            if (Time < ChargeupTime)
                spriteBatch.DrawLineBetter(start, end, Color.Red * Utils.InverseLerp(0f, 12f, Time, true), 3f);
            spriteBatch.Draw(texture, drawPosition, null, projectile.GetAlpha(Color.White), projectile.rotation - MathHelper.PiOver2, origin, projectile.scale, 0, 0f);
            return false;
        }
    }
}
