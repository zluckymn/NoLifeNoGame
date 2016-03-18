var cwf = cwf || {};
cwf.isClientStarted = false; // 是否启动了客户端
cwf.domain = ""; // 传给flash
cwf.webc = null; // 主控flash swfObject id
cwf.webcloaded = false;
cwf.webopt = null; // 操作flash swfObject id
cwf.cbQue = {};
cwf.isReady = false;
cwf.initCb_ = {fn:null, args:null};
cwf.tasks = {
    getData: function(uuid) {
        if (uuid == "") return [];
        var task = this[uuid], d = [], data;
        if (MT.isUndefined(task)) return d;
        data = task.data;
        if (MT.isPlainObject(data)) {
            d = (data.data + "").replace(/\.:/g, "\\");
            d = MT.JSON.parse(d) || [];
        }
        return d;
    },
    delItem: function(uuid, fid) {
        if (uuid == "") return;
        var task = this[uuid] || {}, dt, i = 0, len;
        dt = this.getData(uuid);
        len = dt.length;
        for (; i < len; ++i) {
            if (dt[i].mx_internal_uid == fid) {
                dt.splice(i, 1);
                break;
            }
        }
        task.data = task.data || {};
        task.data.data = MT.JSON.stringify(dt);
    },
    delAll: function(uuid) {
        if (uuid == "") return;
        var task = this[uuid] || {};
        task.data = task.data || {};
        task.data.data = "[]";
    },
    send: function(uuid, files, cb, args) {
        files = files || [];
        var len = files.length,
            i = 0, nd, f;
        if (len > 0) {
            nd = [];
            for (; i < len; ++i) {
                f = files[i];
                if (f.path == "") continue;
                var file = {};
                file.nativePath = f.path.replace(/\\/g, ".:");
                file.args = f.strParam + "";
                nd.push(file);
            }
            var strFileList = MT.JSON.stringify(nd),
                o = cwf.tasks[uuid] || {},
                dt = o.data || {};
            dt.data = strFileList;
            if (args) dt.args = args;
            cwf.flashToair(uuid, dt);
        }
        if (cb && MT.isFunction(cb)) cwf.cbQue[uuid] = cb;
    }
};
cwf.needdoac = true;
cwf.setStatus = function(stat) { // 客户端连接状态，供flash call
	this.isClientStarted = stat;
    cwf.view.statText.show(stat);
    //console.log(stat);
    if (stat) {
        if (cwf.needdoac) {
            //cwf.needdoac = false;
            cwf.doAc();
        }
    } else {
        cwf.needdoac = true;
        //this.webc = null;
    }
    cwf.Help();
};
cwf.getReady = function() { // 判断页面是否ready
    return cwf.isReady;
};
cwf.setWebcInit = function(webGuid) {
    this.webGuid = webGuid;
    this.webc = this.getSWF_("webCore");
    if (this.isClientStarted) cwf.doAc_();
};
cwf.doAc = function() {
    if (cwf.initCb_.fn != null) {
        cwf.initCb_.fn.apply(cwf, cwf.initCb_.args || []);
        cwf.initCb_.fn = null;
        cwf.initCb_.args = null;
    }
};
cwf.setWeboptInit = function() {
    cwf.webopt = this.getSWF_("MTSpear");
    cwf.movieChange();
};
cwf.getSWF_ = function (id) {
    //var isIE = /msie/.test(navigator.userAgent.toLowerCase());
    var isIE = navigator.userAgent.toLowerCase().search(/(msie\s|trident.*rv:)([\w.]+)/) != -1;
    if (isIE) {
        return window[id + "_ob"];
    } else {
        return document[id + "_em"] || window[id + "_em"];
    }
};
cwf.bForceUpdate = false;
cwf.checkAppVerUp = function(hasInstalled, inv) {
    var v = cwf.forceUpadate(inv);
    if (hasInstalled && v == -1) {
        cwf.bForceUpdate = true;
    }
};
cwf.checkAppInstalled = function(hasInstalled, inv) {
    var li = $("#_installtips"),
        v;

    v = cwf.forceUpadate(inv);

    if (inv && inv != '' && v == -1) { // 小于 1.6.9的版本，强制提示升级
        cwf.bForceUpdate = true;
        cwf.showForce();
    } else {
        cwf.bForceUpdate = false;
        if (hasInstalled) {
            $("#mtFlash").show();
            $("#_forceUpdate").hide();
            li.hide();
        } else {
            li.show();
        }
    }
};
cwf.forceUpadate = function(version) {
    return MT.string.compareVersions(version, '1.6.9');
};
cwf.showForce = function() {
    var li = $("#_installtips"),
        fup = $("#_forceUpdate"),
        flash = $("#mtFlash");

    if (cwf.view.dlgHelp.node) {
        cwf.view.dlgHelp.show();

        li.hide();
        fup.show();
        flash.hide();
    }
};
cwf.checkWbc = function() { // 检测swfobject是否已生成
    if (MT.isObject(this.webc)) return true;
    return false;
};
cwf.checkWbopt = function() {
    if (MT.isObject(this.webopt)) return true;
    return false;
};
cwf.airResponse = function(uuid, args) {
    if (MT.isUndefined(uuid) || uuid == "") return false;
    if (args == "|--DOWNLOAD--|") { // down load response
        if ($.tmsg) $.tmsg("udtip", "已成功添加到“MTT飞象传动平台”下载队列！", {infotype:1,offsets:[0,-80]});
        return true;
    }
    var cb = cwf.cbQue[uuid];
    if (MT.isUndefined(cb) || !MT.isFunction(cb)) return false;
    if ($.tmsg) $.tmsg("udtip", "已成功添加到“MTT飞象传动平台”上传队列！", {infotype:1,offsets:[0,-80]});
    cb();
    delete cwf.cbQue[uuid];
};
cwf.airToflash = function(v) {
    if (!v) return;
    var uuid = v.taskId, task;
    if (!uuid || uuid == "") return;
    task = this.tasks[uuid];
    if (!MT.isUndefined(task)) {
        var tdata = task.data;
        if (MT.isUndefined(tdata)) {
            task["data"] = v;
        } else {
            var dt = tdata.data, addt, d;
            addt = v.data || "[]";
            //addt = addt.replace(/\\\\/g, "\\");
            if (addt.length > 2) {
                dt = dt.replace(/\]$/g, ",") + addt.replace(/^\[/g, "");
                tdata.data = dt;
            }
        }
        if (!v.isSplit) {
            var fn = task["callback"];
            if (MT.isFunction(fn)) fn.call(this, uuid);
        }
    }
};
cwf.flashToair = function(uuid, data) {
    data = this.checkWebGuid(data);
    if (!data.taskId) data.taskId = uuid;
    //data.data = data.data.replace(/\\/g, "\\\\");
	if (cwf.checkWbc()) cwf.webc.jsCallflashToAir(data);
    if (MT.isString(uuid) && uuid != "") {
        delete cwf.tasks[uuid];
    } else if (MT.isArray(uuid) && uuid.length) {
        MT.each(uuid, function(val) {
            delete cwf.tasks[uuid];
        });
    }
};
cwf.checkWebGuid = function(data) {
    data = data || {};
    var me = this;
    if (MT.isUndefined(data.webGuid)) {
        data.webGuid = me.webGuid;
        data.wDomain = me.getDomain();
        data.webPort_ = me.getPort();
        data.hostServ = cwf.getHostServ();
        data.type = "MULTIUPLOAD";
    }
    return data;
};
cwf.getDomain = function() {
    return window.location.hostname;
};
cwf.getPort = function() {
    return window.location.port;
};
cwf.getHost = function() { // 获取域名端口
	var host = window.location.hostname,
		port = window.location.port;
	this.domain = host;
	if (port != "") {
       this.domain += ":" + port;
	}
};
cwf.airFlash_ = function() {
    var corepne = $("#g_airFlash");
    if (corepne[0]) return;
    corepne = $('<div id="g_airFlash">air</div>');
    $(document.body).append(corepne);
    corepne.css({width:"5px",height:"5px"});
    cwf.embedSWF("#g_airFlash", "airFlash", "/webMT/badge/air/air.swf?r=" + new Date().getTime(), 5, 5);
};
cwf.coreFlash = function() {
    var corepne = $("#coreFlash");
    if (corepne[0]) return;
    corepne = $('<div id="coreFlash">dadf</div>');
    $(document.body).append(corepne);
    corepne.css({fontSize:0,width:"5px",height:"5px",position:"absolute",top:0,right:0});
    cwf.embedSWF("#coreFlash", "webCore", "/webMT/badge/mtWeb.swf?r=" + new Date().getTime(), 5, 5);
};
cwf.initFlash = function() { // 嵌入flash
    var appUrl = "http://" + this.domain + "/webMT/app/MTShield.air",
        flashvars = "appname=MTShield&appurl=" + appUrl + "&airversion=1.5";
    cwf.embedSWF("#mtFlash", "MTSpear", "/webMT/badge/mtOpt.swf?r=" + new Date().getTime(), 285, 160, flashvars);
};
cwf.embedSWF = function(el, id, url, w, h, flashvars) {
    var c = $(el);
    if (!MT.ua.flash.HAS_FLASH) {
        c.html('<a href="http://www.adobe.com/go/getflashplayer"><img src="/webMT/badge/get_flash_player.gif" alt="Get Adobe Flash player" /></a>');
    } else if (!MT.ua.flash.isVersion('9')) {
        c.html(compile_(id, "/webMT/badge/playerProductInstall.swf", w, h, flashvars));
    } else {
        c.html(compile_(id, url, w, h, flashvars));
    }

    function compile_(id, url, w, h, vars) {
        var s = '<object classid="clsid:D27CDB6E-AE6D-11cf-96B8-444553540000" codebase="http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=9,0,0,0" width="'+w+'" height="'+h+'" name="'+id+'_ob" id="'+id+'_ob" data="'+url+'"><param name="quality" value="high" /><param name="AllowScriptAccess" value="always" /><param name="wmode" value="transparent" />'+(vars?'<param name="flashVars" value="'+vars+'" />':'')+'<param name="SRC" value="'+url+'" /><embed src="'+url+'" width="'+w+'" height="'+h+'" quality="high" pluginspage="http://www.adobe.com/shockwave/download/download.cgi?P1_Prod_Version=ShockwaveFlash" type="application/x-shockwave-flash" id="'+id+'_em" name="'+id+'_em"></embed> </object>';
        return s;
    }
};
cwf.movieChange = function() {
    $("#_gotoInstallview").unbind("click").bind("click", function() {
        if (cwf.checkWbopt()) {
            if (MT.isFunction(cwf.webopt.jsCallshowInstall)) cwf.webopt.jsCallshowInstall();
            cwf.checkAppInstalled(false);
        }
    });
    $("#_gotoStartview").unbind("click").bind("click", function() {
        if (cwf.checkWbopt()) {
            if (MT.isFunction(cwf.webopt.jsCallshowStart)) cwf.webopt.jsCallshowStart();
            cwf.checkAppInstalled(true);
        }
    });
};
cwf.Help = function() {
    cwf.untip();
    var stat = this.isClientStarted;
    if (stat) { // connect,hide help
        cwf.view.dlgHelp.hide();
    } else { // show help
        cwf.view.dlgHelp.show();
    }
};
cwf.launchApp_ = function() {
    /*var pne = cwf.view.dlgHelp.node;
    if (pne[0]) {
        var ct = pne.find(".midd");
        ct.mask("启动中，请稍候...");
    }*/
};
// download
cwf.dl = function(files) {
    if (cwf.bForceUpdate) {
        cwf.showForce();
        return false;
    }
    cwf.initCb_ = {fn:cwf.dl, args:arguments};
    if (!this.isClientStarted) {
        cwf.Help();
        return;
    }
    if (!cwf.checkWbc()) {
        if ($.tmsg) $.tmsg("udtip", "初始化中，请稍后！", {infotype:3});
        //cwf.initCb_ = {fn:cwf.dl, args:arguments};
        return false;
    }
    if (cwf.checkWbc() && files.length) {
        if ($.tmsg) $.tmsg("udtip", "下载窗口启动中，请稍候...", {infotype:3,notime:true});
        if (MT.isFunction(cwf.webc.jsCalldoDownLoad)) {
            files = files || [];
            var uuid = Math.uuid(8),
                _hostServ = cwf.getHostServ(),
                strFileList = MT.JSON.stringify(files);
            cwf.webc.jsCalldoDownLoad(uuid, strFileList, _hostServ);
        }
    }
};
cwf.untip = function() {
    if (YH.$id('udtip')) YH.$id('udtip').style.display = 'none';
};
cwf.getHostServ = function() {
    if (typeof New_Master_Server_Address != 'undefined') {
        return New_Master_Server_Address;
    }
    return '';
};
// upload
cwf.up = function(args, callback, tag, isSingle) {
    if (cwf.bForceUpdate) {
        cwf.showForce();
        return false;
    }
    cwf.initCb_ = {fn:cwf.up, args:arguments};
    if (!this.isClientStarted) {
        cwf.Help();
        return;
    }
    if (!cwf.checkWbc()) {
        if ($.tmsg) $.tmsg("udtip", "初始化中，请稍后！", {infotype:3});
        //cwf.initCb_ = {fn:cwf.up, args:arguments};
        return false;
    }
    var uuid = "", tmpuuid, flag = false;
    if (tag) {
        flag = true;
        tmpuuid = $(tag).attr("_uuid");
        uuid = (tmpuuid && tmpuuid.length == 8) ? tmpuuid : "";
    }
    if (uuid == "") {
         uuid = Math.uuid(8);
         if (flag) $(tag).attr("_uuid", uuid);
    }
    if ($.tmsg) $.tmsg("udtip", "上传窗口启动中，请稍候...", {infotype:3,notime:true});
    var _hostServ = cwf.getHostServ();
    if (typeof isSingle != "undefined" && isSingle) {
        if (MT.isFunction(cwf.webc.jsCallOneUpload)) cwf.webc.jsCallOneUpload(uuid, args, _hostServ);
    } else {
        if (MT.isFunction(cwf.webc.jsCallMultiUpload)) cwf.webc.jsCallMultiUpload(uuid, args, _hostServ);
    }
    cwf.tasks[uuid] = cwf.tasks[uuid] || {};
    cwf.tasks[uuid]["callback"] = callback;
};
cwf.upone = function(args, callback, tag) { // upload one
    cwf.up(args, callback, tag, true);
};
// del uping
cwf.delUp = function(args) {
    cwf.initCb_ = {fn:cwf.delUp, args:arguments};
    if (!this.isClientStarted) {
        cwf.Help();
        return;
    }
    if (!cwf.checkWbc()) {
        if ($.tmsg) $.tmsg("udtip", "初始化中，请稍后！", {infotype:3});
        //cwf.initCb_ = {fn:cwf.delUp, args:arguments};
        return false;
    }
    var uuid = Math.uuid(8),
        _hostServ = cwf.getHostServ();
    if (MT.isFunction(cwf.webc.jsCalldoDelUping)) cwf.webc.jsCalldoDelUping(uuid, args, _hostServ);
};
// cwf view
cwf.view = {
    dlgHelp: {
        id: ".Client",
        init: function() {
            if ($('div.Client')[0]) return;
            var pne = cwf.tpl.help.join(""), self = this;
            $(document.body).append(pne);
            this.node = $(this.id);
            this.node.find("a.close").unbind("click").bind("click", function() {
                self.hide();
            });
            cwf.initFlash();
        },
        show: function() {
            var pos = this.node.getAlignToXY(document, "c-c"),
                isShow = this.node.height() != 0, oy;
            oy = isShow ? 40 : 228;
            this.node.css({left:pos.x, top:pos.y-oy, "visibility":"visible"}).show();
        },
        hide: function() {
            var nd = this.node, ct = nd.find(".midd");
            if (ct.isMasked()) ct.unmask();
            nd.css({"visibility":"hidden"});
        }
    },
    statText: {
        show: function(stat) {
            var txt = stat ? "已连接" : "已断开";
            if ($("#linkstat")[0]) $("#linkstat").html(txt);
        }
    }
};
cwf.tpl = {
    help: ['<div class="Client small">',
        '<div class="top">',
            '<div class="l"></div>',
            '<div class="m">',
                '<div class="font">MTT飞象传动平台</div>',
                '<div class="btn"><a href="javascript:;" class="close"></a></div>',
            '</div>',
            '<div class="r"></div>',
        '</div>',
        '<div class="midd">',
            '<div class="m">',
                '<div id="mtFlash">',
                    '<h1>请安装flash</h1>',
                    '<p><a href="http://www.adobe.com/go/getflashplayer"><img src="/webMT/badge/get_flash_player.gif" alt="Get Adobe Flash player" /></a></p>',
                '</div>',
                '<div class="webfont">',
                  '<ul>',
                     '<li class="last" id="_installtips">',
                '还没安装？<br />',
                '<p>点击下载：<a href="/webMT/app/install/MTShield.exe"><span style="color:#ff6633">MTT飞象传动平台</span></a>（windows版）</p>',
                     '</li>',
                     '<li class="last" id="_forceUpdate" style="display:none;">',
                '<span style="color:#ff0000;font-size:14px;font-weight:bold">检测到版本更新(ver 1.6.9)，请更新</span><br />',
                '<p><a href="/webMT/app/install/MTShield.exe"><span style="color:#ff6633;font-size:20px">请点我更新</span></a>（windows版）</p>',
				'<p>需要帮助？<a target="_blank" href="/webMT/UpdateReadme/金地控件更新计划.htm"><span style="color:#ff6633;font-size:20px">请点我看更新步骤</span></a></p>',
		        '<p>更新完成?请<a href="javascript:window.location.reload();"><span style="color:#ff6633">点我刷新</span></a></p>',
				'<p>需要帮助请联系工作人员：</p>',
				'<p>客服01：05923385501</p>',
				'<p>客服02：05923385502</p>',
					'</li>',
                  '</ul>',
                '</div>',
            '</div>',
        '</div>',
         '<div class="bott">',
             '<div class="l"></div>',
             '<div class="m"></div>',
             '<div class="r"></div>',
         '</div>',
    '</div>']
};

$(function() {
    cwf.isReady = true;
	cwf.getHost();
    cwf.airFlash_();
    cwf.coreFlash();
    //cwf.view.dlgHelp.init();
});
