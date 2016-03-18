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
   
     var clientInterViewList = dataOp.FindAll("ClientInterview").OrderByDescending(c => c.Date("buildDate")).ToList();
     var lastInterViewObj = clientInterViewList.FirstOrDefault();
     if (lastInterViewObj == null) lastInterViewObj = new BsonDocument();
    var nextInterViewList = clientInterViewList.Where(c => c.Int("interviewId") != lastInterViewObj.Int("interviewId")).ToList();
      %>
   <%Html.RenderPartial("Nav");%>
<div class="content_width">
   <div class="block_content_first">
       <div class="arro"></div>
       <div class="p_block_N"></div>
     <div class="title">
       <div class="title_name_C fontred">银禾专访</div> 
       <div class="title_name_E">YINHOO INTERVIEW</div> 
     </div>
     <div class="p_block_l"></div>
      <div class="Now_interview">  
         <div class="pic"><a href="/CompanySite/InterviewDetail/?interviewId=<%=lastInterViewObj.Text("interviewId") %>"><img src="<%=lastInterViewObj.Text("ImagePath") %>" /></a><br />
        （第<span class="fontred"><%=lastInterViewObj.DateFormat("buildDate","yyyyMMdd") %></span>期）</div>
         <div class="info">
           <div class="name">本期嘉宾：<%=lastInterViewObj.Text("name")%></div>
            主题：<%=lastInterViewObj.Text("Theme")%><br />
地点：<%=lastInterViewObj.Text("address")%><br />
时间：<%=lastInterViewObj.Text("buildDate")%>
      <div class="detail">
         <%= lastInterViewObj.Text("remark")%>
      </div>
      <div class="more">
          <a href="/CompanySite/InterviewDetail/?interviewId=<%=lastInterViewObj.Text("interviewId") %>">详细阅读</a>      </div>
        </div>
     </div>
     <div class="p_block_l"></div>
      <div class="interview_list">
      <%foreach(var interView in nextInterViewList) {%>
          <div class="interview_list_block">
              <div class="pic"><a href="/CompanySite/InterviewDetail/?interviewId=<%=interView.Text("interviewId") %>"><img src="<%=interView.Text("ImagePath") %>" /></a></div>
              <div class=" detail">
                <div class="detail_title"><a href="/CompanySite/InterviewDetail/?interviewId=<%=interView.Text("interviewId") %>"><%=interView.Text("position")%><%=interView.Text("name")%>：<%=interView.Text("theme")%></a></div>
                 <div class="detail_list">
                 <%=interView.Text("remark")%>
                 </div>
                 <div class="detail_info">
                   （第<span class="fontred"><%=interView.DateFormat("buildDate", "yyyyMMdd")%></span>期）
                 </div>
              </div>
          </div>
    <%} %>
      
      
        </div>
       <div class="p_block_l"></div>
       <div class="p_block_l"></div>
   </div>
</div>
</asp:Content>
