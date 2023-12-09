using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Dusts
{
    public class EggDust : ModDust
    {
        public override void OnSpawn(Dust dust)
        {
            dust.color = default;
            dust.noGravity = true;
            dust.noLight = true;
            dust.alpha = 0;
        }

        public override bool Update(Dust dust)
        {
            dust.position += dust.velocity;
            dust.scale *= 0.965f;
            dust.velocity *= 0.94f;
            if (dust.scale < 0.05f)
                dust.active = false;

            return false;
        }
    }
}
