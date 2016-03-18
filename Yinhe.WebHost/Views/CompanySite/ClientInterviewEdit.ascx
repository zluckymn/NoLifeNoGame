<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
<% 
   var interviewId = PageReq.GetParam("interviewId");
   var fileRelList = dataOp.FindAllByQuery("FileRelation", Query.And( //所有封面图关联
            Query.EQ("tableName", "ClientInterview"),
            Query.EQ("keyName", "interviewId"),
            Query.EQ("keyValue", interviewId.ToString()),
            Query.EQ("fileObjId", "203"))).ToList();
            
    var fileList = dataOp.FindAllByQuery("FileLibrary", Query.In("fileId", fileRelList.Select(c => c.GetValue("fileId")))).ToList();  //所有封面图
    var compObj = dataOp.FindOneByKeyVal("ClientInterview", "interviewId", interviewId.ToString());
    var queryStr = string.Empty;
    if (compObj.Int("interviewId")!=0)
    {
          queryStr = "db.ClientInterview.distinct('_id',{'interviewId':'" + interviewId.ToString() + "'})";
      
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
    <input type="hidden" name="interviewId" value="<%=compObj.Text("interviewId")%>" />
    <input type="hidden" name="tbName" value="ClientInterview" />
    <input type="hidden" name="queryStr" value="<%=queryStr %>" />
    <div class="Editcontent">
        <table>
            <tr>
                <td width="70" height="35">
                    嘉宾：
                </td>
                <td width="250">
                    <input type="text" name="name" class="inputborder"   style="width: 150px"
                       value="<%=compObj.String("name") %>" />
                </td>
                 <td width="70" height="35">
                    职位：
                </td>
                <td>
                    <input type="text" name="position" class="inputborder"   style="width: 150px"
                       value="<%=compObj.String("position") %>" />
                </td>
               
            </tr>
            <tr>
                <td height="35">
                    地址：
                </td>
                <td>
                    <input type="text" name="address" class="inputborder"   style="width: 150px"  value="<%=compObj.String("address") %>" />
                </td>
                <td>
                    主题：
                </td>
                <td>
                    <input type="text" name="theme" class="inputgreenbgCursor" style="width: 150px" 
                        value="<%=compObj.String("theme") %>"   />
                </td>
            </tr>
             <tr>
                <td>
                    时间：
                </td>
                <td colspan="3">
                    <input type="text" name="buildDate" class="inputgreenbgCursor" style="width: 150px" readonly
                        value="<%=compObj.String("buildDate") %>"  onclick="WdatePicker({dateFmt:'yyyy-MM-dd'})"  />
                </td>
            </tr>

            
             <tr>
                <td height="35" valign="top" style=" padding-top:8px;*padding-top:13px">
                    核心提示：
                </td>
                <td colspan="3"  valign="top" style=" padding-top:8px">
                    <textarea  name="remark" rows="6" cols="57" class="wordwrap"><%=compObj.String("remark")%></textarea>
               </td>
               
             </tr>

              <tr>
                <td height="35" valign="top" style=" padding-top:8px;*padding-top:13px">
                    详细内容：
                </td>
                <td colspan="3"  valign="top" style=" padding-top:8px">
                    <textarea  name="detailRemark" rows="6" cols="57" class="wordwrap"><%=compObj.String("detailRemark")%></textarea>
               </td>
             </tr>
        </table>
    </div>
    </form>
</div>
 <a class="sign_in_button_2" onclick="SaveCompany()" href="javascript:;">保存<span></span></a></br>
<%if (compObj.Int("interviewId") != 0)
  { %>
<div style="display:none">
    <h2>
        缩略图附件</h2>
    <hr />
    <div class="Editcontent">
        <div class="con">
            <table width="100%" class="tableborder">
                <tr class="tableborder-title">
                    <td width="30" align="center">
                        <input type="checkbox" name="seriesCover" />
                    </td>
                    <td>
                        文件名称
                    </td>
                    <td width="80" align="center">
                        大小
                    </td>
                    <td width="60" align="center">
                        封面图
                    </td>
                    <td width="100" align="center">
                        操作
                    </td>
                </tr>
                <%  foreach (var tempRel in fileRelList)
                    {
                        var tempFile = fileList.Where(t => t.Int("fileId") == tempRel.Int("fileId")).FirstOrDefault();
                %>
                <tr>
                    <td width="30" align="center">
                        <input type="checkbox" name="seriesCover" value="<%=tempFile.Int("fileId") %>" />
                    </td>
                    <td>
                        <a href="javascript:void(0);" class="abrown title_name" onclick='<%=FileCommonOperation.GetClientOnlineRead(tempFile) %>'>
                            <%=tempFile.String("name")%></a>
                    </td>
                    <td width="80" align="center">
                        <%=tempFile.String("size")%>
                    </td>
                    <td width="60" align="center">
                        <input name="rdoCover" type="radio" value="<%=tempFile.Int("fileId") %>" <%if(tempRel.Text("isCover").ToLower() == "true"){ %>
                            checked<%} %> onclick='SetCoverImage("<%=tempRel.Int("fileRelId") %>");' />
                    </td>
                    <td width="100" align="center">
                        <a class="blue" href="javascript:void(0);" onclick='<%=FileCommonOperation.GetClientOnlineRead(tempFile) %>'>
                            阅读</a>&nbsp;<a href="javascript:void(0);" class="red" onclick='DeleteFileByFileId("<%=tempFile.Text("fileId")%>");'>
                                删除</a>
                    </td>
                </tr>
                <%  } %>
            </table>
        </div>
        <div style="padding-top: 5px;">
            <input type="hidden" id="fileTypeId" name="fileTypeId" value="0" />
            <input type="hidden" id="fileObjId" name="fileObjId" value="0" />
            <input type="hidden" id="keyValue" name="keyValue" value="0" />
            <input type="hidden" id="tableName" name="tableName" value="0" />
            <input type="hidden" id="keyName" name="keyName" value="0" />
            <input type="hidden" id="fileSaveType" name="fileSaveType" value="multiply" />
            <input type="hidden" id="uploadUUID" name="uploadUUID" />
            <input type="hidden" id="uploadType" name="uploadType" value="1" />
            <input type="hidden" id="delFileRelIds" name="delFileRelIds" value="" />
            <input type="hidden" id="uploadFileList" name="uploadFileList" />
            <a href="javascript:;" class="N-btn0003" uploadtype="1" filetypeid="0" fileobjid="203"
                keyvalue="<%=interviewId %>" tablename="ClientInterview" keyname="interviewId" onclick='UploadFiles(this,"true");'>
                <img src="<%=SysAppConfig.HostDomain%>/Content/images/icon/ico-up.png" />上传封面图<span></span></a>
        </div>
       
    </div>
</div>
<br /><br /><br />
 <a href="javascript:;" onclick='$("#imgDiv").toggle()'>查看/隐藏图片</a>
 <%if (!string.IsNullOrEmpty(compObj.Text("ImagePath"))){ %>
  <div id="imgDiv"  ><img src="<%= compObj.Text("ImagePath")%>"/></div></br>
  <%} %>
  <input type="file" id="file" name="file"/>  <input type="hidden" id="flag" name="flag" value="ajax文件上传"/> <input type="button" id="btnUpload" value="上传图片" />
<%} %>
 
 
<script type="text/javascript">
    $(document).ready(function () {
        cwf.view.dlgHelp.init();
        $("#btnUpload").click(function () {
            ajaxFileUpload();
        });
    });

    function ajaxFileUpload() {

        var data = { tableName: 'ClientInterview', keyValue: '<%=compObj.Int("interviewId") %>', keyField: 'interviewId', fieldName: "ImagePath" }
        var elementIds = ["flag"]; //flag为id、name属性名
        $.ajaxFileUpload({
            url: '/CompanySite/SaveAjaxImage',
            type: 'post',
            data: data,
            secureuri: false, //一般设置为false
            fileElementId: 'file', // 上传文件的id、name属性名
            dataType: 'Text', //返回值类型，一般设置为json、application/json
            elementIds: elementIds, //传递参数到服务器
            success: function (data, status) {
               // alert(status)
                ReloadDiv();
            },
            error: function (data, status, e) {
                alert(status)

            }
        });
        ReloadDiv();
        //return false;
    }
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
