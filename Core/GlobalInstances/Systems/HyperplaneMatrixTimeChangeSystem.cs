using InfernumMode.Core.Netcode;
using InfernumMode.Core.Netcode.Packets;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class HyperplaneMatrixTimeChangeSystem : ModSystem
    {
        public static int? SoughtTime
        {
            get;
            set;
        }

        public static bool SeekingDayTime
        {
            get;
            set;
        }

        public static float BackgroundChangeInterpolant
        {
            get;
            set;
        }

        public override void ModifyTimeRate(ref double timeRate, ref double tileUpdateRate, ref double eventUpdateRate)
        {
            if (!SoughtTime.HasValue)
                return;

            timeRate = 300D;
            eventUpdateRate = 300D;
        }

        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            backgroundColor *= Lerp(1f, 0.4f, BackgroundChangeInterpolant);
        }

        public override void PreUpdateWorld()
        {
            bool fadeIn = SoughtTime.HasValue;
            BackgroundChangeInterpolant = Clamp(BackgroundChangeInterpolant + fadeIn.ToDirectionInt() * 0.05f, 0f, 1f);
            if (!SoughtTime.HasValue)
                return;

            // Make time move very, very fast.
            if (!Main.IsFastForwardingTime())
            {
                PacketManager.SendPacket<TimeChangeSystemPacket>();
                Main.sundialCooldown = 0;
                Main.Sundialing();
            }

            // Disable the effect if very close to the sought time.
            if (Distance(SoughtTime.Value, (int)Main.time) <= 320f && Main.dayTime == SeekingDayTime)
            {
                Main.time = SoughtTime.Value;
                SoughtTime = null;
                PacketManager.SendPacket<TimeChangeSystemPacket>();
                Main.UpdateTimeRate();
                Main.fastForwardTimeToDawn = false;
            }
        }

        public static void ApproachTime(int time, bool day)
        {
            SoughtTime = time;
            SeekingDayTime = day;
            if (day)
                Main.fastForwardTimeToDawn = false;
            else
                Main.fastForwardTimeToDusk = false;
        }
    }
}
