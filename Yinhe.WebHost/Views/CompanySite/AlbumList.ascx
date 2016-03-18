<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%
    var companyList = dataOp.FindAll("CompanyAlbum").ToList();
   
%>
 <div>
    <table width="100%" class="tableborder" id="segmentConcerns">
        <tr class="tableborder-title">
            <td width="100">
                相册名称
            </td>
            <td >
               描述
            </td>
            <td >
                时间
            </td>
             <td width="70" align="center">
                操作
            </td>
        </tr>
      
        <%foreach (var concern in companyList)
          {
              var queryStr = "db.CompanyAlbum.distinct('_id',{'albumId':'" + concern.String("albumId") + "'})";
              %>
        <tr>
            <td class="edit" width="60">
                <div class="tb_con"><%=concern.String("name")%></div>
              
            </td>
            <td class="edit" width="175">
                <div class="tb_con"><%=concern.String("remark")%></div>
              
            </td>
            <td class="edit" width="175">
                <div class="tb_con"><%=concern.ShortDate("buildDate")%></div>
             
            </td>
           <td align="center" width="40">
               <a href="javascript:;" class="blue" onclick="editCompany(<%=concern.Text("albumId") %>)" >编辑</a>
                <a href="javascript:;" class="red" onclick="CustomDeleteItem(this,changeleftTab,1)" tbName="CompanyAlbum" queryStr="<%=queryStr%>">删除</a>
            </td>
        </tr>
        <%}%>
    </table>
    <div style="padding-top: 5px;">
        <a href="javascript:;" class="sign_in_button_2" onclick="editCompany(0)">新增</a>
    </div>
    <br />
 
</div>
<script>
    function editCompany(albumId) {
        $("#addInfoDiv").load("/CompanySite/AlbumEdit?albumId=" + albumId);
        if (albumId != 0)
            $("#addInfoDiv").attr("lurl", "/CompanySite/AlbumEdit?albumId=" + albumId);
      
    }
 
</script>