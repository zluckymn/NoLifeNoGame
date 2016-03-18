/**
 * wkgrid.js 2011/5/9 by Qingbao.Zhao, depend wkpcomm.js
 */
var _isexpLibId = false;
var commApi = {
	"getPlanData": "/Projects/ProjArranged/TaskList",
	"createTasks":'/Projects/ProjArranged/QuickCreateTask', // 创建任务
    "editFormula":'/Projects/Assignment/FormulaEdit', // 编辑公式
	"delFormula":'/Projects/Assignment/DeleteFormula', // 删除公式
	"delTask": '/Projects/Assignment/DeleteTask', // 删除任务
	"saveTask": '/Projects/Assignment/TaskEdit', // 保存任务
	"saveTaskRelation":'/Projects/ProjArranged/QuickSaveTaskRelation', // 前置任务保存
    "moveTask": '/Projects/Assignment/MoveTask', // 移动任务
    "batchFzr": '/Projects/Assignment/BatchSetTaskOwner/', // 批量设置负责人
    'saveBgColor': '/Projects/Assignment/ChangeDateBgColor/', // 保存日期单元格底色
	"tpl":['<tr id="tr{taskId}" cp="1">',
		'<td field="torder" class="datagrid-td-rownumber"><div class="datagrid-cell-rownumber">{rowindex}</div></td>',
		'<td field="name" class="{cls}" width="464"><div style="padding-left:{padleft}px;padding-right:5px;float:left"><img style="{icon}" id="img{taskId}" src="/Content/Images/zh-cn/Common/ico14.jpg"></div><div style="float:left">{name}</div></td>',
        '<td field="period" style="text-align:center">{period}</td>',
        '<td field="startDate"{clrs}>{startDate}</td>',
		'<td field="endDate"{clre}>{endDate}</td>',
		'<td field="ownerName">{ownerName}</td>',
        '<td field="hasApproval" style="text-align:center">{SetApproval}</td>',
        '<td field="taskRelations" width="130" class="norbor">{pretasks}</td>',
		'</tr>']
}, saveTip = $(".savetip");

var gAjaxQueue = $.manageAjax.create('AjaxQueue', { // 针对上面commApi 编号1的
	queue:true, maxRequests:2, beforeSend: function(){saveTip.show();}, complete: function(){saveTip.hide();}
}), gLoadQueue = $.manageAjax.create('loadQueue', {
	queue:true
});

var hoverbg = '#d0e5f5', selbg = '#fcf1a9', copyDate = {planId:0, taskId:0, field:'', v:'', f:''},
    epcl = ["/Content/Images/zh-cn/Common/ico14.jpg", "/Content/Images/zh-cn/Common/ico15.jpg"];;

function a2DataGrid(config){
	this.config = config || {};
	this.grid = this.config.el;
	this.bd = this.grid.find("table:first");
	this.headcol = this.bd.find("thead"); // 表头
    this.headcol = this.headcol.find("tr:last"); // 参考表头
	this.columns = null;
	this.dt = this.bd.find("tbody"); // 数据
	this.focusDiv = this.grid.find(".focusDiv"); // focus proxy
	this.inputDiv = this.grid.find(".inputDiv"); // editor proxy
    this.colorPk = this.focusDiv.find(".curdot");
    this.selRows = {}; // 选中的行
    this.shiftSel = {'s': -1, 'sels': {}};

	this.init = function(){
		var self = this, focusDiv = this.focusDiv;
        this.resetData_();
		this.dt.unselectable(); this.colIdxs = {};
        this.setRelations = false; this.exempt = false; // 豁免操作否
		this.columns = this.getCloumns(); // 表头配置
		this.projId = this.grid.attr("data-projId"); this.secdPlanId = this.grid.attr("data-secdPlanId");

        this.colorPk.click(function() {
            var _tid = self.curCell.tid, curCol = self.colorPk.attr('cfield'), curCr, cx;
            if (curCol.indexOf('start') != -1) {
                cx = 'startDateBg';
            } else {
                cx = 'endDateBg';
            }
            curCr = self.getTaskFieldData(_tid, cx) || '';
            iColorShow(curCr, $(this), function(c) {
                self.saveColor(_tid, cx, c, curCol);
            });
        });

		focusDiv.mousedown(function(ev){
			var tag = self.curCell.td, _idx = $(tag).index();
			if(self.config.keynav) self.config.keynav.setxy(_idx, tag.parentNode.rowIndex-1, self.dt, self);
            if (_idx == 1 && (!self.isEdit || !self.setRelations || !self.exempt)) $(tag).trigger('draggable');
			ev.preventDefault();
		}).unselectable()
		.dblclick(function(){
			self.initEditor();
		}).bind("contextmenu", function(e){
            if (self.setRelations || self.exempt) return false;
			self.menu(e); var _menu = $("#wkmenu");
            _menu.find(".menu-insertTask").hide(); _menu.find(".menu-insertsubTask").hide();
            _menu.find(".menu-delTask").hide();
            if(!_isexpLibId){
                _menu.find(".menu-insertTask").show(); _menu.find(".menu-insertsubTask").show();
                _menu.find(".menu-delTask").show(); _menu.find('.menu-gjwd').hide();
            }
			return false;
		});
		this.grid.find("input.createTask").click(function(){
			self.createTask(false, 0);
		});
        this.grid.find("a.expall").click(function(){ // 全部展开
			var _rows = self.dt[0].rows, _tr, i =0;
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
		this.grid.find("a.colall").click(function(){ // 全部折叠
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
        $(document).keydown(function(e) { // copyDate = {planId:0, taskId:0, field:'', v:'', f:''}
            var keyCode = e.keyCode || e.which;
            if (e.ctrlKey) {
                var ccell = self.curCell, td, _tid, field, _v;
                if (!ccell || !ccell.td || ccell.td.tagName.toLowerCase() != 'td') return;
                td = ccell.td; _tid = ccell.tid;
                field = td.getAttribute("field");
                if (field != 'startDate' && field != 'endDate') return;
                if (keyCode == 67) { // copy
                    copyDate.planId = self.secdPlanId;
                    copyDate.taskId = _tid;
                    copyDate.field = field;
                    _v = self.getTaskFieldData(_tid, field);
                    copyDate.v = _v;
                    var _tp = field.indexOf('start') != -1 ? "S" : "E", _f = self.formula[_tp + _tid] || '';
                    copyDate.f = _f != '' ? _f : '';
                } else if (keyCode == 86) { // paste
                    if (copyDate.planId == 0 || copyDate.taskId == 0 || copyDate.field == '') return;
                    self.copyDate_();
                }
            }
        });
	}
    this.showColorpk = function() {
        var _idx = $(this.curCell.td).index(), cfield = this.columns[_idx].field, isshow;
        if (cfield === 'startDate' || cfield === 'endDate') {
            this.colorPk.show().attr('cfield', cfield);
            isshow = isColorShow();
            if (isshow) iColorHide();
        } else {
            this.colorPk.hide(); iColorHide();
        }
    };
	this.getCloumns = function(){ // 获取表头的属性配置
		var tr = this.headcol, _cols = [], self = this;
		tr.find("th").each(function(i){
			var td = $(this);
			var col = {
				title: td.text(),
				align: td.attr("align") || "left"
			};
			if(td.attr("field")){
				col.field = td.attr("field");
                self.colIdxs[col.field] = i;
			}
			if(td.attr("ccode")){
				col.ccode = td.attr("ccode");
			}
			if(td.attr("formatter")){
				col.formatter = eval(td.attr("formatter"));
			}
			if(td.attr("editor")){
				var s = $.trim(td.attr("editor"));
				if(s.substr(0, 1) == "{"){
					col.editor = eval("(" + s + ")");
				}else{
					col.editor = s;
				}
			}
			if(td.attr("rowspan")){
				col.rowspan = parseInt(td.attr("rowspan"));
			}
			if(td.attr("colspan")){
				col.colspan = parseInt(td.attr("colspan"));
			}
			if(td.attr("width")){
				col.width = parseInt(td.attr("width"));
			}
			if(td.attr("hidden")){
				col.hidden = td.attr("hidden") == "true";
			}
			_cols.push(col);
		});
		return _cols;
	}
	this.init();
	this.loadData();
}
a2DataGrid.prototype.copyDate_ = function() { // 日期粘帖
    var ccell = this.curCell, td, _tid, field, _v, self = this;
    if (!ccell || !ccell.td || ccell.td.tagName.toLowerCase() != 'td') return;
    td = ccell.td; _tid = ccell.tid;
    field = td.getAttribute("field");
    if ((field != 'startDate' && field != 'endDate') || (_tid == copyDate.taskId && copyDate.field == field)) return;
    var _tp = field.indexOf('start') != -1 ? "S" : "E", _f = copyDate.f,
        flag = false, nowc = _tp + _tid, _cr = [];
    if (_f != '') { // copy formula
        if (_f == self.formula[nowc]) return;
        _cr = _f.match(/[seg]\d{1,}/ig);
        var i = 0, fc;
        for (; fc = _cr[i]; ++i) {
            if (fc == nowc) {
                flag = true;
                break;
            }
        }
    } else if (!this.checkModified(_tid, field, copyDate.v)) {
        return;
    }
    if (flag) {
        $.tmsg("errs", '公式拷贝，不允许自己引用自己。请检查！');
        return false;
    }
    self.updateFieldData(_tid, field, copyDate.v);
    if (_f != '') {
        self.formula[nowc] = _f;
        _cr.length > 0 && self.updatefRef(_cr, nowc);
        self.saveFormula({formula:nowc, fRef:_cr, expression:_f, modified:true, mask:true});
    } else {
        var _nf = self.formula[nowc];
        if (_nf && _nf!= '') self.delFormula({formula:nowc, opera:'del'});
        /*self.formula[nowc] = "";
        for(var key in self.fRef){
            self.fRef[key].remove(nowc);
        }*/
        self.saveTask(_tid);
    }
    self.updateView(_tid, field, copyDate.v);
}
a2DataGrid.prototype.resetData_ = function() {
    this.curCell = null;
	this.formula = {}; // key:value 公式左边(单元格):公式右边
    this.fRef = {}; // key:value  对应单元格:与之相关的公式
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
    this.isEdit = false; this.editFormula = false;
}
a2DataGrid.prototype.loadData = function(){ // 加载数据
	var self = this;
	self.grid.mask("数据加载中，请稍候...");
	gLoadQueue.add({
		dataType:'JSON', cache:false,
		url: commApi.getPlanData, data:{secdPlanId:self.secdPlanId},
		success: function(rs){
			var r = eval('('+rs+')');
			self.kwFilter(); self.iRowCount = 0;
			self.renderRows(r, 0, 0, 0);
            if (self.config.afterRender) self.config.afterRender.call(self, YH.$id('tplan'+self.secdPlanId));
			self.grid.unmask();
		}
	});
}
a2DataGrid.prototype.kwFilter = function(){
	var self = this, rc = this.headcol;
    /*
	rc.find("th:eq(3)").bind("click", function(){
		sxmenu(self, this, "prev");
	});
	rc.find("th:eq(4)").bind("click", function(){
		sxmenu(self, this, "next");
	});
	rc.find("th:eq(7)").bind("click", function(){
		attmenu(self, this);
	});*/
}
a2DataGrid.prototype.calcrs_ = function(dt, lvl) {
    var i = 0, _tmp, _tid = dt['taskId'],
        _cls = "gray", clrs = '', clre = '', pretasks = '', _url;

    if (dt['name'] != "请填写任务名称") {
        _cls = "";
        if (dt['relConDiagId'] > 0) _cls = ' class="red"';
        _url = '<a href="javascript:;"'+_cls+'>';
        dt['name'] = _url + dt['name'].replace(/ /g, "&nbsp;") + '</a>';
    }
    dt['SetApproval'] = dt['hasApproval'] ? '是' : '';
    if (dt['startDateBg'] && dt['startDateBg'].indexOf('#') != -1) clrs = ' style="background-color:'+dt['startDateBg']+'"';
    if (dt['endDateBg'] && dt['endDateBg'].indexOf('#') != -1) clre = ' style="background-color:'+dt['endDateBg']+'"';
    if (dt['taskRelations'] && dt['taskRelations'].length) {
        for(; _tmp = dt['taskRelations'][i]; ++i) {
            pretasks += _tmp.relTaskIndex + _tmp.relTaskType + ',';
        }
        pretasks = pretasks.replace(/,$/g, '');
    }
    dt["pretasks"] = pretasks; dt["cls"] = _cls;
    dt['clrs'] = clrs; dt['clre'] = clre;
    dt['padleft'] = 16 * lvl;
    dt['icon'] = "cursor:pointer";
    if (dt['needSplit'] == 0) dt['spliterName'] = "";
    return dt;
}
a2DataGrid.prototype.renderRows = function(data, texp, pid, lvl, ino){ // 渲染
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
        self.rowsindex[_ex.taskId] = {index:_rowindex}; _rowindex++;
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
		this.dt.append(cc); this.reOrder();
	} else if (ino > 0) {
        $(cc).insertAfter($("#tr"+ino)); setone = true;
        this.reOrder();
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
	if(this.curCell) this.focusDiv.hide();
    f && $.each(f, function(i, n){
		cc = n.split("=");
		if(cc.length){
            var _nowC = cc[0], _cna = _nowC.charAt(0), _fd = ccodeToField[_cna],
                _tkid = _nowC.replace(/[se]/ig, '');
            self.formula[_nowC] = cc[1];
        $("#tr" + _tkid).find("td[field='"+_fd+"']").addClass('dotgreen');
		self.updatefRef(cc[1].match(/[seg]\d{1,}/ig), cc[0]);}
	});
	this.initEv();
}
a2DataGrid.prototype.createTask = function(frommenu, pos){ // 创建任务 pos: 1--创建任务，2--子任务，0--尾部插入
	var self = this, cell = self.curCell, curTaskId, nodePid = 0, cur = 0; // secdPlanId,projId,isExpLib
	if(frommenu){
		curTaskId = cell.tid; cur = curTaskId;
        if (pos == 1) {
            nodePid = curTaskId > 0 ? self.stof[curTaskId] : 0;
        } else if (pos == 2) {
            nodePid = curTaskId; curTaskId = 0;
        }
	}else{
		curTaskId = 0;
	}

	this.grid.mask("数据处理中，请稍候...");
	$.ajax({
		type:'POST',
		url:commApi.createTasks,
		data:{curTaskId:curTaskId,secdPlanId:self.secdPlanId,projId:self.projId,isExpLib:0,taskId:0,nodePid:nodePid},
		success: function(rs){
			if(rs.Success){
				self.insertRows(rs.htInfo, frommenu, pos, cur);
				self.grid.unmask();
			}else{
				$.tmsg("createtask", rs.msgError, {infotype:2});
			}
		}
	});
}
a2DataGrid.prototype.addRemark = function(){ // 添加备注
	var tpl = '<textarea style="width:390px;height:150px;"></textarea>', self = this, tid = self.curCell.tid,
		dt = self.getTaskFieldData(tid, "remark") + "";
	box(tpl, {boxid:'remark', title:'添加备注', cls:'shadow-container2', modal:true,
		onOpen:function(o){
			o.db.find("textarea").val(dt);
		},
		submit_cb:function(o){
			var newtxt = o.db.find("textarea").val();
			if(dt != newtxt){
				self.rows[self.rowsindex[tid].index]['remark'] = newtxt;
				self.saveTask(tid);
			}
		}
	});
}
a2DataGrid.prototype.getIDs_ = function(pid) { // 获取要移动、删除的taskID
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
}
function __setParentImg(parentid) {
    var myo = $("#tr" + parentid), _img = $("#img" + parentid);
    _img.css("visibility", "visible");
    if (myo.attr("cp") == 0) _img.trigger("click");
    myo.find("td:eq(1)").css("font-weight", "bold");
}
a2DataGrid.prototype.insertRows = function(data, frommenu, pos, curTaskId){ // 创建任务
	var cell = this.curCell, isInsert = false, lkexp = false, lvl = 0,
        ao = curTaskId, pid = 0;
	if(frommenu && cell.tid > 0){
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
    this.renderRows([data.taskInfo||0], lkexp, pid, lvl, ao);
    if (pos === 2 && curTaskId > 0) __setParentImg(curTaskId);
}
a2DataGrid.prototype.delTasks = function(){ // 删除任务
	var rows = this.dt[0].rows, tid, self = this, ids = "", f = [], _attr, relConDiagId = 0;
	$.each(rows, function(i, n){
		_attr = n.getAttribute("isSel");
		if(_attr && _attr == "1") {
            tid = n.id.replace("tr", "");
            relConDiagId = self.getTaskFieldData(tid, 'relConDiagId');
            if (relConDiagId == 0) {
                ids += tid + ",";
                f.push(tid);
            }
        }
	});

	if(confirm("确定删除选中的任务吗？\n（注意：与脉络图关联的节点无法删除，任务红色部分！）")){
        if (f.length == 0) return;
		this.grid.mask("数据处理中，请稍候...");
		$.ajax({
			type:'POST',
			url:commApi.delTask, data:{ ids: ids },
			success:function(rs){
				if (rs.Success){
					self.deleteRows(f);
					self.grid.unmask();
				}else{
					$.tmsg("m_task", rs.msgError,{infotype:2});
				}
			}
		});
	}
}
a2DataGrid.prototype.deleteRows = function(f){ // 删除选中的任务
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
}
a2DataGrid.prototype.reOrder = function(){ // reOrder
	var k = 1;
	this.dt.find("div.datagrid-cell-rownumber").each(function(i){
		if(this.parentNode.parentNode.style.display != 'none'){this.innerHTML = String(k); k++;}
    });
}
function kickselbg(rows){
	$.each(rows, function(i, n){
		n.style.backgroundColor = '';
		n.setAttribute("isSel", "0");
	});
}
function getbgc(obj){
	return (function(){
		return rgb2hex(obj.style.backgroundColor);
	})();
}
a2DataGrid.prototype.onmd = function(tag, _idx){
	this.curCell = {
		td:tag,
		rowindex:tag.parentNode.rowIndex-1,
		colindex:_idx,
		rowspan:tag.rowSpan, colspan:tag.colSpan,
		tid: tag.parentNode.id.replace("tr", "") // 任务id
	};
	if(this.config.keynav) this.config.keynav.setxy(_idx, tag.parentNode.rowIndex-2, this.dt, this);
}
function getTd(o) {
    var _tgn = o.tagName.toLowerCase(), tag = o;
    if (_tgn == 'a' || _tgn == 'div' || _tgn == 'img' || _tgn == 'span') {
        tag = tag.parentNode;
        if ((_tgn != 'div') && tag.tagName.toLowerCase() != 'td') tag = tag.parentNode;
    }
    return tag;
}
a2DataGrid.prototype.initEv = function(){
	var self = this, focusDiv = this.focusDiv, _menu = $("#wkmenu");
	this.dt.find("tr").unbind(".wkgrid").bind("mouseenter.wkgrid", function(){
		if(getbgc(this) != selbg) this.style.backgroundColor = hoverbg;
	}).bind("mouseleave.wkgrid", function(){
		if(getbgc(this) == hoverbg) this.style.backgroundColor = '';
	});
	this.dt.unbind(".wkgrid").bind("mousedown.wkgrid", function(ev){
		var tag = ev.target, _idx, _tgn = tag.tagName.toLowerCase();
        if (_tgn == 'img' && tag.parentNode.tagName.toLowerCase() != 'td') self.ec(tag);
        tag = getTd(tag);
        _idx = $(tag).index();
		if(self.isEdit){ // 编辑
            if(self.editFormula && self.columns[_idx].ccode){
				self.createfpDiv(tag);
				return false;
			}
			var ck = self.checkSave();
			if(!ck) return false;
		}
		self.inputDiv.hide();
		$("#wcalendar").hide();
		self.onmd(tag, _idx);
		ev.preventDefault();
	}).bind("contextmenu.wkgrid", function(e){
		if(self.isEdit || self.setRelations || self.exempt) return false;
        var ccell = self.curCell, tag = ccell.td, $tag, hasSelected, _idx, _tr;
        tag = getTd(tag);
        self.menu(e);
		if(tag.tagName.toLowerCase() == 'td'){
			$tag = $(tag); _tr = tag.parentNode; _idx = $tag.index();
			hasSelected = (getbgc(_tr) == selbg);
			if(!hasSelected) self.unSelRows();
			self.SelRow_(_tr, ccell.tid);
			if (_idx == 0) {
				self.focusDiv.hide();
			} else {
				self.upFocusDiv();
			}
		}
        _menu.find(".menu-insertTask").hide(); _menu.find(".menu-insertsubTask").hide();
        _menu.find(".menu-delTask").hide();
        if(!_isexpLibId){
            _menu.find(".menu-insertTask").show(); _menu.find(".menu-insertsubTask").show();
            _menu.find(".menu-delTask").show(); _menu.find('.menu-gjwd').hide();
        }
        _menu.find(".menu-appro").removeAttr("disabled");

		return false;
	}).bind("click.wkgrid", function(ev){
        var ccell = self.curCell, tag = ccell.td, _tgn = tag.tagName.toLowerCase(),
            $tag = $(tag), _idx = $tag.index(), _tr = tag.parentNode, _tid = ccell.tid;
		if(self.isEdit){ // 编辑
            if(self.editFormula && self.columns[_idx].ccode){
				return false;
			}
			var ck = self.checkSave();
			if(!ck) return false;
		}
		if(tag.tagName.toLowerCase() == 'td'){
            if(ev.ctrlKey || ev.shiftKey || self.setRelations || self.exempt){
                if (!ev.shiftKey) {
                    if(getbgc(_tr) == selbg){
                        self.unSelRow_(_tr, _tid);
                    }else{
                        self.shiftSel['s'] = ccell.rowindex;
                        self.SelRow_(_tr, _tid);
                    }
                    self.shiftSel['sels'] = {};
                } else {
                    if (self.shiftSel['s'] === -1) self.shiftSel['s'] = ccell.rowindex;
                    self.shiftSel_(ccell.rowindex);
                }
                if (self.setRelations) taskRelations.setRS(self);
            }else{
                self.unSelRows();
                self.shiftSel['s'] = ccell.rowindex;
                self.SelRow_(_tr, _tid);
            }
		}
		if(_idx==0){
			self.focusDiv.hide();
		}else{
			if (!self.setRelations && !self.exempt) self.upFocusDiv();
		}
        if (!self.setRelations || !self.exempt) self.showColorpk();
	}).bind("dblclick.wkgrid", function(ev){
		if(self.isEdit || self.exempt || self.setRelations) return false;
		var tag = $(getTd(ev.target));
		self.initEditor(tag);
	});
    self.initDrag_();
}
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
        if(getbgc(_tr) != selbg) {
            tid = _tr.id.replace('tr', '');
            this.SelRow_(_tr, tid);
            this.shiftSel['sels'][tid] = _tr;
        }
    }
}
a2DataGrid.prototype.unSelRow_ = function (_tr, tid) {
    _tr.style.backgroundColor = ''; _tr.setAttribute("isSel", "0");
    delete this.selRows[tid];
}
a2DataGrid.prototype.SelRow_ = function (_tr, tid) {
    _tr.style.backgroundColor = selbg; _tr.setAttribute("isSel", "1");
    this.selRows[tid] = _tr;
}
a2DataGrid.prototype.unSelRows = function() { // 取消多行选中
    var tid, _tr;
    for (tid in this.selRows) {
        _tr = this.selRows[tid];
        if (_tr) {
            this.unSelRow_(_tr, tid);
        }
    }
    this.shiftSel = {'s': -1, 'sels': {}};
}
a2DataGrid.prototype.initDrag_ = function() {
    var moveid, tomoveid, movetype, self = this, trs = this.dt.find("tr"),
        _accp = "td", _1b, isOver = false, _1c, _top, _obj;

    trs.find("td:eq(1)").draggable({
        helper: function(event){
            var _proxy = $('<div class="dp1"></div>').html($(event.target).text()).appendTo(document.body);
            return _proxy;
        },
        cursor: 'pointer', addClasses: false,
        cursorAt: {top: 0, left: 0},
        revert: false, distance: 5, scroll:true,
        start: function(e, ui) {
            if (self.isEdit || self.setRelations || self.exempt) return false;
            isOver =false; self.focusDiv.hide();
            _1b = e.pageY;
        },
        drag: function(e, ui) {
            _1b = e.pageY;
            if (isOver && _obj[0]) {
                if (_1b > _top + (_1c - _top) / 2) {
                    if (_1c - _1b < 5) {
                        indicator1.css({ top: _1c-1 }).show();
                        _obj.find("div:last")[0].style.backgroundColor = '';
                        movetype = "next";
                    } else {
                        _obj.find("div:last")[0].style.backgroundColor = '#009933';
                        movetype = "child";
                        indicator1.hide();
                    }
                } else {
                    if (_1b - _top < 5) {
                        indicator1.css({ top: _top-1 }).show();
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
        stop: function() {
        }
    }).droppable({
        accept: _accp, tolerance: 'pointer', addClasses: false,
        over: function(e, ui) {
            _obj = $(this);
            var offs = _obj.offset();
            _top = offs.top;
            _1c = _top + _obj.outerHeight();
            indicator1.css({ left: offs.left, width: _obj.outerWidth() });
            isOver = true;
        },
        out: function(e, ui) {
            isOver = false;
            indicator1.hide();
            $(this).find("div:last")[0].style.backgroundColor = '';
        },
        drop: function(e, ui) {
            var tid = this.parentNode.id.replace("tr", ""), sf = this,
                cell = self.curCell, curId = cell.tid || 0;
            if (curId != tid) {
                hiConfirm('确定移动到当前节点吗?', '请注意！', function(r) {
                    if (r) {
                        self.moveTask_(curId, tid, movetype);
                    }
                    _clear();
                });
            }
            function _clear() {
                isOver =false;
                indicator1.hide();
                $(sf).find("div:last")[0].style.backgroundColor = '';
            }
        }
    });
}
a2DataGrid.prototype.showicon_ = function(pid) {
    var _ids = this.getIDs_(pid), myo = $("#tr" + pid), _img = $("#img"+pid);
    if (_ids.length == 1) {
        _img.css("visibility", "hidden");
        myo.find("td:eq(1)").css("font-weight", "normal");
    }
}
a2DataGrid.prototype.isAncestry_ = function(tomoveid, anc) {
    var __pid = parseInt(this.stof[tomoveid], 10);
    if (__pid > 0 && anc != __pid) {
        return this.isAncestry_(String(__pid), anc);
    } else {
        return __pid == anc;
    }
}
a2DataGrid.prototype.reCalcPadding_ = function(parentid, trid) {
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
    $.each(arr, function(i, n) {
        xx = $("#tr" + n).find("td:eq(1)").find("div:first");
        x1 = parseInt(xx.css("paddingLeft"), 10);
        x1 = x1 / 16;
        xx.css("paddingLeft", 16 * (x1 - c) + 'px');
    });
}
a2DataGrid.prototype.moveTask_ = function(moveid, tomoveid, type) {
    var self = this, ids = self.getIDs_(moveid),
        _pid = self.stof[moveid],
        parentid = type == "child" ? tomoveid : self.stof[tomoveid],
        _istop = self.isAncestry_(tomoveid, moveid);

    if (type == "child" && parentid == _pid) return false;
    if (_istop) {
        $.tmsg("m_task", "父节点不能移到子节点上！", { infotype: 2 });
        return false;
    }

    $.post(commApi.moveTask, { moveId: moveid, toMoveId: tomoveid, moveType: type }, function(data) {
        if (data.Success) {
            $.tmsg("m_task", "操作成功！", { infotype: 1 });
            if (parentid > 0) {
                __setParentImg(parentid);
            }
            var toids = self.getIDs_(tomoveid), i = 0, toobj;
            toids = $.grep(toids, function(n, i) {
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
            $.tmsg("m_task", data.msgError, { infotype: 2 });
        }
    }, 'json');
}
a2DataGrid.prototype.ec = function(img) { // 展开折叠
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
}
a2DataGrid.prototype.menu = function(e){ // menu
	var self = this, cell = self.curCell, _ci = cell.colindex,
        _menu = $("#wkmenu"), dx = 264;
	_menu.menu({left: e.pageX, top: e.pageY,
		onClick: function(_nd){
		},
		onShow:function(){

		}
	});
	_menu.find(".menu-insertTask").unbind("click").bind("click", function(){
		self.createTask.call(self, true, 1); _menu.hide();
	});
    _menu.find(".menu-insertsubTask").unbind("click").bind("click", function(){
		self.createTask.call(self, true, 2); _menu.hide();
	});
	_menu.find(".menu-jfw").unbind("click").bind("click", function(){
		var _ids = [];
        for (var tid in self.selRows) {
            _ids.push(tid);
        }
        if (_ids.length === 0) return false;
        _ids = _ids.join(',');
		dtDeliverables.call(self, _ids, 0); _menu.hide();
	});
    _menu.find(".menu-fzr").unbind("click").bind("click", function(ev){
		self.batchFuze.call(self, ev); _menu.hide();
	});
	_menu.find(".menu-rtpl").unbind("click").bind("click", function(){
		opGuideL.init.call(self, cell.tid, 0, true); _menu.hide();
	});
    _menu.find(".menu-rtpl1").unbind("click").bind("click", function(){
		opGuideL.init.call(self, cell.tid, 1, true); _menu.hide();
	});
    _menu.find(".menu-rtpl2").unbind("click").bind("click", function(){
		opGuideL.init.call(self, cell.tid, 2, true); _menu.hide();
	});
	_menu.find(".menu-delTask").unbind("click").bind("click", function(){
		self.delTasks.call(self); _menu.hide();
	});
	_menu.find(".menu-remark").unbind("click").bind("click", function(){
		self.addRemark.call(self); _menu.hide();
	});
    _menu.find(".menu-appro").unbind("click").bind("click", function(){
		apprSet.init.call(self, cell.tid); _menu.hide();
	});
    _menu.find(".menu-gjwd").unbind("click").bind("click", function(){ // 维度挂接
        var wd = self.getTaskFieldData(cell.tid, 'valueId');
	    glwd.init.call(self, cell.tid, wd); _menu.hide();
	});
    if (_isexpLibId) dx = 210;
	if($(document).scrollTop()+YH.dom.getViewportHeight() < YH.dom.getMouseY(e) + 212){
		_menu.css("top", $(document).scrollTop()+YH.dom.getViewportHeight() - dx);
	}
}
a2DataGrid.prototype.batchFuze = function(ev){ // 批量设置负责人
    var x = YH.dom.getMouseX(ev), y = YH.dom.getMouseY(ev),
        ids = [], rows = this.dt[0].rows, self = this, _attr;
	$.each(rows, function(i, n){
		_attr = n.getAttribute("isSel");
		if(_attr && _attr == "1") ids.push(n.id.replace("tr", ""));
	});
    if (ids.length === 0) return false;

    //this.grid.mask("处理中，请稍候...");
    getTaskPeopleListForOneClick(x, y, projId, 2, function(_userId, _profId, _userName) {
        $.ajax({
            type: 'POST', url: commApi.batchFzr, data: { taskIds: ids.join(','), userId: _userId, profId: _profId },
            success: function(data) {
                if (data.Success) {
                    $.tmsg("m_user", "设置成功！", { infotype: 1 });
                    var i = 0, _id, _td;
                    for (; _id = ids[i]; ++i) {
                        self.updateFieldData(_id, 'ownerName', _userName);
                        self.updateFieldData(_id, 'ownerUserId', _userId);
                        self.updateFieldData(_id, 'ownerProfId', _profId);
                        _td = document.getElementById('tr' + _id);
                        _td = _td.childNodes;
                        if (_td[5]) $(_td[5]).html(_userName);
                    }
                } else {
                    $.tmsg("m_user", data.msgError, { infotype: 2 });
                }
                //self.grid.unmask();
            }
        });

    });
}
a2DataGrid.prototype.getCell = function(row, col){ // 根据行列获取cell dom
	if(row < 0 || row > this.iRowCount-1) return false;
	var r = this.dt.find("tr").eq(row);
	return (r.find("td").get(col));
}
a2DataGrid.prototype.createfpDiv = function(td, nofill){
	if(!td) return;
	var self = this, n = this.grid.find(".fpd"), n1 = this.grid.find(".fpp"), inputfield = this.inputDiv.find("input"), v, nof = false || nofill, _col = this.columns[$(td).index()];
	if(!n[0] || !n1[0]){
		n = $("<div/>").addClass("fpDiv fpd").appendTo(self.grid);
		n1 = $("<div/>").addClass("fpDiv fpp").appendTo(self.grid);
	}
	if(inputfield[0]){
		if(_col.ccode == 'G' && !/g|([+-]$)/ig.test(inputfield.val())) return false;
		var ofset = $(td).position(), l = ofset.left, t = ofset.top, w = $(td).outerWidth(), h = $(td).outerHeight(), shown = _col.ccode == 'G' ? n1 : n;
		shown.css({left:l-1, top:t-1, width:w, height:h, display:'block'});
		if(nof) return;
		v = inputfield.val(); var np = _col.ccode + (td.parentNode.rowIndex-1);
		if(_col.ccode != 'G'){
			v = v == '=' ? (v+np) : v.replace(/[se]\d{1,}|[se]/ig, np);
		}else{
			if(/g/i.test(v)){
				v = v.replace(/[g]\d{1,}|g/ig, np);
			}else if(/[+-]$/i.test(v)){
				v = v + np;
			}
		}
		inputfield.val(v).focus();
		window.setTimeout(function() {
            DGUntil.setCursorPosition(inputfield[0], v.length);
        }, 300);
	}
}
a2DataGrid.prototype.findfpDiv = function(er){
	this.hidefpDiv(er);
	if(!/[seg]\d{1,}/i.test(er)) return false;
	var arr = DGUntil.refRowindex(er), rowindex = arr[2], e = arr[1].toUpperCase(), sf = this, colindex, td;
	$.each(sf.columns, function(i,n){
		if(n.ccode && n.ccode == e){
			colindex = i;
			return false;
		}
	});
	rowindex = parseInt(rowindex, 10) - 1;
	td = this.getCell(rowindex, colindex);
	return td;
}
a2DataGrid.prototype.hidefpDiv = function(dg){
	dg = dg.toLowerCase(); var cls = 'fpd';
	if(dg == 'all'){
		cls = 'fpDiv';
	}else if(dg.indexOf('g') != -1){
		cls = 'fpp';
	}
	this.grid.find("."+cls).length && this.grid.find("."+cls).hide();
}
a2DataGrid.prototype.initEditor = function(tag){
	var ctd = tag || $(this.curCell.td), of = ctd.position(), l = of.left-2, t = of.top-2, w = ctd.outerWidth(), h = ctd.outerHeight();
	this.focusDiv.hide();
	this.showEditor(l,t,w,h);
}
a2DataGrid.prototype.showEditor = function(l,t,w,h){ // 编辑单元格
	var cell = this.curCell, td = cell.td, cellindex = cell.colindex, _col = this.columns[cellindex],
		_editor, inputfiled = null, self = this, td = $(td), _txt, relConDiagId;
	if(_col.editor){
		_txt = self.getTaskFieldData(cell.tid, _col.field);
        relConDiagId = self.getTaskFieldData(cell.tid, 'relConDiagId');
        if (_col.field == 'name' && relConDiagId > 0) {
            $.tmsg('m_unedt', '与脉络图关联的节点的名称无法编辑！');
            return false;
        }_isexpLibId
        if (_col.field == 'name' && _isexpLibId) {
            $.tmsg('m_unedt', '集团计划模板的名称暂时无法编辑！');
            return false;
        }
        if (!self.setRelations) {
		self.isEdit = true;
		_editor = $.isPlainObject(_col.editor) ? _col.editor.type : _col.editor;
        if(/[se]/i.test(_col.ccode) && self.formula[_col.ccode+cell.tid]){
			var _expr = self.formula[_col.ccode+cell.tid];
			if(_expr != ""){
				_expr = _expr.replace(/[seg]\d{1,}/ig, function(o){
					return self.taskIdTorowId(o);
				});
				_txt = "=" + _expr;
				self.editFormula = true;
			}
		}
		inputfiled = $("<input/>").addClass("inputField").css({width:w-2,height:22+'px','line-height':22+'px'}).val(_txt);
		this.inputDiv.html(inputfiled).css({left:l, top:t, width:w, height:h, display:'block'}).focus();
		if(_editor != "combobox")  inputfiled.focus();
        }
		switch(_editor){
			case "multiLineText":
				inputfiled = $("<textarea/>").addClass("txtarea").css({width:w-2,height:h-2});
				this.inputDiv.html(inputfiled);
				inputfiled.wkelastic().val(_txt);
				DGUntil.setCursorPosition(inputfiled[0], 0, inputfiled.val().length);
				break;
			case "singleLineText":
				DGUntil.setCursorPosition(inputfiled[0], 0, inputfiled.val().length);
				if(_col.field == 'ownerName'){
					var up = $('<input type="hidden" class="data-uid" /><input type="hidden" class="data-profid" />'), _o1, _o2;
					this.inputDiv.append(up);
					_o1 = this.inputDiv.find("input.data-uid"); _o2 = this.inputDiv.find("input.data-profid");
					bindASearchableInput(inputfiled, _o1, _o2, this.projId, 2, false, false);
				}
				break;
			case "Date":
				inputfiled.css("ime-mode", "disabled");
				DGUntil.setCursorPosition(inputfiled[0], inputfiled.val().length);
				var regexp = /^\d{1,4}([-\/](\d{1,2}([-\/](\d{1,2})?)?)?)?$/,
					regFormula = /^=[se](\d{1,}([+-]((g(\d{1,}([+-](\d{1,})?)?)?)|(\d{1,}([+-](g(\d{1,})?)?)?))?)?)?$/i;
				inputfiled.bind("keypress", function(e){
					var keyCode = e.keyCode || e.which;
					if(keyCode == 32) return false;
					var pos = DGUntil.getPositionForInput(this), inputstr = String.fromCharCode(keyCode), v = $(this).val(),
						st = DGUntil.getSelectionText(), pos1 = v.length, _rmsel = '';
					if(!/^=/.test(v) && inputstr == '='){ // 进入公式编辑 E15=E13+G13-20
						$(this).val(''); self.editFormula = true; $(".wcalendar").hide();
					}else if(/^=/.test(v)){
						if(st){ pos1 = v.indexOf(st); v = v.replace(st, ""); pos=pos1;}
						v = v.substr(0, pos) + inputstr + v.substr(pos);
						return regFormula.test(v);
					}else{
                        self.editFormula = false;
                        _rmsel = v.substr(0, pos).replace(st, '');
						return regexp.test(_rmsel + inputstr + v.substr(pos));
					}
				}).bind("paste dragenter", function(){
					return false;
				}).bind("click", function(){
					if(self.editFormula) return;
					var ofs = $(this).offset(), l = ofs.left, t = ofs.top, h = $(this).outerHeight(), ipt = this;
					var str = $(this).val(), _calendar1 = $("#wcalendar"), md = {el:this}, _calendar;
					_calendar1.empty();
					_calendar = $("<div style=\"margin:0;padding:0;width:180px;height:175px;line-height:20px;\"></div>").appendTo(_calendar1);
					_calendar.calendar({onSelect:function(d){
						$(ipt).val(DGUntil.dateformatter(d)).focus();
						_calendar.hide();
					},weeks:["日","一","二","三","四","五","六"],months:["1月","2月","3月","4月","5月","6月","7月","8月","9月","10月","11月","12月"],fit:true,el:ipt});
					if(DGUntil.isDate(str)) _calendar.calendar('moveTo', new Date(str.replace("-", "/")));
					_calendar.calendar("resize");
					_calendar1.css({left:l, top:t+h+1}).show();
					/*if(_col.ccode){
						if(_col.ccode == 'S'){
							md.maxDate = (self.getTaskFieldData(cell.tid, ccodeToField['E'])+"").replace("-", "/");
						}else{
							md.minDate = (self.getTaskFieldData(cell.tid, ccodeToField['S'])+"").replace("-", "/");
						}
					}
					WdatePicker(md);*/
				}).bind("keyup", function(){
					if(!/^=/.test(this.value)){
						self.editFormula = false; self.hidefpDiv('all');
						return;
					}
                    __showfpDiv(this);
				});
                __showfpDiv(inputfiled[0]);
                break;
			case "numberbox":
				DGUntil.setCursorPosition(inputfiled[0], 0, inputfiled.val().length);
				inputfiled.wknumberbox();
				break;
			case "dummy":
				if(_col.field == "taskRelations"){ // 前置任务
                    var _pretasks = '';
                    if (_txt && _txt.length) {
                        $.each(_txt, function(_i, _n) {
                            _pretasks += _n.relTaskIndex + _n.relTaskType + ',';
                        });
                        _pretasks = _pretasks.replace(/,$/g, '');
                    }
                    self.isEdit = false; this.inputDiv.html(_pretasks);
                    self.setRelations = true;
                    taskRelations.init(this, cell.tid);
				}
				break;
		}
        if (self.setRelations) return;
		if(_editor != "multiLineText" && _editor != "dummy") inputfiled.stcenter(h);
		if(_editor == 'Date') inputfiled.trigger("click");
		_editor != "dummy" && inputfiled.keydown(function(ev){
			var keyCode = ev.keyCode || ev.which;
			if($(this).hasClass("wanted") && $.trim($(this).val()+String.fromCharCode(keyCode)) != "") $(this).removeClass("wanted");
			if(keyCode == 13){
				return self.checkSave();
			}else if(keyCode == 27){ // ESC
				self.cancelEdit();
			}
		}).blur(function(ev){
			if(_editor == 'Date' || _col.field == 'ownerName'){
				return false;
			}
			self.checkSave();
		});
	}
    function __showfpDiv(obj) {
        var _cell = obj.value.match(/[seg]\d{1,}/ig) || [], _ntd = null;
        self.hidefpDiv('all');
        $.each(_cell, function(i, n){
             _ntd = self.findfpDiv(n);
             _ntd && self.createfpDiv(_ntd, true);
        });
    }
}
a2DataGrid.prototype.checkSave = function(){
	var ck = this.checkField();
	if(!ck.success){
		$.tmsg("errs", ck.err);
		return false;
	}else{
		this.saveData(ck); // 任务保存处理
	}
	return true;
}
a2DataGrid.prototype.checkField = function(){ // 保存前检查
	var self = this, cell = this.curCell, td = cell.td, cellindex = cell.colindex, _col = this.columns[cellindex],
		_editor, inputfield = null, td = $(td), isobj = $.isPlainObject(_col.editor), flag = true, err = "", val, data = {}, modified = false, rtype = 'task', result = '', _taskId = cell.tid; // rtype: 'task' 'formula'
	_editor = isobj ? _col.editor.type : _col.editor;
	if(_editor == "multiLineText"){
		inputfield = this.inputDiv.find("textarea");
	}else if(_editor == "dummy"){
	}else{
		inputfield = this.inputDiv.find("input");
	}
	if(isobj && _col.editor.required){
		inputfield.removeClass("wanted");
		if($.trim(inputfield.val()) == ""){
			inputfield.addClass("wanted");
			flag = false; err = "请填写 " + _col.title;
		}
	}
    function __ckclo(formula, para){
		var _bk;
		for(_bk in self.fRef){
			if(self.formula[para] != '' && self.fRef[_bk].indexOf(para) != -1){
				if(formula == _bk) return true;
				return __ckclo(formula, _bk);
			}
		}
		return false;
	}
	if(flag){
		if(_editor == "Date"){
			val = $.trim(inputfield.val()); var _cr, formula, nowRowId = _col.ccode + _taskId, nr1 = _col.ccode + (cell.rowindex+1), arrfRef = []; result = val;
			if(self.editFormula){
				if(!/^=[se]\d{1,}(([+-]g\d{1,})?([+-]\d{1,})?|([+-]\d{1,})?([+-]g\d{1,})?)$/i.test(val)){
					flag = false; err = "输入的公式不正确！（公式格式，如：E3 或 E2-G2+1 或 S1-2 或 S1+G1）";
				}else{
					_cr = val.match(/[seg]\d{1,}/ig);
					for(var i = 0; i < _cr.length; ++i){
						var _ci = _cr[i].toUpperCase(), _rindex = _ci.replace(/[seg]/ig, ""), _bk = false;
						if(_rindex > self.rows.length){
							flag = false; err = "<span style='color:#ff0000'>" + _ci + "</span> 不存在！";
						}else{
							formula = self.rowIdTotaskId(_ci);
							if(/[se]/ig.test(formula)) _bk = __ckclo(nowRowId, formula);
							if(nowRowId == formula){
								flag = false; err = "<span style='color:#ff0000'>" + _ci + "</span> 为自己！";
							}else if(_bk){
								flag = false; err = "<span style='color:#ff0000'>不允许单元格 "+_ci+"和"+nr1+"</span> 闭环引用！";
							}else{arrfRef.push(formula);}
						}
					}//return false;
					if(flag){
						var _expr = val.toUpperCase().replace(/[seg]\d{1,}/ig, function(o){
							return self.rowIdTotaskId(o);
						}), calcF = this.calcFormula(nowRowId, arrfRef, _expr);
						result = calcF.result;
						rtype = 'formula';
						data.formula = calcF.formula;
					}
				}
			}else if(val && !DGUntil.isDate(val)){
				flag = false; err = "输入的日期不正确！（日期格式，如：2011/8/8 或 2011-8-8）";
			}else{
				if((self.formula)[nowRowId]) data.formula = {formula:nowRowId, opera:'del'};
			}
			// check modified
			if(flag){
				if(rtype == 'formula'){
					modified = data.formula.modified;
				}else{
					modified = self.checkModified(_taskId, _col.field, result);
				}
			}
		} else{
			result = $.trim(inputfield.val());
			if(_col.field == 'ownerName'){ // 负责人 用户id，专业id
				var uid = self.inputDiv.find("input.data-uid").val(), profid = self.inputDiv.find("input.data-profid").val();
				if(uid && profid){
					modified = self.checkModified(_taskId, _col.field, {tfzr_uid:uid,tfzr_profid:profid});
					data.fzr = {uid:uid, profid:profid};
				}else{
					modified = false;
				}
			}else{
				modified = self.checkModified(_taskId, _col.field, result);
			}
		}
	}
	return {success:flag, err:err, taskId:_taskId, field:_col.field, result:result, type:rtype, modified:modified, data:data};
}
a2DataGrid.prototype.checkModified = function(taskId, field, data){ // 判断字段修改否
	var flag = false, taskData = {};
	taskData = this.getTaskData(taskId);

	if(field == 'ownerName'){ // 负责人判断，依据用户id，专业id
		if(data.tfzr_uid != taskData["ownerUserId"] || data.tfzr_profid != taskData["ownerProfId"]) flag = true;
	} else if (field == 'endDate' || field == 'startDate') {
        var _dte = taskData[field], _t1, _t2;
        _dte = _dte ? _dte : '1911/1/1'; _dte = new Date(_dte.replace(/-/g, '/'));
        data = data ? data : '1911/1/1'; data = new Date(data.replace(/-/g, '/'));
        if (_dte.getTime() != data.getTime()) flag = true;
	}else{
		if(data != taskData[field]) flag = true;
	}
	return flag;
}
a2DataGrid.prototype.saveData = function(data){ // 保存 任务数据
	if(data.data.formula && data.data.formula.opera == 'del'){
		this.delFormula(data.data.formula);
	}
    if(data.modified){
		if(data.type == 'formula'){ // 进入公式保存处理
            this.updateFieldData(data.taskId, data.field, data.result);
            this.saveFormula(data.data.formula); // 保存公式
        } else {
			// 任务数据
			var dt = data.result;
			if(data.field == 'status'){
				dt = data.data.stat;
			}else if(data.field == 'ownerName'){
				this.updateFieldData(data.taskId, "ownerUserId", data.data.fzr.uid);
				this.updateFieldData(data.taskId, "ownerProfId", data.data.fzr.profid);
			}
			this.updateFieldData(data.taskId, data.field, dt);
			this.saveTask(data.taskId);
		}

        this.updateView(data.taskId, data.field, data.result);
	}
	this.cancelEdit();
}
a2DataGrid.prototype.saveTask = function(taskId){ // ajax task
	var Dt = this.getTaskData(taskId), postDt = {};
	postDt['name'] = Dt.name; postDt['taskId'] = Dt.taskId; postDt['needInherit'] = "0";
	postDt['levelId'] = -1; postDt['needSplit'] = "0"; postDt['splitUserId'] = "-1"; postDt['splitProfId'] = "0";
	postDt['ownerUserId'] = Dt.ownerUserId; postDt['ownerProfId'] = Dt.ownerProfId;
	postDt['curStartData'] = Dt.startDate; postDt['curEndData'] = Dt.endDate; postDt['remark'] = Dt.remark;
	postDt['period'] = Dt.period;
	gAjaxQueue.add({
		type:'POST', url:commApi.saveTask, data:postDt,
		success:function(rs){
			if(!rs.Success){
				$.tmsg("saveTask", rs.msgError, {infotype:2});
			}else{
				var ck = ckFunction.rule(Dt), _tr = $("#tr"+taskId);
				if(ck){
					for(var key in ccodeToField){
						_tr.find("td[field='"+ccodeToField[key]+"']").addClass("error");
					}
				}else{
					if( _tr.find("td.error").length) _tr.find("td.error").removeClass("error");
				}
			}
		}
	});
}
a2DataGrid.prototype.saveFormula = function(data){ // 保存公式
	// data = {formula:formula, fRef:fRef, expression:expression, modified:modified}}
	var self = this, formula;
	if(data.modified){
		formula = data.formula; var expression = formula + "=" + data.expression, arr;
		if (!data.mask) this.grid.mask("处理中，请稍候...");
		arr = DGUntil.refRowindex(formula);
		// ajax
		$.ajax({
			type:'POST',
			url:commApi.editFormula, data:{taskId:arr[2], type:arr[1], formula:expression},
            complete:function(){if (!data.mask) self.grid.unmask();},
			success: function(rs){
				if(rs.Success){
					self.formula[formula] = data.expression;
					self.updatefRef(data.fRef, formula);
					var Dt = self.getTaskData(arr[2]), ck = ckFunction.rule(Dt), _tr = $("#tr"+arr[2]);
					if(ck){
						for(var key in ccodeToField){
							_tr.find("td[field='"+ccodeToField[key]+"']").addClass("error");
						}
					}else{
						if( _tr.find("td.error").length) _tr.find("td.error").removeClass("error");
					}
                    _tr.find("td[field='"+ccodeToField[arr[1]]+"']").addClass('dotgreen');
				}else{
					$.tmsg("editFormula", rs.msgError, {infotype:2});
				}
			}
		});
	}
	this.cancelEdit();
}
a2DataGrid.prototype.saveColor = function(taskId, field, data, curCol){ // 保存 日期单元格底色
    var self = this, ck = self.checkModified(taskId, field, data),
        f = self.dt.find("#tr"+taskId).find("td[field='"+curCol+"']");
    if (ck) {
        var sd, ed, t;
        if (field.indexOf('start') != -1) {
            sd = data;
            t = 0; ed = self.getTaskFieldData(taskId, 'endDateBg');
        } else {
            t = 1;
            sd = self.getTaskFieldData(taskId, 'startDateBg'); ed = data;
        }
        gAjaxQueue.add({
            type:'POST', url:commApi.saveBgColor+taskId, data:{startBg:sd,endBg:ed,type:t},
            success:function(rs){
                if(!rs.Success){
                    $.tmsg("cellbgcolor", rs.msgError, {infotype:2});
                } else {
                    self.updateFieldData(taskId, field, data);
                    f[0].style.backgroundColor = data;
                }
            }
        });
    }
}
a2DataGrid.prototype.updatefRef = function(fRef, formula){
	var self = this;
	$.each(fRef, function(i, n){
		if(!self.fRef[n]) self.fRef[n] = [];
		if(self.fRef[n].indexOf(formula) == -1) self.fRef[n].push(formula);
	});
}
a2DataGrid.prototype.delFormula = function(data){ // 删除公式
	// data = {formula:formula, opera:'del'}
	var fmn = data.formula, self = this, arr;
	arr = DGUntil.refRowindex(fmn);
	gAjaxQueue.add({
		type:'POST', url:commApi.delFormula, data:{taskId:arr[2], type:arr[1]},
		success:function(rs){
			if(rs.Success){
				self.formula[fmn] = "";
				for(var key in self.fRef){
					self.fRef[key].remove(fmn);
				}
                $("#tr" + arr[2]).find("td[field='"+ccodeToField[arr[1]]+"']").removeClass('dotgreen');
			}else{
				$.tmsg("delFormula", rs.msgError, {infotype:2});
			}
		}
	});
}
a2DataGrid.prototype.updateView = function(taskId, field, data){ // 刷新视图
	var key = null, self = this, frs = [];
	for(var k in ccodeToField){
		if(ccodeToField[k] == field){
			key = k; break;
		}
	}
	self.updateView1(taskId, field, data);
	function _dg(k){
		var formulas = self.fRef[k]||[], f, _cr, rs, ri;
		$.each(formulas, function(i, n){
			f = self.formula[n];
			if(f!=''){
				_cr = f.match(/[seg]\d{1,}/ig); ri = DGUntil.refRowindex(n);
				rs = self.calcFormula(n, _cr, f);
				self.updateFieldData(ri[2], ccodeToField[ri[1].toUpperCase()], rs.result);
				//self.updateView1(ri[2], ccodeToField[ri[1].toUpperCase()], rs.result);
                frs.push({'taskId': ri[2], 'field': ccodeToField[ri[1].toUpperCase()], 'result': rs.result});
				if(self.fRef[n] && self.fRef[n].length){
					_dg(n);
				}
			}
		});
	}
	if(key){ // 公式关联field
		_dg(key+taskId);
        var i = 0, _dt;
        for (; _dt = frs[i]; ++i) {
            var f = document.getElementById('tr'+_dt.taskId),
                idx = self.colIdxs[_dt.field], _t = _dt.result;
            f = f.childNodes[idx];
            _t = _t.replace(/\//g, "-");
            if (f) {
                var txt = f.childNodes[0];
                if (txt) {
                    txt.nodeValue = _t;
                } else {
                    txt = document.createTextNode(_t);
                    f.appendChild(txt);
                }
            }
        }
	}
}
a2DataGrid.prototype.updateView1 = function(taskId, c, data){ // 刷新视图1
	var field = ccodeToField[c] || c, _t = data, f = this.dt.find("#tr"+taskId).find("td[field='"+field+"']");
	if(field == 'name'){
		if(f.hasClass("gray")) f.removeClass("gray");
        var _url = _isexpLibId ? '<a href="javascript:;">' : '<a href="'+ commApi.taskUrl + taskId + '?projId='+this.projId+'" target="_blank">', exem = '';
        f = f.find("div:last");
        if (f.find('span')[0]) exem = '<span>（已豁免）</span>';
        _t = _url + data.replace(/ /g, "&nbsp;") + '</a>' + exem;
	}
    if (field == 'startDate' || field == 'endDate') _t = _t.replace(/\//g, "-");
	f.html(_t);
	this.upFocusDiv();
}
a2DataGrid.prototype.cancelEdit = function(){ // 按 ESC 取消编辑
	this.inputDiv.children().remove();
	if(this.editFormula) this.hidefpDiv('all'); this.isEdit = false; this.editFormula = false;
	this.inputDiv.hide(); $("#wcalendar").hide();
	if(typeof hideAllTaskPeopleList == 'function') hideAllTaskPeopleList();
	this.focusDiv.show();
}
a2DataGrid.prototype.calcFormula = function(formula, fRef, expression){ // S37 [S32,G32] S32+G32-5
	var i = 0, flag = true, err = '', rr, fRefData, f, result = '', d, dt = {}, days;
	for(; i < fRef.length; ++i){
		rr = DGUntil.refRowindex(fRef[i]); // SEG rr[1]  task/rowindex rr[2]
		fRefData = this.getTaskFieldData(rr[2], ccodeToField[rr[1].toUpperCase()]).replace("-","/");
		dt[fRef[i].toUpperCase()] = fRefData;
		if(rr[1].toUpperCase() != 'G') d = new Date(fRefData||'1900/1/1');
	}
	expression = expression.replace("=", "");
	f = expression.replace(/[se]\d{1,}/ig, "").toUpperCase();
	if(f != ""){
		f = f.replace(/g\d{1,}/ig, function(_){
			return dt[_] || 0
		});
		days = parseInt(eval(f), 10) || 0;
		d.addDays(days);
	}
	result = d.Format("yyyy-MM-dd");
	var fmd = !this.formula[formula] ? true : !(this.formula[formula] == expression);
	return {success:flag, result:result, err:err, formula:{formula:formula, fRef:fRef, expression:expression, modified:fmd}};
}
var ccodeToField = {'S':'startDate', 'E':'endDate', 'G':'period'};
var ckFunction = {
	"name":"checkdate",
	"rule":function(record){
		var s = record[ccodeToField['S']], e = record[ccodeToField['E']], p = record[ccodeToField['G']], _f1 = false, _f2 = false, diff;
		if(s == '' || e == '') return false;
		s = new Date(s&&s.replace("-","/")); e = new Date(e&&e.replace("-","/")); p = parseInt(p, 10) || 0;
		if(s > e) _f1 = true;
		diff = parseInt(Math.abs(Date.parse(s) - Date.parse(e))/1000/60/60/24, 10);
		if(diff != p) _f2 = true;
		return _f1 || _f2
	}
};
a2DataGrid.prototype.getTaskData = function(taskId){ // 获取单条任务数据
	var rowsindex = this.rowsindex[taskId].index, rs;
	rs = this.rows[rowsindex];
	return rs;
}
a2DataGrid.prototype.getTaskFieldData = function(taskId, field){ // 获取field数据
	var taskData = this.getTaskData(taskId);
	return taskData[field] || "";
}
a2DataGrid.prototype.updateFieldData = function(taskId, field, data){ // 更新field数据
    if (field == 'endDate' || field == 'startDate') data = data.replace(/\//g, "-");
	this.rows[this.rowsindex[taskId].index][field] = data;
}
a2DataGrid.prototype.rowIdTotaskId = function(cr){ // E1 => E15
	var cd = cr.replace(/\d/g, ""), _rindex = parseInt(cr.replace(/[seg]/ig, ""), 10) - 1;
	_rindex = this.dt.find("tr:eq("+_rindex+")").attr("id").replace("tr", "");
	return (cd+_rindex);
}
a2DataGrid.prototype.taskIdTorowId = function(cr){ // E15 => E1
	var cd = cr.replace(/\d/g, ""), _tid = parseInt(cr.replace(/[seg]/ig, ""), 10);
	_tid = (this.dt.find("#tr"+_tid))[0].rowIndex-1;
	return (cd+_tid);
}
a2DataGrid.prototype.upFocusDiv = function(){ // focus单元格
	var self = this, td = self.curCell.td;
	var offset = $(td).position(),
		l = offset.left - 2, t = offset.top - 2, w = $(td).outerWidth() - 2, h = $(td).outerHeight() - 1;
	this.showFocusDiv(l,t,w,h);
}
a2DataGrid.prototype.showFocusDiv = function(l,t,w,h){ // focus单元格
	this.isEdit = false;
	this.focusDiv.css({left:l, top:t, width:w, height:h, display:'block'}).focus();
}

$.fn.a2DataGrid = function(config){
    if (typeof expLibId != 'undefined') {
        if (expLibId == 1) {
            _isexpLibId = true;
        }
    }
	var l = $(this).length;
	return this.each(function() {
		new a2DataGrid($.extend({el:$(this), cols:7, plans:l, startindex:1}, config||{}));
	});
}

var taskRelations = (function() {
    var relStatus = [
    { relType: 'FS', relName: '完成-开始(FS)' },
    { relType: 'SS', relName: '开始-开始(SS)' },
    { relType: 'FF', relName: '完成-完成(FF)' },
    { relType: 'SF', relName: '开始-完成(SF)' }
    ], taskData = {}, nowTaskId;
    function relFormatter(value) {
        var str = "", i = 0, isck = false;
        for (; i < relStatus.length; ++i) {
            if (value == relStatus[i].relType) isck = true;
            str += '<option value="' + relStatus[i].relType + '"' + (isck ? " selected" : "") + '>' + relStatus[i].relName + '</option>';
            isck = false;
        }
        return str;
    }
    function showList(obj, taskId) {
        taskData = obj.getTaskData(taskId); nowTaskId = taskId;
        var tpl = [], _tr;
        box('<div style="padding:5px;"><div class="Reminder" style="margin:7px auto; width:250px;">点击任务列表选取需要的前置任务！</div><table id="grlist" class="tableborder" width="100%" bgcolor="#ffffff"><thead bgColor="#f7f7f7"><td>标识号</td><td>任务名称</td><td>类型</td><td>延隔时间</td><td>操作</td></thead><tbody></tbody></table></div>', {boxid:'relts', title:'<span style="color:#009933">'+ taskData.name + '</span> 的前置任务', contentType:'html', cls:'shadow-container3', submit_BtnName: "保 存", cancel_BtnName: "关 闭", pos:'r-r', offset:[-50,0],
            onOpen: function(o) {
                var _t = '', _d = '';
                if (taskData.taskRelations && taskData.taskRelations.length) {
                    obj.unSelRows();
                    $.each(taskData.taskRelations, function(i, n) {
                        _t = '<select>' + relFormatter(n.relTaskType) + '</select>';
                        _d = n.delayType == 0 ? 'd' : '%';
                        tpl.push('<tr tid="'+n.relTaskId+'"><td>'+n.relTaskIndex+'</td><td>'+n.relTaskName+'</td><td>'+_t+'</td><td><input type="text" style="width:30px;" value="'+n.delayCount+'" />'+_d+'</td><td><a href="javascript:;" class="mols">移除</a></td></tr>');
                        _tr = document.getElementById('tr'+n.relTaskId);
                        obj.SelRow_(_tr, n.relTaskId);
                    });
                }
                _t = tpl.join('');
                var _ct = $("#grlist tbody");
                _ct.html(_t);
                bindF(_ct, obj);
                _t = null;
            },
            submit_cb: function(o) {
                var _rows = $("#grlist tbody")[0].rows, str = '', // id，FS，112|Y|
                    _id, _tp, _dl, newrl = [], ri, uv = '';

                $.each(_rows, function(i, n) {
                    _id = n.getAttribute("tid"); _tp = $(n).find("select option:selected").val();
                    _dl = $(n).find("input").val();
                    ri = $(n).find("td:first").text(); var nm = $(n).find("td:eq(1)").text();
                    str += _id + ',' + _tp + ',' + _dl + '|Y|';
                    newrl.push({"relTaskId":_id, "relTaskIndex":ri, "relTaskName":nm, "relTaskType":_tp, "delayType":0, "delayCount":_dl});
                    uv += ri + _tp + ',';
                });

                obj.grid.mask(""); o.fbox.mask("处理中，请稍候...");
                $.ajax({
                    type:'POST', url:commApi.saveTaskRelation,
                    data:{projId: obj.projId, secdPlanId:obj.secdPlanId, sucTaskId:nowTaskId, saveKey:str},
                    complete: function() { obj.grid.unmask(); o.fbox.unmask(); _reset(obj, taskId);},
                    success: function(rs) {
                        if(rs.Success){
                            obj.updateFieldData(taskId, "taskRelations", newrl);
                            obj.updateView1(taskId, "taskRelations", uv.replace(/,$/g, ''));
                        }else{
                            $.tmsg("createRL", rs.msgError, {infotype:2});
                        }
                    }
                });
            },
            onDestroy: function(o) {
                obj.setRelations = false;
                _reset(obj, nowTaskId);
            }
        })
    }
    function _reset(obj, taskId) {
        obj.unSelRows();
        $("#tr"+taskId).find("td[field='taskRelations']").trigger('click');
        obj.inputDiv.hide(); obj.focusDiv.hide();
    }
    function ckcycle(nowid, rl) {
        if (rl && rl.length) {
            var f = false;
            $.each(rl, function(i, n) {
                if (n.relTaskId == nowid) {
                    f = true;
                    return false;
                }
            });
            return f;
        }
        return false;
    }
    function reBuildList(obj, clicktaskid) {
        var _rows = obj.dt.find("tr[isSel='1']"), ct = $("#grlist tbody"), tpl = [], tid,
            ri, m = 0, tp, relTaskName, _t, relTaskType = 0, delayCount = 0, delayType = 0, _d, dt;
        if (!ct.length) return;
        $.each(_rows, function(i, n) {
            tid = n.id.replace('tr', '');
            dt = obj.getTaskData(tid);
            if (tid == nowTaskId || ckcycle(nowTaskId, dt.taskRelations)) return true;
            ri = $(n).find("td:eq(0)").text();
            relTaskName = dt.name;
            for (; tp = taskData.taskRelations[m]; ++m) {
                relTaskType = ''; delayCount = 0;
                if (tp.relTaskId == tid) {
                    relTaskName = tp.relTaskName;
                    relTaskType = tp.relTaskType;
                    delayCount = tp.delayCount;
                    break;
                }
            }
            _d = delayType == 0 ? 'd' : '%';
            _t = '<select>' + relFormatter(relTaskType) + '</select>';
            tpl.push('<tr tid="'+tid+'"><td>'+ri+'</td><td>'+relTaskName+'</td><td>'+_t+'</td><td><input type="text" style="width:30px;" value="'+delayCount+'" />'+_d+'</td><td><a href="javascript:;" class="mols">移除</a></td></tr>');
        });
        tpl = tpl.join('');
        ct.html(tpl);
        bindF(ct, obj);
    }
    function bindF(c, obj) {
        c.find("a.mols").each(function() {
            $(this).click(function() {
                var p = this.parentNode.parentNode, tskid = p.getAttribute("tid"), _tr;
                _tr = document.getElementById('tr'+tskid);
                obj.unSelRow_(_tr, tskid);
                $(p).remove();
            });
        });
        c.find("input").each(function() {
            $(this).wknumberbox();
        });
    }
    return {
        init: function(obj, taskId) {
            showList(obj, taskId);
        },
        setRS: function(obj, clicktaskid) {
            reBuildList(obj, clicktaskid);
        }
    };
})();

var glwd = (function() {
    function showbox(taskId, wd, grid) {
        box('<table cellpadding="3" cellspacing="5"><tr><td>选择你要挂接的维度：</td><td><select><option value="0">请选择</option></select></td></tr></table>', {boxid:'showwd', title:'挂接维度',
            onOpen: function(o) {
                o.fbox.mask('loading...');
                $.ajax({
                    cache: false, url: '/Projects/ProjArranged/GetAllPSValue',
                    data: {pageSize: 9999},
                    complete: function() {o.fbox.unmask();},
                    success: function(data) {
                        var str = '', dt, sel = '';
                        for (var i = 0; i < data.length; ++i) {
                            dt = data[i];
                            sel = wd == dt.valueId ? ' selected' : '';
                            str += '<option value="'+dt.valueId+'"'+sel+'>'+dt.name+'</option>';
                        }
                        o.fbox.find('select').append(str);
                    }
                });
            },
            submit_cb: function(o) {
                var sel = o.fbox.find('select'), v = sel.find("option:selected").val();
                if (v == 0) {
                    $.tmsg('m_wd', '请选择要挂接的维度');
                    return false;
                } else {
                    $.post('/Projects/ProjArranged/SaveTaskDimension', {taskId: taskId, valIds: v}, function(data) {
                        if(data.Success){
                            $.tmsg("m_wd", '操作成功！', {infotype:1});
                            grid.updateFieldData(taskId, 'valueId', v);
                        } else {
				            $.tmsg("m_wd", data.msgError, {infotype:2});
                        }
                    });
                }
            }
        });
    }
    return {
        init: function(tid, wd) {
            showbox(tid, wd, this);
        }
    };
})();