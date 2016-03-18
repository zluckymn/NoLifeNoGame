using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yinhe.WebReference.AuthorityService;

namespace Yinhe.WebReference
{
  public  class SSWebService
    {
      public AuthorityService.authorityService ws = new authorityService();

      public string GetToken(string loginName, string passWord)
      {
          UserToken token = ws.authenticate(loginName, passWord);
          return token.id;

      }



    }
}
