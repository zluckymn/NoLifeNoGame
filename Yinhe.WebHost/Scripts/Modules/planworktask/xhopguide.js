YH.addCSSRule(".thd th", "background-color:#e4e4e4;height:30px;", 0);
YH.addCSSRule(".gflist", "margin:5px 0 0 5px;background-color:#ccc;", 0);
YH.addCSSRule(".gflist thead", "background-color:#e4e4e4;", 0);
YH.addCSSRule(".gflist tr", "background-color:#fff;", 0);
YH.addCSSRule(".gflist tr td", "padding: 5px 2px;line-height: 18px;white-space: normal;word-wrap: normal;", 0);
YH.addCSSRule(".rowodd td", "background-color:#f2f2f2;", 0);
YH.addCSSRule("#opg_pag td", "padding:0;", 0);
var opGuideL = (function() { // 作业指引
    var tpl = ['<table cellpadding="0" cellspacing="0">',
        '<tr><td width="220" height="450" style="position:relative;padding-top:10px;"><div style="position:absolute;left:0;top:0;width:0;height:480px;background:#eff5fb;" id="_gleftpne"></div><div id="ltree" style="height:445px;overflow:auto;"></div></td>',
        '<td style="border-left:1px solid #ccc;position:relative;"><div id="_gtabs" class="worktab"><ul><li><a href="javascript:;" class="select">工作指引<span></span></a></li><li><a href="javascript:;">成果标准<span></span></a></li><li><a href="javascript:;">成果范例<span></span></a></li><li><a href="javascript:;">汇报文档<span></span></a></li></ul></div><div id="_gflist" style="width:570px;height:420px;overflow-y:auto;">',
		'<table cellpadding="0" class="gflist" cellspacing="1">',
		'<thead class="thd"><th width="30"></th><th width="300">文档名称</th><th width="60">上传者</th><th width="80">时间</th><th width="80">操作</th></thead>',
		'<tbody></tbody>',
		'</table>',
        '</div>',
        '<div style="padding:3px 2px;background-color:#ffffff; height:25px;">',
        '<div style="float:left;" id="opg_search"><input type="text" style="width:100px" /><input type="button" value="搜索" /></div>',
        '<div style="float:left;margin-left:10px;width:330px;" id="opg_pag"></div>',
        '<div style="float:right"><a class="zPushBtn" href="javascript:;" id="upnewpanel" style="margin-right:0px;"><img src="/Content/Images/zh-cn/General/icon003a2.gif"/><b>添加指引</b></a></div>', // <a href="javascript:;" id="closebox">关闭</a>
        '</div>',
        '<div style="border-top:1px solid #ccc;position:absolute;width:565px;padding:10px 5px 5px;background:#fff;bottom:0px;left:0px;display:none;" id="uppne"><div style="height:28px;"><a href="javascript:;" id="_gselfile" class="zPushBtn"><img src="/Content/Images/zh-cn/General/icon003a2.gif"/><b>添加文件</b></a></div>',
        '<form id="_guploadForm"><table id="newlist" width="100%" class="tableborder"><tr bgcolor="#f7f7f7"><td style="display:none;"></td><td align="left">文件名</td><td width="30">格式</td><td width="60">大小</td><td width="130">路径</td><td width="40">操作</td></tr></table></form>',
        '<div style="padding:5px;text-align:right"><a class="zInputBtn" href="javascript:;"><input type="button" value="上传" id="upnew" class="inputButton" /></a> 或 <a href="javascript:;" id="cancelupload" class="blue">取消</a></div>',
        '</div>',
        '</td></tr></table>'
		].join(""), tid, currentPage = 1, pageSize = 15, busiOrgId, pag, kw = '', tp;
    function settype(type, o) {
        tp = type;
        var _tabct = { 0: '指引', 1: '标准', 2: '范例', 3: '文档' };
        $("#upnewpanel").find("b").html('添加' + _tabct[type]);
        //成果范例又说暂时需要跟着区域，暂时修改 by Radar
        //if (type === 2 || type === 3) {
        if (type === 3) {
            $("#_gleftpne").animate({ width: '222px' });
        } else {
            $("#_gleftpne").animate({ width: '0' });
        }
        getFileList('');
    }
    function __pbox(obj, taskId, type, hideHB) {
        var taskName = obj.getTaskFieldData(taskId, 'name'), sinpt, sbtn;
        tid = taskId; tp = type;
        box(tpl, { boxid: 'pbox', title: '<span style="color:#009933">' + taskName + '</span>', modal: true, width: 800, cancel_BtnName: "关 闭", no_submit: true, no_cancel: true,
            onOpen: function(o) {
                o.fbox.mask("loading..."); $("#_gleftpne").css('zIndex', (parseInt(o.fbox.css('zIndex'), 10) + 5) || 10050);
                var tabs = $("#_gtabs").find("li");
                if (hideHB) $(tabs.get(3)).hide();
                settype(type, o);
                tabs.each(function() {
                    $(this).click(function() {
                        var _tp = $(this).index();
                        settype(_tp, o); tabs.find('a').removeClass('select');
                        $(this).find("a").addClass('select');
                    });
                });
                $(tabs.get(type)).trigger('click');
                $("#ltree").SelectTree({
                    startXml: "/Projects/ProjArranged/GetBusinessOrgXml/?r=" + Math.random(),
                    defaultShowItemLv: 5,
                    _onClick: function(id, name, obj, node) {
                        busiOrgId = id;
                        currentPage = 1;
                        getFileList('');
                    },
                    _afterPrint: function(obj) {
                        obj.selectTreeItem();
                    }
                });
                var sech = $("#opg_search").find("input");
                sinpt = $(sech[0]); sbtn = $(sech[1]);
                pag = $("#opg_pag");
                $("#upnewpanel").click(function() {
                    uploadnew(true);
                });
                $("#closebox").click(function() {
                    o.destroy();
                });
                sbtn.click(function() {
                    searchFiles($.trim(sinpt.val()));
                });
                o.fbox.unmask();
            },
            submit_cb: function(o) {
            }
        });
    }
    function getFileList(keyword) {
        //if (!busiOrgId) return;
        var flist = $("#_gflist"), _busiOrgId;
        flist.mask('load...');
        //成果范例又说暂时需要跟着区域，暂时修改 by Radar
        //_busiOrgId = (tp === 2 || tp === 3) ? 0 : busiOrgId;
        _busiOrgId = busiOrgId;
        $.ajax({
            type: 'get', url: '/Projects/ProjArranged/GetDesignGuide', cache: false,
            data: { taskId: tid, busiOrgId: _busiOrgId, keyWord: keyword, pageSize: pageSize, current: currentPage, fileType: tp },
            dataType: 'json',
            complete: function() { flist.unmask(); },
            success: function(rs) {
                var i = 0, rend = [], _o, _s, count = 1, idx, totalCount = 0, cls;
                for (; i < rs.length; ++i) {
                    _o = rs[i];
                    if (totalCount == 0) {
                        totalCount = _o.totalCount;
                    }
                    idx = count + (currentPage - 1) * pageSize; cls = '';
                    if (idx % 2 === 0) cls = ' class="rowodd"';
                    _s = '<tr' + cls + '><td idx="1">' + idx + '</td><td><a href="javascript:void(0)" onclick="ReadOnlineCustom(\'' + _o.fileId + '\',\'1\',0)">' + _o.name + _o.ext + '</a></td><td> ' + _o.createUser + '</td><td>' + _o.createDate + '</td><td> <a href="javascript:void(0)" onclick="DownLoadByIdCustom(\'' + _o.fileId + '\',\'1\',0)" class="green">下载</a>' + (_o.isBlocFile != 1 ? '<a href="javascript:void(0)" delflag="' + _o.relId + '">删除</a>' : '') + '</td></tr>';
                    rend.push(_s); _s = null;
                    count++;
                }
                var ct = flist.find('tbody');
                _s = rend.join('');
                ct.html(_s); _s = null; rend = null;
                if (totalCount > 0) buildPag(totalCount);
                flist.find("a[delflag]").each(function() {
                    $(this).unbind('click').bind('click', function() {
                        delFile(this);
                    });
                });
                var tabs = $("#_gtabs").find("li");
                $(tabs.get(tp)).find("span").html('(' + totalCount + ')');
            }
        });
    }
    function delFile(o) {
        var tag = $(o), fileId = tag.attr('delflag');
        if (confirm('确定删除吗？\n删除后不可恢复！')) {
            $.post('/Projects/ProjArranged/DeleTaskFiles', { relIds: fileId }, function(data) {
                if (data.Success) {
                    $(o).parent().parent().remove();
                    reOrder();
                }
                else {
                    hiAlert(data.msgError, '删除失败！');
                }
            });
        }
    }
    function reOrder() {
        $("td[idx='1']").each(function(i) {
            $(this).text(i + 1);
        });
    }
    function buildPag(totalCount) {
        pag.pagination({ total: totalCount, display_pc: 3, displayMsg: "", showInput: false, showPageList: false, showRefresh: false, loading: false, pageSize: pageSize,
            onSelectPage: function(current_page, pagesize) {
                currentPage = current_page;
                getFileList(kw);
            }
        });
    }
    function searchFiles(keyword) {
        currentPage = 1;
        kw = keyword;
        getFileList(keyword);
    }
    function uploadnew(first) {
        var uppne = $("#uppne");
        if (first) $("#newlist").find("tr:gt(0)").remove();
        uppne.slideDown(function() {
            var pne = $(this);
            $("#cancelupload").unbind('click').bind('click', function() {
                pne.slideUp();
            });
            $("#_gselfile").unbind('click').bind('click', function() {
                SelBatch2('newlist', tid, busiOrgId, tp);
            });
            if (first) {
                SelBatch2('newlist', tid, busiOrgId, tp);
            }
        });
        $("#upnew").unbind('click').bind('click', function() {
            opGuidesSave(function() {
                uppne.slideUp();
                currentPage = 1;
                getFileList('');
            });
        });
    }
    return {
        init: function(taskId, type, hideHB) {
            __pbox(this, taskId, type, hideHB);
        }
    }
})();

function _removeTmp(o) {
    $(o).parent().parent().remove();
}
//批量选择和上传文件
function SelBatch2(tableid, taskId, businessOrgId, fileType) {
    var arr = fs.GetSelectList(0, "");
    if (arr == false) { return false; }
    var len = arr.length, l = $("tr[name='newupload']").length, html = "", i = 0, ct = [];
    if (len == 0) { return false; }
    //成果范例又说暂时需要跟着区域，暂时修改 by Radar
    //var _busiOrgId = (fileType === 2 || fileType === 3) ? 0 : businessOrgId;
    var _busiOrgId = businessOrgId;
    for (; i < len; ++i) {
        var FileFullName = arr[i].path.substring(arr[i].path.lastIndexOf("\\") + 1),
            FileName = FileFullName.substring(0, FileFullName.lastIndexOf(".")),
            FileExt = fs.GetFileExt(arr[i].path);
        ct.push('<tr name="newupload">');
        ct.push('<td style="display:none;"><input type="hidden" name="fileRels[' + l + '].fileRelationId" value="0" /><input type="hidden" name="fileRels[' + l + '].taskId" value="' + taskId + '" /><input type="hidden" name="fileRels[' + l + '].fileType" value="' + fileType + '" /><input type="hidden" name="fileRels[' + l + '].businessOrgId" value="' + _busiOrgId + '" /><input type="hidden" name="fileRels[' + l + '].remark" value="" /><input type="hidden" name="fileRels[' + l + '].status" value="" /></td>');
        ct.push('<td><input name="fileRels[' + l + '].FileName" value="' + FileName + '" style="width:100px" /></td>');
        ct.push('<td><input name="fileRels[' + l + '].Ext" value="' + FileExt + '" style="width:100px"/></td>');
        ct.push('<td><input name="fileRels[' + l + '].Size" value="' + arr[i].size + '" style="width:100px"/></td>');
        ct.push('<td><input name="fileRels[' + l + '].LocalFilePath" value="' + arr[i].path + '" style="width:100px"/></td>');
        ct.push('<td><a href="javascript:;" onclick="_removeTmp(this);">移除</a></td>');
        ct.push('</tr>');
        l++;
    }
    html = ct.join('');
    $("#" + tableid).find('tr:last').after(html);
}
function opGuidesSave(callback) {
    $("tr[name='newupload']").each(function(i) {
        var inpts = $(this).find("input");
        inpts.each(function() {
            this.name = this.name.replace(/\d/g, i);
        });
    });
    var formData = $("#_guploadForm").serialize();
    if (formData == "" && formData == null) return false;
    var box = findObjByBoxid("pbox");
    box.fbox.mask('处理中，请稍候...');
    $.ajax({
        type: 'POST', url: '/Projects/ProjArranged/SaveTaskFiles', data: formData,
        complete: function() { box.fbox.unmask(); },
        success: function(data) {
            if (data.Success == false) {
                alert(data.msgError);
            } else {
                if (data.htInfo && data.htInfo.params) {
                    var files = eval("(" + data.htInfo.params + ")"),
                        len = files.length, str = "";

                    for (var i = 0; i < len; i++) {
                        var file = files[i];
                        if (str == "") {
                            str = fs.bLocalSign + "|" + file.path + "|" + file.strParam;
                        } else {
                            str += "||" + fs.bLocalSign + "|" + file.path + "|" + file.strParam;
                        }

                    }
                    //批量添加到文件列表
                    if (str != "") {
                        fs.AddToListEx(str);
                    }
                    $.tmsg('m_upld', '操作成功！', { infotype: 1 });
                    callback && callback();
                }
            }
        }
    });
}

var apprSet = (function () {
    var taskId;
    function showPanel(t) {
        box('/DesignManage/ProjTaskFlow?taskId=' + taskId + '&r=' + Math.random(), { boxid: 'apprset', title: '审批流程设置', contentType: 'ajax', cls: 'shadow-container', modal: true, width: 700, submit_BtnName: '保存', no_cancel: true,
            onLoad: function () {
                changeFlowInfoDiv();
                //流程选择弹窗初始化时不显示编辑模块
         //       changeFlowInfoEditDiv();
            },
            submit_cb: function () {
                saveTaskBusFlow();
            }
        });
    }
    return {
        init: function (tid) {
            taskId = tid;
            showPanel(this);
        }
    };
})();