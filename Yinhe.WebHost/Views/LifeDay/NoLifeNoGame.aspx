<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/BootStrap_flatui.Master"
    Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    生活日常
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="HeadContent" runat="server">
 
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
    <%
        var addition = SysAppConfig.Mission_PointAddition;//10%加成
        var maxAddition = SysAppConfig.Mission_MaxPointAddition;//最高加成70&

        string WorkPlanManageConnectionString = System.Web.Configuration.WebConfigurationManager.AppSettings["WorkPlanManageConnectionString"];
        DataOperation dataOp = new DataOperation(WorkPlanManageConnectionString, true);
        var helper = new Yinhe.ProcessingCenter.LifeDay.LifeDayHelper(dataOp);
      
       
        var weixin = PageReq.GetParam("weixin");
        var userId = PageReq.GetParam("userId");
        var missionType = PageReq.GetParam("missionType");//任务类型
        var _missionType = PageReq.GetParamInt("missionType");//任务类型
        var type = PageReq.GetParamInt("type");//库类型，任务或者周任务类型 1代表愿望单，欲望库类型
        var isEdit = PageReq.GetParamInt("isEdit");//库类型，任务或者周任务类型 1代表愿望单，欲望库类型
        if (string.IsNullOrEmpty(weixin))
        {
            Response.Write("请使用微信登陆");
            return;
        }


        var curUser = helper.InitialUserInfo(weixin);

        if (curUser == null) return ;
        
        var missionQuery = Query.EQ("weixin", weixin);

        var hitAllTaskList = dataOp.FindAllByQuery("MissionLibrary", missionQuery).Where(c=>c.Int("status")==0).ToList();

        var allUserTaskList = hitAllTaskList.Where(c => c.Int("missionType") == _missionType && c.Int("type") == type).ToList();
      
  
        //获取系统任务
        ///获取今天完成的每日任务
        //var personAchievementMissionIds = helper.GetTodaySystemAchievement(weixin).Select(c =>c.Text("missionId"));

        //var commonMissionList = dataOp.FindAllByQuery("MissionLibrary", Query.And(Query.EQ("missionType", missionType), Query.EQ("type", "2"), Query.NotIn("_id", TypeConvert.ToObjectIdList(personAchievementMissionIds.ToList())))).ToList();
        //allUserTaskList.AddRange(commonMissionList);
        
        var curMissionDesc = string.Empty;
        var icon = "user";
        if (type == 0)
        {
            switch (missionType)
            {
                case "0": curMissionDesc = "每日"; icon = "time"; break;
                case "1": curMissionDesc = "每周"; icon = "photo"; break;
                case "2": curMissionDesc = "副本"; icon = "list"; break;
                case "3": curMissionDesc = "深渊"; icon = "eye"; break;
            }
        }
        else
        {
            icon = "paypal";
            curMissionDesc = "愿望";
        }
     

       
        var ssLandMark = @"/Content/LifeDay/sprite_item/iconmark/0.png";
        var hellChallengeCount = SysAppConfig.Mission_HellChallengeCount;
        var validHellChallengeCount=hellChallengeCount-curUser.Int("execHellChallengeCount");
        
       
       
          %>
   <%Html.RenderPartial("Nav"); %>

    <%if (type == 0)
      { %>
    <div class="btn-group" style="padding:0px">
        <a class="btn btn-primary  <%if(missionType=="0"){ %>active<%} %>" href="/LifeDay/NoLifeNoGame?weixin=<%=weixin%>&missionType=0">
            <span class="fui-time">每日</span></a> <a class="btn btn-primary  <%if(missionType=="1"){ %>active<%} %>"
                href="/LifeDay/NoLifeNoGame?weixin=<%=weixin%>&missionType=1"><span class="fui-photo">
                    周</span></a> <a class="btn btn-primary  <%if(missionType=="2"){ %>active<%} %>"
                        href="/LifeDay/NoLifeNoGame?weixin=<%=weixin%>&missionType=2"><span class="fui-list">
                            副本</span></a> <a class="btn btn-primary  <%if(missionType=="3"){ %>active<%} %>"
                                href="/LifeDay/NoLifeNoGame?weixin=<%=weixin%>&missionType=3"><span class="fui-eye">
                                    深渊</span></a>
    </div>
    <%} %>
     
        
    <%--   <div  class="running" ><div style="line-height:289px;height:100%;text-align:center;vertical-align:middle;">无用</div></div> --%>
        <h4><%= curMissionDesc%>任务<a href="javascript:;" onclick="$('#missionFormDiv').toggle();$('#missionInfoDiv').toggle();$('#hellDiv').hide();$('#hellSwitch').bootstrapSwitch('state', false);">
        <span style=" font-size:60%"  class="fui-new" <%if(allUserTaskList.Count()<=0&&missionType!="3"){%> data-placement="right" title="轻按进行添加任务"  data-toggle="tooltip" <%} %>></span></a>  
        <%if (missionType == "3"){ %>
        <span style=" float:right" >
        <input  type="checkbox" hellMode="false"  data-toggle="switch" data-on-text="开启" data-off-text="深渊" name="default-switch" id="hellSwitch" /></span>
        <%} %>
        </h4> 
    <%Html.RenderPartial("UserInfo"); %>
      <div name="changeDiv" id="missionInfoDiv" >
        <div class="row">
            <div class="col-sm-6 col-md-4">
                <div class="todo">
                    <div class="todo-search">
                        <input class="todo-search-field" type="search" onkeyup="Search()" value="" placeholder="Search" />
                    </div>
                    <ul>
                        <%
                            var curDate = DateTime.Now.ToString("yyyy-MM-dd");
                            foreach (var mission in allUserTaskList.OrderByDescending(c => c.Int("templateType")).ThenByDescending(c => c.Int("comboHit")))
                          {
                              var curComboHit = mission.Int("comboHit");
                              var curAddition = addition * curComboHit;//最高加成70%
                              if (curAddition >= maxAddition)
                              {
                                  curAddition = maxAddition;
                              }
                              var isNew = mission.Date("createDate").ToString("yyyy-MM-dd") == curDate;
                        %>
                        <li onclick="SaveBtnShow()" missionid="<%=mission.Text("_id")%>" point="<%=mission.Int("completeRewardPoint")%>">
                            <div class="todo-icon fui-<%=icon%>">
                            </div>
                            <div class="todo-content">
                                <h4 class="todo-name">
                                   <%if (mission.Int("templateType") == 2)
                                     { %>
                                       <font color="#e67e22"><%=mission.Text("name")%><small style="font-size: 60%; color: #e67e22;">(系统)</small></font>
                                    <%}
                                     else
                                     { %>
                                       <%=mission.Text("name")%><%if (isNew)
                                                                  { %><small style=" font-size:60%;color: #E67E22;">new~</small><%} %>
                                    <%} %>
                                    &nbsp;<%if (mission.Int("completeRewardPoint") > 0)
                                            {%>+<%} %><%=mission.Text("completeRewardPoint")%>P
                                    <%if (curComboHit != 0)
                                      { %>
                                    <small style="font-size: 60%; color: #E67E22;">
                                        <%=curAddition*100%>%↑
                                        <%=curComboHit%>hits</small>
                                    <%} %>
                                </h4>
                                <%=mission.Text("remark")%><%=mission.Text("limitedTime")%>
                            </div>
                        </li>
                        <%} %>
                    </ul>
                </div>
                <!-- /.todo -->
            </div>
            <!-- /.col-md-4 -->
        </div>
        <div  id="btnNav" style="display:none">
        <br/> 
        <nav  class="navbar navbar-default navbar-fixed-bottom">
        
          <div class="navbar-inner navbar-content-center">
                     <a class="btn btn-primary btn-large btn-block" href="javascript:;"  onclick="MissionComplete()">
                        完成</a>
             </div>

        </nav>
        </div>
       </div>
       
      <div class="col-md-12" id="missionFormDiv" name="changeDiv" style="display:none">
       <small>快速添加</small>
        <a href="/LifeDay/MissionQuickAdd?weixin=<%=weixin %>&type=<%=type%>&missionType=<%=missionType %>"   ><span class="fui-plus"  ></span>

        </a><a href="javascript:;" onclick="window.location.reload();"><span style=" float:right" class="fui-cross"></span></a>
        
        <div class="tooltip fade right in" role="tooltip" id="tooltip497986" style="top: -14px; left:90px; display: block;">
            <div class="tooltip-arrow"></div>
            <div class="tooltip-inner"><small>单击看看其他人在做什么</small></div>
        </div>

         <form action="" id="missionForm" class="form">
            <input type="hidden" name="missionType" value="<%=missionType %>">
            <input type="hidden" name="type" value="<%=type %>">
            <input type="hidden" name="weixin" value="<%=weixin%>">
            <input type="hidden" name="userId" value="<%=curUser.Text("userId")%>">
          <%--  <input type="hidden" name="queryStr" value="db.MissionLibrary.distinct('_id',{'missionId':'52'})">--%>
             <%
                 var nameTip="早上8点前起床";
                 var remarkTip="备注【每日更新】";
                var pointTip="10";
                switch(missionType){ 
                    case "0"://每日;
                       break;
                    case "1": //每周
                        nameTip = "跑步锻炼";
                        remarkTip = "一周内跑步3次【每周更新】";
                        break;
                    case "2"://副本
                        nameTip = "学习新技能";
                        remarkTip = "该任务为一次性完成，point较高";
                        pointTip = "100";
                        break;
                    case "3": //深渊
                         nameTip = "晚上12点后睡觉";
                         remarkTip = "堕入深渊的任务";
                         pointTip = "-10";
                        break;
             } %>
            
            <div class="form-group">
             <input type="text" name="name" class="form-control" placeholder="输入名称如：<%=nameTip%>">
            </div>
            <div class="form-group">
               <input type="text" name="completeRewardPoint" class="form-control" placeholder="完成任务point点数,如<%=pointTip %>">
            </div>
            <div class="form-group">
            <textarea class="form-control"  name="remark" rows="3"  placeholder="输入<%=remarkTip %>"></textarea>
            </div>
            
          
             <div class="navbar-inner navbar-content-center">
                     <a class="btn btn-primary btn-large btn-block"  href="javascript:;"  onclick="MissionSave(this)">
                        保存</a>
                  </div>
          </form>
        
        </div>

       <div   style=" display:none; height:320px; text-align:center; " name="changeDiv"  id="hellDiv" >
       <p    style=" background:url('/Content/LifeDay/sprite_map_hellmap/hellbg.png'); -moz-background-size:100% 100%;
   background-size:100% 100%;" >
        <img style="padding-top:140px" src="/Content/LifeDay/sprite_map_hellmap/hellgate.png">
       </p>
       <div class="col-md-12">
        <div class="navbar-inner navbar-content-center">
                     <a class="btn btn-primary btn-large btn-block" href="javascript:;"  onclick="HellChallenge()">
                        挑战<img  src="/Content/LifeDay/sprite_item/iconmark/39.png"/><small id="hellChallengeCount" style=" font-size:80%"><%=validHellChallengeCount%></small>/<small style=" font-size:80%"><%=hellChallengeCount%>次</small></a>
                  </div>
                  </div>
        </div>

        
 <%--    <div class="raredrop" >
     <div  style='background-image:url("/Content/LifeDay/sprite_item_common/coat/118.Png"); width:28px; height:28px;  margin:auto; margin-top:40px  '>
     <img style=" float:left; width:28px; height:28px; " src="<%=ssLandMark %>"></img> 
     </div>
     <small  style=" font-size:50%"><font style="line-height:30px;" color = "#f1c40f">拉比纳的雪崩长靴</font></small>
     </div>--%>
     

        <script type="text/javascript">
  //future block air ice
    $._messengerDefaults = { extraClasses: 'messenger-fixed messenger-theme-fucture messenger-on-top '  };
 

//    var str='<div>测试1，，大丈夫!</div>';
    
  
     
//    var str3= '<span class="raredrop" > <font  style=" font-size:50%;line-height:110px;" color = "#f1c40f">拉比纳的雪崩长靴</font></span>,<span class="raredrop" ><font  style=" font-size:50%;line-height:110px;" color = "#f1c40f">别云剑-无用</font></span>';
//    
//     $.globalMessenger().post({
//                            message: "",
//                            hideAfter: 113,
//                            type: 'success',
//                            showCloseButton: true
//                            });
//      $.globalMessenger().post({
//                            message:str3,
//                            hideAfter: 113,
//                            type: 'success',
//                            showCloseButton: true
//                            });


       $(function () {
        
        $('[data-toggle=tooltip]').tooltip();
       
      });

       $('#hellSwitch').on({
        'init.bootstrapSwitch': function() {
          var state = true; // 从服务器获取按钮状态

           // $("#hellSwitch").bootstrapSwitch("state", state);// 初始化状态


        },
        'switchChange.bootstrapSwitch': function(event, state) {
            // 如果没有焦点，证明不是用户触发的,　不做任何处理
            //if ($("#hellSwitch").is(":focus") == false) return;
               if(state==true)
               {
               ShowDiv('hellDiv');
               }else{
                ShowDiv('missionInfoDiv');
               }
            // 处理
        }
    });


     var isHellMode = "false";
     var curPoint=<%=curUser.Int("point")%>;
    
    
     function ShowDiv(divName)
      {
          if(typeof(divName)=="undefined")
          {
             divName="missionInfoDiv";
           }
        // $("#"+divName).toggle();
          $("div[name='changeDiv']").each(function () {
               if(divName== $(this).attr("id"))
               {
                 $(this).show();
               } else{
               
               $(this).hide();
               }
          }) ;
       }

    

   

      function SaveBtnShow()
      {
         if($("#btnNav").is(":hidden"))
         {
            $("#btnNav").show();
         } 
      
      }
     
 
      function MissionComplete() {
          var missionIds = "";
          $(".todo-done").each(
          function () {
              var missionId = $(this).attr("missionId");
              curPoint+=Number($(this).attr("point"));
             // alert($(this).attr("point"));
              missionIds += missionId + ","
              $(this).remove();

          }
          );

          if(missionIds=="")
          {
               $.globalMessenger().post({
                            message: "请先选择任务",
                            hideAfter: 2,
                            type: 'error',
                            showCloseButton: true
                            });
               return;
          }
       
          $.ajax({
              url: '/LifeDay/MissionComplete',
              type: 'post',
              data: { missionIds: missionIds, weixin: '<%=weixin%>', userId: '<%=userId%>' },
              dataType: 'json',
              error: function () {
              },
              success: function (data) {
                  if (data.Success == false) {
                     //$.tmsg("m_jfw", data.Message, { infotype: 2 });
                       $.globalMessenger().post({
                        message: data.Message,
                        hideAfter: 2,
                        type: 'error',
                        showCloseButton: true
                        });
                         setTimeout(function(){window.location.reload();},3000);
                         
                  }
                  else {
                     //$.tmsg("m_jfw", "大成功！", { infotype: 1 });
                     
                       CheckItemDrop(data);
                       CheckLevelUpAction(data);
                           

                       $("#pointPositionDiv").html(curPoint+"P");
                      //window.location.reload();
                  }
              }
          });

      }

      



      function Search() {

          var val = $(".todo-search-field").val();
          //if (val = "") return;
          $(".todo-name").each(
          function () {

              if ($(this).html().indexOf(val) >= 0) {
                  // $(this).parent().parent().attr("class","todo-done");
                  $(this).parent().parent().show();
              } else {
                  //  $(this).parent().parent().attr("class", ""); 
                  $(this).parent().parent().hide();
              }
          }
      );
      }
      
     function FormValueSet()
     {
        $("input[name='name']").val("");
        $("input[name='completeRewardPoint']").val("");
        $("textarea[name='remark']").val("");
        
     }
     function MissionSave(obj)
     {
         $(obj).html("保存中...");
          $(obj).attr("disabled",true);
      
         var formdata = $("#missionForm").serialize();
         if($("input[name='name']").val()=="")
         {
         
           $.globalMessenger().post({
                        message: "请输入任务名称",
                        hideAfter:2,
                        type: 'error',
                        showCloseButton: true
          });
          }
      
            $.ajax({
                url: "/LifeDay/MissionSave",
                type: 'post',
                data: formdata,
                dataType: 'json',
                error: function () {
                    $.tmsg("m_jfw", "未知错误，请联系服务器管理员，或者刷新页面重试", { infotype: 2 });
                },
                success: function (data) {
                     var hideAfter=2;
                     var message= data.Message;
                    if (data.Success == false) {
                  
                        $.globalMessenger().post({
                        message: message,
                        hideAfter:hideAfter,
                        type: 'error',
                        showCloseButton: true
                    });
                    }
                    else {
                        $.globalMessenger().post({
                        message: "保存成功",
                        hideAfter:hideAfter,
                        type: 'success',
                        showCloseButton: true
                    });
                      FormValueSet();
                     $(obj).html("保存");
                       $(obj).attr("disabled",false);
                    }
                }
            });
   
     }


     function ShowBMask() {
    if (!document.getElementById("Bmask")) {
        document.documentElement.style.overflow = "hidden";
        var mask = '<div id="newImg"  style=" margin-top:400px ;  text-align:center; ">';
        mask += ' <img style=" width:330px;heigh:200px;" src="/Content/flat-ui/img/customerPic/battle_small.gif" /> ';
        mask += '</div>';

        $(document.body).append('<div id=Bmask style=" position:absolute; top:0px; left:0px; height:150%; width:100%;"><div style="filter:alpha(opacity=50);opacity:0.5;  position:absolute; top:0px; left:0px; height:100%; width:100%; background-color:black"></div>' + mask + '</div>');
        $("#Bmask").css("top", $(document.documentElement).scrollTop());

        var topindex = 0;
        $("div").each(function() {
            if (parseInt($(this).css("z-index")) > topindex)
            { topindex = parseInt($(this).css("z-index")); }
        });

        $("#Bmask").css("z-index", topindex + 2);
        //$("#newImg").css("z-index", topindex +3);
        
    }
}

function RemoveBMask() {
    $("#Bmask").remove();
    document.documentElement.style.overflow = "auto";
}
   
      

      function HellChallenge() {
      
      
        var hellChallengeCount=$("#hellChallengeCount").html();
     
         if(hellChallengeCount<=0)
         {
             $.globalMessenger().post({
                            message: "挑战次数已完！",
                            hideAfter: 2,
                            type: 'error',
                            showCloseButton: true
                            });
                   return false;
         
         }
           //加载gif动画
         ShowBMask();
         
         hellChallengeCount=hellChallengeCount-1;
          $.ajax({
              url: '/LifeDay/HellChallenge',
              type: 'post',
              data: {weixin: '<%=weixin%>', userId: '<%=userId%>' },
              dataType: 'json',
              error: function () {
              },
              success: function (data) {
                  if (data.Success == false) {
                     //$.tmsg("m_jfw", data.Message, { infotype: 2 });
                       $.globalMessenger().post({
                        message: data.Message,
                        hideAfter: 2,
                        type: 'error',
                        showCloseButton: true
                        });
                         setTimeout(function(){window.location.reload();},3000);
                         
                  }
                  else {

                    setTimeout(function(){
                    
                       RemoveBMask();
                       CheckItemDrop(data);
                       CheckLevelUpAction(data);
                         
                      $("#hellChallengeCount").html(hellChallengeCount);
                      //window.location.reload();
                    
                    },3000);
                    
                  }
              }
          });

      }


       ///检查稀有物品
      function CheckItemDrop(data)
      {

                     var hideAfter=2;
                     var message=data.Message;
                     
                     if(typeof(data.Message)=="undefined"||data.Message==""||data.Message==null)
                     {
                        hideAfter=2;
                     }else
                     {
                        //是否爆出特殊物品
                        if(typeof(data.FileInfo)=="undefined"||data.FileInfo==""||data.FileInfo==null)
                        {
                          hideAfter=2;
                        }else
                        {
                            hideAfter=3600;
                            $.globalMessenger().post({
                            message: data.FileInfo,
                            hideAfter:hideAfter,
                            type: 'success',
                            showCloseButton: true
                             });
                        }
                     }
                        $.globalMessenger().post({
                            message: message,
                            hideAfter:hideAfter,
                            type: 'success',
                            showCloseButton: true
                        });
      
      }

      
      ///检查升级
      function CheckLevelUpAction(data)
      {

                       if(typeof(data.htInfo)!="undefined"&&typeof(data.htInfo.isLevelUp)!="undefined")
                        {
                           if(data.htInfo.isLevelUp=="true")
                           {
                            $.globalMessenger().post({
                                                message: "恭喜您的等级已提升",
                                                hideAfter:2,
                                                type: 'success',
                                                showCloseButton: true
                                            });
                        }
                        }
      
      }
        </script>
</asp:Content>
