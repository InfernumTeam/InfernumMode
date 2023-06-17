using CalamityMod.NPCs;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Core.TrackedMusic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas.SupremeCalamitasBehaviorOverride;

namespace InfernumMode.Content.Skies
{
    public class SCalScreenShaderData : ScreenShaderData
    {
        public static Color BackgroundColor
        {
            get;
            set;
        }

        public static float MusicBrightness
        {
            get;
            set;
        }

        public static Color GriefColor => new(238, 58, 58);

        public static Color LamentColor => new(33, 158, 248);

        public static Color EpiphanyColor => Color.Lerp(Color.Yellow, Color.Red, 0.56f);

        public static Color AcceptanceColor => new(78, 78, 78);

        public SCalScreenShaderData(Ref<Effect> shader, string passName) : base(shader, passName) { }

        public override void Apply()
        {
            // If scal is not present do not draw.
            if (CalamityGlobalNPC.SCal < 0)
            {
                BackgroundColor = GriefColor;
                return;
            }

            if (BackgroundColor == Color.Transparent)
                BackgroundColor = GriefColor;

            UseTargetPosition(Main.LocalPlayer.Center);
            UseColor(BackgroundColor);

            NPC scal = Main.npc[CalamityGlobalNPC.SCal];
            float lifeRatio = scal.life / (float)scal.lifeMax;
            float brightness = 1f;
            bool acceptancePhase = scal.Infernum().ExtraAI[4] == 4f && scal.ai[0] == (int)SCalAttackType.DesperationPhase;

            // Make the brightness dissipate.
            MusicBrightness = Clamp(MusicBrightness - 0.026f, 0f, 3f);

            if (acceptancePhase)
                BackgroundColor = Color.Lerp(BackgroundColor, AcceptanceColor, 0.1f);
            else
            {
                if (InfernumMode.MusicModIsActive)
                    ChangeBackgroundColors_ScarsOfCalamity();
                else
                    ChangeBackgroundColors_StainedBrutalCalamity(lifeRatio, ref brightness);
            }

            // Perform various matrix calculations to transform SCal's arena to UV coordinate space.
            Rectangle arena = scal.Infernum().Arena;
            Vector4 uvScaledArena = new(arena.X, arena.Y - 6f, arena.Width + 8f, arena.Height + 14f);
            uvScaledArena.X -= Main.screenPosition.X;
            uvScaledArena.Y -= Main.screenPosition.Y;
            Vector2 downscaleFactor = new(Main.screenWidth, Main.screenHeight);
            Matrix toScreenCoordsTransformation = Main.GameViewMatrix.TransformationMatrix;
            Vector2 coordinatePart = Vector2.Transform(new Vector2(uvScaledArena.X, uvScaledArena.Y), toScreenCoordsTransformation) / downscaleFactor;
            Vector2 areaPart = Vector2.Transform(new Vector2(uvScaledArena.Z, uvScaledArena.W), toScreenCoordsTransformation with
            {
                M41 = 0f,
                M42 = 0f
            }) / downscaleFactor;
            uvScaledArena = new Vector4(coordinatePart.X, coordinatePart.Y, areaPart.X, areaPart.Y);

            Shader.Parameters["uvArenaArea"].SetValue(uvScaledArena);
            UseImage(InfernumTextureRegistry.GrayscaleWater.Value, 0, SamplerState.AnisotropicWrap);

            UseOpacity(0.36f);
            UseIntensity(brightness + MusicBrightness * 2f);
            base.Apply();
        }

        public static void ChangeBackgroundColors_ScarsOfCalamity()
        {
            // Make the backgrounds change based on who's singing in the Scars of Calamity track.
            var songTime = TrackedMusicManager.SongElapsedTime;
            var solaria = CalamitasTrackedMusic.SolariaSections.Where(m => m.End >= songTime);
            var mai = CalamitasTrackedMusic.MaiSections.Where(m => m.End >= songTime);
            bool solariaIsSinging = solaria.Any() && songTime >= solaria.First().Start && songTime <= solaria.First().End;
            bool maiIsSinging = mai.Any() && songTime >= mai.First().Start && songTime <= mai.First().End;

            // Make the brightnmess gradually increase as the song goes on.
            float idealBrightness = Lerp(1f, 1.5f, (float)(songTime.TotalSeconds / TrackedMusicManager.TrackedSong.Duration.TotalSeconds));
            MusicBrightness = Lerp(MusicBrightness, idealBrightness, 0.09f);

            // If Solaria is singing, use a red background color.
            // If Mai is singing, use a blue background color.
            // If both are singing, use a mixture of both colors.
            // If neither are singing, use an orange color.
            Color idealBackgroundColor = EpiphanyColor;
            if (maiIsSinging && solariaIsSinging)
                idealBackgroundColor = Color.Purple;
            else if (maiIsSinging)
                idealBackgroundColor = LamentColor;
            else if (solariaIsSinging)
                idealBackgroundColor = Color.DarkRed;

            BackgroundColor = Color.Lerp(BackgroundColor, idealBackgroundColor, 0.1f);
        }

        public static void ChangeBackgroundColors_StainedBrutalCalamity(float lifeRatio, ref float brightness)
        {
            // Make the backgrounds change based on SCal's HP thresholds in accordance with the Stained Brutal Calamity track.
            if (lifeRatio <= Phase4LifeRatio)
            {
                BackgroundColor = Color.Lerp(BackgroundColor, EpiphanyColor, 0.1f);
                brightness = 2f;
            }
            else if (lifeRatio <= Phase3LifeRatio)
                BackgroundColor = Color.Lerp(BackgroundColor, LamentColor, 0.1f);

            // Incorporate the music into the background brightness.
            CheckForMusicIntensity_StainedBrutalCalamity();
        }

        public static void CheckForMusicIntensity_StainedBrutalCalamity()
        {
            static List<(TimeSpan, TimeSpan)> splitMusicPointsIntoSections(List<TimeSpan> musicPoints)
            {
                List<(TimeSpan, TimeSpan)> sections = new();
                for (int i = 0; i < musicPoints.Count - 1; i += 2)
                    sections.Add((musicPoints[i], musicPoints[i + 1]));

                return sections;
            };

            var songTime = TrackedMusicManager.SongElapsedTime;

            // Grief section.
            if (CalamityGlobalNPC.SCalGrief == CalamityGlobalNPC.SCal)
            {
                var grief = splitMusicPointsIntoSections(Grief_HighPoints).Where(m => m.Item2 >= songTime);
                if (grief.Any() && songTime >= grief.First().Item1 && songTime <= grief.First().Item2)
                    MusicBrightness = Clamp(MusicBrightness + 0.06f, 0f, 1f);
            }

            // Lament section.
            else if (CalamityGlobalNPC.SCalLament == CalamityGlobalNPC.SCal)
            {
                var lament = splitMusicPointsIntoSections(Lament_HighPoints).Where(m => m.Item2 >= songTime);
                if (lament.Any() && songTime >= lament.First().Item1 && songTime <= lament.First().Item2)
                    MusicBrightness = Clamp(MusicBrightness + 0.06f, 0f, 1f);
            }

            // Epiphany section.
            else if (CalamityGlobalNPC.SCalEpiphany == CalamityGlobalNPC.SCal && CalamityGlobalNPC.SCalAcceptance != CalamityGlobalNPC.SCal)
            {
                var epiphany = splitMusicPointsIntoSections(Epiphany_HighPoints.Select(s => s.Key).ToList()).Where(m => m.Item2 >= songTime);
                if (epiphany.Any() && songTime >= epiphany.First().Item1 && songTime <= epiphany.First().Item2)
                {
                    float maxBrightness = Epiphany_HighPoints[epiphany.First().Item1];
                    MusicBrightness = Clamp(MusicBrightness + 0.08f, 0f, maxBrightness);
                }
            }
        }
    }
}
