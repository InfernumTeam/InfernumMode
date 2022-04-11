using CalamityMod;
using CalamityMod.Events;
using InfernumMode.BehaviorOverrides.BossAIs.HiveMind;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cultist
{
    public class AncientDoom : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public Player Target => Main.player[(int)Projectile.ai[1]];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Doomer");
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 50;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 90;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            // Initialize a target.
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.localAI[0] == 0f)
            {
                Projectile.ai[1] = Player.FindClosest(Projectile.Center, 1, 1);
                Projectile.localAI[0] = 1f;

                Projectile.netUpdate = true;
            }

            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Time, true) * Utils.GetLerpValue(4f, 35f, Projectile.timeLeft, true);
            Projectile.scale = Utils.GetLerpValue(0f, 15f, Time, true) * Utils.GetLerpValue(4f, 35f, Projectile.timeLeft, true) * 1.35f;
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5 == 4)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D doomTexture = Main.projectileTexture[Projectile.type];
            Rectangle frame = doomTexture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            for (int i = 0; i < 10; i++)
            {
                Color drawColor = Color.Magenta * Projectile.Opacity * 0.18f;
                Vector2 drawPosition = Projectile.Center + (MathHelper.TwoPi * i / 10f + Main.GlobalTimeWrappedHourly * 4.4f).ToRotationVector2() * 5f;
                spriteBatch.Draw(doomTexture, drawPosition, frame, drawColor, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }

            spriteBatch.ResetBlendState();

            // Make line telegraphs.
            if (Projectile.timeLeft < 40f)
            {
                for (int i = 0; i < 9; i++)
                {
                    Vector2 beamDirection = (MathHelper.TwoPi * i / 9f).ToRotationVector2();
                    if (Projectile.localAI[1] == 1f)
                        beamDirection = beamDirection.RotatedBy(MathHelper.TwoPi / 18f);
                    spriteBatch.DrawLineBetter(Projectile.Center, Projectile.Center + beamDirection * DoomBeam.LaserLength, Color.Purple, (float)Math.Sin(MathHelper.Pi * Utils.GetLerpValue(0f, 40f, Projectile.timeLeft, true)) * 2f);
                }
            }

            return true;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = Projectile;

        public override void Kill(int timeLeft)
        {
            // Make some strong sounds.
            var sound = SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/FlareSound"), Target.Center);
            if (sound != null)
                sound.Volume = MathHelper.Clamp(sound.Volume * 1.61f, -1f, 1f);
            sound = SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/TeslaCannonFire"), Target.Center);
            if (sound != null)
                sound.Pitch = -0.21f;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // And explode into a bunch of powerful projectiles.
            Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<HiveMindWave>(), 0, 0f);
            for (int i = 0; i < 35; i++)
            {
                Vector2 doomVelocity = (MathHelper.TwoPi * i / 35f).ToRotationVector2() * 3.15f;
                if (BossRushEvent.BossRushActive)
                    doomVelocity *= 1.5f;

                Utilities.NewProjectileBetter(Projectile.Center, doomVelocity, ModContent.ProjectileType<DarkPulse>(), 170, 0f);
                Utilities.NewProjectileBetter(Projectile.Center, doomVelocity * 0.25f, ModContent.ProjectileType<DarkPulse>(), 170, 0f);
            }
            for (int i = 0; i < 9; i++)
            {
                Vector2 beamDirection = (MathHelper.TwoPi * i / 9f).ToRotationVector2();
                if (Projectile.localAI[1] == 1f)
                    beamDirection = beamDirection.RotatedBy(MathHelper.TwoPi / 18f);
                Utilities.NewProjectileBetter(Projectile.Center, beamDirection, ModContent.ProjectileType<DoomBeam>(), 240, 0f);
            }
        }
    }
}
