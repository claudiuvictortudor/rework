using System.Collections.Generic;

namespace AutomaticWip.Web.Business.DataModels
{
    /// <summary>
    /// User Model
    /// </summary>
    public sealed class User
    {
        /// <summary>
        /// Initialize a new <see cref="User"/>
        /// </summary>
        public User(string user)
        {
            Key = user;
            Permissions = new List<string>();
        }

        /// <summary>
        /// The user id
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Collection of permissions
        /// </summary>
        public List<string> Permissions { get; }
    }
}
