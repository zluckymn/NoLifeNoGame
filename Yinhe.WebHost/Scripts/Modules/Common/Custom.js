function regInput(obj, reg, inputStr) {
    var docSel = document.selection.createRange();
    if (docSel.parentElement().tagName != "INPUT") return false;
    oSel = docSel.duplicate();
    oSel.text = "";
    var srcRange = obj.createTextRange();
    oSel.setEndPoint("StartToStart", srcRange);
    var str = oSel.text + inputStr + srcRange.text.substr(oSel.text.length);
    return reg.test(str);
}

//获取滚动条移动top信息
function getScrollT() {
    var t, l, w, h;
    if (document.documentElement && document.documentElement.scrollTop) {

        t = document.documentElement.scrollTop;

    } else if (document.body) {
        t = document.body.scrollTop;

    }
    return t;
}

function checkIE() {
    var X, V, N;
    V = navigator.appVersion;
    N = navigator.appName;
    if (N == "Microsoft Internet Explorer")
        X = parseFloat(V.substring(V.indexOf("MSIE") + 5, V.lastIndexOf("Windows")));
    else
        X = parseFloat(V);
    return X;
}

function SetErrorImg(obj, type) {
    obj.onerror = "";
    ErrorImg(obj, type);
    //window.setTimeout(ErrorImg, 100, obj,type);
}

function ErrorImg(obj, type) {
    if (type == "m") {
        obj.src = "/Content/Images/zh-cn/Common/default_m.png";
    }
    else if (type == "hl") {
    obj.src = "/Content/Images/zh-cn/Common/sys-icon/default_hl.png";
    }
    else {
        obj.src = "/Content/Images/zh-cn/Common/default_l.png";
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


function G(objid) {
    return document.getElementById(objid);
}

//GroupManage.html
function moveOrg(id, name) {
    G("Orgid").value = id;
    $("#sp_curCatName").html(name);
    window.ifmtree.location.href = "/HumanResources/Organization/OrgTree";
    //Popdiv('move');
    showBox('move');
}

//小耀修改，目的是新旧弹窗互换，修改时间2011，07，28 加载树结构 未完成
//function moveOrg(id, name) {

//    box("<div class='shadow-container' id='move' style='display: none; position: absolute;' ></div>",
//         { title: '移动', contentType: 'html',
//        onOpen: function(o) {
//                    G("bkid").value = id;
//            G("Orgid").value = id;
//            $("#sp_curCatName").html(name);
//                $("#move", o.db).SelectTree({
//                    startXml: "/HumanResources/Organization/StartStateXmlOrgPost",
//                    asynUrl: "/HumanResources/Organization/AsynStateXmlOrgPost",
//                    defaultShowItemLv: 1,
//                    hasCheckBox: false 

//        });
//        },
//        onload:function(o){
//            //window.ifmtree.location.href = "/HumanResources/Organization/OrgTree";
//        },
//        submit_cb: function(o) {
//            AddNewOrg();
//        }
//    });

//GroupManage.html
//小耀修改，目的是新旧弹窗互换，修改时间2011，07，28
//function Addbk(id) {
//    G("bkid").value = id;
//    //Popdiv('add');
//    showBox('add');
//}

function Addbk(id) {
    box("#add", { title: '新增', contentType: 'selector',
        onOpen: function(o) {
            G("bkid").value = id;
        },
        submit_cb: function(o) {
            AddNewOrg();
        }
    });
}

function changeto(title, id) {
    G("Orgtoid").value = id;
    G("Msg").innerHTML = "移动到：" + title;
    G("jd1").disabled = false;
    G("jd2").disabled = false;
    G("jd3").disabled = false;
}

//GroupManage.html
function moveto(action, type) {
    G("action").value = action;
    //这里写处理事件
    //var url = "/HumanResources/Organization/MoveTree?orgid=" + G("Orgid").value + "&toorgid=" + G("Orgtoid").value + "&type=" + type;

    //window.location.href = url;

    //这里写处理事件
    var moveid = $("#Orgid").attr("value");
    var tomoveid = $("#Orgtoid").attr("value");
    if (moveid == tomoveid) {
        top.showInfoBar("不能移动到节点本身");
        return false;
    }
    var rand = Math.random();
    $.get(
           "/HumanResources/Organization/MoveTree",
           {
               moveid: moveid,
               tomoveid: tomoveid,
               type: type,
               r: rand
           },
            function(data) {
                var result = eval("(" + data + ")");
                if (result.success) {
                    window.location.reload();
                } else {
                    alert(result.errors.msg);
                }
            }
        );

}

//OrgEdit_info.html
function addtag(html) {
    if (G("tags").value.length == 0) { G("tags").value = html; } else { G("tags").value += "," + html; }
    //Hidediv('tag');
    HideBox(this, 'tag');
}

//GroupManage.html
function editG(id) {
    G("id").value = id;
    //中间编写取数据部分
    Popdiv("editGroup");
}
function deleteSelect(id) {
    $("#" + id).find("option").each(function() {
        if ($(this).attr("selected") == true) { $(this).remove(); }
    });
}

function selectcheckone(obj) {
    if ($(obj).find("input[type=checkbox]").attr("checked") == true)
    { $(obj).find("input[type=checkbox]").attr("checked", false); }
    else
    { $(obj).find("input[type=checkbox]").attr("checked", true); }
}

function hideAllSelect() {
    if (checkIE() == "6") { $("select[multiple=true]").each(function() { $(this).hide(); }); }
}

function showAllSelect() {
    if (checkIE() == "6") { $("select[multiple=true]").each(function() { $(this).show(); }); }
}

function getSubStrLen(sSource, iLen, maxIsOmit) {
    if (sSource.replace(/[^\x00-\xff]/g, "xx").length <= iLen)
        return sSource;
    var str = "";
    var reLen = 0;
    var schar;
    for (var i = 0; schar = sSource.charAt(i); i++) {
        str += schar;
        reLen += (schar.match(/[^\x00-\xff]/) != null ? 2 : 1);
        if (reLen >= iLen)
            break;
    }
    if (maxIsOmit)
        return str + "...";
    return str;
}

function ShowBMask() {
    if (!document.getElementById("Bmask")) {
        document.documentElement.style.overflow = "hidden";
        var mask = '<div style="margin-top:25%; text-align:center; background:url(/Content/Images/zh-cn/bg.jpg) no-repeat center top; height:86px">';
        mask += '<table height="95"><tr><td width="25"><img src="/Content/Images/zh-cn/loading.gif" width="20" height="20" /></td><td style="font-size:14px; font-weight:bold; color:#DCDCDC">正在加载...</td></tr></table>';
        mask += '</div>';

        $(document.body).append('<div id=Bmask style="filter:alpha(opacity=50); position:absolute; top:0px; left:0px; height:100%; width:100%; background-color:Black">' + mask + '</div>');
        $("#Bmask").css("top", $(document.documentElement).scrollTop());

        var topindex = 0;
        $("div").each(function() {
            if (parseInt($(this).css("z-index")) > topindex)
            { topindex = parseInt($(this).css("z-index")); }
        });

        $("#Bmask").css("z-index", topindex + 2);
    }
}

function RemoveBMask() {
    $("#Bmask").remove();
    document.documentElement.style.overflow = "auto";
}

///obj={togAtt:'',showImg:'',hideImg:'',allShowId:['',''],allHideId:['','']}
/// togAtt: 标志缩放的属性名称
/// showImg: 展开图标
/// hideImg: 折叠图标
/// allShowId: 全部展开按钮:数组
/// allHideId: 全部折叠按钮：数组
function ListToggle(obj) {
    if (obj.togAtt == null) {
        obj.togAtt = 'tog';
    }

    if (obj.hideImg == null) {
        obj.hideImg = '';
    }
    if (obj.showImg == null) {
        obj.showImg = '';
    }

    $('[tog=tog]').each(function(i) {
        var togId = $(this).attr('togId');

        // 为每个图标绑定事件
        $(this).click(function() {
            if ($(this).attr("src").indexOf(obj.showImg) == -1) {

                $("#" + togId).hide();
                $(this).attr("src", obj.showImg);
            } else {
                $("#" + togId).show();
                $(this).attr("src", obj.hideImg);
            }
        })

        //收缩全部
        if (i != 0) {
            $("#" + togId).hide();
            $("img[togId=" + togId + "]").attr("src", obj.showImg);
        } else {
            $("img[togId=" + togId + "]").attr("src", obj.hideImg);
        }
    })

    $(obj.allShowId).each(function(i) {
        $("#" + obj.allShowId[i]).click(function() {
            $('[tog=tog]').each(function(i) {
                var togId = $(this).attr('togId');
                $("#" + togId).show();
                $(this).attr("src", obj.hideImg);
            })
        })
    })

    $(obj.allHideId).each(function(i) {
        $("#" + obj.allHideId[i]).click(function() {
            $('[tog=tog]').each(function(i) {
                var togId = $(this).attr('togId');
                $("#" + togId).hide();
                $(this).attr("src", obj.showImg);
            })
        })
    })
}


function getChinaDayFromDate(date) {
    var bsYear;
    var bsDate;
    var bsWeek;
    var arrLen = 8; //数组长度 
    var sValue = 0; //当年的秒数 
    var dayiy = 0; //当年第几天 
    var miy = 0; //月份的下标 
    var iyear = 0; //年份标记 
    var dayim = 0; //当月第几天 
    var spd = 86400; //每天的秒数 
    var year1999 = "30;29;29;30;29;29;30;29;30;30;30;29"; //354 
    var year2000 = "30;30;29;29;30;29;29;30;29;30;30;29"; //354 
    var year2001 = "30;30;29;30;29;30;29;29;30;29;30;29;30"; //384 
    var year2002 = "30;30;29;30;29;30;29;29;30;29;30;29"; //354 
    var year2003 = "30;30;29;30;30;29;30;29;29;30;29;30"; //355 
    var year2004 = "29;30;29;30;30;29;30;29;30;29;30;29;30"; //384 
    var year2005 = "29;30;29;30;29;30;30;29;30;29;30;29"; //354 
    var year2006 = "30;29;30;29;30;30;29;29;30;30;29;29;30";
    var month1999 = "正月;二月;三月;四月;五月;六月;七月;八月;九月;十月;十一月;十二月"
    var month2001 = "正月;二月;三月;四月;闰四月;五月;六月;七月;八月;九月;十月;十一月;十二月"
    var month2004 = "正月;二月;闰二月;三月;四月;五月;六月;七月;八月;九月;十月;十一月;十二月"
    var month2006 = "正月;二月;三月;四月;五月;六月;七月;闰七月;八月;九月;十月;十一月;十二月"
    var Dn = "初一;初二;初三;初四;初五;初六;初七;初八;初九;初十;十一;十二;十三;十四;十五;十六;十七;十八;十九;二十;廿一;廿二;廿三;廿四;廿五;廿六;廿七;廿八;廿九;三十";
    var Ys = new Array(arrLen);
    Ys[0] = 919094400; Ys[1] = 949680000; Ys[2] = 980265600;
    Ys[3] = 1013443200; Ys[4] = 1044028800; Ys[5] = 1074700800;
    Ys[6] = 1107878400; Ys[7] = 1138464000;
    var Yn = new Array(arrLen); //农历年的名称 
    Yn[0] = "己卯年"; Yn[1] = "庚辰年"; Yn[2] = "辛巳年";
    Yn[3] = "壬午年"; Yn[4] = "癸未年"; Yn[5] = "甲申年";
    Yn[6] = "乙酉年"; Yn[7] = "丙戌年";
    var D = new Date(date);
    var yy = D.getYear();
    var mm = D.getMonth() + 1;
    var dd = D.getDate();
    var ww = D.getDay();
    if (ww == 0) ww = "<font color=RED>星期日";
    if (ww == 1) ww = "星期一";
    if (ww == 2) ww = "星期二";
    if (ww == 3) ww = "星期三";
    if (ww == 4) ww = "星期四";
    if (ww == 5) ww = "星期五";
    if (ww == 6) ww = "<font color=RED>星期六";
    ww = ww;
    var ss = parseInt(D.getTime() / 1000);
    if (yy < 100) yy = "19" + yy;
    for (i = 0; i < arrLen; i++)
        if (ss >= Ys[i]) {
        iyear = i;
        sValue = ss - Ys[i]; //当年的秒数 
    }
    dayiy = parseInt(sValue / spd) + 1; //当年的天数 
    var dpm = year1999;
    if (iyear == 1) dpm = year2000;
    if (iyear == 2) dpm = year2001;
    if (iyear == 3) dpm = year2002;
    if (iyear == 4) dpm = year2003;
    if (iyear == 5) dpm = year2004;
    if (iyear == 6) dpm = year2005;
    if (iyear == 7) dpm = year2006;
    dpm = dpm.split(";");
    var Mn = month1999;
    if (iyear == 2) Mn = month2001;
    if (iyear == 5) Mn = month2004;
    if (iyear == 7) Mn = month2006;
    Mn = Mn.split(";");
    var Dn = "初一;初二;初三;初四;初五;初六;初七;初八;初九;初十;十一;十二;十三;十四;十五;十六;十七;十八;十九;二十;廿一;廿二;廿三;廿四;廿五;廿六;廿七;廿八;廿九;三十";
    Dn = Dn.split(";");
    dayim = dayiy;
    var total = new Array(13);
    total[0] = parseInt(dpm[0]);
    for (i = 1; i < dpm.length - 1; i++) total[i] = parseInt(dpm[i]) + total[i - 1];
    for (i = dpm.length - 1; i > 0; i--)
        if (dayim > total[i - 1]) {
        dayim = dayim - total[i - 1];
        miy = i;
    }
    bsWeek = ww;
    bsDate = yy + "年" + mm + "月" + dd + "日";
    bsDate2 = dd;
    bsYear = "农历" + Yn[iyear];
    bsYear2 = Mn[miy] + Dn[dayim - 1];
    if (ss >= Ys[7] || ss < Ys[0]) bsYear = Yn[7];
    return bsDate + "&nbsp;" + bsWeek + '&nbsp;' + bsYear + "&nbsp;" + bsYear2; //（<font color="#f76120">腊八节</font>）
}

//因性能优化，且较少客户使用，删除改接口
//$(document).ready(function() {
//    if (window.location.href == top.window.location.href) {
//        if (window.isIndex) {
//            //首页
//            MessageCenter(1);
//        } else {
//            //非首页
//            MessageCenter(0);
//        }
//    }
//});

var DataString;
var MessageHistory = "";
var isindex = "";
var MessageCookieName = "";
function MessageCenter(isIndex) {
    isindex = isIndex;
    var url = "/Settings/Size/GetBroadcast?e=" + Math.random();

    $.get(url, function(data) {
        DataString = data;

        if (DataString == "[]") { return false; }
        //DataString = [{ name: "重要提示11111", id: "1", html: "这里是重要提示的内容", poptype: "1", popstep: "30" }, { name: "重要提示22222", id: "2", html: "这里是3232132132321事实上重要提示的内容", poptype: "1", popstep: "30" }, { name: "重要提示33333", id: "3", html: "这里是3213213213重要提示的内容", poptype: "1", popstep: "30"}];
        var CookieSysMessageCenter = getCookie(location.hostname.replace(/\./g, "") + location.port + "SysMessageCenter");

        //cookie格式
        //CookieSysMessageCenter = id|poptype|lastpoptime|timestep|isindex,id|poptype|lastpoptime|timestep|isindex
        if (CookieSysMessageCenter != null) {
            var newCookieSysMessageCenter = "";
            var SysMessageCenterArr = CookieSysMessageCenter.split(",");
            for (var smc = 0; smc < SysMessageCenterArr.length; smc++) {
                $(DataString).each(function() {
                    if (parseInt($(this).attr("id"), 10) == parseInt(SysMessageCenterArr[smc].split("|")[0], 10)) {
                        if (newCookieSysMessageCenter == "") {
                            newCookieSysMessageCenter = $(this).attr("id") + "|" + $(this).attr("poptype") + "|" + SysMessageCenterArr[smc].split("|")[2] + "|" + $(this).attr("popstep") + "|" + $(this).attr("range");
                        }
                        else {
                            newCookieSysMessageCenter += "," + $(this).attr("id") + "|" + $(this).attr("poptype") + "|" + SysMessageCenterArr[smc].split("|")[2] + "|" + $(this).attr("popstep") + "|" + $(this).attr("range");
                        }
                    }
                });
            }
            $(DataString).each(function() {
                var addSmc = true;
                for (var smc = 0; smc < SysMessageCenterArr.length; smc++) {
                    if (parseInt($(this).attr("id"), 10) == parseInt(SysMessageCenterArr[smc].split("|")[0], 10)) { addSmc = false; }
                }
                if (addSmc == true) {
                    if (newCookieSysMessageCenter == "") {
                        newCookieSysMessageCenter = $(this).attr("id") + "|" + $(this).attr("poptype") + "||" + $(this).attr("popstep") + "|" + $(this).attr("range");
                    }
                    else {
                        newCookieSysMessageCenter += "," + $(this).attr("id") + "|" + $(this).attr("poptype") + "||" + $(this).attr("popstep") + "|" + $(this).attr("range");
                    }
                }
            });
            setCookie(location.hostname.replace(/\./g, "") + location.port + "SysMessageCenter", newCookieSysMessageCenter, 365);
        } else {
            var newCookieSysMessageCenter = "";
            $(DataString).each(function() {
                if (newCookieSysMessageCenter == "") {
                    newCookieSysMessageCenter = $(this).attr("id") + "|" + $(this).attr("poptype") + "||" + $(this).attr("popstep") + "|" + $(this).attr("range");
                }
                else {
                    newCookieSysMessageCenter += "," + $(this).attr("id") + "|" + $(this).attr("poptype") + "||" + $(this).attr("popstep") + "|" + $(this).attr("range");
                }
            });
            setCookie(location.hostname.replace(/\./g, "") + location.port + "SysMessageCenter", newCookieSysMessageCenter, 365);
        }
        if (DataString.length > 0) {
            showMessageCenter();
        }
    });
}

function GetshowMessage() {
    var SysMessageCenterArr = getCookie(location.hostname.replace(/\./g, "") + location.port + "SysMessageCenter").split(",");

    for (var smc = SysMessageCenterArr.length - 1; smc >= 0; smc--) {
        //如果上次弹出事件为空则可以直接弹出
        if (SysMessageCenterArr[smc].split("|")[2].length == 0 && getMessageHistoryIndex(SysMessageCenterArr[smc].split("|")[0]) == -1 && SysMessageCenterArr[smc].split("|")[4] <= isindex) {
            UpdateMessageCenterPopTime(SysMessageCenterArr[smc].split("|")[0]);
            return SysMessageCenterArr[smc].split("|")[0];
        }
        //如果属于一次弹出并且弹出事件不为空的直接排除,如果属于多次弹出的，判断上次弹出时间
        if (SysMessageCenterArr[smc].split("|")[1] == 0) {
            var TesttimeStep = (new Date().getTime() - SysMessageCenterArr[smc].split("|")[2]) / 3600 / 1000;
            if (TesttimeStep >= SysMessageCenterArr[smc].split("|")[3] && getMessageHistoryIndex(SysMessageCenterArr[smc].split("|")[0]) == -1 && SysMessageCenterArr[smc].split("|")[4] <= isindex) {
                UpdateMessageCenterPopTime(SysMessageCenterArr[smc].split("|")[0]);
                return SysMessageCenterArr[smc].split("|")[0];
            }
        }
    }
    return 0;
}

function GetshowMessageCount() {
    var conut = 0;
    var SysMessageCenterArr = getCookie(location.hostname.replace(/\./g, "") + location.port + "SysMessageCenter").split(",");

    for (var smc = SysMessageCenterArr.length - 1; smc >= 0; smc--) {
        //如果上次弹出事件为空则可以直接弹出
        if (SysMessageCenterArr[smc].split("|")[2].length == 0 && getMessageHistoryIndex(SysMessageCenterArr[smc].split("|")[0]) == -1 && SysMessageCenterArr[smc].split("|")[4] <= isindex) {
            conut++;
        }
        else {
            //如果属于一次弹出并且弹出事件不为空的直接排除,如果属于多次弹出的，判断上次弹出时间
            if (SysMessageCenterArr[smc].split("|")[1] == 0) {
                var TesttimeStep = (new Date().getTime() - SysMessageCenterArr[smc].split("|")[2]) / 3600 / 1000;
                if (TesttimeStep >= SysMessageCenterArr[smc].split("|")[3] && getMessageHistoryIndex(SysMessageCenterArr[smc].split("|")[0]) == -1 && SysMessageCenterArr[smc].split("|")[4] <= isindex) {
                    conut++;
                }
            }
        }
    }
    return conut;
}

function UpdateMessageCenterPopTime(id) {
    var newCookieSysMessageCenter = "";
    var SysMessageCenterArr = getCookie(location.hostname.replace(/\./g, "") + location.port + "SysMessageCenter").split(",");
    for (var smc = 0; smc < SysMessageCenterArr.length; smc++) {
        if (id == SysMessageCenterArr[smc].split("|")[0]) {
            if (newCookieSysMessageCenter == "") {
                newCookieSysMessageCenter = SysMessageCenterArr[smc].split("|")[0] + "|" + SysMessageCenterArr[smc].split("|")[1] + "|" + new Date().getTime() + "|" + SysMessageCenterArr[smc].split("|")[3] + "|" + SysMessageCenterArr[smc].split("|")[4];
            }
            else {
                newCookieSysMessageCenter += "," + SysMessageCenterArr[smc].split("|")[0] + "|" + SysMessageCenterArr[smc].split("|")[1] + "|" + new Date().getTime() + "|" + SysMessageCenterArr[smc].split("|")[3] + "|" + SysMessageCenterArr[smc].split("|")[4];
            }
        }
        else {
            if (newCookieSysMessageCenter == "") {
                newCookieSysMessageCenter = SysMessageCenterArr[smc].split("|")[0] + "|" + SysMessageCenterArr[smc].split("|")[1] + "|" + SysMessageCenterArr[smc].split("|")[2] + "|" + SysMessageCenterArr[smc].split("|")[3] + "|" + SysMessageCenterArr[smc].split("|")[4];
            }
            else {
                newCookieSysMessageCenter += "," + SysMessageCenterArr[smc].split("|")[0] + "|" + SysMessageCenterArr[smc].split("|")[1] + "|" + SysMessageCenterArr[smc].split("|")[2] + "|" + SysMessageCenterArr[smc].split("|")[3] + "|" + SysMessageCenterArr[smc].split("|")[4];
            }
        }
    }
    setCookie(location.hostname.replace(/\./g, "") + location.port + "SysMessageCenter", newCookieSysMessageCenter, 365);
}


function setMessageHistory(index) {
    if (MessageHistory != "") {
        var arr = MessageHistory.split(",");
        for (var x = 0; x < arr.length; x++) {
            if (arr[x] == index) { return x; }
        }
    }

    if (MessageHistory == "") { MessageHistory = "" + index; }
    else { MessageHistory += "," + index; }
    return -1;
}


function getMessageHistoryIndex(index) {

    var arr = MessageHistory.split(",");
    for (var x = 0; x < arr.length; x++) {
        if (arr[x] == index) { return x; }
    }
    return -1;
}

function showMessageCenter(Hisindex) {
    var showid = 0;
    if (Hisindex) {
        showid = Hisindex;
        //上一页 读取旧内容
        //        var arr = MessageHistory.split(",");
        //        for (var x = 0; x < arr.length; x++) {
        //            if (Hisindex = arr[x]) { showid = arr[x - 1]; }
        //        }
    }
    else {
        //下一页 读取新内容
        showid = GetshowMessage();
        if (showid == 0) { return false; }
        //写入历史记录
        setMessageHistory(showid);
    }

    var index = 0;
    $(DataString).each(function(i) {
        if (showid == $(this).attr("id")) {
            index = i;
        }
    });

    //取出内容，显示弹窗
    $("#SysMessageCenter").find("#SysMessageCenterBox").html(DataString[index].html);
    $("#SysMessageCenter").find(".name").html(DataString[index].name);
    top.initSignalBox("SysMessageCenter");
    top.showBox("SysMessageCenter");


    //判断写入哪些按钮
    $("#SysMessageCenter").find(".btn").html("");

    if (setMessageHistory(showid) >= 1) {
        $("#SysMessageCenter").find(".btn").append("<INPUT name=prev onclick='showMessageCenter(" + MessageHistory.split(",")[setMessageHistory(showid) - 1] + ");' value=' 上一条 ' type=button>");
    }

    if (setMessageHistory(showid) < MessageHistory.split(",").length - 1) {
        $("#SysMessageCenter").find(".btn").append("<INPUT name=next onclick='showMessageCenter(" + MessageHistory.split(",")[setMessageHistory(showid) + 1] + ");' value=' 下一条 ' type=button>");
    }

    var showMessageCount = GetshowMessageCount();

    if ($("#SysMessageCenter").find(".btn").find("input[name=next]").length == 0) {
        if (showMessageCount > 0) {
            $("#SysMessageCenter").find(".btn").append("<INPUT name=next onclick='showMessageCenter();' value=' 下一条 ' type=button>");
        }
    }

    if (showMessageCount == 0) {
        $("#SysMessageCenter").find(".btn").append("<INPUT name=close onclick='HideBox(this);' value=' 关闭 ' type=button>");
        $("#SysMessageCenter").find(".oper").find("a").show();
    }
}

function ShowStepTip(url) {
    $(document).ready(function() {
        $("#centerMsg").remove();
        $(".content:first").prepend("<div id=centerMsg style='display:none;'></div>");
        $("#centerMsg").load(url, function() {
            if ($("centerMsg").html() != "") { $("#centerMsg").fadeIn("slow"); }
        });
    });
}


//从序列化好的参数列表中查找参数值
function GetValFromParam(key, params) {
    var arrPairs = params.split("&");
    for (var i = 0, len = arrPairs.length; i < len; i++) {
        if (arrPairs[i].indexOf("=") != -1) {
            var name = arrPairs[i].split("=")[0];
            var value = arrPairs[i].split("=")[1];
            if (name == key) {
                return value;
            }
        }
    }
    return "";
}

//判断年份是否是闰年

function isLeapYear(year) {

    if (year % 400 == 0) {
        return false;
    } else if (year % 4 == 0) {
        return true;
    } else {
        return false;
    }
}

//计算两个日期的差值
function compareDate(date1, date2) {
    var regexp = /^(\d{1,4})[-|\.]{1}(\d{1,2})[-|\.]{1}(\d{1,2})$/;
    var monthDays = [0, 3, 0, 1, 0, 1, 0, 0, 1, 0, 0, 1];
    regexp.test(date1);
    var date1Year = RegExp.$1;
    var date1Month = RegExp.$2;
    var date1Day = RegExp.$3;

    regexp.test(date2);
    var date2Year = RegExp.$1;
    var date2Month = RegExp.$2;
    var date2Day = RegExp.$3;

    firstDate = new Date(date1Year, date1Month, date1Day);
    secondDate = new Date(date2Year, date2Month, date2Day);

    result = Math.floor((secondDate.getTime() - firstDate.getTime()) / (1000 * 3600 * 24));
    for (j = date1Year; j <= date2Year; j++) {
        if (isLeapYear(j)) {
            monthDays[1] = 2;
        } else {
            monthDays[1] = 3;
        }
        for (i = date1Month - 1; i < date2Month; i++) {
            result = result - monthDays[i];
        }
    }
    return result;
}


//计算日期加上天数后的日期
function addDays(date1, days) {
    var monthDays = [0, 3, 0, 1, 0, 1, 0, 0, 1, 0, 0, 1];
    var regexp = /^(\d{1,4})[-|\.]{1}(\d{1,2})[-|\.]{1}(\d{1,2})$/;
    regexp.test(date1);
    var date1Year = RegExp.$1;
    var date1Month = RegExp.$2-1;
    var date1Day = RegExp.$3;
    firstDate = new Date(date1Year, date1Month, date1Day);
    firstDate.setTime(firstDate.getTime() + days * 1000 * 3600 * 24);
    var diff = 0;
    for (j = date1Year; j <= firstDate.getYear(); j++) {
        if (isLeapYear(j)) {
            monthDays[1] = 2;
        } else {
            monthDays[1] = 3;
        }
        for (i = date1Month - 1; i < firstDate.getMonth() - 1; i++) {
            diff = diff + monthDays[i];
        }
    }
    result = firstDate.getYear() + "-" + (firstDate.getMonth()+1) + "-" + firstDate.getDate();
    if (diff != 0) {
        result = addDays(result, diff);
    }
    return result;

}

// 给相应的 div tr 或者 td 加上高亮 Class
// 和显示文字按钮条（如果有）
// 并去掉所有其它元素之前被此函数加上的高亮状态
// 依赖 class : tr_hover 和 picbtn
function switchTrHoverStyle(trObj) {
    $(".tr_hover").removeClass("tr_hover");
    $(".picbtnshow").removeClass("picbtnshow").addClass("picbtn");

    trObj.addClass("tr_hover");
    trObj.find(".picbtn").addClass("picbtnshow").removeClass("picbtn");
}

//前端处理过长标准化名称打乱表格的情况
//====================================
function cutTooLongResult(divObj) {
    var maxNameShowLength = 36;

    var thisText = divObj.text();
    var subThisText = getSubStrLen(thisText, maxNameShowLength, ".");

    if (thisText != subThisText) {
        divObj.parent().find("img").attr("alt", thisText);
        divObj.parent().find("a").attr("title", thisText);
        divObj.find("a").html(subThisText);
    }
}

$(document).ready(function() {

    $("div.picname").each(function() {
        cutTooLongResult($(this));
    });

    $("div.seriesname").each(function() {
        cutTooLongResult($(this));
    });
});
//=======================================


//===================万科专用，用于显示个人相关消息提醒====================
function ShowPersonStandardInfo() {
    $.getJSON("/Standard/StandardResult/GetStandarRemindInfo?r=" + Math.random(), function(data) {
        var RetStr = "";
        $(data).each(function(i) {
            if (this.type == 1) {
                if (this.count1 == 0) {
                    RetStr += "<li onclick='window.location.href=\"\/Standard\/StandardResult\/FlowKeepingCenter\";'>您发布的标准被应用了" + this.count2 + "次</li>";
                }
                else {
                    RetStr += "<li onclick='window.location.href=\"\/Standard\/StandardResult\/FlowKeepingCenter\";'>您发布的标准被应用了" + this.count1 + "次(共" + this.count2 + "次)</li>";
                }
            }
            if (this.type == 2) {
                if (this.count1 + this.count2 == 0) {
                    RetStr += "<li onclick='window.location.href=\"\/Standard\/StandardResult\/FlowKeepingCenter\";'>您共有" + this.count3 + "个待处理申请</li>";
                }
                else {
                    RetStr += "<li onclick='window.location.href=\"\/Standard\/StandardResult\/FlowKeepingCenter\";'>您有" + this.count1 + "条新下载申请，" + this.count2 + "次催促(" + this.count3 + "个待处理申请)</li>";
                }
            }
            if (this.type == 3) {
                if (this.count1 + this.count2 == 0) {
                    RetStr += "<li onclick='window.location.href=\"\/Standard\/StandardResult\/InteractionKeepingCenter\";'>你能够下载的标准数量为" + this.count3 + "个</li>";
                }
                else {
                    if (this.type == 2) { RetStr += "<li onclick='window.location.href=\"\/Standard\/StandardResult\/InteractionKeepingCenter\";'>" + this.count1 + "条下载审批通过," + this.count2 + "条被拒绝(已通过标准共" + this.count3 + "个)</li>"; }
                }
            }
            if (this.type == 4) { if (this.count1 > 0) RetStr += "<li onclick='window.location.href=\"\/Standard\/StandardResult\/InteractionKeepingCenter\";'>" + this.count1 + "个您申请或应用过的标准化成果被修改</li>"; }
            if (this.type == 5) { if (this.count1 > 0) RetStr += "<li onclick='window.location.href=\"\/Standard\/StandardResult\/ProjectFeedBack\/" + this.id + "\";'>" + this.name + "添加了" + this.count1 + "条标准（共有" + this.count2 + "条标准）</li>"; }
        });
        var ShowPersonStandardInfobox = $('<div style="text-align:left;position:absolute;width: 280px;right:4px;  margin: 50px auto;border:1px solid #b0d5ee;background-color:White;"><div style="color:#0c75a4;background-color:#e4f3f6; font-size:14x; line-height:30px;height:30px;font-weight:bold;text-indent:5px;"><div style="float:right;cursor:hand;padding:7px 10px 0 0"><img src="/Content/Images/zh-cn/standproduct/ico-close3.gif" title="关闭"></div>最新提醒</div><ul style="height:160px;overflow:auto;" class="xxtx"></ul></div>');
        ShowPersonStandardInfobox.find("div").click(function() {
            clearInterval(ShowPersonStandardInfoStay);
            ShowPersonStandardInfobox.fadeOut();
        });
        ShowPersonStandardInfobox.find("ul").append(RetStr);
        ShowPersonStandardInfobox.find("li").css("cursor", "hand");
        $(document.body).append(ShowPersonStandardInfobox);
        //alert(ShowPersonStandardInfobox.find("li").length * 24);
        var showCount = 0;
        var ShowPersonStandardInfoStay = setInterval(function() {
            ShowPersonStandardInfobox.css("top", document.documentElement.scrollTop + document.documentElement.clientHeight - ShowPersonStandardInfobox.height() - 72 + "px");
            showCount++;
            if (showCount > 600) {
                clearInterval(ShowPersonStandardInfoStay);
                ShowPersonStandardInfobox.fadeOut();
            }
        }, 50);
    })
}

function preLoadImg(url) {
    var img = new Image();
    img.src = url;
}
preLoadImg("/Content/Images/zh-cn/Persons/ico11.jpg");
preLoadImg("/Content/Images/zh-cn/Persons/ico11-1.jpg");
