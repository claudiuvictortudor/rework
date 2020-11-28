using System;

namespace AutomaticWip.Contracts
{
    /// <summary>
    /// Helper class for compile statements, which allow prettier code for compile clauses
    /// </summary>
    public sealed class Compile
    {
        /// <summary>
        /// Will throw a <see cref="InvalidOperationException"/> if the assertion
        /// is true, with the specificied message.
        /// </summary>
        /// <param name="assertion">if set to <c>true</c> [assertion].</param>
        /// <param name="message">The message.</param>
        /// <example>
        /// Sample usage:
        /// <code>
        /// <![CDATA[
        /// Guard.Against(string.IsNullOrEmpty(name), "Name must have a value");
        /// ]]>
        /// </code>
        /// </example>
        public static void Against(bool assertion, string message, Action @callback = null)
        {
            if (!assertion)
                return;
            try
            {
                callback?.Invoke();
            }
            catch { }
            finally
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Will throw exception of type <typeparamref name="TException"/>
        /// with the specified message if the assertion is true
        /// </summary>
        /// <typeparam name="TException"></typeparam>
        /// <param name="assertion">if set to <c>true</c> [assertion].</param>
        /// <param name="message">The message.</param>
        /// <example>
        /// Sample usage:
        /// <code>
        /// <![CDATA[
        /// Guard.Against<ArgumentException>(string.IsNullOrEmpty(name), "Name must have a value");
        /// ]]>
        /// </code>
        /// </example>
        public static void Against<TException>(bool assertion, string message, Action @callback = null)
            where TException : Exception
        {
            if (!assertion)
                return;

            try
            {
                callback?.Invoke();
            }
            catch { }
            finally
            {
                throw (TException)Activator.CreateInstance(typeof(TException), message);
            }
        }

        /// <summary>
        /// Will throw a <see cref="InvalidOperationException"/> if the assertion
        /// is true, with the specificied message.
        /// </summary>
        /// <param name="assertion">if set to <c>true</c> [assertion].</param>
        /// <param name="message">The message.</param>
        /// <example>
        /// Sample usage:
        /// <code>
        /// <![CDATA[
        /// Guard.Against(string.IsNullOrEmpty(name), "Name must have a value");
        /// ]]>
        /// </code>
        /// </example>
        public static void Against(bool assertion, Func<string> message, Action @callback = null)
        {
            if (!assertion)
                return;
            try
            {
                callback?.Invoke();
            }
            catch { }
            finally
            {
                throw new InvalidOperationException(message());
            }
        }

        /// <summary>
        /// Will throw exception of type <typeparamref name="TException"/>
        /// with the specified message if the assertion is true
        /// </summary>
        /// <typeparam name="TException"></typeparam>
        /// <param name="assertion">if set to <c>true</c> [assertion].</param>
        /// <param name="message">The message.</param>
        /// <example>
        /// Sample usage:
        /// <code>
        /// <![CDATA[
        /// Guard.Against<ArgumentException>(string.IsNullOrEmpty(name), "Name must have a value");
        /// ]]>
        /// </code>
        /// </example>
        public static void Against<TException>(bool assertion, Func<string> message, Action @callback = null)
            where TException : Exception
        {
            if (!assertion)
                return;

            try
            {
                callback?.Invoke();
            }
            catch { }
            finally
            {
                throw (TException)Activator.CreateInstance(typeof(TException), message());
            }
        }
    }
}
