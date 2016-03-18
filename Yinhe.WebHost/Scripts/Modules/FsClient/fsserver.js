/*
功能：dwg图框中对应的属性
*/
function Dwg() {
    this.dwgprjname = "";
    this.dwgname = "";
    this.dwgarcharea = "";
    this.dwgscale = "";
    this.path = "";
}
Dwg.prototype.toString = function() {
    return "path:" + this.path + "|" + "dwgprjname:" + this.dwgprjname + "|" + "dwgname:" + this.dwgname + "|" + "dwgarcharea:" + this.dwgarcharea + "|" + "dwgscale:" + this.dwgscale;
}

/*
功能:文件类型判断
*/
function FileType() {
    this.ImageFileType = ",.jpg,.gif,.png,.jpeg,.bmp,.tif,.tiff,.ico,.psd,";
    this.OfficeType = ",.doc,.xls,.mpp,.ppt,.vsd,.docx,.docm,.xlsx,.xlsm,.xlsb,.pptx,.pptm,";
    this.DWGFileType = ",.dwg,";
    this.PDFFileType = ",.pdf,";
    this.IEFileType = ",.htm,.html,.asp,.aspx,.jsp,";
    this.TxtFileType = ",.txt,";
}
FileType.prototype.GetFileType = function(ext) {
    ext = "," + ext + ",";
    if (this.ImageFileType.indexOf(ext) >= 0) { return "Image"; }
    if (this.OfficeType.indexOf(ext) >= 0) { return "Office"; }
    if (this.DWGFileType.indexOf(ext) >= 0) { return "Dwg"; }
    if (this.PDFFileType.indexOf(ext) >= 0) { return "Pdf"; }
    if (this.IEFileType.indexOf(ext) >= 0) { return "Ie"; }
    if (this.TxtFileType.indexOf(ext) >= 0) { return "Txt"; }
    return "Other";
}

/*
功能：文件上传控件
   
日期：2009-03-12
   
作者：倪圳源

此部分移入 fsconfig.js 中进行配置
function Fs() {
this.curVer = "2.0.0.4";
this.ip = "192.168.2.158";
this.port = "2000";
this.websvrurl = "http://192.168.2.4:8018/AsynServices/FileServer/FsToDatabase.asmx";
this.UpdateBag = "http://192.168.2.4:8018/Home/LoadActiveXSetup/1";
}

*/
//原来在线升级接口已无效
Fs.prototype.UpdateCtl = function() {
    mgrObj.UpdateCtl(this.UpdateBag);
    return;
}
///功能：连接文件服务器
///参数说明：strIp:文件服务器地址
///          strPort:文件服务器Ip
///          strWebSvrUrl:回调处理WebServices的URL
///返回值：设置成功-true
Fs.prototype.GetVersion = function() {
    return mgrObj.GetVersion();
}

///功能：连接文件服务器
///参数说明：strIp:文件服务器地址
///          strPort:文件服务器Ip
///          strWebSvrUrl:回调处理WebServices的URL
///返回值：设置成功-true
Fs.prototype.SetCSip = function(strIp, strPort, strWebSvrUrl) {
    if (LoadActiveX == 1) { return false; }
    if (!strWebSvrUrl) { strWebSvrUrl = this.websvrurl; }
    mgrObj.SetWebSvrUrl(strWebSvrUrl);
    if (!strIp) { strIp = this.ip; }
    if (!strPort) { strPort = this.port; }
    var result = mgrObj.SetCServerIP(strIp, strPort);
    try {
        mgrObj.SetFSGroup(this.group);
    }
    catch (err) {
    }
    return result;
}
///功能：保存文件对话框
///参数说明：filename:要被保存的文件名
///返回值：被保存文件的完整路径
Fs.prototype.Save = function(filename) {
    var result = mutiselect.ShowSave("所有文件(*.*)|*.*||", filename, true);
    return result;
}
///功能：获取本地ie临时文件夹
Fs.prototype.GetIETemp = function() {
    var result = mutiselect.GetTempPath();
    return result;
}
///功能：选择文件对话框
///参数：i选择文件的个数，0--不限制
///返回值：返回选择文件的xml
Fs.prototype.OpenSelect = function(i, ext) {
    var ReturnselExt = "*.*";
    if (ext) {
        if (ext != "") {
            ReturnselExt = "(";
            var arr = ext.split("\\0");
            for (var x = 0; x < arr.length; x++) {
                if (arr[x] != "") {
                    if (arr[x].indexOf("*") >= 0) {
                        selExt = arr[x];
                    } else {
                        selExt = "*" + arr[x];
                    }
                    if (ReturnselExt != "(") ReturnselExt += ";";
                    ReturnselExt += selExt;
                }
            }
        }
    }

    ReturnselExt = ReturnselExt + ")\0" + ReturnselExt.replace("(","") + "\0\0";

    mutiselectNew.nFilterRarType = fs.selectPackageFileType;
    mutiselectNew.strFilterFileType = fs.selectPackageFileExt;
    mutiselectNew.bSelectPreThumb = 0;
    var result = mutiselectNew.SelectFilesByFilter(i, ReturnselExt);

    //mutiselect.ShowOpen(i, selExt);
    return result;
}
///功能：选择文件对话框
///返回值：文件对象数组 (文件对象包括 <name:文件名，path:文件客户端路径> 属性)
Fs.prototype.GetSelectList = function(i, selext) {
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//处理控件问题，如果有任何异常则禁止使用
if (!showStartMsg()) { return false; }
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    var result = this.OpenSelect(i, selext);
    var xmlDoc = loadXML(result);
    var files = xmlDoc.getElementsByTagName("file");
    var len = files.length;
    var arrfile = new Array();
    var i = 0;
    if (len == 0) { return false; }
    for (i = 0; i < len; i++) {
        if (files[i].getAttribute("size") != "0 KB") {
            var objFile = new Object();
            objFile.name = files[i].getAttribute("name");
            objFile.path = files[i].getAttribute("path");
            objFile.size = files[i].getAttribute("size");
            arrfile.push(objFile);
        } else { top.showInfoBar(files[i].getAttribute("name") + "文件大小为0KB，不能选择。"); }
    }
    return arrfile;
}

///功能：添加上传任务
///参数说明：filepath：要上传的本地文件路径  
///          param：上传文件相应的参数（主要用于上传完成回发处理实际业务逻辑）
Fs.prototype.AddUploadTask = function(filepath, param) {
    var result = uploadObj.AddTask(this.bLocalSign, filepath, param);
    return result;
}
///功能：添加下载任务
///参数说明：filename:下载客户端的文件名
///          filepath:要下载文件的希哈值
Fs.prototype.AddDownLoadTask = function(filename, filepath) {
    var clientPath = this.Save(filename);
    if (clientPath != "") {
        downloadObj.AddTask(0, filepath, clientPath);
    }
}
///功能：读取文件
///参数说明：filename:下载客户端的文件名
///          filepath:要下载文件的希哈值
Fs.prototype.ReadFile = function(filename, filepath) {
    var fileType = this.GetFileType(filename);
    if (fileType == "Other") {
        alert("文件格式不支持在线查阅，请下载后查看！");
    }
    else {
        var fi = this.GetIETemp() + "\\" + filename;
        readObj.AddTask(1, filepath, fi);
    }
}

///功能：读取文件，可在线编辑
///参数说明：filename:下载客户端的文件名
///          filepath:要下载文件的希哈值
Fs.prototype.ReadFileEx = function(filename, filepath, param) {

    var fileType = this.GetFileType(filename);
    if (fileType == "Other") {
        alert("文件格式不支持在线查阅，请下载后查看！");
    }
    else {
        var fi = this.GetIETemp() + "\\" + filename;

        if (fileType == "Office") {
            readObj.AddEditOnline(filepath, fi, param);
        }
        else {
            readObj.AddTask(1, filepath, fi);
        }
    }
}

///功能：切割dwg图
///参数说明：dwgpath:本地dwg文件路径
///          param:切割dwg的参数（用于回发后，处理实际业务逻辑）
Fs.prototype.SplitDwg = function(dwgpath, param) {
    var result = spliteObj.AddSpliteTask(dwgpath, param);
    this.AddToList(dwgpath, "dwg@" + param);
    return result;
}

Fs.prototype.ViewFile = function(path) {
    webviewerObj.ShowViewer(path);
}

///功能：获取dwg切割后子图对象
///参数说明：strXml:xml文档
///          count:子图个数
Fs.prototype.GetSplitDwgFiles = function(strXml, count) {
    var xmlDoc = loadXML(strXml);
    var arrDwg = new Array();
    for (var i = 0; i < count; i++) {
        var dwgFile = xmlDoc.getElementsByTagName("SubFile" + i)[0];
        var dwg = new Dwg();
        dwg.path = dwgFile.text;
        dwg.dwgprjname = dwgFile.getAttribute("dwgprjname");
        dwg.dwgname = dwgFile.getAttribute("dwgname");
        dwg.dwgarcharea = dwgFile.getAttribute("dwgarcharea");
        dwg.dwgscale = dwgFile.getAttribute("dwgscale");
        arrDwg.push(dwg);
    }
    return arrDwg;
}
///功能：添加上传文件到队列
///参数说明：imgpath:要上传的文件
///           param:用于处理实际业务逻辑
Fs.prototype.AddToList = function(imgpath, param) {
    uploadObj.AddTask(this.bLocalSign, imgpath, param);
    //uploadObj.AddTask(0, imgpath, param);
    //thumbUploadObj.AddToList(imgpath, param);
}

///功能：添加上传文件到队列
///参数说明：imgpath:要上传的文件
///           param:用于处理实际业务逻辑
Fs.prototype.AddToListEx = function(strPath) {
    uploadObj.AddTasks(strPath);
    //thumbUploadObj.AddToList(imgpath, param);
}


///功能：添加到上传队列
///参数说明：imgpath:要上传的文件
///          param:用于处理实际业务逻辑
Fs.prototype.GetList = function() {
    //var result = thumbUploadObj.GetList();
    var result = "";
    return result;
}
///功能：删除上传队列的项
///参数说明：imgpath:要上传的文件
///          param:用于处理实际业务逻辑
Fs.prototype.RemoveItem = function(imgpath, param) {
    //thumbUploadObj.Remove(imgpath, param); //在控件中删除
}
///功能：获取上传队列对象数组（对象包括 <filename:本地文件路径，param:用于处理实际业务逻辑参数> 属性)）
Fs.prototype.GetListArr = function() {
    var result = this.GetList();
    var xmlDoc = loadXML(result);
    var imgs = xmlDoc.getElementsByTagName("img");
    var len = imgs.length;
    var arrList = new Array();
    for (var i = 0; i < len; i++) {
        var item = new Object();
        item.filename = imgs[i].getAttribute("filename");
        item.param = imgs[i].getAttribute("param");
        arrList.push(item);
    }
    return arrList;
}
///功能：提取dwg缩略图
///参数说明：dwgpath:本地dwg文件
///          width:缩略图宽度
///          height:缩略图高度
///          param:参数
Fs.prototype.DwgToBmp = function(dwgpath, width, height, param) {
    thumbObj.AddConvert(dwgpath, width, height, param);
    return;
}
///功能：将文件转为16进制字符串
///参数说明：filepath:本地文件路径
Fs.prototype.FileToHex = function(filepath) {
    var result = mutiselect.LoadFile(filepath);
    return result;
}

///功能：删除临时产生的缩略图文件
///参数说明：filepath:本地文件路径
Fs.prototype.DeleteTempFile = function(filepath) {
    var result = mutiselect.DeleteTempFile(filepath);
    return result;
}

///功能：截取文件名
///参数说明：fullFileName:本地文件路径
Fs.prototype.GetFileName = function(fullFileName) {
    var pos = fullFileName.lastIndexOf("\\");
    var dot = fullFileName.lastIndexOf(".");
    return fullFileName.substring(pos + 1, dot);
}
///功能：截取文件扩展名
///参数说明：fullFileName:本地文件路径
Fs.prototype.GetFileExt = function(fullFileName) {
    var arr = fullFileName.split(".");
    var ext = arr[arr.length - 1];
    if (arr.length == 1) { return ""; }
    ext = ext.toLowerCase();
    return "." + ext;
}
Fs.prototype.GetFileType = function(fullFileName) {
    var ext = this.GetFileExt(fullFileName);
    var fileType = new FileType();
    return fileType.GetFileType(ext);
}
//清空上传队列
Fs.prototype.ClearList = function() {
    var arr = fs.GetListArr();
    var len = arr.length;
    for (var i = 0; i < len; i++) {
        var obj = arr[i];
        fs.RemoveItem(obj.filename, obj.param);
    }
}
///功能：操作上传队列文件（上传上传队列中的文件）
Fs.prototype.HandleList = function() {
    if (LoadActiveX == 1) { return false; }
    //alert(fs.GetList());
    var arr = fs.GetListArr();
    var len = arr.length;
    if (len <= 0) { return; }
    for (var i = 0; i < len; i++) {
        var obj = arr[i];
        if (obj.filename == "") {
            fs.RemoveItem(obj.filename, obj.param);
            continue;
        }
        var type = obj.param.split("@")[0];
        switch (type) {
            case "dwg":
            case "dwg_child":
                this.AddUploadTask(obj.filename, obj.param);
                break;
            case "dwg_img":
                UploadThumbImg(obj.filename, obj.param);
                break;
            case "img":
                //上传图片的缩略图                
                UploadThumbImg(obj.filename, obj.param);
                break;
            case "att":
            case "matdoc":
            case "doc":
                this.AddUploadTask(obj.filename, obj.param);
                break;
            default:
                this.AddUploadTask(obj.filename, obj.param);
                break;
        }
    }
}


// var index = readObj.BatchPrintDwgFile("3F581659A0B971FD00AC7ECBA6FBEF5F7573B2E6879B220F25C109DDA3C7CB6C|C:\\test1.dwg|A4^C8D5E987769C49D9B7C12C8CEC6A4FE7042DE4FDDFE0682C6AF1F9B119B8351F|C:\\test2.dwg|A3^087693EF62B4C9F26C71376E782FDE38C252B736F3C0A886871163187E2A409B|C:\\test3.dwg|A5^D1C745C0425DBAAA99005C5B2DEAC29E89D83406C0CC5EEBF112FBE5918DF572|C:\\test4.dwg|A5");
// var progressNum = readObj.GetPrintDownloadProgress(index);

//fs.ClearList();

//fs.DwgToBmp("C:\\Users\\Administrator\\Desktop\\temp\\1.jpg",120,110,"测试生成缩略图");

/*
var str = "4C016A35C5916ED3B74BB892D28F8C0C933F7FE37F3511B91BB77E226656A6EE";
fs.ReadFile("1.txt", str);

window.setTimeout("fs.HandleList();", 2000);
//fs.HandleList();

var arr = fs.GetListArr();
for (var i = 0; i < arr.length; i++) {
alert("fileName:" + arr[i].filename + "\r\n param:" + arr[i].param);
fs.DwgToBmp(arr[i].filename, 160, 120, arr[i].param);
fs.RemoveItem(arr[i].filename, arr[i].param);
}
alert(fs.GetList());
*/