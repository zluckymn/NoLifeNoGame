<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/BootStrap_flatui.Master"  Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
生活日常
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="HeadContent" runat="server">
 

 <%
     string WorkPlanManageConnectionString = System.Web.Configuration.WebConfigurationManager.AppSettings["WorkPlanManageConnectionString"];
     DataOperation dataOp = new DataOperation(WorkPlanManageConnectionString, true);
     var helper = new Yinhe.ProcessingCenter.LifeDay.LifeDayHelper(dataOp);
     var weixin = PageReq.GetParam("weixin");
     var groupId = PageReq.GetParam("groupId");
     var missionType = PageReq.GetParam("missionType");//任务类型
     var type = PageReq.GetParamInt("type");//库类型，任务或者周任务类型 1代表愿望单，欲望库类型
     var curUser = helper.InitialUserInfo(weixin);
     if (curUser == null) return;
     var missionQuery = Query.EQ("weixin", weixin);
    // var missionQuery = Query.EQ("weixin", weixin);

     var group = dataOp.FindOneByQuery("MissionTemplateGroup", Query.EQ("groupId", groupId));
     if (group == null) group = new BsonDocument();
     var hitAllTaskNames = dataOp.FindAllByQuery("MissionLibrary", missionQuery).SetFields("name", "missionTemplateId").ToList();
     var curGroupMissionIdsList = dataOp.FindAllByQuery("MissionTemplateGroupRelation", Query.EQ("groupId", groupId)).Select(c => (BsonValue)c.Text("missionTemplateId"));
     var curGroupMissionList = dataOp.FindAllByQuery("MissionTemplate", Query.In("missionTemplateId", curGroupMissionIdsList)).OrderByDescending(c => c.Int("userJoinCount")).ToList();
     %>
 <%Html.RenderPartial("Nav"); %>
  <h4><%=group.Text("name") %></h4>
 <%Html.RenderPartial("UserInfo"); %>
   <div class="row"  >
   <div class="col-sm-6 col-md-4" >
      <div class="todo">
           <small>请添加即将执行的任务</small><a href="javascript:;" onclick="history.back(-1)"><span style=" float:right" class="fui-cross"></span></a>
            <ul>
            <%foreach (var missionTemplate in curGroupMissionList)
              {
                  var isCheck = hitAllTaskNames.Exists(c => c.Text("name") == missionTemplate.Text("name") || c.Text("missionTemplateId") == missionTemplate.Text("_id"));
                %>
              <li onclick="SaveMission('<%=missionTemplate.Text("_id") %>')" <%if(isCheck){ %>class="todo-done"<%} %>>
                <div class="todo-icon fui-list"></div>
                <div class="todo-content">
                  <h4 class="todo-name">
                     <%=missionTemplate.Text("name")%>  <small style="font-size:60%; color:#BDC3C7"><%= missionTemplate.Int("userJoinCount")%>人参与</small>
                  </h4>
                     <%=missionTemplate.Text("remark")%>
                </div>
              </li>
              <%} %>
             </ul>
          </div><!-- /.todo -->
   </div>
   </div>
  <script>
      function SaveMission(missionTemplateId) {
         
          $.ajax({
              url: "/LifeDay/QuickMissionSave",
              type: 'post',
              data: { missionTemplateId: missionTemplateId,
                      weixin: "<%=weixin %>",
                      type: "<%=type %>",
                      missionType: "<%=missionType%>",

               },
              dataType: 'json',
              error: function () {
                  $.tmsg("m_jfw", "未知错误，请联系服务器管理员，或者刷新页面重试", { infotype: 2 });
              },
              success: function (data) {
                  var hideAfter = 1;
                  var message = data.Message;
                  if (data.Success == false) {

                      $.globalMessenger().post({
                          message: message,
                          hideAfter: hideAfter,
                          type: 'error',
                          showCloseButton: true
                      });
                  }
                  else {
                      $.globalMessenger().post({
                          message: "保存成功",
                          hideAfter: hideAfter,
                          type: 'success',
                          showCloseButton: true
                      });
                      FormValueSet();

                  }
              }
          });
      }
  </script>
  </asp:Content>