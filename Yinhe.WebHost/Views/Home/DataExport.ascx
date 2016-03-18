<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<form id="saveForm" action="" method="post">
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
            <input type="text" style="width: 600px; font-size: 20px" name="queryJson" />
        </td>
        <td>
            查询JSON
        </td>
    </tr>
    <tr>
        <td colspan="3">
            <input type="button" value="获取数据" onclick="getFile();" />
        </td>
    </tr>
</table>
</form>
<br />
<br />
<!--导出日志相关数据-->
<form id="exportLogForm" action="" method="post">
<table id="exportLogTable">
    <tr>
        <td>
            行为日志Id:
        </td>
        <td>
            <input type="text" style="width: 600px; font-size: 20px" name="behaviorId" />
        </td>
        <td>
        </td>
    </tr>
    <tr>
        <td>
            导出类型:
        </td>
        <td>
            <select name="logType">
                <option value="5" selected="selected">被错误删除数据</option>
                <option value="3">被错误添加数据</option>
            </select>
        </td>
        <td>
        </td>
    </tr>
    <tr>
        <td colspan="3">
            <input type="button" value="获取数据" onclick="getLogFile();" />
        </td>
    </tr>
</table>
</form>
<script type="text/javascript">
    function getFile() {
        var tbName = $("#saveForm").find("input[name=tbName]").val();
        var queryJson = $("#saveForm").find("input[name=queryJson]").val();

        alert(tbName);
        alert(queryJson);

        window.location.href = "/Home/ExportDataBaseToFile?tbName=" + tbName + "&queryJson=" + queryJson + "&r=" + Math.random();
    }

    //导出日志文件
    function getLogFile() {
        var behaviorId = $("#exportLogForm").find("input[name=behaviorId]").val();
        var logType = $("#exportLogForm").find("select[name=logType]").val();

        var dataType = "oldData";
        if (logType == 3) dataType = "opData";

        $.ajax({
            url: "/Home/GetLogRecoveryFile",
            type: 'post',
            data: {
                behaviorId: behaviorId,
                type: logType,
                dataType: dataType
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
                    alert("导出成功");
                }
            }
        });
    }

</script>
