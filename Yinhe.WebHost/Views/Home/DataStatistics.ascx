<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace=" MongoDB.Bson.IO" %>
<%@ Import Namespace="MongoDB.Driver.Builders" %>
<% DataOperation dataOp = new DataOperation();%>
<!----项目任务数,文档数统计---->
<%--<div>
    <table class="table01" width="800">
        <tr>
            <td>
                项目
            </td>
            <td>
                任务数
            </td>
            <td>
                文档总关联
            </td>
            <td>
                非文件夹
            </td>
            <td>
                文件夹
            </td>
            <td>
                文档总数
            </td>
        </tr>
        <%
            List<BsonDocument> allProjList = dataOp.FindAll("XH_DesignManage_Project").ToList();

            int taskCount = 0;
            int relCount = 0;
            int noFolerCount = 0;
            int folerCount = 0;

            foreach (var tempProj in allProjList)
            {
                List<BsonDocument> taskList = dataOp.FindAllByKeyVal("XH_DesignManage_Task", "projId", tempProj.String("projId")).ToList();

                taskCount += taskList.Count;

                List<string> taskIdList = taskList.Select(t => t.String("taskId")).ToList();

                List<BsonDocument> fileRelList = dataOp.FindAllByQuery("FileRelation", Query.And(Query.EQ("tableName", "XH_DesignManage_Task"), Query.EQ("keyName", "taskId"), Query.In("keyValue", TypeConvert.StringListToBsonValueList(taskIdList)))).ToList();

                relCount += fileRelList.Count();

                List<string> structIdList = fileRelList.Select(t => t.String("structId")).ToList();                  //交付物的所有目录结构
                List<string> deliverIdList = fileRelList.Select(t => t.String("fileId")).ToList();                   //交付物所有非目录文档

                List<string> allStructIdList = new List<string>();    //所有目录结构

                allStructIdList.AddRange(structIdList);
                foreach (var tempStructId in structIdList)
                {
                    if (tempStructId.Trim() != "")
                    {
                        allStructIdList.AddRange(dataOp.FindChildNodes("FileStructure", tempStructId).Select(t => t.String("structId")));
                    }
                }

                List<BsonDocument> allDirFileList = dataOp.FindAllByKeyValList("FileLibrary", "structId", allStructIdList).ToList();

                noFolerCount += deliverIdList.Distinct().Count();
                folerCount += allDirFileList.Count();  
        %>
        <tr>
            <td>
                <%=tempProj.String("name") %>
            </td>
            <td>
                <%=taskList.Count %>
            </td>
            <td>
                <%=fileRelList.Count %>
            </td>
            <td>
                <%=deliverIdList.Count %>
            </td>
            <td>
                <%=allDirFileList.Count %>
            </td>
            <td>
                <%=deliverIdList.Count + allDirFileList.Count%>
            </td>
        </tr>
        <% } %>
        <tr>
            <td>
                <%=allProjList.Count %>
            </td>
            <td>
                <%=taskCount%>
            </td>
            <td>
                <%=relCount %>
            </td>
            <td>
                <%=noFolerCount %>
            </td>
            <td>
                <%=folerCount%>
            </td>
            <td>
                <%=noFolerCount+folerCount%>
            </td>
        </tr>
    </table>
    <h3>
        没大小文档数:
        <%=dataOp.FindAll("FileLibrary").Where(t=>t.String("size") == "").Count()%>
    </h3>
</div>--%>
<!----关卡节点时间统计---->
<%--<div>
    <% 
        List<BsonDocument> projList = dataOp.FindAll("XH_DesignManage_Project").ToList();

        List<string> projIdList = projList.Select(t => t.String("projId")).ToList();

        List<BsonDocument> planList = dataOp.FindAllByKeyValList("XH_DesignManage_Plan", "projId", projIdList).ToList();

        List<string> planIdList = planList.Select(t => t.String("planId")).ToList();

        List<BsonDocument> taskList = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.And(
            Query.In("diagramId", new List<BsonValue>() { "7", "37", "63", "79" }),
            Query.In("planId", TypeConvert.StringListToBsonValueList(planIdList)))).ToList();
    
    
    %>
    <table border="1">
        <tr>
            <td>
                名称
            </td>
            <td>
                项目名称
            </td>
            <td>
                计划开
            </td>
            <td>
                计划完
            </td>
            <td>
                实际开
            </td>
            <td>
                实际完
            </td>
        </tr>
        <%  foreach (var tempTask in taskList.OrderBy(t => t.String("projId")))
            { %>
        <tr>
            <td>
                <%=tempTask.String("name") %>
            </td>
            <td>
                <%=tempTask.SourceBsonField("projId","name") %>
            </td>
            <td>
                <%=tempTask.String("curStartData")%>
            </td>
            <td>
                <%=tempTask.String("curEndData")%>
            </td>
            <td>
                <%=tempTask.String("factStartDate")%>
            </td>
            <td>
                <%=tempTask.String("factEndDate")%>
            </td>
        </tr>
        <%  } %>
    </table>
</div>--%>
<!----用户登录统计---->
<%--<div>
    <% 
        List<BsonDocument> loginList = dataOp.FindAllByKeyVal("SysBehaviorLog", "logType", "1").ToList();

        List<string> userIdList = loginList.Select(t => t.String("logUserId")).ToList();

        List<BsonDocument> userList = dataOp.FindAllByKeyValList("SysUser", "userId", userIdList).ToList();

        List<BsonDocument> userPostList = dataOp.FindAllByKeyValList("UserOrgPost", "userId", userIdList).ToList();

        List<string> postIdList = userPostList.Select(t => t.String("postId")).ToList();

        List<BsonDocument> postList = dataOp.FindAllByKeyValList("OrgPost", "postId", postIdList).ToList();

        List<string> orgIdList = postList.Select(t => t.String("orgId")).ToList();

        List<BsonDocument> orgList = dataOp.FindAllByKeyValList("Organization", "orgId", orgIdList).ToList();
    %>
    <table border="1">
        <tr>
            <td>
                序号
            </td>
            <td>
                用户
            </td>
            <td>
                部门
            </td>
            <td>
                时间
            </td>
        </tr>
        <%  int index = 0;
            foreach (var tempLog in loginList.OrderBy(t => t.String("timeSort")))
            {
                index++;

                BsonDocument tempUser = userList.Where(t => t.Int("userId") == tempLog.Int("logUserId")).FirstOrDefault();
                BsonDocument tempPostRel = userPostList.Where(t => t.Int("userId") == tempUser.Int("userId")).FirstOrDefault();
                BsonDocument tempPost = postList.Where(t => t.Int("postId") == tempPostRel.Int("postId")).FirstOrDefault();
                BsonDocument tempOrg = orgList.Where(t => t.Int("orgId") == tempPost.Int("orgId")).FirstOrDefault();
        %>
        <tr>
            <td>
                <%=index%>
            </td>
            <td>
                <%=tempUser.String("name")%>
            </td>
            <td>
                <%=tempOrg.String("name")%>
            </td>
            <td>
                <%=tempLog.String("logTime")%>
            </td>
        </tr>
        <%  } %>
    </table>
</div>--%>
<!----任务完成统计---->
<%--<div>
    <% 
        List<BsonDocument> logList = dataOp.FindAllByQuery("SysBehaviorLog", Query.And(Query.Matches("path", "SavePostInfo"),
            Query.EQ("tbName", "XH_DesignManage_Task"),
            Query.EQ("status", "4"))).ToList();
    
    %>
    <table border="1">
        <tr>
            <td>
                序号
            </td>
            <td>
                任务
            </td>
            <td>
                所属项目
            </td>
            <td>
                完成时间
            </td>
        </tr>
        <%  int index = 0;
            foreach (var tempLog in logList.OrderBy(t => t.String("timeSort")))
            {
                index++;

                BsonDocument tempTask = dataOp.FindOneByQuery("XH_DesignManage_Task", TypeConvert.NativeQueryToQuery(tempLog.String("queryStr")));
                BsonDocument tempProj = dataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", tempTask.String("projId"));
        %>
        <tr>
            <td>
                <%=index%>
            </td>
            <td>
                <%=tempTask.String("name")%>
            </td>
            <td>
                <%=tempProj.String("name")%>
            </td>
            <td>
                <%=tempLog.String("logTime")%>
            </td>
        </tr>
        <%  } %>
    </table>
</div>--%>
<!----成果统计---->
<div>
    <% 
        List<BsonDocument> retList = dataOp.FindAll("XH_StandardResult_StandardResult").ToList();   //标准成果
        List<BsonDocument> matList = dataOp.FindAll("XH_Material_Material").ToList();           //材料
        List<BsonDocument> seedList = dataOp.FindAll("XH_Material_SeedlingsResult").ToList();   //苗木
        List<BsonDocument> itemList = dataOp.FindAll("XH_StandardResult_DiscreteDesignItem").ToList();  //离散设计要素
        List<BsonDocument> caseList = dataOp.FindAll("XH_StandardResult_CaseModels").ToList();  //标杆案例

        List<string> userIdList = new List<string>();
        userIdList.AddRange(retList.Select(t => t.String("createUserId")));
        userIdList.AddRange(matList.Select(t => t.String("createUserId")));
        userIdList.AddRange(seedList.Select(t => t.String("createUserId")));
        userIdList.AddRange(itemList.Select(t => t.String("createUserId")));
        userIdList.AddRange(caseList.Select(t => t.String("createUserId")));
        userIdList = userIdList.Distinct().ToList();

        List<BsonDocument> userList = dataOp.FindAllByKeyValList("SysUser", "userId", userIdList).ToList();
        List<BsonDocument> userPostList = dataOp.FindAllByKeyValList("UserOrgPost", "userId", userIdList).ToList();
        List<string> postIdList = userPostList.Select(t => t.String("postId")).ToList();
        List<BsonDocument> postList = dataOp.FindAllByKeyValList("OrgPost", "postId", postIdList).ToList();
        List<string> orgIdList = postList.Select(t => t.String("orgId")).ToList();
        List<BsonDocument> orgList = dataOp.FindAllByKeyValList("Organization", "orgId", orgIdList).ToList();

        List<BsonDocument> fileRelList = dataOp.FindAllByQuery("FileRelation", Query.Or(
            Query.EQ("tableName", "XH_StandardResult_StandardResult"),
            Query.EQ("tableName", "XH_Material_Material"),
            Query.EQ("tableName", "XH_Material_SeedlingsResult"),
            Query.EQ("tableName", "XH_StandardResult_DiscreteDesignItem"),
            Query.EQ("tableName", "XH_StandardResult_CaseModels")
            )).ToList();

        List<BsonDocument> logList = dataOp.FindAllByQuery("SysBehaviorLog", Query.Or(
            Query.Matches("path", "UnitView"),
            Query.Matches("path", "FineDecorationView"),
            Query.Matches("path", "FacadeView"),
            Query.Matches("path", "DemonstrationAreaView"),
            Query.Matches("path", "LandscapeView"),
            Query.Matches("path", "PEMView"),
            Query.Matches("path", "PublicPartsView"),
            Query.Matches("path", "MaterialShow"),
            Query.Matches("path", "MaterialSeedlingShow"),
            Query.Matches("path", "DDLibraryShow"),
            Query.Matches("path", "CaseModelsShow")
            )).ToList();


        List<BsonDocument> typeList = dataOp.FindAll("XH_StandardResult_ResultType").ToList();      //标准成果类型
    %>
    <table border="1">
        <tr>
            <td>
                序号
            </td>
            <td>
                类型
            </td>
            <td>
                成果名称
            </td>
            <td>
                创建者
            </td>
            <td>
                部门
            </td>
            <td>
                创建时间
            </td>
            <td align="center">
                文档数
            </td>
            <td>
                浏览数
            </td>
        </tr>
        <%  int index = 0;
            foreach (var tempRet in retList.OrderBy(t => t.String("typeId")))
            {
                index++;

                BsonDocument tempType = typeList.Where(t => t.Int("typeId") == tempRet.Int("typeId")).FirstOrDefault();

                BsonDocument tempUser = userList.Where(t => t.Int("userId") == tempRet.Int("createUserId")).FirstOrDefault();
                BsonDocument tempPostRel = userPostList.Where(t => t.Int("userId") == tempUser.Int("userId")).FirstOrDefault();
                BsonDocument tempPost = postList.Where(t => t.Int("postId") == tempPostRel.Int("postId")).FirstOrDefault();
                BsonDocument tempOrg = orgList.Where(t => t.Int("orgId") == tempPost.Int("orgId")).FirstOrDefault();

                List<BsonDocument> tempFileRelList = fileRelList.Where(t => t.String("tableName") == "XH_StandardResult_StandardResult" && t.Int("keyValue") == tempRet.Int("retId")).ToList();

                List<BsonDocument> tempLogList = logList.Where(t => t.Int("retId") == tempRet.Int("retId")).ToList();
        %>
        <tr>
            <td>
                <%=index%>
            </td>
            <td>
                <%=tempType.String("name")%>
            </td>
            <td>
                <%=tempRet.String("name")%>
            </td>
            <td>
                <%=tempUser.String("name")%>
            </td>
            <td>
                <%=tempOrg.String("name")%>
            </td>
            <td>
                <%=tempRet.String("createDate") %>
            </td>
            <td>
                <%=tempFileRelList.Count %>
            </td>
            <td>
                <%=tempLogList.Count %>
            </td>
        </tr>
        <%  } %>
        <%  foreach (var tempRet in matList.OrderByDescending(t => t.Int("baseCatId")))
            {
                index++;

                BsonDocument tempUser = userList.Where(t => t.Int("userId") == tempRet.Int("createUserId")).FirstOrDefault();
                BsonDocument tempPostRel = userPostList.Where(t => t.Int("userId") == tempUser.Int("userId")).FirstOrDefault();
                BsonDocument tempPost = postList.Where(t => t.Int("postId") == tempPostRel.Int("postId")).FirstOrDefault();
                BsonDocument tempOrg = orgList.Where(t => t.Int("orgId") == tempPost.Int("orgId")).FirstOrDefault();

                List<BsonDocument> tempFileRelList = fileRelList.Where(t => t.String("tableName") == "XH_Material_Material" && t.Int("keyValue") == tempRet.Int("matId")).ToList();

                List<BsonDocument> tempLogList = logList.Where(t => t.Int("matId") == tempRet.Int("matId")).ToList();
        %>
        <tr>
            <td>
                <%=index%>
            </td>
            <td>
                材料库
            </td>
            <td>
                <%=tempRet.String("name")%>
            </td>
            <td>
                <%=tempUser.String("name")%>
            </td>
            <td>
                <%=tempOrg.String("name")%>
            </td>
            <td>
                <%=tempRet.String("createDate") %>
            </td>
            <td>
                <%=tempFileRelList.Count %>
            </td>
            <td>
                <%=tempLogList.Count %>
            </td>
        </tr>
        <%  } %>
        <%  foreach (var tempRet in seedList)
            {
                index++;
                BsonDocument tempUser = userList.Where(t => t.Int("userId") == tempRet.Int("createUserId")).FirstOrDefault();
                BsonDocument tempPostRel = userPostList.Where(t => t.Int("userId") == tempUser.Int("userId")).FirstOrDefault();
                BsonDocument tempPost = postList.Where(t => t.Int("postId") == tempPostRel.Int("postId")).FirstOrDefault();
                BsonDocument tempOrg = orgList.Where(t => t.Int("orgId") == tempPost.Int("orgId")).FirstOrDefault();

                List<BsonDocument> tempFileRelList = fileRelList.Where(t => t.String("tableName") == "XH_Material_SeedlingsResult" && t.Int("keyValue") == tempRet.Int("resultId")).ToList();

                List<BsonDocument> tempLogList = logList.Where(t => t.Int("resultId") == tempRet.Int("resultId")).ToList();
        %>
        <tr>
            <td>
                <%=index%>
            </td>
            <td>
                苗木库
            </td>
            <td>
                <%=tempRet.String("name")%>
            </td>
            <td>
                <%=tempUser.String("name")%>
            </td>
            <td>
                <%=tempOrg.String("name")%>
            </td>
            <td>
                <%=tempRet.String("createDate") %>
            </td>
            <td>
                <%=tempFileRelList.Count %>
            </td>
            <td>
                <%=tempLogList.Count %>
            </td>
        </tr>
        <%  } %>
        <%  foreach (var tempRet in itemList)
            {
                index++;
                BsonDocument tempUser = userList.Where(t => t.Int("userId") == tempRet.Int("createUserId")).FirstOrDefault();
                BsonDocument tempPostRel = userPostList.Where(t => t.Int("userId") == tempUser.Int("userId")).FirstOrDefault();
                BsonDocument tempPost = postList.Where(t => t.Int("postId") == tempPostRel.Int("postId")).FirstOrDefault();
                BsonDocument tempOrg = orgList.Where(t => t.Int("orgId") == tempPost.Int("orgId")).FirstOrDefault();

                List<BsonDocument> tempFileRelList = fileRelList.Where(t => t.String("tableName") == "XH_StandardResult_DiscreteDesignItem" && t.Int("keyValue") == tempRet.Int("itemId")).ToList();

                List<BsonDocument> tempLogList = logList.Where(t => t.String("path").Contains("DDLibraryShow") && t.Int("id") == tempRet.Int("itemId")).ToList();
        %>
        <tr>
            <td>
                <%=index%>
            </td>
            <td>
                离散设计要素库
            </td>
            <td>
                <%=tempRet.String("name")%>
            </td>
            <td>
                <%=tempUser.String("name")%>
            </td>
            <td>
                <%=tempOrg.String("name")%>
            </td>
            <td>
                <%=tempRet.String("createDate") %>
            </td>
            <td>
                <%=tempFileRelList.Count %>
            </td>
            <td>
                <%=tempLogList.Count %>
            </td>
        </tr>
        <%  } %>
        <%  foreach (var tempRet in caseList)
            {
                index++;
                BsonDocument tempUser = userList.Where(t => t.Int("userId") == tempRet.Int("createUserId")).FirstOrDefault();
                BsonDocument tempPostRel = userPostList.Where(t => t.Int("userId") == tempUser.Int("userId")).FirstOrDefault();
                BsonDocument tempPost = postList.Where(t => t.Int("postId") == tempPostRel.Int("postId")).FirstOrDefault();
                BsonDocument tempOrg = orgList.Where(t => t.Int("orgId") == tempPost.Int("orgId")).FirstOrDefault();

                List<BsonDocument> tempFileRelList = fileRelList.Where(t => t.String("tableName") == "XH_StandardResult_CaseModels" && t.Int("keyValue") == tempRet.Int("cmlId")).ToList();

                List<BsonDocument> tempLogList = logList.Where(t => t.String("path").Contains("CaseModelsShow") && t.Int("id") == tempRet.Int("cmlId")).ToList();
        %>
        <tr>
            <td>
                <%=index%>
            </td>
            <td>
                标杆案例库
            </td>
            <td>
                <%=tempRet.String("name")%>
            </td>
            <td>
                <%=tempUser.String("name")%>
            </td>
            <td>
                <%=tempOrg.String("name")%>
            </td>
            <td>
                <%=tempRet.String("createDate") %>
            </td>
            <td>
                <%=tempFileRelList.Count %>
            </td>
            <td>
                <%=tempLogList.Count %>
            </td>
        </tr>
        <%  } %>
    </table>
</div>
<!----任务上传文档统计---->
<%--<div>
    <% 
        List<BsonDocument> logList = dataOp.FindAllByQuery("SysBehaviorLog", Query.And(Query.Matches("path", "SaveMultipleUploadFiles"),
                Query.EQ("tableName", "XH_DesignManage_Task"))).ToList();

        List<string> taskIdList = logList.Select(t => t.String("keyValue")).ToList();
        List<BsonDocument> taskList = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "taskId", taskIdList).ToList();

        List<string> projIdList = taskList.Select(t => t.String("projId")).ToList();
        List<BsonDocument> projList = dataOp.FindAllByKeyValList("XH_DesignManage_Project", "projId", projIdList).ToList();

        foreach (var tempLog in logList)
        {
            BsonDocument tempTask = taskList.Where(t => t.Int("taskId") == tempLog.Int("keyValue")).FirstOrDefault();
            if (tempTask == null) continue;

            tempLog.Add("projId", tempTask.String("projId"));
        }
    
    %>
    <table border="1">
        <tr>
            <td>
                序号
            </td>
            <td>
                任务
            </td>
            <td>
                所属项目
            </td>
            <td>
                文档数
            </td>
            <td>
                上传时间
            </td>
        </tr>
        <%  int index = 0;
            foreach (var tempLog in logList.OrderByDescending(t => t.String("timeSort")).ThenBy(t => t.String("projId")))
            {
                index++;

                BsonDocument tempTask = taskList.Where(t => t.Int("taskId") == tempLog.Int("keyValue")).FirstOrDefault();
                BsonDocument tempProj = projList.Where(t => t.Int("projId") == tempTask.Int("projId")).FirstOrDefault();

                int fileCount = tempLog.String("uploadFileList").Split(new string[] { "|Y|" }, StringSplitOptions.RemoveEmptyEntries).Count();

                if (tempTask == null || tempProj == null) continue;
        %>
        <tr>
            <td>
                <%=index%>
            </td>
            <td>
                <%=tempTask.String("name")%>
            </td>
            <td>
                <%=tempProj.String("name")%>
            </td>
            <td>
                <%=fileCount %>
            </td>
            <td>
                <%=tempLog.String("logTime")%>
            </td>
        </tr>
        <%  } %>
    </table>
</div>--%>
<%--<div>
    <% 
        List<BsonDocument> fileRelList = dataOp.FindAllByKeyVal("FileRelation", "tableName", "XH_DesignManage_Task").ToList();  //所有任务文档关联

        List<string> taskIdList = fileRelList.Select(t => t.String("keyValue")).ToList();
        List<BsonDocument> taskList = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "taskId", taskIdList).ToList();

        List<string> projIdList = taskList.Select(t => t.String("projId")).ToList();
        List<BsonDocument> projList = dataOp.FindAllByKeyValList("XH_DesignManage_Project", "projId", projIdList).ToList();

        
        
    %>
    <table border="1">
        <tr>
            <td>
                序号
            </td>
            <td>
                任务
            </td>
            <td>
                所属项目
            </td>
            <td>
                文档数
            </td>
            <td>
                上传时间
            </td>
        </tr>
        <%  int index = 0;
            foreach (var tempTask in taskList.OrderBy(t => t.String("projId")))
            {
                index++;

                BsonDocument tempProj = projList.Where(t => t.Int("projId") == tempTask.Int("projId")).FirstOrDefault();

                var tempFileRelList = fileRelList.Where(t => t.Int("keyValue") == tempTask.Int("taskId")).ToList();

                List<string> dateTimeList = tempFileRelList.Select(t => t.Date("createDate").ToString("yyyy-MM-dd")).Distinct().ToList();

                if (tempTask == null || tempProj == null) continue;


                foreach (var tempTime in dateTimeList)
                {  
        %>
        <tr>
            <td>
                <%=index%>
            </td>
            <td>
                <%=tempTask.String("name")%>
            </td>
            <td>
                <%=tempProj.String("name")%>
            </td>
            <td>
                <%=tempFileRelList.Where(t => t.String("createDate").Contains(tempTime)).Count()%>
            </td>
            <td>
                <%=tempTime%>
            </td>
        </tr>
        <%      }
            } %>
    </table>
</div>--%>
<!--项目信息汇总表-->
<%--<div>
    <% 
        List<BsonDocument> engList = dataOp.FindAll("XH_DesignManage_Engineering").ToList();
        List<BsonDocument> projList = dataOp.FindAll("XH_DesignManage_Project").ToList();

        List<BsonDocument> cityList = dataOp.FindAll("XH_ProductDev_City").ToList();
        List<BsonDocument> areaList = dataOp.FindAll("XH_ProductDev_Area").ToList();

        List<BsonDocument> serieList = dataOp.FindAll("XH_ProductDev_ProductSeries").ToList();
        List<BsonDocument> lineList = dataOp.FindAll("XH_ProductDev_ProductLine").ToList();

        List<string> userIdList = new List<string>();
        userIdList.AddRange(projList.Select(t => t.String("createUserId")));
        userIdList.AddRange(projList.Select(t => t.String("updateUserId")));

        List<BsonDocument> allUserList = dataOp.FindAllByKeyValList("SysUser", "userId", userIdList.Distinct().ToList()).ToList();    //获取所有用到的相关人员

    %>
    <table border="1">
        <tr>
            <td>
                项目名称
            </td>
            <td>
                所属地块
            </td>
            <td>
                区域
            </td>
            <td>
                城市
            </td>
            <td>
                项目负责人
            </td>
            <td>
                项目状态
            </td>
            <td>
                产品系列
            </td>
            <td>
                产品线
            </td>
            <td>
                占地面积
            </td>
            <td>
                物业类型
            </td>
            <td>
                建筑面积
            </td>
            <td>
                拿地时间
            </td>
            <td>
                开盘时间
            </td>
            <td>
                交付时间
            </td>
            <td>
                预估售价
            </td>
            <td>
                创建人
            </td>
            <td>
                更新人
            </td>
            <td>
                创建时间
            </td>
            <td>
                更新时间
            </td>
        </tr>
        <%  int index = 0;
            foreach (var tempProj in projList)
            {
                index++;

                BsonDocument tempEng = engList.Where(t => t.Int("engId") == tempProj.Int("engId")).FirstOrDefault();
                BsonDocument tempCity = cityList.Where(t => t.Int("cityId") == tempEng.Int("cityId")).FirstOrDefault();
                BsonDocument tempArea = areaList.Where(t => t.Int("areaId") == tempEng.Int("areaId")).FirstOrDefault();
                BsonDocument tempSerie = serieList.Where(t => t.Int("seriesId") == tempProj.Int("seriesId")).FirstOrDefault();
                BsonDocument tempLine = lineList.Where(t => t.Int("lineId") == tempProj.Int("lineId")).FirstOrDefault();

                BsonDocument createUser = allUserList.Where(t => t.Int("userId") == tempProj.Int("createUserId")).FirstOrDefault();
                BsonDocument updateUser = allUserList.Where(t => t.Int("userId") == tempProj.Int("updateUserId")).FirstOrDefault();
        %>
        <tr>
            <td>
                <%=tempProj.String("name") %>
            </td>
            <td>
                <%=tempEng.String("name")%>
            </td>
            <td>
                <%=tempArea.String("name")%>
            </td>
            <td>
                <%=tempCity.String("name")%>
            </td>
            <td>
                <%=tempProj.String("designManager")%>
            </td>
            <td>
                <%=tempProj.String("statusId")%>
            </td>
            <td>
                <%=tempSerie.String("name")%>
            </td>
            <td>
                <%=tempLine.String("name")%>
            </td>
            <td>
                <%=tempProj.String("CoversArea")%>
            </td>
            <td>
                <%=tempProj.String("PropertyType")%>
            </td>
            <td>
                <%=tempProj.String("BuildingArea")%>
            </td>
            <td>
                <%=tempProj.String("TakePlaceDate")%>
            </td>
            <td>
                <%=tempProj.String("OpeningDate")%>
            </td>
            <td>
                <%=tempProj.String("DeliverDate")%>
            </td>
            <td>
                <%=tempProj.String("SellingPrice")%>
            </td>
            <td>
                <%=createUser.String("name")%>
            </td>
            <td>
                <%=updateUser.String("name") %>
            </td>
            <td>
                <%=tempProj.CreateDate() %>
            </td>
            <td>
                <%=tempProj.UpdateDate() %>
            </td>
        </tr>
        <%  } %>
    </table>
</div>--%>
<!--任务信息汇总表-->
<%--<div>
    <% 
        List<BsonDocument> taskList = dataOp.FindAll("XH_DesignManage_Task").ToList();

        List<BsonDocument> engList = dataOp.FindAll("XH_DesignManage_Engineering").ToList();
        List<BsonDocument> projList = dataOp.FindAll("XH_DesignManage_Project").ToList();

        List<BsonDocument> cityList = dataOp.FindAll("XH_ProductDev_City").ToList();
        List<BsonDocument> areaList = dataOp.FindAll("XH_ProductDev_Area").ToList();

        List<BsonDocument> serieList = dataOp.FindAll("XH_ProductDev_ProductSeries").ToList();
        List<BsonDocument> lineList = dataOp.FindAll("XH_ProductDev_ProductLine").ToList();

        List<BsonDocument> levelList = dataOp.FindAll("XH_DesignManage_ConcernLevel").ToList();

        List<BsonDocument> diagramList = dataOp.FindAll("XH_DesignManage_ContextDiagram").ToList();

        List<BsonDocument> managerList = dataOp.FindAll("XH_DesignManage_TaskManager").ToList(); //找出所有任务管理人

        List<BsonDocument> flowRelList = dataOp.FindAll("XH_DesignManage_TaskBusFlow").ToList();
        List<BsonDocument> flowList = dataOp.FindAll("BusFlow").ToList();

        List<string> userIdList = new List<string>();
        userIdList.AddRange(managerList.Select(t => t.String("userId")));

        List<BsonDocument> allUserList = dataOp.FindAllByKeyValList("SysUser", "userId", userIdList).ToList();    //获取所有用到的相关人员

    %>
    <table border="1">
        <tr>
            <td>
                项目名称
            </td>
            <td>
                所属地块
            </td>
            <td>
                区域
            </td>
            <td>
                城市
            </td>
            <td>
                任务名称（ID）
            </td>
            <td>
                工期
            </td>
            <td>
                计划开始
            </td>
            <td>
                计划结束
            </td>
            <td>
                实际开始
            </td>
            <td>
                实际结束
            </td>
            <td>
                任务负责人
            </td>
            <td>
                任务类型
            </td>
            <td>
                审批流程名称
            </td>
            <td>
                对应地铁图
            </td>
        </tr>
        <%  int index = 0;
            foreach (var tempTask in taskList.OrderBy(t => t.String("nodeKey")))
            {
                index++;

                BsonDocument tempProj = projList.Where(t => t.Int("projId") == tempTask.Int("projId")).FirstOrDefault();
                
                if (tempProj == null) continue;
                
                BsonDocument tempEng = engList.Where(t => t.Int("engId") == tempProj.Int("engId")).FirstOrDefault();

                BsonDocument tempCity = cityList.Where(t => t.Int("cityId") == tempEng.Int("cityId")).FirstOrDefault();
                BsonDocument tempArea = areaList.Where(t => t.Int("areaId") == tempEng.Int("areaId")).FirstOrDefault();
                BsonDocument tempSerie = serieList.Where(t => t.Int("seriesId") == tempProj.Int("seriesId")).FirstOrDefault();
                BsonDocument tempLine = lineList.Where(t => t.Int("lineId") == tempProj.Int("lineId")).FirstOrDefault();

                BsonDocument ownerManager = managerList.Where(m => m.Int("taskId") == tempTask.Int("taskId") && m.Int("type") == (int)TaskManagerType.TaskOwner).FirstOrDefault();
                BsonDocument ownerUser = allUserList.Where(t => t.Int("userId") == (ownerManager != null ? ownerManager.Int("userId") : -1)).FirstOrDefault();

                BsonDocument tempLevel = levelList.Where(t => t.Int("levelId") == tempTask.Int("levelId")).FirstOrDefault();
                BsonDocument tempDiagram = diagramList.Where(t => t.Int("diagramId") == tempTask.Int("diagramId")).FirstOrDefault();

                BsonDocument tempFlowRel = flowRelList.Where(m => m.Int("taskId") == tempTask.Int("taskId")).FirstOrDefault();
                BsonDocument tempFlow = flowList.Where(t => t.Int("flowId") == tempFlowRel.Int("flowId")).FirstOrDefault();
        %>
        <tr>
            <td>
                <%=tempProj.String("name") %>
            </td>
            <td>
                <%=tempEng.String("name")%>
            </td>
            <td>
                <%=tempArea.String("name")%>
            </td>
            <td>
                <%=tempCity.String("name")%>
            </td>
            <td>
                <%=tempTask.String("name")%>(<%=tempTask.Int("taskId") %>)
            </td>
            <td>
                <%=tempTask.String("period")%>
            </td>
            <td>
                <%=tempTask.String("curStartData")%>
            </td>
            <td>
                <%=tempTask.String("curEndData")%>
            </td>
            <td>
                <%=tempTask.String("factStartDate")%>
            </td>
            <td>
                <%=tempTask.String("factEndDate")%>
            </td>
            <td>
                <%=ownerUser.String("name")%>
            </td>
            <td>
                <%=tempLevel.String("name")%>
            </td>
            <td>
                <%=tempFlow.String("name") %>
            </td>
            <td>
                <%=tempDiagram.String("name")%>
            </td>
        </tr>
        <%  } %>
    </table>
</div>--%>
