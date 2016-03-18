<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage" %>

<%@ Import Namespace=" MongoDB.Bson.IO" %>
<%@ Import Namespace="MongoDB.Driver.Builders" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <title></title>
    <link href="/Content/css/common/common.css" rel="stylesheet" type="text/css" />
    <script src="/Scripts/Reference/Common/jquery-1.5.1.js" type="text/javascript"></script>
    <script src="/Scripts/Modules/Common/YH.js" type="text/javascript"></script>
    <script src="/Scripts/Modules/Common/jquery.bgiframe.js" type="text/javascript"></script>
    <script src="/Scripts/Modules/Common/popbox.js" type="text/javascript"></script>
    <link href="/Content/css/common/common.css" rel="stylesheet" type="text/css" />
    <link href="/Content/css/client/xuhui/designManagement.css" rel="stylesheet" type="text/css" />

    <script src="../../Scripts/Reference/datePicker/WdatePicker.js" type="text/javascript"></script>
</head>
<style>
    .divselect
    {
        color: #193b5f;
        line-height: 26px;
        text-indent: 0px;
        padding-right: 8px;
        padding-left: 8px;
        border-top-color: #cbdcf4;
        border-right-color: #cbdcf4;
        border-bottom-color: #ffffff;
        border-left-color: #cbdcf4;
        border-width: 1px;
        border-style: solid;
        float: left;
        background-color: rgb(255,255,255);
        font-weight: bold;
    }
    .div
    {
        color: #193b5f;
        line-height: 28px;
        text-indent: 0px;
        padding-right: 8px;
        padding-left: 8px;
        float: left;
    }
    .div01 img
    {
        vertical-align: middle;
        text-align: center;
    }
    .box_tit{ padding:0px;}
    #container{ width:1000px}
</style>
<body>
    <% 
        // string WorkPlanManageConnectionString = System.Web.Configuration.WebConfigurationManager.AppSettings["WorkPlanManageConnectionString"];
        //var dataop = new DataOperation(WorkPlanManageConnectionString,true );
        
        //Yinhe.ProcessingCenter.DesignManage.TaskFormula.DesignManage_PlanBll planBll = Yinhe.ProcessingCenter.DesignManage.TaskFormula.DesignManage_PlanBll._(dataop);
        //Yinhe.ProcessingCenter.Administration.SysUserBll userBll = Yinhe.ProcessingCenter.Administration.SysUserBll._(dataop);
        //var curUser = userBll.FindUsersByWeiXin("o07hUuCIEplPWBota64HbLRW0hZk");

        //var taskList = planBll.GetUserTaskList(curUser.Text("userId"));
        //var delayDoingTaskList = taskList.Where(t => t.Int("status", 2) <= (int)TaskStatus.NotStarted && t.String("curStartData") != "" && t.Date("curStartData") < DateTime.Now).ToList();
        ////延迟结束的任务
        //var delayDoneTaskList = taskList.Where(t => t.Int("status", 2) == (int)TaskStatus.Processing && t.Int("status", 2) != (int)TaskStatus.Completed && t.String("curEndData") != "" && t.Date("curEndData") < DateTime.Now).ToList();
                     
        //var result = new StringBuilder();
        //if (delayDoingTaskList.Count() != 0 || delayDoneTaskList.Count() != 0)
        //{
        //    if (delayDoingTaskList.Count() != 0)
        //    {
        //        result.AppendFormat("您有以下延迟开始的任务\r\n");
        //        foreach (var delayTask in delayDoingTaskList)
        //        {
        //            result.AppendFormat("【{0}】\r\n", delayTask.Text("name"));
        //        }
        //    }
        //    if (delayDoneTaskList.Count() != 0)
        //    {
        //        result.AppendFormat("您有以下延迟完成的任务\r\n");
        //        foreach (var delayTask in delayDoneTaskList)
        //        {
        //            result.AppendFormat("【{0}】\r\n", delayTask.Text("name"));
        //        }
        //    }
        //}
   
        var curDir = new System.IO.DirectoryInfo(Server.MapPath(""));
        var plugPath = @"D:\Projects\A3";
        if (curDir != null && curDir.Parent != null && curDir.Parent.Parent != null)
            plugPath = curDir.Parent.Parent.FullName;
            
        
    %>
    <%----%>
    <form id="submitForm" enctype="multipart/form-data" action="/Home/TestGetFile" method="post"
    onsubmit="return submitFile();">
    <table class="tablesorter" cellspacing="0">
        <tr>
            <td>
                <input type="file" name="upfile" value="浏览文件">
            </td>
        </tr>
        <tr>
            <td>
                <input type="submit" name="upgrade" value="确定" class="alt_btn">
            </td>
        </tr>
    </table>
    </form>
    <script type="text/javascript">
        function submitFile() {
            $("#submitForm").ajaxSubmit({
                type: 'post',
                dataType: "dataJson",
                url: "/Home/TestGetFile",
                success: function (data) {
                    alert("success");
                    alert(data);
                },
                error: function (XmlHttpRequest, textStatus, errorThrown) {
                    alert("error");
                }
            });
            return false;
        }

        //        function operate2() {

        //            // jquery 表单提交
        //            $("#submitForm").ajaxSubmit(function (message) {
        //                // 对于表单提交成功后处理，message为提交页面operation.htm的返回内容
        //                alert(message.toString());
        //            });

        //            return false; // 必须返回false，否则表单会自己再做一次提交操作，并且页面跳转
        //        }
    </script>
    <div id="container">
        <div class="div01" style="margin-left: 13px; margin-top: 40px; margin-bottom: 15px;">
            <h2>
                <img src="../../Content/images/style/arro0003.gif" />
                当前连接数据库:
                <%=SysAppConfig.DataBaseConnectionString %></h2>
        </div>
        <hr />
        <div style="margin-left: 14px; margin-top: 18px; margin-bottom: 15px; font-weight: bold;
            font-size: 17px">
            <img src="../../Content/images/icon/lightbulb.png" />&nbsp;发布插件:&nbsp;<input type="text"
                style="width: 500px; font-size: 15px" name="A3Dir" id="A3Dir" value="<%=plugPath %>" />&nbsp;&nbsp;<input
                    type="button" value="读取" onclick="loadPluginList();" />
            <script type="text/javascript">

                function loadPluginList() {
                    var A3Dir = $("#A3Dir").val();
                    $("#adminOperaDiv").load("/Home/PublishPlugins?A3Dir=" + A3Dir + "&r=" + Math.random());
                }
            </script>
        </div>
        <hr />
        <div>
            <div class="div01" style="margin-left: 15px; margin-bottom: 5px; margin-top: 15px;">
                <h2>
                    <img src="../../Content/images/icon/28.png" />
                    管理员操作</h2>
            </div>
            <div style="border-bottom: 2px solid #2245a9; margin-bottom: 10px;">
            </div>
            <div class=" box_tit CJJ">
                <div class="divselect">
                    <a class=" blue02" href="javascript:;" onclick="$('#adminOperaDiv').load('/Home/DataEdit?r' + Math.random());">
                        数据修改</a></div>
                <div class="div">
                    <a class=" blue02" href="javascript:;" onclick="$('#adminOperaDiv').load('/Home/DataDelete?r' + Math.random());">
                        数据删除</a>
                </div>
                <div class="div">
                    <a class=" blue02" href="javascript:;" onclick="$('#adminOperaDiv').load('/Home/DataExport?r' + Math.random());">
                        数据导出</a>
                </div>
                <div class="div">
                    <a class=" blue02" href="javascript:;" onclick="$('#adminOperaDiv').load('/Home/DataImport?r' + Math.random());">
                        数据导入</a>
                </div>
                <div class="div">
                    <a class=" blue02" href="javascript:;" onclick="$('#adminOperaDiv').load('/Home/ReSetTreeKey?r' + Math.random());">
                        重置树形表关键字</a>
                </div>
                <div class="div">
                    <a class=" blue02" href="javascript:;" onclick="$('#adminOperaDiv').load('/Home/SendMessage?r' + Math.random());">
                        消息发送</a>
                </div>
                <div class="div">
                    <a class=" blue02" href="javascript:;" onclick="$('#adminOperaDiv').load('/Home/DataToString?r' + Math.random());">
                        重置表数据为字符串</a>
                </div>
                <div class="div">
                    <a class=" blue02" href="javascript:;" onclick="$('#adminOperaDiv').load('/Home/DataStatistics?r' + Math.random());">
                        数据统计</a>
                </div>
                <div class="div">
                    <a class=" blue02" href="javascript:;" onclick="$('#adminOperaDiv').load('/Home/ReSetLogTime?r' + Math.random());">
                        重置日志时间</a>
                </div>
                <div class="div">
                    <a class=" blue02" href="javascript:;" onclick="$('#adminOperaDiv').load('/Home/ResetThumbPicPath?r' + Math.random());">
                        缩略图地址批量修改</a>
                </div>
                <div class="div">
                    <a class=" blue02" href="javascript:;" onclick="$('#adminOperaDiv').load('/Home/FileStatistics?r' + Math.random());">
                        文档统计</a>
                </div>
                <div class="div">
                    <a class=" blue02" href="javascript:;" onclick="$('#adminOperaDiv').load('/Home/FileRelRedundancy?r' + Math.random());">
                        文档冗余统计</a>
                </div>
                <div class="div">
                    <a class=" blue02" href="javascript:;" onclick="$('#adminOperaDiv').load('/Home/PKCounterStatistics?r' + Math.random());">
                        主键统计</a>
                </div>
            </div>
            <div class=" box_con">
                <div id="adminOperaDiv" style="margin-top: 5px;">
                </div>
            </div>
        </div>
    </div>
</body>
<script>
    $(document).ready(function () { loadPluginList(); });
    $("div.CJJ").find("div:eq(0)").find("a").click();
    $("div.CJJ").find("div").each(function () {
        $(this).click(function () {
            $(this).addClass("divselect").removeClass("div").siblings().removeClass("divselect").addClass("div");
        })
    })
</script>
</html>
