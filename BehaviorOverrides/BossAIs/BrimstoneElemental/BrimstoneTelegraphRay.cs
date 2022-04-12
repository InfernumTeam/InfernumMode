using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.BrimstoneElemental
{
    public class BrimstoneTelegraphRay : BaseLaserbeamProjectile
    {
        public int OwnerIndex => (int)Projectile.ai[1];
        public override float Lifetime => 120;
        public override Color LaserOverlayColor => Color.White;
        public override Color LightCastColor => Color.Red;
        public override Texture2D LaserBeginTexture => Utilities.ProjTexture(Projectile.type);
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/BrimstoneRayMid").Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/BrimstoneRayEnd").Value;
        public override float MaxLaserLength => 3100f;
        public override float MaxScale => 1f;
        public Vector2 OwnerEyePosition => Main.npc[OwnerIndex].Center + new Vector2(Main.npc[OwnerIndex].spriteDirection * 20f, -70f);
        public override void SetStaticDefaults() => DisplayName.SetDefault("Brimstone Deathray");

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = (int)Lifetime;
            CooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
        }
        public override void AttachToSomething()
        {
            if (!Main.projectile.IndexInRange(OwnerIndex))
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = OwnerEyePosition;
            Projectile.velocity = new Vector2(Main.npc[OwnerIndex].Infernum().ExtraAI[0], Main.npc[OwnerIndex].Infernum().ExtraAI[1]).SafeNormalize(Vector2.UnitY);
        }

        public override void DetermineScale() => Projectile.scale = 0.1f;

        public override bool? CanDamage() => false ? null : false;
    }
}
