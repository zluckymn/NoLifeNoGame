<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Permissions" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Common" %>
<% 
    //当前用户拥有的所有的查看权限代码
    StringBuilder sb = new StringBuilder();
    var codeRighList = auth.AllRoleRights.Select(s => s.String("code")).Distinct();
    sb.Append("{");
    foreach (var code in codeRighList)
    {
        sb.AppendFormat("\"{0}\":\"{0}\",",code);
    }
    string menuCodes = sb.ToString().TrimEnd(',') + "}";
    bool isShowProjLib = auth.IsShowProjLibMenu();
    bool isShowProj = auth.IsShowProjMenu();
    //string menuCodes = string.Join(",",codeRighList);
%>

<script type="text/javascript">
    var menuCodes = '<%=menuCodes %>';
    var isShowProjLib = '<%=isShowProjLib %>';
    var isShowProj = '<%=isShowProj %>';
</script>
