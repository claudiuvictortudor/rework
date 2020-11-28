using AutomaticWip.Contracts;
using AutomaticWip.Web.Business.DataModels;
using System;
using System.Collections.Generic;

namespace AutomaticWip.Web.Business.Security
{
    public static class UAManager
    {
        /// <summary>
        /// Action:permission
        /// </summary>
        static readonly IDictionary<string, string> Rules = new Dictionary<string, string>();

        /// <summary>
        /// Administrator permission
        /// </summary>
        const string Admin = "$$";

        /// <summary>
        /// Configure rules 
        /// </summary>
        public static void ConfigureRules(string action, string permission)
        {
            lock (Rules)
            {
                Rules.Add(action, permission);
            }
        }

        /// <summary>
        /// Validates if user can perform an action
        /// </summary>
        /// <param name="permissions">User's permissions</param>
        /// <param name="action">Action to execute</param>
        /// <returns>True if user can execute</returns>
        public static bool Validate(User user, string action)
        {
            var permission = "";
            if (user.Permissions.Contains(nameof(Admin)))
                return true;

            lock (Rules)
            {
                Compile.Against<InvalidOperationException>(!Rules.ContainsKey(action), $"Unknown action: {action}");
                permission = Rules[action];
            }

            return user.Permissions.Contains(permission);
        }

        /// <summary>
        /// 3rd party login tool
        /// </summary>
        /// <param name="user">User to login</param>
        /// <param name="permissions">Permissions of logged user</param>
        /// <returns></returns>
        public static bool Login(this User user)
        {
            user.Permissions.Add(nameof(Admin));
            return true;
        }
    }
}
