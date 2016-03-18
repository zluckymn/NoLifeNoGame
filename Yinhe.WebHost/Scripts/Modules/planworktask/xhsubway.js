(function (c) { var a = ["DOMMouseScroll", "mousewheel"]; c.event.special.mousewheel = { setup: function () { if (this.addEventListener) { for (var d = a.length; d; ) { this.addEventListener(a[--d], b, false) } } else { this.onmousewheel = b } }, teardown: function () { if (this.removeEventListener) { for (var d = a.length; d; ) { this.removeEventListener(a[--d], b, false) } } else { this.onmousewheel = null } } }; c.fn.extend({ mousewheel: function (d) { return d ? this.bind("mousewheel", d) : this.trigger("mousewheel") }, unmousewheel: function (d) { return this.unbind("mousewheel", d) } }); function b(i) { var g = i || window.event, f = [].slice.call(arguments, 1), j = 0, h = true, e = 0, d = 0; i = c.event.fix(g); i.type = "mousewheel"; if (i.wheelDelta) { j = i.wheelDelta / 120 } if (i.detail) { j = -i.detail / 3 } d = j; if (g.axis !== undefined && g.axis === g.HORIZONTAL_AXIS) { d = 0; e = -1 * j } if (g.wheelDeltaY !== undefined) { d = g.wheelDeltaY / 120 } if (g.wheelDeltaX !== undefined) { e = -1 * g.wheelDeltaX / 120 } f.unshift(i, j, e, d); return c.event.handle.apply(this, f) } })(jQuery);

var subWayMap = (function () {
    var _loadingPne, opt;

    function __setSubWay() {
        var vw = YH.dom.getViewportWidth(), vh = YH.dom.getViewportHeight()
        var tw = opt.addWidth ? opt.addWidth : 0;
        var th = opt.addHeight ? opt.addHeight : 0;
        tpl = '<div style="margin:0;padding:0;width:' + (vw - 100 + tw) + 'px;height:' + (vh - 175 + th) + 'px;overflow:hidden;position:relative;"><div class="popsubwaycontainer" style="margin:0;padding:0;cursor:pointer;position:absolute;"></div><input type="hidden" class="swid" value="-1" /><input type="hidden" class="pointId" value="-1" /><input type="hidden" value="" class="swname" /></div>', title = opt.type == 'set' ? "选择对应地铁节点" : "查看地铁图", modal = opt.modal ? true : false, nobutton = opt.nobutton ? false : true, selid = opt.swid;
        var color = { "1": "#b5b5b5", "2": "#ffcc33", "3": "#66cc40", "4": "#f6652b" }, swid, swname, doc = YH.isStrict ? document.documentElement : document.body;

        doc.style.overflow = 'hidden';
        box(tpl, { boxid: 'subway', title: title, width: vw - 100 + tw, height: vh - 100 + th, modal: modal, no_submit: nobutton, submit_BtnName: "确定选择关联的节点", cancel_BtnName: "关闭",
            onOpen: function (o) {
                var subwaycontainer = o.db.find(".popsubwaycontainer"), _smtag = subwaycontainer[0],
                    divo = document.getElementById('wrapplan' + opt.planId), mapId = opt.mapId || $(divo).attr('mapId');
                new MT.RenderGraph({
                    container: _smtag.parentNode,
                    paperCt: _smtag,
                    url: '/home/GetEditDt/' + mapId,
                    callback: function () {
                        var me = this, ct = $(me.paperct.tag),
                         planid = $(ct).attr("data-pid");
                        $.ajax({
                            url: '/home/DecisionPointList', data: { secdPlanId: opt.planId }, cache: false,
                            success: function (rs) {
                                if (rs == "") {
                                    $("#m_subwayt").hide();
                                    $.tmsg("m_subway", "该计划未选择任何地铁图！");
                                } else {
                                    var dcache = me.dcache, cvw = dcache.width, cvh = dcache.height, paper = me.paper, nodes = me.nodes, swid, swname, sc = 1,
                                    st = paper.set(), check = "M2.379,14.729 5.208,11.899 12.958,19.648 25.877,6.733 28.707,9.561 12.958,25.308z", _ck, objPointId;
                                    swid = o.db.find(".swid"), swname = o.db.find(".swname");
                                    objPointId = o.db.find(".pointId");

                                    ct.css({ width: cvw + 'px', height: cvh + 'px' });
                                    ct.draggable();

                                    if (opt.type == 'set' || opt.type == 'thum') {
                                        o.addBtn('cancelSet', '', '取消关联    ', function (fx) {
                                            MT.off(o.db.find(".popsubwaycontainer").parent().get(0));
                                            if (opt.callback) {
                                                opt.callback.call(this, '', '', '');
                                            }
                                            o.destroy();
                                        });
                                        _ck = paper.path(check).attr({ fill: "#009933", stroke: "none" }).translate(0, 0).hide();
                                    }

                                    var tx = 0, ty = 0, bb, bbck, _isSel = false;
                                    $.each(rs.pointList, function (i, n) {
                                        var pid = String(n.textPId), nd = nodes[pid];
                                        if (nd) {
                                            nd.attr({ title: n.name }).data('swid', pid).data('pointId', n.pointId);
                                            if (color[n.status]) {
                                                nd.attr({ fill: color[n.status] });
                                            }
                                            bb = nd.getBBox();
                                            if (selid && selid == pid) {
                                                _ck.translate(bb.x, bb.y).toFront().show();
                                                tx = bb.x; ty = bb.y;
                                                _isSel = true;
                                            }

                                            (function (tag) {
                                                tag.click(function () {
                                                    swid.val(this.data("swid")); swname.val(this.attr("title"));
                                                    objPointId.val(this.data('pointId'));
                                                    var _bb = this.getBBox();
                                                    if (opt.type == 'set') {
                                                        var _bbck = _ck.getBBox();
                                                        _ck.translate(_bb.x - tx, _bb.y - ty).toFront().show();
                                                        tx = _bb.x; ty = _bb.y;
                                                    }
                                                }).mouseover(function () {
                                                    this.attr("stroke-width", 2);
                                                    getRelTasks(opt.planId, this.data("swid"), this.node, this.data('pointId'));
                                                }).mouseout(function () {
                                                    this.attr("stroke-width", 1);
                                                });
                                            })(nd);
                                        }
                                    });

                                    o.fbox.unmask();
                                }
                            }
                        });
                    }
                });
            },
            submit_cb: function (o) {
                if (opt.callback) {
                    opt.callback.call(this, o.db.find(".swid").val(), o.db.find(".swname").val(), o.db.find(".pointId").val());
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
    return {
        viewPoint: function (options) {
            opt = options || {};
            __setSubWay();
        }
    }
})();

function getRelTasks(planId, sid, obj, pointId) {
    var tpane = $("#tasktip"), ofs = $(obj).offset(), _l = ofs.left, _t = ofs.top, nl;
    if (!tpane[0]) {
        tpane = $("<div style=\"background-color:#ffffce;display:none;position:absolute;margin:0;padding:0;z-index:9999;border:1px solid #ece0b0; color:#666666; padding:5px\" id=\"tasktip\"></div>").appendTo(document.body);
        tpane.mouseenter(function () { }).mouseleave(function () { tpane.hide(); });
    }
    $.ajax({
        type: 'POST',
        url: '/Home/DecisionNewPointTask/' + planId + '?textPid=' + sid, cache: false,
        success: function (rs) {
            var i = 0, cc = ['<table cellpadding="0" cellspacing="0">'], has = false, _;
            for (; i < rs.length; ++i) {
                _ = rs[i];
                cc.push('<tr><td height=25 class="fontblue"></td><td class="fontblue" width="250"><a target="_blank" href="/DesignManage/ProjTaskInfo/' + _.id + '">' + _.name + '</a></td><td width="100">' + _.otherParam + '</td><td class="fontorange" width="40">' + _.value + '</td></tr>'); has = true;
            }
            cc.push("</table>");
            if (has) {
                tpane.html(cc.join(""));
                nl = _l + $(obj).width();
                tpane.css({ left: nl, top: _t - tpane.height() - 10 });
                if (nl + tpane.width() > YH.dom.getViewportWidth()) tpane.css("left", YH.dom.getViewportWidth() - tpane.width());
                tpane.show();
            } else {
                tpane.hide();
            }
        }
    });
}