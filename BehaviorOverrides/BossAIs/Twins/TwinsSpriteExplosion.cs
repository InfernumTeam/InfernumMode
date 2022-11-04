using InfernumMode.BaseEntities;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.BehaviorOverrides.BossAIs.Twins
{
    public class TwinsSpriteExplosion : BaseSpriteExplosionProjectile
    {
        public bool SpazmatismVariant
        {
            get => Projectile.ai[1] == 1f;
            set => Projectile.ai[1] = value.ToInt();
        }

        public override int GetFrameUpdateRate => 4;

        public override Color ExplosionColor => Color.Lerp(SpazmatismVariant ? Color.Lime : Color.Red, SpazmatismVariant ? Color.Yellow : Color.Orange, Projectile.identity / 8f % 0.67f) with { A = 0 } * 1.5f;
    }
}
