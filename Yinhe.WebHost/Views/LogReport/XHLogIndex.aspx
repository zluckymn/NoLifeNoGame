<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="Yinhe.ProcessingCenter.ViewPageBase" %>

<%@ Import Namespace="MongoDB.Driver.Builders" %>
<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="HeadContent" runat="server">
    <link href="/Content/css/common/common.css" rel="stylesheet" type="text/css" />
    <script src="/Scripts/Reference/Common/jquery-1.5.1.js" type="text/javascript"></script>
    <script src="/Scripts/Modules/Common/YH.js" type="text/javascript"></script>
    <script src="/Scripts/Modules/Common/jquery.bgiframe.js" type="text/javascript"></script>
    <script src="/Scripts/Modules/Common/popbox.js" type="text/javascript"></script>
    <link href="/Content/css/common/common.css" rel="stylesheet" type="text/css" />
    <link href="/Content/css/client/xuhui/xuhui.css" rel="stylesheet" />
    <link href="/Content/css/client/xuhui/designManagement.css" rel="stylesheet" type="text/css" />
    <script type="text/javascript" src="/Scripts/Modules/Common/YHLoader.js?m=pagination"></script>
    <script src="/Scripts/Modules/Common/Yinhoo.SelelctTree.js" type="text/javascript"></script>
    <script src="/Scripts/Modules/Common/SelectUsers.js" type="text/javascript"></script>
    <script src="/Scripts/Modules/planworktask/pwt.js" type="text/javascript"></script>
    <script src="/Scripts/Reference/datePicker/WdatePicker.js" type="text/javascript"></script>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="MainContent" runat="server">
    <% 
        List<BsonDocument> resolveTypeList = dataOp.FindAll("SysLogResolve").ToList();

        List<BsonDocument> userList = dataOp.FindAll("SysUser").ToList();
    %>
    <style>
        .selectLi li.this
        {
            background: rgb(119, 169, 222);
            color: #fff;
            padding: 2px 4px 4px 2px;
        }
        .selectLi li
        {
            float: left;
            margin-left: 5px;
            margin-right: 5px;
            padding: 2px 4px 4px 2px;
            margin-bottom: 10px;
            margin-top: 10px;
          
            white-space: nowrap;
        }
        .selectLi ul
        {
            width:100%;
            }
    </style>
    <div class="con_top">
        <div class="path_nav">
            &nbsp;&nbsp;系统日志管理
        </div>
        <div class="right_box">
        </div>
    </div>
    <div class="content">
        <!-------邪恶的分隔线------->
        <div class="screening01">
            <table width="100%">
                <tr>
                    <td style="width: 70px;">
                        操作用户：
                    </td>
                    <td>
                        <input type="text" name="user" id="userName" value="" onclick='SelectUser();' />
                        <input type="hidden" name="user" id="userId" value="" />
                    </td>
                </tr>
                <tr>
                    <td>
                        操作时间：
                    </td>
                    <td>
                        <input class="inputborder" id="stStr" onclick="WdatePicker()" type="text" name="stStr"
                            readonly="readonly" value="" />
                        &nbsp; - &nbsp;
                        <input class="inputborder" onclick="WdatePicker()" type="text" name="stStr" readonly="readonly"
                            id="edStr" value="" />
                    </td>
                </tr>
                <tr>
                    <td>
                        操作类型：
                    </td>
                    <td>
                        <div class="selectLi">
                            <ul>
                                <%  foreach (var tempResolve in resolveTypeList)
                                    {  
                                        
                                %>
                                <li resolveid="<%=tempResolve.Int("resolveId")  %>">
                                    <%=tempResolve.String("name")%></li>
                                <%  } %>
                            </ul>
                        </div>
                    </td>
                </tr>
            </table>
            <div class="screen_bottom">
                <a href="javascript:void(0);" onclick="showLogList(1);" class="btn_06">确 定<span></span></a>
                <a href="/Supplier/Designsupplier" class="btn_06" onclick="showLogList(2);">重 置<span></span></a>
            </div>
        </div>
        <br />
        <div id="logInfoListDiv">
        </div>
        <!-------邪恶的分隔线------->
    </div>
    <script type="text/javascript">
        $("div.selectLi").find("li").each(function () {
            $(this).click(function () {

                if ($(this).attr("class") == "this") {
                    $(this).removeClass("this");
                } else {
                    $(this).addClass("this");
                }
            });
        });

        function showLogList(type) {
            var userId = $("#userId").val();
            var stStr = $("#stStr").val();
            var edStr = $("#edStr").val();
            var resolveIds = "";
            if (type == 1) {
                $("div.selectLi").find("li.this").each(function () {
                    resolveIds += $(this).attr("resolveid") + ",";
                })
            } else {
                $("div.selectLi").find("li.this").each(function () {
                    $(this).removeClass("this");
                });
            }

            $("#logInfoListDiv").load("/LogReport/LogInfoList?userId=" + userId + "&stStr=" + stStr + "&edStr=" + edStr + "&resolveIds=" + resolveIds + "&r=" + Math.random());
            //window.open("/LogReport/LogInfoList?userId=" + userId + "&stStr=" + stStr + "&edStr=" + edStr + "&resolveIds=" + resolveIds + "&r=" + Math.random());
        }

        function SelectUser(flowPosId, names, ids) {
            var name = "";
            var id = "";
            SelectUsers.init({
                single: true,
                multiSel: false,
                rType: "cb",
                onOpen: function (o) {
                    SelTree();
                    setFrameSelAddDefault(id, name);
                },
                callback: function (rs) {
                    for (var key in rs) {
                        name += (rs[key]);
                        id += (key + ",");
                    }
                    $("#userName").val(name);
                    $("#userId").val(id);
                }
            });
        }

        //设置已选人员框的默认值
        function setFrameSelAddDefault(defValue, defTexts) {
            if (defValue == null) return;
            if (defValue.length <= 0)
                return;
            var values = defValue.split(",");
            var texts = defTexts.split(",");
            for (var i = 0; i < values.length; i++) {
                $("#selAdd").append("<option value='" + values[i] + "'>" + texts[i] + "</option>");
            }
        }
    </script>
</asp:Content>
