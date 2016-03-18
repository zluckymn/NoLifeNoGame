<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace=" MongoDB.Bson.IO" %>
<%@ Import Namespace="MongoDB.Driver.Builders" %>
<% 
    DataOperation dataOp = new DataOperation();

    int userId = PageReq.GetParamInt("userId");

    List<BsonDocument> noSizeFileList = dataOp.FindAllByQuery("FileLibrary", Query.And(
        Query.EQ("createUserId", userId.ToString()),
        Query.Or(
            Query.Exists("size", false),
            Query.EQ("size", ""),
            Query.Exists("guid", false),
            Query.EQ("guid", ""))
        )).ToList();    //所有没有大小的文件

    List<string> fileIdList = noSizeFileList.Select(t => t.String("fileId")).Distinct().ToList();

    List<BsonDocument> fileRelList = dataOp.FindAllByKeyValList("FileRelation", "fileId", fileIdList).ToList();
    
%>
<div style=" padding:20px">
<table class="tableborder" width="100%">
    <tr style=" background-color:rgb(245, 245, 245)">
        <td>
            文件名
        </td>
        <td width="300">
            上传路径
        </td>
        <td width="80">
            上传时间
        </td>
        <td width="60">
            是否有效
        </td>
        <td width="80">
            归属
        </td>
    </tr>
    <%  foreach (var tempFile in noSizeFileList)
        {
            BsonDocument tempFileRel = fileRelList.Where(t => t.Int("fileId") == tempFile.Int("fileId")).FirstOrDefault();
            
    %>
    <tr>
        <td class="fontblue">
            <%=tempFile.String("name") %>
        </td>
        <td>
            <%=tempFile.String("localPath")%>
        </td>
        <td>
            <%=tempFile.String("createDate") %>
        </td>
        <td>
            <%=tempFileRel== null? "无效":"有效" %>
        </td>
        <td>
            <%  if (tempFileRel != null)
                { %>
            <%=tempFileRel.String("tableName") %>(<%=tempFileRel.String("keyName")%>
            =
            <%=tempFileRel.String("keyValue")%>)
            <%  } %>
        </td>
    </tr>
    <%  } %>
</table>
</div>