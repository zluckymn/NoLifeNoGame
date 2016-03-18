var FileListDownLoadDiv_;

function initFileListDownCheckBox() {
    //选中已保存的下载项
    var NeedFileListDown = getCookie("initFileListDown");
    if (NeedFileListDown != null) {
        var NeedFileListDownA = NeedFileListDown.split("|");
        for (var x = 0; x < NeedFileListDownA.length; x++) {
            $("input[type=checkbox][value=" + NeedFileListDownA[x] + "]").attr("checked", true);
            $("input[type=checkbox][fvalue=" + NeedFileListDownA[x] + "]").attr("checked", true);
        }
    }
    if ($("input[type=checkbox][NeedFileListDown=True]:checked").length > 0) {
        var pos = "bottom";
        if (top.document.body != document.body) { pos = "right"; }

        FileListDownLoadDiv_.meerkat({
            height: '40px',
            width: '250px',
            position: pos,
            close: '.closemConfig',
            dontShowAgain: '.dont-show',
            animationIn: 'slide',
            animationSpeed: 300
        });
    }
    //初始化需要进行文件下载的CheckBox
    $("input[type=checkbox][NeedFileListDown=True]").each(function() {

        $(this).unbind("click").bind("click", function() {
            if ($("#FileDownWindow").length > 0) {
                closeById("BoxFileDownWindow");
            }


            var Add = "";
            var Del = "";
            var NeedDownFileList = getCookie("initFileListDown");
            if (NeedDownFileList == null) NeedDownFileList = "";

            $("input[type=checkbox][NeedFileListDown=True][value!=0]").each(function() {
                var v = $(this).val();
                if (v.indexOf("=") == -1) { v = $(this).attr("fvalue"); }
                if ($(this).attr("checked") == true) {
                    if (Add != "") { Add += "|"; }
                    Add += v;
                } else {
                    if (Del != "") { Del += "|"; }
                    Del += v;
                }
            });

            if ($("input[type=checkbox][NeedFileListDown=True]:checked").length > 0) {
                var pos = "bottom";
                if (top.document.body != document.body) { pos = "right"; }
                FileListDownLoadDiv_.meerkat({
                    height: '40px',
                    width: '250px',
                    position: pos,
                    close: '.closemConfig',
                    dontShowAgain: '.dont-show',
                    animationIn: 'slide',
                    animationSpeed: 300
                });

                if (NeedDownFileList == "") {
                    NeedDownFileList = Add;
                } else {
                    var AddList = Add.split("|");
                    var NeedDownFileListA = NeedDownFileList.split("|");
                    for (var x = 0; x < AddList.length; x++) {
                        var ad = true;
                        for (var y = 0; y < NeedDownFileListA.length; y++) {
                            if (NeedDownFileListA[y] == AddList[x]) ad = false;
                        }
                        if (ad == true) {
                            if (NeedDownFileList != "") NeedDownFileList += "|";
                            NeedDownFileList += AddList[x];
                        }
                    }

                    //NeedDownFileListA = $.merge(NeedDownFileListA, AddList);
                    //$.unique(NeedDownFileListA);
                    //NeedDownFileList = "";
                    //for (var x = 0; x < NeedDownFileListA.length; x++) {
                    //   if (NeedDownFileList != "") { NeedDownFileList += "|"; }
                    //    NeedDownFileList += NeedDownFileListA[x];
                    //}
                }
                setCookie("initFileListDown", NeedDownFileList, 365);
            } else {
                FileListDownLoadDiv_.destroyMeerkat();
            }

            DeleteNeedFileListDown(Del, NeedDownFileList);
        });
    });
}


function DeleteNeedFileListDown(DelList, NeedDownFileList) {
    if (DelList == "" || NeedDownFileList == "") return false;
    DelList = DelList.split("|");
    var NeedDownFileListA = NeedDownFileList.split("|");
    NeedDownFileList = "";

    for (var x = 0; x < NeedDownFileListA.length; x++) {
        var del = false;
        for (var y = 0; y < DelList.length; y++) {
            if (DelList[y] == NeedDownFileListA[x]) { del = true; }
        }
        if (del == false) {
            if (NeedDownFileList != "") NeedDownFileList += "|";
            NeedDownFileList += NeedDownFileListA[x];
        }
    }
    setCookie("initFileListDown", NeedDownFileList, 365);
}

//导出文件列表批量下载
function FileListDownCustom() {
    var p = top.mutiselect.ShowPath();
    if (p == "") { return false; };
    $("td.tempFileList").each(function() {
        //处理文件夹部分
        var name = $(this).find("input[name=name]");
        var ext = $(this).find("input[name=ext]");
        var hash = $(this).find("input[name=hash]");
        //开始下载
        downloadObj.AddTask(0, hash.val(), p + "\\" + name.val() + ext.val());
    });
}

function DownNeedFileListDown(NeedFileListDown, vprojId) {
    if (vprojId) projId = vprojId;
    if (NeedFileListDown == null && $("input[type=checkbox][NeedFileListDown]:checked").length == 0) {
        $.tmsg("m_jfw", "您未选择任何需要下载的文件", { infotype: 2 });
    } else {

        if (NeedFileListDown == null) {
            $("input[type=checkbox][NeedFileListDown]:checked").each(function() {
                var v = $(this).val();
                if (v.indexOf("=") == -1) { v = $(this).attr("fvalue"); }
                if (NeedFileListDown != "") NeedFileListDown += "|";
                NeedFileListDown += v;
            });
        } else {
        var NeedFileListDownA = NeedFileListDown.split("|");
            var count = false;
            for (var x = 0; x < NeedFileListDownA.length; x++) {
                if (NeedFileListDownA[x].split("=")[1] == projId) {
                    count = true;
                }
            }
            if (count != true) {
                $("input[type=checkbox][NeedFileListDown]:checked").each(function() {
                    var v = $(this).val();
                    if (v.indexOf("=") == -1) { v = $(this).attr("fvalue"); }
                    if (NeedFileListDown != "") NeedFileListDown += "|";
                    NeedFileListDown += v;
                });
            }
        }
        var FileDownWindow = box("<div id=FileDownWindow><div style='width:100%; height:200px; overflow:scroll;overflow-x:hidden;background-color:White'><table width='100%'></table></div></div>", { title: "批量下载", boxid: 'BoxFileDownWindow', prt: null, contentType: 'html', cls: 'shadow-container', submit_cb: function(o) {
            var p = top.mutiselect.ShowPath();
            if (p == "") { return false; }
            $("td[hash]", o.db).each(function() {
                //处理文件夹部分
                var patha = $(this).html().split("\\");
                var temp = p;
                if (patha.length > 1) {
                    for (var x = 0; x < patha.length - 1; x++) {
                        temp = temp + "\\" + patha[x];
                        selFile.CreateFolderByPath(temp);
                    }
                }
                //开始下载
                downloadObj.AddTask(0, $(this).attr("hash"), p + "\\" + $(this).html());
                //开始下载后清空所有选中文件
                delCookie("initFileListDown");

            });
            $.tmsg("m_jfw", "下载开始,共" + $("td[hash]", o.db).length + "个文件。");
        }
        });

        var NeedFileListDownA = NeedFileListDown.split("|");
        var key = ""; var FileList = "";
        var html = "";
        for (var x = 0; x < NeedFileListDownA.length; x++) {
            if (NeedFileListDownA[x].split("=")[1] == projId) {
                if (NeedFileListDownA[x].split("=")[0] == "directory") {
                    if (key != "") { key += ","; }
                    key += NeedFileListDownA[x].split("=")[2];
                } else {
                    if (FileList != "") { FileList += ","; }
                    FileList += NeedFileListDownA[x].split("=")[1];
                    var o = NeedFileListDownA[x].split("=")[2].split("-");
                    var dhash = o[4];
                    var dname = o[2];
                    var dext = o[3];

                    if (o.length > 5) {
                        dhash = o[o.length - 1];
                        dext = o[o.length - 2];
                        dname = "";
                        for (var dx = 2; dx < o.length - 2; dx++) {
                            if (dname != "") dname += "-";
                            dname += o[dx];
                        }
                    }

                    html += "<tr><td height='25' style='background-color:#e0ecf9; border-bottom:1px solid #d3e1f0' hash='" + dhash + "' href=javascript:;>" + dname + dext + "</td></tr>";
                }
            }
        }

        var urlKeyForDirectory = "ProjDatasets";
        if (window.urlKeyForDirectoryChangeTo) { urlKeyForDirectory = urlKeyForDirectoryChangeTo; }
        $("#FileDownWindow", parent.document).find("table").append(html);

        if (key != "") {
            $.ajax({
                url: "/Projects/" + urlKeyForDirectory + "/GetDocPackageFileInfo/" + projId,
                type: 'post',
                data: { fileInfoStr: key },
                cache: false,
                dataType: 'html',
                error: function() {
                    hiAlert('未知错误，请联系服务器管理员，或者刷新页面重试', '数据读取失败');
                },
                success: function(data) {
                    if (data.Success == false) {
                        hiAlert(data.msgError, '数据读取失败');
                    }
                    else {
                        var RetStr = data.split("|")[1];
                        if (RetStr != "") {
                            RetStr = eval('(' + RetStr + ')');
                            html = "";
                            for (var x = 0; x < RetStr.length; x++) {
                                //alert(RetStr[x].filePath.replace(/\\/g, "\\\\"));
                                var hash = RetStr[x].hash == "" ? null : RetStr[x].hash;
                                html += "<tr><td height='25' class='fontblue' hash='" + hash + "' href=javascript:;>" + RetStr[x].filePath + "\\" + RetStr[x].name + RetStr[x].ext + "</td></tr>";
                            }
                            $("#FileDownWindow", parent.document).find("table").append(html);
                        }
                    }
                }
            });



        }



    }
}

//2012.04.18 万科新增下载文件夹
function ProjCatDownLoadFile(id) {
    $("#List").selectTreeItem(id);
    var p = top.mutiselect.ShowPath();

    if (p == "") { return false; }
    var urlKeyForDirectory = "ProjDatasets";
    if (window.urlKeyForDirectoryChangeTo) { urlKeyForDirectory = urlKeyForDirectoryChangeTo; }
    if (id != "") {
        $.ajax({
            url: "/Projects/" + urlKeyForDirectory + "/GetDocCatPackageFileInfo/" + projId,
            type: 'post',
            data: { fileInfoStr: id },
            cache: false,
            dataType: 'html',
            error: function() {
                hiAlert('未知错误，请联系服务器管理员，或者刷新页面重试', '数据读取失败');
            },
            success: function(data) {
                if (data.Success == false) {
                    hiAlert(data.msgError, '数据读取失败');
                }
                else {
                    var RetStr = data.split("|")[1];
                    if (RetStr != "") {
                        RetStr = eval('(' + RetStr + ')');
                        html = "";
                        var path = "";
                        for (var x = 0; x < RetStr.length; x++) {
                            //alert(RetStr[x].filePath.replace(/\\/g, "\\\\"));
                            var hash = RetStr[x].hash == "" ? null : RetStr[x].hash;
                            html += "<tr><td height='25' class='fontblue' hash='" + hash + "' href=javascript:;>" + RetStr[x].filePath + "\\" + RetStr[x].name + RetStr[x].ext + "</td></tr>";
                        }
                        $("#hashValue table").append(html);

                        $("#hashValue td[hash]").each(function() {

                            var patha = $(this).html().split("\\");
                            var temp = p;
                            if (patha.length > 1) {
                                for (var x = 0; x < patha.length - 1; x++) {
                                    temp = temp + "\\" + patha[x];
                                    selFile.CreateFolderByPath(temp);
                                }
                            }
                            //开始下载
                            downloadObj.AddTask(0, $(this).attr("hash"), p + "\\" + $(this).html());

                        });
                        $.tmsg("m_jfw", "下载开始,共" + $("td[hash]").length + "个文件。");
                        $("#hashValue table").html("");

                    } else {
                        $.tmsg("m_jfw", "下载开始,共" + 0 + "个文件。");

                    }
                }
            }
        });
    }
}

function MakeFIleDownList(NeedFileListDown) {
    var NeedFileListDownA = NeedFileListDown.split("|");
    var ids = "";
    for (var x = 0; x < NeedFileListDownA.length; x++) {
        if (NeedFileListDownA[x].split("=")[1] == projId) {
            if (NeedFileListDownA[x].split("=")[0] == "directory") {
            } else {
                if (ids != "") { ids += ","; }
                ids += NeedFileListDownA[x].split("=")[2].split("-")[0];
            }
        }
    }
    $.ajax({
        url: '/Projects/ProjDatasets/CreateFileUrlHtml?fileIds=' + ids,
        type: 'get',
        cache: false,
        dataType: 'html',
        error: function() {
            hiAlert('未知错误，请联系服务器管理员，或者刷新页面重试', '数据读取失败');
        },
        success: function(RetfileName) {
            box('/UploadFiles/temp/' + RetfileName,
               { title: '导出文件链接列表', contentType: 'ajax', cls: 'shadow-container2', submit_BtnName: '确定导出', width: 581,
                   onLoad: function(o) {
                       $(".hideButton", o.db).css("display", "none");
                   },
                   submit_cb: function() {
                       window.location.href = "/Projects/ProjDatasets/DownFileUrlHtml?fileName=" + escape(RetfileName);
                   }
               });

        }
    });



}

$(document).ready(function() {

    if ($(top.document.body).find("#FileListDownLoadDiv").length == 0) {
        //构建信息提示框
        var w = 240, i = 235;
        if (top.verificationCode == "AB9BE7AD-AE9A-43b3-A9F3-4FAB9061F53E") { w = 360; i = 355; }

        FileListDownLoadDiv_ = '<div id="FileListDownLoadDiv" style="border: #f4cb49 1px solid; background:#fff9cc; width:' + w + 'px;padding:10px;  position: relative; display: none;">';
        FileListDownLoadDiv_ += '<span style="position: absolute; margin-left: ' + i + 'px; margin-top: -8px; cursor: hand" class="closemConfig">';
        FileListDownLoadDiv_ += '<img src="/Content/Images/zh-cn/standproduct/ico-close2.gif" /></span>';
        FileListDownLoadDiv_ += '<a id="a" href="javascript:void(0);" class="blue4">批量下载选中的文件</a>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;';
        if (top.verificationCode == "AB9BE7AD-AE9A-43b3-A9F3-4FAB9061F53E") { FileListDownLoadDiv_ += '<a id="c" href="javascript:void(0);" class="blue4">导出文件链接列表</a>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;'; }
        if (top.verificationCode == "294728B6-FF56-4acd-A526-BFAFAC5E87D5") {
            FileListDownLoadDiv_ += '<a id="b" href="javascript:void(0);" class="blue4">取消已选文件</a>';
        } else {
            FileListDownLoadDiv_ += '<a id="b" href="javascript:void(0);" class="blue4">清空已选文件</a>';
        }
        FileListDownLoadDiv_ += '</div>';
        FileListDownLoadDiv_ = $(FileListDownLoadDiv_);
        FileListDownLoadDiv_.appendTo(document.body);
    }

    //初始化脚本
    //YHLoader.load("r_jquery_jquery.meerkat.1.3.js");
    //定义批量下载按钮事件
    FileListDownLoadDiv_.find("#a").unbind("click").bind("click", function() {
        var NeedFileListDown = getCookie("initFileListDown");
        DownNeedFileListDown(NeedFileListDown);
    });

    //定义批量取消选中事件
    FileListDownLoadDiv_.find("#b").unbind("click").bind("click", function() {
        delCookie("initFileListDown");
        FileListDownLoadDiv_.destroyMeerkat();
        $("input[type=checkbox][NeedFileListDown=True]").attr("checked", false);
    });

    if (top.verificationCode == "AB9BE7AD-AE9A-43b3-A9F3-4FAB9061F53E") {
        //导出文件链接列表
        FileListDownLoadDiv_.find("#c").unbind("click").bind("click", function() {
            var NeedFileListDown = getCookie("initFileListDown");
            MakeFIleDownList(NeedFileListDown);
        });
    }


    //奥园 AB9BE7AD-AE9A-43b3-A9F3-4FAB9061F53E 
    //龙光 BD3BF586-A073-452d-B0FF-9F48DE73DFLG 

    initFileListDownCheckBox();
});
