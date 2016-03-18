<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>

<%
    var dataOp = new DataOperation();

    var fileRelList = dataOp.FindAll("FileRelation").ToList();

    var startTime = PageReq.GetParam("startTime");
    var endTime = PageReq.GetParam("endTime");

    if (startTime != "")
        fileRelList = fileRelList.Where(t => t.Date("createDate") >= Convert.ToDateTime(startTime)).ToList();
    if (endTime != "")
        fileRelList = fileRelList.Where(t => t.Date("createDate") <= Convert.ToDateTime(endTime)).ToList();
    %>
    <table>
        <tr>
            <td>
                开始时间
            </td>
            <td>
                <input type="text" id="startTime" value="<%=startTime %>" readonly="readonly" onclick="WdatePicker();"/>
            </td>
            <td>
                结束时间
            </td>
            <td>
                <input type="text" id="endTime" value="<%=endTime %>" onclick="WdatePicker();" readonly="readonly"/>
            </td>
            <td>
                <input type="button" onclick="searthFiles();" value="确定"/>
            </td>
        </tr>
    </table>
    <div style="color:Gray">
        查询条件：<%=startTime==""?DateTime.MinValue.ToString("yyyy-MM-dd"):startTime %> &lt 创建时间 &lt <%=endTime==""?DateTime.Now.ToString("yyyy-MM-dd"):endTime %> 
    </div>
    <table border="1" width="80%">
        <tr>
            <td>
                表名（tableName）
            </td>
            <td>
                主键名（keyName）
            </td>
            <td>
                主键值（keyValue）
            </td>
            <td>
                无效文件个数
            </td>
            <td>
                操作
            </td>
        </tr>
    <%
        var totalCount = 0;
        foreach (var tableName in fileRelList.Select(t => t.String("tableName")).Distinct())
        {
            var hitFileRelList = fileRelList.Where(t => t.String("tableName") == tableName).ToList();
            var keyNames = hitFileRelList.Select(t => t.String("keyName")).Distinct().ToList();
            List<String> allResultKeyValue = new List<String>();
            if (tableName != "")
            {
                foreach (var kName in keyNames)
                {
                    var index = 0;
                    allResultKeyValue = dataOp.FindAll(tableName).Select(t => t.String(kName)).ToList();

                    var allKeyValues = hitFileRelList.Where(t => t.String("keyName") == kName && allResultKeyValue.Contains(t.String("keyValue")) == false).Select(t => t.String("keyValue")).Distinct().ToList();
                    foreach (var keyValue in allKeyValues)
                    {
                        var hitFileList = hitFileRelList.Where(t => t.String("keyName") == kName && t.String("keyValue") == keyValue).ToList();
                        var fileRelIds = string.Join(",", hitFileList.Select(t => t.String("fileRelId")));
                        var hitFileCount = hitFileList.Count;
                        totalCount += hitFileCount;
     %>
     <tr>
     <%if (index++ == 0)
       { %>
        <td rowspan="<%=allKeyValues.Count() %>" style="vertical-align:top">
            <%=tableName%>
        </td>
        <td rowspan="<%=allKeyValues.Count() %>" style="vertical-align:top">
            <%=kName%>
        </td>
     <%} %>
        <td>
            <%=keyValue%>
        </td>
        <td>
            <%=hitFileCount%>
        </td>
        <td>
            <input type="button" value="删除" onclick="deleteFiles(this)" fileRelIds="<%=fileRelIds %>" />
        </td>
     </tr>
     <%}
                }
            }
            else
            {
                var index = 0;
                var hitFileList  = hitFileRelList.Where(t => t.String("tableName") == "").ToList();
                var allKeyValues = hitFileList.Select(t => t.String("keyValue")).Distinct().ToList();
                var kName = hitFileList.Select(t => t.String("keyName")).Distinct().ToList();
                foreach (var keyValue in allKeyValues)
                {
                    var hitValueFiles = hitFileList.Where(t => t.String("keyValue") == keyValue).ToList();
                    var fileRelIds =string.Join(",", hitValueFiles.Select(t => t.String("fileRelId")).ToList());
                    var hitFileCount = hitValueFiles.Count;
                %>
            <tr>
     <%if (index++ == 0)
       { %>
        <td rowspan="<%=allKeyValues.Count() %>" style="vertical-align:top">
            <%=tableName%>
        </td>
        <td rowspan="<%=allKeyValues.Count() %>" style="vertical-align:top">
            <%=string.Join(",",kName)%>
        </td>
     <%} %>
        <td>
            <%=keyValue%>
        </td>
        <td>
            <%=hitFileCount%>
        </td>
        <td>
            <input type="button" value="删除" onclick="deleteFiles(this)" fileRelIds="<%=fileRelIds %>" />
        </td>
     </tr>
           <%}
            }
        }%>
    </table>
    <div>
        <span>全部冗余文件数：<%=totalCount %></span>
    </div>
   
    <script type="text/javascript">
        function searthFiles() {
            var startTime = $("#startTime").val();
            var endTime = $("#endTime").val();
            if (startTime != "" && endTime != "") {
                if (startTime > endTime) {
                    alert("开始时间大于结束时间，请重新选择");
                    return false;
                }
            }
            var url = "/Home/FileRelRedundancy?startTime=" + startTime + "&endTime=" + endTime + "&r=" + Math.random();
            $('#adminOperaDiv').load(url);
        }

        function deleteFiles(obj) {
            var fileRelIds = $(obj).attr("fileRelIds");

            $.ajax({
                url: "/Home/DeleFiles",
                type: 'post',
                data: {
                    delFileRelIds: fileRelIds
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
                        alert("删除成功");
                        $('#adminOperaDiv').load('/Home/FileRelRedundancy?startTime=<%=startTime %>&endTime=<%=endTime %>&r=' + Math.random());
                    }
                }
            });

        }
    </script>