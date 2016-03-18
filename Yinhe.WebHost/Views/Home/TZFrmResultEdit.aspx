<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Client.Master" Inherits="Yinhe.ProcessingCenter.ViewPageBase" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Permissions" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Common" %>
<%@ Import Namespace="Yinhe.ProcessingCenter.Document" %>
<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="HeadContent" runat="server">
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Reference/Common/jquery-1.5.1.js"
    type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/mt.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/FileCommon.js" type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/Scripts/Modules/YHFlexPaper/js/swfobject/swfobject.js"
    type="text/javascript"></script>
<script src="<%=SysAppConfig.HostDomain %>/webMT/js/cwf.js" type="text/javascript"></script>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
<%
    string uuid = Guid.NewGuid().ToString();
    string nodeId = PageReq.GetParam("nodeId");     //当前节点
    string nodePid = PageReq.GetParam("nodePid");   //父节点Id       //这个改为在页面中自己获取
    if (nodeId == "undefine") nodeId = string.Empty;
    if (nodePid == "undefine") nodePid = string.Empty;
    //string typeObjId = PageReq.GetParam("typeObjId");   //类型对象
    string typeObjId = "7";

    List<BsonDocument> projectList = dataOp.FindAllByKeyVal("ProjectNode", "typeObjId", "4").ToList();

    BsonDocument entity = new BsonDocument();
    string updateQuery = "";
    string tbName = "ProjectNode";

    BsonDocument parent = dataOp.FindOneByKeyVal(tbName, "nodeId", nodePid);    //父节点

    BsonDocument curTypeObj = dataOp.FindOneByKeyVal("ProjTypeObj", "typeObjId", typeObjId);   //当前节点类型

    List<BsonDocument> attrObjList = dataOp.FindAllByKeyVal("ProjAttrObj", "typeObjId", typeObjId).ToList(); //可用属性类型

    string masterFile = PageReq.GetParam("mf");
    string secFile = PageReq.GetParam("sf");
    masterFile = @"e:\\201210284758-1.dwg";
    secFile = @"e:\\201210284758T3-1.dwg";
%>
<div class="content">
 <form id="myForm">
    <input type="hidden" id="nodeId" name="nodeId" value="" />
    <input type="hidden" name="tbName" value="<%=tbName %>" />
<%--    <input type="hidden" name="queryStr" value="<%=updateQuery %>" />--%>
    <input type="hidden" name="nodePid" id="nodePid" value="<%=nodePid %>" />
    <input type="hidden" name="typeObjId" value="<%=typeObjId %>" />
    <table class="form_table">
        <tr>
            <td>
                项目名
            </td>
            <td style="color: #bf2f28">
                <select id="projSelect" onchange="projSelectChangeHandle(this)">
                    <option class="projOption" value="0" <%if(nodeId == "0" ){%> selected="selected"<%;}%> >--请选择项目--</option>
                    <%foreach (BsonDocument proj in projectList)
                      { 
                    %><option <%if(nodeId == proj.String("nodeId") ){%> selected="selected" <%}%> class="projOption"
                        value="<%=proj.String("nodeId") %>"><%=proj.String("name") %></option>
                    <%
                  }%>
                </select>
            </td>
            <td>
                成果分类
            </td>
            <td style="color: #bf2f28">
                <select id="selCat" onchange="changeCategory(this)">
                    <option class="projOption" value="0" <%if(nodeId == "0" ){%> selected="selected"<%;}%> >--请选择分类--</option>
                </select>
            </td>
        </tr>
        <tr>
            <td>
                名称:
            </td>
            <td>
                <input type="text" name="name" id="nodeNameSel" value="<%=entity.String("name") %>" class="inputborder" />
            </td>
        </tr>
        <tr id="box_propType" <%if (attrObjList.Count <= 0) { %> style="display: none;" <%} %>>
            <td>
                属性类型:
            </td>
            <td>
                <select name="attrObjId" onchange="changeAttrType(this);">
                    <option value="0">无属性类型</option>
                    <%  foreach (var attrObj in attrObjList.OrderBy(t => t.String("nodeKey")))
                        { %>
                    <option value="<%=attrObj.Int("attrObjId") %>" <%if (entity.Int("attrObjId") == attrObj.Int("attrObjId")){ %>
                        selected="selected" <%} %>>
                        <%=attrObj.String("name")%>
                    </option>
                    <%  } %>
                </select>
            </td>
        </tr>
    </table>
    <div id="nodeAttrEditDiv" style="height: 360px; overflow: auto; overflow-x: hidden; padding: 10px;">
    </div>
        <input type="hidden" id="hidMasterFile" name="hidMasterFile" value="<%=masterFile %>" />
    <input type="hidden" id="hidSecFile" name="hidSecFile" value="<%=secFile %>" />
        <input type="hidden" name="queryStr" value="" />
        <input type="hidden" id="fileTypeId" name="fileTypeId" value="0" />
        <input type="hidden" id="fileObjId" name="fileObjId" value="54" />
        <input type="hidden" id="uploadType" name="uploadType" value="2" />
        <input type="hidden" id="tableName" name="tableName" value="ProjectNode" />
        <input type="hidden" id="keyName" name="keyName" value="nodeId" />
        <input type="hidden" id="fileSaveType" name="fileSaveType" value="multiply" />
        <input type="hidden" id="delFileRelIds" name="delFileRelIds" value="" />

        <input type="hidden" id="keyValue" name="keyValue" value="0" />
        <input type="hidden" id="uploadFileList" name="uploadFileList" />


    </form>
    <input type="button" value="提交" onclick="saveInfo()" />
</div>

<script type="text/javascript" language="javascript">
    function projSelectChangeHandle(obj) {
        //项目选择处理函数
//        var nodeId = $("option:selected").attr("nodeId");
//        var nodePid = $("#nodePid").attr("value");
//        window.location.href = "/Client/TZFrmResultEdit?nodeId=" + nodeId + "&nodePid=" + nodePid;
        var projId = $(obj).val();
        $("#nodeId").val($(obj).val());

        
        $("#selCat").html("<option class=\"projOption\" value=\"0\" selected=\"selected\">--请选择分类--</option>");

        $.get("/Client/GetProjNodeTreeJson?tbName=ProjectNode&curNodeId=" + projId + "&typeObjId=5&itself=0",
        function (data) {
            $(data).each(function (i, d) {
                $("#selCat").append("<option value='" + d.id + "'>" + d.name + "</option");
            });
        });
    }
    function changeAttrType(obj) {
        var attrObjId = $(obj).val();
        $("#nodeAttrEditDiv").load("/Client/NodeAttrEdit?nodeId=<%=nodeId %>&attrObjId=" + attrObjId + "&r=" + Math.random());
    }

    function changeCategory(obj) {
        $("#nodePid").val($(obj).val());
    }
    function saveInfo(obj) {
        var NewUserFilePath = "";
        var splitStr = "|H|";
        NewUserFilePath += $("#hidMasterFile").val();
        //NewUserFilePath += splitStr + $("#hidSecFile").val();
        $("#uploadFileList").val(NewUserFilePath);
        var formdata = $("#myForm").serialize();

        $.ajax({
            url: "/Client/SavePostInfo",
            type: 'post',
            data: formdata,
            dataType: 'json',
            error: function () {
                $.tmsg("m_jfw", "未知错误，请联系服务器管理员，或者刷新页面重试", { infotype: 2 });
            },
            success: function (data) {
                alert(data.FileInfo);
                var str = data.FileInfo.split("|");
                var result = eval("(" + str[0] + ")");
                var fileIdList = "";
                if (result.success) {
                    if (str.length > 1) {
                        if (str[1] != "") {
                            alert(str[1]);
                            var files = eval("(" + str[1] + ")");
                            var uuid = "<%=uuid %>";
                            if (files.length > 0) {
                                var jsonStr = "";
                                $(files).each(function (i, f) {
                                    var str = "{ \"nativePath\": \"" + f.path + "\", \"args\": \"" + f.strParam + "\",\"FileTypeTag\":\"0\",\"GroupFileTag\": \"" + uuid + "\"}";
                                    if (jsonStr == "") {
                                        jsonStr = str;
                                    } else {
                                        jsonStr += "," + str;
                                    }
                                })
                                alert(jsonStr);
                                if (jsonStr == "") {
                                    jsonStr = "{ \"nativePath\": \"" + $("#hidSecFile").val() + "\", \"args\": \"sysObject@0-0-0\",\"FileTypeTag\":\"1\",\"GroupFileTag\": \"" + uuid + "\"}"; ;
                                } else {
                                    jsonStr += "," + "{ \"nativePath\": \"" + $("#hidSecFile").val() + "\", \"args\": \"sysObject@0-0-0\",\"FileTypeTag\":\"1\",\"GroupFileTag\": \"" + uuid + "\"}";
                                }
                                jsonStr = "[" + jsonStr + "]";
                                alert(jsonStr);
                                cwf.webc.jsCallflashToAir(jsonStr);
                                alert("文件已加入上传队列");
                            }
                            //cwf.tasks.send(uuid, files, function () {
                            //    alert("文件已加入上传队列");

                            // });

                        }
                    }
                }
            }
        });
    }
    $(document).ready(function () {
        cwf.view.dlgHelp.init();
    });
</script>
</asp:Content>
