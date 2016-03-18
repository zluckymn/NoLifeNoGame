<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<% 
    
    //var allFileList = dataOp.FindAll("FileLibrary").ToList();
    //var list = (from i in allFileList
    //            group i by i.Text("createUserId")
    //                into g
    //                select new
    //                {
    //                    count = g.Count(),
    //                    userId = g.Key
    //                }).ToList();
    //var newList = list.OrderByDescending(t => t.count).Take(10);
    //var userIds = newList.Select(t => t.userId).ToList();
    //var userList = dataOp.FindAllByKeyValList("SysUser", "userId", userIds);


    //string updateQuery = "db.FileCount.distinct('_id',{'state':'0'})";
    //dataOp.Delete("FileCount", updateQuery);
    //List<string> hasList = new List<string>();
    //foreach (var item in userList)
    //{
    //    int fileCounts = 0;
    //    var fileCount = list.Where(t => t.userId == item.Text("userId")).FirstOrDefault();
    //    fileCounts = fileCount == null ? 0 : fileCount.count;
    //    if (!hasList.Contains(item.Text("userId")))
    //    {
    //        BsonDocument doc = new BsonDocument();
    //        doc.Add("userId", item.Text("userId"));
    //        doc.Add("count", fileCounts);
    //        doc.Add("state", "0");
    //        dataOp.Insert("FileCount", doc);
    //    }
    //}
    var fileList = dataOp.FindAll("FileLibrary").ToList();
    var resultList = dataOp.FindAll("StandardResult_StandardResult").ToList();
    var userList = dataOp.FindAll("SysUser").ToList();
    var fileObjectList = dataOp.FindAll("FileObject").Where(t => t.Text("nodeKey").StartsWith("000003")).ToList();

    var resultDocList = fileList.Where(t => fileObjectList.Select(m => m.Int("fileObjId")).Contains(t.Int("fileObjId"))).Distinct().ToList();

    var loginCount = dataOp.FindAll("SysBehaviorLog").Where(t => t.Int("logType") == 1).ToList();
%>
<div>
总文件数：<%=fileList.Count%> <br/>
成果数： <%=resultList.Count%> <br/>
成果文档数: <%=resultDocList.Count%><br/>
用户数量：<%=userList.Count%><br/>
用户登陆数量：<%=loginCount.Count%><br/>
</div><br/>