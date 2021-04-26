using System;
using System.Reflection;

namespace InfernumMode.OverridingSystem
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class ExcludeBasedOnPropertyAttribute : Attribute
    {
        public ExcludeBasedOnPropertyAttribute(int npcTypeID, EntityOverrideContext exclusionDomain, Type propertyLocation, string propertyName, object propertyObjectReference = null)
        {
            PropertyInfo property = propertyLocation.GetProperty(propertyName, Utilities.UniversalBindingFlags);

            if (property.PropertyType != typeof(bool))
                throw new ArgumentException("An exclusion property must be a boolean in order to do exclusive checks.", nameof(property));

            if ((property.GetGetMethod().IsStatic && propertyObjectReference != null) || 
                (!property.GetGetMethod().IsStatic && propertyObjectReference is null))
                throw new ArgumentException("In order to access a property, an appropriate object reference (or lack thereof) is required.", nameof(propertyObjectReference));

			bool conditionGetter() => (bool)property.GetValue(propertyObjectReference);
			OverridingListManager.ExclusionList[new OverrideExclusionContext(npcTypeID, exclusionDomain)] = conditionGetter;
        }
        public ExcludeBasedOnPropertyAttribute(string calamityEntityName, EntityOverrideContext exclusionDomain, Type propertyLocation, string propertyName, object propertyObjectReference = null) :
           this(InfernumMode.CalamityMod.NPCType(calamityEntityName), exclusionDomain, propertyLocation, propertyName, propertyObjectReference)
		{

		}
    }
}
