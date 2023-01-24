using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Polterghast
{
    public class NotSpecialSoul : ModProjectile
    {
        public bool Cyan => Projectile.ai[0] == 1f;

        public bool AcceleratesAndHomes => Projectile.ai[1] == 1f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Soul");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;
            Projectile.penetrate = -1;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Projectile.timeLeft);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = reader.ReadInt32();

        public override void AI()
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.ghostBoss))
            {
                Projectile.Kill();
                return;
            }

            NPC polterghast = Main.npc[CalamityGlobalNPC.ghostBoss];

            // Fade in/out and rotate.
            Projectile.Opacity = Utils.GetLerpValue(300f, 295f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 25f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            // Handle frames.
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5 == 4)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];

            // Create periodic puffs of dust.
            if (Projectile.timeLeft % 18 == 17)
            {
                for (int i = 0; i < 16; i++)
                {
                    Vector2 dustOffset = Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / 16f) * new Vector2(4f, 1f);
                    dustOffset = dustOffset.RotatedBy(Projectile.velocity.ToRotation());

                    Dust ectoplasm = Dust.NewDustDirect(Projectile.Center, 0, 0, 175, 0f, 0f);
                    ectoplasm.position = Projectile.Center + dustOffset;
                    ectoplasm.velocity = dustOffset.SafeNormalize(Vector2.Zero) * 1.5f;
                    ectoplasm.color = Color.Lerp(Color.Purple, Color.White, 0.5f);
                    ectoplasm.scale = 1.5f;
                    ectoplasm.noGravity = true;
                }
            }

            // Return to the polterghast.
            if (Projectile.timeLeft < 3)
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, polterghast.Center, 0.06f);
                Projectile.velocity = (Projectile.velocity * 11f + Projectile.SafeDirectionTo(polterghast.Center) * 36f) / 12f;
                Projectile.damage = 0;
                if (Projectile.Hitbox.Intersects(polterghast.Hitbox))
                {
                    polterghast.ai[2]--;
                    Projectile.Kill();
                }
                Projectile.timeLeft = 2;
            }

            // Accelerate and home.
            else if (Projectile.timeLeft < 135f && AcceleratesAndHomes)
            {
                float WHATTHEFUCKDOYOUWANTFROMME = Projectile.velocity.Length() + 0.01f;
                Player target = Main.player[polterghast.target];
                Projectile.velocity = (Projectile.velocity * 169f + Projectile.SafeDirectionTo(target.Center) * WHATTHEFUCKDOYOUWANTFROMME) / 170f;
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * WHATTHEFUCKDOYOUWANTFROMME;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Polterghast/SoulLarge" + (Cyan ? "Cyan" : "")).Value;
            if (Projectile.whoAmI % 2 == 0)
                texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Polterghast/SoulMedium" + (Cyan ? "Cyan" : "")).Value;

            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 2, texture);
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color color = Color.White;
            color.A = 0;
            return color * Projectile.Opacity;
        }
    }
}
