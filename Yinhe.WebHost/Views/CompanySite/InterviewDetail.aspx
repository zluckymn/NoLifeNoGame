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
   var interviewId=PageReq.GetParam("interviewId");

   var lastInterViewObj = dataOp.FindOneByKeyVal("ClientInterview", "interviewId", interviewId);
    
      %>
   <%Html.RenderPartial("Nav");%>
<div class="content_width">
   <div class="block_content_first">
       <div class="arro"></div>
       <div class="p_block_N"></div>
     <div class="title">
       <div class="title_name_C fontred">银禾专访</div> 
       <div class="title_name_E">YINHOO INTERVIEW<br />（第<span class="fontred"><%=lastInterViewObj.DateFormat("buildDate","yyyyMMdd") %></span>期）</div> 
     </div>
     <div class="p_block_l"></div>
      <div class="Now_interview">  
         <div class="pic"><img src="<%=lastInterViewObj.Text("ImagePath")%>" /><br />
        </div>
         <div class="info">
           <div class="name">本期嘉宾：<%=lastInterViewObj.Text("name")%></div>
            主题：<%=lastInterViewObj.Text("Theme")%><br />
地点：<%=lastInterViewObj.Text("address")%><br />
时间：<%=lastInterViewObj.Text("buildDate")%>
      <div class="detail">
         <%=lastInterViewObj.Text("remark")%>
      </div>
         </div>
      </div>
     <div class="p_block_l"></div>
      <div class="interview_list detail_lineheight"> 
      <%=lastInterViewObj.Text("detailRemark").Replace("\n\r", "<br>").Replace("\r\n", "<br>")%>
      </div>
     <div class="p_block_l"></div>
       <div class="p_block_l"></div>
   </div>
</div>
</asp:Content>
