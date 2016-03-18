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
        <td>
            数据:
        </td>
        <td>
            <textarea name="dataJson" style="width: 600px; font-size: 20px"></textarea>
        </td>
        <td>
            数据JSON
        </td>
    </tr>
    <tr>
        <td colspan="3">
            <input type="button" value="保存" onclick="saveInfo();" />
        </td>
    </tr>
</table>
</form>
<script type="text/javascript">
    function saveInfo() {
        var formdata = $("#saveForm").serialize();

        $.ajax({
            url: "/Home/UniversalSave",
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
