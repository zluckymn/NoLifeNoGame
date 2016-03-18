<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
 <%
     string WorkPlanManageConnectionString = System.Web.Configuration.WebConfigurationManager.AppSettings["WorkPlanManageConnectionString"];
     DataOperation dataOp = new DataOperation(WorkPlanManageConnectionString, true);
     var helper = new Yinhe.ProcessingCenter.LifeDay.LifeDayHelper(dataOp);
     var weixin = PageReq.GetParam("weixin");
     var userId = PageReq.GetParam("userId");
     var missionType = PageReq.GetParam("missionType");//任务类型
     var _missionType = PageReq.GetParamInt("missionType");//任务类型
     var type = PageReq.GetParamInt("type");//库类型，任务或者周任务类型 1代表愿望单，欲望库类型
     var curUser = helper.InitialUserInfo(weixin);

     if (curUser == null) return;
     var missionQuery = Query.EQ("weixin", weixin);
     var hitAllTaskList = dataOp.FindAllByQuery("MissionLibrary", missionQuery).Where(c => c.Int("status") == 0).ToList();

    
     var unReadTaskCount = hitAllTaskList.Where(c => c.Int("type") == 0).Count();
     var unReadWishCount = hitAllTaskList.Where(c => c.Int("type") == 1).Count();
     var curUserLevel = curUser.Int("level");   
      %>
 
 <nav class="navbar navbar-inverse navbar-embossed" role="navigation">
            <div class="navbar-header">
              <button type="button" class="navbar-toggle" data-toggle="collapse" data-target="#navbar-collapse-01">
                <span class="sr-only">Toggle navigation</span>
              </button>
              <a class="navbar-brand" href="javascript:;"><%=curUser.Text("name")%>'Life日常<small style=" font-size:50%; color:#F39C12">Lv<%=curUserLevel%></small></a>
            </div>
            <div class="collapse navbar-collapse" id="navbar-collapse-01">
              <ul class="nav navbar-nav navbar-left">
                <li><a href="/LifeDay/NoLifeNoGame?weixin=<%=weixin%>&missionType=0">成就库<%if (unReadTaskCount > 0)
                                                                                          {%><span class="navbar-unread"><%=unReadTaskCount%></span><%} %></a></li>
                <li><a href="/LifeDay/NoLifeNoGame?weixin=<%=weixin%>&type=1">愿望库<%if (unReadWishCount > 0)
                                                                                          {%><span class="navbar-unread"><%=unReadWishCount%></span><%} %></a></li>
                <li><a href="/LifeDay/MissionDetail?weixin=<%=weixin%>">成就清单</a></li>
               </ul>
            </div><!-- /.navbar-collapse -->
      </nav>