using System;
using System.Linq;
using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Content.Items.Weapons.Melee;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Melee
{
    public class MyrindaelBonkProjectile : ModProjectile
    {
        public PrimitiveTrailCopy PierceAfterimageDrawer;

        public Player Owner => Main.player[Projectile.owner];

        public float LungeProgression => Utils.GetLerpValue(0f, Myrindael.LungeTime, Time, true);

        public const int CooldownTime = 30;

        public ref float Time => ref Projectile.ai[0];

        public override string Texture => "InfernumMode/Content/Items/Weapons/Melee/Myrindael";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Myrindael");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 60;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Myrindael.LungeTime + CooldownTime;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            // Die if no longer holding the right click button or otherwise cannot use the item.
            if (!Owner.Calamity().mouseRight || Owner.dead || !Owner.active || Owner.noItems)
            {
                Projectile.Kill();
                Owner.velocity = (Owner.velocity * 0.3f).ClampMagnitude(0f, 11f);
                return;
            }

            if (Time == 1f)
                SoundEngine.PlaySound(InfernumSoundRegistry.VassalSlashSound, Projectile.Center);

            // Stick to the owner.
            Projectile.Center = Owner.MountedCenter;

            float angularVelocity = Pi * Pow(LungeProgression, 3f) * 0.024f;
            float currentRotation = Projectile.velocity.ToRotation();
            float idealRotation = Owner.MountedCenter.AngleTo(Owner.Calamity().mouseWorld);
            Projectile.velocity = currentRotation.AngleTowards(idealRotation, angularVelocity).ToRotationVector2();
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
            Owner.heldProj = Projectile.whoAmI;

            Owner.fallStart = (int)(Owner.position.Y / 16f);

            if (LungeProgression < 1f)
            {
                float velocityPower = Utils.Remap(LumUtils.Convert01To010(LungeProgression), 0f, 1f, 0.25f, 1f);
                Vector2 newVelocity = Projectile.velocity * Myrindael.LungeSpeed * (0.09f + 0.91f * velocityPower);
                Owner.velocity = newVelocity;
                Owner.Calamity().LungingDown = true;

                // Release anime-like streak particle effects at the side of the owner to indicate motion.
                if (Main.rand.NextBool(2))
                {
                    Vector2 energySpawnPosition = Owner.Center + Main.rand.NextVector2Circular(90f, 90f) + Owner.velocity * 2f;
                    Vector2 energyVelocity = -Owner.velocity.SafeNormalize(Vector2.UnitX * Owner.direction) * Main.rand.NextFloat(6f, 8.75f);
                    Particle energyLeak = new SquishyLightParticle(energySpawnPosition, energyVelocity, Main.rand.NextFloat(0.55f, 0.9f), Color.Yellow, 30, 3.4f, 4.5f);
                    GeneralParticleHandler.SpawnParticle(energyLeak);
                }
            }

            Time++;
        }

        public override void OnKill(int timeLeft)
        {
            Owner.Calamity().LungingDown = false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Create lightning from the sky.
            SoundEngine.PlaySound(InfernumSoundRegistry.MyrindaelHitSound, Projectile.Center);
            if (Main.myPlayer == Projectile.owner)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 lightningSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 30f, -700f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), lightningSpawnPosition, Vector2.UnitY * Main.rand.NextFloat(50f, 70f), ModContent.ProjectileType<MyrindaelLightning>(), Projectile.damage, 0f, Projectile.owner, PiOver2, Main.rand.Next(100));
                }
            }

            Owner.GiveIFrames(-1, 20);
            Owner.velocity = Owner.SafeDirectionTo(target.Center) * -18f;
            Projectile.Kill();
        }

        public override Color? GetAlpha(Color lightColor) => lightColor * Projectile.Opacity * (float)(1f - Math.Pow(LungeProgression, 5D));

        public float PierceWidthFunction(float completionRatio)
        {
            float width = Utils.GetLerpValue(0f, 0.1f, completionRatio, true) * Projectile.scale * 20f;
            width *= 1f - Pow(LungeProgression, 5f);
            return width;
        }

        public Color PierceColorFunction(float completionRatio) => Color.Lime * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = new(0, texture.Height);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            // Initialize the trail drawer.
            PierceAfterimageDrawer ??= new(PierceWidthFunction, PierceColorFunction, null, true, GameShaders.Misc["CalamityMod:ExobladePierce"]);
            Color mainColor = LumUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly * 2f % 1, Color.Cyan, Color.DeepSkyBlue, Color.Turquoise, Color.Blue);
            Color secondaryColor = LumUtils.MulticolorLerp((Main.GlobalTimeWrappedHourly * 2f + 0.2f) % 1, Color.Cyan, Color.DeepSkyBlue, Color.Turquoise, Color.Blue);

            mainColor = Color.Lerp(Color.White, mainColor, 0.4f + 0.6f * Pow(LungeProgression, 0.5f));
            secondaryColor = Color.Lerp(Color.White, secondaryColor, 0.4f + 0.6f * Pow(LungeProgression, 0.5f));
            Vector2 trailOffset = Projectile.Size * 0.5f - Main.screenPosition + (Projectile.rotation - PiOver4).ToRotationVector2() * 90f;
            GameShaders.Misc["CalamityMod:ExobladePierce"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/EternityStreak"));
            GameShaders.Misc["CalamityMod:ExobladePierce"].UseImage2($"Images/Extra_{ExtrasID.FlameLashTrailShape}");
            GameShaders.Misc["CalamityMod:ExobladePierce"].UseColor(mainColor);
            GameShaders.Misc["CalamityMod:ExobladePierce"].UseSecondaryColor(secondaryColor);
            GameShaders.Misc["CalamityMod:ExobladePierce"].Apply();
            PierceAfterimageDrawer.Draw(Projectile.oldPos.Take(12), trailOffset, 53);
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, 0, 0);

            return false;
        }
    }
}
