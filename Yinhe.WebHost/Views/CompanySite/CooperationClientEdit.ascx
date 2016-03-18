<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
<% 
   var clientId = PageReq.GetParam("clientId");
   var fileRelList = dataOp.FindAllByQuery("FileRelation", Query.And( //所有封面图关联
            Query.EQ("tableName", "CooperationClient"),
            Query.EQ("keyName", "clientId"),
            Query.EQ("keyValue", clientId.ToString()),
            Query.EQ("fileObjId", "204"))).ToList();
            
    var fileList = dataOp.FindAllByQuery("FileLibrary", Query.In("fileId", fileRelList.Select(c => c.GetValue("fileId")))).ToList();  //所有封面图
    var compObj = dataOp.FindOneByKeyVal("CooperationClient", "clientId", clientId.ToString());
    var queryStr = string.Empty;
    if (compObj.Int("clientId")!=0)
    {
          queryStr = "db.CooperationClient.distinct('_id',{'clientId':'" + clientId.ToString() + "'})";
      
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
    <input type="hidden" name="clientId" value="<%=compObj.Text("clientId")%>" />
    <input type="hidden" name="tbName" value="CooperationClient" />
    <input type="hidden" name="queryStr" value="<%=queryStr %>" />
    <div class="Editcontent">
        <table>
            <tr>
                <td width="70" height="35">
                    客户名称：
                </td>
                <td width="250">
                    <input type="text" name="name" class="inputborder"   style="width: 150px"
                       value="<%=compObj.String("name") %>" />
                </td>
                 <td width="80" height="35">
                    客户负责人：
                </td>
                <td>
                    <input type="text" name="clientUser" class="inputborder"   style="width: 150px"
                       value="<%=compObj.String("clientUser") %>" />
                </td>
            </tr>
            <tr>
                <td height="35">
                    标题：
                </td>
                <td>
                    <input type="text" name="title" class="inputborder"   style="width: 150px"  value="<%=compObj.String("title") %>" />
                </td>
                <td>
                  
                </td>
                <td>
                   
                </td>
            </tr>
      
            <tr>
                <td height="35" valign="top" style=" padding-top:8px;*padding-top:13px">
                    介绍：
                </td>
                <td colspan="3" width="250" valign="top" style=" padding-top:8px">
                    <textarea width="270" name="introduceInfo" rows="6" cols="57" class="wordwrap"><%=compObj.String("introduceInfo")%></textarea>
               </td>
             </tr>
              <tr>
                <td height="35" valign="top" style=" padding-top:8px;*padding-top:13px">
                    总结：
                </td>
                <td colspan="3" width="250" valign="top" style=" padding-top:8px">
                    <textarea  width="270" name="summary" rows="6" cols="57" class="wordwrap"><%=compObj.String("summary")%></textarea>
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
  <a class="sign_in_button_2" onclick="SaveCompany()" href="javascript:;">保存<span></span></a></br></br>


<%if (compObj.Int("clientId") != 0)
  {

      ///获取实践列表
      var clientPractiseList = dataOp.FindAllByKeyVal("CooperationClientPractice", "clientId", clientId.ToString());
       %>
      <div>
       <h2>
        成功实践</h2>
    <hr />
    <table width="100%" class="tableborder" id="segmentConcerns">
        <tr class="tableborder-title">
            <td width="100">
               时间
            </td>
            <td width="175">
                总结
            </td>
             <td width="70" align="center">
                操作
            </td>
        </tr>
      
        <%foreach (var practice in clientPractiseList)
          {
              var practiceQueryStr = "db.CooperationClientPractice.distinct('_id',{'practiceId':'" + practice.String("practiceId") + "'})";
              %>
        <tr>
            <td class="edit" width="60">
                <div class="tb_con"><%=practice.String("buildDate")%></div>
              
            </td>
            <td class="edit" width="175">
                <div class="tb_con"><%=practice.String("summary")%></div>
              
            </td>
            
            <td align="center" width="40">
               <a href="javascript:;" class="blue" onclick="editPractice(<%=practice.Text("practiceId") %>)" >编辑</a>
                <a href="javascript:;" class="red" onclick="CustomDeleteItem(this,ReloadDiv,1)" tbName="CooperationClientPractice" queryStr="<%=practiceQueryStr%>">删除</a>
            </td>
        </tr>
        <%}%>
    </table>
    <div style="padding-top: 5px;">
        <a href="javascript:;" class="sign_in_button_2" onclick="editPractice(0)">新增</a>
    </div>
    </div>


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
            <a href="javascript:;" class="N-btn0003" uploadtype="1" filetypeid="0" fileobjid="204"
                keyvalue="<%=clientId %>" tablename="CooperationClient" keyname="clientId" onclick='UploadFiles(this,"true");'>
                <img src="<%=SysAppConfig.HostDomain%>/Content/images/icon/ico-up.png" />上传封面图<span></span></a>
        </div>
       
    </div>
</div>
<br /><br /><br />
<input type=radio  value="0" checked  onclick="radClick(this)" name="fileType"/>缩略图
<input type=radio  value="1"  onclick="radClick(this)"  name="fileType"/>LOGO
 

  <div id="imgDiv"  >
   <%if (!string.IsNullOrEmpty(compObj.Text("ImagePath"))){ %><img src="<%= compObj.Text("ImagePath")%>"/>  <%} %><br/>
  
  </div>
  <div id="logoDiv" style="display:none"  >
   <%if (!string.IsNullOrEmpty(compObj.Text("logo"))){ %><img src="<%= compObj.Text("logo")%>"/>  <%} %>
   </div>

  <input type="file" id="file" name="file"/>  <input type="hidden" id="flag" name="flag" value="ajax文件上传"/> <input type="button" id="btnUpload" value="上传图片" />

<%} %>


 
<script type="text/javascript">
    $(document).ready(function () {
        cwf.view.dlgHelp.init();
        $("#btnUpload").click(function () {
            ajaxFileUpload();
        });
    });

    function radClick(obj) {
        $("#imgDiv").toggle()
        $("#logoDiv").toggle()
    }


    function editPractice(practiceId) {
        var url = "";
        url = "/CompanySite/CooperationClientPracticeEdit?practiceId=" + practiceId + "&clientId=<%= clientId%>";
        if (practiceId != 0)
            url == "/CompanySite/CooperationClientPracticeEdit?practiceId=" + practiceId + "&clientId=<%= clientId%>";


        box(url, { boxid: 'practiceEdit', title: '实践编辑', width: 300, contentType: 'ajax',
            submit_cb: function (o) {
                saveInfo(o.fbox.find("form"));
            }
        });
    }


    function saveInfo(obj) {
        var formdata = $(obj).serialize();
        $.ajax({
            url: "/Home/SavePostInfo",
            type: 'post',
            data: formdata,
            dataType: 'json',
            error: function () {
                $.tmsg("m_jfw", "未知错误，请联系服务器管理员，或者刷新页面重试", { infotype: 2 });
            },
            success: function (data) {
                if (data.Success == false) {
                                   $.tmsg("m_jfw", data.Message, { infotype: 2 });
                }
                else {
                                    $.tmsg("m_jfw", "保存成功！", { infotype: 1 });
                    ReloadDiv();
                }
            }
        });
    }



    function ajaxFileUpload() {
        var fileType = $('input:radio[name="fileType"]:checked').val();
        var fieldName = "ImagePath";
        if (fileType == 1) {
            fieldName = "logo";
        }
       
        var data = { tableName: 'CooperationClient', keyValue: '<%=compObj.Int("clientId") %>', keyField: 'clientId', fieldName: fieldName }
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
                $.tmsg("m_jfw", "保存成功！", { infotype: 1 });
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
