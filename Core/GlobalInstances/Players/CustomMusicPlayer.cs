using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class CustomMusicPlayer : ModPlayer
    {
        public bool UsingHeadphones
        {
            get;
            set;
        }

        public string CurrentTrackName
        {
            get;
            set;
        }

        public override void PreUpdate()
        {
            if (!UsingHeadphones)
                CurrentTrackName = string.Empty;

            // Create music particles if a track is playing.
            if (Main.myPlayer == Player.whoAmI && Main.rand.NextBool(10) && InfernumMode.MusicModIsActive && UsingHeadphones && !string.IsNullOrEmpty(CurrentTrackName))
            {
                int musicNoteID = Main.rand.Next(ProjectileID.EighthNote, ProjectileID.TiedEighthNote + 1);
                Vector2 noteSpawnPosition = Player.Top + new Vector2(Main.rand.NextFloatDirection() * 16f, Main.rand.NextFloat(12f));

                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(note =>
                {
                    note.scale = 0.4f;
                });
                Projectile.NewProjectile(Player.GetSource_FromThis(), noteSpawnPosition, -Vector2.UnitY.RotatedByRandom(0.7f), musicNoteID, 0, 0f, Player.whoAmI);
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag["UsingHeadphones"] = UsingHeadphones;
            tag["CurrentTrackName"] = CurrentTrackName;
        }

        public override void LoadData(TagCompound tag)
        {
            UsingHeadphones = tag.GetBool("UsingHeadphones");
            CurrentTrackName = tag.GetString("CurrentTrackName");
        }
    }
}
