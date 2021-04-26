using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Terraria.ModLoader;

namespace InfernumMode.OverridingSystem
{
	public enum EntityOverrideContext
    {
        NPCAI,
        NPCSetDefaults,
        NPCPreDraw,
        NPCFindFrame,
        ProjectileAI
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class OverrideAppliesToAttribute : Attribute
    {
        public OverrideAppliesToAttribute(int npcTypeID, Type methodType, string methodName, EntityOverrideContext overrideContext, bool shouldNotUse = false)
        {
            if (shouldNotUse)
                return;

            MethodInfo method = methodType.GetMethod(methodName, Utilities.UniversalBindingFlags);
            var paramTypes = method.GetParameters().Select(parameter => parameter.ParameterType).ToList();
            paramTypes.Add(method.ReturnType);

            Type delegateType = Expression.GetDelegateType(paramTypes.ToArray());

            Delegate methodAsDelegate = Delegate.CreateDelegate(delegateType, method);

            switch (overrideContext)
            {
                case EntityOverrideContext.NPCAI:
                    OverridingListManager.InfernumNPCPreAIOverrideList[npcTypeID] = methodAsDelegate;
                    break;
                case EntityOverrideContext.NPCSetDefaults:
                    OverridingListManager.InfernumSetDefaultsOverrideList[npcTypeID] = methodAsDelegate;
                    break;
                case EntityOverrideContext.NPCPreDraw:
                    OverridingListManager.InfernumPreDrawOverrideList[npcTypeID] = methodAsDelegate;
                    break;
                case EntityOverrideContext.NPCFindFrame:
                    OverridingListManager.InfernumFrameOverrideList[npcTypeID] = methodAsDelegate;
                    break;
                case EntityOverrideContext.ProjectileAI:
                    OverridingListManager.InfernumProjectilePreAIOverrideList[npcTypeID] = methodAsDelegate;
                    break;
            }
        }

        public OverrideAppliesToAttribute(string calamityEntityName, Type methodType, string methodName, EntityOverrideContext overrideContext, bool shouldNotUse = false) :
            this(ModLoader.GetMod("CalamityMod").NPCType(calamityEntityName), methodType, methodName, overrideContext, shouldNotUse)
        { }
    }
}
