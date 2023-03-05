using CalamityMod;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians.GuardianComboAttackManager;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class CommanderSpear : ModProjectile
    {
        public ref float Timer => ref Projectile.ai[0];

        public NPC Owner => Main.npc[(int)Projectile.ai[1]];

        // This uses this enum due to needing the same states.
        public DefenderShieldStatus Status => (DefenderShieldStatus)Owner.Infernum().ExtraAI[CommanderSpearStatusIndex];

        public float SpearRotation => Owner.Infernum().ExtraAI[CommanderSpearRotationIndex];

        public static int GlowTime => 30;

        public float PositionOffset => Owner.Infernum().ExtraAI[CommanderSpearPositionOffsetIndex];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Spear");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 132;
            Projectile.height = 132;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.Opacity = 0;
            Projectile.timeLeft = 7000;
            Projectile.penetrate = -1;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            bool shouldKill = Status is DefenderShieldStatus.MarkedForRemoval;
            if (!Owner.active || Owner.type != ModContent.NPCType<ProfanedGuardianCommander>() || shouldKill)
            {
                // Reset this index.
                Owner.Infernum().ExtraAI[CommanderSpearStatusIndex] = 0;
                Projectile.Kill();
                return;
            }

            // Move where the commander tells it to.
            if (Status is DefenderShieldStatus.ActiveAndAiming)
            {
                Projectile.rotation = SpearRotation;
                Projectile.netUpdate = true;
                Projectile.netSpam = 0;
            }
            Vector2 offset = Projectile.rotation.ToRotationVector2() * 20f * PositionOffset;
            Projectile.Center = Owner.Center + offset;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.05f, 0f, 1f);
            Projectile.timeLeft = 2000;
        }

        public override bool CanHitPlayer(Player target)
        {
            return base.CanHitPlayer(target);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            if (Owner.Infernum().ExtraAI[CommanderSpearSmearOpacityIndex] > 0f)
            {
                Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/SemiCircularSmear").Value;
                float opacity = Owner.Infernum().ExtraAI[CommanderSpearSmearOpacityIndex] * 0.4f;
                float rotation = Projectile.rotation + MathHelper.PiOver2 * 1.1f;
                Main.EntitySpriteDraw(smear, Projectile.Center - Main.screenPosition, null, WayfinderSymbol.Colors[1] with { A = 0 } * opacity, rotation, smear.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            }

            float backglowAmount = 12f;
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (MathHelper.TwoPi * i / backglowAmount).ToRotationVector2() * 4f;
                Color backglowColor = WayfinderSymbol.Colors[1];
                backglowColor.A = 0;
                Main.spriteBatch.Draw(texture, Projectile.Center + backglowOffset - Main.screenPosition, null, backglowColor * MathHelper.Clamp(Projectile.Opacity * 2f, 0f, 1f) * Owner.Opacity, Projectile.rotation + MathHelper.PiOver4, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity * Owner.Opacity, Projectile.rotation + MathHelper.PiOver4, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
