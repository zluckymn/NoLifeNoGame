<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<HandleErrorInfo>" %>
 
<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    AboutAlert
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="HeadContent" runat="server">
    <link href="/Content/css/common/common.css" rel="stylesheet" type="text/css" />
    
 </asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
 
    <div class="erro">
        <table width="100%">
            <tr>
                <td width="200" align="center" valign="top"></td>
                <td valign="top" style="padding:20px 0">
                    <div style="margin:15px 0 30px 0; line-height:20px"></div>
                    <div style="color:red; line-height:25px">误操作！请联系技术支持工程师,电话13600911514</div></td>
            </tr>
        </table>

    </div>
    
  
</asp:Content>
 