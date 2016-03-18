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
     var companyObj = dataOp.FindAll("CompanyInfo").Where(c=>c.Int("type")==1).FirstOrDefault();
     var clientList = dataOp.FindAll("CooperationClient").ToList();
      %>
   <%Html.RenderPartial("Nav");%>
   <div class="banner">
  <div class="content_width"><img src="<%=SysAppConfig.HostDomain %>/Content/images/client/FLKL/banner.jpg" /></div>
</div>
<div class="p_block_l"></div>
   <div class="content_width">
  <div class="block_content_first">
    <div class="arro"></div>
    <div class="font_pic"><img src="<%=SysAppConfig.HostDomain %>/Content/images/client/FLKL/ico0002.png" /></div>
    <div class="p_block_N"></div>
    <div class="font_l">
    <%=companyObj.Text("introduceInfo")%>
    </div>
    <div class="p_block_N"></div>
    <div class="btn_0001"><a href="/CompanySite/About">马上了解</a></div>
    <div class="p_block_l"></div>
  </div>
  <div class="p_block_N"></div>
  <div class="block_content">
    <div class="p_block_N"></div>
    <div class="title">
      <div class="title_name_C">成功实践</div>
      <div class="title_name_E">VERIFICATION</div>
    </div>
    <div class="p_block_N"></div>
    <div class="Slidebox pic_scoll">
      <div class="SBcontent">
        <ul class="SBlist">
        <%
            var length = clientList.Count();
            var size = 4;
            var index = 1;
            foreach(var client in clientList){
              var logo = client.Text("logo");
              var imgagePath = client.Text("ImagePath");
              
               %>
          <%if (index % 4 == 1)
           { %>
          <li class="SBitem">
          <%} %>
            <div class="pic_block">
              <div class="image">
                <div class="pic"><a href="/CompanySite/ClientDetail/?clientId=<%=client.Text("clientId") %>" target=_blank><img src="<%=imgagePath %>" /></a></div>
              </div>
              <div class="name"><%=client.Text("title")%></div>
              <div class="detail"> <%=client.Text("introduceInfo")%><br />
               </div>
            </div>
           <%if (index % 4 == 0 || index >= length)
              {%>
          </li>
          <%} index++; %>
          <%} %>
        </ul>
      </div>
      <div class="SBnav PNG_FIX">
          <div class="btn"> <a class="goRight" href="#">&gt;</a> <a class="goLeft" href="#">&lt;</a> </div>
      </div>
    </div>
    
    <div class="p_block_l"></div>
  </div>
  <div class="p_block_N"></div>
  <!----<div class="block_content">
    <div class="p_block_S"></div>
    <div class="Interview">
    <%var clientInterViewList = dataOp.FindAll("ClientInterview").OrderByDescending(c => c.Date("buildDate")).Take(5).ToList();
      var lastInterViewObj = clientInterViewList.FirstOrDefault();
      if (lastInterViewObj == null) lastInterViewObj = new BsonDocument();
      var nextInterViewList = clientInterViewList.Where(c => c.Int("interviewId") != lastInterViewObj.Int("interviewId")).ToList();
      %>
      <div class="title">银禾<span class="fontred">专访</span> <span class="fontsize_N">（第<span class="fontred"><%=lastInterViewObj.DateFormat("buildDate","yyyyMMdd") %></span>期）</span></div>
      <div class="p_block_S"></div>
      <div class="pic"><a href="/CompanySite/InterviewDetail/?interviewId=<%=lastInterViewObj.Text("interviewId") %>" target=_blank ><img src="<%=lastInterViewObj.Text("ImagePath") %>" /></a></div>
      <div class="detail">
        <div class="name"><a href="/CompanySite/InterviewDetail/?interviewId=<%=lastInterViewObj.Text("interviewId") %>" target=_blank > 本期嘉宾：<%= lastInterViewObj.Text("name")%></a></div>
        主题：<%=lastInterViewObj.Text("Theme")%><br />
        地点：<%=lastInterViewObj.Text("address")%><br />
        时间：<%=lastInterViewObj.Text("buildDate")%> </div>
      <div class="p_block_S"></div>
      <div class="past"> 往期访谈<br />
      <%foreach(var interView in nextInterViewList) {%>
        <a href="/CompanySite/InterviewDetail/?interviewId=<%=interView.Text("interviewId") %>" target=_blank><img width="45px" height="45px" src="<%=interView.Text("ImagePath")%>"/> </a>
         <%} %>
         </div>
      <div class="oper"> <a href="/CompanySite/Interview" class="fontred">更多>></a> </div>
    </div>
    <div class="Honor ZZ_tab_click">
      <div class="title">银禾<span class="fontred">荣誉</span></div>
      <div class="timeline_box">
      <%
          //获取银禾荣耀
          var companyHonorList = dataOp.FindAll("CompanyHonor").OrderBy(c=>c.Date("buildDate")).ToList();
          var lastObj = companyHonorList.FirstOrDefault();
          var latestObj = companyHonorList.OrderByDescending(c => c.Date("buildDate")).FirstOrDefault();
          var lastYea = lastObj != null ? lastObj.Date("buildDate").Year : 2008;
          var latest = latestObj != null ? latestObj.Date("buildDate").Year : DateTime.Now.Year;
        %>
        <div class="timeline">
          <ul>
          <%for (var i = lastYea; i <= latest;i++)
            {%>
            <li class="ZZ_tab_head">
              <div class="year"><%=i%></div>
              <div class="dot"></div>
            </li>
            <%} %>
          </ul>
        </div>
      </div>
      <div class="timeline_content ">
      <%foreach(var honor in companyHonorList){ %>
        <div class="timeline_content_block ZZ_tab_body">
          <div class="pic"><img src="<%=honor.Text("ImagePath") %>" /></div>
          <div class="detail">
            <div class="year fontred"><%=honor.Date("buildDate").Year%></div>
            <%=honor.Text("introduceInfo")%> </div>
        </div>
        <%} %>
      </div>
      <div class="p_block_S"> </div>
    </div>
  </div>---->
</div>
<script src="/Content/js/slidebox.js" type="text/javascript"></script>
<script src="/Content/js/yinhoo.web.js" type="text/javascript"></script>
</asp:Content>
