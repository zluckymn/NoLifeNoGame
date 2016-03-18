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
   
     var clientList = dataOp.FindAll("CooperationClient").ToList();
   
      %>
   <%Html.RenderPartial("Nav");%>
<div class="content_width">
   <div class="block_content_first">
       <div class="arro"></div>
       <div class="p_block_N"></div>
     <div class="title">
       <div class="title_name_C fontred">成功实践</div> 
       <div class="title_name_E">VERIFICATION</div> 
     </div>
     <div class="p_block_l"></div>
      <div class="p_block_l"></div>
      <div class="pic_scoll verification_list">
      <%foreach(var client in clientList) {%>
         <div class="pic_block">
            <div class="image">
               <div class="pic"><a href="/CompanySite/ClientDetail/?clientId=<%=  client.Text("clientId")%>" target=_blank ><img src="<%= client.Text("ImagePath")%>" /></a></div>
            </div>
            <div class="name"><%=client.Text("title").Replace("\r\n", "<br/>")%></div>
            <div class="detail">
              <%=client.Text("introduceInfo").Replace("\r\n", "<br/>")%>   </div>
         </div>
         <%} %>
          
      </div>
      <div class="p_block_l"></div>
       <div class="p_block_l"></div>
   </div>
</div>
</asp:Content>
