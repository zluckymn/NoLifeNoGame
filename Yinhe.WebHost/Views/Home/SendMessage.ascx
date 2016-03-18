<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<input type="button" value="发送信息" onclick="sendMessage();" />
<script type="text/javascript">
    function sendMessage() {
        $.ajax({
            url: "/Home/SendMsg",
            type: 'post',
            data: {},
            dataType: 'json',
            error: function () {
                alert("未知错误，请联系服务器管理员，或者刷新页面重试");
            },
            success: function (data) {
                if (data.Success == false) {
                    alert(data.Message);
                }
                else {
                    alert("发送成功");
                }
            }
        });
    }
</script>
