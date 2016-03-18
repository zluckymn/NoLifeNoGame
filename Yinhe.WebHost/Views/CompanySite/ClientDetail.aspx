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
     var clientId = PageReq.GetParam("clientId");
     var client = dataOp.FindOneByKeyVal("CooperationClient", "clientId", clientId);
     if (client == null) client = new BsonDocument();
     var practiceList = dataOp.FindAllByKeyVal("CooperationClientPractice", "clientId", clientId).ToList();
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
     <div class="p_block_l"></div><div class="p_block_l"></div>
     <div class="font_l">
         <img src="<%=client.Text("logo") %>" />
     </div>
     <div class="p_block_S"></div>
      <div class="font_l fontred"><b><%=client.Text("title")%>：<%=client.Text("introduceInfo")%></b></div>
      <div class="p_block_l"></div><div class="p_block_S"></div>
      <div class="pic_scoll">
      <div class="verification_block">
      <%var index=1;
        foreach (var practice in practiceList)
        { %>
         <div class="verification_block_list">
           <div class="verification_block_border">
            <div class="verification_block_year"><%=practice.Text("buildDate")%></div>
            <div class="verification_block_content">
            <p><%=practice.Text("summary").Replace("\r\n","<br/>")%></p>
            </div>
           </div>
           <div class="verification_block_bottom"></div>
         </div>
         <%if (index++ <practiceList.Count())
           {%>
         <div class="arro"></div>
         <%}
        } %>
         </div>
     
      </div>
      <div class="p_block_l"></div>
       <div class="p_block_l"></div></div>
     
  </div>
</asp:Content>
