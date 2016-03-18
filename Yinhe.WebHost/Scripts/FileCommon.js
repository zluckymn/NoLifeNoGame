
var upCallbackFunc = null;
function UploadFiles(o, isMultiply, callbackFunc) {
    upCallbackFunc = callbackFunc;
    if ($(o).attr("fileObjId")) {
        $("#fileObjId").val($(o).attr("fileObjId"));
    }
    if ($(o).attr("fileTypeId")) {
        $("#fileTypeId").val($(o).attr("fileTypeId"));
    }
    if ($(o).attr("keyValue")) {
        $("#keyValue").val($(o).attr("keyValue"));
    }
    if ($(o).attr("tableName")) {
        $("#tableName").val($(o).attr("tableName"));
    }
    if ($(o).attr("keyName")) {
        $("#keyName").val($(o).attr("keyName"));
    }
    if ($(o).attr("uploadType")) {
        $("#uploadType").val($(o).attr("uploadType"));
    }
    if ($(o).attr("fileRel_profId")) {
        if (typeof $("#fileRel_profId").val() != "undefined") {
            $("#fileRel_profId").val($(o).attr("fileRel_profId"));
        }
    }
    if ($(o).attr("fileRel_stageId")) {
        if (typeof $("#fileRel_stageId").val() != "undefined") {
            $("#fileRel_stageId").val($(o).attr("fileRel_stageId"));
        }
    }
    if ($(o).attr("fileRel_fileCatId")) {
        if (typeof $("#fileRel_fileCatId").val() != "undefined") {
            $("#fileRel_fileCatId").val($(o).attr("fileRel_fileCatId"));
        }
    }
    if (isMultiply == "true") {
        cwf.up("p=multi", SelectFileCallback, o);
    }
    else if (isMultiply == "false") {
        cwf.upone("p=multi", SelectFileCallback, o);
    }
}

function NewUploadFiles(o, isMultiply, ext, callbackFunc) {
    upCallbackFunc = callbackFunc;
    if ($(o).attr("fileObjId")) {
        $("#fileObjId").val($(o).attr("fileObjId"));
    }
    if ($(o).attr("fileTypeId")) {
        $("#fileTypeId").val($(o).attr("fileTypeId"));
    }
    if ($(o).attr("keyValue")) {
        $("#keyValue").val($(o).attr("keyValue"));
    }
    if ($(o).attr("tableName")) {
        $("#tableName").val($(o).attr("tableName"));
    }
    if ($(o).attr("keyName")) {
        $("#keyName").val($(o).attr("keyName"));
    }
    if ($(o).attr("uploadType")) {
        $("#uploadType").val($(o).attr("uploadType"));
    }

    if (isMultiply == "true") {
        cwf.up("p=multi", SelectFileCallback, o);
    }
    else if (isMultiply == "false") {
        cwf.upone("p=one|typeFilter|" + "*" + ext, SelectFileCallback, o);
    }
}

function SelectFileCallback(uuid) {
    var propertyStr = "&";
    var fileObjId = $("#fileObjId").val();
    var fileTypeId = $("#fileTypeId").val();
    var keyValue = $("#keyValue").val();
    var tableName = $("#tableName").val();
    var keyName = $("#keyName").val();
    var fileRel_profId = $("#fileRel_profId").val();
    var fileRel_stageId = $("#fileRel_stageId").val();
    var fileRel_fileCatId = $("#fileRel_fileCatId").val();
    var uploadType = "";

    if ($("#uploadType")) {
        uploadType = $("#uploadType").val();
    }

    $("input[name^='Property_']").each(function (i, val) {
        var prop = $(this).attr("name") + "=" + $(this).attr("value") + "&";
        propertyStr += prop;
    });

    var d = cwf.tasks.getData(uuid);
    var cb = $("#CallBack").val();
    var cbBtn = $("#CallBack");
    var i = 0;
    var len = d.length;
    var str = "";
    var o;

    var NewUserFilePath = "";

    var splitStr = "|H|";

    for (; i < len; ++i) {
        o = d[i];
        if (NewUserFilePath == "") {
            NewUserFilePath = o.nativePath;
        }
        else {
            NewUserFilePath += splitStr + o.nativePath;
        }
        NewUserFilePath += ("|Y|" + o.rootDir);

    }
    var dataStr = "fileObjId=" + fileObjId;
    dataStr += "&fileTypeId=" + fileTypeId;
    dataStr += "&uploadFileList=" + encodeURIComponent(NewUserFilePath);
    dataStr += "&tableName=" + tableName;
    dataStr += "&keyName=" + keyName;
    dataStr += "&keyValue=" + keyValue;
    dataStr += "&uploadType=" + uploadType;
    dataStr += "&fileRel_profId=" + fileRel_profId;
    dataStr += "&fileRel_stageId=" + fileRel_stageId;
    dataStr += "&fileRel_fileCatId=" + fileRel_fileCatId;
    $("input[fileattributes=true]").each(function () {
        if ($(this).val() != null) {
            dataStr += "&" + $(this).attr("name") + "=" + $(this).val();
            $(this).val(null);
        }
    });
    var arr;
    $.ajax({
        url: '/home/SaveMultipleUploadFiles',
        type: 'post',
        data: dataStr + propertyStr,
        dataType: 'html',
        error: function () {
            //top.showInfoBar("添加失败");
            alert("添加失败");
            //$(document.body).css('overflow', '');
            //$("#mask").remove();
        },
        success: function (data) {
            var str = data.split("|");
            var result = eval("(" + str[0] + ")");
            var fileIdList = "";
            if (result.success) {
                if (str.length > 1) {
                    if (str[1] != "") {
                        var files = eval("(" + str[1] + ")");
                        cwf.tasks.send(uuid, files, function () {
                            //$(document.body).css('overflow', '');
                            //$("#mask").remove();

                            if (cb != "" && typeof cb != "undefined") {
                                cbBtn.click();
                            } else {
                                if (upCallbackFunc != null) {
                                    upCallbackFunc();

                                    upCallbackFunc = null;
                                }
                                else {
                                    window.location.reload();
                                }
                            }

                        });

                    }
                }
            }


        }
    });
}

function DeleteFiles(ids, func, param) {
    if (confirm("确定要删除该对象吗？删除后将无法恢复！")) {
        $.ajax({
            url: "/Home/DeleFiles",
            type: 'post',
            data: { delFileRelIds: ids },
            dataType: 'json',
            error: function () {
                alert('未知错误，请联系服务器管理员，或者刷新页面重试', '保存失败');
            },
            success: function (data) {
                //alert(data);
                if (data.Success == false) {
                    alert(data.msgError, '删除失败');
                }
                else {
                    if (func) {
                        if (typeof param != "undefined") {
                            func(param);
                        } else {
                            func();
                        }
                    } else {
                        window.location.reload();
                    }
                }
            }
        });
    }

}

function DeleteFiles1(ids, func) {
    $.ajax({
        url: "/Home/DeleFiles",
        type: 'post',
        data: { delFileRelIds: ids },
        dataType: 'json',
        error: function () {
            alert('未知错误，请联系服务器管理员，或者刷新页面重试', '保存失败');
        },
        success: function (data) {
            //alert(data);
            if (data.Success == false) {
                alert(data.msgError, '删除失败');
            }
            else {
                if (func) {
                    func();
                } else {
                    window.location.reload();
                }
            }
        }
    });


}
function DeleteFilesRelation(ids, func) {

    if (confirm("确定要删除该对象吗？删除后将无法恢复！")) {
        $.ajax({
            url: "/Home/DeleCaseFileRelations",
            type: 'post',
            data: { delFileRelIds: ids },
            dataType: 'json',
            error: function () {
                alert('未知错误，请联系服务器管理员，或者刷新页面重试', '保存失败');
            },
            success: function (data) {
                if (data.Success == false) {
                    alert(data.msgError, '删除失败');
                }
                else {
                    if (data.FileInfo != null && data.FileInfo != "") {
                        DeleteFiles1(ids, func);
                    }
                    else {
                        if (func) {
                            func();
                        } else {

                            window.location.reload();
                        }
                    }

                }
            }
        });
    }
}

function UploadFilesAndSave(o, isMultiply) {
    $("#fileObjId").val($(o).attr("fileObjId"));
    $("#fileTypeId").val($(o).attr("fileTypeId"));
    $("#keyValue").val($(o).attr("keyValue"));
    $("#tableName").val($(o).attr("tableName"));
    $("#keyName").val($(o).attr("keyName"));
    if (isMultiply == "true") {
        cwf.up("p=multi", SelectFileCallbackAndSave, o);
    }
    else if (isMultiply == "false") {
        cwf.upone("p=multi", SelectFileCallbackAndSave, o);
    }
}

function SelectFileCallbackAndSave(uuid) {  //
    var fileObjId = $("#fileObjId").val();
    var fileTypeId = $("#fileTypeId").val();
    var keyValue = $("#keyValue").val();
    var tableName = $("#tableName").val();
    var keyName = $("#keyName").val();
    var d = cwf.tasks.getData(uuid);
    var i = 0;
    var len = d.length;
    var str = "";
    var o;
    // alert();
    //$(document.body).css('overflow', 'hidden');
    //var mask = '<div style="margin-top:25%; text-align:center; background:url(/Content/Images/zh-cn/bg.jpg) no-repeat center top; height:86px">';
    //mask += '<table height="95"><tr><td width="25"><img src="/Content/Images/zh-cn/loading.gif" width="20" height="20" /></td><td style="font-size:14px; font-weight:bold; color:#DCDCDC">正在保存...</td></tr></table>';
    //mask += '</div>';
    //$(document.body).append('<div id=mask style="filter:alpha(opacity=50); position:absolute; top:0px; left:0px; height:100%; width:100%; background-color:Black">' + mask + '</div>');

    var NewUserFilePath = "";

    var splitStr = "|H|";

    for (; i < len; ++i) {
        o = d[i];
        if (NewUserFilePath == "") {
            NewUserFilePath = o.nativePath;
        }
        else {
            NewUserFilePath += splitStr + o.nativePath;
        }
        NewUserFilePath += ("|Y|" + o.rootDir);

    }
    $("#uploadFileList").val(NewUserFilePath);
    var html = "";
    var a = NewUserFilePath.split("|H|");
    for (var i = 0; i < d.length; i++) {
        var fileName = "";
        var fileExt = "";
        //var b = a[i].split("|Y|");
        //var path = b[0];
        //var index = path.lastIndexOf("\\") + 1;
        fileName = d[i].name;
        fileExt = d[i].type || '';
        fileName = fileName.replace(fileExt, '');
        //if (fileArr.length >= 2) {
        html += '<tr>';
        html += '<td><div class="tb_con">' + fileName;
        html += '</div></td>';
        html += '<td><div class="tb_con">' + fileExt;
        html += '</div></td>';
        html += '<td><div class="tb_con">' + d[i].fileSizeCalc;
        html += '</div></td>';
        html += '<td><div class="tb_con">';
        html += '</div></td>'
        html += '</tr>';
        //}


    }
    var AppendTable = $(".table01:visible:first");
    if (fileTypeId != 0) {
        AppendTable = $("table[filetypeid=" + fileTypeId + "]");
    }
    AppendTable.find("tbody").html(html);
    //$("#mask").remove();

}

function DeleteFilesAndSave(fileRetId, o) {
    var delIds = $("#delFileRelIds").val();
    delIds = delIds + fileRetId + ",";
    $("#delFileRelIds").val(delIds);
    //alert($("#delFileRelIds").val());
    $(o).parent().parent().parent().remove();
}

function DeleteFolderByStructId(structId) {
    if (confirm("确定要删除该对象吗？删除后将无法恢复！")) {
        $.ajax({
            url: "/home/DeleFolder",
            type: 'post',
            data: { delstructIds: structId },
            dataType: 'json',
            error: function () {
                alert('未知错误，请联系服务器管理员，或者刷新页面重试', '保存失败');
            },
            success: function (data) {
                //alert(data);
                if (data.Success == false) {
                    alert(data.msgError, '删除失败');
                }
                else {
                    window.location.reload();
                }
            }
        });
    }
}

function DeleteFileByFileId(fileId, func) {
    if (confirm("确定要删除该对象吗？删除后将无法恢复！")) {
        $.ajax({
            url: "/Home/DeleFilesByFileId",
            type: 'post',
            data: { delFileIds: fileId },
            dataType: 'json',
            error: function () {
                alert('未知错误，请联系服务器管理员，或者刷新页面重试', '保存失败');
            },
            success: function (data) {
                //alert(data);
                if (data.Success == false) {
                    alert(data.msgError, '删除失败');
                }
                else {
                    if (func) {
                        func();
                    } else {
                        window.location.reload();
                    }
                }
            }
        });
    }
}


function Listdown() {
    if ($("input[name=downList]:checked").length == 0) { alert("请选择需要批量下载的文件!"); return false; }
    var downlist = [];
    $("input[name=downList]:checked").each(function () {

        var name, guId;
        name = $(this).attr("fname");
        guId = $(this).attr("guid");
        downlist.push({ guid: guId, name: name });
    });
    if (downlist.length) {
        cwf.dl(downlist);
    }
    // $.tmsg("m_jfw", "下载开始！", { infotype: 1 });
}

function DeleteFileByFileIds(ids) {
    if (confirm("确定要删除该对象吗？删除后将无法恢复！")) {
        $.ajax({
            url: "/Home/DeleFilesByFileId",
            type: 'post',
            data: { delFileIds: ids },
            dataType: 'json',
            error: function () {
                alert('未知错误，请联系服务器管理员，或者刷新页面重试', '保存失败');
            },
            success: function (data) {
                if (data.Success == false) {
                    alert(data.msgError, '删除失败');
                }
                else {
                    window.location.reload();
                }
            }
        });
    }
}

function DeleteFileVersionByFileVerIds(ids) {
    if (confirm("确定要删除该对象吗？删除后将无法恢复！")) {
        $.ajax({
            url: "/Home/DeleFilesByFileVerId",
            type: 'post',
            data: { delFileIds: ids },
            dataType: 'json',
            error: function () {
                alert('未知错误，请联系服务器管理员，或者刷新页面重试', '保存失败');
            },
            success: function (data) {
                if (data.Success == false) {
                    alert(data.msgError, '删除失败');
                }
                else {
                    window.location.reload();
                }
            }
        });
    }
}


function UploadNewVersionFiles(o) {
    if ($(o).attr("fileId")) {
        $("#fileId").val($(o).attr("fileId"));
    }
    cwf.upone("p=multi", SelectFileNewVersionCallback, o);


}

function NewUploadNewVersionFiles(o, ext) {
    if ($(o).attr("fileId")) {
        $("#fileId").val($(o).attr("fileId"));
    }
    cwf.upone("p=multi|typeFilter|" + "*" + ext, SelectFileNewVersionCallback, o);
}

function SelectFileNewVersionCallback(uuid) {

    var fileId = $("#fileId").val();

    var d = cwf.tasks.getData(uuid);

    var i = 0;
    var len = d.length;
    var str = "";
    var o;

    var NewUserFilePath = "";

    var splitStr = "|H|";

    for (; i < len; ++i) {
        o = d[i];
        if (NewUserFilePath == "") {
            NewUserFilePath = o.nativePath;
        }
        else {
            NewUserFilePath += splitStr + o.nativePath;
        }


    }

    var dataStr = "fileId=" + fileId;
    dataStr += "&uploadFileList=" + encodeURIComponent(NewUserFilePath);

    var arr;
    $.ajax({
        url: '/home/SaveNewVersion',
        type: 'post',
        data: dataStr,
        dataType: 'html',
        error: function () {

            alert("添加失败");

        },
        success: function (data) {
            var str = data.split("|");
            var result = eval("(" + str[0] + ")");
            var fileIdList = "";
            if (result.success) {
                if (str.length > 1) {
                    if (str[1] != "") {
                        var files = eval("(" + str[1] + ")");
                        cwf.tasks.send(uuid, files, function () {
                            //$(document.body).css('overflow', '');
                            //$("#mask").remove();
                            window.location.reload();

                        });

                    }
                }
            }


        }
    });
}

var fileTableId;
var uploadUUID;
var FileName, FileEXT, FileSize, FilePath,FileMathNum;
function UploadFilesNew(o, isMultiply, tableId, callbackFunc, param1, param2) {
    upCallbackFunc = callbackFunc;
    fileTableId = tableId;
    if ($(o).attr("fileObjId")) {
        $("#fileObjId").val($(o).attr("fileObjId"));
    }
    if ($(o).attr("fileTypeId")) {
        $("#fileTypeId").val($(o).attr("fileTypeId"));
    }
    if ($(o).attr("keyValue")) {
        $("#keyValue").val($(o).attr("keyValue"));
    }
    if ($(o).attr("tableName")) {
        $("#tableName").val($(o).attr("tableName"));
    }
    if ($(o).attr("keyName")) {
        $("#keyName").val($(o).attr("keyName"));
    }
    if ($(o).attr("uploadType")) {
        $("#uploadType").val($(o).attr("uploadType"));
    }
    if ($(o).attr("fileRel_profId")) {
        if (typeof $("#fileRel_profId").val() != "undefined") {
            $("#fileRel_profId").val($(o).attr("fileRel_profId"));
        }
    }
    if ($(o).attr("fileRel_stageId")) {
        if (typeof $("#fileRel_stageId").val() != "undefined") {
            $("#fileRel_stageId").val($(o).attr("fileRel_stageId"));
        }
    }
    if ($(o).attr("fileRel_fileCatId")) {
        if (typeof $("#fileRel_fileCatId").val() != "undefined") {
            $("#fileRel_fileCatId").val($(o).attr("fileRel_fileCatId"));
        }
    }
    if (isMultiply == "true") {
         
        cwf.up("p=multi", SelectFileCallbackNew);
    }
    else if (isMultiply == "false") {
        cwf.upone("p=multi", SelectFileCallbackNew);
    }
}
//格式控制
function UploadExtFilesNew(o, isMultiply, tableId, ext, callbackFunc, param1, param2) {
    upCallbackFunc = callbackFunc;
    fileTableId = tableId;
    if ($(o).attr("fileObjId")) {
        $("#fileObjId").val($(o).attr("fileObjId"));
    }
    if ($(o).attr("fileTypeId")) {
        $("#fileTypeId").val($(o).attr("fileTypeId"));
    }
    if ($(o).attr("keyValue")) {
        $("#keyValue").val($(o).attr("keyValue"));
    }
    if ($(o).attr("tableName")) {
        $("#tableName").val($(o).attr("tableName"));
    }
    if ($(o).attr("keyName")) {
        $("#keyName").val($(o).attr("keyName"));
    }
    if ($(o).attr("uploadType")) {
        $("#uploadType").val($(o).attr("uploadType"));
    }
    if ($(o).attr("fileRel_profId")) {
        if (typeof $("#fileRel_profId").val() != "undefined") {
            $("#fileRel_profId").val($(o).attr("fileRel_profId"));
        }
    }
    if ($(o).attr("fileRel_stageId")) {
        if (typeof $("#fileRel_stageId").val() != "undefined") {
            $("#fileRel_stageId").val($(o).attr("fileRel_stageId"));
        }
    }
    if ($(o).attr("fileRel_fileCatId")) {
        if (typeof $("#fileRel_fileCatId").val() != "undefined") {
            $("#fileRel_fileCatId").val($(o).attr("fileRel_fileCatId"));
        }
    }
    if (isMultiply == "true") {

        cwf.up("p=multi", SelectFileCallbackNew);
    }
    else if (isMultiply == "false") {
        cwf.upone("p=one|typeFilter|" + "*" + ext, SelectFileCallbackNew);
    }
}
function SelectFileCallbackNew(uuid) {
  $("#uploadUUID").val(uuid);
    uploadUUID = uuid;
    var propertyStr = "&";
    var fileObjId = $("#fileObjId").val();
    var fileTypeId = $("#fileTypeId").val();
    var keyValue = $("#keyValue").val();
    var tableName = $("#tableName").val();
    var keyName = $("#keyName").val();
    var fileRel_profId = $("#fileRel_profId").val();
    var fileRel_stageId = $("#fileRel_stageId").val();
    var fileRel_fileCatId = $("#fileRel_fileCatId").val();
    var uploadType = "";

    if ($("#uploadType")) {
        uploadType = $("#uploadType").val();
    }

    $("input[name^='Property_']").each(function (i, val) {
        var prop = $(this).attr("name") + "=" + $(this).attr("value") + "&";
        propertyStr += prop;
    });

    var d = cwf.tasks.getData(uuid);
    var cb = $("#CallBack").val();
    var cbBtn = $("#CallBack");
    var i = 0;
    var len = d.length;
    var str = "";
    var o;

    var NewUserFilePath = "";
    if ($("#uploadFileList")) {
        NewUserFilePath = $("#uploadFileList").val();
    }
    var splitStr = "|H|";
    
    for (; i < len; ++i) {
        o = d[i];
        FileMathNum = parseInt(Math.random() * 10000000);
        if (NewUserFilePath == "") {
            NewUserFilePath = o.nativePath;
        }
        else {
            NewUserFilePath += splitStr + o.nativePath;
        }
        NewUserFilePath += ("|N|"+FileMathNum+"|Y|" + o.rootDir)
        NewUserFilePath += ("|Y|fileObjId|-|" + fileObjId + "|N|fileTypeId|-|" + fileTypeId + "|N|uploadType|-|" + uploadType + "|N|keyValue|-|"+keyValue +"|N|keyName|-|"+keyName+"|N|tableName|-|"+tableName+ "|N|isCover|-|No");
        FileName = d[i].name;
        FileEXT = d[i].type || '';
        FilePath = o.nativePath + "|N|" + FileMathNum + "|Y|" + o.rootDir + "|Y|fileObjId|-|" + fileObjId + "|N|fileTypeId|-|" + fileTypeId + "|N|uploadType|-|" + uploadType + "|N|keyValue|-|" + keyValue + "|N|keyName|-|" + keyName + "|N|tableName|-|" + tableName + "|N|isCover|-|No";
        FileName = FileName.replace(FileEXT, '');
        FileSize = d[i].fileSizeCalc;
        addFileTr();
    }
    if ($("#uploadFileList")) {
        $("#uploadFileList").val(NewUserFilePath);
    }
//    for (var i = 0; i < d.length; i++) {
//       
//    }

}

function addFileTr() {
    var html = "";
    html += '<tr><td><div class="tb_con"><input type="checkbox" fileName="fileInputNo" fileRelId="'+FilePath+'"/></div></td>'
    html += '<td><div class="tb_con"> <a href="javascript:void(0);" class="gray" onclick=\'alertRead()\'>';
    html += '<img src="/Content/images/icon/error.png" style="visibility: inherit" />'
    html += FileName;
    html += '</a></div></td><td align="center"><div class="tb_con">';
    html += FileEXT;
    html += '</div></td> <td align="center"><div class="tb_con">';
    html += FileSize;
    html += '</div></td><td align="center"><div class="tb_con">';
    if (fileTableId.indexOf("cover") != -1) {
        html += '<input type="radio" name="imgFirst" value="1" fileTableId="' + fileTableId + '" onclick=\'SetCoverImageNew("' + FileMathNum + '",this);\' />设置封面图 &nbsp;'
    } html += ' <a href="javascript:void(0);" class="gray" onclick=\'alertRead()\'>阅读</a> '
    html += '<a href="javascript:void(0);"class="gray" onclick=\'alertDown();\'> 下载</a>';
    html += ' <a href="javascript:void(0);" class="red" Num="'+ FileMathNum+'" path="'+FilePath+'" onclick=\'delFileRow(this)\'>删除</a>';
    html += '</div></td></tr>';
    $("#" + fileTableId).find('tr:last').after(html);
    

}


function alertDown() {
    alert("请先保存后再下载!");
}
function alertRead() {
    alert("请先保存后再阅读!");
}
function delFileRow(obj) {
    $(obj).parent().parent().parent().remove();
    var tempPath = $(obj).attr("path");
    var tempUploadFileList=$("#uploadFileList").val();
    if (tempUploadFileList.indexOf(tempPath + "|H|") != -1) {
        $("#uploadFileList").val(tempUploadFileList.replace(tempPath + "|H|", ""));
    }
    else if (tempUploadFileList.indexOf(tempPath ) != -1) {
        $("#uploadFileList").val(tempUploadFileList.replace(tempPath, ""));
    }
    //alert($("#uploadFileList").val())
}
function SetCoverImageNew(id, obj) {
    var tempTableId = $(obj).attr("fileTableId");
    var tempPath = $(obj).parent().find("a[Num=" + id + "]").attr("path");
    if ($(obj).attr("checked") == true) {
        $(obj).parent().find("a[Num=" + id + "]").attr("path", tempPath.replace("isCover|-|No", "isCover|-|Yes"));
        $(obj).parent().parent().parent().find("input[fileName=fileInputNo]").attr("fileRelId", tempPath.replace("isCover|-|No", "isCover|-|Yes"));
        if ($("#uploadFileList").val().indexOf(tempPath) != -1) {
            $("#uploadFileList").val($("#uploadFileList").val().replace(tempPath, $(obj).parent().find("a[Num=" + id + "]").attr("path")));
        }
        alert("设置成功！");
       
    }
    else if ($(obj).attr("checked") == false) {
        $(obj).parent().find("a[Num=" + id + "]").attr("path", tempPath.replace("isCover|-|Yes", "isCover|-|No"));
        $(obj).parent().parent().parent().find("input[fileName=fileInputNo]").attr("fileRelId", tempPath.replace("isCover|-|Yes", "isCover|-|No"));
        if ($("#uploadFileList").val().indexOf(tempPath) != -1) {
            $("#uploadFileList").val($("#uploadFileList").val().replace(tempPath, $(obj).parent().find("a[Num=" + id + "]").attr("path")));
        }
        alert("设置成功！");
     }
 }





