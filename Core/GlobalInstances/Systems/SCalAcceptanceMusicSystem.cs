using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Calamity = CalamityMod.CalamityMod;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class SCalAcceptanceMusicSystem : ModSystem
    {
        public static int ThemeTimer
        {
            get;
            set;
        }

        public static int ThemeDuration => 3840;

        public override void UpdateUI(GameTime gameTime)
        {
            if (ThemeTimer >= 1)
            {
                ThemeTimer++;
                if (ThemeTimer >= ThemeDuration || !InfernumMode.CalMusicModIsActive)
                    ThemeTimer = 0;
            }
        }

        public override void OnWorldUnload() => ThemeTimer = 0;
    }

    public class SCalAcceptanceMusicPlayerScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => (SceneEffectPriority)10;

        public override bool IsSceneEffectActive(Player player) => SCalAcceptanceMusicSystem.ThemeTimer >= 1;

        public override int Music => Calamity.Instance.GetMusicFromMusicMod("CalamitasDefeat") ?? MusicID.Eerie;
    }
}
