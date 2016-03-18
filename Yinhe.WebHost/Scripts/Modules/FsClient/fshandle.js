var preParam = "";
var fsHandleVer = "";
function ReadOnlineCustom(id, ver, type, isBreakShow) {
    fsHandleVer = ver;
    if ($("#showimg_" + id).attr("href")) { $("#showimg_" + id).click(); return true; }
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //处理控件问题，如果有任何异常则禁止使用
    if (!showStartMsg()) { return false; }
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    var url = "";
    if (type == "1") {
        url = "/Projects/ProjectResults/FileInfo/" + id + "?action=1"; preParam = "doc";
    }
    else if (type == "2") {
        url = "/ManagementStandards/Home/FileInfo/" + id + "?action=1"; preParam = "tech";
    }
    else if (type == "3") {
        url = "/ProductStorage/Series/FileInfo/" + id + "?action=1"; preParam = "product";
    }
    else if (type == "4") {
        url = "/CustomerStandards/Home/FileInfo/" + id + "?action=1"; preParam = "cus";
    }
    else if (type == "5") {
        url = "/SpecDoc/Home/FileInfo/" + id + "?action=1"; preParam = "spec";
    }
    else if (type == "6") {
        url = "/Persons/TasksCenter/FileInfo/" + id + "?action=1"; preParam = "newAttachVer";
    }
    else if (type == "7") {
        url = "/Expert/BidProjectInfo/FileInfo/" + id + "?action=1"; preParam = "BidAttachVer";
    }

    else if (type == "8") {
        url = "/Engineering/Home/FileInfo/" + id + "?action=1"; preParam = "PTAttachVer";
    }
    else if (type == "9") {
        url = "/Material/home/FileInfo/" + id + "?action=1"; preParam = "MatDoc";

    }
    else if (type == "10") {
        url = "/ContractManager/Home/FileInfo/" + id + "?action=1"; preParam = "conAjd";
    }
    else if (type == "11") {
        url = "/Persons/MailCenter/FileInfo/" + id + "?action=1"; preParam = "att";
    }
    else if (type == "12") {
        url = "/Marketing/CustOpportunity/FileInfo/" + id + "?action=1"; preParam = "custRefFiles";
    }
    else if (type == "13") {
        url = "/Evaluation/Comment/FileInfo/" + id + "?action=1"; preParam = "comAtt";
    }
    else if (type == "14") {
        url = "/Marketing/CustOpportunity/CmpFileInfo/" + id + "?action=1"; preParam = "cmpAtt";
    }
    else if (type == "15") {
        url = "/Training/Home/FileInfo/" + id + "?action=1"; preParam = "training";
    }
    else if (type == "16") {
        //工作流附件
        url = "/WorkFlowView/Home/FileInfo/" + id + "?action=1"; preParam = "flowAjd";
    }
    else if (type == "17") {
        //工作流附件
        url = "/Foundation/Home/FileInfoForChild/" + id + "?action=1"; preParam = "dwgvsthumb";
    }
    else if (type == "18") {
        //工作流附件
        url = "/Persons/TasksCenter/FileInfoForChild/" + id + "?action=1"; preParam = "dwgvlthumb";
    }
    else if (type == "0") {
        //系统图档库
        url = "/Foundation/Home/FileInfo/" + id + "?action=1"; preParam = "sysObject"; //系统图档库只要不同已经存在的相同就好
        updateDownloadCountCustom(id, 0, 0); //更新下载数量
    }
    else {
        url = "/CustomerAnalyze/Home/FileInfo/" + id + "?action=1"; preParam = "target";
    }

    $.post(
        url,
        {
            num: ver
        },
        function(data) {
            //if (data == "") { return false; }
            // data = eval("(" + data + ")");
            if (data.success) {
                if (data.hash != "") {
                    //isBreakShow=1，强制查看，忽略文件大小///////////////////////////////////////////////////////////////////////////////
                    if (isBreakShow == 1) {
                        downfile(id, type, data, 0);
                        //特殊情况，不引用FLASH播放器直接打开    
                    } else if (isBreakShow == 2) {
                        downfile(id, type, data, 1);
                        //默认情况，当文件大小大于系统指定大小时，出现提示///////////////////////////////////////////////////////////////////////////////
                    } else {
                        if (data.filesize > fs.SysCustFileSize) {
                            hiConfirm('该附件大于' + fs.SysCustFileSize + 'M，需要等待较长时间。<br>建议返回页面直接下载文件查看（需要您有该文件的下载权限）<br>', '提醒', function(r) {
                                if (r) { ReadOnlineCustom(id, ver, type, 1); return false; }
                                else
                                { return false; }
                            });
                            $("#popup_ok").val("打开");
                            $("#popup_cancel").val("不打开");

                        } else {
                            downfile(id, type, data, 0);
                        }
                    }
                } else {
                    alert("文件可能正在传输或者没有上传完毕，暂时无法查看，请稍后出现文件大小之后再查看");
                }
            } else {
                alert(data.errors.msg);
            }
        }
   , "json");
}

function downfile(id, type, data, noflash) {
    updateFileCountCustom(id, type);
    var filename = data.name + data.ext.toLowerCase();
    var verCode = "";
    if (window.verificationCode) { verCode = verificationCode; }
    if (noflash == 0 && (verCode == "736D430A-1F8C-4923-AA4F-1A3003CA46FB" || verCode == "294728B6-FF56-4acd-A526-BFAFAC5E87D5")) {
        //万科专用，针对office系列和pdf 进行弹出flash展示
        if (data.ext.toLowerCase() == ".pdf" || data.ext.toLowerCase() == ".doc" || data.ext.toLowerCase() == ".xls" || data.ext.toLowerCase() == ".ppt" || data.ext.toLowerCase() == ".docx" || data.ext.toLowerCase() == ".xlsx" || data.ext.toLowerCase() == ".pptx") {
            if (type == "1") {
                box("/BaseStore/FileStoreControl/SWFFileView/projectresultdoc/" + id + "?ver=" + fsHandleVer, { boxid: 'showFile', pos: 't-t', contentType: 'ajax', title: data.name, prt: null, cls: 'shadow-container', width: '744' });
            }
            else if (type == "9") {
                box("/BaseStore/FileStoreControl/SWFFileView/materialDoc/" + id + "?ver=" + fsHandleVer, { boxid: 'showFile', pos: 't-t', contentType: 'ajax', title: data.name, prt: null, cls: 'shadow-container', width: '744' });

            } 
            else {
                box("/BaseStore/FileStoreControl/FileView/" + id, { boxid: 'showFile', pos: 't-t', contentType: 'ajax', title: data.name, prt: null, cls: 'shadow-container', width: '744' });
            }

            return false;
        }
    }

    //此处加入对DOC等office文件进行单独处理
    if (data.ext.toLowerCase() == ".doc" || data.ext.toLowerCase() == ".xls" || data.ext.toLowerCase() == ".ppt" || data.ext.toLowerCase() == ".docx" || data.ext.toLowerCase() == ".xlsx" || data.ext.toLowerCase() == ".pptx") {
        var result = mutiselect.GetTempPath();
        var filepathTemp = result + "\\" + data.name + "_" + Math.round(Math.random() * 9999) + data.ext.toLowerCase();
        showModalDialog("/home/ShowDoc?docid=" + id + "&vernum=" + data.ver + "&type=" + type + "&ext=" + data.ext.toLowerCase(), 'YinhooEdit', 'directories:no;scrollbars:no;dialogWidth:960px;dialogHeight:600px;status:no;help:no;Maximize=yes;Minimize=yes;');
        return false;
    }

    ///////////////////////////////////////
    //此处加入对Jpg等被浏览器支持的图片文件进行单独处理
    if (data.ext.toLowerCase() == ".jpg" || data.ext.toLowerCase() == ".bmp" || data.ext.toLowerCase() == ".png" || data.ext.toLowerCase() == ".gif") {
        var result = mutiselect.GetTempPath();
        filepathTemp = result + "\\" + data.name + "_" + Math.round(Math.random() * 9999) + data.ext.toLowerCase();

        if (data.thumbPath) {
            if (data.thumbPath.length > 0) { filepathTemp = data.thumbPath; }
            else {
                mutiselect.DeleteTempFile(filepathTemp);
                IsReadPic = 1;
                readObj.AddTask(3, data.hash, filepathTemp);
            }
        }
        else {
            mutiselect.DeleteTempFile(filepathTemp);
            IsReadPic = 1;
            readObj.AddTask(3, data.hash, filepathTemp);
        }
        var Pichtml = '<a href="' + filepathTemp + '" class="highslide" onclick="return hs.expand(this)"></a>';
        //if (isBreakShow != 2) 
        Pichtml += '<div class="highslide-caption">' + data.name + '</div>';
        $(top.document.body).append(Pichtml);
        if (data.thumbPath) { if (data.thumbPath.length > 0) { $(top.document).find("a:last").click(); } }
        return false;
    }

    if (data.ext.toLowerCase() == ".dwg" && fs.bViewPdfOfDwg == 1) {
        if (data.pdfPath != "") { window.open(data.pdfPath); return false; }
    }

    //readfileStart(data.name + data.ext.toLowerCase());

    if (data.bedit == "0") {
        fs.ReadFile(filename, data.hash);
    } else if (data.bedit == "1") {
        fs.ReadFileEx(filename, data.hash, preParam + "@" + id.toString() + "-" + data.verId);
    }
}

function DownLoadByIdCustom(id, ver, type) {
    // 用于判断用户是否已达到下载上限
    $.get("/Settings/Size/IsUserCanDownLoad?time=" + Math.random(), function(num) {

        if (parseInt(num) <= 0) {
            hiAlert('您下载的文档数量已达上限', '无法下载');
            return false;
        }
        else {

            if (num <= fs.PopDownLimitLength) hiOverAlert('您还可以下载文档的数量为: ' + num);

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //处理控件问题，如果有任何异常则禁止使用
            if (!showStartMsg()) { return false; }
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var url = ""; var preParam = "";
            if (type == "1") {
                url = "/Projects/ProjectResults/FileInfo/" + id + "?action=2"; preParam = "doc";
            }
            else if (type == "2") {
                url = "/ManagementStandards/Home/FileInfo/" + id + "?action=2"; preParam = "tech";
            }
            else if (type == "3") {
                url = "/ProductStorage/Series/FileInfo/" + id + "?action=2"; preParam = "product";
            }
            else if (type == "4") {
                url = "/CustomerStandards/Home/FileInfo/" + id + "?action=2"; preParam = "cus";
            }
            else if (type == "5") {
                url = "/SpecDoc/Home/FileInfo/" + id + "?action=2"; preParam = "spec";
            }
            else if (type == "6") {
                url = "/Persons/TasksCenter/FileInfo/" + id + "?action=2"; preParam = "newAttachVer";
            }
            else if (type == "7") {
                url = "/Expert/BidProjectInfo/FileInfo/" + id + "?action=2"; preParam = "BidAttachVer";
            }
            else if (type == "8") {
                url = "/Engineering/Home/FileInfo/" + id + "?action=2"; preParam = "PTAttachVer"; //
            }
            else if (type == "9") {
                url = "/FileServer/FileInfo/" + id + "?action=2"; preParam = "MatDoc"; //
            }
            else if (type == "10") {
                url = "/ContractManager/Home/FileInfo/" + id + "?action=2"; preParam = "conAjd"; //
            }
            else if (type == "11") {
                url = "/Persons/MailCenter/FileInfo/" + id + "?action=2"; preParam = "att"; //
            }
            else if (type == "12") {
                url = "/Marketing/CustOpportunity/FileInfo/" + id + "?action=2"; preParam = "custRefFiles"; //
            }
            else if (type == "13") {
                url = "/Evaluation/Comment/FileInfo/" + id + "?action=2"; preParam = "comAtt";
            }
            else if (type == "14") {
                url = "/Marketing/CustOpportunity/CmpFileInfo/" + id + "?action=2"; preParam = "cmpAtt";
            }
            else if (type == "15") {
                url = "/Training/Home/FileInfo/" + id + "?action=2"; preParam = "training";
            }
            else if (type == "16") {
                //工作流附件
                url = "/WorkFlowView/Home/FileInfo/" + id + "?action=2"; preParam = "flowAdj";
            }
            else if (type == "0") {
                //系统图档库
                url = "/Foundation/Home/FileInfo/" + id + "?action=2"; preParam = "sysObj"; //系统图档库只要不同已经存在的相同就好
            }
            else {
                url = "/CustomerAnalyze/Home/FileInfo/" + id + "?action=2"; preParam = "target";
            }

            $.post(url, { num: ver }, function(data) {
                //  if (data == "") { return false; }
                // data = eval("(" + data + ")");
                if (data.success) {
                    if (data.hash != "") {
                        //alert(1);
                        var filename = data.name;
                        if (filename.substr(filename.length - data.ext.toLowerCase().length, data.ext.toLowerCase().length) != data.ext.toLowerCase())
                        { filename = data.name + data.ext.toLowerCase(); }
                        fs.AddDownLoadTask(filename, data.hash);
                        updateDownloadCountCustom(id, type);
                    } else {
                        alert("文件可能正在传输或者没有上传完毕，暂时无法下载，请稍后出现文件大小之后再下载");
                    }
                } else {
                    alert(data.errors.msg);
                }
            }, "json");

        }
    });
}

//下载成功后,在下载记录表中新增一条数据
function updateDownloadCountCustom(id, type, downOrView) {
    //alert(type);
    var tableName = "";
    var referFieldName = "";
    var referFieldValue = id;

    if (type == "1") {
        tableName = "ProjectResultDoc";
        referFieldName = "projRetDocId";

        //url = "/Projects/ProjectResults/FileInfo/" + id + "?action=2"; preParam = "doc";
    }
    else if (type == "2") {
        tableName = "TargetAttachment";
        referFieldName = "docId";

        //url = "/ManagementStandards/Home/FileInfo/" + id + "?action=2"; preParam = "tech";
    }
    else if (type == "3") {
        tableName = "ProductAdjunct";
        referFieldName = "proAjdId";

        //url = "/ProductStorage/Series/FileInfo/" + id + "?action=2"; preParam = "product";
    }
    else if (type == "4") {
        tableName = "CusMagAttachment";
        referFieldName = "docId";

        //url = "/CustomerStandards/Home/FileInfo/" + id + "?action=2"; preParam = "cus";
    }
    else if (type == "5") {
        tableName = "SpecDocAttachment";
        referFieldName = "docId";

        //url = "/SpecDoc/Home/FileInfo/" + id + "?action=1"; preParam = "spec";
    }
    else if (type == "6") {
        tableName = "ProjLibAttachment";
        referFieldName = "docId";

        //url = "/Persons/TasksCenter/FileInfo/" + id + "?action=2"; preParam = "newAttachVer";
    }
    else if (type == "7") {
        tableName = "BidProjAttachment";
        referFieldName = "docId";

        //url = "/Expert/BidProjectInfo/FileInfo/" + id + "?action=2"; preParam = "BidAttachVer";
    }
    else if (type == "8") {
        tableName = "ProjTargetAdj";
        referFieldName = "targetAttId";

        //url = "/Engineering/Home/FileInfo/" + id + "?action=1"; preParam = "PTAttachVer"; //
    }
    else if (type == "9") {
        tableName = "MaterialDoc";
        referFieldName = "matDocId";

        //url = "/FileServer/FileInfo/" + id + "?action=1"; preParam = "MatDoc"; //
    }
    else if (type == "10") {
        tableName = "ContractAdjunct";
        referFieldName = "adjId";

        //url = "/ContractManager/Home/FileInfo/" + id + "?action=1"; preParam = "conAjd"; //
    }
    else if (type == "11") {
        tableName = "MailRefAttachment";
        referFieldName = "mailRefAttId";

        //url = "/Persons/MailCenter/FileInfo/" + id + "?action=1"; preParam = "att"; //
    }
    else if (type == "12") {
        tableName = "CustRefFile";
        referFieldName = "custRefFileId";

        //url = "/Marketing/CustOpportunity/FileInfo/" + id + "?action=1"; preParam = "custRefFiles"; //
    }
    else if (type == "13") {
        tableName = "CommentAttachment";
        referFieldName = "ComAttId";

        //url = "/Evaluation/Comment/FileInfo/" + id + "?action=1"; preParam = "comAtt";
    }
    else if (type == "14") {
        tableName = "CompetitorAttachment";
        referFieldName = "comAttId";

        //url = "/Marketing/CustOpportunity/CmpFileInfo/" + id + "?action=1"; preParam = "cmpAtt";
    }
    else if (type == "15") {
        tableName = "TrainingAttachment";
        referFieldName = "docId";

        //url = "/Training/Home/FileInfo/" + id + "?action=1"; preParam = "training";
    }
    else if (type == "16") {
        tableName = "AttributeAttachment";
        referFieldName = "caseDocId";

        //url = "/CustomerAnalyze/Home/FileInfo/" + id + "?action=2"; preParam = "target";
    }

    else if (type == "0") {
        tableName = "FileLibrary";
        referFieldName = "fileId";

        //url = "/CustomerAnalyze/Home/FileInfo/" + id + "?action=2"; preParam = "target";
    }
    if (downOrView == null || typeof (downOrView) == "undefined" || downOrView == 1) {
        $.get('/Settings/Size/UpdateDownloadCount/' + id + "?tname=" + tableName + "&rname=" + referFieldName + "&r=" + Math.random());

    }
    else {
        $.get('/Settings/Size/UpdateViewCount/' + id + "?tname=" + tableName + "&rname=" + referFieldName + "&r=" + Math.random());

    }


    return;
}

function updateFileCountCustom(id, type) {
    return;
}






function CancelFileUpload(P) {
    $.post(
        "/FileServer/RollbackFile",
        {
            param: P
        },
        function(data) { alert(data); });
}

function Listdown() {
    if ($("input[name=DownList]:checked").length == 0) { alert("请选择需要批量下载的文件!"); return false; }

    var p = '';
    if (IsYHNewFileServer == 0) {
        p = mutiselect.ShowPath();
        if (p == "") { return false; }
    }
    var downlist = [];
    $("input[name=DownList]:checked").each(function() {
        var name, guId, ext;
        name = $(this).attr("fname");
        guId = $(this).attr("guid");
        ext = $(this).attr("ext");
        if ($(this).attr("isnew") == "1") {
            downlist.push({ guid: guId, name: name + ext });
        }
        else if (mutiselect) {
            if (mutiselect.FileExists(p + "\\" + $(this).attr("fname") + $(this).attr("ext"))) {
                var t = Math.round(new Date().getTime() / 1000);
                if (confirm("该目录下已存在" + $(this).attr("fname") + $(this).attr("ext") + "这个文件，是否覆盖下载？(点击取消，文件将自动改名为" + $(this).attr("fname") + t + $(this).attr("ext") + "下载)"))
                { downloadObj.AddTask(0, $(this).attr("hash"), p + "\\" + $(this).attr("fname") + $(this).attr("ext")); }
                else
                { downloadObj.AddTask(0, $(this).attr("hash"), p + "\\" + $(this).attr("fname") + t + $(this).attr("ext")); }

            }
            else {
                downloadObj.AddTask(0, $(this).attr("hash"), p + "\\" + $(this).attr("fname") + $(this).attr("ext"));
            }
        }
        //UpdateDownView($(this).attr("view"));
    });
    if (IsYHNewFileServer == 1 && downlist.length) {
        cwf.dl(downlist);
    } else {
        $.tmsg("m_jfw", "下载开始！", { infotype: 1 });
    }
}


function DownLoadById(docid, vernum) {
    $.get('/Projects/ProjectResults/UpdateDownloadCount/' + docid + "?r=" + Math.random());
    DownLoadByIdCustom(docid, vernum, 1);
}

function updateDownloadCount(docid) {
    $.get('/Projects/ProjectResults/UpdateDownloadCount/' + docid + "?r=" + Math.random());
}
function updateFileCount(docid) {
    $.get('/Projects/ProjectResults/UpdateFileCount/' + docid + "?r=" + Math.random());
}

var IsReadPic = 0;
function ReadOnline(docid, vernum) {
    updateFileCount(docid);
    if ($("#showimg_" + docid).attr("href")) { $("#showimg_" + docid).click(); }
    else {
        ReadOnlineCustom(docid, vernum, 1);
    }
}


document.onkeydown = function(e) {
    if (e == null) {
        keycode = event.keyCode
    }
    else {
        keycode = e.which
    }
    if (keycode == 27) {
        $('#IV_window').remove();
        $('#IV_HideSelect').remove();
        $('#IV_overlay').remove();
        $('html').css('overflow', 'auto');
    }
};


function DownLoadAttById(id) {

    // 用于判断用户是否已达到下载上限
    $.get("/Settings/Size/IsUserCanDownLoad?time=" + Math.random(), function(num) {

        if (parseInt(num) <= 0) {
            hiAlert('您下载的文档数量已达上限', '无法下载');
            return false;
        }
        else {

            if (num <= fs.PopDownLimitLength) hiOverAlert('您还可以下载文档的数量为: ' + num);

            $.post("/FileServer/AttachMentInfo/" + id, {}, function(data) {
                //if (data == "") { return false; }
                //data = eval("(" + data + ")");
                if (data.success) {
                    if (data.hash != "") {
                        var filename = data.name;
                        fs.AddDownLoadTask(filename, data.hash);
                        updateDownloadCountCustom(id, "11");
                    } else {
                        alert("文档暂不能下载！");
                    }
                } else {
                    alert(data.errors.msg);
                }
            }, "json");
        }
    });
}

function ReadAttOnline(id) {
    $.post(
        "/FileServer/AttachMentInfo/" + id,
        {
    },
        function(data) {
            //if (data == "") { return false; }
            //data = eval("(" + data + ")");
            if (data.success) {
                if (data.hash != "") {
                    var filename = data.name;
                    fs.ReadFile(filename, data.hash);
                } else {
                    alert("文档暂不能查看！");
                }
            } else {
                alert(data.errors.msg);
            }
        }, "json"
   );
}

function ReadCustRefFileOnline(id) {
    ReadOnlineCustom(id, 0, 12);
    /**
    $.getJSON("/FileServer/CustRefFileInfo/" + id,
    function(data) {
    if (data.Success == true) {
    if (data.Record.hash != "") {
    var filename = data.Record.name;
    fs.ReadFile(filename, data.Record.hash);
    } else {
    alert("文档暂不能查看");
    }
    } else {
    alert(data.Message);
    }
    }
    );
    **/
}

function DownloadCustRefFile(id) {

    // 用于判断用户是否已达到下载上限
    $.get("/Settings/Size/IsUserCanDownLoad?time=" + Math.random(), function(num) {

        if (parseInt(num) <= 0) {
            hiAlert('您下载的文档数量已达上限', '无法下载');
            return false;
        }
        else {

            if (num <= fs.PopDownLimitLength) hiOverAlert('您还可以下载文档的数量为: ' + num);

            $.getJSON("/FileServer/CustRefFileInfo/" + id, function(data) {
                if (data.Success == true) {
                    if (data.Record.hash != "") {
                        var filename = data.Record.name;
                        fs.AddDownLoadTask(filename, data.Record.hash);
                        updateDownloadCountCustom(id, "12");
                    } else {
                        alert("文档暂不能下载");
                    }
                } else {
                    alert(data.Message);
                }
            });
        }
    });
}

function ReadMatDocOnline(id) {
    ReadOnlineCustom(id, 0, 9);
    /**
    $.getJSON("/FileServer/MatDoc/" + id,
    function(data) {
    if (data.Success == true) {
    if (data.Record.hash != "") {
    var filename = data.Record.name;
    fs.ReadFile(filename, data.Record.hash);
    } else {
    alert("文档暂不能下载");
    }
    } else {
    alert(data.Message);
    }
    }
    );
    **/
}
function DownloadMatDoc(id) {

    // 用于判断用户是否已达到下载上限
    $.get("/Settings/Size/IsUserCanDownLoad?time=" + Math.random(), function(num) {

        if (parseInt(num) <= 0) {
            hiAlert('您下载的文档数量已达上限', '无法下载');
            return false;
        }
        else {

            if (num <= fs.PopDownLimitLength) hiOverAlert('您还可以下载文档的数量为: ' + num);

            $.getJSON("/FileServer/MatDoc/" + id, function(data) {
                if (data.Success == true) {
                    if (data.Record.hash != "") {
                        var filename = data.Record.name;
                        fs.AddDownLoadTask(filename, data.Record.hash);
                        updateDownloadCountCustom(id, "9");
                    } else {
                        alert("文档暂不能下载");
                    }
                } else {
                    alert(data.Message);
                }
            });
        }
    });
}



function ReadOnlineNew(docid) { 
    updateFileCount(docid);
    ReadOnlineCustom(docid, 0, -1);
}
function New_ReadOnlineNew(docid, isnew, guid, name, ext, thumbPicPath) {
    if (isnew == 0) {
        ReadOnlineNew(docid);
        return;
    }
    updateFileCount(docid);

    var swfPath = thumbPicPath.replace("_m.jpg", "_sup.swf");
    var supPath = thumbPicPath.replace("_m.", "_sup.");
    cwf.readOnline({ "guid": guid, "name": name, "ext": ext, "id": docid, "type": 0, "ver": 1, "swfUrl": swfPath, "imgUrl": supPath });
}
function New_DownLoadAttByIdNew(id, isnew, guid, name, ext) {
    if (isnew == 0) {
        DownLoadAttByIdNew(id);
        return;
    }
    // 用于判断用户是否已达到下载上限
    $.get("/Settings/Size/IsUserCanDownLoad?time=" + Math.random(), function(num) {

        if (parseInt(num) <= 0) {
            hiAlert('您下载的文档数量已达上限', '无法下载');
            return false;
        }
        else {

            if (num <= fs.PopDownLimitLength) hiOverAlert('您还可以下载文档的数量为: ' + num);

            $.post("/FileServer/AttributeAttachment/" + id, {}, function(data) {
                //if (data == "") { return false; }
                //data = eval("(" + data + ")");
                if (data.Success) {
                    if (data.Record.hash != "") {
                        var filename = data.Record.name;
                        cwf.downLoad(guid ,filename ,id,15);
                        updateDownloadCountCustom(id, "16");
                    } else {
                        alert("文档暂不能下载！");
                    }
                } else {
                    alert(data.errors.msg);
                }
            }, "json");

        }
    });
}
function DownLoadAttByIdNew(id) {
    // 用于判断用户是否已达到下载上限
    $.get("/Settings/Size/IsUserCanDownLoad?time=" + Math.random(), function(num) {

        if (parseInt(num) <= 0) {
            hiAlert('您下载的文档数量已达上限', '无法下载');
            return false;
        }
        else {

            if (num <= fs.PopDownLimitLength) hiOverAlert('您还可以下载文档的数量为: ' + num);

            $.post("/FileServer/AttributeAttachment/" + id, {}, function(data) {
                //if (data == "") { return false; }
                //data = eval("(" + data + ")");
                if (data.Success) {
                    if (data.Record.hash != "") {
                        var filename = data.Record.name;
                        fs.AddDownLoadTask(filename, data.Record.hash);
                        updateDownloadCountCustom(id, "16");
                    } else {
                        alert("文档暂不能下载！");
                    }
                } else {
                    alert(data.errors.msg);
                }
            }, "json");

        }
    });
}


function readfileProgress(fProgress, strFileName, ullFileSize) {
    var pos = strFileName.lastIndexOf("\\");
    var dot = strFileName.lastIndexOf(".");
    strFileName = strFileName.substring(pos + 1, dot);


    //var FileSize = parseInt(ullFileSize / 10.24)/100 + "KB";
    //if (parseInt(FileSize) > 1024) { FileSize = parseInt(parseInt(FileSize) / 10.24)/100 + "MB"; }
    //if (parseInt(FileSize) > 1024) { FileSize = parseInt(parseInt(FileSize) / 10.24)/100 + "G"; }
    //hiOverAlert("文件已下载" + parseInt(fProgress * 100) + "%");
    if (!document.getElementById("fProgressDiv")) {
        var html = '<div class="shadow-container2" id="fProgressDiv" style="position:absolute; display:none">';
        html += '</div>';
        $(document.body).append(html);
        initSignalBox('fProgressDiv');
    }
    var html = '';
    html += '<div class="title">';
    html += '<div class="name">下载进度</div>';
    html += '<div class="oper"><a href="#"></a></div>';
    html += '</div>';
    html += '<div class="contain">';
    html += '<div style="border:1px solid #a7c5e2; padding:20px 10px; background-color:#FFFFFF">';
    html += '<div style="font-weight:bold; padding:0 20px 5px 40px; line-height:16px;word-wrap:break-word;">' + strFileName + '</div>';
    html += '<div style="height:20px; padding-left:40px">';
    html += '<div class="loading"><div class="bar" style="width:' + parseInt(fProgress * 100) + '%' + '"></div></div>';
    html += '<div style="float:left; line-height:16px">' + parseInt(fProgress * 100) + '%</div>';
    html += '</div>';
    //html += '<div style="clear:both; color:#798699; line-height:16px; padding-left:40px">文件大小: ' + FileSize + '&nbsp;&nbsp; &nbsp;&nbsp;</div>'; //速度:10KB&nbsp;&nbsp;<strong>/ </strong>S
    html += '</div>';
    html += '</div>';
    html += '<div class="bottom"></div>';
    $("#fProgressDiv").html(html);
    showBox("fProgressDiv");
    if (fProgress == 1) { HideBox(this, "fProgressDiv"); $("#fProgressDiv").remove(); }
}

function readfileStart(filename) {
    hiOverAlert("开始下载文件：" + filename);
}


function switchDownloadValue(retId) {
    var url = "/Standard/StandardResult/GetFileList?retId=" + retId + "&filetype=2&r" + Math.random();
    var html = "";
    $.get(url, function(data) {
        if (data.length == 0) { $.tmsg("m_jfw", "该标准还没有上传数据包", { infotype: 2 }); return false; }
        if (data.length == 1) { eval(data[0].getClientDownLoad); } else {
        for (var x = 0; x < data.length; x++) {
            html += "<div><input name=DownList isnew='" + data[x].isNewFileServer + "'guid='"+data[x].guid+"' hash='" + data[x].hash + "' fname='" + data[x].name + "' ext='" + data[x].ext + "' type=checkbox checked>" + data[x].name + " <a href='javascript:;' onclick='" + data[x].getClientOnlineRead + "'>查看</a> <a href='javascript:;' onclick=\"" + data[x].getClientDownLoad + "\">下载</a></div>"
            }
            box("<div style='padding:10px'><div>该标准包含多个文件，请选择需要的文件进行下载</div>" + html + "</div>", { boxid: 'DownLoadFiles', contentType: 'html', submit_BtnName: '批量下载选定文件', title: '下载标准完整数据包',
                submit_cb: function(o) {
                    Listdown();
                }
            });
        }
    });
}
