<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%
    var companyList = dataOp.FindAll("Recruitment").OrderBy(c=>c.Int("order").ToList();
   
%>
 <div>
    <table width="100%" class="tableborder" id="segmentConcerns">
        <tr class="tableborder-title">
            <td width="150">
                标题
            </td>
            <td width="200" >
                职位
            </td>
            <td width="200" >
                工作职责
            </td>
            <td>
                要求
            </td>
          
            
            <td width="70" align="center">
                操作
            </td>
        </tr>
      
        <%foreach (var concern in companyList)
          {
              var queryStr = "db.Recruitment.distinct('_id',{'recruId':'" + concern.String("recruId") + "'})";
              %>
        <tr>
            <td class="edit">
                <div class="tb_con"><%=concern.String("title")%></div>
              
            </td>
            <td class="edit">
                <div class="tb_con"><%=concern.String("position")%></div>
              
            </td>
            <td class="edit">
                <div class="tb_con"><%=concern.String("responsibilities")%></div>
             
            </td>
            <td class="edit">
                <div class="tb_con"><%=concern.String("requirement")%></div>
            
            </td>
             <td align="center">
               <a href="javascript:;" class="blue" onclick="editCompany(<%=concern.Text("recruId") %>)" >编辑</a>
                <a href="javascript:;" class="red" onclick="CustomDeleteItem(this,changeleftTab,1)" tbName="Recruitment" queryStr="<%=queryStr%>">删除</a>
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
    function editCompany(recruId) {
        $("#addInfoDiv").load("/CompanySite/RecruitmentEdit?recruId=" + recruId);
        if (recruId != 0)
        $("#addInfoDiv").attr("lurl","/CompanySite/RecruitmentEdit?recruId=" + recruId);
    }

   
</script>