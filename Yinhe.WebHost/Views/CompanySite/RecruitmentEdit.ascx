<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
<% 
   var recruId = PageReq.GetParam("recruId");
    var compObj = dataOp.FindOneByKeyVal("Recruitment", "recruId", recruId.ToString());
    var queryStr = string.Empty;
    if (compObj.Int("recruId")!=0)
    {
          queryStr = "db.Recruitment.distinct('_id',{'recruId':'" + recruId.ToString() + "'})";
      
    }
    else
    {
        compObj = new BsonDocument();
    }
   
%>
<div>
    <h2>
        基本信息</h2>
    <hr />
     <form action="" name="companyForm" action="" id="companyForm" method="post">
    <input type="hidden" name="recruId" value="<%=compObj.Text("recruId")%>" />
    <input type="hidden" name="tbName" value="Recruitment" />
    <input type="hidden" name="queryStr" value="<%=queryStr %>" />
    <div class="Editcontent">
        <table>
            <tr>
                <td width="70" height="35">
                    标题：
                </td>
                <td width="250">
                    <input type="text" name="title" class="inputborder"   style="width: 150px"
                       value="<%=compObj.String("title") %>" />
                </td>
                <td width="70">
                    职位：
                </td>
                <td>
                   <input type="text" name="position" class="inputborder"   style="width: 150px"
                       value="<%=compObj.String("position") %>" />
                </td>
            </tr>
            <tr>
                <td height="35">
                    人数：
                </td>
                <td>
                    <input type="text" name="userCount" class="inputborder"   style="width: 150px"
                       value="<%=compObj.String("userCount") %>" />
                </td>
                <td>
                    联系方式：
                </td>
                <td>
                    <input type="text" name="contact" class="inputgreenbgCursor" style="width: 150px"  
                        value="<%=string.IsNullOrEmpty(compObj.String("contact"))?"lin.yan@yinhootech.com":compObj.String("contact")%>"   />
                </td>
            </tr>
          
               <tr>
                <td height="35" valign="top" style=" padding-top:8px;*padding-top:13px">
                    职位要求：
                </td>
                <td colspan="3" valign="top" style=" padding-top:8px">
                    <textarea width="270" name="requirement" rows="6" cols="57" class="wordwrap"><%=compObj.String("requirement")%></textarea>
               </td>
             </tr>
               <tr>
                <td height="35" valign="top" style=" padding-top:8px;*padding-top:13px">
                    工作职责：
                </td>
                <td colspan="3" valign="top" style=" padding-top:8px">
                    <textarea  width="270" name="responsibilities" rows="6" cols="57" class="wordwrap"><%=compObj.String("responsibilities")%></textarea>
               </td>
             </tr>
             <tr>
                <td height="35" valign="top" style=" padding-top:8px;*padding-top:13px">
                    备注说明：
                </td>
                <td colspan="3"  valign="top" style=" padding-top:8px">
                    <textarea  name="remark" rows="6" cols="57" class="wordwrap"><%=compObj.String("remark")%></textarea>
               </td>
             </tr>
        </table>
    </div>
    </form>
</div>
 
  <a class="sign_in_button_2" onclick="SaveCompany()" href="javascript:;">保存<span></span></a>
 
<script type="text/javascript">
    $(document).ready(function () {
        cwf.view.dlgHelp.init();
    });

    function SaveCompany(obj) {
        var formData = $("#companyForm").serialize();
        $.ajax({
            url: '/Home/SavePostInfo',
            type: 'post',
            data: formData,
            dataType: 'json',
            error: function () {
                $.tmsg("m_jfw", "未知错误，请联系服务器管理员，或者刷新页面重试", { infotype: 2 });
            },
            success: function (data) {
                if (data.Success == false) {
                    $.tmsg("m_jfw", data.Message, { infotype: 2 });

                }
                else {
                    $.tmsg("m_jfw", "保存成功", { infotype: 1 });
                    ReloadDiv();
                }
            }
        });
    }

 
</script>
