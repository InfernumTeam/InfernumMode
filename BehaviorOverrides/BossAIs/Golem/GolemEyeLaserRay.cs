using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class GolemEyeLaserRay : BaseLaserbeamProjectile
    {
        public ref float AngularVelocity => ref Projectile.ai[0];
        public int OwnerIndex => (int)Projectile.ai[1];
        public override float Lifetime => 120;
        public override Color LaserOverlayColor => Color.White;
        public override Color LightCastColor => Color.White;
        public override Texture2D LaserBeginTexture => ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/GolemHeadBeamBegin", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/GolemHeadBeamMid", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/GolemHeadBeamEnd", AssetRequestMode.ImmediateLoad).Value;
        public override float MaxLaserLength => 2400f;
        public override float MaxScale => 1f;
        public override string Texture => "InfernumMode/ExtraTextures/GolemHeadBeamBegin";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Heat Ray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = (int)Lifetime;
            Projectile.Calamity().DealsDefenseDamage = true;
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
            if (!Main.npc.IndexInRange(OwnerIndex))
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = Main.npc[OwnerIndex].Bottom + new Vector2((AngularVelocity > 0f).ToDirectionInt() * 15f, -57f).RotatedBy(Main.npc[OwnerIndex].rotation) + Projectile.velocity * 2f;
            Projectile.velocity = Projectile.velocity.RotatedBy(AngularVelocity).SafeNormalize(Vector2.UnitY);
        }

        

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(BuffID.OnFire, 240);
    }
}
