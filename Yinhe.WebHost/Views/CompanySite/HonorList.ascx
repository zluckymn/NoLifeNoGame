<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%
    var companyList = dataOp.FindAll("CompanyHonor").ToList();
   
%>
 <div>
    <table width="100%" class="tableborder" id="segmentConcerns">
        <tr class="tableborder-title">
            <td width="100">
                公司名称
            </td>
            <td  width="175">
                称号
            </td>
            <td  width="175">
                时间
            </td>
            
            <td width="70" align="center">
                操作
            </td>
        </tr>
      
        <%foreach (var concern in companyList)
          {
              var queryStr = "db.CompanyHonor.distinct('_id',{'honorId':'" + concern.String("honorId") + "'})";
              %>
        <tr>
            <td class="edit">
                <div class="tb_con"><%=concern.String("name")%></div>
              
            </td>
            <td class="edit">
                <div class="tb_con"><%=concern.String("honor")%></div>
              
            </td>
            <td class="edit">
                <div class="tb_con"><%=concern.ShortDate("buildDate")%></div>
             
            </td>
           <td align="center">
               <a href="javascript:;" class="blue" onclick="editCompany(<%=concern.Text("honorId") %>)" >编辑</a>
                <a href="javascript:;" class="red" onclick="CustomDeleteItem(this,changeleftTab,1)" tbName="CompanyHonor" queryStr="<%=queryStr%>">删除</a>
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
    function editCompany(honorId) {
        $("#addInfoDiv").load("/CompanySite/HonorEdit?honorId=" + honorId);
        if (honorId != 0)
        $("#addInfoDiv").attr("lurl","/CompanySite/HonorEdit?honorId=" + honorId);
      
    }
 
</script>