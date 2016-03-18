using System;
using System.Security.Principal;

namespace Yinhe.ProcessingCenter
{
    /// <summary>
    /// 用户凭证处理类
    /// </summary>
    public class Principal : IPrincipal
    {
        #region IPrincipal Members

        /// <summary>
        ///                     Determines whether the current principal belongs to the specified role.
        /// </summary>
        /// <returns>
        /// true if the current principal is a member of the specified role; otherwise, false.
        /// </returns>
        /// <param name="role">
        ///                     The name of the role for which to check membership. 
        ///                 </param>
        public bool IsInRole(string role) { throw new NotImplementedException(); }

        /// <summary>
        ///                     Gets the identity of the current principal.
        /// </summary>
        /// <returns>
        ///                     The <see cref="T:System.Security.Principal.IIdentity" /> object associated with the current principal.
        /// </returns>
        public IIdentity Identity { get; set; }

        #endregion
    }
}