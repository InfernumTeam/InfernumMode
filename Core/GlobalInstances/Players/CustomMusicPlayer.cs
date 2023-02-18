using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.TrackedMusic;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
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

        public bool UnlockAllMusic
        {
            get;
            set;
        }

        public float HeadRotationTime
        {
            get;
            set;
        }

        public bool ListeningToMusic => InfernumMode.MusicModIsActive && UsingHeadphones && !string.IsNullOrEmpty(CurrentTrackName);

        public override void PreUpdate()
        {
            if (!UsingHeadphones)
                CurrentTrackName = string.Empty;

            // Create music particles if a track is playing.
            if (Main.myPlayer == Player.whoAmI && Main.rand.NextBool(16) && ListeningToMusic)
            {
                int musicNoteID = Main.rand.Next(ProjectileID.EighthNote, ProjectileID.TiedEighthNote + 1);
                Vector2 noteSpawnPosition = Player.Top + new Vector2(Main.rand.NextFloatDirection() * 16f, Main.rand.NextFloat(12f));

                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(note =>
                {
                    note.scale = 0.5f;
                });
                Projectile.NewProjectile(Player.GetSource_FromThis(), noteSpawnPosition, -Vector2.UnitY.RotatedByRandom(0.7f), musicNoteID, 0, 0f, Player.whoAmI);
            }
        }

        public void BobHeadToMusic()
        {
            // Return the head rotation to its intended angle if there is no music high point being played.
            if (!TrackedMusicManager.TryGetSongInformation(out var songInfo) || !songInfo.HighPoints.Any(s => s.WithinRange(TrackedMusicManager.SongElapsedTime)) || Player.velocity.Length() > 0.1f)
            {
                Player.headRotation = Player.headRotation.AngleTowards(0f, 0.042f);
                HeadRotationTime = 0f;
                return;
            }

            // Jam to the music in accordance with its BMP.
            float beatTime = MathHelper.TwoPi * songInfo.BeatsPerMinute / 3600f;
            if (songInfo.HeadBobState == BPMHeadBobState.Half)
                beatTime *= 0.5f;
            if (songInfo.HeadBobState == BPMHeadBobState.Quarter)
                beatTime *= 0.25f;

            HeadRotationTime += beatTime;
            Player.headRotation = (float)Math.Sin(HeadRotationTime) * 0.276f;
            Player.eyeHelper.BlinkBecausePlayerGotHurt();
        }

        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            if (ListeningToMusic)
                BobHeadToMusic();
        }

        public override void SaveData(TagCompound tag)
        {
            tag["UsingHeadphones"] = UsingHeadphones;
            tag["UnlockAllMusic"] = UnlockAllMusic;
            tag["CurrentTrackName"] = CurrentTrackName;
        }

        public override void LoadData(TagCompound tag)
        {
            UsingHeadphones = tag.GetBool("UsingHeadphones");
            UnlockAllMusic = tag.GetBool("UnlockAllMusic");
            CurrentTrackName = tag.GetString("CurrentTrackName");
        }
    }
}
