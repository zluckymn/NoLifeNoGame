<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/FLKL.Master" Inherits="Yinhe.ProcessingCenter.ViewFreePageBase" %>

<%@ Import Namespace="Yinhe.ProcessingCenter.Common" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
 

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="HeadContent" runat="server">
  
  <link href="<%=SysAppConfig.HostDomain %>/Content/css/client/FLKL/css.css" rel="stylesheet"
    type="text/css" />
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
 <%
     var companyList = dataOp.FindAll("CompanyInfo").ToList();
      %>
   <%Html.RenderPartial("Nav");%>
<div class="content_width">
   <div class="block_content_first">
       <div class="arro"></div>
       <div class="p_block_N"></div>
     <div class="title">
       <div class="title_name_C fontred">联系我们</div> 
       <div class="title_name_E">CONTACT US</div> 
     </div>
     <div class="p_block_l"></div><div class="p_block_l"></div>
     <%foreach (var company in companyList)
       { %>
     <div class="contact">
    <div class="map"><img src="<%=company.Text("companyImage") %>"g" width="200" height="150" /><br />
          [<a href="#" class="showMap">点击查看大图</a>]       </div>
          <div class="contact_info">
            <div class="name fontred"><%=company.Text("name")%></div>
              <div><%=company.Text("address")%><br />
                联 系 人：<%=company.Text("contact")%> <br />
                电话：：<%=company.Text("telphone")%> <br />
              邮件：<%=company.Text("email")%>  </div>
          </div>
     </div>
     <div class="p_block_l"></div>
    <%} %>
       <div class="p_block_l"></div>
       <div class="p_block_l"></div>
   </div>
</div>
<div class="pop">
	<div class="pop-close"></div>
	<div class="popMap">
    	<div id="baiduMap" class="baiduMap"></div>
    </div>
</div>

<script src="/Content/js/yinhoo.web.js" type="text/javascript"></script>
<script type="text/javascript" src="http://api.map.baidu.com/api?v=2.0&ak=NGsX55I0WlpUKox7vIAWeQIl"></script>
</asp:Content>
