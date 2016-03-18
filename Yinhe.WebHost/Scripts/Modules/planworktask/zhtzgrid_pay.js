/**
* jdgrid.js 2011/11/14 by Qingbao.Zhao, depend wkpcomm.js
*/
var _isexpLibId = false,
    __globalDomain = typeof globalHostDomain_ != 'undefined' ? globalHostDomain_ : '';
var commApi = {
    "getPlanData": "/Home/TaskList",
    "createTasks": '/Home/QuickCreateTask', // 创建任务
    "delTask": '/Home/DeleteTask', // 删除任务
    "editFormula": '/Home/FormulaEdit', // 编辑公式
    "delFormula": '/Home/DeleteFormula', // 删除公式
    "saveTask": '/Home/SaveTaskInfo', // 保存任务
    "importExp": '/DesignManage/QuicklyLoadExperience/', // 导入公司经验
    "iExpLoad": '/Home/NewTaskList', // 获取对应公司经验的任务结构
    "iExpSave": '/Home/QuickyCreatePlan', // 导入到计划
    "exportExp": '/DesignManage/CreateExperience/', // 导出为公司经验
    "exphasexist": '/Home/HasExistExpLibObj', // 判断导出计划的名称是否重名
    "saveAsExp": '/Home/SaveAsExpLib/', // 保存为经验
    "saveTaskRelation": '/Home/QuickSaveTaskRelation', // 前置任务保存
    "moveTask": '/Home/MovePostInfo', // 移动任务
    "templinkData": '/Home/GetTaskListByPlanTemplate', //获取模板计划中一级计划下的联动节点
    "projlinkData": '/home/GetTaskListByNodeTypeId', //获取二级项目中一级开发项目下的联动节点
    "PlanTaskRelData": '/home/SaveAcrossPlanTaskRelation', //设置前置后置任务
    //2012-1-14地址修改projtaskdetail to ProjTaskInfo
    "taskUrl": '/DesignManage/ProjTaskInfo/', // 任务详情链接
    "batchFzr": '/Home/BatchSetTaskOwner/', // 批量设置负责人
    'imgUrl': __globalDomain + '/Content/images/icon/', // 任务状态图标目录
    'taskStatImg': { '2': '', '3': 'ico_yellow.png', '4': 'ico_green.png' }, // 任务状态图标
    "tpl": ['<tr id="tr{taskId}" cp="1">',
		'<td field="torder" class="datagrid-td-rownumber"><div class="datagrid-cell-rownumber">{rowindex}</div></td>',
		'<td field="name" class="{cls}" width="302"><div style="padding-left:{padleft}px;padding-right:5px;float:left"><img style="{icon}" id="img{taskId}" src="' + __globalDomain + '/Content/images/DesignManage/ico14.jpg"></div><div style="float:left;width:{nwidth}px">{name}</div></td>',
        '<td field="ownerName">{ownerName}</td>',
        '<td field="pretasks">{pretasks}</td>',
        '<td field="relTaskFileCount" style="text-align:center">{relTaskFileCount}</td>',
		'<td field="designUnitName">{designUnitName}</td>',
        '<td field="totalContractAmount" style="text-align:center">{totalContractAmount}</td>',
        '<td field="payedContractAmount" style="text-align:center">{payedContractAmount}</td>',
        '<td field="unpayedContractAmount" style="text-align:center">{unpayedContractAmount}</td>',
        '<td field="nodeTypeId">{nodeName}</td>',
		'<td field="status" style="text-align:center" class="norbor">{statusName}</td>',
		'</tr>'],
    "tplv": ['<tr cp="1">', '<td><div class="tb_con">{rowindex}</div></td>', '<td><div class="tb_con"><div style="padding-left:{padleft}px;padding-right:5px;float:left"><img style="{icon}" src="' + __globalDomain + '/Content/images/DesignManage/ico-Expand.gif"></div><div style="float:left">{name}</div></div></td>', '<td><div class="tb_con">{ownerName}</div></td><td><div class="tb_con"></div></td><td><div class="tb_con"></div></td>', '</tr>']
}, saveTip = $(".savetip");

var gAjaxQueue = $.manageAjax.create('AjaxQueue', { // 针对上面commApi 编号1的
    queue: true, maxRequests: 2, beforeSend: function () { saveTip.show(); }, complete: function () { saveTip.hide(); }
}), gLoadQueue = $.manageAjax.create('loadQueue', {
    queue: true
});

var hoverbg = '#d0e5f5', selbg = '#fcf1a9', copyDate = { planId: 0, taskId: 0, field: '', v: '', f: '' },
    epcl = [__globalDomain + "/Content/images/DesignManage/ico14.jpg", __globalDomain + "/Content/images/DesignManage/ico15.jpg"];

function a2DataGrid(config) {
    this.config = config || {};
    this.grid = this.config.el;
    this.bd = this.grid.find("table:first");
    this.headcol = this.bd.find("thead"); // 表头
    this.headcol = this.headcol.find("tr:last"); // 参考表头
    this.columns = null;
    this.dt = this.bd.find("tbody"); // 数据
    this.focusDiv = this.grid.find(".focusDiv"); // focus proxy
    this.inputDiv = this.grid.find(".inputDiv"); // editor proxy
    this.selRows = {}; // 选中的行
    this.shiftSel = { 's': -1, 'sels': {} };

    this.init = function () {
        var self = this, focusDiv = this.focusDiv;
        this.resetData_();
        this.dt.unselectable(); this.colIdxs = {};
        this.columns = this.getCloumns(); // 表头配置
        this.projId = this.grid.attr("data-projId"); this.secdPlanId = this.grid.attr("data-secdPlanId");
        this.hasRight = this.grid.attr("canedit") || '1';
        this.canDel = this.grid.attr("candel") || '1';
        this.hasRight = this.hasRight == '1' ? true : false;
        this.canDel = this.canDel == '1' ? true : false;
        this._userId = this.grid.attr("userId") || -100;

        focusDiv.mousedown(function (ev) {
            var tag = self.curCell.td, _idx = $(tag).index();
            if (self.config.keynav) self.config.keynav.setxy(_idx, tag.parentNode.rowIndex - 1, self.dt, self);
            if (_idx == 1 && !self.isEdit) $(tag).trigger('draggable');
            ev.preventDefault();
        }).unselectable()
		.dblclick(function () {
		    var cell = self.curCell, tid = cell.tid, ownerUserId = self.getTaskFieldData(tid, 'ownerUserId');
		    if (!self.hasRight) {
		        if (ownerUserId != self._userId) {
		            return false;
		        }
		    }
		    self.initEditor();
		}).bind("contextmenu", function (e) {
		    var cell = self.curCell, tid = cell.tid, ownerUserId = self.getTaskFieldData(tid, 'ownerUserId');
		    if (!self.hasRight) {
		        if (ownerUserId != self._userId) {
		            return false;
		        }
		    }
		    self.menu(e); var _menu = $("#wkmenu");
		    return false;
		});
        this.grid.find("input.createTask").click(function () {
            self.createTask(false, 0);
        });
        this.grid.find("input.imexp").click(function () {
            var t = $(this);
            self.importExp(t);
        });
        this.grid.find("input.exexp").click(function () {
            self.exportExp();
        });
        this.grid.find("a.expall").click(function () { // 全部展开
            var _rows = self.dt[0].rows, _tr, i = 0;
            for (; _tr = _rows[i]; ++i) {
                _tr = $(_tr);
                _tr.show();
                if (_tr.attr("cp") == "0") {
                    _tr.attr("cp", "1");
                    _tr.find("img:first").attr("src", epcl[0]);
                }
            }
            self.focusDiv.hide();
        });
        this.grid.find("a.colall").click(function () { // 全部折叠
            var _rows = self.dt[0].rows, _tr, i = 0, _id;
            for (; _tr = _rows[i]; ++i) {
                _id = _tr.id.replace('tr', '');
                _tr = $(_tr);
                if (self.stof[_id] == 0) {
                    if (_tr.attr("cp") == "1") {
                        _tr.attr("cp", "0");
                        _tr.find("img:first").attr("src", epcl[1]);
                    }
                } else {
                    _tr.hide();
                }
            }
            self.focusDiv.hide();
        });
    };
    this.getCloumns = function () { // 获取表头的属性配置
        var tr = this.headcol, _cols = [], self = this;
        tr.find("th").each(function (i) {
            var td = $(this);
            var col = {
                title: td.text(),
                align: td.attr("align") || "left"
            };
            if (td.attr("field")) {
                col.field = td.attr("field");
                self.colIdxs[col.field] = i;
            }
            if (td.attr("ccode")) {
                col.ccode = td.attr("ccode");
            }
            if (td.attr("formatter")) {
                col.formatter = eval(td.attr("formatter"));
            }
            if (td.attr("editor")) {
                var s = $.trim(td.attr("editor"));
                if (s.substr(0, 1) == "{") {
                    col.editor = eval("(" + s + ")");
                } else {
                    col.editor = s;
                }
            }
            if (td.attr("rowspan")) {
                col.rowspan = parseInt(td.attr("rowspan"));
            }
            if (td.attr("colspan")) {
                col.colspan = parseInt(td.attr("colspan"));
            }
            if (td.attr("width")) {
                col.width = parseInt(td.attr("width"));
            }
            if (td.attr("hidden")) {
                col.hidden = td.attr("hidden") == "true";
            }
            _cols.push(col);
        });
        return _cols;
    };
    this.init();
    this.loadData();
}

a2DataGrid.prototype.resetData_ = function () {
    this.curCell = null;
    this.rows = []; // 任务数据
    /* 父子任务关系链
    stof = {
    tid: pid,
    tid1: pid,
    };
    ftos = {
    pid: [tid, tid1],
    pid1: [tid2],
    };
    */
    this.stof = {}; this.ftos = {};
    this.rowsindex = {}; // 任务数据索引
    this.iRowCount = 0;
    this.isEdit = false;
};


a2DataGrid.prototype.loadData = function () { // 加载数据
    var self = this;
    self.grid.mask("数据加载中，请稍候...");
    gLoadQueue.add({
        dataType: 'JSON', cache: false,
        url: commApi.getPlanData, data: { planId: self.secdPlanId },
        success: function (rs) {
            var r = rs;
            self.zhhyFilter(); self.iRowCount = 0;
            self.renderRows(rs, 0, 0, 0);
            if (self.config.afterRender) self.config.afterRender.call(self, YH.$id('tplan' + self.secdPlanId));
            self.grid.unmask();
            $("a[class=colall]").click();  //执行折叠事件
        }
    });

};
a2DataGrid.prototype.zhhyFilter = function () {
    var self = this, rc = this.headcol;
    rc.find("th:eq(9)").bind("click", function () {
        nodemenu(self, this);
    });
    rc.find("th:eq(10)").bind("click", function () {
        statusmenu(self, this);
    });
};
a2DataGrid.prototype.calcrs_ = function (dt, lvl) {
    var i = 0, _tmp, hasChild = false, _tid = dt['taskId'], tca,
        _cls = "gray", clrs = '', clre = '', pretasks = '', _url, _exemption = '', padleft, fcount = 0;

    if (dt['name'] != "请填写任务名称") {
        _cls = "";
        if (dt['relConDiagId'] > 0 || dt['isKeyTask'] == 1) {
            //_cls = ' class="';
            if (dt['relConDiagId'] > 0) _cls += 'red';
            if (dt['isKeyTask'] == 1) _cls += ' redstrong';
            //_cls += '"';
        }
        _url = _isexpLibId ? '<a href="javascript:;">' : '<a href="' + commApi.taskUrl + _tid + '" target="_blank" class="' + _cls + '">';
        if (dt['hasCIList'] == 1) _exemption += '<img src="/content/images/icon/ico0006.png" title="有配置清单" name="cilist" style="vertical-align:middle;" />';
        dt['name'] = _url + dt['name'].replace(/ /g, "&nbsp;") + '</a>' + _exemption;
    }
    dt['SetApproval'] = dt['hasApproval'] ? '是' : '';
    if (dt['taskRelations'] && dt['taskRelations'].length) {
        for (; _tmp = dt['taskRelations'][i]; ++i) {
            pretasks += _tmp.relTaskName + ' (' + _tmp.relTaskStateName + '),<br/>';
            fcount += parseInt(_tmp.relTaskFileCount, 10) || 0;
        }
        pretasks = pretasks.replace(/,<br\/>$/g, '');
    }
    dt["relTaskFileCount"] = String(fcount);
    tca = parseFloat(dt["totalContractAmount"], 10) || 0;
    if (tca > 0) dt["totalContractAmount"] = tca + '万元';
    dt["pretasks"] = pretasks;
    dt["cls"] = _cls;
    dt["pointName"] = String.cut(dt["pointName"] || "", 12, '...');
    dt['clrs'] = clrs; dt['clre'] = clre;
    if (dt.status <= 2) {
        dt["statusName"] = '';
    } else {
        dt["statusName"] = '<img src="' + commApi.imgUrl + commApi.taskStatImg[dt.status] + '" alt="' + dt["statusName"] + '" />';
    }
    padleft = 16 * lvl;
    dt['padleft'] = padleft;
    dt['nwidth'] = 302 - padleft - 28;
    dt['icon'] = "cursor:pointer";
    if (dt['needSplit'] == 0) dt['spliterName'] = "";
    return dt;
};
a2DataGrid.prototype.renderRows = function (data, texp, pid, lvl, ino) { // 渲染
    var tasks, self = this, cc = [], tpl = commApi.tpl.join(""), _rowindex = 0,
        ctsks = [], m = 0, _ex, setv = [], setone = false, f = null;
    if ($.isArray(data)) {
        tasks = data || [];
    } else {
        tasks = data.jsonGroupTaskList;
        f = data.jsonFormulaList;
    }
    if (tasks.length === 0 || tasks[0] === 0) return;
    if (texp || ino > 0) _rowindex = self.rows.length;
    this.rows = this.rows.concat(tasks);
    for (; _ex = tasks[m]; ++m) {
        self.rowsindex[_ex.taskId] = { index: _rowindex }; _rowindex++;
        _ex = $.extend({}, _ex);
        ctsks.push(_ex);
    }
    _rowindex = 0;
    function rd(pid, lv) {
        var dt, i = 0, _tid, _pid, tmp;
        for (; dt = ctsks[i]; ++i) {

            _tid = dt.taskId; _pid = dt.nodePid; setv.push(_tid);
            if (_pid == pid) {
                self.stof[_tid] = _pid;
                self.ftos[_tid] = self.ftos[_tid] || [];
                self.ftos[_pid] = self.ftos[_pid] || [];
                if (self.ftos[_pid].indexOf(_tid) == -1) self.ftos[_pid].push(_tid);
                _rowindex++;
                dt = self.calcrs_(dt, lv);
                dt["rowindex"] = _rowindex;
                cc.push(tpl.compileTpl(dt));
                self.iRowCount++;
                rd(_tid, lv + 1);
            }
        }
    }

    rd(pid, lvl);
    cc = cc.join('');
    if (texp) {
        this.dt.append(cc); this.reOrder(true);
    } else if (ino > 0) {
        $(cc).insertAfter($("#tr" + ino)); setone = true;
        this.reOrder(true);
    } else {
        this.dt.html(cc);
    } cc = null;
    if (setone) {
        for (m = 0; _ex = setv[m]; ++m) {
            if (self.ftos[_ex].length == 0) $("#img" + _ex).css("visibility", "hidden");
        }
    } else {
        for (var k in self.ftos) {
            if (self.ftos[k].length == 0) {
                $("#img" + k).css("visibility", "hidden");
            }
        }
    }
    setv = null;
    if (this.curCell) this.focusDiv.hide();
    this.initEv();
};
a2DataGrid.prototype.createTask = function (frommenu, pos) { // 创建任务 pos: 1--创建任务，2--子任务，0--尾部插入

    var self = this, cell = self.curCell, curTaskId, nodePid = 0, cur = 0; // secdPlanId,projId,isExpLib
    if (frommenu) {
        curTaskId = cell.tid; cur = curTaskId;
        if (pos == 1) {
            nodePid = curTaskId > 0 ? self.stof[curTaskId] : 0;
        } else if (pos == 2) {
            nodePid = curTaskId; curTaskId = 0;
        }
    } else {
        curTaskId = 0;
    }

    this.grid.mask("数据处理中，请稍候...");

    $.ajax({
        type: 'POST',
        url: commApi.createTasks,
        data: { curTaskId: curTaskId, planId: self.secdPlanId, projId: self.projId, isExpLib: 0, taskId: 0, nodePid: nodePid },
        success: function (rs) {
            if (rs.Success) {
                self.insertRows(rs.htInfo, frommenu, pos, cur);
                self.grid.unmask();
            } else {
                $.tmsg("createtask", rs.Message, { infotype: 2 });
            }
        }
    });
};
a2DataGrid.prototype.addRemark = function () { // 添加备注
    var tpl = '<textarea style="width:390px;height:150px;"></textarea>', self = this, tid = self.curCell.tid,
		dt = self.getTaskFieldData(tid, "remark") + "";
    box(tpl, { boxid: 'remark', title: '添加备注', cls: 'shadow-container', modal: true, width: 400,
        onOpen: function (o) {
            o.db.find("textarea").val(dt);
        },
        submit_cb: function (o) {
            var newtxt = o.db.find("textarea").val();
            if (dt != newtxt) {
                self.rows[self.rowsindex[tid].index]['remark'] = newtxt;
                self.saveTask(tid);
            }
        }
    });
};
a2DataGrid.prototype.preTask = function () { // 费用支付选择基本计划中的节点作为前置节点
    var self = this, tid = self.curCell.tid, nodeTypeId, relationType;
    box("#projTaskNodeList", { boxid: 'UpLoadDiv', title: '选择前置节点', contentType: 'selector', width: 600,
        onOpen: function (o) {
            o.fbox.find(".nodeList").load("/DesignManage/ProjTaskSelectNode?taskId=" + tid + "&nodeTypeId=0&relationType=0&r=" + Math.random());
            o.fbox.find(".nodeSearch").find("a[s]").each(function () {

                $(this).click(function () {
                    $(this).addClass("select").siblings().removeClass("select");
                    if ($(this).attr("s") == "s") {
                        o.fbox.find(".nodeList").load("/DesignManage/ProjTaskSelectNode?taskId=" + tid + "&nodeTypeId=0&relationType=0&r=" + Math.random(), function () {

                        });
                    } else {
                        o.fbox.find(".nodeList").load("/DesignManage/ProjTaskSelectNode?taskId=" + tid + "&nodeTypeId=2&relationType=0&r=" + Math.random(), function () {

                        });
                    }
                })

            })

            o.fbox.find(".nodeSearch").find("a[t]").each(function () {

                $(this).click(function () {
                    if ($(this).attr("t") == "t") {
                        var keyword = $(this).parent().find("input").val();
                        o.fbox.find(".nodeList").load("/DesignManage/ProjTaskSelectNode?taskId=" + tid
+ "&nodeTypeId=0&relationType=0&keyWord=" + escape(keyword) + "&r=" + Math.random(), function () {

});
                    } else {

                        if (o.fbox.find("a.select").attr("s") == "s") {
                            nodeTypeId = 0;
                            relationType = 0;
                        } else {
                            nodeTypeId = 2;
                            relationType = 1;
                        }

                        o.fbox.find(".nodeList").load("/DesignManage/ProjTaskSelectNode?taskId=" + tid + "&nodeTypeId=" + nodeTypeId + "&relationType=" + relationType + "&r=" + Math.random(), function () {

                        });
                    }
                })

            })


        },

        submit_cb: function (o) {
            var obj = o.fbox.find("form");
            var keys = "", arr = [];
            $("input[name=key]:checked", o.db).each(function () {
                var $this = $(this);
                keys += $this.val() + "|Y|";
                arr.push({name: $this.attr("relTaskName"), status: $this.attr("relTaskStateName"), fileCount: $this.attr("relTaskFileCount")});
            })
            $("input[name=saveKey]", o.db).val(keys);
            saveTaskRelationInfo(obj, arr, tid);
        }
    })

    function saveTaskRelationInfo(obj, arr, tid) {
        var formdata = $(obj).serialize();

        $.ajax({
            url: "/Home/QuickSaveTaskRelationEx",
            type: 'post',
            data: formdata,
            dataType: 'json',
            error: function () {
                $.tmsg("m_jfw", "未知错误，请联系服务器管理员，或者刷新页面重试", { infotype: 2 });
            },
            success: function (data) {
                if (data.Success == false) {
                    $.tmsg("m_jfw", data.Message, { infotype: 2 });
                }
                else {
                    $.tmsg("m_jfw", "保存成功！", { infotype: 1 });
                    // reloadContractDiv();
                    self.updateRelTasks(tid, arr);
                }
            }
        });
    }
};
a2DataGrid.prototype.updateRelTasks = function(taskId, arr) {
    var str = "", _tmp, i = 0, filecount = 0;
    if (arr.length) {
        for (; _tmp = arr[i]; ++i) {
            str += _tmp.name + ' (' + _tmp.status + '),<br/>';
            filecount += parseInt(_tmp.fileCount, 10) || 0;
        }
        str = str.replace(/,<br\/>$/g, '');
    }
    this.updateView(taskId, "pretasks", str);
    this.updateView(taskId, "relTaskFileCount", filecount);
    this.cancelEdit();
};
a2DataGrid.prototype.getIDs_ = function (pid) { // 获取要移动、删除的taskID
    var arr = [], self = this;

    function getC_(_pid) {
        var _a = self.ftos[_pid], _n, i = 0;
        arr.push(_pid);
        for (; _n = _a[i]; ++i) {
            getC_(_n);
        }
    }
    getC_(pid);

    return arr;
};
a2DataGrid.prototype.checkChildRed_ = function (pid) { // 判断对应节点的子节点里面是否有与地铁图关联的节点存在
    var ret = false, self = this;

    function getC_(_pid) {
        var _a = self.ftos[_pid], _n, i = 0, red_;
        for (; _n = _a[i]; ++i) {
            red_ = self.getTaskFieldData(_n, 'strPointId');
            if (typeof red_ != 'undefined') {
                ret = true;
                break;
            } else {
                getC_(_n);
            }
        }
    }
    getC_(pid);

    return ret;
}
function __setParentImg(parentid) {
    var myo = $("#tr" + parentid), _img = $("#img" + parentid);
    _img.css("visibility", "visible");
    if (myo.attr("cp") == 0) _img.trigger("click");
    myo.find("td:eq(1)").css("font-weight", "bold");
}
a2DataGrid.prototype.insertRows = function (data, frommenu, pos, curTaskId) { // 创建任务

    var cell = this.curCell, isInsert = false, lkexp = false, lvl = 0,
        ao = curTaskId, pid = 0;
    if (frommenu && cell.tid > 0) {
        isInsert = true;
    }
    if (pos === 0 || curTaskId === 0) {
        lkexp = true;
    } else if (isInsert) {
        var _img = $("#img" + curTaskId), pad = parseInt(_img.parent().css("padding-left"), 10) || 0;
        lvl = pad / 16; pid = this.stof[curTaskId];
        if (pos === 2) { lvl += 1; pid = curTaskId; }
        var ids = this.getIDs_(curTaskId);
        if (ids.length > 1) {
            ao = ids[ids.length - 1];
        }
    }
    this.renderRows([data.taskInfo || 0], lkexp, pid, lvl, ao);
    if (pos === 2 && curTaskId > 0) __setParentImg(curTaskId);
};
a2DataGrid.prototype.delTasks = function () { // 删除任务
    var tid, self = this, ids = "", f = [], relConDiagId = -1, flag = false, exMsg = "";
    for (tid in self.selRows) {
        relConDiagId = self.getTaskFieldData(tid, 'strPointId');
        if (!relConDiagId || relConDiagId == -1) {
            if (!self.checkChildRed_(tid)) {
                ids += tid + ",";
                f.push(tid);
            } else {
                flag = true;
            }
        } else {
            flag = true;
        }
    }

    if (flag) {
        exMsg = "";
    }

    if (confirm("确定删除选中的任务吗？" + exMsg)) {
        if (f.length == 0) {
            return;
        }
        this.grid.mask("数据处理中，请稍候...");
        $.ajax({
            type: 'POST',
            url: commApi.delTask, data: { ids: ids },
            success: function (rs) {
                if (rs.Success) {
                    self.deleteRows(f);
                    self.grid.unmask();
                } else {
                    $.tmsg("m_task", rs.Message, { infotype: 2 });
                }
            }
        });
    }
};
a2DataGrid.prototype.deleteRows = function (f) { // 删除选中的任务
    var i = 0, tid, removeIds = [], togids = [], self = this, _tr;
    f.reverse();
    for (; tid = f[i]; ++i) {
        dodel(tid, tid);
    }

    function dodel(taskId, parentId) {
        var _pid, _tid, j = 0, ids = [];
        _pid = self.stof[taskId];
        removeIds.push(taskId);
        if (_pid && parentId != _pid) {
            ids = self.ftos[_pid]; // pid: [tid1, tid2...]
            ids.remove(taskId);
            delete self.stof[taskId];
            if (ids.length === 0) togids.push(_pid);
        }
        ids = self.ftos[taskId];
        ids.reverse();
        for (; _tid = ids[j]; ++j) {
            dodel(_tid, taskId);
        }
    }

    for (i = 0; tid = removeIds[i]; ++i) {
        _tr = $("#tr" + tid);
        if (_tr[0]) _tr.remove();
        self.iRowCount--;
    }
    for (i = 0; tid = togids[i]; ++i) { // 移除折叠
        _tr = $("#tr" + tid);
        if (_tr[0]) $("#img" + tid).css("visibility", "hidden");
    }

    this.focusDiv.hide();
    this.reOrder();
};
a2DataGrid.prototype.reOrder = function (incd) { // reOrder
    var k = 1;
    this.dt.find("div.datagrid-cell-rownumber").each(function (i) {
        if (incd) {
            this.innerHTML = String(k); k++;
        } else if (this.parentNode.parentNode.style.display != 'none') {
            this.innerHTML = String(k); k++;
        }
    });
};
function getbgc(obj) {
    return (function () {
        return rgb2hex(obj.style.backgroundColor);
    })();
}
a2DataGrid.prototype.onmd = function (tag, _idx) {
    this.curCell = {
        td: tag,
        rowindex: tag.parentNode.rowIndex - 1,
        colindex: _idx,
        rowspan: tag.rowSpan, colspan: tag.colSpan,
        tid: tag.parentNode.id.replace("tr", "") // 任务id
    };
    if (this.config.keynav) this.config.keynav.setxy(_idx, tag.parentNode.rowIndex - 2, this.dt, this);
};
function getTd(o) {
    var _tgn = o.tagName.toLowerCase(), tag = o;
    if (_tgn == 'a' || _tgn == 'div' || _tgn == 'img' || _tgn == 'span') {
        tag = tag.parentNode;
        if ((_tgn != 'div') && tag.tagName.toLowerCase() != 'td') tag = tag.parentNode;
    }
    return tag;
}
a2DataGrid.prototype.initEv = function () {
    var self = this, focusDiv = this.focusDiv, _menu = $("#wkmenu");
    this.dt.find("tr").unbind(".wkgrid").bind("mouseenter.wkgrid", function () {
        if (getbgc(this) != selbg) this.style.backgroundColor = hoverbg;
    }).bind("mouseleave.wkgrid", function () {
        if (getbgc(this) == hoverbg) this.style.backgroundColor = '';
    });
    this.dt.unbind(".wkgrid").bind("mousedown.wkgrid", function (ev) {
        var tag = ev.target, _idx, _tgn = tag.tagName.toLowerCase();
        if (_tgn == 'img' && tag.name != "cilist" && tag.parentNode.tagName.toLowerCase() != 'td') {
            self.ec(tag);
        }
        tag = getTd(tag);
        _idx = $(tag).index();
        if (self.isEdit) { // 编辑
            var ck = self.checkSave();
            if (!ck) return false;
        }
        self.inputDiv.hide();
        $("#wcalendar").hide();
        self.onmd(tag, _idx);
        ev.preventDefault();
    }).bind("contextmenu.wkgrid", function (e) {
        if (self.isEdit) return false;
        var ccell = self.curCell, tag = ccell.td, $tag, hasSelected, _idx, _tr;
        self.menu(e);
        if (tag.tagName.toLowerCase() == 'td') {
            $tag = $(tag); _tr = tag.parentNode; _idx = $tag.index();
            hasSelected = (getbgc(_tr) == selbg);
            if (!hasSelected) self.unSelRows();
            self.SelRow_(_tr, ccell.tid);
            if (_idx == 0) {
                self.focusDiv.hide();
            } else {
                self.upFocusDiv();
            }
        }
        _menu.find(".menu-appro").removeAttr("disabled");

        return false;
    }).bind("click.wkgrid", function (ev) {
        var ccell = self.curCell, tag = ccell.td, _tgn = tag.tagName.toLowerCase(),
            $tag = $(tag), _idx = $tag.index(), _tr = tag.parentNode, _tid = ccell.tid;
        if (self.isEdit) { // 编辑
            var ck = self.checkSave();
            if (!ck) return false;
        }
        if (tag.tagName.toLowerCase() == 'td') {
            if (ev.ctrlKey || ev.shiftKey || self.setRelations || self.exempt) {
                if (!ev.shiftKey) {
                    if (getbgc(_tr) == selbg) {
                        self.unSelRow_(_tr, _tid);
                    } else {
                        self.shiftSel['s'] = ccell.rowindex;
                        self.SelRow_(_tr, _tid);
                    }
                    self.shiftSel['sels'] = {};
                } else {
                    if (self.shiftSel['s'] === -1) self.shiftSel['s'] = ccell.rowindex;
                    self.shiftSel_(ccell.rowindex);
                }
                if (self.setRelations) taskRelations.setRS(self);
            } else {
                self.unSelRows();
                self.shiftSel['s'] = ccell.rowindex;
                self.SelRow_(_tr, _tid);
            }
        }
        if (_idx == 0) {
            self.focusDiv.hide();
        } else {
            self.upFocusDiv();
        }
    }).bind("dblclick.wkgrid", function (ev) {
        var cell = self.curCell, tid, ownerUserId;
        if (!cell) return;
        tid = cell.tid; ownerUserId = self.getTaskFieldData(tid, 'ownerUserId');
        if (!self.hasRight) {
            if (ownerUserId != self._userId) {
                return false;
            }
        }
        if (self.isEdit) return false;
        var tag = $(getTd(ev.target));
        self.initEditor(tag);
    });
    self.initDrag_();
};
a2DataGrid.prototype.shiftSel_ = function (endRowIdx) {
    var s = this.shiftSel['s'], e, t, i, sels = this.shiftSel['sels'],
        tid, _tr, rows = this.dt[0].rows;
    if (s === -1 || !endRowIdx) return;
    e = endRowIdx - 1;
    if (s > e) {
        t = s - 1;
        s = e;
        e = t;
    }
    for (tid in sels) {
        this.unSelRow_(sels[tid], tid);
    }
    this.shiftSel['sels'] = {};
    for (i = s; i <= e; ++i) {
        _tr = rows[i];
        if (getbgc(_tr) != selbg) {
            tid = _tr.id.replace('tr', '');
            this.SelRow_(_tr, tid);
            this.shiftSel['sels'][tid] = _tr;
        }
    }
};
a2DataGrid.prototype.unSelRow_ = function (_tr, tid) {
    _tr.style.backgroundColor = ''; _tr.setAttribute("isSel", "0");
    delete this.selRows[tid];
};
a2DataGrid.prototype.SelRow_ = function (_tr, tid) {
    _tr.style.backgroundColor = selbg; _tr.setAttribute("isSel", "1");
    this.selRows[tid] = _tr;
};
a2DataGrid.prototype.unSelRows = function () { // 取消多行选中
    var tid, _tr;
    for (tid in this.selRows) {
        _tr = this.selRows[tid];
        if (_tr) {
            this.unSelRow_(_tr, tid);
        }
    }
    this.shiftSel = { 's': -1, 'sels': {} };
};
a2DataGrid.prototype.initDrag_ = function () {
    if (!this.hasRight) return;
    var moveid, tomoveid, movetype, self = this, trs = this.dt.find("tr"),
        _accp = "td", _1b, isOver = false, _1c, _top, _obj;
    trs.find("td:eq(1)").draggable("destroy").droppable("destroy");
    trs.find("td:eq(1)").draggable({
        helper: function (event) {
            var _proxy = $('<div class="dp1"></div>').html($(event.target).text()).appendTo(document.body);
            return _proxy;
        },
        cursor: 'pointer', addClasses: false,
        cursorAt: { top: 0, left: 0 },
        revert: false, distance: 5, scroll: true,
        start: function (e, ui) {
            if (self.isEdit) return false;
            isOver = false; self.focusDiv.hide();
            _1b = e.pageY;
        },
        drag: function (e, ui) {
            _1b = e.pageY;
            if (isOver && _obj[0]) {
                if (_1b > _top + (_1c - _top) / 2) {
                    if (_1c - _1b < 5) {
                        indicator1.css({ top: _1c - 1 }).show();
                        _obj.find("div:last")[0].style.backgroundColor = '';
                        movetype = "next";
                    } else {
                        _obj.find("div:last")[0].style.backgroundColor = '#009933';
                        movetype = "child";
                        indicator1.hide();
                    }
                } else {
                    if (_1b - _top < 5) {
                        indicator1.css({ top: _top - 1 }).show();
                        _obj.find("div:last")[0].style.backgroundColor = '';
                        movetype = "pre";
                    } else {
                        _obj.find("div:last")[0].style.backgroundColor = '#009933';
                        movetype = "child";
                        indicator1.hide();
                    }
                }
            }
        },
        stop: function () {
        }
    }).droppable({
        accept: _accp, tolerance: 'pointer', addClasses: false,
        over: function (e, ui) {
            _obj = $(this);
            var offs = _obj.offset();
            _top = offs.top;
            _1c = _top + _obj.outerHeight();
            indicator1.css({ left: offs.left, width: _obj.outerWidth() });
            isOver = true;
        },
        out: function (e, ui) {
            isOver = false;
            indicator1.hide();
            $(this).find("div:last")[0].style.backgroundColor = '';
        },
        drop: function (e, ui) {
            var tid = this.parentNode.id.replace("tr", ""), sf = this,
                cell = self.curCell, curId = cell.tid || 0;
            if (curId != tid) {
                if (confirm('确定移动到当前节点吗?')) {
                    self.moveTask_(curId, tid, movetype);
                    _clear();
                }
            }
            function _clear() {
                isOver = false;
                indicator1.hide();
                $(sf).find("div:last")[0].style.backgroundColor = '';
            }
        }
    });
};
a2DataGrid.prototype.showicon_ = function (pid) {
    var _ids = this.getIDs_(pid), myo = $("#tr" + pid), _img = $("#img" + pid);
    if (_ids.length == 1) {
        _img.css("visibility", "hidden");
        myo.find("td:eq(1)").css("font-weight", "normal");
    }
};
a2DataGrid.prototype.isAncestry_ = function (tomoveid, anc) {
    var __pid = parseInt(this.stof[tomoveid], 10);
    if (__pid > 0 && anc != __pid) {
        return this.isAncestry_(String(__pid), anc);
    } else {
        return __pid == anc;
    }
};
a2DataGrid.prototype.reCalcPadding_ = function (parentid, trid) {
    var x = 0;
    if (parentid != 0) {
        x = parseInt($("#tr" + parentid).find("td:eq(1)").find("div:first").css("padding-left"), 10);
    }
    x = x / 16;
    var arr = this.getIDs_(trid), bs = 1, xx, x1, c;
    xx = $("#tr" + trid).find("td:eq(1)").find("div:first");
    x1 = parseInt(xx.css("paddingLeft"), 10);
    x1 = x1 / 16;
    c = x > 0 ? x1 - x - 1 : (parentid == 0 ? x1 : x1 - 1);
    $.each(arr, function (i, n) {
        xx = $("#tr" + n).find("td:eq(1)").find("div:first");
        x1 = parseInt(xx.css("paddingLeft"), 10);
        x1 = x1 / 16;
        xx.css("paddingLeft", 16 * (x1 - c) + 'px');
    });
};
a2DataGrid.prototype.moveTask_ = function (moveid, tomoveid, type) {
    var self = this, ids = self.getIDs_(moveid),
        _pid = self.stof[moveid],
        parentid = type == "child" ? tomoveid : self.stof[tomoveid],
        _istop = self.isAncestry_(tomoveid, moveid);

    if (type == "child" && parentid == _pid) return false;
    if (_istop) {
        $.tmsg("m_task", "父节点不能移到子节点上！", { infotype: 2 });
        return false;
    }

    $.post(commApi.moveTask, { tbName: 'XH_DesignManage_Task', moveId: moveid, moveToId: tomoveid, type: type }, function (data) {
        if (data.Success) {
            $.tmsg("m_task", "操作成功！", { infotype: 1 });
            if (parentid > 0) {
                __setParentImg(parentid);
            }
            var toids = self.getIDs_(tomoveid), i = 0, toobj;
            toids = $.grep(toids, function (n, i) {
                return ids.indexOf(n) == -1;
            });
            if (type != 'pre') ids = ids.reverse();
            for (; i < ids.length; ++i) {
                if (type == "pre") {
                    $("#tr" + ids[i]).insertBefore($("#tr" + tomoveid));
                } else {
                    toobj = $("#tr" + toids[toids.length - 1]);
                    $("#tr" + ids[i]).insertAfter(toobj);
                }
            }
            self.stof[moveid] = parentid;
            if (self.ftos[parentid].indexOf(moveid) == -1) self.ftos[parentid].push(moveid);
            self.ftos[_pid].remove(moveid);

            self.reCalcPadding_(parentid, moveid);
            self.reOrder();
            self.showicon_(_pid);
        } else {
            $.tmsg("m_task", data.Message, { infotype: 2 });
        }
    }, 'json');
};
a2DataGrid.prototype.ec = function (img) { // 展开折叠
    var _pading = img.parentNode, tr = _pading.parentNode.parentNode, self = this,
        pid = tr.id.replace("tr", ""), cp = tr.getAttribute("cp");

    if (cp == '1') {
        tr.setAttribute("cp", "0"); cp = 0;
        img.setAttribute("src", epcl[1]);
    } else {
        tr.setAttribute("cp", "1"); cp = 1;
        img.setAttribute("src", epcl[0]);
    }
    togec(pid, cp);

    function togec(parentId, _cpflag) {
        var ids, i = 0, tid, _tr, _cp;
        ids = self.ftos[parentId] || []; // pid: [tid1, tid2, tid3]
        for (; tid = ids[i]; ++i) {
            _tr = document.getElementById('tr' + tid);
            _cp = _tr.getAttribute("cp");
            if (_cpflag == 0) { // 折叠
                _tr.style.display = 'none';
                togec(tid, 0);
            } else {
                _tr.style.display = '';
                if (_cp == '1') togec(tid, 1);
            }
        }
    }
    return false;
};
a2DataGrid.prototype.menu = function (e) { // menu
    var self = this, cell = self.curCell, _ci = cell.colindex,
        _menu = $("#wkmenu"), dx = 206;

    var nodel = false;

    var tid = cell.tid, ownerUserId = self.getTaskFieldData(tid, 'ownerUserId');

    if ($.trim($("tr[id=tr" + tid + "]").find("td:eq(6)").html()) != "四级计划" && $.trim($("tr[id=tr" + tid + "]").find("td:eq(6)").html()) != "") {
        nodel = true;
    }

    if (!self.hasRight) {
        if (ownerUserId != self._userId) {
            return false;
        }
    }

    _menu.menu({ left: e.pageX, top: e.pageY,
        onClick: function (_nd) {
        },
        onShow: function () {

        }
    });
    _menu.find(".menu-insertTask").unbind("click").bind("click", function () {
        if (self.canDel || self.hasRight) {
            self.createTask.call(self, true, 1);
        } else {
            $.tmsg("power", "你没有权限进行此操作");
        }
        _menu.hide();
    });
    _menu.find(".menu-insertsubTask").unbind("click").bind("click", function () {
        if (self.canDel || self.hasRight) {
            self.createTask.call(self, true, 2);
        } else {
            $.tmsg("power", "你没有权限进行此操作");
        }
        _menu.hide();
    });
    _menu.find(".menu-jfw").unbind("click").bind("click", function () {
        var _ids = '';
        for (var tid in self.selRows) {
            _ids = tid;
            break;
        }
        if (_ids === '') return false;
        if (typeof PatternEdit != 'undefined' && $.isFunction(PatternEdit)) {
            PatternEdit(_ids, self.projId);
        }
        _menu.hide();
    });
    _menu.find(".menu-fzr").unbind("click").bind("click", function (ev) {
        self.batchFuze.call(self, ev); _menu.hide();
    });
    _menu.find(".menu-delTask").unbind("click").bind("click", function () {
        if (self.canDel || self.hasRight) {
            self.delTasks.call(self);
        } else {
            $.tmsg("power", "你没有权限进行此操作");
        }
        _menu.hide();
    });
    _menu.find(".menu-remark").unbind("click").bind("click", function () {
        self.addRemark.call(self); _menu.hide();
    });
    _menu.find(".menu-appro").unbind("click").bind("click", function () {console.log(apprSet);
        apprSet.init.call(self, cell.tid); _menu.hide();
    });

    _menu.find(".menu-preTask").unbind("click").bind("click", function () {
        self.preTask.call(self); _menu.hide();
    });

    if (nodel == true) {
        _menu.find(".menu-delTask").hide();

    } else {
        _menu.find(".menu-delTask").show();
    }

    if (_isexpLibId) dx = 206;
    if ($(document).scrollTop() + YH.dom.getViewportHeight() < YH.dom.getMouseY(e) + 230) {
        _menu.css("top", $(document).scrollTop() + YH.dom.getViewportHeight() - dx);
    }
};
a2DataGrid.prototype.batchFuze = function (ev) { // 批量设置负责人
    var x = YH.dom.getMouseX(ev), y = YH.dom.getMouseY(ev),
        ids = [], rows = this.dt[0].rows, self = this, tid;
    for (tid in self.selRows) {
        ids.push(tid);
    }
    if (ids.length === 0) return false;

    //this.grid.mask("处理中，请稍候...");
    getTaskPeopleListForOneClick(x, y, projId, 2, function (_userId, _profId, _userName) {
        $.ajax({
            type: 'POST', url: commApi.batchFzr, data: { taskIds: ids.join(','), userId: _userId, profId: _profId },
            success: function (data) {
                if (data.Success) {
                    $.tmsg("m_user", "设置成功！", { infotype: 1 });
                    var i = 0, _id, _td;
                    for (; _id = ids[i]; ++i) {
                        self.updateFieldData(_id, 'ownerName', _userName);
                        self.updateFieldData(_id, 'ownerUserId', _userId);
                        self.updateFieldData(_id, 'ownerProfId', _profId);
                        //self.updateView(_id, 'ownerName', _userName);
                        _td = document.getElementById('tr' + _id);
                        _td = _td.childNodes;
                        if (_td[2]) $(_td[2]).html(_userName);
                    }
                } else {
                    $.tmsg("m_user", data.Message, { infotype: 2 });
                }
                //self.grid.unmask();
            }
        });

    });
};
a2DataGrid.prototype.getCell = function (row, col) { // 根据行列获取cell dom
    if (row < 0 || row > this.iRowCount - 1) return false;
    var r = this.dt.find("tr").eq(row);
    return (r.find("td").get(col));
};
a2DataGrid.prototype.initEditor = function (tag) {
    var ctd = tag || $(this.curCell.td), of = ctd.position(), l = of.left - 2, t = of.top - 2, w = ctd.outerWidth(), h = ctd.outerHeight();
    this.focusDiv.hide();
    this.showEditor(l, t, w, h);
};

function nodeFormatter(value) {
    var str = "", i = 0, isck = false;
    for (; i < zhhynode.length; i++) {
        if (value == zhhynode[i].nodeid) isck = true;
        str += '<option value="' + zhhynode[i].nodeid + '"' + (isck ? " selected" : "") + '>' + zhhynode[i].nodename + '</option>';
        isck = false;
    }
    return str;
}

a2DataGrid.prototype.showEditor = function (l, t, w, h) { // 编辑单元格
    var cell = this.curCell, td = cell.td, cellindex = cell.colindex, _col = this.columns[cellindex],
		_editor, inputfiled = null, self = this, td = $(td), _txt, relConDiagId;
    if (_col.editor) {
        _txt = self.getTaskFieldData(cell.tid, _col.field);
        relConDiagId = self.getTaskFieldData(cell.tid, 'relConDiagId');
        if (_col.field == 'name' && relConDiagId > 0) {
            //$.tmsg('m_unedt', '与脉络图关联的节点的名称无法编辑！', { infotype: 3 });
            //return false;
        }
        self.isEdit = true;
        _editor = $.isPlainObject(_col.editor) ? _col.editor.type : _col.editor;
        inputfiled = $("<input/>").addClass("inputField").css({ width: w - 2, height: 22 + 'px', 'line-height': 22 + 'px' }).val(_txt);
        this.inputDiv.html(inputfiled).css({ left: l, top: t, width: w, height: h, display: 'block' }).focus();
        if (_editor != "combobox") inputfiled.focus();
        switch (_editor) {
            case "multiLineText":
                inputfiled = $("<textarea/>").addClass("txtarea").css({ width: w - 2, height: h - 2 });
                this.inputDiv.html(inputfiled);
                inputfiled.wkelastic().val(_txt);
                DGUntil.setCursorPosition(inputfiled[0], 0, inputfiled.val().length);
                break;
            case "combobox":
                inputfiled = $("<select/>").css({ width: td.width(), border: 'none' });
                var formatter = _col.formatter(self.getTaskFieldData(cell.tid, _col.field)) || "", _mh; inputfiled.html(formatter);
                this.inputDiv.html(inputfiled); inputfiled.focus();
                break;
            case "singleLineText":
                DGUntil.setCursorPosition(inputfiled[0], 0, inputfiled.val().length);
                if (_col.field == 'ownerName') {
                    var up = $('<input type="hidden" class="data-uid" /><input type="hidden" class="data-profid" />'), _o1, _o2;
                    this.inputDiv.append(up);
                    _o1 = this.inputDiv.find("input.data-uid"); _o2 = this.inputDiv.find("input.data-profid");
                    bindASearchableInput(inputfiled, _o1, _o2, this.projId, 2, false, false);
                }
                break;
            case "numberbox":
                DGUntil.setCursorPosition(inputfiled[0], 0, inputfiled.val().length);
                inputfiled.wknumberbox("",'.');
                break;
            case "dummy":
                if (_col.field == "taskRelations") { // 前置任务
                    inputfiled.hide();
                    this.preTask();
                }
                break;
        }
        if (_editor != "multiLineText" && _editor != "dummy") inputfiled.stcenter(h);
        _editor != "dummy" && inputfiled.keydown(function (ev) {
            var keyCode = ev.keyCode || ev.which;
            if ($(this).hasClass("wanted") && $.trim($(this).val() + String.fromCharCode(keyCode)) != "") $(this).removeClass("wanted");
            if (_editor == 'combobox') {
                ev.preventDefault();
                ev.stopPropagation();
            }
            if (keyCode == 13) {
                return self.checkSave();
            } else if (keyCode == 27) { // ESC
                self.cancelEdit();
            }
        }).blur(function (ev) {
            if (_col.field == 'ownerName') {
                return false;
            }
            self.checkSave();
        });
    }
};
a2DataGrid.prototype.checkSave = function () {
    var ck = this.checkField();
    if (!ck.success) {
        $.tmsg("errs", ck.err);
        return false;
    } else {
        this.saveData(ck); // 任务保存处理
    }
    return true;
};
a2DataGrid.prototype.checkField = function () { // 保存前检查
    var self = this, cell = this.curCell, td = cell.td, cellindex = cell.colindex, _col = this.columns[cellindex],
		_editor, inputfield = null, td = $(td), isobj = $.isPlainObject(_col.editor), flag = true, err = "", val, data = {}, modified = false, rtype = 'task', result = '', _taskId = cell.tid; // rtype: 'task' 'formula'
    _editor = isobj ? _col.editor.type : _col.editor;
    if (_editor == "multiLineText") {
        inputfield = this.inputDiv.find("textarea");
    } else if (_editor == "dummy") {
        inputfield = this.inputDiv.find("select");
    } else if (_editor == "combobox") {
        inputfield = this.inputDiv.find("select");
    } else {
        inputfield = this.inputDiv.find("input");
    }
    if (isobj && _col.editor.required) {
        inputfield.removeClass("wanted");
        if ($.trim(inputfield.val()) == "") {
            inputfield.addClass("wanted");
            flag = false; err = "请填写 " + _col.title;
        }
    }
    if (flag) {
        if (_editor == 'combobox') {
            var _select = inputfield.find("option:selected");
            result = _select.text();
            if (_col.field == 'levelId' || _col.field == 'nodeTypeId') {
                data.plvl = _select.val();
            }
            modified = self.checkModified(_taskId, _col.field, _select.val());
        } else {
            result = $.trim(inputfield.val());
            if (_col.field == 'ownerName') { // 负责人 用户id，专业id
                var uid = self.inputDiv.find("input.data-uid").val(), profid = self.inputDiv.find("input.data-profid").val();
                if (uid/* && profid*/) {
                    modified = self.checkModified(_taskId, _col.field, { tfzr_uid: uid, tfzr_profid: profid });
                    data.fzr = { uid: uid, profid: profid };
                } else {
                    var _name = self.getTaskFieldData(_taskId, 'name');
                    if (result == '' && _name != '') {
                        modified = true; data.fzr = { uid: -1, profid: 0 };
                    } else {
                        modified = false;
                    }
                }
            } else {
                modified = self.checkModified(_taskId, _col.field, result);
            }
        }
    }
    return { success: flag, err: err, taskId: _taskId, field: _col.field, result: result, type: rtype, modified: modified, data: data };
};
a2DataGrid.prototype.checkModified = function (taskId, field, data) { // 判断字段修改否
    var flag = false, taskData = {};
    taskData = this.getTaskData(taskId);

    if (field == 'ownerName') { // 负责人判断，依据用户id，专业id
        if (data.tfzr_uid != taskData["ownerUserId"] || data.tfzr_profid != taskData["ownerProfId"]) flag = true;
    } else {
        if (data != taskData[field]) flag = true;
    }
    return flag;
};
a2DataGrid.prototype.saveData = function (data) { // 保存 任务数据
    if (data.modified) {
        var ckdiff, needChgPeriod = false;

        // 任务数据
        var dt = data.result;
        if (data.field == 'levelId' || data.field == 'nodeTypeId') {
            dt = data.data.plvl;
        } else if (data.field == 'status') {
            dt = data.data.stat;
        } else if (data.field == 'ownerName') {
            this.updateFieldData(data.taskId, "ownerUserId", data.data.fzr.uid);
            this.updateFieldData(data.taskId, "ownerProfId", data.data.fzr.profid);
        }
        this.updateFieldData(data.taskId, data.field, dt);

        this.saveTask(data.taskId);

        this.updateView(data.taskId, data.field, data.result);
    }
    this.cancelEdit();
};
a2DataGrid.prototype.saveTask = function (taskId, options) { // ajax task

    var Dt = this.getTaskData(taskId), postDt = {};
    postDt['name'] = Dt.name; postDt['taskId'] = Dt.taskId; postDt['needInherit'] = "0";
    postDt['levelId'] = Dt.levelId; postDt['needSplit'] = "0"; postDt['splitUserId'] = "-1";
    postDt['nodeTypeId'] = Dt.nodeTypeId; postDt['splitProfId'] = "0";
    postDt['ownerUserId'] = Dt.ownerUserId; postDt['ownerProfId'] = Dt.ownerProfId;
    postDt['designUnitName'] = Dt.designUnitName; postDt['remark'] = Dt.remark;
    postDt['totalContractAmount'] = Dt.totalContractAmount;
    postDt['tbName'] = 'XH_DesignManage_Task';
    postDt['queryStr'] = "db.XH_DesignManage_Task.distinct('_id',{'taskId':'" + taskId + "'});";
    if (typeof options != "undefined") {
        postDt['isLink'] = options.isLink;
        postDt['isInitialResult'] = options.isInitialResult;
    }
    gAjaxQueue.add({
        type: 'POST', url: commApi.saveTask, data: postDt,
        success: function (rs) {
            if (!rs.Success) {
                $.tmsg("saveTask", rs.Message, { infotype: 2 });
            } else {
                var _tr = $("#tr" + taskId);
                if (_tr.find("td.error").length) _tr.find("td.error").removeClass("error");
            }
        }
    });
};
a2DataGrid.prototype.updateView = function (taskId, field, data) { // 刷新视图
    this.updateView1(taskId, field, data);
};
a2DataGrid.prototype.updateView1 = function (taskId, c, data) { // 刷新视图1
    var field = c, _t = data, f = this.dt.find("#tr" + taskId).find("td[field='" + field + "']");
    if (field == 'name') {
        if (f.hasClass("gray")) f.removeClass("gray");
        var _url = _isexpLibId ? '<a href="javascript:;">' : '<a href="' + commApi.taskUrl + taskId + '" target="_blank">', exem = '';
        f = f.find("div:last");
        if (f.find('span')[0]) exem = '<span>' + f.find('span:first').text() + '</span>';
        if (f.find('img[name="cilist"]')[1]) exem += '<img src="/content/images/icon/ico0006.png" style="vertical-align:middle;" title="有配置清单" name="cilist" />';
        _t = _url + data.replace(/ /g, "&nbsp;") + '</a>' + exem;
    } else if (field == 'totalContractAmount') {
        var _tca = parseFloat(_t, 10);
        if (!isNaN(_tca)) {
            _t += '万元';
        } else {
            _t = '0';
        }
    }
    f.html(_t);
    this.upFocusDiv();
};
a2DataGrid.prototype.cancelEdit = function () { // 按 ESC 取消编辑
    if (this.config.keynav) this.config.keynav.isEdit = false;
    this.inputDiv.children().remove();
    this.isEdit = false;
    this.inputDiv.hide(); $("#wcalendar").hide();
    if (typeof hideAllTaskPeopleList == 'function') hideAllTaskPeopleList();
    this.focusDiv.show();
};
a2DataGrid.prototype.getTaskData = function (taskId) { // 获取单条任务数据
    var rowsindex = this.rowsindex[taskId].index, rs;
    rs = this.rows[rowsindex];
    return rs;
};
a2DataGrid.prototype.getTaskFieldData = function (taskId, field) { // 获取field数据
    var taskData = this.getTaskData(taskId);
    return taskData[field] || "";
};
a2DataGrid.prototype.updateFieldData = function (taskId, field, data) { // 更新field数据
    if (field == 'endDate' || field == 'startDate') data = data.replace(/\//g, "-");
    this.rows[this.rowsindex[taskId].index][field] = data;
};
a2DataGrid.prototype.rowIdTotaskId = function (cr) { // E1 => E15
    var cd = cr.replace(/\d/g, ""), _rindex = parseInt(cr.replace(/[seg]/ig, ""), 10) - 1;
    _rindex = this.dt.find("tr:eq(" + _rindex + ")").attr("id").replace("tr", "");
    return (cd + _rindex);
};
a2DataGrid.prototype.taskIdTorowId = function (cr) { // E15 => E1
    var cd = cr.replace(/\d/g, ""), _tid = parseInt(cr.replace(/[seg]/ig, ""), 10);
    _tid = (this.dt.find("#tr" + _tid))[0].rowIndex - 1;
    return (cd + _tid);
};
a2DataGrid.prototype.upFocusDiv = function () { // focus单元格
    var self = this, td = self.curCell.td;
    var offset = $(td).position(),
		l = offset.left - 2, t = offset.top - 2, w = $(td).outerWidth() - 2, h = $(td).outerHeight() - 1;
    this.showFocusDiv(l, t, w, h);
};
a2DataGrid.prototype.showFocusDiv = function (l, t, w, h) { // focus单元格
    this.isEdit = false;
    this.focusDiv.css({ left: l, top: t, width: w, height: h, display: 'block' }).focus();
};
a2DataGrid.prototype.importExp = function (t) { // 导入经验
    var expData = [], _secdPlanId = -1, self = this, secdPlanId = self.secdPlanId, setoffset_year, setoffset_month, setoffset_day, isIgnorePerson, isIgnoreDate, isIgnorePassed, InportPlanName, isContractPlan;
    if (t.attr("isContractPlan") == "1") {
        isContractPlan = "?isContractPlan=1";
    } else {
        isContractPlan = "";
    }

    box(commApi.importExp + secdPlanId + isContractPlan, { boxid: 'loadExp', title: '载入公司经验', contentType: 'ajax', modal: true, submit_BtnName: '确定导入', cls: 'shadow-container', width: 800,
        onLoad: function (o) {
            //o.fbox.center();
            var _o1 = o.fbox.find("#manager"), _o2 = o.fbox.find("#managerId");
            bindASearchableInput(_o1, _o2, null, secdPlanId, 2, false, false);
            o.db.find("li[secdPlanId]").each(function () {
                $(this).click(function () {
                    _secdPlanId = $(this).attr("secdPlanId");
                    o.db.find("li[secdPlanId]").removeClass("libg");
                    $(this).addClass("libg");
                    o.fbox.mask("loading...");
                    $.ajax({
                        url: commApi.iExpLoad, data: { secdPlanId: _secdPlanId }, cache: false,
                        complete: function () { o.fbox.unmask(); },
                        success: function (data) {
                            expData = data;
                            if ($.isArray(data)) {
                                data = data || [];
                            } else {
                                data = data.jsonGroupTaskList;
                            }
                            var cc = [], tpl = commApi.tplv.join(""), ct = 0;
                            function rd(pid, lv) {
                                var dt, i = 0, _tid, _pid, tmp;
                                for (; dt = data[i]; ++i) {
                                    _tid = dt.taskId; _pid = dt.nodePid;
                                    if (_pid === pid) {
                                        ct++;
                                        dt = self.calcrs_(dt, lv);
                                        dt["rowindex"] = ct;
                                        cc.push(tpl.compileTpl(dt));
                                        rd(_tid, lv + 1);
                                    }
                                }
                            }

                            rd(0, 0);
                            cc = cc.join('');
                            if (cc == "") cc = "<tr><td colspan='6'>暂无内容</td></tr>";
                            $("#viewExp").find("tbody").html(cc);
                        }
                    });
                });
            });
            $("#searchexp").click(function () {
                var compname = $.trim(o.db.find("input[name='companyname']").val()),
					compexp = $.trim(o.db.find("input[name='companyexp']").val()), lastobj;
                o.db.find(".cnclor").removeClass("cnclor");
                o.db.find(".expbg").removeClass("expbg");
                compname && o.db.find("div[data-orgname='1']").each(function () {
                    if ($(this).text().toLowerCase().indexOf(compname) != -1) {
                        $(this).addClass("cnclor");
                        lastobj = this;
                        if (compexp) {
                            $(this).next().find("li[data-expname='1']").each(function () {
                                if ($(this).text().toLowerCase().indexOf(compexp) != -1) {
                                    $(this).addClass("expbg");
                                    lastobj = this;
                                }
                            });
                        }
                    }
                });
                !compname && compexp && o.db.find("li[data-expname='1']").each(function () {
                    if ($(this).text().toLowerCase().indexOf(compexp) != -1) {
                        $(this).addClass("expbg");
                        lastobj = this;
                    }
                });
                if (lastobj) {
                    YH.dom.scrollintoContainer(lastobj, $("#exp_clist"), true, true);
                    if (lastobj.tagName.toLowerCase() == "li") $(lastobj).trigger("click");
                }
            });
            setoffset_year = o.db.find("input[name='OffSetYear']");
            setoffset_month = o.db.find("input[name='OffSetMonth']");
            setoffset_day = o.db.find("input[name='OffSetDay']");
            isIgnorePerson = o.db.find("input[name='isIgnorePerson']");
            isIgnoreDate = o.db.find("input[name='isIgnoreDate']");
            isIgnorePassed = o.db.find("input[name='isIgnorePassed']");
            InportPlanName = o.db.find("input[name='name']");
            setoffset_year.onumberbox(); setoffset_month.onumberbox(); setoffset_day.onumberbox();
        },
        submit_cb: function (o) {
            if (_secdPlanId == -1) {
                $.tmsg("m_loadexp", "请选择要载入的公司经验！");
                return false;
            }
            if (InportPlanName.val() == "") {
                $.tmsg("m_loadexp", "请输入计划名！");
                return false;
            }

            if (confirm("确定载入吗？")) {
                o.fbox.mask("载入中...");
                var _str = '?OffSetYear=' + setoffset_year.val() + "&OffSetMonth=" + setoffset_month.val() + "&OffSetDay=" + setoffset_day.val();

                if (!isIgnorePerson.attr("checked")) {
                    _str += "&isIgnorePerson=1";
                } else {
                    _str += "&isIgnorePerson=0";
                }
                if (!isIgnoreDate.attr("checked")) {
                    _str += "&isIgnoreDate=1";
                } else {
                    _str += "&isIgnoreDate=0";
                }
                if (isIgnorePassed.length) {
                    if (!isIgnorePassed.attr("checked")) {
                        _str += "&isIgnorePassed=1";
                    } else {
                        _str += "&isIgnorePassed=0";
                    }
                }
                _str += "&name=" + escape(InportPlanName.val());
                $.ajax({
                    url: commApi.iExpSave + _str, data: { copySecPlanId: _secdPlanId, sourceSecPlanId: self.secdPlanId }, type: 'POST',
                    complete: function () { o.fbox.unmask(); },
                    success: function (data) {
                        if (data.Success) {
                            //self.renderRows(expData, true);
                            self.resetData_();
                            self.loadData();
                            //$.tmsg("m_loadexp", "导入成功！",{infotype:1});
                        } else {
                            $.tmsg("m_loadexp", data.Message, { infotype: 2 });
                        }
                    }
                });
            }
        }
    })
};
a2DataGrid.prototype.exportExp = function () { // 导出经验
    var self = this, secdPlanId = self.secdPlanId;
    box(commApi.exportExp + secdPlanId, { boxid: 'expExp', title: '导出为公司经验', contentType: 'ajax', modal: true, submit_BtnName: '确定导出', cls: 'shadow-container', width: 700,
        onLoad: function (o) {
            $("#validStartDate").click(function () {
                WdatePicker({ maxDate: $("#validEndDate").val().replace("-", "/") });
            });
            $("#validEndDate").click(function () {
                WdatePicker({ minDate: $("#validStartDate").val().replace("-", "/") });
            });
        },
        submit_cb: function (o) {
            var _name = $.trim(o.db.find("input[name='name']").val()), _orgId = $("#orgId option:selected").val(),
                isIgnorePassed = o.db.find("input[name='isIgnorePassed']"), _str = '', dt;
            if (!_name) {
                $.tmsg("m_exp", "名称不能为空！", { infotype: 2 });
                return false;
            }
            dt = { name: _name, orgId: _orgId };
            if (isIgnorePassed.length) {
                if (!isIgnorePassed.attr("checked")) {
                    dt = { name: _name, orgId: _orgId, isIgnorePassed: '1' };
                } else {
                    dt = { name: _name, orgId: _orgId, isIgnorePassed: '0' };
                }
            }
            $.ajax({ type: 'POST',
                url: commApi.exphasexist, data: dt, cache: false,
                success: function (data) {
                    if (data.Data.success) {
                        if (confirm("已经重名 是否继续覆盖")) {
                            __saveep(o);
                        } else {
                            $.tmsg("m_exp", "换个名称吧！");
                        }
                    } else {
                        __saveep(o);
                    }
                }
            });
            function __saveep(o) {
                $("#expLibForm").validate({
                    submitHandler: function (form) {
                        var dt = $("#expLibForm").serialize(), ids = "";
                        $("#patternIdArray option").each(function () {
                            ids += $(this).val() + ",";
                        });
                        o && o.fbox.mask("导出中...请稍后...");
                        $.ajax({ type: 'POST',
                            url: commApi.saveAsExp + '?' + dt, data: { patternIds: ids },
                            complete: function () { o && o.fbox.unmask(); },
                            success: function (data) {
                                if (data.Success) {
                                    $.tmsg("m_exp", "操作成功！", { infotype: 1 });
                                    o.destroy();
                                } else {
                                    $.tmsg("m_exp", data.Message, { infotype: 2 });
                                }
                            }
                        });
                    }
                });
                $("#expLibForm").submit();
            }
            return false;
        }
    });
};

$.fn.a2DataGrid = function (config) {
    if (typeof expLibId != 'undefined') {
        if (expLibId > 0) {
            _isexpLibId = true;
        }
    }
    var l = $(this).length;
    return this.each(function () {
        new a2DataGrid($.extend({ el: $(this), cols: 10, plans: l, startindex: 1 }, config || {}));
    });
}


// 关注级别筛选
function trHide(tr, isortable) {
    if (tr.style.display == 'none') return;
    if (!isortable) {
        var td1, td2, rspan, cspan, ptd = null, l = 1, _cells = tr.cells, gid,
            canPlus = false, nexttr, prevtr, pl;
        td1 = _cells[1]; td2 = _cells[2];
        rspan = td1.getAttribute("rowSpan"); cspan = td1.getAttribute("colSpan");
        gid = td1.getAttribute("gid");

        if (td1.style.display == 'none') {
            nexttr = tr.nextSibling;
            if (!nexttr || nexttr.cells[1].getAttribute('gid') != gid) {
                prevtr = tr.previousSibling; ptd = prevtr.cells[1];
                while (prevtr && ptd.getAttribute('gid') == gid) {
                    if (prevtr.style.display != 'none') {
                        ptd = prevtr.cells[1];
                        if (!canPlus) {
                            canPlus = true; pl = prevtr;
                        }
                        if (ptd.style.display != 'none') {
                            break;
                        }
                    }
                    prevtr = prevtr.previousSibling;
                }
                if (canPlus) {
                    ptd = ptd.parentNode;
                    ptd.cells[1].setAttribute('rowSpan', pl.rowIndex - ptd.rowIndex + 1);
                }
            }
        } else if (rspan > 1) {
            nexttr = tr.nextSibling;
            while (nexttr && nexttr.cells[1].getAttribute('gid') == gid) {
                if (canPlus) l++;
                if (nexttr.style.display != 'none' && !canPlus) {
                    canPlus = true;
                    ptd = nexttr;
                }
                nexttr = nexttr.nextSibling;
            }
            if (ptd) {
                ptd = ptd.cells[1];
                ptd.setAttribute("rowSpan", l); ptd.style.display = '';
                if (cspan > 1) ptd.setAttribute("colSpan", cspan);
            }
        }

        rspan = td2.getAttribute("rowSpan"); canPlus = false; gid = td2.getAttribute("gid");
        l = 1; ptd = null;
        if (td2.style.display == 'none') {
            nexttr = tr.nextSibling;
            if (!nexttr || nexttr.cells[2].getAttribute('gid') != gid) {
                prevtr = tr.previousSibling; ptd = prevtr.cells[2];
                while (prevtr && ptd.getAttribute('gid') == gid) {
                    if (prevtr.style.display != 'none') {
                        ptd = prevtr.cells[2];
                        if (!canPlus) {
                            canPlus = true; pl = prevtr;
                        }
                        if (ptd.style.display != 'none') {
                            break;
                        }
                    }
                    prevtr = prevtr.previousSibling;
                }
                if (canPlus) {
                    ptd = ptd.parentNode;
                    ptd.cells[2].setAttribute('rowSpan', pl.rowIndex - ptd.rowIndex + 1);
                }
            }
        } else if (rspan > 1 && cspan == 1) {
            nexttr = tr.nextSibling;
            while (nexttr && nexttr.cells[2].getAttribute('gid') == gid) {
                if (canPlus) l++;
                if (nexttr.style.display != 'none' && !canPlus) {
                    canPlus = true;
                    ptd = nexttr;
                }
                nexttr = nexttr.nextSibling;
            }
            if (ptd) {
                ptd = ptd.cells[2];
                ptd.setAttribute("rowSpan", l); ptd.style.display = '';
            }
        }
    }
    tr.style.display = 'none';
}
function trShow(tr, isortable) {
    if (tr.style.display != 'none') return;
    if (!isortable) {
        var td1, td2, rspan, cspan = 1, ptd = null, l = 1,
            _cells = tr.cells, gid, prevtr, nexttr, canPlus = false;
        td1 = _cells[1]; td2 = _cells[2];
        gid = td1.getAttribute("gid");
        prevtr = tr.previousSibling;
        while (prevtr && prevtr.cells[1].getAttribute('gid') == gid) {
            if (prevtr.style.display != 'none') {
                ptd = prevtr;
                //break;
            }
            prevtr = prevtr.previousSibling;
        }

        cspan = td1.getAttribute("colSpan"); var _l1 = 1;
        //        if (ptd) {
        ////            var ocell = ptd.cells[1];
        ////            td1.style.display = 'none'; _l1 = tr.rowIndex - ptd.rowIndex + 1;
        ////            if (ocell.rowSpan < _l1) {
        ////                ocell.setAttribute("rowSpan", _l1);
        ////            }
        //        } else { // tr后面
        ////            nexttr = tr.nextSibling;
        ////            while (nexttr && nexttr.cells[1].getAttribute('gid') == gid) {
        ////                l++;
        ////                if (nexttr.style.display != 'none' && !canPlus) {
        ////                    canPlus = true; _l1 = l;
        ////                    ptd = nexttr;
        ////                }
        ////                nexttr = nexttr.nextSibling;
        ////            }
        ////            if (canPlus) _l1 += ptd.cells[1].rowSpan - 1;
        ////            td1.setAttribute("rowSpan", _l1);
        ////             td1.style.display = '';
        ////            if (canPlus) ptd.cells[1].style.display = 'none';
        //        }

        gid = td2.getAttribute("gid"); ptd = null; l = 1; canPlus = false; _l1 = 1;
        prevtr = tr.previousSibling;
        while (prevtr && prevtr.cells[2].getAttribute('gid') == gid) {
            if (prevtr.style.display != 'none') {
                ptd = prevtr;
                //break;
            }
            prevtr = prevtr.previousSibling;
        }
        //        if (ptd) {
        //            var ocell = ptd.cells[2];
        //            td2.style.display = 'none'; _l1 = tr.rowIndex - ptd.rowIndex + 1;
        //            if (ocell.rowSpan < _l1) {
        //                ocell.setAttribute("rowSpan", _l1);
        //            }
        //        } else { // tr后面
        //            nexttr = tr.nextSibling;
        //            while (nexttr && nexttr.cells[2].getAttribute('gid') == gid) {
        //                l++;
        //                if (nexttr.style.display != 'none' && !canPlus) {
        //                    canPlus = true; _l1 = l;
        //                    ptd = nexttr;
        //                }
        //                nexttr = nexttr.nextSibling;
        //            }
        //            if (cspan == 1) {
        //                if (canPlus) _l1 += ptd.cells[2].rowSpan - 1;
        //                td2.setAttribute("rowSpan", _l1);
        //                 td2.style.display = '';
        //                if (canPlus) ptd.cells[2].style.display = 'none';
        //            }
        //        }
    }
    tr.style.display = '';
}

function nodemenu(p, o) { // 关注节点筛选
    var xmenu = $("#nodemenu");
    if (!xmenu[0]) return; $("#sortmenu").hide();
    var w = xmenu.width(), h = xmenu.height(), $tag = $(o), offset = $tag.offset(), _h = $tag.height(), _w = $tag.width(), _t = offset.top, _l = offset.left, tbody = p.dt[0], _idx = $tag.index(), str = "";
    if (xmenu.find("li").length == 3 && zhhynode) {
        var _arr = [];
        $.each(zhhynode, function (i, n) {
            if (n.nodeid != '-1') _arr.push('<li><input type="checkbox" name="nodegrps" value="' + n.nodename + '" />' + n.nodename + '</li>');
        });
        $(_arr.join("")).insertAfter(xmenu.find("li:first"));
    }
    xmenu.find("input[type='checkbox']").unbind("click").bind("click", function () {
        if ($(this).parent().index() == 0) {
            if ($(this).attr("checked")) {
                xmenu.find("input[type='checkbox']").attr("checked", "checked");
            } else {
                xmenu.find("input[type='checkbox']").removeAttr("checked");
            }
        } else {
            if (!$(this).attr("checked")) xmenu.find("input[type='checkbox']:first").removeAttr("checked");
        }
    });

    if ($(document).scrollTop() + YH.dom.getViewportHeight() < _t + h) {
        _t = $(document).scrollTop() + YH.dom.getViewportHeight() - h - _h;
    }
    xmenu.css({ left: _l - w + _w, top: _t + _h + 6 }).show();
    $("#confirmnode").unbind("click").bind("click", function () {
        str = "|YYY|";
        xmenu.find("input[type='checkbox']").each(function () {
            if ($(this).attr("checked")) str += $(this).val() + "|YYY|";
        });
        doFilternode(tbody, str, _idx, p.sortable);
        p.reOrder.call(p); p.focusDiv.hide();
    });
    $("#cancellvlnode").unbind("click").bind("click", function () {
        xmenu.hide();
    });
}

function doFilternode(tbody, str, idx, isortable) {
    $("#nodemenu").hide();
    var tr, node, i = 0, _rows = tbody.rows;
    for (; tr = _rows[i]; ++i) {
        node = tr.cells[idx].innerHTML;
        if (str == "|YYY|" || str.indexOf("|YYY|g_gall|YYY|") != -1) {
            trShow(tr, isortable);
        } else {
            if (node != "") {
                node = "|YYY|" + node + "|YYY|";
                if (str.indexOf(node) == -1) { // 移除
                    trHide(tr);
                } else if (str.indexOf(node) != -1) {
                    trShow(tr, isortable);
                }
            } else {
                if (str.indexOf("|YYY|g_gblank|YYY|") != -1) {
                    trShow(tr, isortable);
                } else {
                    trHide(tr);
                }
            }
        }
    }
}

function statusmenu(p, o) { // 关注状态筛选
    var xmenu = $("#statusmenu");
    if (!xmenu[0]) return; $("#sortmenu").hide();
    var w = xmenu.width(), h = xmenu.height(), $tag = $(o), offset = $tag.offset(), _h = $tag.height(), _w = $tag.width(), _t = offset.top, _l = offset.left, tbody = p.dt[0], _idx = $tag.index(), str = "";
    if (xmenu.find("li").length == 3 && zhhystatu) {
        var _arr = [];
        $.each(zhhystatu, function (i, n) {
            if (n.staid != '-1') _arr.push('<li><input type="checkbox" name="statusgrps" value="' + n.statusname + '" />' + n.statusname + '</li>');
        });
        $(_arr.join("")).insertAfter(xmenu.find("li:first"));
    }
    xmenu.find("input[type='checkbox']").unbind("click").bind("click", function () {
        if ($(this).parent().index() == 0) {
            if ($(this).attr("checked")) {
                xmenu.find("input[type='checkbox']").attr("checked", "checked");
            } else {
                xmenu.find("input[type='checkbox']").removeAttr("checked");
            }
        } else {
            if (!$(this).attr("checked")) xmenu.find("input[type='checkbox']:first").removeAttr("checked");
        }
    });

    if ($(document).scrollTop() + YH.dom.getViewportHeight() < _t + h) {
        _t = $(document).scrollTop() + YH.dom.getViewportHeight() - h - _h;
    }
    xmenu.css({ left: _l - w + _w, top: _t + _h + 6 }).show();
    $("#confirmstatus").unbind("click").bind("click", function () {
        str = "|YYY|";
        xmenu.find("input[type='checkbox']").each(function () {
            if ($(this).attr("checked")) str += $(this).val() + "|YYY|";
        });
        doFilterstatus(tbody, str, _idx, p.sortable);
        p.reOrder.call(p); p.focusDiv.hide();
    });
    $("#cancelstatus").unbind("click").bind("click", function () {
        xmenu.hide();
    });
}
function doFilterstatus(tbody, str, idx, isortable) {
    $("#statusmenu").hide();
    var tr, lvl, i = 0, _rows = tbody.rows;
    for (; tr = _rows[i]; ++i) {
        lvl = tr.cells[idx].innerHTML;
        lvl = $(lvl).attr("alt");
        if (str == "|YYY|" || str.indexOf("|YYY|g_gall|YYY|") != -1) {
            trShow(tr, isortable);
        } else {
            if (lvl != "" && typeof lvl != "undefined") {
                lvl = "|YYY|" + lvl + "|YYY|";
                if (str.indexOf(lvl) == -1) { // 移除
                    trHide(tr);
                } else if (str.indexOf(lvl) != -1) {
                    trShow(tr, isortable);
                }
            } else {
                if (str.indexOf("|YYY|g_gblank|YYY|") != -1) {
                    trShow(tr, isortable);
                } else {
                    trHide(tr);
                }
            }
        }
    }
}