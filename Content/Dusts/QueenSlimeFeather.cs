using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Dusts
{
    public class QueenSlimeFeather : ModDust
    {
        public override void OnSpawn(Dust dust)
        {
            dust.noLight = true;
        }

        public override bool Update(Dust dust)
        {
            dust.position += dust.velocity;
            dust.scale *= 0.94f;

            if (dust.velocity.Y < 6f && !dust.noGravity)
                dust.velocity.Y += 0.4f;
            Lighting.AddLight(dust.position, 0.255f, 0.255f, 0.255f);
            if (dust.scale < 0.35f)
                dust.active = false;
            
            return false;
        }

        public override Color? GetAlpha(Dust dust, Color lightColor) => new Color(dust.color.R, dust.color.G, dust.color.B, dust.alpha);
    }
}