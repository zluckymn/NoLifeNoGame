<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
<% var  tableName = PageReq.GetParam("tableName");
   var  primaryKey = PageReq.GetParam("primaryKey");
   var  primaryKeyValue = PageReq.GetParam("primaryKeyValue");
   var fileObjId = PageReq.GetParam("fileObjId");
%>
<%if (!string.IsNullOrEmpty(primaryKey) && primaryKey != "0"  )
  {
      var userPhotoReObj = dataOp.FindOneByQueryStr("FileRelation", string.Format("tableName={0}&fileObjId={1}&keyValue={2}", tableName, fileObjId, primaryKeyValue));
      string path = "";
      var fileModel = new BsonDocument();
      if (userPhotoReObj != null)
      {
          fileModel = dataOp.FindOneByKeyVal("FileLibrary", "fileId", userPhotoReObj.Text("fileId"));
          path = fileModel.Text("thumbPicPath").Replace("_m", "_us");
      }
%>
<%if (path != "")
  {%>
<div class="sign_photo">
    <img src="<%=path %>" alt="文件" />  <a href="javascript:void(0);" class="look" onclick='<%=FileCommonOperation.GetClientOnlineRead(fileModel) %>'>
                                阅读</a>
                            <a href="javascript:void(0);" class="download" onclick='<%=FileCommonOperation.GetClientDownLoad(fileModel) %>'>
                                    下载</a></div>
      

<div style="margin-left: 5px; float: left">
    <a href="javascript:void(0);" hidefocus="true" onclick='resetDiv();' class="look">刷新</a>
    <a href="javascript:void(0);" hidefocus="true" class="delete" onclick='DeleteFilesRelation("<%=userPhotoReObj.String("fileRelId") %>",resetDivTime)'>
        删除</a></div>
<%}
  else
  {%>
暂无文件 <a class="N-btn0003" href="javascript:;" onclick='UploadFiles(this,"false",resetDivTime)'
    filetypeid="0" fileobjid="<%=fileObjId %>" keyvalue="<%=primaryKeyValue%>" tablename="<%=tableName %>" keyname="<%=primaryKey %>"
    id="file_upload">
    <img src="<%=SysAppConfig.HostDomain%>/Content/images/client/XC/icon/ico-up.png"/>上传</a>
<%} %>
<%} %>
<script type="text/javascript">
  
</script>
