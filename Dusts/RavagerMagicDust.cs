using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Dusts
{
    public class RavagerMagicDust : ModDust
    {
        public override void OnSpawn(Dust dust) => dust.noGravity = true;

        public override bool MidUpdate(Dust dust)
        {
            if (dust.customData != null && dust.customData is NPC npc)
                dust.position += npc.position - npc.oldPos[1];

            else if (dust.customData != null && dust.customData is Player player)
                dust.position += player.position - player.oldPosition;

            else if (dust.customData != null && dust.customData is Vector2 vector)
            {
                Vector2 idealVelocity = (vector - dust.position).SafeNormalize(-Vector2.UnitY);
                dust.velocity = (dust.velocity * 4f + idealVelocity * dust.velocity.Length()) / 5f;
            }

            if (!dust.noLight)
                Lighting.AddLight(dust.position, Color.BlueViolet.ToVector3() * 0.35f);

            return true;
        }

        public override Color? GetAlpha(Dust dust, Color lightColor) => new Color(lightColor.R, lightColor.G, lightColor.B, 25);
    }
}
