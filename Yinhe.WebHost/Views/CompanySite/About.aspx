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
     var companyObj = dataOp.FindAll("CompanyInfo").Where(c => c.Int("type") == 1).FirstOrDefault();
     var companyAlbumList = dataOp.FindAll("CompanyAlbum").ToList();
     var compAlbumFileList = dataOp.FindAll("CompanyAlbumFile").ToList();
      %>
   <%Html.RenderPartial("Nav");%>

 

<div class="content_width">
 <div class="block_content_first">
     <div class="arro"></div>
       <div class="p_block_N"></div>
     <div class="title">
       <div class="title_name_C fontred"><a class="colTitBar" name="about"></a>我们是谁</div> 
       <div class="title_name_E">WHO WE ARE</div> 
     </div>
     <div class="p_block_N"></div>
       <div class="font_style"><%= companyObj.Text("aboutUs")%></div>
       <div class="p_block_N"></div>
       <div class="p_block_l"></div>
  </div>
</div>
<div class="p_block_N"></div> 


<div class="content_width overflowHidden">  
   <div class="block_content">
     <div class="p_block_N"></div>
     <div class="title">
       <div class="title_name_C fontred"><a class="colTitBar" name="product"></a>我们的产品</div> 
       <div class="title_name_E">OUR PRODUCTS</div> 
      </div>
       <%=companyObj.Text("ourProducts")%>
       <div class="p_block_N"></div><div class="p_block_N"></div>
     
  </div>
  <div class="p_block_N"></div>
  <div class="block_content">
   <div class="p_block_N"></div>
     <div class="title">
       <div class="title_name_C fontred"><a class="colTitBar" name="culture"></a>技术特长</div> 
       <div class="title_name_E">CORPORATE CULTURE</div> 
      </div>
       <%=companyObj.Text("ourTech")%>
    <div class="p_block_l"></div> 
      <div class="p_block_l"></div>
  
    
  </div>
  <div class="p_block_N"></div>
  <%--<div class="block_content">
   <div class="p_block_N"></div>
     <div class="title">
       <div class="title_name_C fontred"><a class="colTitBar" name="honor"></a>银禾荣誉</div> 
       <div class="title_name_E">YINHOO  HONOR</div> 
      </div>
      <div class="p_block_N"></div> 
         <div style="padding:0 15px; position:relative;"> 
          <div class="timeline">
          <%  //获取银禾荣耀
              var companyHonorList = dataOp.FindAll("CompanyHonor").OrderBy(c => c.Date("buildDate")).ToList();
              var lastObj = companyHonorList.FirstOrDefault();
              var latestObj = companyHonorList.OrderByDescending(c => c.Date("buildDate")).FirstOrDefault();
              var lastYea = lastObj != null ? lastObj.Date("buildDate").Year : 2008;
              var latest = latestObj != null ? latestObj.Date("buildDate").Year : DateTime.Now.Year; %>
            <ul style="margin-left:350px;">
              <%for (var i = lastYea; i <= latest;i++)
                {%>
                <li >
                  <div class="year"><%=i%></div>
                  <div class="dot"></div>
                </li>
                <%} %>
            </ul>
          </div>
          <div class="p_block_N"></div> 
          <div class="timeline_content">
              <div class="timeline_pic"><img src="<%=SysAppConfig.HostDomain %>/Content/images/client/FLKL/pic0010.jpg" /></div>
              <div class="timeline_content_area">
               <%foreach(var honor in companyHonorList){ %>
              <div class="timeline_content_block">
                 <div class="pic"><img src="<%=honor.Text("ImagePath") %>" /></div>
                 <div class="detail">
                   <div class="year fontred"><%=honor.Date("buildDate").Year%></div>
                    <%=honor.Text("introduceInfo")%> 
                 </div>
              </div> 
              <%} %>
              </div>
              
         </div>
         </div>     
         <div class="p_block_l"></div> 
      <div class="p_block_l"></div>
  </div>--%>
</div>
 <script>
    $(document).ready(function () {
        $(".nav li").click(function () {
            $(this).addClass('select').sibling().removeClass('select');
        })

    });
</script>
</asp:Content>
