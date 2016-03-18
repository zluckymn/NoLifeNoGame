<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<form id="importForm" action="" method="post">
<table id="importTable">
    <tr>
        <td colspan="3">
            <h2>
                导入数据</h2>
        </td>
    </tr>
    <tr>
        <td>
            表名:
        </td>
        <td>
            <input type="text" style="width: 560px; font-size: 20px" name="tbName" />
        </td>
        <td>
            tbName
        </td>
    </tr>
    <tr>
        <td>
            JSON地址:
        </td>
        <td>
            <input type="text" name="jsonUrl" style="width: 560px; font-size: 20px" value="" />
        </td>
        <td>
            D:/temp.json
        </td>
    </tr>
    <tr>
        <td>
            是否追加:
        </td>
        <td>
            <input type="radio" name="isAdd" value="0" checked="checked" />覆盖
            <input type="radio" name="isAdd" value="1" />追加
        </td>
        <td>
        </td>
    </tr>
    <tr>
        <td>
            是否影响其他表:
        </td>
        <td>
            <input type="radio" name="isAffect" value="0" checked="checked" />不影响
            <input type="radio" name="isAffect" value="1" />影响
        </td>
        <td>
        </td>
    </tr>
    <tr>
        <td colspan="3">
            <input type="button" value="导入" onclick="importInfo();" />
        </td>
    </tr>
</table>
</form>
<br />
<br />
<form id="recoveryLogForm" action="" method="post">
<table id="recoveryLogTable">
    <tr>
        <td colspan="3">
            <h2>
                恢复日志数据</h2>
        </td>
    </tr>
    <tr>
        <td>
            表名:
        </td>
        <td>
            <input type="text" style="width: 560px; font-size: 20px" name="tbName" />
        </td>
        <td>
            tbName
        </td>
    </tr>
    <tr>
        <td>
            JSON地址:
        </td>
        <td>
            <input type="text" name="jsonUrl" style="width: 560px; font-size: 20px" value="D:\Insert\" />
        </td>
        <td>
            D:/temp.json
        </td>
    </tr>
    <tr>
        <td>
            恢复类型:
        </td>
        <td colspan="2">
            <input type="radio" name="type" value="Ins" />
            插入
            <input type="radio" name="type" value="Del" />
            删除
        </td>
    </tr>
    <tr>
        <td colspan="3">
            <input type="button" value="恢复" onclick="recoveryLogInfo();" />
        </td>
    </tr>
</table>
</form>
<br />
<br />
<script type="text/javascript">
    function importInfo() {
        var formdata = $("#importForm").serialize();

        $.ajax({
            url: "/Home/ImportJsonInfoToDataBase",
            type: 'post',
            data: formdata,
            dataType: 'json',
            error: function () {
                alert("未知错误，请联系服务器管理员，或者刷新页面重试");
            },
            success: function (data) {
                if (data.Success == false) {
                    alert(data.Message);
                }
                else {
                    alert("保存成功");
                }
            }
        });
    }

    function recoveryLogInfo() {
        var formdata = $("#recoveryLogForm").serialize();

        $.ajax({
            url: "/Home/RecoveryLogData",
            type: 'post',
            data: formdata,
            dataType: 'json',
            error: function () {
                alert("未知错误，请联系服务器管理员，或者刷新页面重试");
            },
            success: function (data) {
                if (data.Success == false) {
                    alert(data.Message);
                }
                else {
                    alert("保存成功");
                }
            }
        });
    }

</script>
