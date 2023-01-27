using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Particles;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class ProfanedRock : ModProjectile
    {
        public static string[] Textures => new string[4]
        {
            "ProfanedRock",
            "ProfanedRock2",
            "ProfanedRock3",
            "ProfanedRock4",
        };

        public string CurrentVarient = Textures[0];

        public static int FadeInTime => 90;

        public Vector2 HoverOffset;

        public ref float Timer => ref Projectile.ai[0];

        public NPC Owner => Main.npc[(int)Projectile.ai[1]];

        public override string Texture => "InfernumMode/Content/BehaviorOverrides/BossAIs/ProfanedGuardians/" + CurrentVarient;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Profaned Rock");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
        }

        public override void SetDefaults()
        {
            // These get changed later, but are this be default.
            Projectile.width = 42;
            Projectile.height = 36;

            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.Opacity = 0;
            Projectile.timeLeft = 360;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int varient = Main.rand.Next(4);
                switch (varient)
                {
                    case 0:
                        CurrentVarient = Textures[varient];
                        break;
                    case 1:
                        CurrentVarient = Textures[varient];
                        Projectile.width = 34;
                        Projectile.height = 38;
                        break;
                    case 2:
                        CurrentVarient = Textures[varient];
                        Projectile.width = 36;
                        Projectile.height = 46;
                        break;
                    case 3:
                        CurrentVarient = Textures[varient];
                        Projectile.width = 28;
                        Projectile.height = 36;
                        break;
                }
                HoverOffset = Projectile.velocity;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
            }
        }

        public override void AI()
        {
            if (!Owner.active || Owner.type != ModContent.NPCType<ProfanedGuardianDefender>())
            {
                Projectile.Kill();
                return;
            }

            Player target = Main.player[Owner.target];

            // Fade in.
            Projectile.Opacity = MathHelper.Lerp(0f, 1f, Timer / FadeInTime);

            if (Timer == FadeInTime && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.velocity = Projectile.SafeDirectionTo(target.Center) * Main.rand.NextFloat(8f, 12f);
                Projectile.netUpdate = true;
            }



            if (Timer >= FadeInTime)
                Projectile.rotation -= 0.1f;
            else
                Projectile.Center = Owner.Center + HoverOffset;

            Timer++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            float backglowAmount = 12;
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (MathHelper.TwoPi * i / backglowAmount).ToRotationVector2() * 4f;
                Color backglowColor = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], 0.5f);
                backglowColor.A = 0;
                Main.EntitySpriteDraw(texture, drawPosition + backglowOffset, null, backglowColor * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor) * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
