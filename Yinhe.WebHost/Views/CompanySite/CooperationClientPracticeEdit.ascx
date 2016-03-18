<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
<% 
   var practiceId = PageReq.GetParam("practiceId");
    var clientId = PageReq.GetParam("clientId");
    var compObj = dataOp.FindOneByKeyVal("CooperationClientPractice", "practiceId", practiceId.ToString());
    var queryStr = string.Empty;
    if (compObj.Int("practiceId")!=0)
    {
          queryStr = "db.CooperationClientPractice.distinct('_id',{'practiceId':'" + practiceId.ToString() + "'})";
      
    }
    else
    {
        compObj = new BsonDocument();
    }
   
%>
 
      <form action="" name="companyForm" action="" id="companyForm" method="post">
    <input type="hidden" name="practiceId" value="<%=compObj.Text("practiceId")%>" />
     <input type="hidden" name="clientId" value="<%=clientId%>" />
    <input type="hidden" name="tbName" value="CooperationClientPractice" />
    <input type="hidden" name="queryStr" value="<%=queryStr %>" />
    <div class="Editcontent">
        <table>
            <tr>
               
                <td width="70">
                    时间：
                </td>
                <td>
                    <input type="text" name="buildDate" class="inputgreenbgCursor" style="width: 150px" 
                        value="<%=compObj.String("buildDate") %>"    />
                </td>
            </tr>
            <tr>
                <td width="270" height="35" valign="top" style=" padding-top:8px;*padding-top:13px">
                    详细内容：
                </td>
                <td colspan="3"  valign="top" style=" padding-top:8px">
                    <textarea     name="summary" rows="6" cols="17" class="wordwrap"><%=compObj.String("summary")%></textarea>
               </td>
             </tr>
        </table>
    </div>
    </form>
    <script>
    
    
    </script>
 
 
 
 
 
