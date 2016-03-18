<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/FLKL.Master" Inherits="Yinhe.ProcessingCenter.ViewFreePageBase" %>

<%@ Import Namespace="Yinhe.ProcessingCenter.Common" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
 

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="HeadContent" runat="server">
  
  <link href="<%=SysAppConfig.HostDomain %>/Content/css/client/FLKL/css.css" rel="stylesheet"
    type="text/css" />
    <script type="text/javascript" src="/Scripts/miaov.js"></script>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
 <%
     var companyObj = dataOp.FindAll("CompanyInfo").Where(c=>c.Int("type")==1).FirstOrDefault();
   
      %>
   <%Html.RenderPartial("Nav");%>
<div class="content_width">
   <div class="block_content_first">
       <div class="arro"></div>
       <div class="p_block_N"></div>
     <div class="title">
       <div class="title_name_C fontred">合作客户</div> 
       <div class="title_name_E">COOPERATION CLIENT</div> 
     </div>
     <div class="p_block_N"></div>
       <%--<%=companyObj.Text("cooperationClient")%>--%>
       <div id="div1">
	<a>龙湖地产</a>
	<a class="red">时代地产</a>
	<a class="yellow">万科集团</a>
	<a>金地集团</a>
	<a class="blue">新城地产</a>
	<a class="red">卓越地产</a>
	<a class="yellow">特工地产</a>
	<a class="yellow">旭辉集团</a>
	<a class="red">佳兆业地产</a>
	<a>复地地产</a>
	<a class="blue">奥园地产</a>
	<a>阿特金斯</a>
	<a class="red">华侨城地产</a>
	<a class="blue">三盛地产</a>
	<a>侨鑫地产</a>
	<a class="blue">中海地产</a>
	<a>南京弘阳</a>
	<a>联发地产</a>
	<a class="yellow">建发地产</a></div>
       </div>
   </div>
</div>
</asp:Content>
