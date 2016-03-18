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
    

     //获取用户等级与经验
     var curUserLevel = curUser.Int("level");
     var curUserExp = curUser.Int("exp");//经验值
     var curUserLevelPercent = "0";
     decimal nexLevelExp = 0;
     //var curLevel = dataOp.FindOneByKeyVal("PersonLevel", "level", (curUserLevel).ToString());
     var nexLevel = dataOp.FindOneByKeyVal("PersonLevel", "level", (curUserLevel + 1).ToString());
     if (nexLevel != null)
     {
         nexLevelExp = nexLevel.Decimal("levelExp");
         // var curLevelExp = curLevel.Int("levelExp");
         if (nexLevelExp != 0)
         {
             curUserLevelPercent = (decimal.Round(curUserExp < 0 ? 0 : curUserExp / nexLevelExp, 2) * 100).ToString();
         }
     }
     else
     {
         nexLevelExp = curUserExp;
         curUserLevelPercent = "100";
     }
     
         
      %>
 
 
       <small>成就点：</small><strong id="pointPositionDiv"><%=curUser.Int("point").ToString("n")%>P</strong>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
       <small>经验：</small>
       <small style=" font-size:60%"><strong id="expPositionDiv"><%=curUserExp%></strong>/<%=nexLevelExp%>exp</small>
     
       <div class="progress">
            <div class="progress-bar" style="width:<%=curUserLevelPercent%>%;"></div>
         </div> 