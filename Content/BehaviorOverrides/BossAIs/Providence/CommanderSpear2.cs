using CalamityMod.NPCs;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Metaballs;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Providence.ProvidenceBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class CommanderSpear2 : ModProjectile
    {
        public int OwnerIndex
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        public NPC Owner => Main.npc[OwnerIndex];

        public static SpearAttackState CurrentBehavior => (SpearAttackState)Main.npc[CalamityGlobalNPC.holyBoss].Infernum().ExtraAI[0];

        public static float CircularSmearInterpolant => Main.npc[CalamityGlobalNPC.holyBoss].Infernum().ExtraAI[1];

        public ref float Time => ref Projectile.ai[1];

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Holy Spear");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 124;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 7200;
            Projectile.Opacity = 0f;
            Projectile.netImportant = true;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(OwnerIndex);

        public override void ReceiveExtraAI(BinaryReader reader) => OwnerIndex = reader.ReadInt32();

        public override void AI()
        {
            // Disappear if the owner or Providence are not present.
            bool notActuallyCharging = CurrentBehavior == SpearAttackState.Charge && Projectile.velocity == Vector2.Zero;
            if (OwnerIndex == -1 || CalamityGlobalNPC.holyBoss == -1 || (!Owner.active && CurrentBehavior != SpearAttackState.Charge) || notActuallyCharging)
            {
                Projectile.active = false;
                return;
            }

            bool stickToOwner = true;
            switch (CurrentBehavior)
            {
                case SpearAttackState.LookAtTarget:
                    float idealRotation = Projectile.AngleTo(Main.player[Owner.target].Center) + PiOver4;
                    Projectile.rotation = Projectile.rotation.AngleLerp(idealRotation, 0.11f).AngleTowards(idealRotation, 0.032f);
                    break;
                case SpearAttackState.SpinInPlace:
                    Projectile.rotation += 0.1f * Pi * Owner.spriteDirection;
                    break;
                case SpearAttackState.Charge:
                    Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
                    Projectile.tileCollide = true;
                    if (Projectile.timeLeft >= 240)
                        Projectile.timeLeft = 240;

                    if (Projectile.velocity.Length() < 38f)
                        Projectile.velocity *= 1.0094f;
                    stickToOwner = false;
                    break;
            }

            // Stick to the owner if necessary.
            if (stickToOwner)
            {
                Projectile.Center = Owner.Center + Projectile.rotation.ToRotationVector2() * 20f;
                Projectile.Opacity = Clamp(Projectile.Opacity + 0.05f, 0f, 1f);
            }

            Time++;
        }

        public override bool? CanDamage() => CurrentBehavior == SpearAttackState.Charge ? null : false;

        public override void OnKill(int timeLeft)
        {
            if (timeLeft <= 5)
                return;

            // Burst into lava metaballs on death.
            if (Main.netMode != NetmodeID.Server)
                ModContent.GetInstance<ProfanedLavaMetaball>().SpawnParticles(ModContent.Request<Texture2D>(Texture).Value.CreateMetaballsFromTexture(Projectile.Center, Projectile.rotation, Projectile.scale, 20f, 30));

            // Release accelerating spears outward.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Utilities.NewProjectileBetter(Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 60f, Projectile.velocity, ModContent.ProjectileType<StrongProfanedCrack>(), 0, 0f);

                float shootOffsetAngle = Main.rand.NextFloat(TwoPi);
                for (int i = 0; i < 15; i++)
                {
                    Vector2 spearDirection = (TwoPi * i / 15f + shootOffsetAngle).ToRotationVector2();
                    Utilities.NewProjectileBetter(Projectile.Center, spearDirection * 0.01f, ModContent.ProjectileType<CrystalTelegraphLine>(), 0, 0f, -1, 0f, 54f);
                    Utilities.NewProjectileBetter(Projectile.Center, spearDirection * 8f, ModContent.ProjectileType<ProfanedSpearInfernum>(), HolySpearDamage, 0f);
                }
            }

            Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = MathF.Max(Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower, 8f);
            ScreenEffectSystem.SetFlashEffect(Projectile.Center, 1f, 13);

            SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceSpearHitSound with
            {
                Volume = 2f
            }, Projectile.Center);
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            if (IsEnraged)
                texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/CommanderSpear2Night").Value;

            if (CircularSmearInterpolant > 0f)
            {
                Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/SemiCircularSmear").Value;
                float opacity = CircularSmearInterpolant * 0.4f;
                float rotation = Projectile.rotation + PiOver2 * 1.1f;
                Main.EntitySpriteDraw(smear, Projectile.Center - Main.screenPosition, null, Color.Gold with
                {
                    A = 0
                } * opacity, rotation, smear.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            }

            float backglowAmount = 12f;
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (TwoPi * i / backglowAmount).ToRotationVector2() * 4f;
                Color backglowColor = IsEnraged ? Color.Cyan : Color.Gold;
                backglowColor.A = 0;
                Main.spriteBatch.Draw(texture, Projectile.Center + backglowOffset - Main.screenPosition, null, backglowColor * Clamp(Projectile.Opacity * 2f, 0f, 1f) * Owner.Opacity, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity * Owner.Opacity, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
