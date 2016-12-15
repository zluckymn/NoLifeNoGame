<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/BootStrap.Master"  Inherits="System.Web.Mvc.ViewPage" %>
<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="HeadContent" runat="server">

</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
 
<div class="container">

      <div class="page-header">
        <h1><strong>奶茶妹专用试题库</strong></h1>
        </div>

<%
    string WorkPlanManageConnectionString = System.Web.Configuration.WebConfigurationManager.AppSettings["WorkPlanManageConnectionString"];
    DataOperation dataOp = new DataOperation(WorkPlanManageConnectionString, true);
    var allLibList=dataOp.FindAll("WPM_QuestionLibrary").ToList();
     %>
<br />
<div class="row">
   
     
<%foreach (var library in allLibList)
  { %>
  <div class="col-lg-4">
   <a href="/QuestionAnswer/QuestionListDetail?libId=<%=library.Text("libId") %>"><%=library.Text("name")%>
  </div>
  <%} %>
   </div>
  </div>
    <script>

        $(document).ready(function () {
            //            loadMenu(228);
            //            initAllBoxs();
            SetMenu(1);
            loadCondition();
            
         
        });
 
         
    </script>
 </asp:Content>