using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.UI.BossHealthBarManager;
using static InfernumMode.Content.BossBars.BossBarManager;

namespace InfernumMode.Content.BossBars
{
    // A lot of this is yoinked from BossHPUI in base Calamity, considering they function pretty much the same internally.
    public class BaseBossBar
    {
        #region Fields/Properties
        public int NPCIndex = -1;		

		public int EnrageTimer;

        public int IncreasingDefenseOrDRTimer;

		public int OpenAnimationTimer;

		public int CloseAnimationTimer;

		public long InitialMaxLife;

		public long PreviousLife;

		/// <summary>
		/// The type of the NPC this bar is indended for.
		/// </summary>
		public int IntendedNPCType;

		/// <summary>
		/// The Display Name of the NPC this bar is indended for.
		/// </summary>
		public string OverridingName;

		/// <summary>
		/// The draw color for the border of the bar.
		/// </summary>
		public Color BarBorderColor;

		/// <summary>
		/// The draw color for the main bar.
		/// </summary>
		public Color BarMainColor;

		/// <summary>
		/// The draw color for the background bar.
		/// </summary>
		public Color BarBackgroundColor;

		/// <summary>
		/// The texture for the bar shader to use.
		/// </summary>
		public string BarOverlayTexturePath;

		public NPC AssociatedNPC
        {
            get
            {
                if (!Main.npc.IndexInRange(NPCIndex))
                    return null;
                return Main.npc[NPCIndex];
            }
        }

        public int NPCType => AssociatedNPC?.type ?? (-1);

        public long CombinedNPCLife
        {
            get
            {
                if (AssociatedNPC == null || !AssociatedNPC.active)
                    return 0L;

                long life = AssociatedNPC.life;
                foreach (KeyValuePair<NPCSpecialHPGetRequirement, NPCSpecialHPGetFunction> requirement in SpecialHPRequirements)
                    if (requirement.Key(AssociatedNPC))
                        return requirement.Value(AssociatedNPC, checkingForMaxLife: false);
                if (!OneToMany.ContainsKey(NPCType))
                    return life;

                for (int i = 0; i < 200; i++)
                    if (Main.npc[i].active && Main.npc[i].life > 0 && OneToMany[NPCType].Contains(Main.npc[i].type))
                        life += Main.npc[i].life;

                return life;
            }
        }

		public long CombinedNPCMaxLife
		{
			get
			{
				if (AssociatedNPC == null || !AssociatedNPC.active)
					return 0L;

				long maxLife = AssociatedNPC.lifeMax;

				foreach (KeyValuePair<NPCSpecialHPGetRequirement, NPCSpecialHPGetFunction> requirement in SpecialHPRequirements)
					if (requirement.Key(AssociatedNPC))
						return requirement.Value(AssociatedNPC, checkingForMaxLife: true);

				if (!OneToMany.ContainsKey(NPCType))
					return maxLife;

				for (int i = 0; i < 200; i++)
					if (Main.npc[i].active && Main.npc[i].life > 0 && OneToMany[NPCType].Contains(Main.npc[i].type))
						maxLife += Main.npc[i].lifeMax;

				return maxLife;
			}
		}

		public bool NPCIsEnraged
		{
			get
			{
				if (AssociatedNPC == null || !AssociatedNPC.active)
					return false;
				if (AssociatedNPC.Calamity().CurrentlyEnraged)
					return true;
				if (!OneToMany.ContainsKey(NPCType))
					return false;
				for (int i = 0; i < 200; i++)
					if (Main.npc[i].active && Main.npc[i].life > 0 && OneToMany[NPCType].Contains(Main.npc[i].type) && Main.npc[i].Calamity().CurrentlyEnraged)
						return true;
				return false;
			}
		}

		public bool NPCIsIncreasingDefenseOrDR
		{
			get
			{
				if (AssociatedNPC == null || !AssociatedNPC.active)
				{
					return false;
				}
				if (AssociatedNPC.Calamity().CurrentlyIncreasingDefenseOrDR)
				{
					return true;
				}
				if (!OneToMany.ContainsKey(NPCType))
				{
					return false;
				}
				for (int i = 0; i < 200; i++)
				{
					if (Main.npc[i].active && Main.npc[i].life > 0 && OneToMany[NPCType].Contains(Main.npc[i].type) && Main.npc[i].Calamity().CurrentlyIncreasingDefenseOrDR)
					{
						return true;
					}
				}
				return false;
			}
		}

		public float NPCLifeRatio
		{
			get
			{
				float lifeRatio = (float)CombinedNPCLife / (float)InitialMaxLife;
				if (float.IsNaN(lifeRatio) || float.IsInfinity(lifeRatio))
					return 0f;

				return lifeRatio;
			}
		}
		#endregion

		#region Statics

		#endregion

		#region Method
		public BaseBossBar(int index, string overridingName)
        {
			NPCIndex = index;
			if (AssociatedNPC != null && AssociatedNPC.active)
			{
				IntendedNPCType = AssociatedNPC.type;
				PreviousLife = CombinedNPCLife;
			}
			OverridingName = overridingName;
        }

		public void Update()
        {
			PreviousLife = CombinedNPCLife;
			if (AssociatedNPC == null || !AssociatedNPC.active || NPCType != IntendedNPCType || AssociatedNPC.Calamity().ShouldCloseHPBar)
			{
				EnrageTimer = Utils.Clamp(EnrageTimer - 4, 0, 120);
				IncreasingDefenseOrDRTimer = Utils.Clamp(IncreasingDefenseOrDRTimer - 4, 0, 120);
				CloseAnimationTimer = Utils.Clamp(CloseAnimationTimer + 1, 0, 120);
				return;
			}

			OpenAnimationTimer = Utils.Clamp(OpenAnimationTimer + 1, 0, 80);
			EnrageTimer = Utils.Clamp(EnrageTimer + NPCIsEnraged.ToDirectionInt(), 0, 120);

			IncreasingDefenseOrDRTimer = Utils.Clamp(IncreasingDefenseOrDRTimer + NPCIsIncreasingDefenseOrDR.ToDirectionInt(), 0, 120);
			if (CombinedNPCMaxLife != 0L && (InitialMaxLife == 0L || InitialMaxLife < CombinedNPCMaxLife))
				InitialMaxLife = CombinedNPCMaxLife;
		}

		public void Draw(SpriteBatch spriteBatch, int x, int y)
        {
			float animationCompletionRatio = 1;
			// Get the width of the bar.
			int mainBarWidth = (int)MathHelper.Min(400f * animationCompletionRatio, 400f * NPCLifeRatio);

			// Get a bunch of variables based on the specific boss.
			DetermineBossSpecificVariables(out BarMainColor, out BarBackgroundColor, out BarBorderColor, out BarOverlayTexturePath);

			// Draw the back bar.
			spriteBatch.Draw(MainBarTexture, new Rectangle(x, y + 4, 400, MainBarTexture.Height), BarBackgroundColor);
			// The main bar.

			spriteBatch.EnterShaderRegion();
			Asset<Texture2D> shaderTexture = ModContent.Request<Texture2D>(BarOverlayTexturePath);
			GameShaders.Misc["Infernum:RealityTear2"].SetShaderTexture(shaderTexture);
			GameShaders.Misc["Infernum:RealityTear2"].Apply();
			spriteBatch.Draw(MainBarTexture, new Rectangle(x, y + 4, mainBarWidth, MainBarTexture.Height), BarMainColor);

			spriteBatch.ExitShaderRegion();
			// The border.
			spriteBatch.Draw(EdgeBorderTexture, new Rectangle(x - 4, y, EdgeBorderTexture.Width, EdgeBorderTexture.Height), BarBorderColor);
			spriteBatch.Draw(MainBorderTexture, new Rectangle(x, y, 400, MainBorderTexture.Height), BarBorderColor);
        }

		public void DetermineBossSpecificVariables(out Color mainBarColor, out Color backBarColor, out Color borderBarColor, out string shaderTexturePath)
        {
            switch (AssociatedNPC.type)
            {
				case NPCID.KingSlime:
					mainBarColor = Color.DeepSkyBlue;
					backBarColor = new(50, 39, 5);
					borderBarColor = Color.Gold;
					shaderTexturePath = "InfernumMode/BossBars/Textures/BlobbyNoise";
					return;
				default:
					mainBarColor = Color.Orange;
					backBarColor = Color.Crimson;
					borderBarColor = Color.Silver;
					shaderTexturePath = "InfernumMode/BossBars/Textures/BlobbyNoise";
					break;
            }
        }
        #endregion
    }
}
