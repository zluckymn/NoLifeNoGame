(function($) {
    $.Perc = function() {
        var n = 0, dt = [], opt = {}, hasInit = false;
        var dcache = function(dlen, dcell) {
            if (dt.length > dlen) {
                dt.splice(dlen, dt.length - dlen)
            }
            dt.push(dcell)
        };
        var ani = function(l) {
            opt.olCards.stop().animate({ left: l }, { 'duration': "slow", 'complete': function() { showDots(l) } });
            opt.olTitle.stop().animate({ left: l }, "slow");
        };
        var showDots = function(_z) {
            var bl = opt.olCards.children().length;
            if (bl > opt.iCds) {
                var mn = Math.floor(Math.abs(_z) / (opt.valListWidth + opt.valAdjustWidth)), tmp = "";
                for (var i = 0; i < mn; i++) {
                    tmp += '<li></li>';
                }
                opt.dotL.html(tmp); tmp = "";
                for (var j = 0; j < bl - mn - opt.iCds; j++) {
                    tmp += '<li></li>';
                }
                opt.dotR.html(tmp);
            }
        };
        var showPath = function(dlen, SelTxt, Last) {
            if (dt.length >= dlen) {
                var M = opt.olSelectPath.children();
                for (var J = M.length - 1; dlen <= J; J--) {
                    $(M[J]).remove();
                }
            }
            var N = $("<a href=\"javascript:;\">" + SelTxt + "</a>");
            var I = $("<li/>");
            if (0 === dlen) {
                I.addClass("root");
            }
            I.append(N);
            opt.olSelectPath.append(I);
            if (1 !== Last) {
                var I1 = $("<li/>").html("...");
                opt.olSelectPath.append(I1)
            }
            N.click(function() {
                if (n > 0 && dlen < opt.iCds) {
                    ani(-3);
                    n = 0;
                    if (0 === n) {
                        opt.linkPrev.addClass("hfix")
                    }
                    opt.linkNext.removeClass("hfix")
                } else {
                    if (dlen - opt.iCds + 1 !== n && dlen > opt.iCds - 1) {
                        var P = (dlen - opt.iCds + 1) * opt.valListWidth * -1 - (dlen - opt.iCds + 2) * opt.valAdjustWidth;
                        ani(P);
                        n = (dlen - opt.iCds + 1);
                        opt.linkPrev.removeClass("hfix");
                        if (opt.olCards.children().length - opt.iCds === n) {
                            opt.linkNext.addClass("hfix")
                        } else {
                            opt.linkNext.removeClass("hfix")
                        }
                    }
                }
            });
        };
        var _cSearchBox = function(dsLen, ul, pLi) { // 创建搜索框
            if (pLi.find("div.quick-find")[0]) return;
            var tip = "输入名称查找",
				div = $("<div/>").addClass("quick-find");
            var inpt = $("<input type=\"text\" />").addClass("text").val(tip);
            if (dsLen > 11) inpt.addClass("short");
            div.append(inpt);
            pLi.prepend(div);
            inpt.focus(function() {
                if (tip === $(this).val()) {
                    $(this).val("");
                }
            }).blur(function() {
                if ("" === $(this).val()) {
                    $(this).val(tip);
                }
            });
            var G = [];
            inpt.keyup(function() {
                var kw = inpt.val().replace(/^[\s\t ]+/g, "").toLowerCase();
                while (0 !== G.length) {
                    var ab = ul.children().get(1);
                    $(ab).removeClass("sticky");
                    var W = G.length + G.shift() + 1;
                    if (ul.children().get(W)) {
                        if (W > 1) $(ab).insertBefore($(ul.children().get(W)));
                    } else {
                        ul.append($(ab));
                    }
                }
                if ("" !== kw) {
                    var V = ul.find("span");
                    for (var Z = V.length - 1; 0 <= Z; Z--) {
                        var ab = $(V[Z]).parent();
                        if (-1 !== $(V[Z]).text().toLowerCase().indexOf(kw)) {
                            G.unshift(Z);
                            $(ab).addClass("sticky");
                            $(ab).insertAfter(ul.children(":first"));
                        }
                    }
                    if (ul.children().length > 0) {
                        ul.scrollTop(0);
                    }
                }
            });
        };
        var _SearchTree = function(ul, pLi) { // 树搜索
            if (pLi.find("div.quick-find")[0]) return;
            var tip = "输入名称查找",
				div = $("<div/>").addClass("quick-find");
            var inpt = $("<input type=\"text\" />").addClass("text").val(tip), ulh = ul.height();
            inpt.addClass("short");
            div.append(inpt);
            pLi.prepend(div);
            inpt.focus(function() {
                if (tip === $(this).val()) {
                    $(this).val("");
                }
            }).blur(function() {
                if ("" === $(this).val()) {
                    $(this).val(tip);
                }
            });
            var G = [];
            inpt.keyup(function() {
                var kw = inpt.val().replace(/^[\s\t ]+/g, "").toLowerCase(), frtNd = null;
                while (0 !== G.length) {
                    var nd = ul.find("div.g_ktree").find("div:eq(" + G.shift() + ")");
                    nd.removeClass("tree-node-search");
                }
                if ("" !== kw) {
                    var bTree = ul.find("div.g_ktree"), V = bTree.find("div");
                    for (var Z = V.length - 1; 0 <= Z; Z--) {
                        var txt = $(V[Z]).find("span:last");
                        if (-1 !== txt.text().toLowerCase().indexOf(kw)) {
                            G.unshift(Z);
                            $(V[Z]).addClass("tree-node-search");
                            bTree.tree('expandTo', V[Z]);
                        }
                    }
                    if (G.length) frtNd = $(V[G[G.length - 1]]);
                    if (frtNd) {
                        YH.dom.scrollintoContainer(frtNd, ul, true, true);
                    }
                }
            });
        };
        var removeCards = function(cardlen, id, name, utype, leaf) {
            var card = opt.olCards.children(), clen = card.length, titles = opt.olTitle.children(); ;
            dcache(cardlen, { sid: id });
            showPath(cardlen, name, (utype == opt.endFlag || leaf == 1) ? 1 : 2);
            if (cardlen < clen) {
                for (var ci = clen - 1; cardlen < ci; ci--) {
                    $(card[ci]).remove();
                    $(titles[ci]).remove();
                }
            }
        };
        var multiRs = function(id, name, ckd, obj, _attrs) {
            var ul = $(".perc-container:last").find("ul"), hext = false;
            ul.children().each(function() {
                var _id = $(this).attr("sid");
                if (_id == id) {
                    if (!ckd) $(this).remove();
                    hext = true;
                    return false;
                }
            });
            if (hext) return;
            var li = $("<li/>").addClass("sli selected").attr("sid", id);
            var span = $("<span/>").addClass("lispan").text(name);
            span.prepend('<input type="checkbox" name="g_endlist" value="' + id + '" checked />');
            li.append(span);
            if (obj[0] && _attrs) {
                var attrs = _attrs.split(",");
                for (var i = 0; i < attrs.length; ++i) {
                    if (attrs[i] != 'sid') {
                        li.attr(attrs[i], obj.attr(attrs[i]));
                    }
                }
            }
            ul.append(li);
            li.click(function() {
                $(this).toggleClass("selected");
                var inpt = $(this).find("input:first"), trigbtn = true;
                if ($(this).hasClass("selected")) {
                    inpt.attr("checked", true);
                } else {
                    inpt.removeAttr("checked");
                }
                if ($(".perc-container:last").find("ul").find("input:checked").length == 0) trigbtn = false;
                opt.onSelect(id, name, $(this));
                $.PercSetup.updateSubmitBtn(trigbtn);
            });
        };
        var render = function(data, nclear, ids, utp, dtp) {
            if (!nclear) {
                dt.splice(0, dt.length);
                n = 0;
                opt.olCards.empty().css("left", "-3px"); opt.olTitle.empty();
            }
            var sel = sel || "0", cardLen = dt.length, u = utp, dty = dtp, uids = ids || "";
            var isCk = opt.multiSel ? "checkbox" : "radio";
            var pLi = $("<li/>").addClass("pnew"), ul = $("<ul/>").addClass("sul").attr({ ids: uids });
            pLi.append(ul);
            var nt = opt.titles["t" + u];
            if (u < 0) nt = opt.titles["t" + opt.defaultT[u]];
            if (dty == "tree") nt += ' [<a href="javascript:;" class="g_e">展开</a> <a href="javascript:;" class="g_c">折叠</a>]';
            var liTitle = $("<li>" + (nt || "") + "</li>");
            opt.olTitle.append(liTitle);
            if (0 === dt.length && dty != "Tree") {
                pLi.addClass("root");
            } else {
                if (dty == "normal") _cSearchBox(data.length, ul, pLi);
            }
            opt.olCards.append(pLi);
            var uu = u < 0 ? opt.defaultT[u] : u, selectid = opt.defV["dv" + uu], selObj = null;
            if (hasInit) selectid = null;
            if (dty == "normal") { // 平级
                if (opt.retObjList && $(opt.retObjList)[0]) {
                    $(opt.retObjList).unbind("change").bind("change", function() {
                        var cgv = $(this).val(), cgsx = $(this).attr("sxname");
                        if (cgv == "0" || cgv == 0 || cgv == "") {
                            cgv = ",";
                            $(opt.retObjList).find("option").each(function() {
                                cgv += $(this).val() + ",";
                            })
                        } else {
                            cgv = "," + cgv + ",";
                        }
                        ul.find("li:gt(0)").each(function() {
                            if (cgv.indexOf("," + $(this).attr(cgsx) + ",") == -1) {
                                $(this).hide();
                            } else {
                                $(this).show();
                            }
                        });
                    })
                }
                ul.append("<li class=\"title\"></li>");
                for (var I = 0; I < data.length; I++) {
                    var rs = data[I], leaf = 2; //rs.leaf ||
                    var li = $("<li/>").addClass("sli"), psb, attrs = ""; //.attr({ "sid": rs.id, "dtype": rs.dtype, "utype": rs.utype });
                    for (var pp in rs) {
                        psb = pp;
                        if (pp == 'id') {
                            psb = "sid";
                        }
                        if (typeof rs[pp] != 'object') {
                            attrs = psb + ",";
                            li.attr(psb, rs[pp]);
                        }
                    }
                    u = rs.utype; attrs = attrs.replace(/,$/i, "");
                    if (u != opt.endFlag) li.addClass("parent");
                    var span = $("<span/>").addClass("lispan").text(rs.text);
                    if (u == opt.endFlag) span.prepend('<input type="' + isCk + '" name="g_rslist" value="' + rs.id + '" />');
                    li.append(span);
                    if (selectid && typeof selectid == 'object' && opt.multiSel) {
                        $.each(selectid, function(i, n) {
                            if (n == rs.id) {
                                selObj = li;
                                return true;
                            }
                        })
                    } else if (selectid && rs.id == selectid) {
                        selObj = li;
                    }
                    if (u == opt.endFlag && opt.retObjList && $(opt.retObjList)[0]) {
                        var retObj = $(opt.retObjList), sx = retObj.attr("sxname"); // <select name="retObjId" id="retObjId" sxname="retObjId"></select>
                        var sxVal = retObj.val();
                        if (parseInt(sxVal) == 0 || sxVal == "") {
                            sxVal = ",";
                            retObj.find("option").each(function() {
                                var rtv = $(this).val();
                                if (rtv != "" && rtv != "0" && rtv != 0) sxVal += rtv + ",";
                            });
                        } else {
                            sxVal = "," + sxVal + ",";
                        }
                        if (rs[sx] && sxVal.indexOf("," + rs[sx] + ",") == -1) {
                            li.hide();
                        }
                    }
                    ul.append(li);
                    (function() { // li click
                        var obj = li, id = rs.id, name = rs.text, lf = leaf, urlt = u, tids = uids, _attrs = attrs;
                        obj.click(function() {
                            if ("undefined" !== typeof dt[cardLen] && dt[cardLen].sid == id && urlt != opt.endFlag) {
                                return false
                            }
                            var inpt = $(this).find("input:first"), ckd = false;
                            urlt = $(this).attr("utype");
                            if ((urlt == opt.endFlag || lf == 1) && opt.multiSel) {
                                $(this).toggleClass("selected");
                                if ($(this).hasClass("selected")) {
                                    inpt.attr("checked", true);
                                    ckd = true;
                                } else {
                                    inpt.removeAttr("checked");
                                }
                                if (opt.showRsBox) multiRs(id, name, ckd, obj, _attrs);
                            } else {
                                $(this).addClass("selected").siblings().removeClass("selected");
                                if (inpt.length) inpt.attr("checked", true);
                            }
                            removeCards(cardLen, id, name, urlt, lf);
                            $.PercSetup.updateSubmitBtn(urlt == opt.endFlag);
                            if (urlt == opt.endFlag && opt.multiSel && $(this).parent().find("input:checked").length == 0) {
                                $.PercSetup.updateSubmitBtn(false);
                                opt.olSelectPath.children(":last").remove();
                            }
                            if (urlt == opt.endFlag && opt.multiSel && opt.showRsBox) {
                                if ($(".perc-container:last").find("ul").find("input:checked").length == 0) $.PercSetup.updateSubmitBtn(false);
                            }
                            if (urlt != opt.endFlag && 1 !== lf) { // load data
                                var sid = $(this).attr("sid"), dtp = $(this).attr("dtype"), utp = $(this).attr("utype");
                                //tids += (tids ? "," : "") + sid;
                                tids = sid;
                                loadData(utp, dtp, true, tids);
                            }
                            if (urlt == opt.endFlag) opt.onSelect(id, name, obj);
                        });
                    })();
                    if (!hasInit && !$.isEmptyObject(opt.defV)) {
                        /*var ulsh = ul[0].scrollHeight, ulh = ul.height(), nodeT = li.offset().top - ul.offset().top, nodeH = li.height();
                        if ((nodeT + nodeH) > ulh) {
                        var c1 = nodeT - ulh;
                        ul.scrollTop(c1);
                        }*/
                        if (selObj) selObj.trigger("click");
                    }
                    selObj = null;
                }
            } else { // tree
                ul.append('<li class=\"title\"></li><div class="g_ktree" style="width:auto;height:auto"></div>');
                var buildtree = ul.find("div.g_ktree");
                buildtree.tree({
                    url: data,
                    selid: selectid,
                    onSelect: function(nd) { if (u == opt.endFlag) opt.onSelect(nd.id, nd.text, nd); },
                    onClick: function(node) {
                        buildtree.tree('toggle', node.target);
                        if ("undefined" !== typeof dt[cardLen] && dt[cardLen].sid == node.id && u != opt.endFlag) {
                            return false
                        }
                        u = node.utype;
                        removeCards(cardLen, node.id, node.text, u);
                        $.PercSetup.updateSubmitBtn(u == opt.endFlag);
                        if (u != opt.endFlag) { // load data
                            //uids += (uids ? "," : "") + node.id;
                            uids = node.id;
                            loadData(node.utype, node.dtype, true, uids);
                        }
                    },
                    onLoadSuccess: function() {
                        var t_opt = buildtree.tree('options');
                        if (t_opt.selid && t_opt.selnode) {
                            var bb = buildtree.tree('getNode', t_opt.selnode[0]);
                            t_opt.selnode.trigger("click", [bb]);
                            buildtree.tree('expandTo', bb.target, function() { YH.dom.scrollintoContainer(bb.target, ul, true, true); });
                        }
                        _SearchTree(ul, pLi);
                    }
                });
                if (liTitle.find("a").length > 1) {
                    liTitle.find("a.g_e").click(function() { buildtree.tree('expandAll'); });
                    liTitle.find("a.g_c").click(function() { buildtree.tree('collapseAll'); });
                }
            }
            if (u == opt.endFlag) {
                hasInit = true;
            }
            go_ani();
        };
        function go_ani() {
            var G = opt.olCards.children().length;
            if (G <= opt.iCds) {
                opt.dotL.html(""); opt.dotR.html("");
                opt.linkPrev.addClass("hfix");
                opt.linkNext.addClass("hfix");
                ani(-3);
            }
            if (G > opt.iCds) {
                var goleft = (G - opt.iCds) * (opt.valListWidth + opt.valAdjustWidth) * -1 - opt.valAdjustWidth;
                ani(goleft);
                n = G - opt.iCds;
                opt.linkPrev.removeClass("hfix");
                opt.linkNext.addClass("hfix");
            }
        }
        var loadData = function(utpye, dtp, nclear, ids) {
            var url = "", u = utpye, dty = dtp.toLowerCase();
            switch (u) {
                case -1:
                    url = opt.apiUrl1; break;
                case -2:
                    url = opt.apiUrl2; break;
                default:
                    url = opt.urls["url" + u];
                    if (/(\.|\/)/i.test(url) == false) {
                        url = opt[url] || opt.urls[url];
                    }
            }
            if (u < 0) u = opt.defaultT[u];
            url = url.replace(/__id__/i, ids).replace(/__type__/i, u);
            if (dty == "normal") { // 平级
                $.ajax({
                    url: url, cache: false,
                    success: function(data) {
                        var rs = eval(data);
                        render(rs, nclear, ids, u, dty);
                    }
                });
            } else { // tree
                render(url, nclear, ids, u, dty);
            }
        };
        return {
            init: function() {
                opt = $.PercSetup.getConfig();
                if ("undefined" === typeof opt) {
                    return false
                }
                opt.linkPrev.click(function() {
                    var H = opt.valListWidth + opt.valAdjustWidth;
                    H = (n - 1) * H * -1 - opt.valAdjustWidth;
                    ani(H);
                    n--;
                    if (0 === n) {
                        opt.linkPrev.addClass("hfix")
                    }
                    opt.linkNext.removeClass("hfix")
                });
                opt.linkNext.click(function() {
                    var G = opt.olCards.children().length - opt.iCds;
                    var I = (opt.valListWidth + opt.valAdjustWidth) * (n + 1) * -1 - opt.valAdjustWidth;
                    ani(I);
                    n++;
                    opt.linkPrev.removeClass("hfix");
                    if (G === n) {
                        opt.linkNext.addClass("hfix")
                    }
                });
                loadData(-1, opt.stct1, false, "");
            },
            _ani: function() {
                n = 1;
                opt.linkPrev.trigger("click");
            },
            reChoose: function() {
                loadData(-2, opt.stct2, false, "");
            }
        }
    } ();
    // 初始
    $.PercSetup = function() {
        var opt = {};
        var cbox = function() {
            var w = (opt.valListWidth + opt.valAdjustWidth) * opt.iCds - opt.valAdjustWidth - 2, wb = w, sx = "";
            var tpl = [
				'<table cellpadding="0" cellspacing="0"><tr><td><div id="perc" style="position:relative;width:' + (w + 42) + 'px">',
					'<div class="bd">',
						'<div class="perc-container" style="width:' + w + 'px"><ol class="tt" id="g_title"></ol><ol id="g_Perclist"></ol></div>',
						opt.showPN ? '<span class="prev"><span>无上一级</span></span><span class="next"><span>无下一级</span></span><a href="javascript:;" hidefocus="true" class="prev hfix" title="上一级" id="g_Prev"><span>上一级</span></a><a href="javascript:;" class="next hfix" title="下一级" hidefocus="true" id="g_Next"><span>下一级</span></a>' : '',
						opt.showPath ? '<div class="g-extra"><dl><div><dt>您当前选择的是：</dt><dd><ol class="g-path" id="g_Path"></ol></dd></div></dl></div>' : '',
						'<div class="dotL"><ul></ul></div><div class="dotR"><ul></ul></div>',
					'</div>',
				'</div></td>',
				opt.showRsBox ? '<td valign="top"><div class="perc-container" style="margin-top:5px;margin-left:10px;width:' + (opt.valListWidth) + 'px"><ol class="tt"><li>' + opt.titleRs + '</li></ol><ol class="rs"><li class="pnew"><ul class="sul"></ul></li></ol></div></td>' : "",
				'</tr></table>'
			].join("");
            if (opt.showRsBox) wb += opt.valListWidth + 31;
            var bx = box(tpl, { boxid: "perc", title: opt.title, width: wb + 50,
                submit_cb: function(o) {
                    var rsc = opt.olCards.children(":last").find("ul:first"), response = {};
                    if (opt.retObjList && $(opt.retObjList)[0]) {
                        sx = $(opt.retObjList).attr("sxname");
                    }
                    if (rsc.find("div.g_ktree")[0]) { // tree
                        var node = rsc.find("div.g_ktree").tree('getSelected');
                        response[node.id] = node.text;
                    } else {
                        if (opt.showRsBox) rsc = $(".perc-container:last").find("ul");
                        rsc.find("input:checked").each(function() {
                            var v = $(this).val(), txt = $(this).parent().text(), a = "";
                            if (sx) {
                                a = ":" + $(this).parent().parent().attr(sx);
                            }
                            response[v + a] = txt;
                        });
                    }
                    return opt.callback(o, response);
                }
            });
            return bx;
        };
        return {
            init: function(cfg) {
                opt = $.extend({}, {
                    title: "选择要引用的成果",
                    showPath: true,
                    showPN: true,
                    iCds: 2,
                    valListWidth: 257,
                    valAdjustWidth: 3,
                    showRsBox: false, // 是否允许保存多选结果
                    titleRs: "已选择成果",
                    apiUrl1: "tree_data.json", // tree_data.json  utype:-1
                    stct1: "tree",
                    apiUrl2: "apiUrl2.html", // utype:-2
                    stct2: "normal",
                    defaultT: { "-1": "4", "-2": "0" },
                    titles: { "t0": "聚合", "t1": "选择工程", "t2": "选择项目", "t3": "选择分项", "t4": "成果目录 [<a href=\"javascript:;\" id=\"cproj\">选择项目</a>]", "t5": "成果列表" },
                    urls: { url1: "url1.html", url2: "url2.html", url3: "url3.html", url4: "tree_data.json", url5: "url5.html" },
                    endFlag: 5,
                    // utype：【自定义：0】，工程：1，项目：2，分项：3，成果目录：4，成果：5   dType: "Normal"  "Tree"
                    defV: {}, // 默认值选中 {"dv0":2, "dv1":3, "dv2":3, "dv3":3, "dv4":4, "dv5":[2,3,12]}
                    multiSel: false, // 对结果是否允许多选
                    callback: function(_b, _r) { },
                    onSelect: function(_e, _d, _o) {
                    },
                    retObjList: null
                }, cfg || {});
                var cc = cbox();
                $.extend(opt, {
                    olCards: $("#g_Perclist"),
                    olTitle: $("#g_title"),
                    linkPrev: $("#g_Prev"),
                    linkNext: $("#g_Next"),
                    olSelectPath: $("#g_Path"),
                    submitBtn: $("input[id^='submit_']", cc.fbox),
                    dotL: $(".dotL>ul"),
                    dotR: $(".dotR>ul")
                });
                opt.submitBtn.attr("disabled", true);
                $.Perc.init();
                if ($("#cproj")[0]) {
                    $("#cproj").live("click", function() {
                        if (opt.olCards.children().length > 2) { $.Perc._ani(); return; }
                        $.Perc.reChoose();
                    });
                }
            },
            getConfig: function() {
                return opt
            },
            updateSubmitBtn: function(r) {
                r ? opt.submitBtn.removeAttr("disabled") : opt.submitBtn.attr("disabled", true);
            }
        }
    } ()
})(jQuery);