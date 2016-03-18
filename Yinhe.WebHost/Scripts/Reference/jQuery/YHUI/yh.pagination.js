/*
* yh.pagination.js
* 2011-1-5
* version: 1.0
*/
(function($) {
    YinHoo.pagination = function(jq, opts) {
        var self = this;
        function initOpt(jq) {
            var o = $.data(jq[0], "pagination");
            if (o) {
                $.extend(o.opt, opts);
            } else {
                $.data(jq[0], "pagination", { opt: $.extend({}, YinHoo.pagination.defaults, opts), pagination: self });
            }
        }
        function PagCal(jq) { // jQuery.pagination
            var _opt = $.data(jq[0], "pagination").opt;
            var pcc = $(".pagination-n", jq);
            return {
                getPages: function() { // 返回页数
                    return !_opt.totalPage ? Math.ceil(_opt.total / _opt.pageSize) : _opt.totalPage;
                },
                getInterval: function(current_page) {
                    var _half = Math.floor(_opt.display_pc / 2);
                    var np = this.getPages();
                    var upper_limit = np - _opt.display_pc; // 1 2 3 4 5 6 7
                    var start = current_page > _half ? Math.max(Math.min(current_page - _half, upper_limit), 0) : 0;
                    var end = current_page > _half ? Math.min(current_page + _half + (_opt.display_pc % 2), np) : Math.min(_opt.display_pc, np);

                    return { start: start, end: end };
                },
                createLink: function(pageid, current_page) {
                    var ps = this.getPages(), strLink;
                    pageid = pageid < 0 ? 0 : (pageid < ps ? pageid : (ps - 1));
                    if (pageid == current_page - 1) {
                        strLink = $("<span class=\"current\">" + (pageid + 1) + "</span>");
                    } else {
                        strLink = $("<a>" + (pageid + 1) + "</a>").attr("href", _opt.link_to.replace(/__id__/, pageid + 1));
                    }
                    strLink.data("pageid", pageid);
                    return strLink;
                },
                appendRange: function(current_page, start, end) {
                    for (var i = start; i < end; i++) {
                        this.createLink(i, current_page).appendTo(pcc);
                    }
                },
                getLinks: function(current_page, eventHandler) {
                    var ps = this.getPages(), begin, end,
						interval = this.getInterval(current_page);
                    pcc.empty();
                    if (interval.start > 0 && _opt.side_pc > 0) {
                        end = Math.min(_opt.side_pc, interval.start);
                        this.appendRange(current_page, 0, end);
                        if (_opt.side_pc < interval.start && _opt.ellipse_text) {
                            $("<span>" + _opt.ellipse_text + "</span>").appendTo(pcc);
                        }
                    }
                    this.appendRange(current_page, interval.start, interval.end);
                    if (interval.end < ps && _opt.side_pc > 0) {
                        if (ps - _opt.side_pc > interval.end && _opt.ellipse_text) {
                            $("<span>" + _opt.ellipse_text + "</span>").appendTo(pcc);
                        }
                        begin = Math.max(ps - _opt.side_pc, interval.end);
                        this.appendRange(current_page, begin, ps);
                    }
                    $('a', pcc).click(eventHandler);
                }
            }
        }
        function render(jq) {
            var _opt = $.data(jq[0], "pagination").opt, tmp = "";
            var pag = jq.addClass("pagination").empty().append("<table cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr></tr></table>");
            var tr = $("tr", pag);
            if (_opt.showPageList) {
                var ps = $("<select class=\"pagination-page-list\"></select>");
                for (var i = 0; i < _opt.pageList.length; i++) {
                    tmp += "<option" + (_opt.pageList[i] == _opt.pageSize ? " selected=\"selected\"" : "") + ">" + _opt.pageList[i] + "</option>";
                }
                $(ps).append(tmp); $("<td></td>").append(ps).appendTo(tr);
                $("<td><div class=\"pagination-btn-separator\"></div></td>").appendTo(tr);
                _opt.pageSize = parseInt(ps.val());
            }
            $("<td><a href=\"javascript:void(0)\" icon=\"pagination-first\"></a></td><td><a href=\"javascript:void(0)\" icon=\"pagination-prev\"></a></td>").appendTo(tr); // first prev
            if (_opt.showPages) { // 显示页码
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
                        $("<a href=\"javascript:void(0)\"></a>").addClass("l-btn").css("float", "left").text(_5.text || "").attr("icon", _5.iconCls || "").bind("click", eval(_5.handler || function() { })).appendTo(td).linkbutton({ plain: true });
                    }
                }
            }
            $("<div class=\"pagination-info\"></div>").appendTo(pag);
            $("<div style=\"clear:both;\"></div>").appendTo(pag);
            $("a[icon^=pagination]", pag).linkbutton({ plain: true });
            jq.find("a[icon=pagination-first]").unbind(".pagination").bind("click.pagination", function() {
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
        function SelectPage(jq, new_current_page) {
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
        function linkeventHandler(ev) {
            var cp = parseInt($(ev.target).text());
            SelectPage(jq, cp);
            ev.stopPropagation();
        }
        function trigger_page(jq) {
            var ops = $.data(jq[0], "pagination");
            if (!ops) return;
            var _opt = ops.opt;
            var _totalpage = PagCal(jq).getPages();
            var num = jq.find("input.pagination-num");
            num.val(_opt.current_page);
            num.parent().next().find("span").html(_opt.afterPageText.replace(/{pages}/, _totalpage));
            var _dmsg = _opt.displayMsg;
            if (!_opt.totalPage) _dmsg = _dmsg.compileTpl({ from: _opt.pageSize * (_opt.current_page - 1) + 1, to: Math.min(_opt.pageSize * (_opt.current_page), _opt.total), total: _opt.total });
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
            if (_opt.showPages) {
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
        link_to: "javascript:;",
        buttons: null,
        showPages: true, // 是否显示页码
        showPageList: true,
        showInput: true,
        showRefresh: true,
        onSelectPage: function(current_page, pagesize) { },
        onBeforeRefresh: function(current_page, pagesize) { },
        onRefresh: function(current_page, pagesize) { },
        onChangePageSize: function(pagesize) { },
        beforePageText: "Page",
        afterPageText: "of {pages}",
        displayMsg: "Displaying {from} to {to} of {total} items"
    };
    $.fn.pagination = function(opt) {
        this.each(function() {
            new YinHoo.pagination($(this), opt);
        })
    }
})(jQuery);