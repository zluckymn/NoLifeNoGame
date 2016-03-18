<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <script src="/Scripts/Reference/Common/jquery-1.5.1.js" type="text/javascript"></script>
    <link href="/Content/css/client/xuhui/xuhui.css" rel="stylesheet" type="text/css" />
    <link href="/Content/css/common/common.css" rel="stylesheet" type="text/css" />
</head>
<body class="body_sign">
    <% 
        string ReturnUrl = PageReq.GetParam("ReturnUrl");

        string MsgType = PageReq.GetSession("MsgType");
        string userName = "";
        string strMsg = "";
        switch (MsgType)
        {
            case "":
                break;
            case "username":
                userName = PageReq.GetSession("UserName");
                strMsg = "用户名不正确";
                break;
            case "password":
                userName = PageReq.GetSession("UserName");
                strMsg = "密码不正确";
                break;
            case "noAdmin":
                userName = PageReq.GetSession("UserName");
                strMsg = "网站维护中，暂时不能登陆";
                break;
            case "validateCode":
                userName = PageReq.GetSession("UserName");
                strMsg = "验证码不对";
                break;
            case "isActive":
                userName = PageReq.GetSession("UserName");
                strMsg = "用户未激活";
                break;


        }
    %>
    <form id="loginfrm" action="" method="post">
    <div class="body_bg01">
        <div class="body_bg02">
            <div class="sign_in_box">
                <div class="sign_left">
                    <div class="sign_logo">
                    </div>
                </div>
                <div class="sign_right">
                    <table>
                        <tr>
                            <td>
                                <span style="font-size: 12px;">&nbsp;</span>
                            </td>
                            <td>
                                <div class="ts_box_txt" id="tdMsg">
                                    <%=strMsg%></div>
                            </td>
                            <td>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                账&nbsp;&nbsp;号：
                            </td>
                            <td>
                                <div class="user_name1">
                                    <input type="text" class="sign_in_inp" id="userName" name="userName" value="" /></div>
                            </td>
                            <td>
                                <%--<div class="ts_box_txt">
                                    账号不能为空！</div>--%>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                密&nbsp;&nbsp;码：
                            </td>
                            <td>
                                <div class="user_key">
                                    <input type="password" class="sign_in_inp" id="passWord" name="password" value=""
                                        onkeydown="if(event.keyCode==13){checkForm();}" /></div>
                            </td>
                            <td>
                                <%--<div class="ts_box_txt">
                                    密码不能为空！</div>--%>
                            </td>
                        </tr>
                        <tr>
                            <td>
                            </td>
                            <td>
                                <input type="checkbox" name="rememberMe" id="rememberMe" style="vertical-align: middle" /><span
                                    class="ts_keep_txt">7天内保持登录</span>
                            </td>
                            <td>
                            </td>
                        </tr>
                        <tr>
                            <td>
                            </td>
                            <td>
                                <a href="javascript:void(0);" class="sign_in_button" onclick="checkForm();"></a>
                                <a href="javascript:void(0);" class="fet_key">忘记密码？</a>
                            </td>
                            <td>
                            </td>
                        </tr>
                    </table>
                </div>
                <div class="clear">
                </div>
            </div>
        </div>
        <div id="sign_footer">
            <p class="copyright">
                ©银禾公司版权所有.ALL RIGHTS RESERVED</p>
        </div>
    </div>
    <div class="body_bg03">
    </div>
    </form>
    <script type="text/javascript">
        function checkForm() {
            var url = "/Account/AjaxLogin";
            var uname = $.trim($("#userName").val()),
                upw = $("#passWord").val();
            if (uname == "") {
                $("#tdMsg").html("请填写用户名");
                return false;
            }
            if (upw == "") {
                $("#tdMsg").html("请填写密码");
                return false;
            }

            var formData = $("#loginfrm").serialize();
            $.ajax({
                url: url,
                type: 'post',
                data: formData + '&ReturnUrl=' + escape('<%=ReturnUrl %>'),
                dataType: 'json',
                error: function () {
                    alert("登录失败");
                },
                success: function (data) {
                    if (data.Success == false) {
                        alert('登录失败 ' + data.Message);
                        window.setTimeout('window.location.reload();', 1);
                    }
                    else {
                        window.setTimeout(function () {
                            top.location = data.htInfo.ReturnUrl;
                        }, 1);
                    }
                }
            });
        }
    
    </script>
</body>
</html>
