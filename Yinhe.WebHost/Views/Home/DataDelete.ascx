<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<form id="delForm" action="" method="post">
<table>
    <tr>
        <td>
            表名:
        </td>
        <td>
            <input type="text" style="width: 600px; font-size: 20px" name="tbName" />
        </td>
        <td>
            tbName
        </td>
    </tr>
    <tr>
        <td>
            查询:
        </td>
        <td>
            <input type="text" style="width: 600px; font-size: 20px" name="queryJson" value="{'cloumnName':'1'} " />
        </td>
        <td>
            {'cloumnName':'1'}
        </td>
    </tr>
    <tr>
        <td colspan="3">
            <input type="button" value="删 除" onclick="mainDel();" />
        </td>
    </tr>
</table>
</form>
<br />
<hr />
<br />
<div>
    <h2>
        冗余列表(计划表名: XH_DesignManage_Plan) (任务表名: XH_DesignManage_Task):</h2>
    <br />
    <% 
        DataOperation dataOp = new DataOperation();

        List<BsonDocument> allProjList = dataOp.FindAll("XH_DesignManage_Project").ToList();

        List<BsonDocument> allPlanList = dataOp.FindAll("XH_DesignManage_Plan").ToList();

        List<string> projIdList = allProjList.Select(t => t.String("projId")).ToList();

        List<string> planIdList = allPlanList.Select(t => t.String("planId")).ToList();

        List<BsonDocument> overPlanList = dataOp.FindAllByQuery("XH_DesignManage_Plan", Query.And(
            Query.NotIn("projId", TypeConvert.StringListToBsonValueList(projIdList)),
            Query.NE("isExpTemplate", "1"),
            Query.NE("isPrimLib", "1")
            )).ToList();

        List<BsonDocument> overTaskList = dataOp.FindAllByQuery("XH_DesignManage_Task", Query.Or(
             Query.NotIn("planId", TypeConvert.StringListToBsonValueList(planIdList)),
             Query.NotIn("projId", TypeConvert.StringListToBsonValueList(projIdList))
             )).ToList();

        overTaskList.AddRange(dataOp.FindAllByQuery("XH_DesignManage_Task", Query.In("planId", TypeConvert.StringListToBsonValueList(overPlanList.Select(t => t.String("planId")).ToList()))));


    %>
    <table border="1" width="500px">
        <tr>
            <td>
                计划Id
            </td>
            <td>
                计划是否存在
            </td>
            <td>
                拥有任务数
            </td>
            <td>
                建议删除查询
            </td>
            <td>
                操作
            </td>
        </tr>
        <%  foreach (var temp in overPlanList)
            {
                var tempTaskList = overTaskList.Where(t => t.Int("planId") == temp.Int("planId")).ToList();
        %>
        <tr>
            <td>
                <%=temp.String("planId") %>
            </td>
            <td>
                yes
            </td>
            <td>
                <%=tempTaskList.Count%>
            </td>
            <td>
                {'planId':'<%=temp.String("planId") %>'}
            </td>
            <td>
                <input type="button" tbname="XH_DesignManage_Plan" queryjson="{'planId':'<%=temp.String("planId") %>'}"
                    value="删 除" onclick="subDel(this);" />
            </td>
        </tr>
        <%  }
            foreach (var temp in overTaskList.Select(t => t.Int("planId")).Distinct().ToList())
            {
                var tempPlan = allPlanList.Where(t => t.Int("planId") == temp).FirstOrDefault();
                
                if (tempPlan != null && (tempPlan.Int("isExpTemplate") == 1 || tempPlan.Int("isPrimLib") == 1)) continue;
                
                var tempTaskList = overTaskList.Where(t => t.Int("planId") == temp).ToList();
        %>
        <tr>
            <td>
                <%=temp %>
            </td>
            <td>
                no
            </td>
            <td>
                <%=tempTaskList.Count%>
            </td>
            <td>
                {'planId':'<%=temp %>'}
            </td>
            <td>
                <input type="button" tbname="XH_DesignManage_Task" queryjson="{'planId':'<%=temp %>'}"
                    value="删 除" onclick="subDel(this);" />
            </td>
        </tr>
        <%  } %>
    </table>
</div>
<script type="text/javascript">
    function mainDel() {
        var tbName = $("#delForm").find("input[name=tbName]").val();
        var queryJson = $("#delForm").find("input[name=queryJson]").val();

        var a = confirm("确定要删除 表" + tbName + "中的记录:" + queryJson);
        if (!a) return;
        else delInfo(tbName, queryJson);
    }

    function subDel(obj) {
        var tbName = $(obj).attr("tbname");
        var queryJson = $(obj).attr("queryjson");

        var a = confirm("确定要删除 表" + tbName + "中的记录:" + queryJson);
        if (!a) return;
        else delInfo(tbName, queryJson);
    }

    function delInfo(tbName, queryJson) {
        $.ajax({
            url: "/Home/UniversalDelete",
            type: 'post',
            data: {
                tbName: tbName,
                queryJson: queryJson
            },
            dataType: 'json',
            error: function () {
                alert("未知错误，请联系服务器管理员，或者刷新页面重试");
            },
            success: function (data) {
                if (data.Success == false) {
                    alert(data.Message);
                }
                else {
                    alert("删除成功");
                    $('#adminOperaDiv').load('/Home/DataDelete?r' + Math.random());
                }
            }
        });
    }
</script>
