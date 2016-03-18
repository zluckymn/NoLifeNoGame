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

        var allUserItemList = dataOp.FindAllByQuery("PersonItem", missionQuery).OrderByDescending(c => c.Int("rarity")).ThenByDescending(c => c.Date("createDate")).ToList();
        //var allItemList = dataOp.FindAll("ItemList");
        List<BsonDocument> allEquipment = null;
        allEquipment = CacheHelper.GetCache("MissionItemList") as List<BsonDocument>;
        if (allEquipment == null)
        {
            allEquipment = dataOp.FindAll("Item").ToList();
            CacheHelper.SetCache("MissionItemList", allEquipment, null, DateTime.Now.AddMinutes(30));
        }
         var lifeDayHelper = new Yinhe.ProcessingCenter.LifeDay.LifeDayHelper(dataOp);

         var fixAllUserItemList =  allUserItemList.Where(c => c.Int("rarity") >= 3).ToList();
         var curAllUserItemList = allUserItemList;
         if(fixAllUserItemList.Count()>20)
         {
             curAllUserItemList = fixAllUserItemList;
         }
%>

   <%Html.RenderPartial("Nav"); %>
    <div class="btn-group">
           <a class="btn btn-primary  active" href="/LifeDay/PersonItemList?weixin=<%=weixin%>" ><span class="fui-list">物品</span></a> 
           <a class="btn btn-primary  <%if(type=="0"){ %>active<%} %>" href="/LifeDay/MissionDetail?weixin=<%=weixin%>&type=0"><span class="fui-time">每周</span></a> 
           <a class="btn btn-primary  <%if(type=="1"){ %>active<%} %>" href="/LifeDay/MissionDetail?weixin=<%=weixin%>&type=1"><span class="fui-photo">每月</span></a> 
           <a class="btn btn-primary  <%if(type=="2"){ %>active<%} %>" href="/LifeDay/MissionDetail?weixin=<%=weixin%>&type=2" ><span class="fui-search">年</span></a> 
     </div>
  <h4>物品信息
  <span style=" float:right" >
  <input  type="checkbox" hellMode="false"  data-toggle="switch" data-on-text="返回" data-off-text="售卖" name="default-switch" id="equipSwitch" />
   
  </span></h4>
  
   <%Html.RenderPartial("UserInfo"); %>
  <div class="row" id="equipmentInfoDiv" name='changeDiv'  >
        <div class="col-sm-6 col-md-4">
          <div class="todo">
            <div class="todo-search">
              <input class="todo-search-field" type="search"  onkeyup="Search()"  value="" placeholder="Search" />
            </div>
            <ul>
            <%var dayLifeHelper = new Yinhe.ProcessingCenter.LifeDay.LifeDayHelper();
              foreach (var item in curAllUserItemList)
              {
                 var curItemInfo = allEquipment.Where(c => c.Text("_id") == item.Text("itemId")).FirstOrDefault();
                 if (curItemInfo == null) continue;
                 var price = lifeDayHelper.GetEquipmentSellPrice(curItemInfo);
                  var color = "white";//默认黑色
                  switch (item.Int("rarity"))
                  {
                      case 0://白
                          color = "#ecf0f1";
                          break;
                      case 1://蓝
                          color = "#3498db";
                          break;
                      case 2://紫
                          color = "#8e44ad";
                          break;
                      case 3://粉
                          color = "#C0139E";
                          break;
                      case 4://传奇
                          color = "#e74c3c";
                          break;
                      case 5://ss
                          color = "#f1c40f";
                          break;
                 }
                  var equipmentImg = dayLifeHelper.GetEquipmentImgDiv(curItemInfo);
                  var equipmentProperty = dayLifeHelper.GetEquipmentProperty(curItemInfo);
                   %>
              <li onclick="SaveBtnShow()" personItemId="<%=item.Text("_id")%>" point="<%=curItemInfo.Int("price")/1000%>" >
                <div class="todo-icon fui-list"></div>
                <div class="todo-content">
                  <h4 class="todo-name">
                  <small style=" font-size:70%">LV<%=curItemInfo.Int("level")%></small><a style="color:<%=color%>"><%=item.Text("name")%></a><%  if (curItemInfo!=null){ %> &nbsp;  +<%=price%>P <%} %>
                   <%=equipmentImg %>
                  </h4>
                  <%=equipmentProperty %>
                 
                  <%if (!string.IsNullOrEmpty(curItemInfo.Text("basic_explain")))
                    {%>
                   <span style="color:#3498db"><%=curItemInfo.Text("basic_explain").Replace("\\N", "<br>")%></span>
                     <br>
                  <%}
                    else if (!string.IsNullOrEmpty(curItemInfo.Text("detail_explain")))
                    { %>
                     <span style="color:#3498db"><%=curItemInfo.Text("detail_explain").Replace("\\N", "<br>")%></span>
                     <br>
                  <%} %>
                 <%if (!string.IsNullOrEmpty(curItemInfo.Text("flavor_text")))
                    {%>
                     <span>"<%=curItemInfo.Text("flavor_text").Replace("\\N", "<br>")%>"</span>
                  <%} %>
                </div>
              </li>
              <%} %>
             </ul>
          </div><!-- /.todo -->
        </div><!-- /.col-md-4 -->
      </div>

         <div  id="btnNav" style="display:none">
        <br/> 
        <nav  class="navbar navbar-default navbar-fixed-bottom">
        
          <div class="navbar-inner navbar-content-center">
                     <a class="btn btn-primary btn-large btn-block" href="javascript:;"  onclick="ItemSell()">
                        售卖</a>
             </div>

        </nav>
        </div>

    <div   name='changeDiv'   style=" display:none; height:320px; text-align:center; " id="quickSellDiv"  >


       <div class="col-md-6">
          <form role="form">
            <div class="form-group">
               <div class="form-group">
              <label class="checkbox" for="checkbox1">
                <input type="checkbox" name="equipmentType" data-toggle="checkbox" checked="checked" value="0" id="checkbox1"  >
                <font style="">白装(<%=allUserItemList.Where(c=>c.Int("rarity")==0).Count()%>)</font>
              </label>
              <label class="checkbox" for="checkbox2">
                <input type="checkbox"  name="equipmentType" data-toggle="checkbox" checked="checked"  value="1" id="checkbox2" >
                  <font style="color:#3498db">蓝装(<%=allUserItemList.Where(c=>c.Int("rarity")==1).Count()%>)</font>
              </label>
            <label class="checkbox" for="checkbox3">
                <input type="checkbox"  name="equipmentType" data-toggle="checkbox" checked="checked"  value="2" id="checkbox3" >
                <font style="color:#8e44ad">紫装(<%=allUserItemList.Where(c=>c.Int("rarity")==2).Count()%>)</font>
           </label>
            <label class="checkbox" for="checkbox4">
                <input type="checkbox"  name="equipmentType" data-toggle="checkbox"  value="3" id="checkbox4" >
               <font style="color:#C0139E">粉装(<%=allUserItemList.Where(c=>c.Int("rarity")==3).Count()%>)</font>
           </label>
            <label class="checkbox" for="checkbox5">
                <input type="checkbox"  name="equipmentType" data-toggle="checkbox"  value="4" id="checkbox5" >
                <font style="color:#e74c3c">传说(<%=allUserItemList.Where(c=>c.Int("rarity")==4).Count()%>)</font>
           </label>
            <label class="checkbox" for="checkbox6">
                <input type="checkbox"  name="equipmentType" data-toggle="checkbox"  value="5" id="checkbox6" >
                 <font style="color:#f1c40f">史诗(<%=allUserItemList.Where(c=>c.Int("rarity")==5).Count()%>)</font>
           </label>
            </div>
            </div>
           </form>
        </div>   
       <div class="col-md-12">
          

        <div class="navbar-inner navbar-content-center">

          
           <a class="btn btn-primary btn-large btn-block" href="javascript:;"  onclick="EquipmentSell()">
                        <img  src="/Content/LifeDay/sprite_item/fieldimage/37.png"/></a>
                  </div>
        </div>
        </div>
  

        <script >
            $._messengerDefaults = { extraClasses: 'messenger-fixed messenger-theme-fucture messenger-on-top ' };

            //批量售卖装备
            function EquipmentSell() {
                var rarityIds = '';
                $(":checkbox[name='equipmentType']:checked").each(function () {
                    rarityIds += $(this).val() + ',';
                    //alert($(this).val());
                });
                $.ajax({
                    url: '/LifeDay/QucikPersonItemSell',
                    type: 'post',
                    data: { rarityIds: rarityIds, weixin: '<%=weixin%>' },
                    dataType: 'json',
                    error: function () {
                    },
                    success: function (data) {
                        if (data.Success == false) {
                            //$.tmsg("m_jfw", data.Message, { infotype: 2 });
                            $.globalMessenger().post({
                                message: data.Message,
                                hideAfter: 3,
                                type: 'error',
                                showCloseButton: true
                            });
                            setTimeout(function () { window.location.reload(); }, 3000);

                        }
                        else {
                            //$.tmsg("m_jfw", "大成功！", { infotype: 1 });

                            var hideAfter = 5;
                            var message = data.Message;
                            if (typeof (data.Message) == "undefined" || data.Message == "" || data.Message == null) {
                                message = "出售成功";
                            }
                            $.globalMessenger().post({
                                message: message,
                                hideAfter: hideAfter,
                                type: 'success',
                                showCloseButton: true
                            });
                            window.location.reload();
                        }
                    }
                });

               
            }


            $('#equipSwitch').on({
                'init.bootstrapSwitch': function () {
                    var state = true; // 从服务器获取按钮状态

                    // $("#hellSwitch").bootstrapSwitch("state", state);// 初始化状态


                },
                'switchChange.bootstrapSwitch': function (event, state) {
                    // 如果没有焦点，证明不是用户触发的,　不做任何处理
                    //if ($("#hellSwitch").is(":focus") == false) return;
                    if (state == true) {
                        ShowDiv('quickSellDiv');
                    } else {
                        ShowDiv('equipmentInfoDiv');
                    }
                    // 处理
                }
            });

  
            function ShowDiv(divName) {
                if (typeof (divName) == "undefined") {
                    divName = "missionInfoDiv";
                }
                // $("#"+divName).toggle();
                $("div[name='changeDiv']").each(function () {
                    if (divName == $(this).attr("id")) {
                        $(this).show();
                    } else {

                        $(this).hide();
                    }
                });
            }



            function SaveBtnShow() {
                
                if ($("#btnNav").is(":hidden")) {
                    $("#btnNav").show();
                } 
            }

            function ItemSell() {
                var personItemIds = "";
             
                $(".todo-done").each(
                  function () {
                      var personItemId = $(this).attr("personItemId");
                  
                       personItemIds += personItemId + ","
                      $(this).remove();

                  }
                  );
        
                if (personItemIds == "") {
                    $.globalMessenger().post({
                        message: "请先选择任务",
                        hideAfter: 3,
                        type: 'error',
                        showCloseButton: true
                    });
                    return;
                }

                $.ajax({
                    url: '/LifeDay/PersonItemSell',
                    type: 'post',
                    data: { personItemIds: personItemIds, weixin: '<%=weixin%>' },
                    dataType: 'json',
                    error: function () {
                    },
                    success: function (data) {
                        if (data.Success == false) {
                            //$.tmsg("m_jfw", data.Message, { infotype: 2 });
                            $.globalMessenger().post({
                                message: data.Message,
                                hideAfter: 3,
                                type: 'error',
                                showCloseButton: true
                            });
                            setTimeout(function () { window.location.reload(); }, 3000);

                        }
                        else {
                            //$.tmsg("m_jfw", "大成功！", { infotype: 1 });

                            var hideAfter = 5;
                            var message = data.Message;
                            if (typeof (data.Message) == "undefined" || data.Message == "" || data.Message == null) {
                                message = "出售成功";
                            }
                            $.globalMessenger().post({
                                message: message,
                                hideAfter: hideAfter,
                                type: 'success',
                                showCloseButton: true
                            });
                             window.location.reload();
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
        </script>
 </asp:Content>
