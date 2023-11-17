using System;
using System.Collections.Generic;
using System.Linq;
using InfernumMode.Core.TrackedMusic;
using Terraria;

namespace InfernumMode.Core.ModCalls.InfernumCalls
{
    // This doesn't really do anything with the tracked music disabled but, oh well.
    public class BopHeadToMusicModCall : ReturnValueModCall<float>
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "BopHeadToMusic";
            }
        }

        public override IEnumerable<Type> InputTypes
        {
            get
            {
                yield return typeof(Player);
                yield return typeof(float);
            }
        }

        protected override float ProcessGeneric(params object[] argsWithoutCommand)
        {
            Player player = (Player)argsWithoutCommand[0];
            float headRotationTime = (float)argsWithoutCommand[1];

            // Return the head rotation to its intended angle if there is no music high point being played.
            if (!TrackedMusicManager.TryGetSongInformation(out var songInfo) || !songInfo.HeadphonesHighPoints.Any(s => s.WithinRange(TrackedMusicManager.SongElapsedTime)) || player.velocity.Length() > 0.1f)
            {
                player.headRotation = player.headRotation.AngleTowards(0f, 0.042f);
                headRotationTime = 0f;
                return headRotationTime;
            }

            // Jam to the music in accordance with its BMP.
            float beatTime = TwoPi * songInfo.BeatsPerMinute / 3600f;
            if (songInfo.HeadBobState == BPMHeadBobState.Half)
                beatTime *= 0.5f;
            if (songInfo.HeadBobState == BPMHeadBobState.Quarter)
                beatTime *= 0.25f;

            headRotationTime += beatTime;
            player.headRotation = Sin(headRotationTime) * 0.276f;
            player.eyeHelper.BlinkBecausePlayerGotHurt();
            return headRotationTime;
        }
    }
}
