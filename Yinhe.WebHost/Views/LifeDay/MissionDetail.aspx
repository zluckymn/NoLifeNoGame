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
        var curUser = dataOp.FindOneByKeyVal("SysUser", "weixin", weixin);
        if (curUser == null)
        {
           return;
        }
        var missionQuery = Query.EQ("weixin", weixin);
        var beginDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
        var endDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
        //查找进一个月的成就清单
        switch (type)
        {
            case "1":
                beginDate = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
                missionQuery = Query.And(missionQuery,Query.GTE("completeDate", beginDate), Query.LTE("completeDate", endDate));
                break;
            case "2":
                beginDate = DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd");
                missionQuery = Query.And(missionQuery, Query.GTE("completeDate", beginDate), Query.LTE("completeDate", endDate));
                break;
            default:
                missionQuery = Query.And(missionQuery, Query.GTE("completeDate", beginDate), Query.LTE("completeDate", endDate));
                break;
         }
        var allUserAchievementList = dataOp.FindAllByQuery("PersonAchievement", missionQuery).ToList();
        var dateGroupList = allUserAchievementList.GroupBy(c => c.Date("completeDate").ToString("yyyy-MM-dd")).Select(c=>c.Key).OrderByDescending(c => c).ToList();
       
%>


      <%Html.RenderPartial("Nav"); %>
        
    <div class="btn-group">
          <a class="btn btn-primary  " href="/LifeDay/PersonItemList?weixin=<%=weixin%>" ><span class="fui-list">物品</span></a> 
           <a class="btn btn-primary  <%if(type=="0"){ %>active<%} %>" href="/LifeDay/MissionDetail?weixin=<%=weixin%>&type=0"><span class="fui-time">每周</span></a> 
           <a class="btn btn-primary  <%if(type=="1"){ %>active<%} %>" href="/LifeDay/MissionDetail?weixin=<%=weixin%>&type=1"><span class="fui-photo">月度</span></a> 
           <a class="btn btn-primary  <%if(type=="2"){ %>active<%} %>" href="/LifeDay/MissionDetail?weixin=<%=weixin%>&type=2" ><span class="fui-search">年度</span></a> 
    </div>
     <h4>成就清单</h4>
     <%Html.RenderPartial("UserInfo"); %>

  <div class="row" >
        <div class="col-sm-6 col-md-4">
          <div class="todo">
            <div class="todo-search">
              <input class="todo-search-field" type="search"  onkeyup="Search()"  value="" placeholder="Search" />
            </div>
            <ul>
            <%foreach (var dateStr in dateGroupList)
              {
                  var rewardDetailList = allUserAchievementList.Where(c => c.Date("completeDate").ToString("yyyy-MM-dd") == dateStr).ToList();
                  if (rewardDetailList.Count() <= 0) continue;
                  var hitAllPoints = rewardDetailList.Sum(c => c.Int("completeRewardPoint"));
                  var allAchieveTitile=string.Join("，",rewardDetailList.Select(c=>c.Text("name")).ToArray());
                  %>
              <li >
                <div class="todo-icon fui-time"></div>
                <div class="todo-content">
                  <h4 class="todo-name">
                  <%=dateStr%> 总计获得：<%if (hitAllPoints > 0)
                                                           { %>+<%} %><%=hitAllPoints %>P
                  </h4>
                   完成 <%=allAchieveTitile %>
                </div>
              </li>
              <%} %>
             </ul>
          </div><!-- /.todo -->
        </div>
        <!-- /.col-md-4 -->
  </div>
   
 </asp:Content>
