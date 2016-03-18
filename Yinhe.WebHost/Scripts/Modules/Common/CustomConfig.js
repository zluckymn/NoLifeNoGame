//为全局添加SelectBox全选事件
$("form").each(function() {
    $(this).submit(function() {
        $("select[multiple=true]").each(
	            function() {
	                $("#" + $(this).attr("id")).find("option").each(function() {
	                    $(this).attr("selected", true);
	                });
	            });
    });
});









//增加时间控件--onClick="WdatePicker()"
//增加验证 <input type="text" name="money" id="money" limit="type:float;required:true;decLen:2" msg="只能为浮点为，且保留两位小数点" onblur="validElement(this)"> 或者是 checkForm(form, checkAll,showBar)


//为无素添加tip提示
//id        为要添加tip提示的元素的ID
//content   为tip提示的内容
function addBetterTip(id, content) {
    if (!$("#" + id).attr("betterTip")) {//如果已经初始化，则不再次初始化
        var default_width = "300";
        $("#" + id).attr("betterTip", "yes");
        $("#" + id).attr("config", "$" + id + "_betterTip?width=" + default_width);
        $(document.body).append("<div id=\"" + id + "_betterTip\" style=\"display:none;\">" + content + "</div>");
        BT_initById(id); //根据id初始化betterTip插件
    }
}

//计算firstId的value与secondId的value的总和放进sumId
function sum(firstId, secondId, sumId) {
    var k;
    if ($("#" + firstId).val().length == 0) {
        k = 0;
    }
    else {

        if (isNaN($("#" + firstId).val())) {
            return;
        }
        k = parseFloat($("#" + firstId).val());


    }

    var m;
    if ($("#" + secondId).val().length == 0) {
        m = 0;
    }
    else {
        if (isNaN($("#" + secondId).val())) {

            return;
        }
        m = parseFloat($("#" + secondId).val());

    }
    var c = m + k;
    $("#" + sumId).val(c);
}


//为无素添加tip提示
//id        为要添加tip提示的元素的ID
//content   为tip提示的内容
//width     为tip提示设置宽度
function addBetterTip(id, content, width) {

    if (!$("#" + id).attr("betterTip")) {//如果已经初始化，则不再次初始化
        var default_width = "300";
        if (width != "") {
            default_width = width;
        }
        $("#" + id).attr("betterTip", "yes");
        $("#" + id).attr("config", "$" + id + "_betterTip?width=" + default_width);
        $(document.body).append("<div id=\"" + id + "_betterTip\" style=\"display:none;\">" + content + "</div>");
        BT_initById(id);
    }
}

//开启进度条
function startProgressBar() {

    if (!document.getElementById("progressBar")) {//未创建

//        var progressBarObject = "<div id=\"progressBar\" ><div><object classid=\"clsid:D27CDB6E-AE6D-11cf-96B8-444553540000\"  codebase=\"http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=7,0,19,0\" width=\"189\" height=\"11\">";
//        progressBarObject = progressBarObject + " <param name=\"movie\" value=\"/Content/Images/zh-cn/Common/loding.swf\" /> ";
//        progressBarObject = progressBarObject + " <param name=\"quality\" value=\"high\" /> ";
//        progressBarObject = progressBarObject + " <embed src=\"/Content/Images/zh-cn/Common/loding.swf\" quality=\"high\" pluginspage=\"http://www.macromedia.com/go/getflashplayer\"  type=\"application/x-shockwave-flash\" width=\"189\" height=\"11\"></embed>";
//        progressBarObject = progressBarObject + "</object></div></div>";
//        $(document.body).append(progressBarObject);

    }
    Popdiv("progressBar");

}

//结束进度条
function endProgressBar() {

    Hidediv("progressBar");

}
//var _st = window.setTimeout;
//window.setTimeout = function(fRef, mDelay) {
//    if (typeof fRef == 'function') {
//        var argu = Array.prototype.slice.call(arguments, 2);
//        var f = (function() { fRef.apply(null, argu); });
//        return _st(f, mDelay);
//    }
//    return _st(fRef, mDelay);
//}

function SetErrorImg(obj, type) {
    obj.onerror = "";
    ErrorImg(obj, type);
    //window.setTimeout(ErrorImg, 100, obj,type);
}
function ErrorImg(obj, type) {
    if (type == "m") {
        obj.src = "/Content/Images/zh-cn/Common/result_m.png";
    } else {
        obj.src = "/Content/Images/zh-cn/Common/result_l.png";
    }
}

function setTypeImg(o, type, ext) {
    o.onerror = "";
    getImg(o, type, ext);
}
function getImg(o, type, ext) {
    switch (ext) {
        case ".mdb":
        case ".accdb":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/access_" + type + ".png";
            break;
        case ".doc":
        case ".docx":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/word_" + type + ".png";
            break;
        case ".xls":
        case ".xlsx":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/excel_" + type + ".png";
            break;
        case ".bmp":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/bmp_" + type + ".png";
            break;
        case ".dwg":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/dwg_" + type + ".png";
            break;
        case ".pdf":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/pdf_" + type + ".png";
            break;
        case ".gif":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/gif_" + type + ".png";
            break;
        case ".html":
        case ".htm":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/html_" + type + ".png";
            break;
        case ".ini":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/ini_" + type + ".png";
            break;
        case ".jpg":
        case ".jpeg":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/jpg_" + type + ".png";
            break;
        case ".psd":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/psd_" + type + ".png";
            break;
        case ".ppt":
        case ".pptx":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/ppt_" + type + ".png";
            break;
        case ".rtf":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/rtf_" + type + ".png";
            break;
        case ".mpp":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/mpp_" + type + ".png";
            break;
        case ".tiff":
        case ".tif":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/tiff_" + type + ".png";
            break;
        case ".txt":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/txt_" + type + ".png";
            break;
        case ".wav":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/txt_" + type + ".png";
            break;
        case ".result":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/result_" + type + ".png";
            break;
        case ".rar":
        case ".7z":
        case ".zip":
        case ".bz2":
        case ".tar":
        case ".gz":
        case ".iso":
        case ".bin":
        case ".cue":
            o.src = "/Content/Images/zh-cn/Common/sys-icon/rar_" + type + ".png";
            break;
        default:
            o.src = "/Content/Images/zh-cn/Common/sys-icon/default_" + type + ".png";
            break;
    }
}

function returnImg(type, ext) {
    switch (ext) {
        case ".mdb":
        case ".accdb":
            return "/Content/Images/zh-cn/Common/sys-icon/access_" + type + ".png";
            break;
        case ".doc":
        case ".docx":
            return "/Content/Images/zh-cn/Common/sys-icon/word_" + type + ".png";
            break;
        case ".xls":
        case ".xlsx":
            return "/Content/Images/zh-cn/Common/sys-icon/excel_" + type + ".png";
            break;
        case ".bmp":
            return "/Content/Images/zh-cn/Common/sys-icon/bmp_" + type + ".png";
            break;
        case ".dwg":
            return "/Content/Images/zh-cn/Common/sys-icon/dwg_" + type + ".png";
            break;
        case ".pdf":
            return "/Content/Images/zh-cn/Common/sys-icon/pdf_" + type + ".png";
            break;
        case ".gif":
            return "/Content/Images/zh-cn/Common/sys-icon/gif_" + type + ".png";
            break;
        case ".html":
        case ".htm":
            return "/Content/Images/zh-cn/Common/sys-icon/html_" + type + ".png";
            break;
        case ".ini":
            return "/Content/Images/zh-cn/Common/sys-icon/ini_" + type + ".png";
            break;
        case ".jpg":
        case ".jpeg":
            return "/Content/Images/zh-cn/Common/sys-icon/jpg_" + type + ".png";
            break;
        case ".psd":
            return "/Content/Images/zh-cn/Common/sys-icon/psd_" + type + ".png";
            break;
        case ".ppt":
        case ".pptx":
            return "/Content/Images/zh-cn/Common/sys-icon/ppt_" + type + ".png";
            break;
        case ".rtf":
            return "/Content/Images/zh-cn/Common/sys-icon/rtf_" + type + ".png";
            break;
        case ".mpp":
            return "/Content/Images/zh-cn/Common/sys-icon/mpp_" + type + ".png";
            break;
        case ".tiff":
        case ".tif":
            return "/Content/Images/zh-cn/Common/sys-icon/tiff_" + type + ".png";
            break;
        case ".txt":
            return "/Content/Images/zh-cn/Common/sys-icon/txt_" + type + ".png";
            break;
        case ".wav":
            return "/Content/Images/zh-cn/Common/sys-icon/txt_" + type + ".png";
            break;
        case ".result":
            return "/Content/Images/zh-cn/Common/sys-icon/result_" + type + ".png";
            break;
        case ".rar":
        case ".7z":
        case ".zip":
        case ".bz2":
        case ".tar":
        case ".gz":
        case ".iso":
        case ".bin":
        case ".cue":
            return "/Content/Images/zh-cn/Common/sys-icon/rar_" + type + ".png";
            break;
        default:
            return "/Content/Images/zh-cn/Common/sys-icon/default_" + type + ".png";
            break;
    }
}

function ReplaceChar(str) {
    str = str.replace(/\>/g, "");
    str = str.replace(/\</g, "");
    str = str.replace(/\?/g, "");
    str = str.replace(/\!/g, "");
    str = str.replace(/[\/]/g, "");
    str = str.replace(/\:/g, "");
    str = str.replace(/\*/g, "");
    str = str.replace(/\|/g, "");
    str = str.replace('"', "").replace('"', "").replace('"', "").replace('"', "");
    str = str.replace("'", "").replace("'", "").replace("'", "").replace("'", "");
    return str;
}


window.onbeforeunload = function() {
    if (window.location.href == top.window.location.href) { PopdivNoall("loading"); }
    //document.body.innerHTML = "页面跳转中..";
}

$(document).ready(function() {
    var onunload = '<div id="loading" style="position:absolute; display:none; z-index:999;">';
    onunload += '<div>';
//    onunload += '<object classid="clsid:D27CDB6E-AE6D-11cf-96B8-444553540000" codebase="http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=7,0,19,0" width="189" height="11">';
//    onunload += '<param name="movie" value="/Content/Images/zh-cn/Common/loding.swf" />';
//    onunload += '<param name="quality" value="high" />';
//    onunload += '<embed src="/Content/Images/zh-cn/Common/loding.swf" quality="high" pluginspage="http://www.macromedia.com/go/getflashplayer" type="application/x-shockwave-flash" width="189" height="11"></embed>';
//    onunload += '</object>';
    onunload += '</div>';
    onunload += '</div>';
    onunload += '<div id="working" style="position:absolute; display:none">';
    onunload += '<div>';
//    onunload += '<object classid="clsid:D27CDB6E-AE6D-11cf-96B8-444553540000" codebase="http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=7,0,19,0" width="189" height="11">';
//    onunload += '<param name="movie" value="/Content/Images/zh-cn/Common/loding.swf" />';
//    onunload += '<param name="quality" value="high" />';
//    onunload += '<embed src="/Content/Images/zh-cn/Common/loding.swf" quality="high" pluginspage="http://www.macromedia.com/go/getflashplayer" type="application/x-shockwave-flash" width="189" height="11"></embed>';
//    onunload += '</object>';
    onunload += '</div>';
    onunload += '</div>';

    onunload += '<div id="SysMessageCenter" class="shadow-container2" style="position:absolute; display:none">';
    onunload += '<div class="title">';
    onunload += '<div class="name">xxxxxxxxx</div>';
    onunload += '<div class="oper"><A style="display:none;" title=关闭 onclick=HideBox(this); href="javascript:void(0)"></A></DIV>';
    onunload += '</div>';
    onunload += '<div class="contain" style="background-color:#FFFFFF">';
    onunload += '<table width="100%">';
    onunload += '<tr>';
    onunload += '<td width="80" valign="top"><br />';
    onunload += '<img src="/Content/Images/zh-cn/Common/infor-pic.jpg" width="60" height="58" /></td>';
    onunload += '<td height="100" style="line-height:20px; color:#767676" id="SysMessageCenterBox"></td>';
    onunload += '</tr>';
    onunload += '</table>';
    onunload += '</div>';
    onunload += '<div class="bottom">';
    onunload += '<div class="btn"><input type="button" value=" 保存 " /></div>';
    onunload += '</div>';
    onunload += '</div>';

    $(document.body).append(onunload);
});


function SetHistory(name, url, tip) {
    var namelist = getCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryName");
    var urllist = getCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryUrl");
    var timelist = getCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryTime");
    var tiplist = getCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryTip");

    var now = new Date();
    var m = now.getMonth() + 1;
    var nowtime = m + "-" + now.getDate();

    if (namelist == null) {
        namelist = name;
        urllist = url;
        timelist = nowtime;
        tiplist = tip;
    } else {
        if (namelist.indexOf(name) == -1 && urllist.indexOf(url) == -1) {
            namelist = name + "," + namelist;
            urllist = url + "," + urllist;
            timelist = nowtime + "," + timelist;
            tiplist = tip + "," + tiplist;
        }
    }

    var arr = namelist.split(",");
    if (arr.length > 5) {
        var brr = urllist.split(",");
        var crr = timelist.split(",");
        var drr = tiplist.split(",");

        namelist = "";
        urllist = "";
        timelist = "";
        tiplist = "";
        for (y = 0; y < 5; y++) {

            if (namelist == "") {
                namelist = arr[y];
                urllist = brr[y];
                timelist = crr[y];
                tiplist = drr[y];
            } else {
                namelist = namelist + "," + arr[y];
                urllist = urllist + "," + brr[y];
                timelist = timelist + "," + crr[y];
                tiplist = tiplist + "," + drr[y];
            }

        }


    }
    delCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryName");
    delCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryUrl");
    delCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryTime");
    delCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryTip");
    setCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryName", namelist, 365);
    setCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryUrl", urllist, 365);
    setCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryTime", timelist, 365);
    setCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryTip", tiplist, 365);
}

function GetHistory(divid, size) {
    $("#" + divid).load("/Control/BrowseHistoryResult?size=5");
}

function DeleteHistoryById(id) {
    var namelist = "";
    var urllist = "";
    var timelist = "";
    var tiplist = "";
    if (getCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryName") == null) { return false; }
    var html = '<table width="100%">';
    var arr = getCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryName").split(",");
    var brr = getCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryUrl").split(",");
    var crr = getCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryTime").split(",");
    var drr = getCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryTip").split(",");

    for (var x = 0; x < arr.length; x++) {


        var Idarr = brr[x].split("/");
        var thisId = Idarr[Idarr.length - 1];
        if (thisId != id) {

            if (namelist == "") {
                namelist = arr[x];
                urllist = brr[x];
                timelist = crr[x];
                tiplist = drr[x];
            } else {
                namelist = namelist + "," + arr[x];
                urllist = urllist + "," + brr[x];
                timelist = timelist + "," + crr[x];
                tiplist = tiplist + "," + drr[x];
            }

        }

    }

    delCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryName");
    delCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryUrl");
    delCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryTime");
    delCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryTip");
    setCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryName", namelist, 365);
    setCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryUrl", urllist, 365);
    setCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryTime", timelist, 365);
    setCookie(location.hostname.replace(/\./g, "") + location.port + "HistoryTip", tiplist, 365);
}

/********************************************************************
* 函数名:
* getCookie
*
* 参数:
* c_name  - Cookie名称
*
* 返回值:
* Cookie内容
* 
* 说明:
* 该函数获取Cookie用户上次登录方式
********************************************************************/
function getCookie(c_name) {
    //alert("取cookie函数中=" + document.cookie);
    if (document.cookie.length > 0) {

        c_start = document.cookie.lastIndexOf(c_name + "=");
        //alert("取得c_start_indexOf的值=" + document.cookie.indexOf(c_name + "="));
        //alert("取得c_start_lastIndexOf的值=" + document.cookie.lastIndexOf(c_name + "="));
        if (c_start != -1) {
            c_start = c_start + c_name.length + 1;
            c_end = document.cookie.indexOf(";", c_start);

            //alert("取得c_end_indexOf的值=" + document.cookie.indexOf(";", c_start));
            //alert("取得c_end_lastIndexOf的值=" + document.cookie.lastIndexOf(";", c_start));

            if (c_end == -1) {
                c_end = document.cookie.length;
            }
            return unescape(document.cookie.substring(c_start, c_end));
        }
    }
    return null;
}
/********************************************************************
* 函数名:
* setCookie
*
* 参数:
* c_name  - Cookie名称
*   value  - Cookie内容
*   expiredays - Cookie日期
*
* 返回值:
* 空
* 
* 说明:
* 该函数设置Cookie保留用户上次登录方式
********************************************************************/
function setCookie(c_name, value, expiredays) {
    // alert("本次设置c_name=" + c_name);
    // alert("本次设置value=" + value);
    var exdate = new Date();
    var today = new Date();

    exdate.setTime(today.getTime() + 1000 * 60 * 60 * 24 * expiredays);
    // 使设置的有效时间正确。增加toGMTString()
    // alert(c_name + "=" + escape(value) + ((expiredays == null) ? "" : ";expires=" + exdate.toGMTString()));
    document.cookie = c_name + "=" + escape(value) + ((expiredays == null) ? "" : ";expires=" + exdate.toGMTString()) + ";path=/";

}
//删除指定名称的cookie
function delCookie(name) {
    var time = new Date();
    time.setTime(time.getTime() - 1);
    var value = getCookie(name);
    document.cookie = name + "=" + value + ";expires=" + time.toGMTString() + ";path=/";
}




function getAMoney(obj) {
    var _value = obj.value.replace(/,/g, "");
    if (/[\d.]/.test(String.fromCharCode(event.keyCode))) {
        //如果.已经存在
        //		if (_value.indexOf(".") > -1) {
        //			event.returnValue = false;
        //			return false;
        //		}
        return true;
    }
    else { return false; }
}
function setAMoney(_this) {
    if ("%" != String.fromCharCode(event.keyCode) && "'" != String.fromCharCode(event.keyCode)) {
        var val = "";
        var don = "";
        var str = _this.value.replace(/,/g, "");
        var num = str;
        if (str.indexOf(".") > -1) {
            num = str.split(".")[0];
            don = str.split(".")[1];
            if (don.length > 2) don = don.substr(0, 2);
        }
        var i = num.length - 1;
        var cstr = "";
        while (i > 2) {
            cstr = "," + num.substr(i - 2, 3) + cstr;
            val = num.substring(0, i - 2) + cstr;
            i = i - 3;
        }
        if (val != "") { if (str.indexOf(".") > -1) _this.value = val + "." + don; else _this.value = val; }
    }
}


function SetCurrency(_value) {
    if (_value <= 1) { return _value; }
    if (_value == "") { return 0; }
    if (isNaN(_value)) { _value = $.trim(_value); _value = _value.replace(/,/g, ""); }
    _value = parseFloat(_value).toFixed(2);
    var val = "";
    var don = "";
    var num = _value;
    num = _value.split(".")[0];
    don = _value.split(".")[1];
    var i = num.length - 1;
    var cstr = "";
    while (i > 2) {
        cstr = "," + num.substr(i - 2, 3) + cstr;
        val = num.substring(0, i - 2) + cstr;
        i = i - 3;
    }
    if (val != "") {
        _value = (val + "." + don);
    }
    return _value;
}

$(document).ready(function() {
    $("*[num='currency']").each(function() {
        if ($.trim($(this).html()) != "")
            $(this).html(SetCurrency($(this).html()));
        else
            $(this).val(SetCurrency($(this).val()));
    });
    //	for (var j = 0; j < $("*[num='currency']").length; j++) {
    //		var _this = $("*[num='currency']:eq(" + j + ")");
    //		var _val = SetCurrency(_this.html());
    //		_this.html(_val);
    //	}
});

function returnNum(val) {
    //if (isNaN(val)) {return val;}
    if (val == "") { return ""; }
    if (!val) { return ""; }
    return val.replace(/,/g, "");
}

//function CheckMoney(_this){
//	var _value = _this.value;
//	var kc=event.keyCode;
//	var _keyValue=String.fromCharCode(kc);
//	//输入.
//	if(/^\.$/.test(_keyValue)){
//		//第一个不允许输入.
//		if(_value.length==0){
//			window.event.returnValue=false;
//			return false;
//		}
//		//如果.已经存在
//		if(_value.indexOf(".")>-1){
//			window.event.returnValue=false;
//			return false;
//		}
//	}
//	//只能输入数字或.
//	
////	alert((/^\d$/.test(_keyValue))+"||"+((kc<48 || kc>57) && kc!=46)+"||"+ /\.\d\d$/.test(_value)+"||"+_value);
////	if(!((kc<48 || kc>57) && kc!=46 || /^\.\d\d$/.test(_value))){
////		window.event.returnValue = true;
////        return;
////	}
////	if(/[\d.]/.test(_keyValue)|| /^\.\d\d$/.test(_value)){
////		window.event.returnValue = true;
////        return;
////	}
//	if(/[\d.]/.test(String.fromCharCode(kc))){
//		window.event.returnValue = true;
//		return true;
//	}
//	else if(kc==8){//退格
//		_this.select();
//		_this.value="";
//		_this.focus();
//		window.event.returnValue = false;
//		return false;
//	}
//	else if(kc==13){
//		event.keyCode=9;
//        event.returnValue = true;
//        return true;
//	}
//	else if(kc==9 || kc==45 || kc==46 || kc==16 || kc==17 || kc==18 || kc==20 || (kc>=112 && kc<=123)){
//        event.returnValue = true;
//        return true;
//    }
//    else{
//		event.returnValue = true;
//		return true;
//    }
//}

//function CheckNum(thetxtNum)
//{
//var _value = thetxtNum.value;
//var kc=window.event.keyCode;

//if(kc==110 || kc==190)//如果是.
//{
//if(_value.length==0)//第一个不允许输入。
//{
//   alert("不能以小数点开头！");
//   window.event.returnValue = false;
//   return;
//}
//if(_value.indexOf(".")>=0)//如果已经存在.
//{
//   //window.event.keyCode=8;
//   alert("不能再次输入小数点！");
//   window.event.returnValue = false;
//   return;
//}
//}

////alert(kc);
//if( (kc>=48 && kc<=57) || (kc>=96 && kc<=105) || kc==110 || kc==190)//如果是数字 或 .
//{
//window.event.returnValue = true;
//return;
//}
//else if(kc==8)//如果是退格
//{
//thetxtNum.select();
//thetxtNum.value="";
//thetxtNum.focus();
//window.event.returnValue = false;
//return;
//}
//else if(kc==13)
//     {
//         window.event.keyCode=9;
//         window.event.returnValue = true;
//         return;
//     }
//else if(kc==9 || kc==45 || kc==46 || kc==16 || kc==17 || kc==18 || kc==20 || (kc>=112 && kc<=123))
//     {
//         window.event.returnValue = true;
//         return;
//     }
//else
//{
////window.event.keyCode=8;
//alert("请输入数字！");
//window.event.returnValue = false;
//return;
//}
//}

////保留小数点后一位
//function Transfer(thetxtNum)
//{
//var _value= parseFloat(thetxtNum.value);
//if(isNaN(_value))
//     {
//         return;
//     }
//thetxtNum.value = _value.toFixed(1);
//}


//jQuery.fn.formatmoney = function() {
//	$(this).bind("change", function() {
//		s = $(this).val();
//		dh = /,/;
//		while (dh.test(s)) {
//			s = s.replace(dh, "");
//		}
//		if (isNaN(s)) {
//			alert("您输入的可能不是数字");
//			return false;
//		}
//		s = s.replace(/^(\d*)$/, "$1.");
//		s = (s + "00").replace(/(\d*\.\d\d)\d*/, "$1");
//		s = s.replace(".", ",");
//		var re = /(\d)(\d{3},)/;
//		while (re.test(s)) {
//			s = s.replace(re, "$1,$2");
//		}
//		s = s.replace(/,(\d\d)$/, ".$1");
//		$(this).val(s.replace(/^\./, "0."));
//	});
//};
//$("input[name=AmountType]").formatmoney();

var SelectTagBox;
function showSelectTag(boxid, url) {
    SelectTagBox = boxid;
    $("#SelectionItem").load(url, function() {
        $("#SelectionItem").find("#Keyword").val($("#" + boxid).val());
        showBox("SelectionItem");
    });
}
var iframeId="";
function showSelectTagiFrame(boxid, url, ifrId) {
    SelectTagBox = boxid;
    iframeId = ifrId;
    $("#SelectionItem").find("#Keyword").val($("#" + iframeId).contents().find("#" + boxid).val());
    showBox("SelectionItem");
}


function LoadDebug() {
    $("*[isLoad=true]").each(function() {
        $(this).css("border", "3px solid red");
        $(this).click(function() {
            window.open($(this).data("LoadUrl"));
        });
    });
}