using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.Sounds;
using InfernumMode.Content.BehaviorOverrides.BossAIs.HiveMind;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Cultist
{
    public class AncientDoom : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public Player Target => Main.player[(int)Projectile.ai[1]];

        public override string Texture => $"Terraria/Images/NPC_{NPCID.AncientDoom}";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Doomer");
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 50;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 60;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
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

            Projectile.Opacity = Utils.GetLerpValue(0f, 10f, Time, true) * Utils.GetLerpValue(4f, 15f, Projectile.timeLeft, true);
            Projectile.scale = Projectile.Opacity * 1.35f;
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5 == 4)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D doomTexture = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = doomTexture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            for (int i = 0; i < 10; i++)
            {
                Color drawColor = Color.Magenta * Projectile.Opacity * 0.18f;
                Vector2 drawPosition = Projectile.Center + (TwoPi * i / 10f + Main.GlobalTimeWrappedHourly * 4.4f).ToRotationVector2() * 5f;
                Main.spriteBatch.Draw(doomTexture, drawPosition, frame, drawColor, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.ResetBlendState();

            // Make line telegraphs.
            if (Projectile.timeLeft < 36f)
            {
                float widthInterpolant = Utils.GetLerpValue(0f, 36f, Projectile.timeLeft, true);
                float lineWidth = CalamityUtils.Convert01To010(widthInterpolant) * 2f;
                for (int i = 0; i < 9; i++)
                {
                    Vector2 beamDirection = (TwoPi * i / 9f).ToRotationVector2();
                    if (Projectile.localAI[1] == 1f)
                        beamDirection = beamDirection.RotatedBy(TwoPi / 18f);

                    Main.spriteBatch.DrawLineBetter(Projectile.Center, Projectile.Center + beamDirection * DoomBeam.LaserLength, Color.Purple, lineWidth);
                }
            }

            return true;
        }



        public override void Kill(int timeLeft)
        {
            // Make some strong sounds.
            SoundEngine.PlaySound(CommonCalamitySounds.FlareSound with { Volume = 1.61f }, Target.Center);
            SoundEngine.PlaySound(TeslaCannon.FireSound with { Pitch = -0.21f }, Target.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // And explode into a bunch of powerful projectiles.
            Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<HiveMindWave>(), 0, 0f);
            for (int i = 0; i < 35; i++)
            {
                Vector2 doomVelocity = (TwoPi * i / 35f).ToRotationVector2() * 3.15f;
                if (BossRushEvent.BossRushActive)
                    doomVelocity *= 1.5f;

                Utilities.NewProjectileBetter(Projectile.Center, doomVelocity, ModContent.ProjectileType<DarkPulse>(), CultistBehaviorOverride.DarkPulseDamage, 0f);
                Utilities.NewProjectileBetter(Projectile.Center, doomVelocity * 0.25f, ModContent.ProjectileType<DarkPulse>(), CultistBehaviorOverride.DarkPulseDamage, 0f);
            }
            for (int i = 0; i < 9; i++)
            {
                Vector2 beamDirection = (TwoPi * i / 9f).ToRotationVector2();
                if (Projectile.localAI[1] == 1f)
                    beamDirection = beamDirection.RotatedBy(TwoPi / 18f);
                Utilities.NewProjectileBetter(Projectile.Center, beamDirection, ModContent.ProjectileType<DoomBeam>(), CultistBehaviorOverride.DoomBeamDamage, 0f);
            }
        }
    }
}
