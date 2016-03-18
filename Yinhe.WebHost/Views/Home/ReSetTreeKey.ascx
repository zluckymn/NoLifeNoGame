<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<form id="resetTreeForm" action="" method="post">
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
        <td colspan="3">
            <input type="button" value="保存" onclick="resetTree();" />
        </td>
    </tr>
</table>
</form>
<script type="text/javascript">
    function resetTree() {

        var tbName = $("#resetTreeForm").find("input[name=tbName]").val();

        $.ajax({
            url: "/Home/ReSetTableTreeKey",
            type: 'post',
            data: {
                tbName: tbName
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
                    alert("重置成功");
                }
            }
        });
    }
</script>
