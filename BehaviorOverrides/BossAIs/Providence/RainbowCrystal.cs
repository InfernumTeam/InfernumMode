using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class RainbowCrystal : ModProjectile
    {
        public float ProvidenceLifeRatio => Main.npc[CalamityGlobalNPC.holyBoss].life / (float)Main.npc[CalamityGlobalNPC.holyBoss].lifeMax;
        public ref float CrystalHue => ref Projectile.ai[0];
        public bool ProvidenceInPhase2 => Projectile.ai[1] == 1f;
        public bool HasStartedFall
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value.ToInt();
        }
        public Color CrystalColor => Main.hslToRgb(CrystalHue, 0.95f, 0.5f);
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Rainbow Crystal Shard");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
            Projectile.extraUpdates = 1;
            CooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(HasStartedFall);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            HasStartedFall = reader.ReadBoolean();
        }

        public override void AI()
        {
            // Don't do anything at all if Providence isn't alive. Calculations are done based on her life ratio.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.holyBoss) || !Main.npc[CalamityGlobalNPC.holyBoss].active)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.timeLeft < 300)
                Projectile.tileCollide = true;

            Projectile.alpha = Utils.Clamp(Projectile.alpha - 8, 0, 255);
            if (Projectile.alpha == 0)
                Lighting.AddLight(Projectile.Center, CrystalColor.ToVector3() * 0.7f);

            Projectile.velocity.X *= 0.995f;

            if (Projectile.velocity.Y >= 0f)
            {
                Projectile.velocity.Y *= 1.06f;
                float maxFallSpeed = MathHelper.Lerp(4.5f, 5.5f, 1f - ProvidenceLifeRatio);

                if (ProvidenceInPhase2)
                    maxFallSpeed += 0.5f;
                if (Projectile.velocity.Y > maxFallSpeed)
                    Projectile.velocity.Y = maxFallSpeed;
            }
            else
                Projectile.velocity.Y *= 0.98f;

            if (!HasStartedFall && Projectile.velocity.Y > -0.5f)
            {
                HasStartedFall = true;
                Projectile.velocity.Y = 0.1f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            EmitIdleDust();
        }

        internal void EmitIdleDust()
        {
            // Don't spawn any dust server-side.
            if (Main.dedServ)
                return;

            for (int i = 0; i < 3; i++)
            {
                if (Main.rand.NextBool(40))
                {
                    Vector2 dustVelocity = Vector2.UnitY.RotatedBy(i * MathHelper.Pi).RotatedBy(Projectile.rotation) * 2.5f;
                    Dust rainbowDust = Dust.NewDustDirect(Projectile.Center, 0, 0, 267, 0f, 0f, 225, CrystalColor, 1.5f);
                    rainbowDust.noGravity = true;
                    rainbowDust.noLight = true;
                    rainbowDust.scale = Projectile.Opacity;
                    rainbowDust.position = Projectile.Center;
                    rainbowDust.velocity = dustVelocity;
                }
            }
            if (Main.rand.NextBool(40))
            {
                Vector2 dustSpawnOffset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(20f, 120f);
                Vector2 dustSpawnPosition = Projectile.Center + dustSpawnOffset;
                Point dustSpawnPositionTileCoords = dustSpawnPosition.ToTileCoordinates();

                // Dust should not be spawned out of the world or inside of tiles.
                if (WorldGen.InWorld(dustSpawnPositionTileCoords.X, dustSpawnPositionTileCoords.Y, 0) && !WorldGen.SolidTile(dustSpawnPositionTileCoords.X, dustSpawnPositionTileCoords.Y))
                {
                    Dust rainbowDust = Dust.NewDustDirect(dustSpawnPosition, 0, 0, 267, 0f, 0f, 127, CrystalColor, 1f);
                    rainbowDust.noGravity = true;
                    rainbowDust.position = dustSpawnPosition;
                    rainbowDust.velocity = -Vector2.UnitY * Main.rand.NextFloat(1.6f, 8.1f);
                    rainbowDust.fadeIn = Main.rand.NextFloat(1f, 2f);
                    rainbowDust.scale = Main.rand.NextFloat(1f, 2f);
                    rainbowDust.noLight = true;

                    rainbowDust = Dust.CloneDust(rainbowDust);
                    rainbowDust.scale *= 0.65f;
                    rainbowDust.fadeIn *= 0.65f;
                    rainbowDust.color = new Color(255, 255, 255, 255);
                }
            }
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27, Projectile.position);
            Vector2 spinningpoint = new Vector2(0f, -3f).RotatedByRandom(3.1415927410125732);
            float dustCount = Main.rand.Next(7, 13);
            Color newColor = Main.hslToRgb(Projectile.ai[0], 1f, 0.5f);
            newColor.A = 255;
            for (float i = 0f; i < dustCount; i++)
            {
                Dust crystalDust = Dust.NewDustDirect(Projectile.Center, 0, 0, 267, 0f, 0f, 0, newColor, 1f);
                crystalDust.position = Projectile.Center;
                crystalDust.velocity = spinningpoint.RotatedBy(MathHelper.TwoPi * i / dustCount) * new Vector2(2.1f, 2f) * (0.8f + Main.rand.NextFloat() * 0.4f);
                crystalDust.noGravity = true;
                crystalDust.scale = 2f;
                crystalDust.fadeIn = Main.rand.NextFloat() * 2f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.EnterShaderRegion();

            MiscShaderData gradientShader = GameShaders.Misc["Infernum:GradientWingShader"];
            gradientShader.UseImage1("Images/Misc/noise");
            gradientShader.UseOpacity(1.2f);
            gradientShader.SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Providence/ProvidenceShaderTexture"));

            gradientShader.Apply();
            gradientShader.Shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly + CrystalHue * MathHelper.TwoPi);
            gradientShader.Shader.CurrentTechnique.Passes[0].Apply();

            Texture2D crystalTexture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 crystalOrigin = crystalTexture.Size() * 0.5f;
            Vector2 crystalDrawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(crystalTexture, crystalDrawPosition, null, Color.White, Projectile.rotation, crystalOrigin, Projectile.scale, SpriteEffects.None, 0f);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
