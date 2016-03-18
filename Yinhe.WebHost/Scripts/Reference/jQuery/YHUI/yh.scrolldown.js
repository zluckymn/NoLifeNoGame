/*
* yh.scrolldown.js
* 2011-1-5
* version: 1.0
*/
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
        loadend: false,
        afterLoad: function(_1, _2) { }
    };
    $.fn.scrolldown = function(opt) {
        this.each(function() {
            new YinHoo.scrolldown($(this), opt);
        })
    }
})(jQuery);