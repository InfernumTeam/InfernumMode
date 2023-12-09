using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.Content.Projectiles.Wayfinder;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians.GuardianComboAttackManager;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class DefenderShield : ModProjectile
    {
        public ref float Timer => ref Projectile.ai[0];

        public NPC Owner => Main.npc[(int)Projectile.ai[1]];

        public DefenderShieldStatus Status => (DefenderShieldStatus)Owner.Infernum().ExtraAI[DefenderShieldStatusIndex];

        public static int GlowTime => 30;

        public Vector2 PositionOffset;

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 110;
            Projectile.hostile = true;
            Projectile.Opacity = 0;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2000;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
        }

        public override void AI()
        {
            bool shouldKill = Status is DefenderShieldStatus.MarkedForRemoval;
            if (!Owner.active || Owner.type != ModContent.NPCType<ProfanedGuardianDefender>() || shouldKill)
            {
                // Reset this index.
                Owner.Infernum().ExtraAI[DefenderShieldStatusIndex] = 0;
                Projectile.Kill();
                return;
            }

            // Move where the defender is aiming.
            if (Status is DefenderShieldStatus.ActiveAndAiming)
            {
                Vector2 idealOffset = Owner.SafeDirectionTo(Main.player[Owner.target].Center) * 60f;
                PositionOffset = Utils.MoveTowards(PositionOffset, idealOffset, 7f);
                Projectile.rotation = PositionOffset.ToRotation();
                Projectile.netUpdate = true;
                Projectile.netSpam = 0;
            }
            Projectile.Center = Owner.Center + PositionOffset;
            Projectile.rotation = PositionOffset.ToRotation();
            Projectile.spriteDirection = (Projectile.SafeDirectionTo(Main.player[Owner.target].Center).X > 0f).ToDirectionInt();

            Projectile.Opacity = Clamp(Projectile.Opacity + 0.05f, 0f, 1f);
            Projectile.timeLeft = 2000;
            Timer++;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(PositionOffset.X);
            writer.Write(PositionOffset.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            PositionOffset.X = reader.ReadSingle();
            PositionOffset.Y = reader.ReadSingle();
        }

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically;//Projectile.rotation is >= PiOver2 and <= TwoPi - PiOver2 ? SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically : SpriteEffects.FlipHorizontally;
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            float backglowAmount = 12f;
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (TwoPi * i / backglowAmount).ToRotationVector2() * 4f;
                Color backglowColor = WayfinderSymbol.Colors[1];
                backglowColor.A = 0;
                Main.spriteBatch.Draw(texture, Projectile.Center + backglowOffset - Main.screenPosition, null, backglowColor * Clamp(Projectile.Opacity * 2f, 0f, 1f) * Owner.Opacity, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, spriteEffects, 0);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity * Owner.Opacity, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, spriteEffects, 0);
            return false;
        }
    }
}
