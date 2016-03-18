<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%
    var companyList = dataOp.FindAll("ClientInterview").ToList();
   
%>
 <div>
    <table width="100%" class="tableborder" id="segmentConcerns">
        <tr class="tableborder-title">
            <td width="100">
                嘉宾
            </td>
            <td >
                主题
            </td>
            <td width="175">
                地址
            </td>
            <td width="175" >
                时间
            </td>
            <td width="70" align="center">
                操作
            </td>
        </tr>
      
        <%foreach (var concern in companyList.OrderByDescending(c=>c.Date("buildDate")))
          {
              var queryStr = "db.ClientInterview.distinct('_id',{'interviewId':'" + concern.String("interviewId") + "'})";
              %>
        <tr>
            <td class="edit">
                <div class="tb_con"><%=concern.String("name")%></div>
              
            </td>
            <td class="edit">
                <div class="tb_con"><%=concern.String("theme")%></div>
              
            </td>
           
             <td class="edit">
                <div class="tb_con"><%=concern.String("address")%></div>
            </td>
             <td class="edit">
                <div class="tb_con"><%=concern.DateFormat("buildDate","yyyyMMdd")%>期</div>
             
            </td>
            <td align="center">
               <a href="javascript:;" class="blue" onclick="editCompany(<%=concern.Text("interviewId") %>)" >编辑</a>
                <a href="javascript:;" class="red" onclick="CustomDeleteItem(this,changeleftTab,1)" tbName="ClientInterview" queryStr="<%=queryStr%>">删除</a>
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
    function editCompany(interviewId) {
        $("#addInfoDiv").load("/CompanySite/ClientInterviewEdit?interviewId=" + interviewId);
        if (interviewId!=0)
        $("#addInfoDiv").attr("lurl","/CompanySite/ClientInterviewEdit?interviewId=" + interviewId);
    }

   
</script>