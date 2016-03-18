/**
* foldertable.js 2011/6/10 by Qingbao.Zhao
*/
var isneedUpNewVer = false, isjdneedUpNewVer = false;
if (typeof isNeedUpNewVer != 'undefined') {
    isneedUpNewVer = true;
}
if (typeof isJdNeedUpNewVer != 'undefined') {
    isjdneedUpNewVer = true;
}
var fldtable = (function() {
var downDisable = false;
    if ($("#Candownload").length > 0) {
        downDisable = true;
    }

    var tpl = ['<div class="contain"><table width="940" id="optable" style="display:none;">',
		'<tr>',
        '<td height="30"><a class="zPushBtn" href="javascript:;" id="showtztree" style="display:none;"><img src="/Content/Images/zh-cn/task/icon042a1.gif" /><b>树形目录</b></a><a class="zPushBtn" href="javascript:;" id="tzckall" data-ck="0"><img src="/Content/Images/zh-cn/task/icon042a1.gif" /><b>全选</b></a></td>',
        '<td><a class="zPushBtn" href="javascript:;" id="tzbatchDel" style="float:right"><img src="/Content/Images/zh-cn/task/icon003a3.gif" /><b>批量删除</b></a>',
        '<a class="zPushBtn" href="javascript:;" id="tzbatchToF" style="float:right"><img src="/Content/Images/zh-cn/task/icon003a7.gif" /><b>批量转正式</b></a></td>',
		'</tr>',
		'</table>',
		'<table width="940" bgcolor="#FFFFFF" class="tableborder">',
		'<tr bgColor=#f5f5f5>',
		'<td colspan="2" align="center" width="272">目录</td>',
		'<td width="410" align="center">图纸名称</td>',
		'<td width="60" align="center">版本</td>',
		'<td width="100" align="center">上次更新</td>',
		'<td width="70" align="center">操作</td>',
		'</tr></table>', '<div style="overflow:auto; overflow-x:hidden; height:400px;" id="ft_container">',
		'<table width="940" bgcolor="#FFFFFF" class="tableborder" style="border-top:0px none;height:auto;">',
		'<tr><td align="center">loading...</td></tr>',
		'</table></div>'], opts,
    _trtpl = '<tr id="tr{refid}"><td width="128" gid="{gid1}" class="fontblue"{rowspan} style="background:#eee;cursor:pointer;{styler}">{folder1}</td><td width="129" class="fontpurple"{rowspans} style="background:#f4f3f3;cursor:pointer;{stylers}" gid="{gid2}">{folder2}</td><td width="410"><input type="checkbox" name="ckb"{showhd} value="{refid}" /><a class="blue" onclick="ReadOnlineCustom(\'{fileId}\',\'{ver}\',0);" href="javascript:void(0);">{tzname}</a> {upnewv}</td><td width="60"><a class="blue4" onclick="{fversion}({refid}, this);" href="javascript:;">V{ver}</a></td><td width="100">{update}</td><td width="70">{opera}</td></tr>', hoverbg = '#d0e5f5', selbg = '#fcf1a9';
    function __initbox(a) {
        var doc = YH.isStrict ? document.documentElement : document.body, projId = opts.projId || 0, title = "查看施工图纸";
        if (a) title = $(a).text();
        box(tpl.join(""), { boxid: "fldtable", title: title, cls: "shadow-container7", modal: true, submit_BtnName: '下载该图纸包', cancel_BtnName: '关 闭', no_submit: downDisable,
            onOpen: function(o) {
                var ct = o.db.find("#ft_container"), dt = ct.find("table:first"), wsbar = YH.dom.getScrollbarWidth(), result;
                doc.style.overflow = 'hidden';
                $.ajax({ type: 'POST',
                    url: '/Projects/Assignment/NewPicPackageDiplay/', data: { taskId: opts.taskId, todoId: opts.todoId, secdPlanId: opts.secdPlanId, ClassStr: opts.ClassStr, docType: opts.docType, projId: projId },
                    success: function(rs) {
                        rs = rs.split("|"); result = eval("(" + rs[0] + ")");
                        if (result.success) {
                            __render(dt, rs);
                            if (dt.height() > ct.height()) {
                                ct.css("width", 940 + wsbar);
                                o.fbox.css("width", wsbar + o.fbox.width());
                            }
                        } else {
                            $.tmsg("ftb", result.errors.msg, { infotype: 2 });
                        }
                    }
                });
            },
            submit_cb: function(o) {
                DownNeedFileListDown("directory=" + projId + "=" + opts.ClassStr, projId);
            },
            onDestroy: function(o) {
                doc.style.overflow = 'auto';
            }
        });
    }
    function __render(container, data) {
        var downDisable = false;
        if ($("#Candownload").length > 0) {
            downDisable = true;
        }
        var cc = [], strhide = "display:none;", operas = "", showhd = "", idx = 0, dt, i, gp, sgp, item, j, sitem, tmpData = {}, f1, f2, rsp, rsps, gid1 = 0, gid2 = 0, styler, stylers, sname, pos, rscount = 0, fv = "";
        if (isjdneedUpNewVer) {
            operas = '<img src="/Content/Images/zh-cn/General/ico-up.png" onclick="inFolderUploadnewVer(\'{fileId}\', this, true);" alt="上传新版本" style="cursor:pointer" /> ';
        }
        if (opts.vt == 0) { // 过程交付物包
            operas += '<img onclick=a3plan_jfw1.setFormal({refid}); alt="设为正式交付物" src="/Content/Images/zh-cn/task/icon003a7.gif" style="cursor:pointer;" /> <a class="green" onclick="DownLoadByIdCustom(\'{fileId}\',\'{ver}\',0);" href="javascript:void(0);">下载</a>  <img onclick="a3plan_jfw1.delProc({refid}, 1);" alt=删除 src="/Content/Images/zh-cn/task/btn-del.png" style="cursor:pointer;" />'; $("#optable").show(); fv = "a3plan_jfw.ProcVersion";
        } else if (opts.vt == 3) { // 工作页面过程交付物包
            operas += '<img style="cursor:pointer;" onclick="UploadFilesOne(\'{refid}\', \'{todoId}\', \'*\');" alt="上传" src="/Content/images/zh-cn/task/ico-up.png" /> <img onclick=a3plan_jfw1.setFormal({refid}); alt="设为正式交付物" src="/Content/Images/zh-cn/task/icon003a7.gif" style="cursor:pointer;" /> <img onclick="a3plan_jfw1.delProc({refid}, 1);" alt="删除" src="/Content/Images/zh-cn/task/btn-del.png" style="cursor:pointer;" />'; $("#optable").show(); fv = "a3plan_jfw.ProcVersion";
        } else if (opts.vt == 1 || opts.vt == 2) { // 正式交付物包
            operas += ' <a class="green" onclick="DownLoadByIdCustom(\'{fileId}\',\'{ver}\',0);" href="javascript:void(0);">下载</a> <img onclick="a3plan_jfw1.delF({refid}, 0);" alt="删除" src="/Content/Images/zh-cn/task/btn-del.png" style="cursor:pointer;" />'; showhd = ' style="display:none;"'; fv = "a3plan_jfw.FVersion";
            if (opts.vt == 2) fv = "javascript:void"; // A3AIII-1908
        } else { // 查看
            if (downDisable) {
                operas += '<a class="blue" onclick="ReadOnlineCustom(\'{fileId}\',\'{ver}\',0);" href="javascript:void(0);">查看</a> <a class="gray"  href="javascript:void(0);">下载</a> ';
            }
            else {
                operas += '<a class="blue" onclick="ReadOnlineCustom(\'{fileId}\',\'{ver}\',0);" href="javascript:void(0);">查看</a> <a class="green" onclick="DownLoadByIdCustom(\'{fileId}\',\'{ver}\',0);" href="javascript:void(0);">下载</a> ';
            }

            showhd = ' style="display:none;"'; fv = "a3plan_jfw.FVersion";
            if (opts.vt == 4) fv = "javascript:void";
        }
        if (data.length > 1 && data[1] != "") {
            data[1] = data[1].replace(/\\/g, "**");
            dt = eval("(" + data[1] + ")");
            for (i = 0; i < dt.length; ++i) {
                item = dt[i]; f1 = 0; rscount = 0;
                gp = item.group.replace(/\*\*/g, "\\"); sgp = item.subgroups; gid1 = gid2;
                $.each(sgp, function(m, n) {
                    rscount += n.files.length;
                }); rscount = rscount == 0 ? 1 : rscount;
                rsp = " rowspan=\"" + rscount + "\"";
                for (j = 0; j < sgp.length; ++j) {
                    sitem = sgp[j]; f2 = 0; rsps = " rowspan=\"" + sitem.files.length + "\""; gid2++;
                    $.each(sitem.files, function(m, n) {
                        sname = sitem.foldername.replace(/\*\*/g, "\\");
                        sname = sname.replace(gp + "\\", "");
                        if ((pos = sname.lastIndexOf("\\")) != -1) {
                            pos = pos + 1;
                            sname = '<span style="color:#af5db1;">' + sname.substr(0, pos) + '</span>' + sname.substr(pos);
                        }


                        if (gp == '') gp = "---";
                        if (sname == '') sname = "---";

                        tmpData['folder1'] = gp; tmpData['folder2'] = sname;
                        styler = ""; stylers = "";
                        if (f1 > 0) {
                            rsp = ""; styler = strhide;
                        }
                        if (f2 > 0) {
                            rsps = ""; stylers = strhide;
                        }
                        tmpData['upnewv'] = '';
                        if (isneedUpNewVer) {
                            tmpData['upnewv'] = '<img src="/Content/Images/zh-cn/General/ico-up.png" onclick="inFolderUploadnewVer(' + n.fileId + ', this);" alt="上传新版本" style="cursor:pointer" />';
                        }
                        tmpData['rowspan'] = rsp; tmpData['rowspans'] = rsps;
                        tmpData['ver'] = n.lastVersion; tmpData['fversion'] = fv;
                        tmpData['styler'] = styler; tmpData['stylers'] = stylers;
                        tmpData['tzname'] = n.name + n.ext; tmpData['refid'] = n.taskRetFileId || "\'\'";
                        tmpData['fileId'] = n.fileId;
                        tmpData['showhd'] = showhd; tmpData['opera'] = operas.compileTpl({ "refid": n.taskRetFileId, "fileId": n.fileId, "todoId": window.todoId || "", "ver": n.lastVersion });
                        tmpData['gid1'] = String(gid1); tmpData['gid2'] = String(gid2); tmpData['update'] = n.lastVersionDate;
                        cc.push(_trtpl.compileTpl(tmpData));
                        f1++; f2++;
                    });
                    gid2++;
                }
            }
            container.html(cc.join(""));
            __initEvent(container);
        } else {
            container.html('<tr><td align="center">暂无记录</td></tr>');
        }
    }
    function __initEvent(container) {
        container.find("tr").unbind(".fdtz").bind("mouseenter.fdtz", function() {
            if (this.style.backgroundColor != selbg) this.style.backgroundColor = hoverbg;
        }).bind("mouseleave.fdtz", function() {
            if (this.style.backgroundColor == hoverbg) this.style.backgroundColor = '';
        });
        container.bind("click.fdtz", function(ev) {
            var tag = ev.target, $tag = $(tag), _idx = $tag.index(), _tr = tag.parentNode;
            if (tag.tagName.toLowerCase() == 'td' && _idx < 2) {
                var isSel = _tr.getAttribute("isSel");
                if (isSel == "1") {
                    _tr.setAttribute("isSel", "0"); __selectGroupRows(container, tag, false);
                } else {
                    _tr.setAttribute("isSel", "1"); __selectGroupRows(container, tag, true);
                }
            }
        });
        $("input[name='ckb']", container).click(function() {
            var refid = $(this).val(), _tr = $("#tr" + refid);
            if (!$(this).attr("checked")) {
                _tr.attr("isSel", "0");
                _tr[0].style.backgroundColor = '';
            } else {
                _tr.attr("isSel", "1");
                _tr[0].style.backgroundColor = selbg;
            }
        });
        $("#tzckall").click(function() {
            if ($(this).attr("data-ck") == "0") {
                $("input[name='ckb']", container).attr("checked", "checked"); $(this).attr("data-ck", "1");
                container.find("tr").css("backgroundColor", selbg);
            } else {
                $("input[name='ckb']", container).removeAttr("checked"); $(this).attr("data-ck", "0");
                container.find("tr").css("backgroundColor", '');
            }
        });
        $("#tzbatchDel").click(function() {
            a3plan_jfw1.delProc(0, 0);
        });
        $("#tzbatchToF").click(function() {
            a3plan_jfw1.setFormal(0, 0);
        });
    }
    function __selectGroupRows(dt, tag, isSel) {
        var rowindex = tag.parentNode.rowIndex, endrow = rowindex + tag.rowSpan - 1, rows = dt[0].rows, i, iss = isSel ? "1" : "0", ri, colr = isSel ? selbg : "";
        for (i = rowindex; i <= endrow; ++i) {
            ri = rows[i];
            ri.style.backgroundColor = colr; ri.setAttribute("isSel", iss);
            if (!isSel) {
                $(ri).find("input[type='checkbox']").removeAttr("checked");
            } else {
                $(ri).find("input[type='checkbox']").attr("checked", "checked");
            }
        }
    }
    return {
        showFView: function(opt, a) {
            if (!opt) return;
            opts = opt || {};
            __initbox(a);
        }
    }
})();

var a3plan_jfw1 = (function() {
    var urls = {
        delProc: '/Persons/WorkShop/PatchDelete/0', // 删除过程交付物
        showTo1: '/Persons/WorkShop/DeliverablesManage/', // 过程转正式1
        saveTo1: '/Persons/WorkShop/SetAsNewDeliver/', // 保存转为正式交付1
        showTo2: '/Persons/WorkShop/ProgressFileManage/', // 过程转正式2
        saveTo2: '/Persons/WorkShop/SetNewVersion/', // 保存转为正式交付2
        delFor: '/Persons/WorkShop/Delete/' // 删除正式交付物
    }, verTag,
		formTpl = '<tr><td><a href="javascript:;" onclick="fldtable.showFView({vt:2});" class="blue">{fprof}-{fstage}-{fcate}</td><td align="center">{update}</td><td align="center"><a href="javascript:;" onclick="a3plan_jfw1.delPackage(\'{fid}\', this)" class="red">删除</a></td></tr>';

    function __setProcToFormal(ids) { // 设为正式交付物
        if (ids.length == 0) {
            $.tmsg("m_jfw", "请选择要转换的过程交付物！");
            return false;
        } else {
            if (confirm("确定将选中的转为正式交付物？")) {
                var boxid = findObjByBoxid("fldtable");
                boxid.fbox.mask("处理中，请稍候...");
                $.ajax({
                    type: 'POST',
                    url: urls.saveTo1 + taskId, data: { fileIds: ids.join(",") }, complete: function() { boxid.fbox.unmask(); },
                    success: function(data) {
                        if (data.Success) {
                            $.tmsg("m_jfw", "转换成功！", { infotype: 1 });
                            if ($("#DeliverFileList")[0]) $("#DeliverFileList").a3Load("/Projects/Assignment/DeliverFileList/" + taskId + "/0?math=" + Math.random());
                        } else {
                            $.tmsg("m_jfw", data.msgError, { infotype: 2 });
                        }
                    }
                });
            }
        }
    }
    function __setProcToFormal1(ids) { // 包设为正式交付物
        if (ids.length == 0) {
            $.tmsg("m_jfw", "请选择要转换的过程交付物！");
            return false;
        } else {
            if (confirm("确定将选中的转为正式交付物？")) {
                $.ajax({
                    type: 'POST',
                    url: urls.saveTo1 + taskId, data: { fileIds: ids.join(",") },
                    success: function(data) {
                        if (data.Success) {
                            $.tmsg("m_jfw", "转换成功！", { infotype: 1 });
                            if ($("#DeliverFileList")[0]) $("#DeliverFileList").a3Load("/Projects/Assignment/DeliverFileList/" + taskId + "/0?math=" + Math.random());
                        } else {
                            $.tmsg("m_jfw", data.msgError, { infotype: 2 });
                        }
                    }
                });
            }
        }
    }
    function __delProc(ids, o, t) {
        if (ids.length == 0) {
            $.tmsg("m_jfw", "请选择要删除的交付物！");
            return false;
        } else {
            if (confirm("确定删除所选的交付物吗？")) {
                $.ajax({ type: 'POST',
                    url: urls.delProc, data: { fileIds: ids.join(",") },
                    success: function(data) {
                        if (data.Success) {
                            $.tmsg("m_jfw", "操作成功！", { infotype: 1 });
                            if (t) {
                                $(o).parent().parent().remove();
                            } else {
                                __delRows(ids, o);
                            }
                        } else {
                            $.tmsg("m_jfw", data.msgError, { infotype: 2 });
                        }
                    }
                });
            }
        }
    }
    function __delPPkg(ids, o) {
        ids = ids.split(",");
        __delProc(ids, o, 1);
    }
    function __delRows(ids, o) {
        var td1, td2, _tr, ptd1, ptd2;
        $.each(ids, function(i, n) {
            _tr = $("#tr" + n);
            td1 = _tr.find("td:eq(0)"); td2 = _tr.find("td:eq(1)");
            rspan = td1.attr("rowspan"); cspan = td1.attr("colspan");
            if (td1.is(":hidden")) {
                ptd1 = $("td[gid='" + td1.attr("gid") + "']").not(":hidden");
                rspan = ptd1.attr("rowspan");
                ptd1.attr("rowspan", rspan - 1);
            } else if (rspan > 1) {
                ptd1 = _tr.next().find("td:eq(0)");
                ptd1.attr("rowspan", rspan - 1).show();
                if (cspan > 1) ptd1.attr("colspan", cspan);
            }
            rspan = td2.attr("rowspan");
            if (td2.is(":hidden")) {
                ptd2 = $("td[gid='" + td2.attr("gid") + "']").not(":hidden");
                if (ptd2[0]) {
                    rspan = ptd2.attr("rowspan");
                    ptd2.attr("rowspan", rspan - 1);
                }
            } else if (rspan > 1 && cspan == 1) {
                ptd2 = _tr.next().find("td:eq(1)");
                ptd2.attr("rowspan", rspan - 1).show();
            }
            _tr.remove();
        });
    }
    function __delF(ids, o) {
        if (ids === "") {
            $.tmsg("m_jfw", "请选择要删除的正式交付物！");
            return false;
        } else {
            if (confirm("确定删除所选的正式交付物吗？")) {
                $.ajax({ type: 'POST',
                    url: urls.delFor + ids, data: { packageType: 0 },
                    success: function(data) {
                        if (data.Success) {
                            $.tmsg("m_jfw", "操作成功！", { infotype: 1 });
                            __delRows([ids], o);
                        } else {
                            $.tmsg("m_jfw", data.msgError, { infotype: 2 });
                        }
                    }
                });
            }
        }
    }
    function __delPkg(ids, o) {
        if (ids === "") {
            $.tmsg("m_jfw", "请选择要删除的图纸包！");
            return false;
        } else {
            if (confirm("确定删除所选的交付物吗？")) {
                $.ajax({ type: 'POST',
                    url: urls.delFor + "1", data: { fileIds: ids, packageType: 1 },
                    success: function(data) {
                        if (data.Success) {
                            $.tmsg("m_jfw", "操作成功！", { infotype: 1 });
                            $(o).parent().parent().remove();
                        } else {
                            $.tmsg("m_jfw", data.msgError, { infotype: 2 });
                        }
                    }
                });
            }
        }
    }
    return {
        delProc: function(id, o) { // 删除过程交付物
            var ids = [];
            if (o === 0) { // 批量
                var inpts = $("input[name='ckb']");
                inpts.each(function() {
                    if ($(this).attr("checked")) {
                        ids.push($(this).val());
                    }
                });
            } else {
                ids.push(id);
            }
            __delProc(ids, o);
        },
        setFormal: function(id, t) { // 过程转正式
            var ids = [];
            if (t == 0) { // 批量
                var inpts = $("input[name='ckb']");
                inpts.each(function() {
                    if ($(this).attr("checked")) {
                        ids.push($(this).val());
                    }
                });
            } else {
                ids.push(id);
            }
            __setProcToFormal(ids);
        },
        pkgToFormal: function(ids) {
            ids = ids.split(",");
            __setProcToFormal1(ids);
        },
        delF: function(id, o) { // 删除正式交付物
            __delF(id, o);
        },
        delPackage: function(ids, o) { // 删除包
            __delPkg(ids, o);
        },
        delPPkg: function(ids, o) {
            __delPPkg(ids, o);
        }
    }
})();

function inFolderUploadnewVer(taskRetFileId, tag, backpos) {

    var arr = fs.GetSelectList(1, "*.*");

    if (arr.length > 0) {
        var path = "";
        for (var x = 0; x < arr.length; x++) {
            if (path != "") { path += "|Y|"; }
            path += arr[x].path;
        }
        $.post('/Projects/ProjArranged/UploadNewVersion',
                {
                    fileId: taskRetFileId,
                    uploadFileList: path
                },
                function(data) {
                    if (data.Success) {
                        var files = eval("(" + data.msgError + ")");
                        for (var x = 0; x < files.length; x++) {
                            var path = files[x].path.replace(/\//g, "\\");
                            fs.AddToList(path, files[x].strParam);
                        }
                        $.tmsg("m_upfile", "上传成功", { infotype: 1 });
                        var verTag = $(tag);
                        if (verTag[0]) {
                            var atag, txttag, txt = '';
                            if (backpos) {
                                atag = verTag.parent().prev().prev();
                                txttag = atag.prev();
                            } else {
                                atag = verTag.parent().next();
                                txttag = verTag.parent();
                            }
                            txt = atag.find('a').html() + "";
                            txt = txt.replace(/[A-Za-z]/g, "");
                            txt = 'V' + String(parseInt(txt, 10) + 1);
                            atag.find('a').html(txt);
                            var _html = txttag.html(),
								a = _html.match(/,\'\d{1,}\',/ig), _s1;
                            _s1 = (a[0] + "").replace(/[^\d]/ig, "") || 0;
                            _s1 = parseInt(_s1, 10) + 1;
                            _html = _html.replace(/,\'\d{1,}\',/ig, ",'" + _s1 + "',");
                            txttag.html(_html);
                        }
                    } else {
                        $.tmsg("m_upfile", data.msgError, { infotype: 2 });
                    }
                }, 'json');

    }
}