namespace InfernumMode.Core.ModCalls
{
    /// <summary>
    /// A <see cref="ModCall"/> that returns nothing.
    /// </summary>
    /// <typeparam name="ReturnType">The type of the value to return</typeparam>
    public abstract class GenericModCall : ModCall
    {
        protected sealed override object Process(params object[] argsWithoutCommand)
        {
            ProcessGeneric(argsWithoutCommand);
            return null;
        }

        protected abstract void ProcessGeneric(params object[] argsWithoutCommand);
    }
}
