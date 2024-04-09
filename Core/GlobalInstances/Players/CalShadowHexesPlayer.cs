using System.Collections.Generic;
using System.Linq;
using InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow;
using InfernumMode.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class CalShadowHexesPlayer : ModPlayer
    {
        public class HexStatus
        {
            public int BuffID
            {
                get;
                set;
            }

            public bool IsActive
            {
                get;
                set;
            }

            public float Intensity
            {
                get;
                set;
            }

            public Color HexColor
            {
                get;
                set;
            }

            public HexStatus(int buffID, Color hexColor, bool isActive = false, float intensity = 0f)
            {
                BuffID = buffID;
                HexColor = hexColor;
                IsActive = isActive;
                Intensity = intensity;
            }
        }

        internal Dictionary<string, HexStatus> HexStatuses = new()
        {
            ["Zeal"] = new(ModContent.BuffType<ZealHex>(), Color.Lerp(Color.Cyan, Color.Lime, 0.36f)),
            ["Accentuation"] = new(ModContent.BuffType<AccentuationHex>(), Color.Lerp(Color.Red, Color.Yellow, 0.64f)),
            ["Catharsis"] = new(ModContent.BuffType<CatharsisHex>(), Color.Lerp(Color.Red, Color.HotPink, 0.68f)),
            ["Weakness"] = new(ModContent.BuffType<WeaknessHex>(), Color.Lerp(Color.Orange, Color.DarkSlateGray, 0.55f)),
            ["Indignation"] = new(ModContent.BuffType<IndignationHex>(), Color.Red),
        };

        public int TotalActiveHexes => HexStatuses.Count(h => h.Value.IsActive);

        public bool HexIsActive(string key) => HexStatuses.TryGetValue(key, out HexStatus status) && (status.IsActive || status.Intensity > 0f);

        public void ActivateHex(string key)
        {
            if (HexStatuses.TryGetValue(key, out HexStatus status))
                status.IsActive = true;
        }

        #region Reset Effects
        public override void ResetEffects()
        {
            foreach (HexStatus status in HexStatuses.Values)
            {
                status.Intensity = Clamp(status.Intensity - 0.02f, 0f, 1f);
                status.IsActive = false;
            }
        }
        #endregion Reset Effects

        #region Life Regen
        public override void UpdateBadLifeRegen()
        {
            if (HexIsActive("Catharsis") && Player.lifeRegen >= 1)
            {
                Player.lifeRegen = 0;
                Player.lifeRegenTime = 0;
            }
        }
        #endregion Life Regen

        #region Updating
        public override void PostUpdate()
        {
            if (HexIsActive("Weakness"))
            {
                Player.statDefense -= 35;
                Player.endurance = Clamp(Player.endurance * 0.5f, 0f, 0.25f);
            }

            // Apply indicator buffs if the player has a hex.
            foreach (HexStatus status in HexStatuses.Values)
            {
                if (status.IsActive || status.Intensity > 0.75f)
                    Player.AddBuff(status.BuffID, 2);
            }
        }
        #endregion Updating

        #region Draw Effects
        public void DrawAllHexes()
        {
            float hoverOffsetFactor = 1f / TotalActiveHexes;
            if (TotalActiveHexes <= 1f)
                hoverOffsetFactor = 0f;

            // Update hex visual intensities.
            float offset = -20f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Texture2D backglowTexture = ModContent.Request<Texture2D>("CalamityMod/Skies/XerocLight").Value;
            foreach (HexStatus status in HexStatuses.Values)
            {
                if (status.IsActive)
                {
                    status.Intensity = Clamp(status.Intensity + 0.1f, 0f, 1f);
                    Main.spriteBatch.Draw(backglowTexture, Player.Center + Vector2.UnitY * (offset + 4f) - Main.screenPosition, null, status.HexColor * status.Intensity * 0.8f, 0f, backglowTexture.Size() * 0.5f, new Vector2(0.27f, 0.15f) * status.Intensity, 0, 0f);
                    CalamitasShadowBehaviorOverride.DrawHexOnTarget(Player, status.HexColor, offset * hoverOffsetFactor, status.Intensity);
                    offset += 40f;
                }
            }
            Main.spriteBatch.ExitShaderRegion();
        }
        #endregion Draw Effects
    }
}
