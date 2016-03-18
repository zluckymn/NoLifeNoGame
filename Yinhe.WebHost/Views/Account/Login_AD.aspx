<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <script src="/Scripts/Reference/Common/jquery-1.5.1.js" type="text/javascript"></script>
    <link href="/Content/css/client/xuhui/xuhui.css" rel="stylesheet" type="text/css" />
    <link href="/Content/css/common/common.css" rel="stylesheet" type="text/css" />
</head>
<% 
    string info = ViewData["info"] as string;
    
%>
<body class="body_sign">
    <%=info%>
</body>
</html>
