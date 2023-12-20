using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.OverridingSystem
{
    public abstract class NPCBehaviorOverride
    {
        #region Statics
        internal static Dictionary<int, NPCBehaviorOverride> BehaviorOverrides = new();

        internal static void LoadAll()
        {
            BehaviorOverrides = new();

            foreach (Type type in Utilities.GetEveryTypeDerivedFrom(typeof(NPCBehaviorOverride), typeof(InfernumMode).Assembly))
            {
                NPCBehaviorOverride instance = (NPCBehaviorOverride)Activator.CreateInstance(type);

                // Check that all the methods exist, and add the npc type to the list if so.
                MethodInfo preAIMethod = type.GetMethod("PreAI", Utilities.UniversalBindingFlags);
                if (preAIMethod is not null)
                    OverridingListManager.InfernumNPCPreAIOverrideList.Add(instance.NPCOverrideType);

                MethodInfo setDefaultsMethod = type.GetMethod("SetDefaults", Utilities.UniversalBindingFlags);
                if (setDefaultsMethod is not null && setDefaultsMethod.DeclaringType != typeof(NPCBehaviorOverride))
                    OverridingListManager.InfernumSetDefaultsOverrideList.Add(instance.NPCOverrideType);

                MethodInfo preDrawMethod = type.GetMethod("PreDraw", Utilities.UniversalBindingFlags);
                if (preDrawMethod is not null && preDrawMethod.DeclaringType != typeof(NPCBehaviorOverride))
                    OverridingListManager.InfernumPreDrawOverrideList.Add(instance.NPCOverrideType);

                MethodInfo findFrameMethod = type.GetMethod("FindFrame", Utilities.UniversalBindingFlags);
                if (findFrameMethod is not null && findFrameMethod.DeclaringType != typeof(NPCBehaviorOverride))
                    OverridingListManager.InfernumFrameOverrideList.Add(instance.NPCOverrideType);

                MethodInfo checkDeadMethod = type.GetMethod("CheckDead", Utilities.UniversalBindingFlags);
                if (checkDeadMethod is not null && checkDeadMethod.DeclaringType != typeof(NPCBehaviorOverride))
                    OverridingListManager.InfernumCheckDeadOverrideList.Add(instance.NPCOverrideType);

                // Call the load hook.
                instance.Load();

                BehaviorOverrides[instance.NPCOverrideType] = instance;
            }
        }

        internal static void LoadPhaseIndicators()
        {
            foreach (int npcID in BehaviorOverrides.Keys)
            {
                NPCBehaviorOverride instance = BehaviorOverrides[npcID];
                float[] phaseThresholds = instance.PhaseLifeRatioThresholds;
                if (!Main.dedServ && InfernumMode.PhaseIndicator != null && phaseThresholds.Length >= 1)
                {
                    foreach (float lifeRatio in phaseThresholds)
                        InfernumMode.PhaseIndicator.Call(0, npcID, (NPC npc, float difficulty) => lifeRatio);
                }

                TipsManager.TipsRegistry[instance.NPCOverrideType] = instance.GetTips().ToList();
            }
        }
        #endregion

        #region Abstracts/Virtuals
        /// <summary>
        /// The type of the NPC that this should run for.
        /// </summary>
        public abstract int NPCOverrideType { get; }

        /// <summary>
        /// The type of the NPC whos tips should be used instead of the current NPCs. Defaults to null.
        /// </summary>
        public virtual int? NPCTypeToDeferToForTips => null;

        /// <summary>
        /// An array of hp thresholds, for use by the custom boss bar to mark phases.
        /// </summary>
        public virtual float[] PhaseLifeRatioThresholds => Array.Empty<float>();

        /// <summary>
        /// Whether the NPC should use the boss immunity cooldown slot. Defaults to true.
        /// </summary>
        public virtual bool UseBossImmunityCooldownID => true;

        /// <summary>
        /// Use this to perform one-time loading tasks.
        /// </summary>
        public virtual void Load() { }

        /// <summary>
        /// Use this to set custom defaults for the npc. This runs after every other mod's.
        /// </summary>
        /// <param name="npc">The NPC</param>
        public virtual void SetDefaults(NPC npc) { }

        /// <summary>
        /// Use this to perform custom behavior for the NPC. Return false to stop <see cref="NPC.AI"/> from running. Returns true by default.
        /// </summary>
        /// <param name="npc">The NPC</param>
        /// <returns>Whether <see cref="NPC.AI"/> should run.</returns>
        public virtual bool PreAI(NPC npc) => true;

        /// <summary>
        /// Use this to send any extra data, for syncing between clients.
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="writer"></param>
        public virtual void SendExtraData(NPC npc, ModPacket writer) { }

        /// <summary>
        /// Use this to read any extra data, for syncing between clients.
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="reader"></param>
        public virtual void ReceiveExtraData(NPC npc, BinaryReader reader) { }

        /// <summary>
        /// Use this to set the NPCs current frame.
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="frameHeight"></param>
        public virtual void FindFrame(NPC npc, int frameHeight) { }

        /// <summary>
        /// Use this to perform custom drawing for the NPC. Return false to stop the game drawing the NPC as well. Returns true by default.
        /// </summary>
        /// <param name="npc">The NPC.</param>
        /// <param name="spriteBatch">The spritebatch to draw with.</param>
        /// <param name="lightColor">The light color at the NPC's center</param>
        /// <returns></returns>
        public virtual bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor) => true;

        /// <summary>
        /// Whether or not to run the code for checking whether an NPC will remain active. Return false to stop the NPC from being despawned
        /// and to stop the NPC from counting towards the limit for how many NPCs can exist near a player. Returns true by default.
        /// </summary>
        /// <param name="npc">The NPC.</param>
        /// <returns></returns>
        public virtual bool CheckDead(NPC npc) => true;

        /// <summary>
        /// Use this to provide a list of tips to display on player death with the blasted tophat.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<Func<NPC, string>> GetTips() => Array.Empty<Func<NPC, string>>();
        #endregion
    }
}
