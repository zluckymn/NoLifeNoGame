<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%
    var actObj = ViewContext.RouteData.Values["action"];
    var actionStr = actObj == null ? "" : actObj.ToString(); ;
     %>
 <div class="top">
  <div class="content_width">
    <div class="logo"> </div>
    <div class="nav">
      <ul >
        <li  ><a  <%if(actionStr=="Index"){ %>class="select"<%} %> href="/CompanySite/Index">首页</a></li>
        <li><a <%if(actionStr=="About"){ %>class="select"<%} %>  href="/CompanySite/About">关于我们</a></li>
        <li><a <%if(actionStr=="Verification"){ %>class="select"<%} %>  href="/CompanySite/Verification">成功实践</a></li>
        <%--<li><a <%if(actionStr=="Interview"){ %>class="select"<%} %>   href="/CompanySite/Interview">银禾专访</a></li>--%>
        <li><a <%if(actionStr=="Client"){ %>class="select"<%} %> href="/CompanySite/Client">合作客户</a></li>
        <li><a <%if(actionStr=="Recruitment"){ %>class="select"<%} %>  href="/CompanySite/Recruitment">招聘信息</a></li>
        <li><a <%if(actionStr=="Contact"){ %>class="select"<%} %>  href="/CompanySite/Contact">联系我们</a></li>
      </ul>
    </div>
  </div>
</div>
