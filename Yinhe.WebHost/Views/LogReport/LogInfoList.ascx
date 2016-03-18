<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="MongoDB.Driver.Builders" %>
<% 
    int pageSize = PageReq.GetParamInt("pageSize"); pageSize = pageSize == 0 ? 25 : pageSize;
    int current = PageReq.GetParamInt("current"); current = current == 0 ? 1 : current;

    //-------------------------------------日期筛选部分-------------------------------------
    string stStr = PageReq.GetParam("stStr");       //日志开始时间
    string edStr = PageReq.GetParam("edStr");       //日志结束时间

    DateTime stTime, edTime;

    var stQuery = Query.Null;
    var edQuery = Query.Null;

    if (DateTime.TryParse(stStr, out stTime)) stQuery = Query.GTE("timeSort", stTime.ToString("yyyyMMddHHmmss"));
    if (DateTime.TryParse(edStr, out edTime)) edQuery = Query.LTE("timeSort", edTime.ToString("yyyyMMddHHmmss"));

    //-------------------------------------用户筛选部分-------------------------------------
    string logUserId = PageReq.GetParam("userId").TrimEnd(',');       //日志用户Id

    var userQuery = Query.Null;

    if (logUserId != "" && logUserId != "0") userQuery = Query.Or(Query.EQ("logUserId", logUserId.ToString()), Query.EQ("userId", logUserId));

    //-------------------------------------操作筛选部分-------------------------------------
    string resolveIds = PageReq.GetParam("resolveIds");     //获取需要解析的类型Id列表

    List<string> resolveIdList = resolveIds.SplitParam(",").ToList();

    List<BsonDocument> resolveList = new List<BsonDocument>();

    if (resolveIdList.Count > 0)
    {
        resolveList = dataOp.FindAllByKeyValList("SysLogResolve", "resolveId", resolveIdList).ToList();  //所有解析类型
    }
    else
    {
        resolveList = dataOp.FindAll("SysLogResolve").ToList();
    }

    var resolveQuery = Query.Null;

    foreach (var tempResolve in resolveList)
    {
        QueryDocument tempQuery = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<QueryDocument>(tempResolve.String("query"));
        resolveQuery = Query.Or(resolveQuery, tempQuery);
    }

    //-------------------------------------获取详细日志-------------------------------------
    var logQuery = dataOp.FindAllByQuery("SysBehaviorLog", Query.And(stQuery, edQuery, userQuery, resolveQuery));

    var allCount = logQuery.Count();
    var totalPage = allCount % pageSize == 0 ? allCount / pageSize : (allCount / pageSize) + 1;
    List<BsonDocument> logList = logQuery.OrderByDescending(t => t.String("timeSort")).Skip((current - 1) * pageSize).Take(pageSize).ToList();

    //-------------------------------------获取详细日志-------------------------------------
    List<string> userIdList = logList.Select(t => t.String("logUserId")).Distinct().ToList();                              //日志用户Id列表  
    List<BsonDocument> userList = dataOp.FindAllByKeyValList("SysUser", "userId", userIdList).ToList();         //所有用到的用户列表
    List<BsonDocument> userPostList = dataOp.FindAllByKeyValList("UserOrgPost", "userId", userIdList).ToList(); //用户岗位关联表
    List<string> postIdList = userPostList.Select(t => t.String("postId")).Distinct().ToList();                            //岗位Id列表
    List<BsonDocument> postList = dataOp.FindAllByKeyValList("OrgPost", "postId", postIdList).ToList();         //岗位列表
    List<string> orgIdList = postList.Select(t => t.String("orgId")).Distinct().ToList();                                  //部门Id列表
    List<BsonDocument> orgList = dataOp.FindAllByKeyValList("Organization", "orgId", orgIdList).ToList();       //部门列表

    List<string> projIdList = logList.Select(t => t.String("projId")).Distinct().ToList();      //用到的项目Id列表
    List<BsonDocument> projList = dataOp.FindAllByKeyValList("XH_DesignManage_Project", "projId", projIdList).ToList();    //用到的项目列表
    List<string> planIdList = logList.Select(t => t.String("planId")).Distinct().ToList();      //用到的计划Id列表
    List<BsonDocument> planList = dataOp.FindAllByKeyValList("XH_DesignManage_Plan", "planId", planIdList).ToList();    //用到的计划列表
    List<string> taskIdList = logList.Select(t => t.String("taskId")).Distinct().ToList();      //用到的任务Id列表
    List<BsonDocument> taskList = dataOp.FindAllByKeyValList("XH_DesignManage_Task", "taskId", taskIdList).ToList();    //用到的任务列表

    List<string> lineIdList = logList.Select(t => t.String("lineId")).Distinct().ToList();      //用到的产品线Id列表
    List<BsonDocument> lineList = dataOp.FindAllByKeyValList("XH_ProductDev_ProductLine", "lineId", lineIdList).ToList();    //用到的产品线

    List<string> retIdList = logList.Select(t => t.String("retId")).Distinct().ToList();      //用到的成果Id列表
    List<BsonDocument> retList = dataOp.FindAllByKeyValList("XH_StandardResult_StandardResult", "retId", retIdList).ToList();    //用到的成果

    List<string> cityIdList = logList.Select(t => t.String("cityId")).Distinct().ToList();      //用到的城市Id列表
    List<BsonDocument> cityList = dataOp.FindAllByKeyValList("XH_ProductDev_City", "cityId", cityIdList).ToList();    //用到的城市

    List<string> segmentIdList = logList.Select(t => t.String("segmentId")).Distinct().ToList();      //用到的客群Id列表
    List<BsonDocument> segmentList = dataOp.FindAllByKeyValList("XH_ProductDev_Segment", "segmentId", segmentIdList).ToList();    //用到的客群
%>
<table class="table05" width="100%">
    <colgroup>
        <col width="40" />
        <col width="70" />
        <col width="90" />
        <col width="100" />
        <col width="95" />
        <col width="115" />
        <col width="110" />
        <col  />
    </colgroup>
    <thead class="thead03">
        <tr>
            <th align="left">
                <div class="th_con">
                    序号</div>
            </th>
            <th align="left">
                <div class="th_con">
                    用户</div>
            </th>
            <th align="left">
                <div class="th_con">
                    部门</div>
            </th>
            <th>
                <div class="th_con">
                    操作类型</div>
            </th>
            <th>
                <div class="th_con">
                    系统模块</div>
            </th>
            <th>
                <div class="th_con">
                    模块对象</div>
            </th>
            <th>
                <div class="th_con">
                    具体数据</div>
            </th>
            <th align="left">
                <div class="th_con">
                    日志时间</div>
            </th>
            <th>
                <div class="th_con">
                    Ip地址</div>
            </th>
        </tr>
    </thead>
    <tbody>
        <%  int i = (current - 1) * pageSize;
            foreach (var tempLog in logList.OrderByDescending(t => t.String("timeSort")))
            {
                i = i + 1;

                BsonDocument tempUser = userList.Where(t => t.Int("userId") == tempLog.Int("logUserId")).FirstOrDefault();
                BsonDocument tempPostRel = userPostList.Where(t => t.Int("userId") == tempUser.Int("userId")).FirstOrDefault();
                BsonDocument tempPost = postList.Where(t => t.Int("postId") == tempPostRel.Int("postId")).FirstOrDefault();
                BsonDocument tempOrg = orgList.Where(t => t.Int("orgId") == tempPost.Int("orgId")).FirstOrDefault();

                string opType = "";     //操作类型
                string module = "";     //模块名称
                string objName = "";    //模块对象
                string concrete = "";   //具体数据

                if (tempLog.Int("logType") == 1)
                {
                    opType = "登录系统";
                    module = "系统";
                }

                if (tempLog.Int("logType") == 2)
                {
                    opType = "退出系统";
                    module = "系统";
                }

                //查看项目
                if (tempLog.String("path").Contains("ProjectIndex"))
                {
                    BsonDocument tempProj = projList.Where(t => t.Int("projId") == tempLog.Int("projId")).FirstOrDefault();
                    opType = "查看项目";
                    module = "项目设计管理";
                    objName = tempProj != null ? tempProj.String("name") : "";
                }

                //查看计划
                if (tempLog.String("path").Contains("PlanManage"))
                {
                    BsonDocument tempProj = projList.Where(t => t.Int("projId") == tempLog.Int("projId")).FirstOrDefault();
                    opType = "查看计划";
                    module = "项目设计管理";
                    objName = tempProj != null ? tempProj.String("name") : "";
                    concrete = tempProj != null ? tempProj.String("name") + "计划" : "";
                }

                //查看任务
                if (tempLog.String("path").Contains("ProjTaskInfo"))
                {
                    string tempTaskId = tempLog.String("path").SplitParam("/")[2];
                    BsonDocument tempTask = dataOp.FindOneByKeyVal("XH_DesignManage_Task", "taskId", tempTaskId);
                    BsonDocument tempProj = null;
                    if (tempTask != null) tempProj = dataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", tempTask.String("projId"));

                    opType = "查看任务";
                    module = "项目设计管理";
                    objName = tempProj != null ? tempProj.String("name") : "";
                    concrete = tempTask != null ? tempTask.String("name") : "";
                }

                //编辑项目
                if (tempLog.String("path").Contains("ProjectManage"))
                {
                    BsonDocument tempProj = projList.Where(t => t.Int("projId") == tempLog.Int("projId")).FirstOrDefault();
                    opType = "编辑项目";
                    module = "项目设计管理";
                    objName = tempProj != null ? tempProj.String("name") : "";
                }

                //编辑计划
                if (tempLog.String("path").Contains("SavePlanInfo") && tempLog.String("tbName") == "XH_DesignManage_Plan")
                {
                    BsonDocument tempProj = projList.Where(t => t.Int("projId") == tempLog.Int("projId")).FirstOrDefault();
                    opType = "编辑计划";
                    module = "项目设计管理";
                    objName = tempProj != null ? tempProj.String("name") : "";
                    concrete = tempProj != null ? tempProj.String("name") + "计划" : "";
                }

                //创建任务
                if (tempLog.String("path").Contains("QuickCreateTask"))
                {
                    BsonDocument tempProj = projList.Where(t => t.Int("projId") == tempLog.Int("projId")).FirstOrDefault();
                    opType = "创建任务";
                    module = "项目设计管理";
                    objName = tempProj != null ? tempProj.String("name") : "";
                }

                //编辑任务
                if (tempLog.String("path").Contains("SavePostInfo") && tempLog.String("tbName") == "XH_DesignManage_Task")
                {
                    BsonDocument tempTask = taskList.Where(t => t.Int("taskId") == tempLog.Int("taskId")).FirstOrDefault();

                    if (tempTask == null) tempTask = dataOp.FindOneByQuery("XH_DesignManage_Task", TypeConvert.NativeQueryToQuery(tempLog.String("queryStr")));
                    BsonDocument tempProj = null;
                    if (tempTask != null) tempProj = dataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", tempTask.String("projId"));

                    if (tempLog.Int("status") == 3) opType = "启动任务";
                    else if (tempLog.Int("status") == 4) opType = "完成任务";
                    else opType = "编辑任务";
                    module = "项目设计管理";
                    objName = tempProj != null ? tempProj.String("name") : "";
                    concrete = tempTask != null ? tempTask.String("name") : "";
                }

                //改变计划状态
                if (tempLog.String("path").Contains("ChangePlanStatus"))
                {
                    BsonDocument tempPlan = planList.Where(t => t.Int("planId") == tempLog.Int("planId")).FirstOrDefault();
                    BsonDocument tempProj = null;
                    if (tempPlan != null) tempProj = dataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", tempPlan.String("projId"));

                    if (tempLog.String("status") == "Processing") opType = "启动计划";
                    else if (tempLog.String("status") == "Completed") opType = "完成计划";
                    module = "项目设计管理";
                    objName = tempProj != null ? tempProj.String("name") : "";
                    concrete = tempProj != null ? tempProj.String("name") + "计划" : "";
                }

                //删除项目
                if (tempLog.String("path").Contains("DelePostInfo") && tempLog.String("tbName") == "XH_DesignManage_Project")
                {
                    BsonDocument tempDataLog = dataOp.FindAllByQuery("", Query.And(
                        Query.EQ("behaviorId", tempLog.String("_id")),
                        Query.EQ("tableName", "XH_DesignManage_Project"))).Where(t => t.String("oldData", "") != "").FirstOrDefault();

                    BsonDocument tempProj = null;
                    if (tempDataLog != null) tempProj = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(tempDataLog.String("oldData"));

                    opType = "删除项目";
                    module = "项目设计管理";
                    objName = tempProj != null ? tempProj.String("name") : "";
                }

                //删除计划
                if (tempLog.String("path").Contains("DelePostInfo") && tempLog.String("tbName") == "XH_DesignManage_Plan")
                {
                    BsonDocument tempDataLog = dataOp.FindAllByQuery("", Query.And(
                        Query.EQ("behaviorId", tempLog.String("_id")),
                        Query.EQ("tableName", "XH_DesignManage_Plan"))).Where(t => t.String("oldData", "") != "").FirstOrDefault();

                    BsonDocument tempPlan = null;
                    if (tempDataLog != null) tempPlan = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(tempDataLog.String("oldData"));
                    BsonDocument tempProj = null;
                    if (tempPlan != null) dataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", tempPlan.String("projId"));

                    opType = "删除计划";
                    module = "项目设计管理";
                    objName = tempProj != null ? tempProj.String("name") : "";
                    concrete = tempProj != null ? tempProj.String("name") + "计划" : "";
                }

                //删除任务
                if (tempLog.String("path").Contains("DeleteTask"))
                {
                    List<BsonDocument> tempDataLog = dataOp.FindAllByQuery("", Query.And(
                        Query.EQ("behaviorId", tempLog.String("_id")),
                        Query.EQ("tableName", "XH_DesignManage_Task"))).Where(t => t.String("oldData", "") != "").ToList();

                    List<BsonDocument> tempTaskList = new List<BsonDocument>();

                    string tempTaskNames = "";
                    foreach (var dataLog in tempDataLog)
                    {
                        var tempTask = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(dataLog.String("oldData"));
                        tempTaskList.Add(tempTask);
                        tempTaskNames += string.Format("{0},", tempTask.String("name"));
                    }

                    BsonDocument tempProj = null;
                    if (tempTaskList.Count > 0) dataOp.FindOneByKeyVal("XH_DesignManage_Project", "projId", tempTaskList[0].String("projId"));

                    opType = "删除任务";
                    module = "项目设计管理";
                    objName = tempProj != null ? tempProj.String("name") : "";
                    concrete = tempTaskNames;
                }

                //编辑产品系列
                if (tempLog.String("path").Contains("SavePostInfo") && tempLog.String("tbName") == " XH_ProductDev_ProductSeries")
                {
                    BsonDocument tempDataLog = dataOp.FindAllByQuery("", Query.And(
                        Query.EQ("behaviorId", tempLog.String("_id")),
                        Query.EQ("tableName", "XH_ProductDev_ProductSeries"))).Where(t => t.String("opData", "") != "").FirstOrDefault();

                    BsonDocument tempSeries = null;
                    if (tempDataLog != null) tempSeries = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(tempDataLog.String("opData"));

                    opType = "编辑产品系列";
                    module = "产品标准平台";
                    objName = tempSeries != null ? tempSeries.String("name") : "";
                }

                //查看产品线
                if (tempLog.String("path").Contains("ProductLineShow"))
                {
                    BsonDocument tempLine = lineList.Where(t => t.Int("lineId") == tempLog.Int("lineId")).FirstOrDefault();

                    BsonDocument tempSeries = null;
                    if (tempLine != null) tempSeries = dataOp.FindOneByKeyVal("XH_ProductDev_ProductSeries", "seriesId", tempLine.String("seriesId"));

                    opType = "查看产品线";
                    module = "产品标准平台";
                    objName = tempSeries != null ? tempSeries.String("name") : "";
                    concrete = tempLine != null ? tempLine.String("name") : "";
                }

                //编辑产品线
                if (tempLog.String("path").Contains("ProductLineEdit"))
                {
                    BsonDocument tempLine = lineList.Where(t => t.Int("lineId") == tempLog.Int("lineId")).FirstOrDefault();

                    BsonDocument tempSeries = null;
                    if (tempLine != null) tempSeries = dataOp.FindOneByKeyVal("XH_ProductDev_ProductSeries", "seriesId", tempLine.String("seriesId"));

                    opType = "编辑产品线";
                    module = "产品标准平台";
                    objName = tempSeries != null ? tempSeries.String("name") : "";
                    concrete = tempLine != null ? tempLine.String("name") : "";
                }

                //编辑产品线土地客群
                if (tempLog.String("path").Contains("ProductLineLSSetting"))
                {
                    BsonDocument tempLine = lineList.Where(t => t.Int("lineId") == tempLog.Int("lineId")).FirstOrDefault();

                    BsonDocument tempSeries = null;
                    if (tempLine != null) tempSeries = dataOp.FindOneByKeyVal("XH_ProductDev_ProductSeries", "seriesId", tempLine.String("seriesId"));

                    opType = "编辑产品线土地客群";
                    module = "产品标准平台";
                    objName = tempSeries != null ? tempSeries.String("name") : "";
                    concrete = tempLine != null ? tempLine.String("name") : "";
                }

                //编辑产品线价值树与产品配置
                if (tempLog.String("path").Contains("ProductLineValTreeSetting"))
                {
                    BsonDocument tempLine = lineList.Where(t => t.Int("lineId") == tempLog.Int("lineId")).FirstOrDefault();

                    BsonDocument tempSeries = null;
                    if (tempLine != null) tempSeries = dataOp.FindOneByKeyVal("XH_ProductDev_ProductSeries", "seriesId", tempLine.String("seriesId"));

                    opType = "编辑产品线价值树与产品配置";
                    module = "产品标准平台";
                    objName = tempSeries != null ? tempSeries.String("name") : "";
                    concrete = tempLine != null ? tempLine.String("name") : "";
                }

                //删除产品系列
                if (tempLog.String("path").Contains("DelePostInfo") && tempLog.String("tbName") == " XH_ProductDev_ProductSeries")
                {
                    BsonDocument tempDataLog = dataOp.FindAllByQuery("", Query.And(
                        Query.EQ("behaviorId", tempLog.String("_id")),
                        Query.EQ("tableName", "XH_ProductDev_ProductSeries"))).Where(t => t.String("oldData", "") != "").FirstOrDefault();

                    BsonDocument tempSeries = null;
                    if (tempDataLog != null) tempSeries = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(tempDataLog.String("oldData"));

                    opType = "删除产品系列";
                    module = "产品标准平台";
                    objName = tempSeries != null ? tempSeries.String("name") : "";
                }

                //删除产品线
                if (tempLog.String("path").Contains("DelePostInfo") && tempLog.String("tbName") == " XH_ProductDev_ProductLine")
                {
                    BsonDocument tempDataLog = dataOp.FindAllByQuery("", Query.And(
                        Query.EQ("behaviorId", tempLog.String("_id")),
                        Query.EQ("tableName", "XH_ProductDev_ProductLine"))).Where(t => t.String("oldData", "") != "").FirstOrDefault();

                    BsonDocument tempLine = null;
                    if (tempDataLog != null) tempLine = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(tempDataLog.String("oldData"));

                    BsonDocument tempSeries = null;
                    if (tempLine != null) tempSeries = dataOp.FindOneByKeyVal("XH_ProductDev_ProductSeries", "seriesId", tempLine.String("seriesId"));

                    opType = "删除产品线";
                    module = "产品标准平台";
                    objName = tempSeries != null ? tempSeries.String("name") : "";
                    concrete = tempLine != null ? tempLine.String("name") : "";
                }

                //查看户型成果 
                if (tempLog.String("path").Contains("UnitView"))
                {
                    BsonDocument tempRet = retList.Where(t => t.Int("retId") == tempLog.Int("retId")).FirstOrDefault();

                    opType = "查看成果";
                    module = "产品模块标准";
                    objName = "户型库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //查看精装修成果
                if (tempLog.String("path").Contains("FineDecorationView"))
                {
                    BsonDocument tempRet = retList.Where(t => t.Int("retId") == tempLog.Int("retId")).FirstOrDefault();

                    opType = "查看成果";
                    module = "产品模块标准";
                    objName = "批量精装修库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //查看立面成果
                if (tempLog.String("path").Contains("FacadeView"))
                {
                    BsonDocument tempRet = retList.Where(t => t.Int("retId") == tempLog.Int("retId")).FirstOrDefault();

                    opType = "查看成果";
                    module = "产品模块标准";
                    objName = "立面库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //查看示范区成果
                if (tempLog.String("path").Contains("DemonstrationAreaView"))
                {
                    BsonDocument tempRet = retList.Where(t => t.Int("retId") == tempLog.Int("retId")).FirstOrDefault();

                    opType = "查看成果";
                    module = "产品模块标准";
                    objName = "示范区库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //查看景观成果
                if (tempLog.String("path").Contains("LandscapeView"))
                {
                    BsonDocument tempRet = retList.Where(t => t.Int("retId") == tempLog.Int("retId")).FirstOrDefault();

                    opType = "查看成果";
                    module = "产品模块标准";
                    objName = "景观库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //查看工艺工法成果
                if (tempLog.String("path").Contains("PEMView"))
                {
                    BsonDocument tempRet = retList.Where(t => t.Int("retId") == tempLog.Int("retId")).FirstOrDefault();

                    opType = "查看成果";
                    module = "产品模块标准";
                    objName = "工艺工法库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //查看公共部位成果
                if (tempLog.String("path").Contains("PublicPartsView"))
                {
                    BsonDocument tempRet = retList.Where(t => t.Int("retId") == tempLog.Int("retId")).FirstOrDefault();

                    opType = "查看成果";
                    module = "产品模块标准";
                    objName = "公共部位库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //编辑户型成果
                if (tempLog.String("path").Contains("UnitEdit"))
                {
                    BsonDocument tempRet = retList.Where(t => t.Int("retId") == tempLog.Int("retId")).FirstOrDefault();

                    opType = tempLog.Int("retId") == 0 ? "新增成果" : "编辑成果";
                    module = "产品模块标准";
                    objName = "户型库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }


                //编辑精装修成果
                if (tempLog.String("path").Contains("FineDecorationEdit"))
                {
                    BsonDocument tempRet = retList.Where(t => t.Int("retId") == tempLog.Int("retId")).FirstOrDefault();

                    opType = tempLog.Int("retId") == 0 ? "新增成果" : "编辑成果";
                    module = "产品模块标准";
                    objName = "批量精装修库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //编辑立面成果
                if (tempLog.String("path").Contains("FacadeEdit"))
                {
                    BsonDocument tempRet = retList.Where(t => t.Int("retId") == tempLog.Int("retId")).FirstOrDefault();

                    opType = tempLog.Int("retId") == 0 ? "新增成果" : "编辑成果";
                    module = "产品模块标准";
                    objName = "立面库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //编辑示范区成果
                if (tempLog.String("path").Contains("DemonstrationAreaEdit"))
                {
                    BsonDocument tempRet = retList.Where(t => t.Int("retId") == tempLog.Int("retId")).FirstOrDefault();

                    opType = tempLog.Int("retId") == 0 ? "新增成果" : "编辑成果";
                    module = "产品模块标准";
                    objName = "示范区库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //编辑景观成果
                if (tempLog.String("path").Contains("LandscapeEdit"))
                {
                    BsonDocument tempRet = retList.Where(t => t.Int("retId") == tempLog.Int("retId")).FirstOrDefault();

                    opType = tempLog.Int("retId") == 0 ? "新增成果" : "编辑成果";
                    module = "产品模块标准";
                    objName = "景观库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //编辑工艺工法成果
                if (tempLog.String("path").Contains("PEMEdit"))
                {
                    BsonDocument tempRet = retList.Where(t => t.Int("retId") == tempLog.Int("retId")).FirstOrDefault();

                    opType = tempLog.Int("retId") == 0 ? "新增成果" : "编辑成果";
                    module = "产品模块标准";
                    objName = "工艺工法库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //编辑公共部位成果
                if (tempLog.String("path").Contains("PublicPartsEdit"))
                {
                    BsonDocument tempRet = retList.Where(t => t.Int("retId") == tempLog.Int("retId")).FirstOrDefault();

                    opType = tempLog.Int("retId") == 0 ? "新增成果" : "编辑成果";
                    module = "产品模块标准";
                    objName = "公共部位库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //删除成果
                if (tempLog.String("path").Contains("DelePostInfo") && tempLog.String("tbName") == " XH_StandardResult_StandardResult")
                {
                    BsonDocument tempDataLog = dataOp.FindAllByQuery("", Query.And(
                        Query.EQ("behaviorId", tempLog.String("_id")),
                        Query.EQ("tableName", "XH_StandardResult_StandardResult"))).Where(t => t.String("oldData", "") != "").FirstOrDefault();

                    BsonDocument tempRet = null;
                    if (tempDataLog != null) tempRet = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(tempDataLog.String("oldData"));

                    opType = "删除成果";
                    module = "产品标准平台";
                    objName = "";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //查看离散设计要素
                if (tempLog.String("path").Contains("DDLibraryShow"))
                {
                    BsonDocument tempRet = dataOp.FindOneByKeyVal("XH_StandardResult_DiscreteDesignItem", "itemId", tempLog.String("id"));

                    opType = "查看离散设计要素";
                    module = "研发基础数据库";
                    objName = "离散设计要素库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //编辑离散设计要素
                if (tempLog.String("path").Contains("DDLibraryManage"))
                {
                    BsonDocument tempRet = dataOp.FindOneByKeyVal("XH_StandardResult_DiscreteDesignItem", "itemId", tempLog.String("id"));

                    opType = tempLog.Int("id") == 0 ? "新增离散设计要素" : "编辑离散设计要素";
                    module = "研发基础数据库";
                    objName = "离散设计要素库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //删除离散设计要素
                if (tempLog.String("path").Contains("DelePostInfo") && tempLog.String("tbName") == " XH_StandardResult_DiscreteDesignItem")
                {
                    BsonDocument tempDataLog = dataOp.FindAllByQuery("", Query.And(
                        Query.EQ("behaviorId", tempLog.String("_id")),
                        Query.EQ("tableName", "XH_StandardResult_DiscreteDesignItem"))).Where(t => t.String("oldData", "") != "").FirstOrDefault();

                    BsonDocument tempRet = null;
                    if (tempDataLog != null) tempRet = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(tempDataLog.String("oldData"));

                    opType = "删除离散设计要素";
                    module = "研发基础数据库";
                    objName = "离散设计要素库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //查看标杆案例
                if (tempLog.String("path").Contains("CaseModelsShow"))
                {
                    BsonDocument tempRet = dataOp.FindOneByKeyVal("XH_StandardResult_CaseModels", "cmlId", tempLog.String("id"));

                    opType = "查看标杆案例";
                    module = "研发基础数据库";
                    objName = "标杆案例库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //编辑标杆案例
                if (tempLog.String("path").Contains("CaseModelsManage"))
                {
                    BsonDocument tempRet = dataOp.FindOneByKeyVal("XH_StandardResult_CaseModels", "cmlId", tempLog.String("id"));

                    opType = tempLog.Int("id") == 0 ? "新增标杆案例" : "编辑标杆案例";
                    module = "研发基础数据库";
                    objName = "标杆案例库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //删除标杆案例
                if (tempLog.String("path").Contains("DelePostInfo") && tempLog.String("tbName") == " XH_StandardResult_CaseModels")
                {
                    BsonDocument tempDataLog = dataOp.FindAllByQuery("", Query.And(
                        Query.EQ("behaviorId", tempLog.String("_id")),
                        Query.EQ("tableName", "XH_StandardResult_CaseModels"))).Where(t => t.String("oldData", "") != "").FirstOrDefault();

                    BsonDocument tempRet = null;
                    if (tempDataLog != null) tempRet = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(tempDataLog.String("oldData"));

                    opType = "删除离标杆案例";
                    module = "研发基础数据库";
                    objName = "标杆案例库";
                    concrete = tempRet != null ? tempRet.String("name") : "";
                }

                //查看土地城市
                if (tempLog.String("path").Contains("LandCityIndex"))
                {
                    BsonDocument tempCity = cityList.Where(t => t.Int("cityId") == tempLog.Int("cityId")).FirstOrDefault();

                    opType = "查看土地城市";
                    module = "土地库";
                    objName = "土地库";
                    concrete = tempCity != null ? tempCity.String("name") : "";
                }

                //编辑土地城市
                if (tempLog.String("path").Contains("LandCityManage"))
                {
                    BsonDocument tempCity = cityList.Where(t => t.Int("cityId") == tempLog.Int("cityId")).FirstOrDefault();

                    opType = "编辑土地城市";
                    module = "土地库";
                    objName = "土地库";
                    concrete = tempCity != null ? tempCity.String("name") : "";
                }

                //查看客群
                if (tempLog.String("path").Contains("SegmentShow"))
                {
                    BsonDocument tempSegment = segmentList.Where(t => t.Int("segmentId") == tempLog.Int("segmentId")).FirstOrDefault();

                    opType = "查看客群";
                    module = "客群库";
                    objName = "客群库";
                    concrete = tempSegment != null ? tempSegment.String("name") : "";
                }

                //编辑客群
                if (tempLog.String("path").Contains("SegmentEdit"))
                {
                    BsonDocument tempSegment = segmentList.Where(t => t.Int("segmentId") == tempLog.Int("segmentId")).FirstOrDefault();

                    opType = tempLog.Int("id") == 0 ? "新增客群" : "编辑客群";
                    module = "客群库";
                    objName = "客群库";
                    concrete = tempSegment != null ? tempSegment.String("name") : "";
                }

                //删除客群
                if (tempLog.String("path").Contains("DelePostInfo") && tempLog.String("tbName") == " XH_ProductDev_Segment")
                {
                    BsonDocument tempDataLog = dataOp.FindAllByQuery("", Query.And(
                        Query.EQ("behaviorId", tempLog.String("_id")),
                        Query.EQ("tableName", "XH_ProductDev_Segment"))).Where(t => t.String("oldData", "") != "").FirstOrDefault();

                    BsonDocument tempSegment = null;
                    if (tempDataLog != null) tempSegment = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(tempDataLog.String("oldData"));

                    opType = "删除客群";
                    module = "客群库";
                    objName = "客群库";
                    concrete = tempSegment != null ? tempSegment.String("name") : "";
                }
        %>
        <tr>
            <td>
                <div class="tb_con">
                    <%=i %></div>
            </td>
            <td>
                <div class="tb_con">
                    <%=tempUser.String("name")%></div>
            </td>
            <td>
                <div class="center">
                    <%=tempOrg.String("name")%></div>
            </td>
            <td align="center">
                <div class="tb_con">
                    <%=opType%></div>
            </td>
            <td align="center">
                <div class="tb_con">
                    <%=module%>
                </div>
            </td>
            <td align="center">
                <div class="tb_con">
                    <%=objName%></div>
            </td>
            <td>
                <div class="tb_con">
                    <%=concrete%></div>
            </td>
            <td align="center">
                <div class="tb_con">
                    <%=tempLog.String("logTime")%></div>
            </td>
            <td>
                <div class="tb_con">
                    <%=tempLog.String("ipAddress")%></div>
            </td>
        </tr>
        <%} %>
    </tbody>
</table>
<div id="page">
</div>
<script type="text/javascript">
    var totalPage = "<%=totalPage %>";
    var current = "<%=current %>";
    var pageSize = "<%=pageSize %>";

    $("#page").pagination({ totalPage: totalPage, display_pc: 7, current_page: current, showPageList: false, showRefresh: false, displayMsg: '', pageSize: pageSize,
        onSelectPage: function (current_page, pageSize) {
            var durl = "/LogReport/LogInfoList?userId=<%=logUserId %>&stStr=<%=stStr %>&edStr=<%=edStr %>&resolveIds=<%=resolveIds %>&current=" + current_page + "&r=" + Math.random();
            $("#logInfoListDiv").load(durl);
        }
    }); 
</script>
