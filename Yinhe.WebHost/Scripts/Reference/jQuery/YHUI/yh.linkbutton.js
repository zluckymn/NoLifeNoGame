/*
 * yh.linkbutton.js
 * 2011-1-5
 * version: 1.0
 */
(function($) {
    YinHoo.linkbutton = function(jq, opts) {
        var self = this;
        function initOpt(jq) {
            var op = {
                id: jq.attr("id"),
                text: $.trim(jq.html()),
                disabled: (jq.attr("disabled") ? true : undefined),
                plain: (jq.attr("plain") ? jq.attr("plain") == "true" : undefined),
                iconCls: (jq.attr("icon") || jq.attr("iconCls"))
            };
            var o = $.data(jq[0], "linkbutton");
            if (o) {
                $.extend(o.opt, opts);
            } else {
                $.data(jq[0], "linkbutton", { opt: $.extend({}, YinHoo.linkbutton.defaults, op, opts), linkbutton: self });
                jq.removeAttr("disabled");
            }
        }
        function setAttr(jq, disable) {
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
        function render(jq) {
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
            getOpt: function(jq) {
                return $.data(jq[0], "linkbutton").opt;
            },
            enable: function(jq) {
                return setAttr(jq, false);
            },
            disable: function(jq) {
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
    $.fn.linkbutton = function(opt) {
        this.each(function() {
            new YinHoo.linkbutton($(this), opt);
        })
    }
})(jQuery);