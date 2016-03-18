<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<style type="text/css">
    .sign_in_box .user_name1, .sign_in_box .user_key
    {
        width: 187px;
        height: 41px;
        background: url(../../content/images/client/xuhui/user_info_bg.png) no-repeat;
    }
    .sign_in_box .user_key
    {
        background-position: left -41px;
    }
    .sign_in_box .user_name1
    {
        background-position: left 0px;
    }
    .sign_in_box
    {
        width: auto;
        background-image: none;
        padding: 30px 30px;
        margin: 5px;
        background-color: #fff;
        border: 1px solid #ccc;
        position: relative;
    }
    .sign_in_box tr td
    {
    }
    .sign_in_box h1
    {
        border-bottom: 1px solid #ccc;
        height: 35px;
        line-height: 35px;
        font-size: 18px;
        font-weight: normal;
        color: #404040;
        font-family: "Microsoft YaHei" ,微软雅黑, "Microsoft JhengHei" ,华文细黑,STHeiti,MingLiu;
        margin-bottom: 12px;
    }
    .sign_in_box .close
    {
        background: url(../../content/images/icon/ico-close.png) no-repeat;
        position: absolute;
        right: -6px;
        top: -6px;
        width: 15px;
        height: 15px;
        display: block;
        cursor: pointer;
    }
</style>
<form id="loginfrm" action="" method="post">
<div class="sign_in_box">
    <a class="close" title="关闭"></a>
    <table>
        <tr>
            <td>
                <div class="user_name1">
                    <input type="text" class="sign_in_inp" id="userName" name="userName" value="" /></div>
            </td>
        </tr>
        <tr>
            <td>
                <div class="user_key">
                    <input type="password" class="sign_in_inp" id="passWord" name="password" value=""
                        onkeydown="if(event.keyCode==13){checkForm();}" /></div>
            </td>
        </tr>
        <tr>
            <td style="padding: 4px 15px 4px 5px; text-align:center">
                <a style="padding: 8px 30px" class="N-btn0002" onclick="checkForm();" href="javascript:;">
                    LOGIN</a> <a class="" href="javascript:;"></a>
            </td>
        </tr>
    </table>
</div>
</form>
