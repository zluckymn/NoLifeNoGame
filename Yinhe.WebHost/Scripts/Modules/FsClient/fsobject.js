/*
功能：输出fs的控件对象
  
mgrObj：连接文件服务器对象
mutiselect：一些本地操作对象（如：选择文件，保存文件，序列化文件）
uploadObj：上传文件到文件服务器
downloadObj：从文件服务器下载文件
readObj：读取文件服务器文件
spliteObj：切割dwg图
thumbObj：dwg提取缩略图
thumbUploadObj：上传队列
新增：oframe:在线编辑OFFICE文件
*/





//是否加载控件，权限高，0：加载 其他情况都不加载
var LoadActiveX = 1;
//是否处于升级控件状态，权限高，0：不需要，1：升级版本A，2：升级版本B，3：升级版本C
var UpdateActive = 0;
//初始化系统版本号简称
var SysActiveXVerStr = "";
var ObjStatus = -1;
//初始化控件版本号
var SysObjVer = "0.0.0.0";
var CheckInstallWellRet = 0;

function printObject() {
    //写入系统初始化需要的Object
    document.write("<object classid=\"clsid:BA106BC1-33A5-45B9-9845-D6CEE0780BB4\"  id=\"VerUpdate\" name=\"VerUpdate\" width=\"0\" height=\"0\"></object>");

    try{
        VerUpdate.SetCheckLockList("FileManager.ocx|dsoframer.ocx|CPMNetWork.ocx|WebBrowser.ocx|A2WebServices.ocx|FreeImage.ocx|FreeImagePlus.ocx|DwgViewXProxy.exe");
    }
    catch (err) { }
    
    //1、进入系统后，调用Service.C升级服务，检测Ver.C是否正常：
    //•正常（服务返回结果：客户端版本=服务端版本）：加载并进入系统（结束）
    //•需要升级：触发C升级模式，不加载控件（结束）
    //•Service.C不存在：开始调用备用接口（4.1.0.0以上），检测Ver.C文件是否存在，mutiselect.CheckInstallWell(BSTR bstrVer, Service.C.Ver)： 
    //•返回0：原则不存在此情况，直接跳出
    //•返回1：安装文件完整，但服务没有注册，且版本号一样，可以加载控件 
    //•提示用户进行服务注册，直接跳出： 
    //•仅提示一次
    //•弹出《服务注册帮助页面》
    //•返回2：安装文件完整，但服务没有注册，且版本号不一样，不可以加载控件 
    //•强制用户进行服务注册，直接跳出 
    //•一直在提示
    //•弹出《服务注册帮助页面》
    //•返回3：安装文件不完整，可能是卸载不干净、或安装失败回滚了 
    //•强制用户重新安装，直接跳出 
    //•弹出《控件安装帮助页面》
    //•返回4（仅在升级到4.1.0.0才会出现这种情况）：控件需要升级Service.C
    //•备用接口不存在：若不存在或者错误，进入下一步检测 2

    //统一接口判断文件是否能加载
    //btrVer： 服务器版本号
    //btrURL： 升级程序下载地址
    //btrWebSvrURL： 获取上面两个参数的WebService地址
    //blngStatus： 升级过程状态，如下
    //  enNone = 0,
    // enDownLoad = 1,
    // enDownLoading = 2,
    // enDownLoadFail = 3,
    // enDownLoadDone = 4,
    // enBackup = 5,
    // enBackuping = 6,
    // enBackupDone = 7,
    // enInstalling = 8,
    // enInstallDone = 9,
    // enInstallFail = 10,
    // enReverting = 11,
    // enRevertDone = 12
    //pbResult： 是否可以加载控件：0可以，1不可以
    //STDMETHODIMP CUpdateObj::CanLoadActiveX(BSTR bstrVer, BSTR bstrURL, BSTR bstrWebSvrURL, LONG* blngStatus, LONG* pbResult)

    try {
        var blngStatus;
        if (VerUpdate.CanLoadActiveX(fs.curVer, fs.UpdateBag, fs.updateSvrUrl, blngStatus) == 0)
        //判断控件安装状态：LoadActiveX
        // 0：可以加载
        // 1：不能加载
        {
            LoadActiveX = 0;
            SysObjVer = fs.curVer;
        }
        else {
            LoadActiveX = 1;
            UpdateActive = 3;
            return false;
        }
        //顺利检测完成，跳出
        SysActiveXVerStr = "Ver.C";
        return true;
    }
    catch (err) { }


    try {

        document.write("<object classid=\"clsid:5688729B-B2AE-4315-8BDA-E062460B1FE6\"  id=\"mutiselect\" name=\"mutiselect\" width=\"0\" height=\"0\" ></object>");

        var svrStatus;
        CheckInstallWellRet = mutiselect.CheckInstallWell(fs.curVer, fs.updateSvrVer, svrStatus);
        //判断控件安装状态：CheckInstallWellRet
        // 0：正常安装
        // 1：安装文件完整，但服务没有注册，且版本号一样，可以加载控件
        // 2：安装文件完整，但服务没有注册，且版本号不一样，不可以加载控件
        // 3：安装文件不完整，可能是卸载不干净、或安装失败回滚了
        if (CheckInstallWellRet == 2) { UpdateActive = 3; }

        if (CheckInstallWellRet == 2 || CheckInstallWellRet == 1) {
            if (getCookie("ServiceRegister") != "True") {
                setCookie("ServiceRegister", "True", 1);
                window.open("/home/ServiceRegister");
                //alert("备用接口，状态为" + CheckInstallWellRet + ",弹出注册提示");
            } else { //alert("备用接口，状态为" + CheckInstallWellRet + ",不弹出注册提示");
            }
        }

        if (CheckInstallWellRet == 3) {
            if (getCookie("ActiveXError") != "True") {
                setCookie("ActiveXError", "True", 1);
                window.open("/home/ActiveXError");
                //alert("备用接口，状态为" + CheckInstallWellRet + ",弹出错误提示");
            } else { //alert("备用接口，状态为" + CheckInstallWellRet + ",不弹出错误提示"); 
            }
        }

        if (CheckInstallWellRet == 0 || CheckInstallWellRet == 1)
        { LoadActiveX = 0; SysObjVer = fs.curVer; }
        else
        { LoadActiveX = 1; return false; }

        //检测完成，跳出
        SysActiveXVerStr = "Ver.C";
        return true;
    }
    catch (err) {
        //alert("备用接口异常！")
    }

    //2、调用Service.B检测Ver.B是否存在：
    //•正常（JS脚本取：客户端版本=服务端版本）：加载并进入系统（结束）
    //•需要升级：触发A(B)升级模式，不加载控件（结束）
    //•Service.B服务不存在：进入下一步检测 3
    try {
        //alert("开始检测Ver.B");
        ObjStatus = VerUpdate.GetStatus();
        SysObjVer = VerUpdate.GetVersion();
        if (SysObjVer == "1.0.0.1") { SysObjVer = "1.0.0"; }

        SysActiveXVerStr = "Ver.B";
        if (SysObjVer != fs.curVer) { LoadActiveX = 1; UpdateActive = 2; return false; } else { LoadActiveX = 0; return true; }
        //检测完成，跳出
        //alert("检测Ver.B完成");

    }
    catch (err) {
        //alert("检测Ver.B发生异常");
        LoadActiveX = 1;
    }

    //3、调用控件检测Ver.A是否存在(当前时代的情况)：
    //•正常（JS脚本取：客户端版本=服务端版本）：加载并进入系统（结束）
    //•需要升级：触发A(B)升级模式，不加载控件（结束）
    //•控件不存在：进入下一步检测 4
    try {
        //alert("开始检测Ver.A");

        document.write("<object classid=\"clsid:6D64BB8A-4043-4A39-8FA3-A486E763D6F0\"  id=\"mgrObj\" name=\"mgrObj\" width=\"0\" height=\"0\"></object>");
        SysObjVer = mgrObj.GetVersion();
        SysActiveXVerStr = "Ver.A";
        //alert("检测Ver.A完成");
        if (SysObjVer != fs.curVer) { LoadActiveX = 1; UpdateActive = 1; } else {
            //走到此步，最后结合版本号进行验证
            if (SysObjVer <= "4.0.0.0") {
                fs.websvrurl = fs.oldwebsvrurl; //检测完成，跳出 
            } LoadActiveX = 0;
            return true;
        }



    }
    catch (err) {
        //alert("检测Ver.A发生异常");

        LoadActiveX = 1;
    }

    //其他情况，如果取得的系统版本号为1.0.0，基本上可视为未卸载干净，不加载控件
    if (SysObjVer == "1.0.0") { LoadActiveX = 1; }

    //全部检查结束，如果到此时仍未跳出，则也不加载控件
    return false;
}

if (printObject()) {
    //alert("检测完成,当前控件版本号为" + SysActiveXVerStr + ",当前控件版本为：" + SysObjVer);
    //写入剩余需要的Object
    //alert("跳出，开始加载控件Object");
    if (!document.getElementById("mutiselect")) {
        // alert("加载：mutiselect");
        document.write("<object classid=\"clsid:5688729B-B2AE-4315-8BDA-E062460B1FE6\"  id=\"mutiselect\" name=\"mutiselect\" width=\"0\" height=\"0\" ></object>");
    }

    if (!document.getElementById("mgrObj")) {
        // alert("加载：mgrObj");
        document.write("<object classid=\"clsid:6D64BB8A-4043-4A39-8FA3-A486E763D6F0\"  id=\"mgrObj\" name=\"mgrObj\" width=\"0\" height=\"0\"></object>");
    }
    document.write("<object classid=\"clsid:BDC79CEF-1E13-42DD-86D6-7FF5C9F698EE\"  id=\"uploadObj\" name=\"uploadObj\" width=\"0\" height=\"0\" ></object>");
    document.write("<object classid=\"clsid:B5629C04-5C7D-4627-9673-06D032ECF4EC\"  id=\"downloadObj\" name=\"downloadObj\" width=\"0\" height=\"0\" ></object>");
    document.write("<object classid=\"clsid:B5629C04-5C7D-4627-9673-06D032ECF4EC\"  id=\"readObj\" name=\"readObj\" width=\"0\" height=\"0\" ></object>");
    document.write("<object classid=\"clsid:C1EDAC7F-F3D8-40A4-AF3F-89A5FEB22AD0\"  id=\"mutiselectNew\" name=\"mutiselectNew\" width=\"0\" height=\"0\" ></object>");

    
//    document.write("<object classid=\"clsid:5E47D37C-FDDE-4669-AB59-FE68555206E1\"  id=\"spliteObj\" name=\"spliteObj\" width=\"0\" height=\"0\" ></object>");
//    document.write("<object classid=\"clsid:119DA279-774D-4F34-A868-8A36EFCF3A24\"  id=\"thumbObj\" name=\"thumbObj\" width=\"0\" height=\"0\" ></object>");
//    document.write("<object classid=\"clsid:4D22E82A-B9C7-4BF5-BA68-22159BB489F8\"  id=\"thumbUploadObj\" name=\"thumbUploadObj\" width=\"0\" height=\"0\" ></object>");
    document.write("<object classid=\"clsid:590A5E27-85CD-4043-8490-94ECCF03CE33\"  id=\"webviewerObj\" name=\"webviewerObj\" width=\"0\" height=\"0\" ></object>");
    //alert("加载控件Object结束");

}


$(document).ready(function() {
showStartTopMsg();
try { fs.SetCSip(); } catch (err) { }
});


function showStartTopMsg() {
    try {
        //一切正常，显示控件信息，初始化控件
        if (LoadActiveX == 0) {
            $("#YinhooActiveXInfo").html('<a id="YHActiveX" href="javascript:void(0)" class="blue" style="text-decoration:none;" onmouseover="ShowSysActiveXInfo(this.id);">控件已安装<img src="/Content/Images/zh-cn/Common/onSuccess.gif" /></a>');
            fs.HandleList();
            return true;
        }

        //需要升级，根据升级类型提供信息显示
        if (LoadActiveX == 1 && UpdateActive > 0) {
            if (UpdateActive == 1) {
                $("#YinhooActiveXInfo").html('<a id="UpdateActiveX" href="javascript:void(0)" onclick="fs.UpdateCtl();" class="blue">请升级控件(最新版本:' + fs.curVer + ')</a>');
            }

            if (UpdateActive == 2) {
                $("#YinhooActiveXInfo").html('<a id="UpdateActiveXIng" onclick="hiAlert(\'控件正在升级中，请关闭IE稍后重新打开。\',\'控件升级\');" href="javascript:void(0)" class="blue">控件升级中(最新版本:' + fs.curVer + ')</a>');
                VerUpdate.DownLoad(fs.curVer, fs.UpdateBag);
            }

            if (UpdateActive == 3) {
                if (CheckInstallWellRet == 2) {
                    $("#YinhooActiveXInfo").html('<a id="UpdateActiveXIng" onclick="window.open(\'/home/ServiceRegister\');" href="javascript:void(0)" class="blue">请升级控件(最新版本:' + fs.curVer + ')</a>');
                }
                else {
                    $("#YinhooActiveXInfo").html('<a id="UpdateActiveXIng" onclick="hiAlert(\'控件正在升级中，请关闭IE稍后重新打开。\',\'控件升级\');" href="javascript:void(0)" class="blue">控件升级中(最新版本:' + fs.curVer + ')</a>');
                }
            }
            return false;
        }

        //不可加载控件，没有升级信息，则显示安装信息
        if (LoadActiveX == 1 && UpdateActive == 0) {
            $("#YinhooActiveXInfo").html('<a id="SetUpActiveX" href="/home/ActiveXSetup" class="blue">请安装控件(最新版本:' + fs.curVer + ')</a>');
            return false;
        }
        return true;
    }
    catch (err) {
        //如果没有安装插件，直接返回
        return false;
    }
}


function showStartMsg() {
    try {
        //一切正常，显示控件信息，初始化控件
        if (LoadActiveX == 0) {
            return true;
        }

        //需要升级，根据升级类型提供信息显示
        if (LoadActiveX == 1 && UpdateActive > 0) {
            ShowTipAboutUpdate();
            return false;
        }

        //不可加载控件，没有升级信息，则显示安装信息
        if (LoadActiveX == 1 && UpdateActive == 0) {
            ShowTipAboutSetUp();
            return false;
        }
        return true;
    }
    catch (err) {
        //如果没有安装插件，直接返回
        return false;
    }
}


function ShowTipAboutSetUp() {
    $(document).ready(function() { top.hiAlert("您还未安装银禾控件！请<A onclick='$(\"#popup_ok\").click();' href='/Home/ActiveXSetup' target=_blank>点此安装！</a>。", "您未安装控件"); });
}

function ShowTipAboutUpdate() {

    if (SysObjVer == "1.0.0") {
        top.hiAlert("您还未安装银禾控件！请<A onclick='$(\"#popup_ok\").click();' href='/home/ActiveXSetup' target=_blank>点此安装！</a>。", "您未安装控件"); return true;
    }

    if (UpdateActive == 1) {
        $(document).ready(function() { top.hiAlert("银禾控件服务器版本已升级(服务器版本：" + fs.curVer + "&nbsp;客户端版本：" + SysObjVer + ")！请<A href='#' onclick='fs.UpdateCtl();$(\"#popup_ok\").click();'>点此更新！</a> 更新过程中需要关闭IE浏览器，进行重试更新！", "控件升级"); });
    }

    if (UpdateActive == 2) {
        OutUpdateStr = "银禾控件正在升级中(服务器版本：" + fs.curVer + ")！请稍后刷新浏览器！当系统右上角显示绿色勾图标时，表示升级成功。</a>";
        $(document).ready(function() { top.hiAlert(OutUpdateStr, "控件升级"); });
        VerUpdate.DownLoad(fs.curVer, fs.UpdateBag);
    }

    if (UpdateActive == 3) {
        if (CheckInstallWellRet != 2) {
            OutUpdateStr = "银禾控件正在升级中(服务器版本：" + fs.curVer + ")！请稍后刷新浏览器！当系统右上角显示绿色勾图标时，表示升级成功。</a>";
            $(document).ready(function() { top.hiAlert(OutUpdateStr, "控件升级"); });
        }
        else {
            OutUpdateStr = "银禾控件需要升级(服务器版本：" + fs.curVer + ")！系统检测到您的升级服务未正确安装或者未正常启动，请先<A href='javascript:void(0)' onclick='window.open(\"/home/ServiceRegister\");'>点此注册服务</a>。</a>";
            $(document).ready(function() { top.hiAlert(OutUpdateStr, "控件升级"); });
        }
    }



}

function ShowSysActiveXInfo(id) {
    $("#" + id).attr("betterTip", "");
    $("#" + id).attr("config", "");
    $("#" + id + "_betterTip").remove();


    var str = "";
    if (mgrObj.GetTaskCount(1) > 0) { str += "上传中：" + mgrObj.GetTaskCount(1) + "<br>"; }
    if (mgrObj.GetTaskCount(2) > 0) { str += "下载中：" + mgrObj.GetTaskCount(2) + "<br>"; }
    if (mgrObj.GetTaskCount(3) > 0) { str += "失败任务：" + mgrObj.GetTaskCount(3) + "<br>"; }
    var tip = "本地控件版本：" + SysObjVer + "<br>";
    if (str == "") { str = "当前没有活动任务"; } else { str += "详情请查看文件传输中心"; }
    tip += str;
    addBetterTip(id, tip);

}

/**
GetTaskCount（i）


typedef [v1_enum] enum
{
enAllType	= 0,	// 获取所有任务
enUploading,		// 获取正在上传
enDownloading,		// 获取正在下载
enErrorTask,		// 获取失败任务
enUploaded,		    // 获取已上传任务
enDownloaded		// 获取已下载任务
**/
//Doc@31-1
//定义统一服务端查询列表
var GetFileUploadServerList = "";
function GetFileUploadStatus(FileId, Obj) {

    try {
        var FileUploadStatusString = mgrObj.GetUploadFileInfo(FileId);
        var FileUploadStatusDataArr = FileUploadStatusString.split("|Y|");
        var FileUploadStatus = FileUploadStatusDataArr[0];
        var FileUploadProc = FileUploadStatusDataArr[1];
        var FileUploadSize = FileUploadStatusDataArr[2];

        //当对象为TD时
        if ($(Obj).attr("tagName").toUpperCase() == "TD") {
            //FileUploadStatus为0时，代表任务已经完成，或者任务不存在
            if (FileUploadStatus == 0) {
                if ($(Obj).attr("GetFileUploadStatus") == "True") {
                    if (GetFileUploadServerList.indexOf(FileId) == -1) {
                        if (GetFileUploadServerList == "")
                        { GetFileUploadServerList = FileId; }
                        else
                        { GetFileUploadServerList += "|" + FileId; }
                    }
                }

            }

            //FileUploadStatus为1时，代表任务进行中
            if (FileUploadStatus == 1) {
                $(Obj).html("上传:<font color=green>" + parseInt(FileUploadProc * 100) + "%</font>");
            }

            //FileUploadStatus为2时，代表任务暂停
            if (FileUploadStatus == 2) {

                if (parseInt(FileUploadProc * 100) == 100) { $(Obj).html("等待:<font color=blue>" + parseInt(FileUploadProc * 100) + "%</font>"); }
                else { $(Obj).html("暂停:<font color=blue>" + parseInt(FileUploadProc * 100) + "%</font>"); }
            }

            //FileUploadStatus为3时，代表任务失败
            if (FileUploadStatus == 3) {
                $(Obj).attr("GetFileUploadStatus", "False");
                $(Obj).html("<font color=red>失败</font>");
            }

        }
    }
    catch (err) { }
}

function GetAllFileUploadStatus() {
    var FileMaskList = "";
    ////////////////////////////
    //hiOverAlert("检查开始");
    ////////////////////////////
    var GetAllFileUploadStatusCount = 0;
    $("a[GetFileUploadStatus=True]").each(function() {
        if ($(this).attr("FileId")) {
            GetFileUploadStatus($(this).attr("FileId"), this);
            GetAllFileUploadStatusCount++;
        }
    });

    $("td[GetFileUploadStatus=True]").each(function() {
        if ($(this).attr("FileId")) {
            GetFileUploadStatus($(this).attr("FileId"), this);
            GetAllFileUploadStatusCount++;
        }
    });




    if (GetAllFileUploadStatusCount == 0) {
        ////////////////////////////
        //hiOverAlert("检查结束");
        ////////////////////////////
        return true;
    } else {
        ////////////////////////////
        //hiOverAlert("检查中，剩余项目" + GetAllFileUploadStatusCount);
        ////////////////////////////
        setTimeout("GetAllFileUploadStatus()", 3000);
    }


    if (GetFileUploadServerList != "") {
        HaveGetFileStatusFromServer = 1;
    } else {
        HaveGetFileStatusFromServer = 0;
    }

}
var HaveGetFileStatusFromServer = 0;
function GetFileStatusFromServer() {
    if (HaveGetFileStatusFromServer == 1) {
        var GetFileUploadServerListArr = GetFileUploadServerList.split("|");
        $.get("/Projects/ProjectResults/ProjRetDocListStatus?p=" + GetFileUploadServerList + "&e=" + Math.random(), function(data) {
            if (data != "") {
                for (var x = 0; x < data.length; x++) {
                    $("td[FileId=" + GetFileUploadServerListArr[x] + "]").html("处理中");
                    if (data[x].type == 1) { $("td[FileId=" + GetFileUploadServerListArr[x] + "]").html(data[x].value); $("td[FileId=" + GetFileUploadServerListArr[x] + "]").attr("GetFileUploadStatus", "Success"); }
                    if (data[x].type == 2) { $("td[FileId=" + GetFileUploadServerListArr[x] + "]").html("快照中"); }
                } 
            }
        });
        GetFileUploadServerList = "";
    }
    setTimeout("GetFileStatusFromServer()", 5000);
}


/**

$(document).ready(function() {
$("a[GetFileUploadStatus=True]").each(function() {
if ($(this).text().length != 0 && $(this).attr("GetFileUploadStatus") == "True") {
$(this).attr("GetFileUploadStatus", "Success");
}
});

$("td[GetFileUploadStatus=True]").each(function() {
if ($(this).text().length != 0 && $(this).attr("GetFileUploadStatus") == "True") {
$(this).attr("GetFileUploadStatus", "Success");
}
});

GetAllFileUploadStatus();
GetFileStatusFromServer();
});

**/