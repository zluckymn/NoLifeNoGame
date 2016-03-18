function SelectFiles() {
    var str = selFile.SelectFiles();
    selFile.AddFiles(str);
}

function SelectFilesEx(title, bPreThumb, bPreDefault, bSpliteDwg) {
//处理控件问题，如果有任何异常则禁止使用
if (!showStartMsg()) {return false;}
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    var str = selFile.SelectFilesEx(title, bPreThumb, bPreDefault, bSpliteDwg);
}

function Delete() {
    selFile.DeleteSelectItem();
}

function Clear() {
    selFile.Clear();
}

function GetFileList() {
    alert(selFile.GetFileList());
}

var CoverTitleImg = "";

function selFile::OnChangePreFile(strFile) { CoverTitleImg = strFile; }

function selFile::OnSaveFileList(strFile) { 
    //alert("选择的文件列表为：" + strFile);
    if(strFile==""){
        return false;
    }
    $(document.body).css('overflow', 'hidden');
    var mask = '<div style="margin-top:25%; text-align:center; background:url(/Content/Images/zh-cn/bg.jpg) no-repeat center top; height:86px">';
    mask += '<table height="95"><tr><td width="25"><img src="/Content/Images/zh-cn/loading.gif" width="20" height="20" /></td><td style="font-size:14px; font-weight:bold; color:#DCDCDC">正在保存...</td></tr></table>';
    mask += '</div>';
    $(document.body).append('<div id=mask style="filter:alpha(opacity=50); position:absolute; top:0px; left:0px; height:100%; width:100%; background-color:Black">' + mask + '</div>');

    var NewUserFilePath = "";

    var splitStr = "|H|";
    
    var NewUserFileArr = strFile.split("?");
    var Cover="";
    var IsCheck = 0;

    for(var x=0;x<NewUserFileArr.length;x++){
    if(NewUserFileArr[x]!=""){
        if (NewUserFilePath == "")
        { NewUserFilePath = NewUserFileArr[x].split("|")[0]; }
        else
        { NewUserFilePath += splitStr + NewUserFileArr[x].split("|")[0]; }
    }
    }
    //alert("Cover:" + Cover + "&NewSysFileId:" + NewSysFileId + "&NewSysFilePath:" + NewSysFilePath + "&NewSysFileIsCut:" + NewSysFileIsCut + "&NewUserFilePath:" + NewUserFilePath + "&NewUserFileCover:" + NewUserFileCover + "&NewUserFileCut:" + NewUserFileCut);
    //return false;
    var bookPageId = $("#bookPageId").val();
    var arr = NewUserFilePath.split(splitStr);
    for (var x = 0; x < arr.length; x++) {
        if (arr[x].length > 0) {
            if (!mutiselect.FileExists(arr[x])) {
                alert("文件：" + arr[x] + "不存在！请检查后重新选择"); $(document.body).css('overflow', '');
                $("#mask").remove(); return false;
            } 
        }
    }
  
    $.ajax({
        url: '/MissionStatement/Home/UploadAttach/',
        type: 'post',
        data: {
            bookPageId: bookPageId,
            vCover: Cover,
            uploadFileList: NewUserFilePath
        },
        dataType: 'html',
        error: function() {
            top.showInfoBar("添加失败");
            $(document.body).css('overflow', '');
            $("#mask").remove();
        },
        success: function(data) {
 
            var str = data.split("|");
            var result = eval("(" + str[0] + ")");
            if (result.success) {
                if (str.length > 1) {
                    if (str[1] != "") {
                        var files = eval("(" + str[1] + ")");
                        var len = files.length;
                        var str = "";
                        for (var i = 0; i < len; i++) {
                            var file = files[i];
                            //fs.AddToList(file.path, file.strParam);
                            if (str == "")
                            { str = fs.bLocalSign +"|" + file.path + "|" + file.strParam; }
                            else { str += "||" + fs.bLocalSign + "|" + file.path + "|" + file.strParam; }
                        }
                        fs.AddToListEx(str);
                    }
                }
                $(document.body).css('overflow', '');
                $("#mask").remove();

                top.showInfoBar("更新成功");
                loadAttachments(bookPageId)
            }
             else {
                $(document.body).css('overflow', '');
                $("#mask").remove();
                top.showInfoBar("上传出错");
            }
        }
    });}
    
    
    function IsImage(ext) {
    var img = ",.jpg,.gif,.png,.jpeg,.bmp,.dwg,.tif,.tiff,.ico,.psd,";
    ext = "," + $.trim(ext.toLowerCase()) + ",";
    if (img.indexOf(ext) >= 0) {
        return true;
    } else {
        return false;
    }
}


