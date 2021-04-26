using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Dusts
{
    public class RavagerMagicDust : ModDust
    {
        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = true;
        }

        public override bool MidUpdate(Dust dust)
        {
            if (dust.customData != null && dust.customData is NPC)
            {
                NPC nPC = (NPC)dust.customData;
                dust.position += nPC.position - nPC.oldPos[1];
            }
            else if (dust.customData != null && dust.customData is Player)
            {
                Player player5 = (Player)dust.customData;
                dust.position += player5.position - player5.oldPosition;
            }
            else if (dust.customData != null && dust.customData is Vector2)
            {
                Vector2 vector3 = (Vector2)dust.customData - dust.position;
                if (vector3 != Vector2.Zero)
                {
                    vector3.Normalize();
                }
                dust.velocity = (dust.velocity * 4f + vector3 * dust.velocity.Length()) / 5f;
            }
            return true;
        }

        public override Color? GetAlpha(Dust dust, Color lightColor)
        {
            return new Color(lightColor.R, lightColor.G, lightColor.B, 25);
        }
    }
}
