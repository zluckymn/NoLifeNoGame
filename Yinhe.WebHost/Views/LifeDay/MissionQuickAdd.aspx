<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/BootStrap_flatui.Master"  Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
生活日常
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="HeadContent" runat="server">
 
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
    <%
       
        
        string WorkPlanManageConnectionString = System.Web.Configuration.WebConfigurationManager.AppSettings["WorkPlanManageConnectionString"];
        DataOperation dataOp = new DataOperation(WorkPlanManageConnectionString, true);
        var weixin = PageReq.GetParam("weixin");
        var type = PageReq.GetParam("type");//0近一周  1近一个月  2近一年 
        var missionType = PageReq.GetParam("missionType");//0近一周  1近一个月  2近一年 
       
        var curMissionGroup = dataOp.FindAll("MissionTemplateGroup").ToList();
%>


      <%Html.RenderPartial("Nav"); %>
        
     <h4>任务分组</h4>
     <%Html.RenderPartial("UserInfo"); %>
     <div id="missionListDiv">
     
      <div class="row"  >
        <div class="col-sm-6 col-md-4" >
          <div class="todo">
              <small>请添加即将执行的任务</small><a href="javascript:;" onclick="history.back(-1)"><span style=" float:right" class="fui-cross"></span></a>
            <ul>
            <%foreach (var group in curMissionGroup)
              { %>
              <li onclick="GetGoupList('<%=group.Text("groupId") %>')">
                <div class="todo-icon fui-list"></div>
                <div class="todo-content">
                  <h4 class="todo-name">
                        <%=group.Text("name")%> <i  class="fui-arrow-right"></i>
                  </h4>
                      <%=group.Text("remark")%>
                </div>
              </li>
              <%} %>
             </ul>
          </div><!-- /.todo -->
        </div>
        <!-- /.col-md-4 -->
  </div>
  </div>
     <script>
           function GetGoupList(groupId) {
            window.location.href= "/LifeDay/MissionQuickGroupDetail?weixin=<%=weixin%>&missionType=<%=missionType %>&type=<%=type%>&groupId=" + groupId;
       }
       
    </script>
 </asp:Content>
