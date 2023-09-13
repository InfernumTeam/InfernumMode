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
                    OverridingListManager.InfernumNPCPreAIOverrideList.Add(instance.NPCOverrideType); // = new OverridingListManager.NPCPreAIDelegate(n => (bool)preAIMethod.Invoke(instance, new object[] { n }));

                MethodInfo setDefaultsMethod = type.GetMethod("SetDefaults", Utilities.UniversalBindingFlags);
                if (setDefaultsMethod is not null && setDefaultsMethod.DeclaringType != typeof(NPCBehaviorOverride))
                    OverridingListManager.InfernumSetDefaultsOverrideList.Add(instance.NPCOverrideType); //= setDefaultsMethod.ConvertToDelegate(instance);

                MethodInfo preDrawMethod = type.GetMethod("PreDraw", Utilities.UniversalBindingFlags);
                if (preDrawMethod is not null && preDrawMethod.DeclaringType != typeof(NPCBehaviorOverride))
                    OverridingListManager.InfernumPreDrawOverrideList.Add(instance.NPCOverrideType); //= new OverridingListManager.NPCPreDrawDelegate((n, s, c) => (bool)preDrawMethod.Invoke(instance, new object[] { n, s, c }));

                MethodInfo findFrameMethod = type.GetMethod("FindFrame", Utilities.UniversalBindingFlags);
                if (findFrameMethod is not null && findFrameMethod.DeclaringType != typeof(NPCBehaviorOverride))
                    OverridingListManager.InfernumFrameOverrideList.Add(instance.NPCOverrideType); //= new OverridingListManager.NPCFindFrameDelegate((n, h) => findFrameMethod.Invoke(instance, new object[] { n, h }));

                MethodInfo checkDeadMethod = type.GetMethod("CheckDead", Utilities.UniversalBindingFlags);
                if (checkDeadMethod is not null && checkDeadMethod.DeclaringType != typeof(NPCBehaviorOverride))
                    OverridingListManager.InfernumCheckDeadOverrideList.Add(instance.NPCOverrideType); //= new OverridingListManager.NPCCheckDeadDelegate(n => (bool)checkDeadMethod.Invoke(instance, new object[] { n }));

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

        public virtual void Load() { }

        public virtual void SendExtraData(NPC npc, ModPacket writer) { }

        public virtual void ReceiveExtraData(NPC npc, BinaryReader reader) { }

        public virtual int? NPCIDToDeferToForTips => null;

        public virtual float[] PhaseLifeRatioThresholds => Array.Empty<float>();

        public virtual IEnumerable<Func<NPC, string>> GetTips() => Array.Empty<Func<NPC, string>>();

        public abstract int NPCOverrideType { get; }

        public virtual void SetDefaults(NPC npc) { }

        public virtual bool PreAI(NPC npc) => true;

        public virtual bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor) => true;

        public virtual void FindFrame(NPC npc, int frameHeight) { }

        public virtual bool CheckDead(NPC npc) => true;
    }
}
