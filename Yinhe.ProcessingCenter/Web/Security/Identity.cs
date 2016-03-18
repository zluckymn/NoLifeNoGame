using System.Security.Principal;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 用户信息标识
    /// </summary>
    public class Identity : IIdentity
    {
        #region IIdentity Members

        /// <summary>
        ///                     Gets the name of the current user.
        /// </summary>
        /// <returns>
        ///                     The name of the user on whose behalf the code is running.
        /// </returns>
        public string Name { get; set; }

        /// <summary>
        ///                     Gets the type of authentication used.
        /// </summary>
        /// <returns>
        ///                     The type of authentication used to identify the user.
        /// </returns>
        public string AuthenticationType { get; set; }

        /// <summary>
        ///                     Gets a value that indicates whether the user has been authenticated.
        /// </summary>
        /// <returns>
        /// true if the user was authenticated; otherwise, false.
        /// </returns>
        public bool IsAuthenticated { get; set; }

        #endregion

    }
}