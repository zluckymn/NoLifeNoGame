<%@ Control Language="C#" Inherits="Yinhe.ProcessingCenter.ViewUserControlBase" %>
<%@ Import Namespace="System.IO" %>
<%
    string A3Dir = PageReq.GetParam("A3Dir");

    if (Directory.Exists(A3Dir))
    {
%>
<input type="hidden" value="<%=A3Dir %>" id="listA3Dir" />

<table border="1">
    <tr>
        <td>
            插件名称
        </td>
        <td>
            最后编译时间
        </td>
        <td>
            操作
        </td>
    </tr>
    <%  foreach (var pluginDir in Directory.GetDirectories(A3Dir))
        {
            if (pluginDir.Replace(A3Dir, "").TrimStart('\\').StartsWith("Plugin_"))
            {
                string pluginName = pluginDir.Replace(A3Dir, "").TrimStart('\\');

                string dllPath = Path.Combine(pluginDir, "bin", pluginName + ".dll");

                FileInfo dllInfo = new FileInfo(dllPath);
    %>
    <tr>
        <td>
            <%=pluginName%>
        </td>
        <td>
            <%=dllInfo.LastWriteTime.ToString()%>
        </td>
        <td>
            <a href="javascript:;" plugindir="<%=pluginDir %>" pluginname="<%=pluginName %>"
                onclick="compiled(this);" class="blue">编译</a> <a href="/Home/DownLoadPluginDLL?pluginDir=<%=pluginDir %>&pluginName=<%=pluginName %>"
                    class="blue">下载</a> <a href="javascript:;" plugindir="<%=pluginDir %>" pluginname="<%=pluginName %>"
                        onclick="coptyDllFile(this);" class="blue">拷贝</a>
        </td>
    </tr>
    <%      }
        }
    %>
</table>
<br />
<table border="1">
    <tr>
        <td colspan="2" align="center">
            发布客户所有插件
        </td>
    </tr>
    <tr>
        <td>
            旭辉(XH)
        </td>
        <td>
            <input type="button" code="XH" onclick="publishClient(this);" value="发布到HOST" />
        </td>
    </tr>
    <tr>
        <td>
            苏宁环球(SNHQ)
        </td>
        <td>
            <input type="button" code="SNHQ" onclick="publishClient(this);" value="发布到HOST" />
        </td>
    </tr>
    <tr>
        <td>
            恒大(HD)
        </td>
        <td>
            <input type="button" code="HD" onclick="publishClient(this);" value="发布到HOST" />
        </td>
    </tr>
</table>
<%  } %>
<script type="text/javascript">
    function compiled(obj) {
        var pluginDir = $(obj).attr("plugindir");
        var pluginName = $(obj).attr("pluginname");

        $.ajax({
            url: "/Home/CompiledPlugin",
            type: 'post',
            data: {
                pluginDir: pluginDir,
                pluginName: pluginName
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
                    alert("编译成功");
                    loadPluginList();
                }
            }
        });
    }

    function coptyDllFile(obj) {
        var pluginDir = $(obj).attr("plugindir");
        var pluginName = $(obj).attr("pluginname");

        $.ajax({
            url: "/Home/CopyPluginDLLToHost",
            type: 'post',
            data: {
                pluginDir: pluginDir,
                pluginName: pluginName
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
                    alert("拷贝成功");
                }
            }
        });
    }

    function publishClient(obj) {
        var A3Dir = $("#listA3Dir").val();
        var clientCode = $(obj).attr("code");
        $.ajax({
            url: "/Home/PublishClientToHost",
            type: 'post',
            data: {
                A3Dir: A3Dir,
                clientCode: clientCode
            },
            error: function () {
                alert("未知错误，请联系服务器管理员，或者刷新页面重试");
            },
            success: function (data) {
                if (data.Success == false) {
                    alert(data.Message);
                }
                else {
                    alert("发布成功");
                }
            }
        });
    }

</script>
