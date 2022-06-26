using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Terraria;

namespace InfernumMode.OverridingSystem
{
    public abstract class NPCBehaviorOverride
    {
        internal static void LoadAll()
        {
            void getMethodBasedOnContext(Type type, NPCBehaviorOverride instance, NPCOverrideContext context)
            {
                string methodName = string.Empty;

                methodName = context switch
                {
                    NPCOverrideContext.NPCAI => "PreAI",
                    NPCOverrideContext.NPCSetDefaults => "SetDefaults",
                    NPCOverrideContext.NPCPreDraw => "PreDraw",
                    NPCOverrideContext.NPCFindFrame => "FindFrame",
                    _ => throw new ArgumentException("The given override context is invalid."),
                };
                MethodInfo method = type.GetMethod(methodName, Utilities.UniversalBindingFlags);
                List<Type> paramTypes = method.GetParameters().Select(parameter => parameter.ParameterType).ToList();
                paramTypes.Add(method.ReturnType);

                Type delegateType = Expression.GetDelegateType(paramTypes.ToArray());
                Delegate methodAsDelegate = Delegate.CreateDelegate(delegateType, instance, method);

                // Cache the delegate in question with intent for it to override the base AI.
                switch (context)
                {
                    case NPCOverrideContext.NPCAI:
                        OverridingListManager.InfernumNPCPreAIOverrideList[instance.NPCOverrideType] = new OverridingListManager.NPCPreAIDelegate(n => (bool)method.Invoke(instance, new object[] { n }));
                        break;
                    case NPCOverrideContext.NPCSetDefaults:
                        OverridingListManager.InfernumSetDefaultsOverrideList[instance.NPCOverrideType] = methodAsDelegate;
                        break;
                    case NPCOverrideContext.NPCPreDraw:
                        OverridingListManager.InfernumPreDrawOverrideList[instance.NPCOverrideType] = new OverridingListManager.NPCPreDrawDelegate((n, s, c) => (bool)method.Invoke(instance, new object[] { n, s, c }));
                        break;
                    case NPCOverrideContext.NPCFindFrame:
                        OverridingListManager.InfernumFrameOverrideList[instance.NPCOverrideType] = methodAsDelegate;
                        break;
                }
            }

            foreach (Type type in Utilities.GetEveryMethodDerivedFrom(typeof(NPCBehaviorOverride), typeof(InfernumMode).Assembly))
            {
                NPCBehaviorOverride instance = (NPCBehaviorOverride)Activator.CreateInstance(type);
                if (instance.ContentToOverride.HasFlag(NPCOverrideContext.NPCAI))
                    getMethodBasedOnContext(type, instance, NPCOverrideContext.NPCAI);
                if (instance.ContentToOverride.HasFlag(NPCOverrideContext.NPCSetDefaults))
                    getMethodBasedOnContext(type, instance, NPCOverrideContext.NPCSetDefaults);
                if (instance.ContentToOverride.HasFlag(NPCOverrideContext.NPCPreDraw))
                    getMethodBasedOnContext(type, instance, NPCOverrideContext.NPCPreDraw);
                if (instance.ContentToOverride.HasFlag(NPCOverrideContext.NPCFindFrame))
                    getMethodBasedOnContext(type, instance, NPCOverrideContext.NPCFindFrame);
            }
        }

        public abstract int NPCOverrideType { get; }
        public abstract NPCOverrideContext ContentToOverride { get; }
        public virtual void SetDefaults(NPC npc) { }
        public virtual bool PreAI(NPC npc) => true;
        public virtual bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor) => true;
        public virtual void FindFrame(NPC npc, int frameHeight) { }
    }
}
