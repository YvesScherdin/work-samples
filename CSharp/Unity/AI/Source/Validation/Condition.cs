namespace MageGame.AI.Validation
{
    /// <summary>
    /// Encapsulated premise.
    /// Stores parameters.
    /// Caches result for re-use.
    /// 
    /// </summary>
    public class Condition
    {
        /// <summary>
        /// Determines result of condition. Should cache the result.
        /// </summary>
        /// <returns></returns>
        virtual public bool Check() => false;

        /// <summary>
        /// Returns cached result.
        /// </summary>
        /// <returns></returns>
        virtual public bool IsMet() => false;
    }
}
