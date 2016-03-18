<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="MongoDB.Driver.Builders" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.MvcFilters" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
 
<%
     
    var projId = PageReq.GetParam("cityId");
    var totalPage = 0;
    var keyWord = Uri.UnescapeDataString(PageReq.GetParam("strKeyWord"));
    var query1 = Query.EQ("tableName", "XH_DesignManage_Task");
    var query2 = Query.EQ("keyName", "taskId");
    CommonSearch search = null;
    if (!string.IsNullOrEmpty(projId))
    {
         var taskIds = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "projId", projId).Select(c => c.Text("taskId"));
         var query3 = Query.In("keyValue", TypeConvert.StringListToBsonValueList(taskIds.ToList()));
         search = new Yinhe.ProcessingCenter.MvcFilters.CommonSearch("FileRelation", Query.And(query1, query2, query3));
         
    }
    else
    {
        search = new Yinhe.ProcessingCenter.MvcFilters.CommonSearch("FileRelation", Query.And(query1, query2));
   }
    var current = PageReq.GetParamInt("current");
    if (current <= 0)
    {
        current = 1;
    }
    var bizFileList = search.QuickSearchForFile(current, out totalPage, keyWord);
    var startIndex = (current - 1) * SysAppConfig.PageSize + 1;
    var endIndex = current + bizFileList.Count() - 1;
%>
<div class="structlisttop" style="margin-top: 30px;">
    <div class="left">
        搜索结果列表</div>
    <div class="right">
        约有 <strong style="color: red;">
            <%=search.totalRecord%></strong> 项符合 "<span style="color: red;"><%=keyWord %></span>"
        的查询结果<%if (bizFileList.Count() != 0)
               {%>，以下是第<strong style="color: red;"><%=startIndex%>-<%=endIndex%></strong> 项<%}%></div>
</div>
<div class="structlistmain">
    <table width="100%">
        <tr>
            <td valign="top">
            <%foreach (var fileLibrary in bizFileList)
              {
                  var fileId = fileLibrary.Int("fileId");
                  var fileRelId = fileLibrary.Int("fileRelId");
                  var file = dataOp.FindOneByKeyVal("FileLibrary", "fileId", fileId.ToString());
                  var fielName = fileLibrary.Text("name");
                  var fileRelation = fileLibrary.ChildBsonList("FileRelation").FirstOrDefault();
                  if (fileRelation == null) continue;
                  var taskId = fileRelation.Text("keyValue");
                  var task=dataOp.FindOneByKeyVal("XH_DesignManage_Task","taskId",taskId);
                  if(task==null)continue;
                  var taskUrl = string.Format("/DesignManage/ProjTaskInfo/{0}", taskId);
                  var projectUrl = string.Format("/DesignManage/ProjectIndex?projId={0}", taskId);
                  var project=task.SourceBson("projId");
                  
                   
            %>
                <div class="SearchResults ">
                    <div class="title">
                        <a href='javascript:void(0)' onclick='<%=FileCommonOperation.GetClientOnlineRead(fileLibrary) %>' ><%if(!String.IsNullOrEmpty(keyWord)){%><%=fielName.Replace(keyWord, "<span>" + keyWord + "</span>")%><%}else{%><%=fielName%><%}%></a></div>
                    <table width="100%">
                        <tr>
                            <td width="130" valign="top">
                                <div class="picdivm">
                                <a href="javascript:;" class="a_img_box" onclick="window.location.href='<%=taskUrl %>'" >
                                    <img src="<%=fileLibrary.String("thumbPicPath").Replace("_m.","_hm.") %>" /></a>
                                </div>
                            </td>
                            <td valign="top">
                                所属项目：<a href="<%=projectUrl%>" target=_blank><%=project!=null?project.Text("name"):string.Empty %></a><br />
                                 
                                <div class="Attached">
                                    创建人：<%=fileLibrary.CreateUserName()%> 创建时间：<%=fileLibrary.CreateDate()%></div>
                               
                                <a href='javascript:void(0)' onclick='<%=FileCommonOperation.GetClientOnlineRead(fileLibrary) %>'
                                    class="green">阅读</a> <a href='javascript:void(0)' onclick='<%=FileCommonOperation.GetClientDownLoad(fileLibrary) %>'
                                        class="blue">下载</a>
                                
                            </td>
                        </tr>
                    </table>
                </div>
            <%}%>
            </td>
        </tr>
    </table>
</div>
<div id="pag"></div>
<script type="text/javascript">
    $(function() {
        var current = <%=current %>;
        $("#pag").pagination({ totalPage: <%=totalPage %>, display_pc: 7, current_page:current,showPageList:false,showRefresh:false,displayMsg:'',
            onSelectPage: function(current_page, pagesize) {
                var keyWord = "<%=keyWord %>";
                var sysStageId = "";
                var sysProfId = "";
                var fileCatId = "";
                var projId = "<%=projId %>";
                var url = "/SearchEngine/SearchCrossProjDocumentControl?keyWord=" + escape(keyWord) + "&sysStageId=" + sysStageId + "&sysProfId=" + sysProfId + "&fileCatId=" + fileCatId + "&projId=" + projId + "&r=" + Math.random() + "&current=" + current_page;
                $("#divResultList").load(url);
            }
        });
    });
</script>

