using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class BrimstoneJewelProj : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public const int ChargeupTime = 142;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Brimstone Jewel");

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90000;
            Projectile.Opacity = 0f;
        }

        // Ensure that rotation is synced. It is very important for SCal's attacks.
        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Projectile.rotation);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.rotation = reader.ReadSingle();

        // Projectile spawning and rotation code are done in SCal's AI.
        public override void AI()
        {
            // Die if SCal is gone.
            if (CalamityGlobalNPC.SCal == -1 || !Main.npc[CalamityGlobalNPC.SCal].active)
            {
                Projectile.Kill();
                return;
            }

            // Stay glued to SCal's hand.
            Projectile.Center = Main.npc[CalamityGlobalNPC.SCal].Center + Projectile.rotation.ToRotationVector2() * 20f;

            // Fade in. While this happens the projectile emits large amounts of flames.
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.05f, 0f, 1f);

            // Frequently sync.
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.timeLeft % 3 == 2)
            {
                Projectile.netUpdate = true;
                Projectile.netSpam = 0;
            }

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 start = drawPosition + Main.screenPosition;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * 4000f;

            if (Time < ChargeupTime)
                Main.spriteBatch.DrawLineBetter(start, end, Color.Red * Utils.GetLerpValue(0f, 12f, Time, true), 3f);
            Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation - MathHelper.PiOver2, origin, Projectile.scale, 0, 0f);
            return false;
        }
    }
}
