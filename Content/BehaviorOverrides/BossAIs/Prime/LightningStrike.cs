using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Prime
{
    public class LightningStrike : BasePrimitiveLightningProjectile
    {
        public override int Lifetime => 90;
        public override int TrailPointCount => 150;

        public override void PostAI()
        {
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.type == NPCID.SkeletronPrime && Projectile.Hitbox.Intersects(n.Hitbox))
                {
                    n.Infernum().ExtraAI[0] = 1f;
                    n.netUpdate = true;
                }
            }
        }

        public override float PrimitiveWidthFunction(float completionRatio)
        {
            float baseWidth = Lerp(4f, 10f, Sin(Pi * 4f * completionRatio) * 0.5f + 0.5f) * Projectile.scale;
            return baseWidth * Sin(Pi * completionRatio);
        }

        public override Color PrimitiveColorFunction(float completionRatio)
        {
            Color baseColor = Color.Lerp(Color.Crimson, Color.DarkRed, Sin(TwoPi * completionRatio + Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f);
            return Color.Lerp(baseColor, Color.Red, (Sin(Pi * completionRatio + Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f) * 0.8f);
        }
    }
}
