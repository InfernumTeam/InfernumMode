using InfernumMode.Common.BaseEntities;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Common.Graphics.Particles
{
    public class ProfanedTempleCinder : BaseCinderParticle
    {
        public override string Texture => "InfernumMode/Common/Graphics/Particles/ProfanedTempleCinder";

        public override void Initialize()
        {
            Color = Main.dayTime ? Color.White : Color.Cyan;
            base.Initialize();
        }
    }
}