<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/FLKL.Master" Inherits="Yinhe.ProcessingCenter.ViewPageBase" %>

<%@ Import Namespace="Yinhe.ProcessingCenter.Common" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
 

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="HeadContent" runat="server">
   <%Html.RenderPartial("HeadContent");%>
     <link href="<%=SysAppConfig.HostDomain %>/Content/css/client/FLKL/css.css" rel="stylesheet"
    type="text/css" />
    <%--<link href="../../Content/css/client/QX/qiaoxin.css" rel="stylesheet" type="text/css" />--%>
    <link href="../../Content/css/client/FLKL/admincss.css" rel="stylesheet" type="text/css" />
    <%--<link href="../../Content/css/client/QX/Series.css" rel="stylesheet" type="text/css" />--%>
  <script language="javascript" type="text/javascript">
        var New_Master_Server_Address = "<%=SysAppConfig.MasterServerAddress %>|<%=SysAppConfig.CustomerCode %>";
    </script>
 
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
   <%
      
       if ( !auth.CheckSysRight("SiteManage_VIEW"))
       {
           string returnUrl = SysAppConfig.LoginUrl;

           if (returnUrl.IndexOf("?") >= 0)
           {
               returnUrl += "&ReturnUrl=" + Server.UrlEncode(Request.RawUrl);
           }
           else
           {
               returnUrl += "?ReturnUrl=" + Server.UrlEncode(Request.RawUrl);
           }
           Response.Redirect(returnUrl);
           return;
       }
        %>
 
   <div class="admin_manage_content">
    <div class="page-info">
        <h1 class="page-title">
           后台配置
        </h1>
        <div class="page-oper">
    </div>
    </div>
    
    <div class="main" style="overflow: visible;">
        <div class="mainleft">
            <div class="sidebar-l" id="leftTabDiv">
                <ul class="side-tabs">
                    <li class="title" showtag="1" class="second_item" onclick="changeleftTab(1);"><a href="javascript:;">首页简介</a> </li>
                    <li class="title"><a href="javascript:;">关于我们</a> </li>
                    <li showtag="3" class="second_item" onclick="changeleftTab(3);"><a href="javascript:;">我们是谁</a></li>
                    <li showtag="11" class="second_item" onclick="changeleftTab(11);"><a href="javascript:;">公司相册</a></li>
                    <li showtag="8" class="second_item" onclick="changeleftTab(8);"><a href="javascript:;">我们的产品</a></li>
                    <li showtag="9" class="second_item" onclick="changeleftTab(9);"><a href="javascript:;">技术特长</a></li>
                    <li showtag="6" class="second_item" onclick="changeleftTab(6);"><a href="javascript:;">银禾荣誉</a> </li>
                    <li class="title" showtag="7" onclick="changeleftTab(7);"><a href="javascript:;">成功实践</a> </li>
                    <li class="title" showtag="4" onclick="changeleftTab(4);"><a href="javascript:;">银禾专访</a> </li>
                    <li class="title" showtag="2" onclick="changeleftTab(2);"><a href="javascript:;">合作客户</a></li>
                    <li class="title" showtag="5" onclick="changeleftTab(5);"><a href="javascript:;">招聘编辑</a> </li>
                    <li class="title" showtag="10"  onclick="changeleftTab(10);"><a href="javascript:;">联系我们</a> </li>   
                 </ul>
            </div>
        </div>
        <div class="mainright" id="addInfoDiv" lurl="">
        </div>
    </div>
    </div>

    <script type="text/javascript">
        $(document).ready(function () {
            var curHash = window.location.hash;
            var showTag = curHash.replace("#", "");
            if (showTag == "") showTag = 1;
            changeleftTab(showTag);
         
        });

        function changeleftTab(showTag) {

            var url = "/CompanySite/CompanyList?r=" + Math.random();
            if (showTag == 10) url = "/CompanySite/CompanyList?r=" + Math.random();
            if (showTag == 2) url = "/CompanySite/ComXheditor?htmlField=cooperationClient&r=" + Math.random();
            if (showTag == 3) url = "/CompanySite/ComXheditor?htmlField=aboutUs&r=" + Math.random();
            if (showTag == 4) url = "/CompanySite/ClientInterviewList?r=" + Math.random();
            if (showTag == 5) url = "/CompanySite/RecruitmentList?r=" + Math.random();
            if (showTag == 6) url = "/CompanySite/HonorList?r=" + Math.random();
            if (showTag == 7) url = "/CompanySite/CooperationClientList?r=" + Math.random();
            if (showTag == 8) url = "/CompanySite/ComXheditor?htmlField=ourProducts&r=" + Math.random();
            if (showTag == 9) url = "/CompanySite/ComXheditor?htmlField=ourTech&r=" + Math.random();
            if (showTag == 1) url = "/CompanySite/ComXheditor?htmlField=introduceInfo&r=" + Math.random();
            if (showTag == 11) url = "/CompanySite/AlbumList?r=" + Math.random();
            $("#addInfoDiv").load(url);
            $("#addInfoDiv").attr("lurl", url);
            window.location.hash = showTag;

            $("#leftTabDiv").find("li").each(function () {
                $(this).removeClass("active");
            });

            $("#leftTabDiv").find("li[showtag=" + showTag + "]").addClass("active");
        }

        //刷新页面
        function ReloadDiv() {
            $("#addInfoDiv").load($("#addInfoDiv").attr("lurl"));
        }

          

    </script>
</asp:Content>
