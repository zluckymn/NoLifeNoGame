<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<HandleErrorInfo>" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    AboutError
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
   <%
      var DefaultUrl = "/ProductDevelopXH/Index";
      var exception = Model != null ? Model.Exception : new Exception();
      #region 捕获各种异常 未处理
      
      #endregion
      var strError = exception != null ? exception.Message : "页面可能由于网络原因无法初始化，请联系管理员";%>
    <%  if (String.IsNullOrEmpty(strError) == false)
    {   %>
    <div class="erro">
        <table width="100%">
            <tr>
                <td width="200" align="center" valign="top"><img src="/Content/Images/zh-cn/Common/erropic.gif" width="129" height="134" /></td>
                <td valign="top" style="padding:20px 0">
                    <div style="margin:15px 0 30px 0; line-height:20px"><%=strError%></div>
                    <div style="color:#666666; line-height:25px"><a href="<%=DefaultUrl %>" class="gray">如果您的浏览器没有自动跳转，请点击这里</a></div></td>
            </tr>
        </table>

    </div>
   <script language="javascript" type="text/javascript">
       var defaultUrl = '<%= DefaultUrl %>';
       if (defaultUrl != "") {
           setTimeout("window.location.href='" + defaultUrl + "'", 10000);
       }
   </script>
 <% }
    else
    {  
        string url = string.Empty;
        if (Request.UrlReferrer != null)
        {
            url=Request.UrlReferrer.ToString();
        }
        string current = Request.Url.ToString();
        if (url == current)
        {
            url = DefaultUrl;
        }   %>
    <div class="erro">
        <table width="100%">
            <tr>
                <td width="200" align="center" valign="top"><img src="/Content/Images/zh-cn/Common/erropic.gif" width="129" height="134" /></td>
                <td valign="top" style="padding:20px 0"><div style="font-size:24px; font-family: 黑体; font-weight:bold; color:#404040">无法找到该页</div>
                <div style="margin:15px 0 30px 0; line-height:20px">出现未知的错误，可能您访问的页面不存在。</div>
                建议您：
                <div style="color:#666666; line-height:25px">1. 单击<a href="<%=url %>" class="orange2">返回</a>到上一个页面<br />
               </div></td>
            </tr>
        </table>
<%  } %>

   
</asp:Content>
 