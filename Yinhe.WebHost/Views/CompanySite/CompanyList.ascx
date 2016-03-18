<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%
    var companyList = dataOp.FindAll("CompanyInfo").ToList();
   
%>
 <div>
    <table width="100%" class="tableborder" id="segmentConcerns">
        <tr class="tableborder-title">
            <td width="150">
                公司名称
            </td>
          
            <td >
                文化
            </td>
            <td width="150">
                宗旨
            </td>
            <td width="250">
                公司地址
            </td>
            
            <td width="70" align="center">
                操作
            </td>
        </tr>
      
        <%foreach (var concern in companyList)
          {
              var queryStr = "db.CompanyInfo.distinct('_id',{'compId':'" + concern.String("compId") + "'})";
              %>
        <tr>
            <td class="edit">
                <div class="tb_con"><%=concern.String("name")%></div>
              
            </td>
           
            <td class="edit">
                <div class="tb_con"><%=concern.String("culture")%></div>
             
            </td>
            <td class="edit">
                <div class="tb_con"><%=concern.String("purpose")%></div>
            
            </td>
            <td class="edit">
                <div class="tb_con"><%=concern.String("address")%></div>
            
            </td>
            <td align="center">
               <a href="javascript:;" class="blue" onclick="editCompany(<%=concern.Text("compId") %>)" >编辑</a>
                <a href="javascript:;" class="red" onclick="CustomDeleteItem(this,changeleftTab,1)" tbName="CompanyInfo" queryStr="<%=queryStr%>">删除</a>
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
   function editCompany(compId) {
       $("#addInfoDiv").load("/CompanySite/CompanyEdit?compId=" + compId);
       if (compId != 0)
       $("#addInfoDiv").attr("lurl", "/CompanySite/CompanyEdit?compId=" + compId);
    }

   
</script>