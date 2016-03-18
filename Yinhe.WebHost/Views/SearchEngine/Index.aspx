<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="Yinhe.ProcessingCenter.ViewPageBase" %>
<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="HeadContent" runat="server">
     <% Html.RenderPartial("HeadContent"); %> 
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
<div class="con_top">
  <div class="path_nav">搜索</div>
</div>
<br />
<%string keyWord = Server.UrlDecode(PageReq.GetParam("keyWord"));%>
    <div class="ggmenu" style="padding-left: 0px;">
        <div style="float: left; margin-top: -10px;">
            <img src="<%=SysAppConfig.HostDomain %>/Content/Images/zh-cn/prodesiman/serch-pic2.jpg" /></div>
        <ul id="tabSelect" >
            <li><a class="select" href="javascript:void(0);" onclick="changeTab(this);" condition=""
                  resulturl="/SearchEngine/SearchProjectControl/" conditionurl="/SearchEngine/ProjectConditionControl">项目库</a></li>
            <li><a href="javascript:void(0);" onclick="changeTab(this);" condition=""
                 resulturl="/SearchEngine/StandardSearchRetList/" conditionurl="/SearchEngine/StandardLibSearchBar">标准库</a></li>
        </ul>
    </div>
     <input type="hidden" id="resultUrl" name="resultUrl" value="" />
    <input type="hidden" id="resultCondition" name="resultCondition" value="" />
    <input type="hidden" id="conditionUrl" name="conditionUrl" value="/SearchEngine/ProjectConditionControl" />
    
    <div style="clear: both; margin-bottom:5px;">
        <table>
            <tr>
                <td class="nopadding" valign=top><img src="<%=SysAppConfig.HostDomain %>/Content/Images/zh-cn/prodesiman/serchbg-left.jpg" /></td>
                <td class="nopadding serchbg">
                    <input name="keyWord" id="keyWord" class="serchbtn" style="width: 500px;"
                        type="text" value="<%=keyWord %>" />
                    <div class="panel" style="width: 400px; top: 342px; left: 437px;">
                    </div>
                </td>
                <td class="nopadding">
                    <input onclick="searchResult();" name="search" type="image" src="/Content/Images/zh-cn/prodesiman/btn-serch.jpg"/>
                    
                    
                </td>
            </tr>
        </table>
        
    </div>
    <div id="divCondition"> 
         </div>

         <div id="divResultList">
         </div>
    <script>

        $(document).ready(function () {
            //            loadMenu(228);
            //            initAllBoxs();
            SetMenu(1);
            loadCondition();
            //弹出消息提醒
            //ShowPersonStandardInfo();
         
        });

        if ($("input[name=keyWord]").val() != "") {
            searchResult();
        }

        function changeTab(obj) {
            
            $('a.select').removeClass("select");
            $(obj).addClass("select");
            $("#tabSelect").val($(obj).attr("tabId"));
            $("#conditionUrl").val($(obj).attr("conditionUrl"));
            $("#divCondition").html('');

            $("#resultCondition").val($(obj).attr("conditionurl"));
            $("#resultUrl").val($(obj).attr("resulturl"));
            loadCondition();
        }

        function loadCondition() {//加载条件控件
            var url = $("#conditionUrl").val();
           
            if (url != "") {
                var condition = $("#resultCondition").val();
                url += "?r=" + Math.random() + condition;
                
                $("#divCondition").load(url);
              
            }
        }



        function searchResult() {
            
            var url = $("#resultUrl").val();

            url += "?strKeyWord=" + escape($("#keyWord").val());
            // alert($("#resultCondition").val());
            if (window.generateCondition)
                generateCondition(); //生成条件表达式
            if ($("#resultCondition").val() != "") {
                //alert($("#resultCondition").val());
                url += $("#resultCondition").val();
            }
            url += "&r=" + Math.random();
             $("#divResultList").html("").load(url);
        }
         
    </script>
 </asp:Content>