namespace InfernumMode.Core.ModCalls
{
    /// <summary>
    /// A <see cref="ModCall"/> that returns a value.
    /// </summary>
    /// <typeparam name="ReturnType">The type of the value to return</typeparam>
    public abstract class ReturnValueModCall<ReturnType> : ModCall
    {
        protected sealed override object Process(params object[] argsWithoutCommand) => ProcessGeneric(argsWithoutCommand);

        protected abstract ReturnType ProcessGeneric(params object[] argsWithoutCommand);
    }
}
