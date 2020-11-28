namespace AutomaticWip.Contracts
{
    public interface ITypeHandler
    {
        /// <summary>
        /// Generic conversion
        /// </summary>
        /// <param name="value">The object's value</param>
        /// <returns>Converted object</returns>
        object Request(object value);
    }
}
