/**
 * YH.ui.js
 * Copyright Yinhoo
 * Date:2010-11-4 latest: 2010-11-12
 */
YinHoo = {
	version:'1.0',
	imgUrl: '/Content/Images/zh-cn/'
};

(function($){ // linkbutton
	YinHoo.linkbutton = function(jq, opts){
		var self = this;
		function initOpt(jq){
			var op = {
				id: jq.attr("id"),
				text: $.trim(jq.html()),
				disabled: (jq.attr("disabled") ? true: undefined),
				plain: (jq.attr("plain") ? jq.attr("plain") == "true": undefined),
				iconCls: (jq.attr("icon") || jq.attr("iconCls"))
			};
			var o = $.data(jq[0], "linkbutton");
			if (o) {
				$.extend(o.opt, opts);
			} else {
				$.data(jq[0], "linkbutton", {opt: $.extend({}, YinHoo.linkbutton.defaults, op, opts), linkbutton: self});
				jq.removeAttr("disabled");
			}
		}
		function setAttr(jq, disable){
			var o = $.data(jq[0], "linkbutton");
			if (disable) {
				o.opt.disabled = true;
				var hrf = jq.attr("href");
				if (hrf) {
					o.href = hrf;
					jq.attr("href", "javascript:void(0)");
				}
				var hclk = jq.attr("onclick");
				if (hclk) {
					o.onclick = hclk;
					jq.attr("onclick", null);
				}
				jq.addClass("l-btn-disabled");
			} else {
				o.opt.disabled = false;
				if (o.href) {
					jq.attr("href", o.href);
				}
				if (o.onclick) {
					jq.click = o.onclick;
				}
				jq.removeClass("l-btn-disabled");
			}
		}
		function render(jq){
			var _opt = $.data(jq[0], "linkbutton").opt;
			jq.empty().addClass("l-btn");
			if (_opt.id) {
				jq.attr("id", _opt.id);
			}
			if (_opt.plain) {
				jq.addClass("l-btn-plain");
			} else {
				jq.removeClass("l-btn-plain");
			}
			if (_opt.text) {
				jq.html(_opt.text).wrapInner("<span class=\"l-btn-left\"><span class=\"l-btn-text\"></span></span>");
				if (_opt.iconCls) {
					jq.find(".l-btn-text").addClass(_opt.iconCls).css("padding-left", "20px");
				}
			} else {
				jq.html("&nbsp;").wrapInner("<span class=\"l-btn-left\"><span class=\"l-btn-text\"><span class=\"l-btn-empty\"></span></span></span>");
				if (_opt.iconCls) {
					jq.find(".l-btn-empty").addClass(_opt.iconCls);
				}
			}
			setAttr(jq, _opt.disabled);
			return self;
		}
		$.extend(self, {
			getOpt: function(jq){
				return $.data(jq[0], "linkbutton").opt;
			},
			enable: function(jq){
				return setAttr(jq, false);
			},
			disable: function(jq){
				return setAttr(jq, true);
			}
		});
		initOpt(jq);
		render(jq);
	};
	YinHoo.linkbutton.defaults = {
		id: null,
		text: "",
		iconCls: null,
		plain: false,
		disabled: false
	};
	$.fn.linkbutton = function(opt){
		this.each(function(){
			new YinHoo.linkbutton($(this), opt);
		})
	}
})(jQuery);

// '<div id="{id}" class="{c_ls}"></div>'.compileTpl({id:"testid", c_ls: "x-win"})
String.prototype.compileTpl = function(o) {
	return this.replace(/\{\w+\}/g,	function(_) {
		return o[_.replace(/\{|\}/g, "")] || ""
	});
};

(function($){ // pagination
	YinHoo.pagination = function(jq, opts){
		var self = this;
		function initOpt(jq){
			var o = $.data(jq[0], "pagination");
			if (o) {
				$.extend(o.opt, opts);
			} else {
				$.data(jq[0], "pagination", {opt: $.extend({}, YinHoo.pagination.defaults, opts), pagination: self});
			}
		}
		function PagCal(jq){ // jQuery.pagination
			var _opt = $.data(jq[0], "pagination").opt;
			var pcc = $(".pagination-n", jq);
			return {
				getPages: function(){ // 返回页数
					return !_opt.totalPage ? Math.ceil(_opt.total / _opt.pageSize) : _opt.totalPage;
				},
				getInterval: function(current_page){
					var _half = Math.floor(_opt.display_pc / 2);
					var np = this.getPages();
					var upper_limit = np - _opt.display_pc; // 1 2 3 4 5 6 7
					var start = current_page > _half ? Math.max( Math.min(current_page - _half, upper_limit), 0 ) : 0;
					var end = current_page > _half ? Math.min(current_page + _half + (_opt.display_pc % 2), np) : Math.min(_opt.display_pc, np);
					return {start:start, end:end};
				},
				createLink: function(pageid, current_page){
					var ps = this.getPages(), strLink;
					pageid = pageid < 0 ? 0 : (pageid < ps ? pageid : (ps-1));
					if(pageid == current_page-1){
						strLink = $("<span class=\"current\">"+ (pageid+1) +"</span>");
					}else{
						strLink = $("<a>"+ (pageid+1) +"</a>").attr("href", _opt.link_to.replace(/__id__/, pageid+1));
					}
					strLink.data("pageid", pageid);
					return strLink;
				},
				appendRange: function(current_page, start, end){
					for(var i = start; i < end; i++){
						this.createLink(i, current_page).appendTo(pcc);
					}
				},
				getLinks: function(current_page, eventHandler){
					var ps = this.getPages(), begin, end,
						interval = this.getInterval(current_page);
					pcc.empty();
					if (interval.start > 0 && _opt.side_pc > 0){
						end = Math.min(_opt.side_pc, interval.start);
						this.appendRange(current_page, 0, end);
						if(_opt.side_pc < interval.start && _opt.ellipse_text){
							$("<span>"+_opt.ellipse_text+"</span>").appendTo(pcc);
						}
					}
					this.appendRange(current_page, interval.start, interval.end);
					if (interval.end < ps && _opt.side_pc > 0){
						if(ps - _opt.side_pc > interval.end && _opt.ellipse_text){
							$("<span>"+_opt.ellipse_text+"</span>").appendTo(pcc);
						}
						begin = Math.max(ps - _opt.side_pc, interval.end);
						this.appendRange(current_page, begin, ps);
					}
					$('a', pcc).click(eventHandler);
				}
			}
		}
		function render(jq){
			var _opt = $.data(jq[0], "pagination").opt, tmp = "";
			var pag = jq.addClass("pagination").empty().append("<table cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr></tr></table>");
			var tr = $("tr", pag);
			if (_opt.showPageList) {
				var ps = $("<select class=\"pagination-page-list\"></select>");
				for (var i = 0; i < _opt.pageList.length; i++) {
					tmp += "<option" + (_opt.pageList[i] == _opt.pageSize ? " selected=\"selected\"" : "") + ">" + _opt.pageList[i] + "</option>";
				}
				$(ps).append(tmp);$("<td></td>").append(ps).appendTo(tr);
				$("<td><div class=\"pagination-btn-separator\"></div></td>").appendTo(tr);
				_opt.pageSize = parseInt(ps.val());
			}
			$("<td><a href=\"javascript:void(0)\" icon=\"pagination-first\"></a></td><td><a href=\"javascript:void(0)\" icon=\"pagination-prev\"></a></td>").appendTo(tr); // first prev
			if (_opt.showPages){ // 显示页码
				$("<td><div class=\"pagination-btn-separator\"></div></td>").appendTo(tr);
				$("<td><div class=\"pagination-n\"></div></td>").appendTo(tr);
			}
			if (_opt.showInput) {
				$("<td><div class=\"pagination-btn-separator\"></div></td>").appendTo(tr);
				$("<span style=\"padding-left:6px;\"></span>").html(_opt.beforePageText).wrap("<td></td>").parent().appendTo(tr);
				$("<td><input class=\"pagination-num\" type=\"text\" value=\"1\" size=\"2\"></td>").appendTo(tr);
				$("<span style=\"padding-right:6px;\"></span>").wrap("<td></td>").parent().appendTo(tr);
			}
			$("<td><div class=\"pagination-btn-separator\"></div></td><td><a href=\"javascript:void(0)\" icon=\"pagination-next\"></a></td><td><a href=\"javascript:void(0)\" icon=\"pagination-last\"></a></td>").appendTo(tr); // separator next last
			if (_opt.showRefresh) {
				$("<td><div class=\"pagination-btn-separator\"></div></td><td><a href=\"javascript:void(0)\" icon=\"pagination-load\"></a></td>").appendTo(tr);
			}
			if (_opt.buttons) { // 自定义按钮部分
				$("<td><div class=\"pagination-btn-separator\"></div></td>").appendTo(tr);
				for (var i = 0; i < _opt.buttons.length; i++) {
					var _5 = _opt.buttons[i];
					if (_5 == "-") {
						$("<td><div class=\"pagination-btn-separator\"></div></td>").appendTo(tr);
					} else {
						var td = $("<td></td>").appendTo(tr);
						$("<a href=\"javascript:void(0)\"></a>").addClass("l-btn").css("float", "left").text(_5.text || "").attr("icon", _5.iconCls || "").bind("click", eval(_5.handler || function() {})).appendTo(td).linkbutton({plain: true});
					}
				}
			}
			$("<div class=\"pagination-info\"></div>").appendTo(pag);
			$("<div style=\"clear:both;\"></div>").appendTo(pag);
			$("a[icon^=pagination]", pag).linkbutton({plain: true});
			jq.find("a[icon=pagination-first]").unbind(".pagination").bind("click.pagination",	function() {
				if (_opt.current_page > 1) {
					SelectPage(jq, 1);
				}
			});
			jq.find("a[icon=pagination-prev]").unbind(".pagination").bind("click.pagination", function() {
				if (_opt.current_page > 1) {
					SelectPage(jq, _opt.current_page - 1);
				}
			});
			jq.find("a[icon=pagination-next]").unbind(".pagination").bind("click.pagination", function() {
				if (_opt.current_page < PagCal(jq).getPages()) {
					SelectPage(jq, _opt.current_page + 1);
				}
			});
			jq.find("a[icon=pagination-last]").unbind(".pagination").bind("click.pagination", function() {
				if (_opt.current_page < PagCal(jq).getPages()) {
					SelectPage(jq, PagCal(jq).getPages());
				}
			});
			jq.find("a[icon=pagination-load]").unbind(".pagination").bind("click.pagination", function() {
				if (_opt.onBeforeRefresh.call(jq, _opt.current_page, _opt.pageSize) != false) {
					SelectPage(jq, _opt.current_page);
					_opt.onRefresh.call(jq, _opt.current_page, _opt.pageSize);
				}
			});
			jq.find("input.pagination-num").unbind(".pagination").bind("keydown.pagination", function(e) {
				if (e.keyCode == 13) {
					var _page = parseInt($(this).val()) || 1;
					SelectPage(jq, _page);
				}
			});
			jq.find(".pagination-page-list").unbind(".pagination").bind("change.pagination", function() {
				_opt.pageSize = $(this).val();
				_opt.onChangePageSize.call(jq, _opt.pageSize);
				SelectPage(jq, _opt.current_page);
			});
		}
		function SelectPage(jq, new_current_page){
			var _opt = $.data(jq[0], "pagination").opt;
			var _totalpage = PagCal(jq).getPages();
			var _f = new_current_page;
			if (new_current_page < 1) {
				_f = 1;
			}
			if (new_current_page > _totalpage) {
				_f = _totalpage;
			}
			_opt.onSelectPage.call(jq, _f, _opt.pageSize);
			_opt.current_page = _f;
			trigger_page(jq);
		}
		function linkeventHandler(ev){
			var cp = parseInt($(ev.target).text());
			SelectPage(jq, cp);
			ev.stopPropagation();
		}
		function trigger_page(jq) {
			var _opt = $.data(jq[0], "pagination").opt;
			var _totalpage = PagCal(jq).getPages();
			var num = jq.find("input.pagination-num");
			num.val(_opt.current_page);
			num.parent().next().find("span").html(_opt.afterPageText.replace(/{pages}/, _totalpage));
			var _dmsg = _opt.displayMsg;
			if(!_opt.totalPage) _dmsg = _dmsg.compileTpl({from:_opt.pageSize * (_opt.current_page - 1) + 1, to:Math.min(_opt.pageSize * (_opt.current_page), _opt.total), total:_opt.total});
			jq.find(".pagination-info").html(_dmsg);
			$("a[icon=pagination-first],a[icon=pagination-prev]", jq).linkbutton({
				disabled: (_opt.current_page == 1)
			});
			$("a[icon=pagination-next],a[icon=pagination-last]", jq).linkbutton({
				disabled: (_opt.current_page == _totalpage)
			});
			if (_opt.loading) {
				jq.find("a[icon=pagination-load]").find(".pagination-load").addClass("pagination-loading");
			} else {
				jq.find("a[icon=pagination-load]").find(".pagination-load").removeClass("pagination-loading");
			}
			if(_opt.showPages){
				PagCal(jq).getLinks(_opt.current_page, linkeventHandler);
			}
			return self;
		}
		function loading(jq, loaded) {
			var _opt = $.data(jq[0], "pagination").opt;
			_opt.loading = loaded;
			if (_opt.loading) {
				jq.find("a[icon=pagination-load]").find(".pagination-load").addClass("pagination-loading");
			} else {
				jq.find("a[icon=pagination-load]").find(".pagination-load").removeClass("pagination-loading");
			}
		}
		$.extend(self, {
			options: function(jq) {
				return $.data(jq[0], "pagination").options;
			},
			loading: function(jq) {
				loading(jq, true);
			},
			loaded: function(jq) {
				loading(jq, false);
			}
		});
		initOpt(jq);
		render(jq);
		trigger_page(jq);
	}
	YinHoo.pagination.defaults = {
		total: 1,
		totalPage: 0,
		pageSize: 10, // 每页items
		display_pc: 7, // 页码中间部分
		current_page: 1,
		side_pc: 1, // 页码2边部分
		ellipse_text: "...",
		pageList: [10, 20, 30, 50],
		loading: false,
		link_to:"javascript:;",
		buttons: null,
		showPages: true, // 是否显示页码
		showPageList: true,
		showInput: true,
		showRefresh: true,
		onSelectPage: function(current_page, pagesize) {},
		onBeforeRefresh: function(current_page, pagesize) {},
		onRefresh: function(current_page, pagesize) {},
		onChangePageSize: function(pagesize) {},
		beforePageText: "Page",
		afterPageText: "of {pages}",
		displayMsg: "Displaying {from} to {to} of {total} items"
	};
	$.fn.pagination = function(opt){
		this.each(function(){
			new YinHoo.pagination($(this), opt);
		})
	}
})(jQuery);

(function($) { // scroll pagination
    YinHoo.scrolldown = function(jq, opts) {
        var self = this;
        function initOpt(jq) {
            var o = $.data(jq[0], "scrolldown");
            if (o) {
                $.extend(o.opt, opts);
            } else {
                $.data(jq[0], "scrolldown", { opt: $.extend({}, YinHoo.scrolldown.defaults, opts), scrolldown: self });
            }
        }
        function onScroll(jq) {
            var _opt = $.data(jq[0], "scrolldown").opt;
            if (!_opt.url) return false;
            var container = jq, nDivHeight = container.height();
            var nHeight = 0, nTop = 0, npage = _opt.current_page;
            container.scroll(function() {
                nHight = $(this)[0].scrollHeight;
                nTop = $(this)[0].scrollTop;
                if (nTop + nDivHeight >= nHight) {
                    if ((_opt.ps_unknown || npage < _opt.pageSize) && !_opt.isloading) {
                        _opt.isloading = true;
                        npage++;
                        //YH.console(_opt.current_page + "====" + _opt.pageSize);
                        if (_opt.ps_unknown && _opt.loadend) return;
                        loaddata(jq, npage);
                        _opt.current_page = npage;
                    }
                }
            });
        }
        function loaddata(jq, current_page) {
            var _opt = $.data(jq[0], "scrolldown").opt, loading = null;
            var _url = _opt.url.replace(/__id__/, current_page);
            loading = $("<div class=\"loading\" style=\"padding:20px 0;width:100%;text-align:center;\"><img src=\"" + YinHoo.imgUrl + "persons/loading19.gif\"></div>").appendTo(jq);
            var ajaxOpt = {
                type: 'POST',
                url: _url,
                error: function() {
                    loading.html("数据加载出错啦 :(");
                },
                complete: function() {
                    loading.remove();
                },
                success: function(data) {
                    if (data.length == 0) {
                        _opt.ps_unknown = true;
                        _opt.pageSize = _opt.current_page; _opt.loadend = true;
                    }
                    _opt.isloading = false;
                    jq.append(data);
                    _opt.afterLoad.call(jq, _opt.current_page, _opt.pageSize);
                }
            };
            if (!$.isEmptyObject(_opt.ajaxOptions)) {
                ajaxOpt = $.extend(ajaxOpt, _opt.ajaxOptions);
            }
            $.ajax(ajaxOpt);
        }
        initOpt(jq); onScroll(jq);
    };
    YinHoo.scrolldown.defaults = {
        current_page: 1,
        pageSize: 1,
        ps_unknown: false, // 总页数未知，通过最后返回的为空判断到底
        url: null,
        ajaxOptions: {},
        isloading: false,
        loadend:false,
        afterLoad: function(_1, _2) { }
    };
    $.fn.scrolldown = function(opt) {
        this.each(function() {
            new YinHoo.scrolldown($(this), opt);
        })
    }
})(jQuery);