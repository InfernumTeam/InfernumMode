using CalamityMod;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Particles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
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

        public float Timer => Lifetime - Projectile.timeLeft;

        public float WaitTime;

        public float ReelbackTime => 20;

        public int Lifetime => (int)(WaitTime + ReelbackTime + 240);

        public float RotationOffset => Projectile.ai[0];

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
            Projectile.timeLeft = Lifetime;
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
                Projectile.netUpdate = true;
                Projectile.timeLeft = Lifetime;
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

            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.05f, 0f, 1f);

            if (Timer < WaitTime)
            {
                // Move around the defender.
                //Vector2 hoverDestination = Owner.Center + ((Timer / 25f) + RotationOffset).ToRotationVector2() * 100f;
                //if (Projectile.velocity.Length() < 2f)
                //    Projectile.velocity = Vector2.UnitY * -2.4f;

                //float flySpeed = MathHelper.Lerp(9f, 23f, Utils.GetLerpValue(50f, 270f, Projectile.Distance(hoverDestination), true));
                //flySpeed *= Utils.GetLerpValue(0f, 50f, Projectile.Distance(hoverDestination), true);
                //Projectile.velocity = Projectile.velocity * 0.85f + Projectile.SafeDirectionTo(hoverDestination) * flySpeed * 0.15f;
                //Projectile.velocity = Projectile.velocity.MoveTowards(Projectile.SafeDirectionTo(hoverDestination) * flySpeed, 4f);
                Projectile.Center = Projectile.Center.MoveTowards(Owner.Center - ((Timer / 15f) + RotationOffset).ToRotationVector2() * 100f, 30f);

            }
            if (Timer == WaitTime)
            {
                Projectile.velocity = Projectile.Center.DirectionTo(target.Center) * -3.2f;
                SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound with { Pitch = 0.9f, Volume = 0.9f} , target.Center);
            }
            if (Timer == WaitTime + ReelbackTime)
            {
                SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.95f, Volume = 0.9f}, target.Center);
                Projectile.velocity = Projectile.Center.DirectionTo(target.Center) * 17f;
                for (int i = 0; i < 20; i++)
                {
                    Vector2 velocity = -Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(Main.rand.NextFloat(-0.15f, 0.15f)) * Main.rand.NextFloat(4f, 6f);
                    Particle rockParticle = new SandyDustParticle(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2f, Projectile.height / 2f), velocity, Color.SandyBrown, Main.rand.NextFloat(1.25f, 1.55f), 90);
                    GeneralParticleHandler.SpawnParticle(rockParticle);

                    Particle fire = new HeavySmokeParticle(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2f, Projectile.height / 2f), Vector2.Zero, Main.rand.NextBool() ? WayfinderSymbol.Colors[1] : WayfinderSymbol.Colors[2], 30, Main.rand.NextFloat(0.2f, 0.4f), 1f, glowing: true, rotationSpeed: Main.rand.NextFromList(-1, 1) * 0.01f);
                    GeneralParticleHandler.SpawnParticle(fire);                 
                }
                if (CalamityConfig.Instance.Screenshake)
                    target.Infernum_Camera().CurrentScreenShakePower = 2f;
            }
            if (Timer > WaitTime)
            {
                Particle rockParticle = new SandyDustParticle(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 3f, Projectile.height / 3f), Vector2.Zero, Color.SandyBrown, Main.rand.NextFloat(0.45f, 0.75f), 30);
                GeneralParticleHandler.SpawnParticle(rockParticle);
                Projectile.rotation -= 0.1f;
            }
        }

        public override bool ShouldUpdatePosition() => true;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            if (Timer >= WaitTime && Timer < WaitTime + ReelbackTime)
            {
                Texture2D invis = InfernumTextureRegistry.Invisible.Value;
                float opacity = MathF.Sin((Timer - WaitTime) / ReelbackTime * MathF.PI);
                Effect laserScopeEffect = Filters.Scene["PixelatedSightLine"].GetShader().Shader;
                laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/CertifiedCrustyNoise").Value);
                laserScopeEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.003f);
                laserScopeEffect.Parameters["mainOpacity"].SetValue((float)Math.Pow((double)opacity, 0.5f));
                laserScopeEffect.Parameters["Resolution"].SetValue(new Vector2(340f));
                Player target = Main.player[Owner.target];
                laserScopeEffect.Parameters["laserAngle"].SetValue((target.Center - Projectile.Center).ToRotation() * -1f);
                laserScopeEffect.Parameters["laserWidth"].SetValue(0.0025f + (float)Math.Pow((double)opacity, 5.0) * ((float)Math.Sin((double)(Main.GlobalTimeWrappedHourly * 3f)) * 0.002f + 0.002f));
                laserScopeEffect.Parameters["laserLightStrenght"].SetValue(3f);
                laserScopeEffect.Parameters["color"].SetValue(Color.Lerp(WayfinderSymbol.Colors[1], Color.OrangeRed, 0.5f).ToVector3());
                laserScopeEffect.Parameters["darkerColor"].SetValue(WayfinderSymbol.Colors[2].ToVector3());
                laserScopeEffect.Parameters["bloomSize"].SetValue(0.06f + (1f - opacity) * 0.1f);
                laserScopeEffect.Parameters["bloomMaxOpacity"].SetValue(0.4f);
                laserScopeEffect.Parameters["bloomFadeStrenght"].SetValue(3f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, laserScopeEffect, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(invis, drawPosition, null, Color.White, 0f, invis.Size() * 0.5f, 1500f, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }

            Color backglowColor = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], 0.5f);
            backglowColor.A = 0;
            float backglowAmount = 12;
            for (int i = 0; i < backglowAmount; i++)
            { 
                Vector2 backglowOffset = (MathHelper.TwoPi * i / backglowAmount).ToRotationVector2() * 4f;
                Main.EntitySpriteDraw(texture, drawPosition + backglowOffset, null, backglowColor * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor) * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            if (Timer >= WaitTime - 30)
            {
                float opacityScalar = (1f + MathF.Sin((Timer - WaitTime / 30) / (WaitTime + ReelbackTime) - (Timer - WaitTime / 30) * 2 * MathF.PI)) / 2f;
                backglowColor = Color.Lerp(backglowColor, Color.OrangeRed, opacityScalar);
                for (int i = 0; i < 3; i++)
                    Main.EntitySpriteDraw(texture, drawPosition, null, backglowColor * Projectile.Opacity * opacityScalar, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }
            return false;
        }
    }
}
