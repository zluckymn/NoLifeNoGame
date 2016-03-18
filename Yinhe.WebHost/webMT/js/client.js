
/*设置窗口的大小属性
*flashWidth属性:宽度
*flashHeight属性:高度
*若要在初始化的时候直接显示在div上，则需需要两个步骤1：设置cwf.isView=true和div的id设置方法cwf.flashId(id)；2：在div显示出来后调用readOnline(file, extraHtml)方法
*/
var cwf = cwf || {};
cwf.flashWidth = 0;
cwf.flashHeight = 0;
//添加是否直接显示在div上的属性
cwf.isView = false;
//指定显示的div的id
cwf.flashId = "undefined";
cwf.setFlashId = function (id) {
    cwf.flashId = id;


}

cwf.arrFileTableField = [
["0", "FileLibrary", "fileId"]
, ["1", "ProjectResultDoc", "projRetDocId"]
, ["2", "TargetAttachment", "docId"]
, ["3", "ProductAdjunct", "proAjdId"]
, ["4", "CusMagAttachment", "docId"]
, ["5", "SpecDocAttachment", "docId"]
, ["6", "ProjLibAttachment", "docId"]
, ["7", "BidProjAttachment", "docId"]
, ["8", "ProjTargetAdj", "targetAttId"]
, ["9", "MaterialDoc", "matDocId"]
, ["10", "ContractAdjunct", "adjId"]
, ["11", "MailRefAttachment", "mailRefAttId"]
, ["12", "CustRefFile", "custRefFileId"]
, ["13", "CommentAttachment", "ComAttId"]
, ["14", "CompetitorAttachment", "comAttId"]
, ["15", "TrainingAttachment", "caseDocId"]
, ["16", "AttributeAttachment", "caseDocId"]
];
cwf.suffixClass = { "arrSwfFileExt": [".pdf", ".doc", ".xls", ".ppt", ".docx", ".xlsx", ".pptx", ".txt"], "arrImageFileExt": [".jpg", ".gif", ".png", ".bmp", ".jpeg"], "arrDwgFileExt": [".dwg", ".dxf", ".dwf"] };

/**
* 在线阅读
* @param Object file 文件参数对象
* eg: file = {"guid":"abc", "name":"我的太阳", "ext":"", "id":"", "type":"", "ver":"", "swfUrl":"", imgUrl:""}
*/
cwf.readOnline = function (file, extraHtml) {
  
    var guid = file.guid, name = file.name, id = file.id,
        type = file.type, ver = file.ver, ext = file.ext, ck, _flag = true;
    if (guid == "") {
        hiAlert('文件尚未上传完成，请稍后阅读', '提示信息');
        return false;
    }
    ck = this.checkSuffix_(ext);
    if (ck == -1) { // 浏览的文件不再后缀列表里，提示下载
        cwf.needToDownLoad_(file);
    } else {
        
        switch (ck) {
            case "arrSwfFileExt": // 在线flash查看
                cwf.flexpaper.loadSWFFile_(file, extraHtml);
                break;
            case "arrImageFileExt": // 图片幻灯
                cwf.image.show_(file);
                break;
            case "arrDwgFileExt": // dwg view
                cwf.dwg_(file, extraHtml);
                break;
            default:
                _flag = false;
                cwf.needToDownLoad_(file);
                break;
        }
        if (_flag) {
            cwf.uploadFileStatNum_(id, type, 0);
        }
    }
};
/**
* 对于无法在线浏览的提示下载到本地查看
*/
cwf.needToDownLoad_ = function (file, msg, showAxdl) {
    if (MT.isObject(file) && MT.isUndefined(file.ext)) {
        file.ext = '';
    }
    var tpl = ['<div class="boxmargin">', '<div id="_g_loadtip" style="line-height:18px"></div>', '<div id="_g_fother">', '<p>{name}{ext}</p>', '</div>', '<div><input type="button" value="下载到本地" class="dinpt" id="_g_dinpt" />' + (showAxdl && showAxdl == "dwgAxdl" ? " <a href=\"/DwgControl/MxDrawX.msi\" target=\"_blank\">安装dwg插件</a>" : "") + '</div>', '</div>'],
        tips = "", str = "";
    tips = (msg == "" || MT.isUndefined(msg)) ? "<p>你所查看的文件暂不支持在线浏览，请下载到本地打开！</p><p>给你造成不便敬请谅解！</p>" : msg;
    str = tpl.join("");
    str = MT.string.compileTpl(str, file);
    box(str, { boxid: "needtodownload_", title: "需下载查看", no_submit: true, setZIndex: false, zIndex: 33333, width: 300,
        onOpen: function (o) {
            $("#_g_loadtip").html(tips);
            $("#_g_dinpt").click(function () {
                cwf.dl([{ guid: file.guid, name: file.name + file.ext}]);
                o.destroy();

            });
        }
    });
};
cwf.checkSuffix_ = function (ext) {
    if (!MT.isString(ext) || ext == "") return -1;
    var key, arr, idx;
    for (key in this.suffixClass) {
        arr = this.suffixClass[key];
        idx = MT.array.indexOf(ext.toLowerCase(), arr);
        if (idx != -1) return key;
    }
    return -1;
};
/**
* 文件下载.
* @param String guid 文件guid.
* @param String name 文件名称.
* @param String id 文件Id.
* @param String type 文档存储位置类别.
*/
cwf.downLoad = function (guid, name, id, type) {
    if (guid == "") {
        hiAlert('文件尚未上传完成，请稍后下载', '提示信息');
        return false;
    }
    $.get("/Home/IsUserCanDownLoad?time=" + Math.random(), function (num) {
        if (parseInt(num) <= 0) {
            hiAlert('您下载的文档数量已达上限', '无法下载');
            return false;
        } else {
            if (num <= fs.PopDownLimitLength) hiOverAlert('您还可以下载文档的数量为: ' + num);
            cwf.dl([{ guid: guid, name: name}]);
            cwf.uploadFileStatNum_(id, type, 1);
        }
    });
};
/**
* 更新文档统计数.
* @param String id 文档Id.
* @param String type 文档存储位置类别.
* @param Number downOrView 0-阅读 1--查看.
*/
cwf.uploadFileStatNum_ = function (id, type, downOrView) {
    var tableName = "",
        referFieldName = "",
        referFieldValue = id,
        arr = this.arrFileTableField[type];
    tableName = arr[1];
    referFieldName = arr[2];
    if (downOrView == null || typeof (downOrView) == "undefined" || downOrView == 1) {
        $.get('/Settings/Size/UpdateDownloadCount/' + id + "?tname=" + tableName + "&rname=" + referFieldName + "&r=" + Math.random());
    } else {
        $.get('/Settings/Size/UpdateViewCount/' + id + "?tname=" + tableName + "&rname=" + referFieldName + "&r=" + Math.random());
    }
};
/**
* flexpaper
*/
cwf.flexpaper = {};
//判断当前浏览器是否安装flash player，若安装返回当前版本
cwf.flexpaper.flashChecker = function () {
    var hasFlash = 0; //是否安装了flash
    var flashVersion = 0; //flash版本

    if (document.all) {
        var swf = new ActiveXObject('ShockwaveFlash.ShockwaveFlash');
        if (swf) {
            hasFlash = 1;
            VSwf = swf.GetVariable("$version");
            flashVersion = parseInt(VSwf.split(" ")[1].split(",")[0]);
        }
    } else {
        if (navigator.plugins && navigator.plugins.length > 0) {
            var swf = navigator.plugins["Shockwave Flash"];
            if (swf) {
                hasFlash = 1;
                var words = swf.description.split(" ");
                for (var i = 0; i < words.length; ++i) {
                    if (isNaN(parseInt(words[i]))) continue;
                    flashVersion = parseInt(words[i]);
                }
            }
        }
    }

    return { f: hasFlash, v: flashVersion }; //属性1是否安装 属性2当前版本号
}
cwf.flexpaper.loadSWFFile_ = function (file, extraHtml) {
   
    var Swfurl = file.swfUrl;
    var __gExtraHtml_ = typeof __gExtraHtml != 'undefined' ? __gExtraHtml : '';
    extraHtml = extraHtml || __gExtraHtml_;
    extraHtml = MT.string.compileTpl(extraHtml, file);
   
    if (Swfurl != "") {

        var vw = YH.dom.getViewportWidth(top.window);
        var tpl = '<div style="border: 1px solid #b8cdc6">' + extraHtml + '<div id="gvflashContent" style="display:none"><p>请安装 Adobe Flash Player version 10.0.0 及以上，确保控件正常显示.<a href="http://www.adobe.com/go/getflashplayer"><img src="/webMT/badge/get_flash_player.gif" alt="获取flash player" /></a></p></div></div>';

        //是否设置cwf.flashId的值,判断是否直接显示在div上
        if (cwf.isView == true && $("#" + cwf.flashId) != "undefined") {
            if (cwf.flexpaper.flashChecker().f) {

                if (cwf.flexpaper.flashChecker().v >= 10) {



                    //   if (!cwf.isExist(cwf.flashId)) {

                    var childDiv = '<div id="gvflashContent" style="display:none"></div>';
                    $("#" + cwf.flashId).append(childDiv);

                    $.ajax({ url: '/Scripts/Modules/YHFlexPaper/js/flexpaper_flash.js', dataType: 'script', cache: true, success: function () { cwf.flexpaper.initFlexpaper_(Swfurl); } });
                    //   }

                }
                else {
                    var flashDiv = '<div style="border: 1px solid #b8cdc6">' + extraHtml + '<div id="gvflashContent" ><p>请安装 Adobe Flash Player version 10.0.0 及以上，确保控件正常显示.<a href="http://www.adobe.com/go/getflashplayer"><img src="/webMT/badge/get_flash_player.gif" alt="获取flash player" /></a></p></div></div>';
                    $("#" + cwf.flashId).append(flashDiv);
                }

            }

        }


        else {


            box(tpl, { boxid: 'showFile', pos: 't-t', contentType: 'html', title: file.name, cls: 'shadow-container', width: vw - 200, no_submit: true, cancel_BtnName: '关 闭',
                onOpen: function (o) {
                    $.ajax({ url: '/Scripts/Modules/YHFlexPaper/js/flexpaper_flash.js', dataType: 'script', cache: true, success: function () { cwf.flexpaper.initFlexpaper_(Swfurl); } });
                }
            });
        }


    } else {
        cwf.needToDownLoad_(file, "文件转换中，暂时无法在线阅读，请下载阅读");
    }
};
cwf.flexpaper.initFlexpaper_ = function (Swfurl) {
    var swfVersionStr = "9.0.0",
        xiSwfUrlStr = "/Scripts/Modules/YHFlexPaper/js/swfobject/expressInstall.swf",
        flashvars = {
            SwfFile: Swfurl,
            Scale: 0.6,
            ZoomTransition: "easeOut",
            ZoomTime: 0.5,
            ZoomInterval: 0.1,
            FitPageOnLoad: false,
            FitWidthOnLoad: true,
            PrintEnabled: false,
            FullScreenAsMaxWindow: false,
            ProgressiveLoading: true,
            PrintToolsVisible: false,
            ViewModeToolsVisible: true,
            ZoomToolsVisible: true,
            FullScreenVisible: true,
            NavToolsVisible: true,
            CursorToolsVisible: true,
            SearchToolsVisible: true,
            localeChain: "zh_CN"
        },
        params = {},

        vw = YH.dom.getViewportWidth(top.window), vh = YH.dom.getViewportHeight(top.window);

    if (cwf.isView == false) {

        cwf.flashWidth = vw - 200;
        cwf.flashHeight = vh - 100;

    }
    else {

        cwf.flashWidth = $("#" + cwf.flashId).attr("flashWidth");
        cwf.flashHeight = $("#" + cwf.flashId).attr("flashHeight");

    }

    params.quality = "high";
    params.bgcolor = "#ffffff";
    params.allowscriptaccess = "always";
    params.allowfullscreen = "true";
    params.wmode = "Opaque";
    var attributes = {};
    attributes.id = "FlexPaperViewer";
    attributes.name = "FlexPaperViewer";

    swfobject.embedSWF(
            "/Scripts/Modules/YHFlexPaper/FlexPaperViewer.swf", "gvflashContent",
            cwf.flashWidth, cwf.flashHeight,
            swfVersionStr, xiSwfUrlStr,
            flashvars, params, attributes);
    swfobject.createCSS("#gvflashContent", "display:block;text-align:left;");
    //将cwf.setFlashId设置为undefined
    cwf.setFlashId("undefined");
    cwf.isView = false;

};
/**
* image view
*/
cwf.image = {};
cwf.image.show_ = function (file) {
    if (!file || !file.imgUrl) {
        alert("该图片不存在！");
        return false;
    }
    if ($("#showimg_" + file.id).attr("href")) { $("#showimg_" + file.id).click(); return true; }

    var id = MT.guid("imgView"),
        Pichtml = $('<a href="' + file.imgUrl + '" id="' + id + '" onclick="return hs.expand(this);" class="highslide"></a><div class="highslide-caption">' + file.name + file.ext + '</div>'), atag;
    $(top.document.body).append(Pichtml);
    atag = $("#" + id, top.document.body);
    atag.trigger("click");
};
/**
* dwg viewr
*/
cwf.checkDwgAx_ = function () {
    if (MT.ua.ie) {
        try {
            var comActiveX = new ActiveXObject("MXDRAWX.MxDrawXCtrl.1");
            return true;
        } catch (e) {
            return false;
        }
    } else {
        return false;
    }
};
cwf.dwg_ = function (file) {
    if (!file || !file.swfUrl) {
        alert("该文件不存在！");
        return false;
    }
    if (cwf.checkDwgAx_()) {
        var vw = YH.dom.getViewportWidth(top.window), vh = YH.dom.getViewportHeight(top.window),
            filepath = file.swfUrl;
        filepath = filepath.replace(/\.swf$/i, file.ext);
        box("/DWG/ViewDwgFile?filePath=" + escape(filepath) + "&_r=" + new Date().getTime(), { boxid: 'dwgview', pos: 't-t', contentType: 'iframe', title: file.name, cls: 'shadow-container', width: vw - 200, height: vh - 40, no_submit: true, link: true, cancel_BtnName: '关闭',
            onLoad: function (o) {
                var ifrm = o.fbox.find('.dlgIframe');
                ifrm.css({ width: vw - 204 + 'px', height: vh - 80 + 'px' });
                ifrm = ifrm[0];
                var dwgctl = ifrm.contentWindow.document.getElementById('dwgClient');
                dwgctl.width = vw - 234;
                dwgctl.height = vh - 130;
            }
        });
    } else {
        cwf.needToDownLoad_(file, "你没安装dwg浏览器插件或者该浏览器不支持dwg在线查看，请下载！", "dwgAxdl");
    }
};

// flash imgViewer
cwf.ImgVMgr = {
    data: [],
    curPos: 0,
    vw: 0, vh: 0,
    init: function () {
        var fct = $('#g_imgViewerCt_b');
        if (fct[0]) return;
        $('<div id="g_imgViewerCt_b" style="position:absolute;left:0;top:0;display:none;z-index:22222;font-size:0;"><div id="g_imgViewerCt_flashct"></div></div>').appendTo(document.body);
    },
    show: function (o) {
        if (!o) {
            alert('数据格式有误！');
            return;
        }
        this.data = o.data || [];
        this.curPos = o.index || 0;
        if (this.data.length) {
            if (this.data.length < this.curPos + 1) this.curPos = this.data.length - 1;
            this._render();
        }
    },
    _render: function () {
        this.init();
        document.documentElement.style.overflow = 'hidden';
        var vw = YH.dom.getViewportWidth(), vh = YH.dom.getViewportHeight();
        this.vw = vw;
        this.vh = vh;
        swfobject.embedSWF("/webMT/badge/imgViewer.swf", "g_imgViewerCt_flashct", vw, vh, "9.0.0", "expressInstall.swf", false, { /*wmode: 'transparent', */allowfullscreen: true, quality: 'high' }, null);
        $('#g_imgViewerCt_b').css({ width: 2, height: 2, top: -20 }).show();
    },
    flashLoaded: function () {
        $('#g_imgViewerCt_b').css({ width: this.vw, height: this.vh, top: $(document).scrollTop() });
    },
    getViewPort: function () {
        return { w: YH.dom.getViewportWidth(), h: YH.dom.getViewportHeight() };
    },
    getImgsByPos: function (idx) { // get image by index
        idx = idx || 0;
        return [this.data[idx]];
    },
    getCurrentPos: function () {
        return this.curPos;
    },
    setCurPos: function (pos) {
        this.curPos = pos;
    },
    getTotalNum: function () {
        return this.data.length;
    },
    unload: function () {
        $('#g_imgViewerCt_b').html('<div id="g_imgViewerCt_flashct"></div>').hide();
        document.documentElement.style.overflow = 'auto';
    },
    bind: function (imgCt, selector) {
        imgCt.undelegate(selector, 'click').delegate(selector, 'click', function () {
            var s = selector.replace(/\.|#/g, ''), o = this,
                data = imgCt.data('gimgList_' + s) || null, rs = [], index = -1;
            if (!data) {
                var tmp;
                imgCt.find(selector).each(function (i) {
                    tmp = { thumbUrl: $(this).attr('data-thumb') || this.src, imgUrl: $(this).attr('data-img') || this.src, title: this.title || this.alt, pos: i };
                    if (o === this) {
                        index = i;
                    }
                    rs.push(tmp);
                });
                data = { data: rs, index: index };
                imgCt.data('gimgList_' + s, data);
            }
            data = data || {};
            if (index == -1) {
                imgCt.find(selector).each(function (i) {
                    if (o === this) {
                        data.index = i;
                        return false;
                    }
                });
            }
            if (data.index == -1) data.index = 0;
            cwf.ImgVMgr.show(data);
        });
    },
    reset: function (imgCt, selector) {
        $(imgCt).removeData('gimgList_' + selector.replace(/\.|#/g, ''));
    }
};