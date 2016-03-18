<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="Yinhe.ProcessingCenter.ViewPageBase" %>

<%@ Import Namespace="Yinhe.ProcessingCenter.Permissions" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Common" %>
<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="HeadContent" runat="server">
    <% Html.RenderPartial("HeadContent"); %>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
    <%
        
    %>
    <div class="container">
        <div class="path_nav">
            反馈后台管理首页</div>
        <div class="feedback-main">
            <div class="feedback-left">
                <div class="leftMenu-classify">
                    <ul id="g_tablist">
                        <li class="on" data-value="1"><i class="fl"></i><a href="javascript:void(0);">默认类<span class="fr">1</span></a></li>
                        <li data-value="2"><i class="fl"></i><a href="javascript:void(0);">bug类<span class="fr">2</span></a></li>
                        <li data-value="3"><i class="fl"></i><a href="javascript:void(0);">优化类<span class="fr">3</span></a></li>
                        <li data-value="4"><i class="fl"></i><a href="javascript:void(0);">其他<span class="fr">4</span></a></li>
                    </ul>
                </div>
            </div>
            <div id="FeedBackList">
            </div>
        </div>
    </div>
    <script type="text/javascript">
        $('#FeedBackList').load('/GeneralFeedBack/FeedBackList?type=1&_=' + Math.random());
        $("#g_tablist").find('li').click(function () {
            $(this).addClass('on').siblings().removeClass('on');
            $('#FeedBackList').load('/GeneralFeedBack/FeedBackList?type=' + $(this).attr("data-value") + '&_=' + Math.random());
        });
    </script>
</asp:Content>
