/* by Qingbao.Zhao 2011/11/14 */
//nodeSet[_nid] = {id:_nid, rp:_rp, label:box.label, pid:pdp[0]+'', dis:pdp[1]+'', isPoints:pdp[2]+'', points:box.points};
function searchNode() {
    /*var inpt = $("#schnodeval"), defv = inpt[0].defaultValue, v;

    inpt.focus(function() {
        v = $.trim($(this).val());
        if (v === defv) $(this).val('');
    }).blur(function() {
        v = $.trim($(this).val());
        if (v === '') $(this).val(defv);
    });

    $("#schnode").click(function() {
        v = $.trim(inpt.val());
        var nd, label;
        if (v == '' || v == defv) {
            return false;
        } else {
            for (var k in _gnodelist) {
                nd = _gnodelist[k];
                label = nd.label.replace(/&#xa;/g, '');
                if (label.indexOf(v) != -1) {
                    YH.dom.scrollintoContainer($(nd.rp.node), $('html,body'), true, true, {noL:true});
                    $(nd.rp.node).trigger('click');
                    break;
                }
            }
        }
    });*/
}
var __globalDomain = typeof globalHostDomain_ != 'undefined' ? globalHostDomain_ : '',
    _gnodelist, _gpoints, _glabels, _gpne, _gtasks = {}, topPoints = {},
    statusImgs = {2: 'smallIconBlue.png', 3: 'smallIconOrange.png', 4: 'smallIconGreen.png', 'jfw': 'jfwicon.png', 'hm': 'jd_restart.png', 'flagYellow': 'card_yellow.png', 'flagRed': 'card_red.png', 'flagGreen': 'flag_green.png'},
    imgUrl = __globalDomain + '/Content/images/DesignManage/', statname = { 2: '未开始', 3: '进行中', 4: '已完成' }, _gIsBig = false, isSelMod = false;
var DGUntil = {
	// 获取光标位置
	getPositionForInput: function(ctrl){ // 单行文本框
		var CaretPos = 0;
		if(document.selection){ // IE Support
			ctrl.focus();
			var Sel = document.selection.createRange();
			Sel.moveStart('character', -ctrl.value.length);
			CaretPos = Sel.text.length;
		}else if(ctrl.selectionStart || ctrl.selectionStart == '0'){// Firefox support
			CaretPos = ctrl.selectionStart;
		}
		return (CaretPos);
	},
	getSelectionText: function(){
		if(window.getSelection){
			return window.getSelection().toString();
		}else if(document.selection && document.selection.createRange){
			return document.selection.createRange().text;
		}
		return '';
	},
	dateformatter: function(date, ft) {
		var y = date.getFullYear();
		var m = date.getMonth() + 1;
		var d = date.getDate(), sp = ft ? ft : "-";
		return y + sp + String.leftPad(m, 2, '0') + sp + String.leftPad(d, 2, '0');
	},
	isDate: function(str){
		str = str.replace(/-/g, "/"); str = str.replace(/\/0/g, "/");
		var reg = /^(\d{1,4})(-|\/)(\d{1,2})\2(\d{1,2})$/, r = str.match(reg), d;
		if(r == null) return false;
		d = new Date(r[1], r[3]-1, r[4]);
		var newStr = d.getFullYear() + r[2] + (d.getMonth()+1) + r[2] + d.getDate();
		return (newStr == str);
	}
};
(function($) {
    $.fn.onumberbox = function(maxnum) {
        $(this).css({imeMode: "disabled", textAlign:"center"});
        this.bind("keypress", function(e){
            var kc = e.keyCode || e.which, inputstr = String.fromCharCode(kc), st = DGUntil.getSelectionText(), v = this.value, pos = DGUntil.getPositionForInput(this);
            if(st){
                pos = v.indexOf(st); v = v.replace(st, "");
            }
            v = v.substr(0, pos) + inputstr + v.substr(pos);
            if(!/^\d+$/g.test(v) || /^0/.test(v)) return false;
        });
        this.bind("paste dragenter", function(){
            return false;
        });
    };
})(jQuery);

$("table.blockTable").bind("click", function() {
    $("#wcalendar").hide();
});
function dopexe(atag, data, secdPlanId) { // 豁免申请 AIII-1894 By Qingbao.Zhao 2011/12/29
    //if (exempt) return false;
    var tpl = '<table cellpadding="3" cellspacing="5"><tr><td style="color:#ff0000;font-size:14px;font-weight:bold;">发起流程豁免操作，请明确说明发起原因。</td></tr><tr><td><textarea style="width:360px;height:150px;"></textarea></td></tr></table>';
    box(tpl, { boxid: 'exemp', title: '豁免申请', cls: 'shadow-container2',
        submit_cb: function(o) {
            var newtxt = $.trim(o.db.find("textarea").val());
            if (newtxt == '') {
                $.tmsg('m_exem', '发起豁免的原因必须填写！');
                return false;
            } else {
                __modifyDate([data], newtxt);
            }
        }
    });
    function __modifyDate(selTasks, newtxt) {
        var _tpl = '<form id="fexemp"><table cellpadding="0" class="gflist" id="seldlist" cellspacing="1"><thead class="thd"><th width="30"></th><th width="300">节点名称</th><th width="80">计划开始</th><th width="80">计划结束</th></thead><tbody></tbody></table></form>', i = 0, tid, str = '', dt;
        for (; dt = selTasks[i]; ++i) {
            tid = dt.taskId;
            str += '<tr tid="' + tid + '"><td>' + (i + 1) + '</td><td>' + dt['name'] + '<input type="hidden" name="exemptList[' + i + '].taskId" value="' + tid + '" /></td><td><input style="width:90px" type="text" name="exemptList[' + i + '].curStartData" value="' + dt['s'] + '" /></td><td><input type="text" name="exemptList[' + i + '].curEndData" style="width:90px" value="' + dt['e'] + '" /></td></tr>';
        }
        box(_tpl, { boxid: 'mdt', title: '为选中的节点重新设置计划时间', width: 600, submit_BtnName: '执行豁免申请',
            onOpen: function(o) {
                var sels = $('#seldlist'), zIndex = parseInt(o.fbox.css('zIndex'), 10) || 11050;
                sels.find('tbody').html(str);
                $('<input type="hidden" name="secdPlanId" value="' + secdPlanId + '" /><input type="hidden" name="remark" value="' + newtxt + '" />').insertAfter(sels);
                sels.find("input").each(function() {
                    $(this).css("ime-mode", "disabled");
                    var regexp = /^\d{1,4}([-\/](\d{1,2}([-\/](\d{1,2})?)?)?)?$/;
                    $(this).bind("keypress", function(e) {
                        var keyCode = e.keyCode || e.which;
                        if (keyCode == 32) return false;
                        var pos = DGUntil.getPositionForInput(this), inputstr = String.fromCharCode(keyCode),
                            v = $(this).val(), st = DGUntil.getSelectionText(), pos1 = v.length, _rmsel = '';
                        _rmsel = v.substr(0, pos).replace(st, '');
                        return regexp.test(_rmsel + inputstr + v.substr(pos));
                    }).bind("paste dragenter", function() {
                        return false;
                    }).bind("click", function() {
                        var ofs = $(this).offset(), l = ofs.left, t = ofs.top, h = $(this).outerHeight(), ipt = this;
                        var str = $(this).val(), _calendar1 = $("#wcalendar"), md = { el: this }, _calendar;
                        _calendar1.empty();
                        _calendar = $("<div style=\"margin:0;padding:0;width:180px;height:180px;line-height:20px;\"></div>").appendTo(_calendar1);
                        _calendar.calendar({ onSelect: function(d) {
                            $(ipt).val(DGUntil.dateformatter(d)).focus();
                            _calendar.hide();
                        }, weeks: ["日", "一", "二", "三", "四", "五", "六"], months: ["1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月"], fit: true
                        });
                        if (DGUntil.isDate(str)) _calendar.calendar('moveTo', new Date(str.replace("-", "/")));
                        _calendar.calendar("resize");
                        _calendar1.css({ left: l, top: t + h + 1, zIndex: zIndex + 50 }).show();
                    });
                });
            },
            submit_cb: function(o) {
                var _rows = $('#seldlist').find('tbody')[0].rows;
                if (_rows.length === 0) {
                    $.tmsg('m_exem', '未选择任何节点！操作取消！');
                } else {
                var s = $('#fexemp').serialize(), arr = {}, _tr, st = '', et = '';
                    if (confirm('确定执行豁免申请吗？\n此操作不可恢复！')) {
                        o.fbox.mask('处理中，请稍候...');
                        $.ajax({
                            type: 'POST', url: '/Projects/ProjArranged/SaveExemptTask', data: s,
                            complete: function() { o.fbox.unmask(); },
                            success: function(data) {
                                if (data.Success) {
                                    $.tmsg("m_exem", "豁免申请成功！", { infotype: 1 });
                                    if(atag) atag.style.display = 'none';
                                } else {
                                    $.tmsg("m_exem", data.ErrorMsg, { infotype: 2 });
                                }
                            }
                        });
                    }
                }
            },
            onDestroy: function() {
                cancelExemp();
            }
        });
    }
    function cancelExemp() {
        $("#wcalendar").hide();
    }
}

function showstatus(paper, bb, dt, stat) {
    var imgStat, hasAttaches = false, x;
    if (statusImgs[stat] && (stat == 3 || stat == 4)) {
        imgStat = paper.image(imgUrl + statusImgs[stat], bb.x+bb.width-10, bb.y+bb.height-10, 10, 12);
        imgStat.attr("cursor", 'pointer');
        (function(nd, dtc0) {
            $(imgStat.node).tipsy({gravity: 'w', html:'true', title: function() {return '<div class="f12">'+dtc0+'</div>';}});
        })(imgStat.node, statname[stat]);
    }
    // 设置交付物 豁免情况
    if (dt['fileCount'] > 0) {
        imgStat = paper.image(imgUrl + statusImgs['jfw'], bb.x+bb.width-22, bb.y+bb.height-10, 10, 10);
        imgStat.attr("cursor", 'pointer'); var dtc = String(dt['fileCount']);
        (function(nd, dtc) {
            $(nd).tipsy({gravity: 'n', html:'true', title: function() {return '<div class="f12"><p>已上传：<span style="color:#fcdb25">'+dtc+'</span></p></div>';}});
        })(imgStat.node, dtc);
        hasAttaches = true;
    }
    if (dt['ExemptDetail'] && dt['ExemptDetail'] != '') {
        x = hasAttaches ? (bb.x+bb.width-34) : (bb.x+bb.width-22);
        imgStat = paper.image(imgUrl + statusImgs['hm'], x, bb.y+bb.height-10, 10, 10);
        imgStat.attr("cursor", 'pointer'); var hmdt = dt['ExemptDetail'];
        (function(nd, hmdt) {
            $(imgStat.node).tipsy({gravity: 'n', html:'true', title: function() {return '<div class="f12"><p>豁免时间：<span style="color:#ccc">'+hmdt+'</span></p></div>';}});
        })(imgStat.node, hmdt);
    }
}
function TimeDelay(paper, bb, dt, ispoint) { // 时间延迟
    var st, et, nowt = new Date(servTime.replace(/-/g, '/')), url,
        flag, tip = '还有', m = -1,
        dmms = 86400000, diffsn, diffen, diffst;

    url = '';
    ispoint = ispoint || '0';
    if (!dt || dt.status == 4) return;
    st = dt.planStartDate + '';
    et = dt.planEndDate + '';
    if (!st && !et) return;
    var sto = new Date(st.replace(/-/g, '/')),
        eto = new Date(et.replace(/-/g, '/')), txtgq = '', _x, _y;

    diffst = st && et ? ((eto - sto) / dmms + 1) : '';

    _x = ispoint == '1' ? (bb.x + bb.width/2 - 8) : bb.x;

    // 节点任务计划开始日期当天或之前没有启动该任务的对该节点亮黄牌，启动任务后黄牌消失；
    if (dt.status <= 2 && st != '') {
        diffsn = (sto - nowt) / dmms;
        if (diffsn <= 0) {
            url = imgUrl + statusImgs['flagYellow'];
            paper.image(url, _x, bb.y, 16, 16);
        }
    } else if (dt.status == 3 && et != '') {
        diffen = (eto - nowt) / dmms;
        if (diffst >= 0 && diffst <= 3) { // 节点任务天数小于等于三天的，不需要亮黄牌

        } else if (diffst > 3) {
            // 对于节点任务天数大于三天的节点，其任务计划结束日期前三天没有结束的，对该节点亮黄牌至其结束任务为止（不管有无完成，节点任务计划结束日期第二天黄牌均消失）
            if (diffen <= 3 && diffen >= 0) {
                url = imgUrl + statusImgs['flagYellow'];
                paper.image(url, _x, bb.y, 16, 16);
                //flag = paper.image(url, bb.x-16, bb.y+bb.height/2-32, 32, 32);
            }
        }
        if (diffen < 0) {
            url = imgUrl + statusImgs['flagRed'];
            paper.image(url, _x, bb.y, 16, 16);
        }
    }
}

function renderTitle_(paper) {
    var nodes = [
            {x:0, y:0, w:104, h:42, txt:"拓展", tx:52, ty:20, s:"gradient=135-#b7c9e3-#dee6f3"},
            {x:104, y:0, w:104, h:42, txt:"营销", tx:140, ty:20, s:"gradient=135-#b7c9e3-#dee6f3"},
            {x:208, y:0, w:444, h:42, txt:"设计", tx:240, ty:20, s:"gradient=135-#b7c9e3-#dee6f3"},
            {x:652, y:0, w:93, h:42, txt:"成本", tx:700, ty:20, s:"gradient=135-#b7c9e3-#dee6f3"},
            {x:745, y:0, w:91, h:42, txt:"运营", tx:790, ty:20, s:"gradient=135-#b7c9e3-#dee6f3"},
            {x:836, y:0, w:100, h:42, txt:"集团总裁", tx:885, ty:20, s:"gradient=135-#b7c9e3-#dee6f3"}
        ],
        i = 0, j, nd, s, att, sa;
    for (; i < nodes.length; i++) {
        nd = nodes[i];
        att = {};
        s = nd.s;
        s = s.split(';');
        for (j = 0; j < s.length; j++) {
            sa = s[j].split('=');
            att[sa[0]] = sa[1];
        }
        paper.rect(nd.x, nd.y, nd.w, nd.h).attr(att);
        paper.text(nd.tx, nd.ty, nd.txt).attr({"font-size":12,"font-weight":"bold"});
    }
}

function doFullSc(o, canv) {
    var stat = o.attr('isfull'),
        ct = $("#pageCon"), _t, m = $("div.contextDiagram"), vw, av;
    vw = YH.dom.getViewportWidth() - YH.dom.getScrollbarWidth() - 20;
    if (stat == '0') {
        o.attr('isfull', '1');
        o.css('top', parseInt(o.css('top'), 10) - 25);
        o.find('a').html('退出全视图模式');
        $('#footer').hide();
        $("#pantc").draggable("option", "disabled", true);
        av = Math.round((vw - 1035) / 2) + 20;
        m.width(1035);
        ct.css({ position: 'absolute', left: av, top: m.offset().top - 10 });
    } else {
        o.attr('isfull', '0');
        o.css('top', parseInt(o.css('top'), 10) + 25);
        o.find('a').html('全视图模式');
        $('#footer').show();
        $("#pantc").draggable("option", "disabled", false);
        $("#pantc").draggable({ axis: 'x' });
        m.width(937);
        ct.css({ position: 'static', left: 'auto', top: 'auto' });
    }
}

$(function () {
    if (typeof isBigMap != 'undefined' && isBigMap) _gIsBig = true;
    var canv = $("#pantc"), paper = Raphael(canv[0], 940, 1650),
        xmlurl = _gIsBig ? 'xhgraph.xml' : 'xhgraph.xml',
        fullsc = $("#fullsc");
    renderTitle_(paper);
    canv.mask('loading'); _gpne = $("#float_layer");
    $.ajax({
        dataType: 'xml', url: __globalDomain + '/content/contextdiagram/' + xmlurl, cache: false,
        success: function (xml) {
            var init = lrShape.init({ p: paper, callback: function () {
                var nid = this.node.id.replace('wf', ''),
                    taskId = _gtasks['wf' + nid], a;
                if (!taskId || isSelMod) return;
                taskId = taskId.taskId;
                a = document.createElement('a');
                a.href = "/DesignManage/ProjTaskInfo/" + taskId; // +"?projId=" + projId; // bts AIII-2200
                a.target = "_blank";
                $(document.body).append(a);
                a.click();
                $(a).remove();
                //showpanel(nid);
            }, dblcallback: function () {
                var nid = this.node.id.replace('wf', '');
                //showpanel(nid);
            }, showtitle: false
            });
            if (init) lrShape.analysisNode(xml);
            _gnodelist = lrShape.getNodes() || {};
            _gpoints = lrShape.getPoints() || {};
            _glabels = lrShape.getLabels() || {};
            //searchNode();
            /*YH.dom.fixedPosition();
            YH.dom.elemFixedPos(_gpne);
            _gpne.css({ left: (YH.dom.getViewportWidth() - _gpne.width()) / 2, 'bottom': 0 });
            _gpne.find('a.close').click(function () {
            _gpne.slideUp();
            });*/
            $.post('/Home/projTaskList/' + projId, function (data) {
                var i = 0, dt, imgStat, _rp, bb, imgSrc, stat, jfw, hm;
                for (; dt = data[i]; ++i) {
                    _gtasks['wf' + dt.nodeId] = dt;
                    _rp = _gnodelist['wf' + dt.nodeId];
                    if (!_rp) continue;
                    bb = _rp.rp.getBBox();
                    // 设置状态
                    stat = dt['status'];
                    showstatus(paper, bb, dt, stat);
                    // 判断时间延延迟
                    TimeDelay(paper, bb, dt, _rp.isPoints);
                }

                /*for (var _k in _gpoints) { // 关卡节点右键菜单
                var _node_gq = _gpoints[_k];
                $(_node_gq.rp.node).bind("contextmenu", function(ev) {
                menu_(ev, this);
                return false;
                });
                }*/
                var off_ = $("div.contextDiagram").offset(), _l = off_.left, _t = off_.top;
                fullsc.css({ left: _l + 945, top: _t }).attr('isfull', '0');
                fullsc.find('a').click(function () {
                    doFullSc(fullsc, canv);
                });
                //doFullSc(fullsc, canv);
                canv.unmask();
                //canv.get(0).style.position = '';
            });
        }, error: function () { alert('出错了，xhgraphjs,请联系管理者！'); }
    });
});
