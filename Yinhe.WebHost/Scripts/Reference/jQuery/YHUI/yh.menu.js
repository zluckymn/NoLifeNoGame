/**
* menu.js
* Date:2010-11-16
*/
if (!window.YinHoo) window.YinHoo = {};
(function($) {
    YinHoo.menu = function(jq, opts) {
        var self = this;
        var defaults = {
            zIndex: 10000,
            left: 0,
            top: 0,
            onShow: $.noop,
            onHide: $.noop,
            onClick: function(ev) { }
        };
        function init(jq, opts) {
            var o = $.data(jq[0], "menu");
            if (o) {
                $.extend(o.opt, opts);
            } else {
                o = $.data(jq[0], "menu", { opt: $.extend({}, defaults, opts), menu: self });
                render(jq);
            }
            jq.css({ left: o.opt.left, top: o.opt.top });
        }
        function render(jq) {
            var _opt = $.data(jq[0], "menu").opt;
            var nMenus = [], _timer = null; // 存储菜单，包括子菜单
            menuFilter(jq);
            for (var i = 0; i < nMenus.length; i++) {
                var menu = nMenus[i];
                wrapMenu(menu);
                menu.find(">div.menu-item").each(function() { // hover show submenu
                    var _e = $(this);
                    _e.mouseover(function() {
                        _e.siblings().each(function() {
                            if (this.submenu) {
                                unSelAll(this.submenu)
                            }
                            $(this).removeClass("m-item-sel");
                        });
                        _e.addClass("m-item-sel");
                        var _smenu = _e[0].submenu; // 有子菜单，则显示
                        if (_smenu) {
                            var _w = _e.offset().left + _e.outerWidth() - 2;
                            if (_w + _smenu.outerWidth() > $(window).width()) {
                                _w = _e.offset().left - _smenu.outerWidth() + 2;
                            }
                            _setMenuPos(_smenu, { left: _w, top: _e.offset().top - 3 });
                        }
                    });
                });
                menu.find("div.menu-item").click(function() {
                    if (!this.submenu) {
                        _hide(jq);
                        var _href = $(this).attr("href");
                        if (_href) {
                            location.href = _href;
                        }
                    }
                    _opt.onClick.call(jq, this);
                });
                menu.bind("mouseenter", function() {
                    if (_timer) {
                        clearTimeout(_timer);
                        _timer = null;
                    }
                }).bind("mouseleave", function() {
                    _timer = setTimeout(function() {
                        _hide(jq);
                    }, 100);
                });
            }
            function menuFilter(jq) { // 菜单分立
                nMenus.push(jq);
                jq.find(">div").each(function() {
                    var sf = $(this);
                    var submenu = sf.find(">div");
                    if (submenu.length > 0) {
                        submenu.insertAfter(jq); // 子菜单分离
                        sf[0].submenu = submenu;
                        menuFilter(submenu);
                    }
                });
            }
            function wrapMenu(menu) {
                menu.addClass("gmenu").find(">div").each(function() {
                    var sf = $(this);
                    if (sf.hasClass("m-item-sep")) {
                        sf.html("&#160;");
                    } else {
                        var txt = sf.html();
                        sf.addClass("menu-item").empty().html("<div class=\"m-item-text\">" + txt + "</div>");
                        var icon = sf.attr("iconCls") || sf.attr("icon");
                        if (icon) {
                            $("<div class=\"m-item-icon\"></div>").addClass(icon).appendTo(sf);
                        }
                        if (this.submenu) { // 有子菜单增加提示
                            $("<div class=\"menu-rightarrow\"></div>").appendTo(sf);
                        }
                    }
                });
                menu.hide();
            }
        }
        function _setMenuPos(jq, pos, cb) {
            if (pos) {
                jq.css(pos);
            }
            jq.show(1, function() {
                jq.css("z-index", defaults.zIndex++);
                if (typeof cb == 'function') cb();
            });
        }
        function _show(jq, pos) {
            var _lt = $.data(jq[0], "menu").opt;
            if (pos) {
                _lt.left = pos.left;
                _lt.top = pos.top;
            }
            _setMenuPos(jq, { left: _lt.left, top: _lt.top }, function() {
                _lt.onShow.call(jq);
                $(document).one("click", function(e) {
                    if (!$.contains(jq[0], e.target) && !$(e.target).parent(".menu")[0]) _hide(jq);
                });
            });
        }
        function _hide(jq) {
            var _opt = $.data(jq[0], "menu").opt;
            unSelAll(jq);
            _opt.onHide.call(jq);
            return false
        }
        function unSelAll(menu) {
            if (!menu[0]) return;
            menu.find("div.menu-item").each(function() {
                if (this.submenu) {
                    unSelAll(this.submenu)
                }
                $(this).removeClass("m-item-sel");
            });
            menu.hide();
        }
        init(jq, opts);
        _show(jq);
        this.show = function(jq, pos) {
            _show(jq, pos)
        }
    }
    $.fn.menu = function(opts) {
        var hd = $(this).data("menu");
        if (!hd) {
            return new YinHoo.menu($(this), opts || {});
        }
        hd.menu.show($(this), { left: opts.left, top: opts.top });
    }
})(jQuery);