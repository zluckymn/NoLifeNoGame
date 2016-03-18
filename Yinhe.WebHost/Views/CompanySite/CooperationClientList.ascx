<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%
    var companyList = dataOp.FindAll("CooperationClient").ToList();
   
%>
 <div>
    <table width="100%" class="tableborder" id="segmentConcerns">
        <tr class="tableborder-title">
            <td width="100">
               客户名称
            </td>
            <td >
                标题
            </td>
          <%--  <td >
                客户负责人
            </td>--%>
            <td width="175">
                介绍
            </td>
            <td width="175">
                总结
            </td>
            
            <td width="70" align="center">
                操作
            </td>
        </tr>
      
        <%foreach (var concern in companyList)
          {
              var queryStr = "db.CooperationClient.distinct('_id',{'clientId':'" + concern.String("clientId") + "'})";
              %>
        <tr>
            <td class="edit">
                <div class="tb_con"><%=concern.String("name")%></div>
              
            </td>
            <td class="edit">
                <div class="tb_con"><%=concern.String("title")%></div>
              
            </td>
           <%-- <td class="edit" width="175">
                <div class="tb_con"><%=concern.String("clientUser")%></div>
             
            </td>--%>
            <td class="edit">
                <div class="tb_con"><%=concern.String("introduceInfo")%></div>
            
            </td>
            <td class="edit">
                <div class="tb_con"><%=concern.String("summary")%></div>
            
            </td>
            <td align="center">
               <a href="javascript:;" class="blue" onclick="editCompany(<%=concern.Text("clientId") %>)" >编辑</a>
                <a href="javascript:;" class="red" onclick="CustomDeleteItem(this,changeleftTab,1)" tbName="CooperationClient" queryStr="<%=queryStr%>">删除</a>
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
    function editCompany(clientId) {
        $("#addInfoDiv").load("/CompanySite/CooperationClientEdit?clientId=" + clientId);
        if (clientId != 0)
            $("#addInfoDiv").attr("lurl", "/CompanySite/CooperationClientEdit?clientId=" + clientId);
    }

   
</script>