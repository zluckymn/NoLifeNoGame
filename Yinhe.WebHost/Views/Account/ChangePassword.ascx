<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>

<form id="changeForm" method="post">
<table>
    <tr>
        <td>旧密码：</td>
        <td><input type="password" name="oldPassword" class="required" /></td>
    </tr>
     <tr>
        <td>新密码：</td>
        <td><input type="password" id="newPassword" name="newPassword" class="required" /></td>
    </tr>
     <tr>
        <td>新密码：</td>
        <td><input type="password" id="verify" class="required" /></td>
    </tr>
</table>

</form>
<script type="text/javascript">
  
</script>

