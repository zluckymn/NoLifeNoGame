/*
* Yinhoo.SelectTree.2.0
*
* Copyright (c) 2010 da.lei<da.lei@yinhootech.com>
*
* Dual licensed under the MIT and GPL licenses (same as jQuery):
*   http://www.opensource.org/licenses/mit-license.php
*   http://www.gnu.org/licenses/gpl.html
*
* CreateDate: 2010-10-25
* Ver 1.0
*
* target:
* 1、	Tree样式调整，风格变更为可配置
* 2、	数据载入多元化，可页面Push加载、异步一次加载&多次加载（同时放开5级限制）
* 3、	移动功能优化、增加批量移动操作
* 4、	增删改的免刷新接口
* 5、	树体的重新布局事件（拖拽改变树的大小）
* 6、	初次加载选中参数、点击CallBack事件、鼠标移动高亮事件优化，可定制
* 7、	旧JsTree原数据导入、兼容方案
* 8、	各式接口的回调函数
*/

(function ($) {

    $.fn.RemoveSelectTree = function () {
        $(this).html("");
        $(this).data("options", null);
        return true;
    }


    $.fn.ReloadSelectTree = function (options) {
        var clear = false;
        if (options.startXml != $(this).data("options").startXml) { clear = true; }
        options = $.extend($(this).data("options"), options);
        if (clear == true) {
            options.nowselected = null;
        }
        $(this).RemoveSelectTree();
        $(this).SelectTree(options);
        return true;
    }

    $.fn.selectTreeItem = function (id) {
        if (id) {
            $(this).find("div[id=div" + id + "]").find("div:first").click();
        } else {
            $(this).find("div[lv=1]:first").find("div:first").click();
        }
        return true;
    }

    $.fn.removeSelection = function () {
        $(this).data("options").selectedhighlight.hide();
        $(this).data("options").nowselected = null;
    }

    //全部折叠 可以指定层级
    $.fn.CollapseSelectTree = function (lv) {
        if (!lv) { lv = 1; }

        options = $(this).data("options");
        options.selectedhighlight.hide();

        var _Tree = $(this);
        _Tree.find("div[lv=" + lv + "]").each(function () {
            var obj = $(this);
            if (obj.data("node").attr("isfolder") != 1) {

                var h = obj.find("div:first").height();
                var img = obj.find("img:first");
                var ico = img.next();
                var cbk = null;
                if (ico.attr("type") == "checkbox") {
                    cbk = ico;
                    ico = ico.next();
                }
                img.attr("src", options.picurl + "ico-Collapsehover.gif");
                ico.attr("src", options.picurl + "ico-folder.gif");
                obj.find("div[lv=" + (parseInt(obj.attr("lv")) + 1) + "]").hide();
                obj.css("height", "auto");

                if (options.nowselected != null) {
                    if (options.nowselected.offset().top < options.outHtmlsInit.offset().top || options.nowselected.offset().top + options.nowselected.height() > options.outHtmlsInit.offset().top + options.outHtmlsInit.height()) {
                        options.selectedhighlight.hide();
                    } else {
                        options.selectedhighlight.css("top", options.nowselected.offset().top - _Tree.find(".tree:first").offset().top + _Tree.find(".tree:first").position().top).show(); //.css("width", options.outHtmlsInit.width() + _Tree.configurePYWidth())
                    }
                }
                if (options.highlight != null) options.highlight.hide();
            }
        });
        return true;
    }

    $.fn.ExpandSelectTree = function () {
        options = $(this).data("options");
        options.selectedhighlight.hide();

        var _Tree = $(this);
        _Tree.find("div[top=top]").each(function () {
            var obj = $(this);

            if (obj.data("node").attr("isfolder") != 1) {

                var img = obj.find("img:first");
                var ico = img.next();
                var cbk = null;
                if (ico.attr("type") == "checkbox") {
                    cbk = ico;
                    ico = ico.next();
                }
                img.attr("src", options.picurl + "ico-Expandhover.gif");
                ico.attr("src", options.picurl + "ico-folderoper.gif");

                obj.css("overflow", "auto");
                obj.find("div[lv=" + (parseInt(obj.attr("lv")) + 1) + "]").show();

                if (options.nowselected != null) {
                    if (options.nowselected.offset().top < options.outHtmlsInit.offset().top || options.nowselected.offset().top + options.nowselected.height() > options.outHtmlsInit.offset().top + options.outHtmlsInit.height()) {
                        options.selectedhighlight.hide();
                    } else {
                        options.selectedhighlight.css("top", options.nowselected.offset().top - _Tree.find(".tree:first").offset().top + _Tree.find(".tree:first").position().top).show(); //.css("width", options.outHtmlsInit.width() + _Tree.configurePYWidth()).show();
                    }
                }
                if (options.highlight != null) options.highlight.hide();
            }
        });
        return true;
    }

    $.fn.SelectTree = function (options) {

        //初始化必须值
        options = $.extend({
            startXml: "",                    //初始化加载用地址
            asynUrl: "",                     //二次加载用地址
            target: this,                    //实现目标（可以不是本身）
            cache: false,                    //数据源地址是否需要缓存（时间戳）
            dataXml: null,                   //Xml数据
            defaultShowItemLv: 1,            //初始展示到第几级
            treeContent: $("<div class='treecontent'></div>"),
            highlight: $("<div type=mh class='selected'><div class='treeleft'></div><div class='treeright'></div></div>"),
            selectedhighlight: $("<div type=sh class='selected'><div class='treeleft'></div><div class='treeright'></div></div>"),
            outLine: null,
            needoutLine: true,
            outHtmlsInit: $("<div class='tree'></div>"),       //Tree初始容器
            outHtmls: $("<div style='width:500px;'></div>"),       //Tree初始容器
            ImgShowHide: 0,                   //状态参数
            ImgShowHideAction: 0,                   //状态参数
            picurl: '../../Content/images/tree/',
            defaultMsg: '数据读取错误',
            nowmouseover: null,
            nowselected: null,
            hasCheckBox: false,
            ReferenceDiv: null,
            behandHtml: "",
            loadIco: '<div style="padding-left:6px;clear:both;"><img src="/Content/images/icon/loading.gif" />&nbsp;加载中...</div>',
            _beforePrint: function () { return; },                           //数据加载后，构成结构前
            _afterPrint: function (obj, type) { return; },                            //构成结构后，输出前
            _afterPrintOnce: function (obj) { return; },
            _onCheckBoxClick: function (obj, node) { return; },
            _onClick: function (id, name, obj, node) { return; },                //点击事件
            _onRightClick: function (id, name, obj, node) { return; },           //点击事件
            _onMouseOver: function (id, name, obj, node) { return; },        //Mouseover
            _onMouseOut: function (id, name, obj, node) { return; }          //Mouseout
        }, options);

        //指定全局this指代对象，避免进入Jquery循环错误判断
        var _Tree = this;
        //回写options 用于重载整棵树
        $(this).data("options", options);

        //装载XML并打开加载循环
        this.setUpTree = function () {
            var root = $(options.dataXml).find("root");
            if (root == null) {
                _Tree.onError("XML信息为空");
                return false;
            }
            this.putOutTreeXml(root, 1);
        }

        //读取XML并进行初始化处理
        this.loadXMLData = function () {

            var mask = $('<div style="margin-top:25%; text-align:center; height:86px"><table height="95"><tr><td width="25"><img src="/Content/images/icon/loading.gif"/></td><td style="font-size:14px; font-weight:bold;">正在加载...</td></tr></table></div>');

            $(_Tree).append(mask);
            //地址初始化处理
            var url = this.urlDelcache(options.startXml);
            $.ajax({
                url: url,
                type: 'GET',
                dataType: 'xml',
                timeout: 20000,
                error: function (xml) {
                    _Tree.onError(options.defaultMsg);
                },
                success: function (xml) {
                    $.extend(options, { dataXml: xml });
                    mask.remove();
                    _Tree.setUpTree();
                }
            });



            return true;
        }

        this.reloadXMLData = function (obj) {
            if (options.asynUrl == "" || options.asynUrl == null) return false;
            var id = obj.data("node").attr("id");
            var lv = parseInt(obj.data("lv")) + 1;
            var _parent = obj.data("id");
            var tempIco = $(options.loadIco);
            obj.append(tempIco);
            $.ajax({
                url: options.asynUrl,
                data: "id=" + id + "&lv=" + lv + "&time=" + Math.random(),
                type: 'GET',
                dataType: 'xml',
                timeout: 20000,
                error: function (xml) {
                    _Tree.onError("加载子节点数据读取错误");
                },
                success: function (xml) {
                    tempIco.remove();
                    $.extend(options, { dataXml: xml });
                    var root = $(options.dataXml).find("root");
                    root.attr("id", id);
                    _Tree.putOutTreeXml(root, lv++, obj, _parent, obj);
                }
            });
        }

        //用于循环读取节点内容，传入初始lv值为1，之后再每次循环中叠加
        this.putOutTreeXml = function (node, lv, parent, _parent, show) {
            if (!_parent) _parent = "0-0-0";
            if (!parent) parent = null;
            var Items = this.getChildNode(node);

            if (Items != null) {
                //输出前CallBack
                if (lv == 1) options._beforePrint();
                $(Items).each(function () {
                    //这里输出内容
                    if (typeof $(this).attr("name") != "undefined") {
                        obj = _Tree.makeHtmlProc($(this), lv, parent, _parent);
                        //循环读取子节点
                        _Tree.putOutTreeXml($(this), lv + 1, obj, obj.data("id"));
                        //每次输出后绑定CallBack
                        options._afterPrintOnce($(options.target));
                    }

                });
                if (lv == 1) {
                    //输出内容
                    options.highlight.hide();
                    options.treeContent.append(options.highlight);
                    options.selectedhighlight.hide();
                    options.treeContent.append(options.selectedhighlight);
                    options.outHtmlsInit.append(options.outHtmls);
                    options.treeContent.append(options.outHtmlsInit);
                    options.target.append(options.treeContent);
                    this.createResizeAble();
                    //配置目标图标的显示效果
                    this.configureIco();


                    //回写options 用于重载整棵树
                    $(this).data("options", options);

                    //输出后CallBack
                    options._afterPrint($(options.target));
                    if (_Tree.find(".tree").find("div").length == 1) {
                        _Tree.html('<div style="color:#a5a5a5; text-align:center; font-size:16px; font-weight:bold;padding-top:30px;">暂无数据</div>');
                    }


                }
                if (show) {
                    obj.css("overflow", "auto");
                    show.find("div[lv=" + (parseInt(show.attr("lv")) + 1) + "]").show();
                    var h = show.height();
                    options.selectedhighlight.hide();
                    show.css("height", "24px").css("overflow", "hidden").animate({ "height": h }, 500, function () {
                        show.css("overflow", "visible").css("height", "auto");
                        _Tree.moveSelection();
                    });

                }
            }
        }

        this.createResizeAble = function () {
            var CustX = null;
            var h = _Tree.height();
            if (h == 0) { h = options.outHtmlsInit.height(); }
            options.outHtmlsInit
                        .css("width", _Tree.width() - 20)
                        .css("height", h)
                        .css("overflow-x", "hidden")
                        .css("overflow-y", "auto");
            //                                    .mousedown(function() {
            //                                        $(this).css("cursor", "move");
            //                                        CustX = event.x;
            //                                    }).mouseup(function() {
            //                                        $(this).css("cursor", "text")
            //                                    }).mouseout(function() {
            //                                        $(this).css("cursor", "text")
            //                                    }).mousemove(function() {
            //                                        if (event.x > CustX && $(this).css("cursor") == "move") {
            //                                            $(this).css("width", parseInt(_Tree.width(), 10) + (event.x - CustX));
            //                                        }
            //                                    });
            options.outHtmlsInit.scroll(function () {
                _Tree.moveSelection();
            });
        }

        this.moveSelection = function () {
            try {
                if (options.nowselected != null) {
                    if (options.nowselected.offset().top < options.outHtmlsInit.offset().top || options.nowselected.offset().top + options.nowselected.height() > options.outHtmlsInit.offset().top + options.outHtmlsInit.height()) {
                        options.selectedhighlight.hide();
                    } else {
                        options.selectedhighlight.css("top", options.nowselected.offset().top - _Tree.find(".tree:first").offset().top + _Tree.find(".tree:first").position().top).css("width", options.outHtmlsInit.width() + _Tree.configurePYWidth()).show();
                    }
                }
            }
            catch (err) {

            }

            //if (options.highlight != null) options.highlight.hide();
        }

        //初始化节点目标图标的显示效果
        this.configureIco = function () {
            $(options.outHtmls).mouseover(function () {
                if (top.window.event) {
                    if (top.window.event.y < $(this).offset().top + $(this).height() - 10
                         && top.window.event.y > $(this).offset().top
                         && top.window.event.x > $(this).offset().left
                         && top.window.event.x < $(this).offset().left + $(this).width()) {
                        options.ImgShowHideAction = 1;

                        if (options.ImgShowHide == 0 || options.ImgShowHide == 100) {
                            _Tree.imgShowHide();
                        }
                    }
                }
            }).mouseout(function () {
                if (top.window.event) {
                    if (top.window.event.y > $(this).offset().top + $(this).height() - 10
                         || top.window.event.y < $(this).offset().top
                         || top.window.event.x < $(this).offset().left
                         || top.window.event.x > $(this).offset().left + $(this).width()) {
                        options.ImgShowHideAction = -1;
                        options.highlight.hide();
                        if (options.ImgShowHide == 0 || options.ImgShowHide == 100) {
                            //_Tree.imgShowHide();
                        }
                    }
                }
            });
            //$(options.outHtmls).find("img[co=true]").css("filter", "alpha(opacity=" + options.ImgShowHide + ")");

        }

        //图标效果附加事件
        this.imgShowHide = function () {
            if (options.ImgShowHideAction == 0
            || (options.ImgShowHideAction == 1 && options.ImgShowHide == 100)
            || (options.ImgShowHideAction == -1 && options.ImgShowHide == 0)) return false;

            if (options.ImgShowHide < 0) { options.ImgShowHide = 0; return false; }
            else if (options.ImgShowHide > 100) { options.ImgShowHide = 100; return false; }

            if (options.ImgShowHideAction == 1) {
                options.ImgShowHide = parseInt(options.ImgShowHide, 10) + 10;
            } else {
                options.ImgShowHide = parseInt(options.ImgShowHide, 10) - 2;
            }
            $(options.outHtmls).find("img[co=true]").css("filter", "alpha(opacity=" + options.ImgShowHide + ")");
            setTimeout(function () { _Tree.imgShowHide() }, 1);
        }

        //输出节点内容
        this.makeHtmlProc = function (node, lv, parent, _parent) {
            node = $(node);
            var obj, name = node.attr("name");
            var objid = lv + "-" + node.attr("id") + "-" + Math.round(new Date().getTime() / 1000);
            var imgtype = "<img src=" + options.picurl + "ico-folder.gif>";
            var imgtypeopen = "<img src=" + options.picurl + "ico-folderoper.gif>";
            if ($(node).attr("isfolder") == 1) {
                imgtype = "<img src=" + options.picurl + "ico-file.gif>";
                imgtypeopen = "<img src=" + options.picurl + "ico-file.gif>";
            }

            var StyleClass = "level0";
            if (lv != 1) { StyleClass = "levelx"; }
            var ckb = "";
            if (options.hasCheckBox) { ckb = "<input value='" + node.attr("id") + "' needChecked=needChecked type=checkbox style='float:left;'>"; }

            if (node.attr("param") && node.attr("param").indexOf("[") != -1 && node.attr("param").indexOf("]") != -1) {
                //折叠后显示总和 By 小E
                if (lv < options.defaultShowItemLv) {
                    //层级高于额定初始展示层级，输出显示状态，节点处于展开状态
                    obj = $("<div isfolder=" + node.attr("isfolder") + " top=top class=" + StyleClass + " id=div" + node.attr("id") + " lv=" + lv + "><img co=true type=Expand src='" + options.picurl + "ico-Expand.gif'>" + ckb + imgtypeopen + "<div class='name' expand_name='" + node.attr("name") + "' collapse_name='" + node.attr("param") + "' state='expand'>" + node.attr("name") + "</div>" + options.behandHtml + "</div>");
                } else if (lv == options.defaultShowItemLv) {
                    //层级等于额定初始展示层级，输出显示状态，节点处于闭合状态
                    obj = $("<div isfolder=" + node.attr("isfolder") + " top=top class=" + StyleClass + " id=div" + node.attr("id") + " lv=" + lv + "><img co=true type=Collapse src='" + options.picurl + "ico-Collapse.gif'>" + ckb + imgtype + "<div class='name' expand_name='" + node.attr("name") + "' collapse_name='" + node.attr("param") + "' state='collapse'>" + node.attr("param") + "</div>" + options.behandHtml + "</div>");
                } else {
                    //层级低于额定初始展示层级，输出隐藏状态，节点处于闭合状态
                    obj = $("<div isfolder=" + node.attr("isfolder") + " top=top class=" + StyleClass + " style='display:none;' id=div" + node.attr("id") + " lv=" + lv + "><img co=true type=Collapse src='" + options.picurl + "ico-Collapse.gif'>" + ckb + imgtype + "<div class='name' expand_name='" + node.attr("name") + "' collapse_name='" + node.attr("param") + "' state='collapse'>" + node.attr("param") + "</div>" + options.behandHtml + "</div>");
                }
            } else {
                if (lv < options.defaultShowItemLv) {
                    //层级高于额定初始展示层级，输出显示状态，节点处于展开状态
                    obj = $("<div isfolder=" + node.attr("isfolder") + " top=top class=" + StyleClass + " id=div" + node.attr("id") + " lv=" + lv + "><img co=true type=Expand src='" + options.picurl + "ico-Expand.gif'>" + ckb + imgtypeopen + "<div class='name'>" + node.attr("name") + "</div>" + options.behandHtml + "</div>");
                } else if (lv == options.defaultShowItemLv) {
                    //层级等于额定初始展示层级，输出显示状态，节点处于闭合状态
                    obj = $("<div isfolder=" + node.attr("isfolder") + " top=top class=" + StyleClass + " id=div" + node.attr("id") + " lv=" + lv + "><img co=true type=Collapse src='" + options.picurl + "ico-Collapse.gif'>" + ckb + imgtype + "<div class='name'>" + node.attr("name") + "</div>" + options.behandHtml + "</div>");
                } else {
                    //层级低于额定初始展示层级，输出隐藏状态，节点处于闭合状态
                    obj = $("<div isfolder=" + node.attr("isfolder") + " top=top class=" + StyleClass + " style='display:none;' id=div" + node.attr("id") + " lv=" + lv + "><img co=true type=Collapse src='" + options.picurl + "ico-Collapse.gif'>" + ckb + imgtype + "<div class='name'>" + node.attr("name") + "</div>" + options.behandHtml + "</div>");
                }
            }

            var nameLine = node.attr("name");
            var idLine = node.attr("id");
            if (parent != null) {
                nameLine = parent.data("nameLine") + "." + nameLine;
                idLine = parent.data("idLine") + "." + idLine;
            }
            obj.data("nameLine", nameLine);
            obj.data("idLine", idLine);
            obj.data("id", objid);
            obj.data("parent", _parent);
            obj.data("lv", lv);
            obj.data("node", node);
            this.configureFunction(obj, node);
            if (parent != null) {
                parent.append(obj);
            } else {
                options.outHtmls.append(obj);
            }
            return obj;
        }

        this.configurePYWidth = function () {
            if (options.outHtmlsInit.height() > options.outHtmlsInit.find("div:first").height()) {
                return 20;
            } else {
                return 3;
            }
        }

        this.configureFunction = function (obj, node) {
            //为每个Obj绑定callback事件,并将node属性赋予到obj上
            obj
               .mouseover(function () {
                   options._onMouseOver(node.attr("id"), node.attr("name"), this, node);
               })
               .mouseout(function () {
                   options._onMouseOut(node.attr("id"), node.attr("name"), this, node);
               });
            obj.find("input[type=checkbox]").click(function () {
                options._onCheckBoxClick($(this), node);
            });
            //为名字显示区域绑定事件
            obj.find("div:first")
            .mousedown(function (e) {
                options.selectedhighlight
                       .css("top", obj.offset().top - _Tree.find(".tree:first").offset().top + _Tree.find(".tree:first").position().top)
                       .css("width", options.outHtmlsInit.width() + _Tree.configurePYWidth())
                       .show();


                options.nowselected = obj;
                options.selectedhighlight.attr("class", "selectedmouseover");
                options.highlight.hide();


                var evt = e;
                $(this).mouseup(function () {
                    $(this).unbind('mouseup');
                    if (evt.button == 0) {
                        //鼠标右键
                        options._onRightClick(node.attr("id"), node.attr("name"), obj, node);
                        return;
                    }
                });

                $(this)[0].oncontextmenu = function () {
                    return false;
                }
            }).click(function (e) {
                var name = obj.find("div.name:first");
                if (name.attr("expand_name")) {
                    var name_split_left = name.attr("expand_name").split("["),
                    name_split_right = name_split_left[1].split("]"),
                    name_split = name_split_right[0];
                    if (name.attr("state") == "collapse" && name_split <= 0) {
                        _Tree.ExpandObj(obj);
                        return false;
                    }
                }

                options.selectedhighlight
                       .css("top", obj.offset().top - _Tree.find(".tree:first").offset().top + _Tree.find(".tree:first").position().top)
                       .css("width", options.outHtmlsInit.width() + _Tree.configurePYWidth())
                       .show();
                options.nowselected = obj;
                options.selectedhighlight.attr("class", "selectedmouseover");
                options.highlight.hide();

                options._onClick(node.attr("id"), node.attr("name"), obj, node);
            }).mouseover(function () {
                //参照层设置，如果需要参考外部层的滚动条高度时，需要增加此参数
                var ReferencePix = 0;
                if (options.ReferenceDiv != null) { ReferencePix = $(options.ReferenceDiv).scrollTop(); }

                if (_Tree.offset().top - obj.offset().top - _Tree.find(".tree:first").offset().top + _Tree.find(".tree:first").position().top + ReferencePix <= 0 &&
                obj.offset().top - _Tree.find(".tree:first").offset().top + _Tree.find(".tree:first").position().top + ReferencePix + 14 <= _Tree.height() + _Tree.find(".tree:first").offset().top) {

                    options.highlight
                       .css("top", obj.offset().top - _Tree.find(".tree:first").offset().top + _Tree.find(".tree:first").position().top + ReferencePix)
                       .css("width", options.outHtmlsInit.width() + _Tree.configurePYWidth())
                       .show();
                }
                //behandHtmlbehandHtmlbehandHtmlbehandHtmlbehandHtmlbehandHtmlbehandHtmlbehandHtmlbehandHtmlbehandHtmlbehandHtml
                _Tree.find("img[name=behandHtml]").hide();
                _Tree.find("img[name=behandHtml2]").hide();
                if (options.behandHtml != "") {
                    obj.find("img[name=behandHtml]:first").show(); obj.find("img[name=behandHtml2]:first").show();
                    obj.find("img[name=behandHtml]").attr("divid", obj.attr("id").replace("div", ""));
                    obj.find("img[name=behandHtml2]").attr("divid", obj.attr("id").replace("div", ""));

                }

                options.nowmouseover = obj;
                if (obj.find("div.name").offset().left + obj.find("div.name").width() > _Tree.offset().left + _Tree.width() - 40 && options.needoutLine == true) {
                    $("div[selectTreeTip=tip]").remove();
                    options.outLine = $("<div selectTreeTip=tip style='position:absolute;padding-left:6px;padding-top:3px;padding-bottom:2px;cursor:hand;border:1px solid #000000;background-color:#fffff7;'>" + obj.find("div.name").html() + "</div>");
                    if (options.behandHtml != "") {
                        options.outLine.append("&nbsp;");
                        if (obj.find("img[name=behandHtml]:first"));
                        {
                            var temp = obj.find("img[name=behandHtml]:first").clone();
                            options.outLine.append(temp);
                        }
                        if (obj.find("img[name=behandHtml2]:first"));
                        {
                            var temp = obj.find("img[name=behandHtml2]:first").clone();
                            options.outLine.append(temp);
                        }

                        options.outLine.append("&nbsp;")
                    .find("img[name=behandHtml]:first").show()
                    .find("img[name=behandHtml2]:first").show();
                    }
                    $(top.document.body).append(options.outLine);
                    options.outLine.css("left", obj.find("div.name").offset().left);
                    options.outLine.css("top", obj.find("div.name").offset().top);
                    options.outLine.click(function () {
                        var name = obj.find("div.name:first");
                        if (name.attr("expand_name")) {
                            var name_split_left = name.attr("expand_name").split("["),
                                name_split_right = name_split_left[1].split("]"),
                                name_split = name_split_right[0];
                            if (name.attr("state") == "collapse" && name_split <= 0) {
                                _Tree.ExpandObj(obj);
                                return false;
                            }
                        }

                        options.selectedhighlight
                       .css("top", obj.offset().top - _Tree.find(".tree:first").offset().top + _Tree.find(".tree:first").position().top)
                       .css("width", options.outHtmlsInit.width() + _Tree.configurePYWidth())
                       .show();

                        options.nowselected = obj;
                        options.selectedhighlight.attr("class", "selectedmouseover");
                        options.highlight.hide();


                        options._onClick(node.attr("id"), node.attr("name"), obj, node);
                    });
                    options.outLine.mouseleave(function () { options.outLine.hide(); });
                } else {
                    if (options.outLine != null) options.outLine.hide();
                }
                if (options.nowmouseover != options.nowselected && options.nowselected != null) {
                    $(options.selectedhighlight).attr("class", "selected");
                }

                if (options.nowmouseover != options.nowselected) {
                    $(options.highlight).attr("class", "unselectactive");
                } else {
                    options.highlight.hide();
                    $(options.selectedhighlight).attr("class", "selectedmouseover");
                }

            });

            //为图片区域绑定事件

            var img = obj.find("img[co=true]:first");
            var ico = img.next();
            var cbk = null;
            if (ico.attr("type") == "checkbox") {
                cbk = ico;
                ico = ico.next();
            }
            if (img) {

                if (ico.attr("src").indexOf("file") != -1) {
                    img.css("cursor", "text").css("visibility", "hidden");
                    ico.css("cursor", "text");
                    img.attr("src", options.picurl + "ico-Expand.gif");
                } else {
                    img.css("cursor", "hand");
                    ico.css("cursor", "hand");

                    img.mouseover(function () {
                        if (img.attr("src").indexOf("ico-Expand") != -1) {
                            $(img).attr("src", options.picurl + "ico-Expandhover.gif");
                        } else {
                            $(img).attr("src", options.picurl + "ico-Collapsehover.gif");
                        }
                        //参照层设置，如果需要参考外部层的滚动条高度时，需要增加此参数
                        var ReferencePix = 0;
                        if (options.ReferenceDiv != null) { ReferencePix = $(options.ReferenceDiv).scrollTop(); }

                        options.highlight
                       .css("top", obj.offset().top - _Tree.find(".tree:first").offset().top + _Tree.find(".tree:first").position().top + ReferencePix)
                       .css("width", options.outHtmlsInit.width() + _Tree.configurePYWidth())
                       .show();

                    }).mouseout(function () {
                        if (img.attr("src").indexOf("ico-Expand") != -1) {
                            $(img).attr("src", options.picurl + "ico-Expand.gif");
                        } else {
                            $(img).attr("src", options.picurl + "ico-Collapse.gif");
                        }
                    }).click(function () {
                        if (img.attr("src").indexOf("ico-Expand") != -1) {
                            _Tree.CollapseObj(obj);
                        } else {
                            _Tree.ExpandObj(obj);
                        }
                    });

                    ico.mouseover(function () {
                        img.mouseover();
                    }).mouseout(function () {
                        img.mouseout();
                    }).click(function () {
                        img.click();
                    });

                    if (cbk != null) {
                        cbk.mouseover(function () {
                            img.mouseover();
                        }).mouseout(function () {
                            img.mouseout();
                        });
                    }
                }
            }
        }

        this.CollapseObj = function (obj) {
            options.selectedhighlight.hide();
            var h = obj.find("div:first").height();
            var img = obj.find("img:first");
            var ico = img.next();
            var cbk = null;
            var name = obj.find("div.name:first");
            if (ico.attr("type") == "checkbox") {
                cbk = ico;
                ico = ico.next();
            }
            img.attr("src", options.picurl + "ico-Collapsehover.gif");
            ico.attr("src", options.picurl + "ico-folder.gif");
            obj.find("div[lv=" + (parseInt(obj.attr("lv")) + 1) + "]").fadeOut(500);
            obj.animate({ "height": h }, 500, function () {
                obj.css("overflow", "visible").css("height", "auto")
                _Tree.moveSelection();
                if (name.attr("collapse_name")) {
                    name.html(name.attr("collapse_name"));
                }
            });
            name.attr("state", "collapse");
        }

        this.ExpandObj = function (obj) {
            if (obj.data("node").attr("isfolder") == 1) { return false; }
            options.selectedhighlight.hide();
            var img = obj.find("img:first");
            var ico = img.next();
            var cbk = null;
            var name = obj.find("div.name:first");
            if (ico.attr("type") == "checkbox") {
                cbk = ico;
                ico = ico.next();
            }
            img.attr("src", options.picurl + "ico-Expandhover.gif");
            ico.attr("src", options.picurl + "ico-folderoper.gif");
            if (obj.find("div[lv=" + (parseInt(obj.attr("lv")) + 1) + "]").length == 0) {
                _Tree.reloadXMLData(obj);
            } else {
                obj.css("overflow", "auto");
                obj.find("div[lv=" + (parseInt(obj.attr("lv")) + 1) + "]").show();
                var h = obj.find("div[lv]:visible").length * 24;
                obj.css("height", "24px").css("overflow", "hidden").animate({ "height": h }, 500, function () {
                    obj.css("overflow", "visible").css("height", "auto")
                    _Tree.moveSelection();
                    if (name.attr("expand_name")) {
                        name.html(name.attr("expand_name"));
                    }
                });
                name.attr("state", "expand");
            }

        }


        //构建层级偏移
        this.makeLeverPadding = function (lv) {
            //return (lv - 1) * 6;
            return 6;
        }

        //获取下一级子节点并返回
        this.getChildNode = function (node) {
            if (node[0].childNodes) {
                return node[0].childNodes;
            } else {
                return null;
            }
        }

        //URL时间戳编码
        this.urlDelcache = function (url) {
            //如果cache被设置为True则不需要进行地址加时间戳操作
            if (options.cache == false) {
                if (url.indexOf("?") >= 0) {
                    url = url + "&r=" + Math.random();
                } else {
                    url = url + "?r=" + Math.random();
                }
            }
            return url;
        }

        //错误&Debug处理中心
        this.onError = function (msg) {
            this.reportError(msg);
        }

        //错误回写
        this.reportError = function (msg) {
            alert(msg);
        }

        this.init = function () {
            this.loadXMLData();
            setInterval(function () { _Tree.moveSelection(); }, 500);
        }

        this.init();
    }
})(jQuery);