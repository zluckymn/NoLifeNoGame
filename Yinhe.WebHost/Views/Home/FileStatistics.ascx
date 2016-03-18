<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace=" MongoDB.Bson.IO" %>
<%@ Import Namespace="MongoDB.Driver.Builders" %>
<% 
    DataOperation dataOp = new DataOperation();

    List<BsonDocument> noSizeFileList = dataOp.FindAllByQuery("FileLibrary", Query.Or(
        Query.Exists("size", false),
        Query.EQ("size", ""),
        Query.Exists("guid", false),
        Query.EQ("guid", ""))).ToList();    //所有没有大小的文件


    List<string> userIdList = noSizeFileList.Select(t => t.String("createUserId")).Distinct().ToList();

    List<BsonDocument> allUserList = dataOp.FindAllByKeyValList("SysUser", "userId", userIdList).ToList();

    List<string> fileIdList = noSizeFileList.Select(t => t.String("fileId")).Distinct().ToList();

    List<BsonDocument> fileRelList = dataOp.FindAllByKeyValList("FileRelation", "fileId", fileIdList).ToList();
    
%>
<table>
    <tr>
        <td>
            总计未上传成功文档数:
        </td>
        <td>
            <h3><font color="red"><%=noSizeFileList.Count %> </font></h3>
        </td>
    </tr>
    <tr>
        <td>
            其中有效文档数:
        </td>
        <td>
            <h3><font color="green"><%=fileRelList.Count%></font></h3>
        </td>
    </tr>
</table>
<table class="tableborder" width="100%">
    <tr id="tableborder-title">
        <td width="200">
            上传人
        </td>
        <td valign=top>
            文档详情
        </td>
    </tr>
    <tr>
        <td style=" padding:0px; border-bottom:0px none; border-right:0px none" valign=top>
        <div style=" height:600px; overflow:auto; overflow-x:hidden">
            <table width="100%">
                <%  foreach (var tempUser in allUserList)
                    {
                        List<BsonDocument> tempFileList = noSizeFileList.Where(t => t.Int("createUserId") == tempUser.Int("userId")).ToList();
                        
                %>
                <tr>
                    <td>
                        <a href="javascript:;" onclick="$('#fileListDiv').load('/Home/FileNoSizeList?userId=<%=tempUser.Int("userId") %>&r' + Math.random());">
                            <%=tempUser.Int("userId") %>:
                            <%=tempUser.String("name") %>
                            (<%=tempFileList.Count %>)</a>
                    </td>
                </tr>
                <%  } %>
            </table>
         </div>
        </td>
        <td valign=top>
            <div id="fileListDiv" style=" height:600px; overflow:auto; overflow-x:hidden">
            </div>
        </td>
    </tr>
</table>
