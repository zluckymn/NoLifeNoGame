var arrImgSize = [{ width: 240, height: 220, type: "l" }, { width: 120, height: 110, type: "m"}]

///文件上传成功回发事件
function uploadObj_OnTaskFinished(filepath, result, param) {
    //alert("文件上传回调函数：\r\n上传文件路径：" + filepath + "\r\n上传结果：" + result + "\r\n上传参数：" + param);
    //uploadObjOnTaskFinished(filepath, result, param)
}
function uploadObjOnTaskFinished(filepath, result, param) {
    var type = param.split("@")[0];
    switch (type) {
        case "dwg":
            //上传dwg母图
            alert("上传dwg母图");
            break;
        case "dwg_child":
            //上传dwg子图
            fs.DwgToBmp(filepath, 400, 300, param);
            alert("上传dwg子图");
            break;
        case "dwg_img":
            //上传的是dwg缩略图
            alert("上传的是dwg缩略图");
            break;
        case "att":
        case "custRefFiles":
        case "doc":
            SaveHash(filepath, result, param);
            break;
        default:
            UpdateServerInfo(filepath, result, param);
            break;
    }
    //alert(filepath + " : " + param);
    //fs.RemoveItem(filepath, param);
}
function UpdateServerInfo(filepath,hash,param) {
    $.post(
           "/Projects/ProjectResults/SaveUploadFileHash",
           {
               docid: param,
               hash: hash,
               status: 1
           },
            function(data) {
                var result = eval("(" + data + ")");
                if (result.success) {
                    fs.RemoveItem(filepath, param);
                    if (result.fileType == "Image") {
                        AddThumbImgToList(filepath, param);
                    }
                    //if(fs.Get)
                    //alert(filepath + "文件上传成果！");
                } else {
                    alert("更新失败！");
                }
            }
    );
}
function SaveHash(filepath, hash, param) {
    $.post(
           "/FileServer/SaveHash",
           {
               p: param,
               hash: hash,
               status: 1
           },
            function(data) {
                var result = eval("(" + data + ")");
                if (result.success) {
                    fs.RemoveItem(filepath, param);
                    if (result.fileType == "Image") {
                        AddThumbImgToList(filepath, param);
                    }
                } else {
                    alert("更新失败！");
                }
            }
    );
}
//添加上传缩略图到上传队列
function AddThumbImgToList(filepath,param){
   var index =param.indexOf('@');
   if(index>=0){
      var firstParam = param.substring(0,index+1);
      param = param.replace(firstParam,"img@");
   }else{
       param ="img@"+param;
   }
   var len = arrImgSize.length;

   for (var i = 0; i < len; i++) {     
       fs.DwgToBmp(filepath, arrImgSize[i].width, arrImgSize[i].height, param + "-" + arrImgSize[i].type);
   }

}
//上传缩略图
function UploadThumbImg(filepath, param) {
    var arrprm = param.split("@");
    var arrp = arrprm[1].split("-");
    var docid = arrp[0];
    var spec = arrp[2];
    var content = fs.FileToHex(filepath);
    if (content == null || content=="") {
        //alert("文件不存在!");
        fs.RemoveItem(filepath, param);
        return;
    }
    
    $.post(
           "/Projects/ProjectResults/UploadThumbImg",
           {
               docid: docid,
               content: content,
               spec: spec
           },
            function(data) {
           var result = eval("(" + data + ")");
           if (result.success) {         
                    fs.RemoveItem(filepath, param);                    
                    fs.DeleteTempFile(filepath);
                } else {
                    alert("更新失败！");
                }

            }
    );
    
}

//在线阅读，文件下载完成回发事件
function readObj_Finished(hash, clientFilePath) {
    if (IsReadPic == 1) {
        IsReadPic = 0;
        $(top.document).find("a:last").click();
        //ViewFile(clientFilePath);
    }

    //alert("在线阅读回调函数：\r\n 文件希哈值:" + hash + "\r\n 下载到本地的文件路径:" + clientFilePath);
    //document.getElementById("imgTest").src = clientFilePath;
    /*
    var fileType = fs.GetFileType(clientFilePath);

    switch (fileType) {
        case "Other":
            window.open(clientFilePath);
            break;
        default:
            fs.ViewFile(clientFilePath);
            break;
    }
    */
}

//提取dwg缩略图回调函数
function thumbObj_Finished(filename, lWidth, lHeight, bstrParam, bstrOutFileName) {
    //alert(filename + " 提取缩略图: " + bstrOutFileName + ", \r\n宽高：" + lWidth + "*" + lHeight + " \r\n参数 " + bstrParam)
    
    //fs.AddToList(bstrOutFileName, bstrParam);
    //UploadThumbImg(bstrOutFileName, bstrParam);
    
    //thumbObjFinished(bstrOutFileName, bstrParam);
}

function thumbObjFinished(bstrOutFileName, param) {
    param = param.replace("dwg_child@", "");
    strParam = "dwg_img@" + param;
    fs.AddToList(bstrOutFileName, strParam);
}
//dwg图切割完成回调函数
function spliteObj_Finished(filename, param, result, count) {
    //alert("图片切割回调函数：\r\n切割文件文件名：" + filename + "\r\n切割参数：" + param + "\r\n返回的结果：" + result + "\r\n切割文件数：" + count);
    splitObjFinished(result, count, param);
}
function splitObjFinished(result, count, param) {
    var arr = fs.GetSplitDwgFiles(result, count);
    for (var i = 0; i < arr.length; i++) {
        //alert(arr[i].toString());
        var dwg = arr[i];
        var strParam = "dwg_child@" + param + "@" + dwg.toString();
        fs.AddToList(dwg.path, strParam);
    }
}



function RemoveUploadItem(filename, strParam) {
    fs.RemoveItem(filename, strParam);
    //top.showInfoBar("文件 " + filename+" 上传失败，请确认文件是否存在，并重新上传");
}
//上传dwg提取的缩略图
/*
function UploadThumbImg(fullFilename, param) {
    var content = fs.FileToHex(fullFilename);
    var strext = fs.GetFileExt(fullFilename);
    var name = fs.GetFileName(fullFilename);
    var p = "123";

    $.post(
       "UploadFile.aspx",
        {
            content: content,
            ext: strext,
            param: param,
            name: name
        },
        function(data) {
            if (data == "") { return false; }
            data = eval("(" + data + ")");
            if (data.success) {
                fs.RemoveItem(fullFilename, param);
            } else {
            alert("文件上传失败!");
            }
        }
   ); 
}
*/
