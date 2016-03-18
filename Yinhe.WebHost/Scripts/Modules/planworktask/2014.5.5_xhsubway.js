/**
 * subway.js 2012/6/21 by Qingbao.Zhao
 */
var __globalDomain = typeof globalHostDomain_ != 'undefined' ? globalHostDomain_ : '';

var createShapes = (function () {
    function __createPath(vml, path, x, y, w, h, r) {
        var res = vml.path(path), a = res.attrs;
        res.X = a.x = x;
        res.Y = a.y = y;
        res.W = a.width = w;
        res.H = a.height = h;
        if (r) {
            res.R = a.r = r;
        }
        res.path = a.path = path;
        res.type = "path";
        return res;
    }
    function __theRhombus(vml, x, y, w, h) { // 棱形
        var path = ["M", x, y, "l", Math.floor(w / 2), Math.floor(h / 2), Math.floor(-w / 2), Math.floor(h / 2), Math.floor(-w / 2), Math.floor(-h / 2), "z"];
        return __createPath(vml, path, x, y, w, h);
    }
    return {
        Rhombus: function (vml, x, y, w, h) {
            return __theRhombus(vml, x || 0, y || 0, w || 0, h || 0);
        }
    };
})();
Raphael.fn.Rhombus = function (x, y, w, h) { // 棱形
    return createShapes.Rhombus(this, x, y, w, h);
};
Raphael.fn.dataSet = function (id, key, value) {
    this.eldata = this.eldata || {};
    var eldata = this.eldata,
        data = eldata[id] = eldata[id] || {};
    if (arguments.length == 2) {
        if (typeof key == 'object') {
            for (var i in key)
             if (key.hasOwnProperty(i)) {
                data[i] = key[i];
            }
            return this;
        }
        return data[key];
    }
    data[key] = value;
    return this;
};

function g_chunk(array, len, process, context, obj) {
    var i = 0, j;
    setTimeout(function () {
        for (j = 0; j < 20 && i < len; ++j) {
            process(array[i], obj);
            i++;
        }
        if (i < len) {
            setTimeout(arguments.callee, 0);
        } else {
            if ($('#m_dosubway')[0]) $('#m_dosubway').hide();
        }
    }, 0);
    /*setTimeout(function () {
        if (array.length) {
            var item = array.shift();
            process.call(context, item, obj);
            if (array.length > 0) {
                setTimeout(arguments.callee, 10);
            }
        } else {
            if ($('#m_dosubway')[0]) $('#m_dosubway').hide();
        }
    }, 10);*/
}

var subWayMap = (function () {
    var url = "/Home/DecisionPointList", _loadingPne, opt,
        color = { "1": "#b5b5b5", "2": "#ffcc33", "3": "#66cc40", "4": "#f6652b" }, tx = 0, ty = 0;
    function __init() {
        $.tmsg("m_subwayt", "决策地铁图加载中...", { infotype: 3, notime: true });
        $.ajax({
            url: url, data: { secdPlanId: opt.planId }, cache: false,
            success: function (rs) {
                if (rs == "") {
                    $("#m_subwayt").hide();
                    $.tmsg("m_subway", "该计划未选择任何地铁图！");
                } else {
                    var data = rs;
                    __setSubWay(data);
                }
            }
        });
    }
    function __setSubWay(data) {
        var vw = YH.dom.getViewportWidth(), vh = YH.dom.getViewportHeight(), cvw = data.width, cvh = data.height,
            imgearr = __globalDomain + "/Content/images/common/earr.gif",
			tpl = '<div style="margin:0;padding:0;width:' + (vw - 100) + 'px;height:' + (vh - 175) + 'px;overflow:hidden;position:relative;"><div class="popsubwaycontainer" style="margin:0;padding:0;left:0;top:0;cursor:pointer;position:absolute;"></div><div class="g-eg" style="margin:0;padding:0;cursor:default;position:absolute;width:0;height:0;right:-5px;bottom:-5px;border:5px solid #999;background-color:#fff;z-index:1200;"></div><input type="hidden" class="swid" value="-1" /><input type="hidden" value="" class="swname" /><div class="earr" style="margin:0;padding:0;cursor:pointer;position:absolute;width:13px;height:13px;right:0px;bottom:0px;border:1px solid #adbfe4;background:url(' + imgearr + ') 0 -17px no-repeat;z-index:1300;" data-op="0"></div></div>', title = opt.type == 'set' ? "选择对应地铁节点" : "查看地铁图", modal = opt.modal ? true : false, nobutton = opt.nobutton ? false : true, selid = opt.swid;
        var swid, swname, doc = YH.isStrict ? document.documentElement : document.body;
        doc.style.overflow = 'hidden';
        box(tpl, { boxid: 'subway', title: title, width: vw - 100, height: vh - 100, modal: modal, no_submit: nobutton, submit_BtnName: "确定选择关联的节点", cancel_BtnName: "关闭",
            onOpen: function (o) {
                if ($("#m_subwayt")[0]) $("#m_subwayt").hide();
                o.fbox.mask("处理中，请稍候...");
                var subwaycontainer = o.db.find(".popsubwaycontainer"), check = "M2.379,14.729 5.208,11.899 12.958,19.648 25.877,6.733 28.707,9.561 12.958,25.308Z", egeye = o.db.find(".g-eg"), earr = o.db.find(".earr");
                swid = o.db.find(".swid"), swname = o.db.find(".swname");

                subwaycontainer.css({ width: cvw, height: cvh });
                var paper = new ScaleRaphael(subwaycontainer[0], cvw, cvh); //Raphael(subwaycontainer[0], cvw, cvh);
                var _r, _w, _h, _ck,
                    paperEge = Raphael(egeye[0], 300, 150), imgSrc = __globalDomain + "/Content/SubwayMap/" + data.imagePath, erect,
                    selX = 0, selY = 0, _isSel = false, _imgObj, dataLen, ri = 0, n;
                _imgObj = paper.image(imgSrc, 0, 0, cvw, cvh);
                paperEge.image(imgSrc, 0, 0, 300, 150);
                erect = paperEge.rect(0, 0, 75, 45).attr({ stroke: "#274b8b", "stroke-width": 2, "cursor": "move", fill: '#fff', opacity: 0.6 });
                /**/
                paper.text(70, 10, "节点状态颜色标识：").attr({ "font-size": "12px" });
                paper.circle(20, 25, 8).attr({ fill: color['1'], stroke: "none" });
                paper.text(50, 30, "未开始").attr({ "font-size": "12px" });
                paper.circle(20, 45, 8).attr({ fill: color['2'], stroke: "none" });
                paper.text(50, 50, "进行中").attr({ "font-size": "12px" });
                paper.circle(20, 65, 8).attr({ fill: color['3'], stroke: "none" });
                paper.text(50, 70, "已完成").attr({ "font-size": "12px" });
                paper.circle(20, 85, 8).attr({ fill: color['4'], stroke: "none" });
                paper.text(50, 90, "已延时").attr({ "font-size": "12px" });
                /**/
                if (opt.type == 'set' || opt.type == 'thum') {
                    o.addBtn('cancelSet', '', '取消关联', function (fx) {
                        MT.off(o.db.find(".popsubwaycontainer").parent().get(0));
                        if (opt.callback) {
                            opt.callback.call(this, '', '');
                        }
                        o.destroy();
                    });
                    _ck = paper.path(check).attr({ fill: "#fff000", stroke: "#009933", "stroke-width":1 }).translate(0, 0).scale(2).hide();
                }
                dataLen = data.pointList.length;
                $.tmsg("m_dosubway", "地铁图描绘中，请稍候...", { notime: true });
                g_chunk(data.pointList, dataLen, _renderPoints, this, { paper: paper, selid: selid, _isSel: _isSel, _ck: _ck, swid: swid, swname: swname });

                /* eagle eye*/
                var cpox = 39, cpoy = 24, cpx = 39, cpy = 24, bbox_x = (vw - 100) / 2, bbox_y = (vh - 175) / 2,
                    _x11, _y11, sc = bbox_x * 2 / cvw;
                var start = function () {
                    this.ox = this.attr("x"); this.oy = this.attr("y");
                }, move = function (dx, dy) {
                    var _x = this.ox + dx, _y = this.oy + dy;
                    if (_x <= 0) {
                        _x = 0;
                    } else if (_x >= 225) {
                        _x = 225;
                    }
                    if (_y <= 0) {
                        _y = 0;
                    } else if (_y >= 105) {
                        _y = 105;
                    }
                    this.attr({ x: _x, y: _y });
                    cpx = _x + cpox;
                    cpy = _y + cpoy;
                }, up = function () {
                    _x11 = -cpx / 300 * cvw * sc + bbox_x;
                    _y11 = -cpy / 150 * cvh * sc + bbox_y;
                    subwaycontainer.stop().animate({ left: _x11, top: _y11 }, { duration: "normal" });
                };
                erect.drag(move, start, up);
                earr.click(function () {
                    var dataop = $(this).attr("data-op");
                    if (dataop == "1") {
                        egeye.stop().animate({
                            width: 0, height: 0
                        }, { duration: "normal", complete: function () {
                            earr.attr("data-op", "0").css("background", "url(" + imgearr + ") 0 -17px no-repeat");
                        }
                        });
                    } else {
                        egeye.stop().animate({
                            width: 305, height: 155
                        }, { duration: "normal", complete: function () {
                            earr.attr("data-op", "1").css("background", "url(" + imgearr + ") 0 0 no-repeat");
                        }
                        });
                    }
                });

                paper.changeSize(cvw * sc, cvh * sc, false, false);
                var _subwp = subwaycontainer.parent().get(0);
                MT.off(_subwp);
                MT.on(_subwp, SIGNAL('mousewheel'), SLOT(function (ev) {
                    if (ev.deltaY > 0) {
                        if (sc > 0.3) sc -= 0.1;
                    } else {
                        if (sc < 1) sc += 0.1;
                    }
                    paper.changeSize(cvw * sc, cvh * sc, false, false);
                    return false;
                }));
                subwaycontainer.draggable();
                o.fbox.unmask();
            },
            submit_cb: function (o) {
                MT.off(o.db.find(".popsubwaycontainer").parent().get(0));
                if (opt.callback) {
                    opt.callback.call(this, swid.val(), swname.val());
                }
            },
            onDestroy: function (o) {
                if (opt.cancel) {
                    opt.cancel.call(this);
                }
                if ($("#tasktip")[0]) $("#tasktip").hide();
                doc.style.overflow = 'auto';
            }
        });
    }
    function _renderPoints(n, obj) {
        var c, _txt, _ts = 0, _txt_x, _txt_y, wcx, wcy, _x1 = 0, _y1 = 0, xcx = 0, ycy = 0,
            selid =obj.selid, _isSel = obj._isSel, _ck = obj._ck, swid = obj.swid, swname = obj.swname, paper = obj.paper, ckwh;

        _w = parseInt(n.width, 10); _h = parseInt(n.height, 10); _r = parseFloat(n.radius, 10) || 0;
        if (n.textSize) _ts = Math.floor(parseInt(n.textSize, 10) / 2) + 2;
        n.pointX = parseInt(n.pointX, 10);
        n.pointY = parseInt(n.pointY, 10);
        switch (n.type) {
            case 0: // circle
                c = paper.circle(n.pointX, n.pointY, _r);
                _txt_x = n.pointX; _txt_y = n.pointY + _ts - _r / 2; _x1 = _r; _y1 = _r;
                break;
            case 1: // rect
                c = paper.rect(n.pointX - _w / 2, n.pointY - _h / 2, _w, _h); _x1 = _w / 2; _y1 = _h / 2; break;
            case 2: // roundrect
                c = paper.rect(n.pointX - _w / 2, n.pointY - _h / 2, _w, _h, _r); _x1 = _w / 2; _y1 = _h / 2; break;
            case 3: // Rhombus
                c = paper.Rhombus(n.pointX - _w / 2, n.pointY - _h / 2, _w, _h); _x1 = _w / 2; _y1 = _h / 2; break;
        }
        if (n.type > 0) { _txt_x = n.pointX; _txt_y = n.pointY; }
        if (n.text) _txt = paper.text(_txt_x, _txt_y - 3, n.text).attr({ "font-size": n.textSize + "px" });
        c.attr({ stroke: n.strokec || "#000", fill: color[n.status] || n.color, "swid": String(n.pointId), "title": n.name });
        paper.dataSet(c.id, { "pointId": n.pointId, "planid": opt.planId });
        if (_ck) ckwh = _ck.getBBox();
        if (selid && selid == n.pointId) {var nowcked = c.getBBox(); tx = nowcked.x + nowcked.width/2 - (ckwh.width/3||0); ty = nowcked.y + nowcked.height/2 - (ckwh.height/2||0); _ck.translate(tx, ty).toFront().show(); selX = n.pointX; selY = n.pointY; _isSel = true; }

        c.click(function () {
            var _sw_id = paper.dataSet(this.id, "pointId");
            swid.val(_sw_id); swname.val(this.attr("title"));
            if (opt.type == 'set') {
                xcx = this.getBBox();
                ycy = xcx.y + xcx.height/2 - (ckwh.height/2||0);
                xcx = xcx.x + xcx.width/2 - (ckwh.width/3||0);
                _ck.translate(xcx - tx, ycy - ty).toFront().show();
                tx = xcx; ty = ycy;
            }
        }).mouseover(function () {
            this.attr("stroke-width", 2);
            var _pointId = paper.dataSet(this.id, "pointId"),
                        _planid = paper.dataSet(this.id, "planid");
            getRelTasks(_planid, _pointId, this.node);
        }).mouseout(function () {
            this.attr("stroke-width", 1);
        });
    }
    return {
        viewPoint: function (options) {
            opt = options || {};
            __init();
        }
    }
})();

function getRelTasks(planId, sid, obj){
	var tpane = $("#tasktip"), ofs = $(obj).offset(), _l = ofs.left, _t = ofs.top, nl;
	if (!tpane[0]) {
		tpane = $("<div style=\"background-color:#ffffce;display:none;position:absolute;margin:0;padding:0;z-index:9999;border:1px solid #ece0b0; color:#666666; padding:5px\" id=\"tasktip\"></div>").appendTo(document.body);
		tpane.mouseenter(function(){}).mouseleave(function(){tpane.hide();});
	}
	$.ajax({
		type:'POST',
		url: '/Home/DecisionPointTask/?id=' + planId + '&sid=' + sid, cache: false,
		success:function(rs){
			var i = 0, cc = ['<table cellpadding="0" cellspacing="0">'], has = false, _;
			for(; i < rs.length; ++i){
                _ = rs[i];
				cc.push('<tr><td height=25 class="fontblue"></td><td class="fontblue" width="250">'+_.name+'</td><td width="100">'+_.otherParam+'</td><td class="fontorange" width="40">'+_.value+'</td></tr>'); has = true;
			}
			cc.push("</table>");
			if(has){
				tpane.html(cc.join(""));
				nl = _l+$(obj).width();
				tpane.css({ left: nl, top: _t - tpane.height() - 10 });
				if(nl+tpane.width() > YH.dom.getViewportWidth()) tpane.css("left", YH.dom.getViewportWidth()-tpane.width());
				tpane.show();
			}else{
				tpane.hide();
			}
		}
	});
}