﻿<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <div>
    正在跳转登陆页,请稍后... .....
    </div>
  
    <script type="text/javascript">
        window.location.href = "<%=SysAppConfig.LoginUrl %>";
    </script>
</body>
</html>
