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
     var recruitmentList = dataOp.FindAll("Recruitment").ToList();
      %>
   <%Html.RenderPartial("Nav");%>
<div class="content_width">
   <div class="block_content_first">
       <div class="arro"></div>
       <div class="p_block_N"></div>
     <div class="title">
       <div class="title_name_C fontred">招聘信息</div> 
       <div class="title_name_E">RECRUITMENT</div> 
     </div>
     <div class="p_block_l"></div><div class="p_block_l"></div>
      <div class="recruitment">
         <div class="Node Node1"><a href="#recr_node1"></a></div>
         <div class="Node Node2"><a href="#recr_node2"></a></div>
         <div class="Node Node3"><a href="#recr_node3"></a></div>
         <div class="Node Node4"><a href="#recr_node7"></a></div>
         <div class="Node Node5"><a href="#recr_node6"></a></div>
         <div class="Node Node6"><a href="#recr_node5"></a></div>
         <div class="Node Node7"><a href="#recr_node4"></a></div>
      </div>
     <div class="p_block_l"></div>

     <%foreach(var recruitment in recruitmentList){ %>
     <div class="recruitment_item">
     <div class="recruitment_title"><a name="recr_node1"></a><%=recruitment.Text("title")%> </div>
     <div class="recruitment_content">
       职位要求：<br />
       <%=recruitment.Text("requirement").Replace("\n\r", "<br>").Replace("\r\n", "<br>")%>
         <div class="p_block_S"></div>
      工作职责：<br />
      <%=recruitment.Text("responsibilities").Replace("\n\r", "<br>").Replace("\r\n", "<br>")%>
          <div class="p_block_S"></div>
         <b>请发简历到：</b><span class="fontred"><%=recruitment.Text("contact")%></span>
     </div>
     </div>
       <div class="p_block_l"></div>
       <div class="p_block_s"></div>
     
     <%} %>
  
   </div>
</div>
</asp:Content>
