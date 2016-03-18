/**
 * wkpcomm.js 2011/5/23 by Qingbao.Zhao
 */
(function ($) {
    $.fn.extend({
        wkelastic: function () {
            var mimics = ['paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft', 'fontSize', 'lineHeight', 'fontFamily', 'width', 'fontWeight'];
            return this.each(function () {
                if (this.type != 'textarea') {
                    return false;
                }
                var $textarea = jQuery(this),
					$twin = jQuery('<div />').css({ 'position': 'absolute', 'display': 'none', 'word-wrap': 'break-word' }),
					lineHeight = parseInt($textarea.css('line-height'), 10) || parseInt($textarea.css('font-size'), '10'),
					minheight = parseInt($textarea.css('height'), 10) || lineHeight * 3,
					maxheight = parseInt($textarea.css('max-height'), 10) || Number.MAX_VALUE,
					goalheight = 0,
					i = 0;
                if (maxheight < 0) { maxheight = Number.MAX_VALUE; }
                $twin.appendTo($textarea.parent());
                var i = mimics.length;
                while (i--) {
                    $twin.css(mimics[i].toString(), $textarea.css(mimics[i].toString()));
                }
                function setHeightAndOverflow(height, overflow) {
                    curratedHeight = Math.floor(parseInt(height, 10));
                    if ($textarea.height() != curratedHeight) {
                        $textarea.css({ 'height': curratedHeight + 'px', 'overflow': overflow });
                    }
                }
                function update() {
                    var textareaContent = $textarea.val().replace(/&/g, '&amp;').replace(/  /g, '&nbsp;').replace(/<|>/g, '&gt;').replace(/\n/g, '<br />');
                    var twinContent = $twin.html().replace(/<br>/ig, '<br />');
                    if (textareaContent + '&nbsp;' != twinContent) {
                        $twin.html(textareaContent + '&nbsp;');
                        if (Math.abs($twin.height() + lineHeight - $textarea.height()) > 3) {
                            var goalheight = $twin.height() + lineHeight;
                            if (goalheight >= maxheight) {
                                setHeightAndOverflow(maxheight, 'auto');
                            } else if (goalheight <= minheight) {
                                //setHeightAndOverflow(minheight,'hidden');
                            } else {
                                setHeightAndOverflow(goalheight - 4, 'hidden');
                            }
                        }
                    }
                }
                $textarea.css({ 'overflow': 'hidden' });
                $textarea.bind('keyup change cut paste', function () {
                    update();
                });
                $textarea.live('input paste', function (e) { setTimeout(update, 250); });
                update();
            });
        },
        wknumberbox: function (maxnum, dot) {
            $(this).css({ imeMode: "disabled", textAlign: "center" });
            this.bind("keypress", function (e) {
                var kc = e.keyCode || e.which, inputstr = String.fromCharCode(kc), st = DGUntil.getSelectionText(), v = this.value, pos = DGUntil.getPositionForInput(this);
                if (st) {
                    pos = v.indexOf(st); v = v.replace(st, "");
                }
                v = v.substr(0, pos) + inputstr + v.substr(pos);
                if (dot) {
                    if (!/^\d+(\.)?\d*$/g.test(v)) return false;
                } else {
                    if (!/^\d+$/g.test(v) || /^0/.test(v)) return false;
                }
            });
            this.bind("paste dragenter", function () {
                return false;
            });
        },
        stcenter: function (h) {
            var _mh = Math.floor((h - $(this).height()) / 2);
            if ($(this)[0].tagName.toLowerCase() == 'input') _mh -= 2;
            $(this).css("marginTop", _mh).focus();
            return this;
        }
    });
    $.extend(Array.prototype, {
        removeById: function (idname, id) {
            for (var i = 0, _len = this.length; i < _len; i++) {
                if (this[i][idname] == id) {
                    this.splice(i, 1);
                    return this;
                }
            }
            return this;
        }
    });
    $.fn.a3Load = function (url, params, fn) {
        var self = this;
        if (params) {
            if ($.isFunction(params)) {
                fn = params;
                params = null;
            }
        }
        $.ajax({
            url: url,
            data: params,
            success: function (rs) {
                $(self).html(rs);
                fn && fn.call(self);
            },
            dataType: 'html'
        });
    };
    $.isCookieEnable = function () {
        var isEnabled = navigator.cookieEnabled;
        if (isEnabled && $.browser.webkit) {
            var cookieName = 'COOKIES_TEST_' + new Date().getTime();
            $.cookie(cookieName, '1');
            if ($.cookie(cookieName)) {
                return false;
            }
            $.cookie(cookieName, null);
        }
        return isEnabled;
    }
    $.fn.onumberbox = function (maxnum) {
        $(this).css({ imeMode: "disabled", textAlign: "center" });
        this.bind("keypress", function (e) {
            var kc = e.keyCode || e.which, inputstr = String.fromCharCode(kc), st = oUntil.getSelectionText(), v = this.value, pos = oUntil.getPositionForInput(this);
            if (st) {
                pos = v.indexOf(st); v = v.replace(st, "");
            }
            v = v.substr(0, pos) + inputstr + v.substr(pos);
            if (v.length > 1 && !/^(-)?\d+$/g.test(v) || v.length === 1 && !/^[-|\d]$/g.test(v) || /^0/.test(v)) return false;
        });
        this.bind("paste dragenter", function () {
            return false;
        });
    };
})(jQuery);

Date.prototype.Format = function(fmt){
	var o = {
		"M+" : this.getMonth() + 1, //月份
        "d+" : this.getDate(), //日
        "h+" : this.getHours(), //小时
        "m+" : this.getMinutes(), //分
        "s+" : this.getSeconds(), //秒
        "q+" : Math.floor((this.getMonth() + 3) / 3), //季度
        "S" : this.getMilliseconds() //毫秒
	};
    if (/(y+)/.test(fmt)) fmt = fmt.replace(RegExp.$1, (this.getFullYear() + "").substr(4 - RegExp.$1.length));
    for (var k in o){
        if (new RegExp("(" + k + ")").test(fmt)) fmt = fmt.replace(RegExp.$1, (RegExp.$1.length == 1) ? (o[k]) : (("00" + o[k]).substr(("" + o[k]).length)));
	}
    return fmt;
}
Date.prototype.addDays = function(d){
    this.setDate(this.getDate() + d);
};
Date.prototype.isLeapYear = function(){
	var year = this.getFullYear();
	return !!((year & 3) == 0 && (year % 100 || (year % 400 == 0 && year)));
}
Date.prototype.getDaysOfMonth = function(){
	var dm = [31,28,31,30,31,30,31,31,30,31,30,31];
	var m = this.getMonth();
	return m == 1 && this.isLeapYear() ? 29 : dm[m];
}
Date.prototype.getFirstDateOfMonth = function() {
	return new Date(this.getFullYear(), this.getMonth(), 1);
}
Date.prototype.getLastDateOfMonth = function(){
	return new Date(this.getFullYear(), this.getMonth(), this.getDaysOfMonth());
}
Date.prototype.addMonths = function(value){
    var day = this.getDate();
	if (day > 28) {
		day = Math.min(day, this.getFirstDateOfMonth().addMonths(value).getLastDateOfMonth().getDate());
	}
	this.setDate(day);
	this.setMonth(this.getMonth() + value);
    return this;
}
Date.prototype.addYears = function(value){
	this.setFullYear(this.getFullYear() + value);
}

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
	getPositionForTextArea: function(ctrl){ // 多行文本框
		var CaretPos = 0;
		if(document.selection){ // IE Support
			ctrl.focus();
			var Sel = document.selection.createRange();
			var Sel2 = Sel.duplicate();
			Sel2.moveToElementText(ctrl);
			var CaretPos = -1;
			while(Sel2.inRange(Sel)){
				Sel2.moveStart('character');
				CaretPos++;
			}
		}else if(ctrl.selectionStart || ctrl.selectionStart == '0'){ // Firefox support
			CaretPos = ctrl.selectionStart;
		}
		return (CaretPos);
	},
	setCursorPosition: function(ctrl, poss, pose){ // 设置光标位置
		if(!pose) pose = poss;
		if(ctrl.setSelectionRange){
			ctrl.focus();
			ctrl.setSelectionRange(poss,pose);
		}else if(ctrl.createTextRange){
			var range = ctrl.createTextRange();
			range.collapse(true);
			range.moveEnd('character', pose);
			range.moveStart('character', poss);
			range.select();
		}
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
	},
	getYMD: function(date){
		var t = new Date();
		if(DGUntil.isDate(date)) t = new Date(date);
		return {year:t.getFullYear(), month:t.getMonth() + 1, day:t.getDate, current:t};
	},
	getTag: function(e){
		var tag = e.target;
		if(tag.tagName.toLowerCase() == 'div') tag = tag.parentNode;
		return tag;
	},
	refRowindex:function(cr){ // E12 => E 12
		var arr = cr.match(/([seg])(\d{1,})/i);
		return arr;
	}
};

var cdocs = (function(){ // 选择设计指引文档
	var tpl = ['<div><a href="javascript:;" class="adddoc">+添加文档</a></div>',
		'<table cellpadding="0" cellspacing="0">',
		'<thead><th>序号</th><th>文档名称</th><th>所属类别</th></thead>',
		'<tr><td>1</td><td>设计指引文档</td><td>分类一</td></tr>',
		'</table>',
		].join("");
	function __pbox(taskId){
		box(tpl, {boxid:'pbox', title:'选择设计指引文档', modal:true, cls:'shadow-container2',
			onOpen:function(o){
				o.db.find(".adddoc").click(function(){
					__fbox();
				});
			},
			submit_cb:function(o){
			}
		});
	}
	function __fbox(){
		box("", {boxid:'fbox', title:'选择设计指引文档', prt:'pbox', cls:'shadow-container3',
			onOpen:function(o){
			},
			submit_cb:function(o){
			}
		});
	}
	return {
		selectDocs:function(taskId){
			__pbox(taskId);
		}
	}
})();

function zero_fill_hex(num, digits){
	var s = num.toString(16);
	while(s.length < digits) s = "0" + s;
	return s;
}
function rgb2hex(rgb){
	if(rgb.charAt(0) == '#') return rgb.toLowerCase();
	var n = Number(rgb), ds = rgb.split(/\D+/), decimal = Number(ds[1]) * 65536 + Number(ds[2]) * 256 + Number(ds[3]);
	return ("#" + zero_fill_hex(decimal, 6)).toLowerCase();
}

function KeyTable(oInit){
	this.block = false;
	this._nBody = null;
	this._iOldX = null;
	this.isEdit = false;
	this._iOldY = null;
	var _that = null;

	function _fnSetFocus(nTarget,x,y, self){
		self._iOldX = x;
		self._iOldY = y;
		self.o.onmd(nTarget, $(nTarget).index());
		$(nTarget, self._nBody).trigger("click");
	}
	function _fnKey(e){
		var self = e.data.handle, cfg = self.o, sidx = 2;
        if (!cfg) return;
        cfg = cfg.config;
        if (!cfg) return;
        startindex = cfg.startindex;
        if (startindex) sidx = 0;
        if (_that.block || !self._nBody || self.o.isEdit || self._iOldX <= sidx) {
			return true;
		}
		if(e.metaKey || e.altKey || e.ctrlKey){
		    return true;
		}
		var x, y, iTableWidth = cfg.cols, iTableHeight = self.o.iRowCount, iKey = (e.keyCode == 9 && e.shiftKey) ? -1 : e.keyCode, _tr;
		if (e.target.tagName.toLowerCase() == 'textarea' || e.target.tagName.toLowerCase() == 'input') return;
		switch(iKey){
		    case 13: /* return */
		        if (!self.isEdit) {
		            e.preventDefault();
		            e.stopPropagation();
		            self.isEdit = true;
		            self.o.initEditor();
		        }
		        return true;
			case 27: /* esc */
				if(self.o.isEdit){
					//self.o.cancelEdit();
				}else{
					self.o.focusDiv.hide();
					_tr = self.o.curCell.td.parentNode;
					_tr.style.backgroundColor = '';
					_tr.setAttribute("isSel", "0");
				}
				return true;
			case -1:
			case 37: /* left arrow */
				if(self._iOldX > sidx+1){
					x = self._iOldX - 1;
					y = self._iOldY;
				}else{
					return false;
				}
				break;
			case 38: /* up arrow */
				if(self._iOldY > 0){
					x = self._iOldX;
					y = self._iOldY - 1;
				} else {
					return false;
				}
				break;
			case 9: /* tab */
			case 39: /* right arrow */
				if(self._iOldX < iTableWidth-1){
					x = self._iOldX + 1;
					y = self._iOldY;
				}else{
					return false;
				}
				break;
			case 40: /* down arrow */
				if(self._iOldY < iTableHeight-1){
					x = self._iOldX;
					y = self._iOldY + 1;
				}else{
					return false;
				}
				break;
			default:
				return true;
		}
		_fnSetFocus(_fnCellFromCoords(x, y, self),x,y, self);
		return false;
	}
	function _fnCellFromCoords(x, y, self){
		return $('tr:eq('+y+')>td:eq('+x+')', self._nBody)[0];
	}

	function _fnInit(oInit, that){
		_that = that;
		if(typeof oInit == 'undefined'){
			oInit = {};
		}
		if($.browser.mozilla || $.browser.opera){
			$(document).bind("keypress", {handle:that}, _fnKey);
		}else{
			$(document).bind("keydown", {handle:that}, _fnKey);
		}
		return this;
    }
    _fnInit(oInit, this);
}
KeyTable.prototype.setxy = function (nx, ny, t, o) {
    this._nBody = t[0];
    this.o = o;
    this.isEdit = false;
    this._iOldX = nx;
    this._iOldY = ny;
};

var lockTableHead = (function () {
    var vph = YH.dom.getViewportHeight(), hasBind = false, sts = {}, gtableid = '', ids = [];
    function doLockHead(o, tableObj) {
        var tableid = tableObj.id, scr, $tb = $(tableObj),
            _rows = $tb.find('thead')[0].rows,
            h = 0, i = 0, _tr, of = $tb.offset();
        for (; _tr = _rows[i]; ++i) {
            h += parseInt(_tr.cells[0].offsetHeight, 10) || 24;
        }
        ids.push(tableid);
        sts['table_' + tableid] = { "tbl": tableObj, "fHeadId": 'scr_' + tableid, "height": h, "left": of.left, "top": of.top, "hasCalW": false, "tblht": tableObj.offsetHeight };
        scr = document.getElementById('scr_' + tableid);
        if (!scr) scr = createScr_(tableObj, tableid);
        gtableid = gtableid == '' ? tableid : gtableid;
        bindRS_();
    }
    function createScr_(tableObj, tableid) {
        var scr = $('<table cellpadding="0" cellspacing="0" id="scr_' + tableid + '"><thead></thead><tbody></tbody></table>').appendTo(document.body),
            _th = scr.find('thead'), $tb = $(tableObj),
            ohead = $tb.find('thead').html();
        _th.html(ohead);
        scr = scr[0];
        if (tableObj.className != '') scr.className = tableObj.className;
        scr.style.display = 'none'; scr.style.zIndex = 999;
        YH.dom.elemFixedPos(scr);
        return scr;
    }
    function resetPos_(tbl) {
        var sct = $(document).scrollTop(), bt;
        if (tbl.top + tbl.height < sct && tbl.top + tbl.tblht - tbl.height / 2 > sct) {
            bt = 1;
        } else if (tbl.top + tbl.tblht < sct) {
            bt = 0;
        } else {
            bt = 0;
        }
        return bt;
    }
    function checkNowId() {
        var i = ids.length - 1, tbid, tbl, rid, r = 0;
        for (; i >= 0; i--) {
            tbid = ids[i];
            tbl = sts['table_' + tbid];
            rid = resetPos_(tbl);
            if (rid == 1) {
                r = tbid;
                break;
            }
        }
        return r;
    }
    function bindRS_() {
        if (hasBind) return;
        hasBind = true;
        $(window).bind("scroll", function () {
            var tbl = sts['table_' + gtableid],
                _b = resetPos_(tbl), scr = document.getElementById('scr_' + gtableid);
            if (_b == 0) {
                var _r = checkNowId();
                if (_r != 0) {
                    ;
                    gtableid = _r;
                    tbl = sts['table_' + gtableid];
                    _b = resetPos_(tbl); scr = document.getElementById('scr_' + gtableid);
                }
            }
            if (!tbl.hasCalW) {
                var _trs = $(scr).find('tr'), r1 = $(tbl.tbl).find('tbody')[0].rows[0],
                    cell, i = 0, _tr, _tr0, _w, _cspan;
                _tr0 = _trs[0];
                _tr = _trs[_trs.length - 1]; scr.style.width = $(tbl.tbl).width() + 1 + 'px';
                if (r1 && r1.cells) {
                    for (; cell = r1.cells[i]; ++i) {
                        _w = $(cell).width() + 'px';
                        _tr.cells[i].style.width = _w;
                        _cspan = cell.colSpan;
                        var _th = _tr0.cells[i];
                        _th.style.width = _w;
                        if (_cspan > 1) {
                            _th.style.width = (parseInt(_w, 10) + 9) + 'px';
                            _th.colSpan = _cspan;
                            i += _cspan - 1;
                            while (_cspan - 1 > 0) {
                                _th.nextSibling.style.display = 'none';
                                _cspan--;
                            }
                        }
                    }
                }
                tbl.hasCalW = true;
            }
            if (_b == 1) {
                scr.style.top = 0;
                scr.style.left = $(tbl.tbl).offset().left + 'px';
                scr.style.display = '';
            } else {
                scr.style.display = 'none';
            }
        }).bind("resize", function () {
            vph = YH.dom.getViewportHeight();
            var tbl = sts['table_' + gtableid], scr = document.getElementById('scr_' + gtableid);
            scr.style.left = $(tbl.tbl).offset().left + 'px';
        });
    }
    return {
        doLock: function (o, tableObj, h) {
            if (!tableObj) return;
            YH.dom.fixedPosition();
            doLockHead(o, tableObj);
        }
    };
})();