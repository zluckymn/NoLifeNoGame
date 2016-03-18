<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title></title>
    <script src="/Scripts/Reference/Common/jquery-1.5.1.js" type="text/javascript"></script>
    <link href="/Content/css/client/FLKL/admincss.css" rel="stylesheet" type="text/css" />
    <style>
    .sign_left .sign_logo{ width:357px; height:94px; background:url(/Content/images/client/FLKL/sign_logo.png) no-repeat;}
    </style>
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




         <div id="logo">
            <img src="/Content/images/client/FLKL/admin/logo.png" alt="" />
        </div>
<div id="loginbox"> 
    <form id="loginfrm" action="" method="post" class="form-vertical" >

    <p><b>后台管理登录</b></p>
                <div class="ts_box_txt" id="tdMsg">
                                    <%=strMsg%></div>
          <div class="control-group">
                    <div class="controls">
                        <div class="input-prepend">
                            <span class="add-on"><span class="icon-user">用户名：</span></span><input type="text" id="userName" name="userName" value="" class="login_input" />
                        </div>
                    </div>
                </div>

                <div class="control-group">
                    <div class="controls">
                        <div class="input-prepend">
                            <span class="add-on"><span class="icon-lock">密码：</span></span><input type="password" id="passWord" name="password" class="login_input"  value=""
                                      onkeydown="if(event.keyCode==13){checkForm();}" />
                        </div>
                    </div>
                </div>
                <div class="form-actions">
                    <span class="pull-left"> </span>
                    <span class="pull-right"> <a href="javascript:void(0);" class="sign_in_button" onclick="checkForm();">登录</a>
                    </span>
                </div>
                  
                               
                              
                           
 



    </form>
    </div>
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
