/*
* importexp.js 导入公司经验
* 2011-4-13 Qingbao.Zhao
*/
var oUntil = {
    getPositionForInput: function(ctrl){ // 单行文本框
		var CaretPos = 0;
		if(document.selection){ // IE Support
			ctrl.focus();
			var Sel = document.selection.createRange();
			Sel.moveStart('character', -ctrl.value.length);
			CaretPos = Sel.text.length;
		}else if(ctrl.selectionStart || ctrl.selectionStart == '0'){// Firefox support
			CaretPos = ctrl.selectionStart;
		}
		return (CaretPos);
	},
    getSelectionText: function(){
		if(window.getSelection){
			return window.getSelection().toString();
		}else if(document.selection && document.selection.createRange){
			return document.selection.createRange().text;
		}
		return '';
	}
};
$.fn.onumberbox = function(maxnum) {
    $(this).css({imeMode: "disabled", textAlign:"center"});
    this.bind("keypress", function(e){
        var kc = e.keyCode || e.which, inputstr = String.fromCharCode(kc), st = oUntil.getSelectionText(), v = this.value, pos = oUntil.getPositionForInput(this);
        if(st){
            pos = v.indexOf(st); v = v.replace(st, "");
        }
        v = v.substr(0, pos) + inputstr + v.substr(pos);
        if(v.length > 1 && !/^(-)?\d+$/g.test(v) || v.length === 1 && !/^[-|\d]$/g.test(v) || /^0/.test(v)) return false;
    });
    this.bind("paste dragenter", function(){
        return false;
    });
};

var a3plan_impexp = (function () {
    var lck,
		urls = {
		    importExp: '/DesignManage/QuicklyLoadExperience/', // 导入公司经验
		    importExpFromPlan: '/DesignManage/QuicklyLoadExperienceJD/', // 导入公司经验
		    iExpLoad: '/home/PlanAndTaskList/', // 获取对应公司经验的任务结构
		    iExpSave: '/home/QuickyCreatePlan' // 导入到计划
		},
        taskStatus = ["待分解", "分解完成", "未开始", "进行中", "已完成"],
		tplv = '<tr><td>{rowindex}</td><td><div style="padding-left:{padleft}px;padding-right:5px;float:left"><img style="{icon}" src="/Content/images/DesignManage/ico-Expand.gif"></div><div style="float:left">{name}</div></td><td width="80">{ownerName}</td><td width="100">{planStartDate}</td><td width="100">{planEndDate}</td><td></td></tr>';
    function __initLoad(cfg) {
    }
    function __calcrs(dt, lvl, data) {
        var lck = 0, i = 0, len = data.length, hasChild = false;
        dt['statusName'] = taskStatus[dt['status']];
        dt['padleft'] = 16 * lvl;
        for (; i < len; ++i) {
            if (data[i].nodePid === dt.taskId) {
                hasChild = true;
                break;
            }
        }
        dt['icon'] = hasChild ? "cursor:pointer" : "visibility:hidden";
        if (dt['needSplit'] == 0) dt['spliterName'] = "";
        dt['lock'] = lck;
        return dt;
    }
    function __reOrder(tb) {
        var o = tb.find("span[name='cnum']");
        o.each(function (i) {
            $(this).text(i + 1);
        });
    }

    function __importExp(force, projID, fromPlan) { // 导入经验

        var expData = [], _secdPlanId = -1, secdPlanId = 0, setoffset_year, setoffset_month, setoffset_day, isIgnorePerson, isIgnoreDate, isIgnorePassed, isContractPlan;
        var _u = fromPlan ? urls.importExpFromPlan : urls.importExp;
        typeof force == "string" ? isContractPlan = "isContractPlan=1" : isContractPlan = "";

        box(_u + secdPlanId + "?projID=" + projID + "&" + isContractPlan, { boxid: 'loadExp', title: '载入公司经验', contentType: 'ajax', modal: true, submit_BtnName: '确定导入', cls: 'shadow-container',width:800,
            onLoad: function (o) {
                //o.fbox.center();
                $("#secPlanStartData").unbind("click").bind("click", function () {
                    WdatePicker({ maxDate: $("#secEndData").val().replace("-", "/") });
                });
                $("#secEndData").unbind("click").bind("click", function () {
                    WdatePicker({ minDate: $("#secPlanStartData").val().replace("-", "/") });
                });
                $("#secPlanName").val(projName || "");
                o.db.find("li[secdPlanId]").each(function () {
                    $(this).click(function () {
                        _secdPlanId = $(this).attr("secdPlanId");
                        o.db.find("li[secdPlanId]").removeClass("libg");
                        $(this).addClass("libg");
                        o.fbox.mask("loading...");
                        $.ajax({
                            url: urls.iExpLoad, data: { secdPlanId: _secdPlanId }, cache: false,
                            success: function (data) {
                                o.fbox.unmask();
                                data = data[0]
                                expData = data.taskList;
                                //$("#secPlanStartData").val(data.secPlanStartDate);
                                //$("#secEndData").val(data.secPlanEndDate);
                                $("#manager").val(data.managerName);
                                $("#managerId").val(data.managerId);
                                $("#managerSysProfId").val(data.managerSysProfId);
                                if (data.managerId != curUserId) {
                                    //$("#setMytoList").show();
                                    $("#checkMe").attr("checked", "checked").val("1"); //$("#setMytoListProf").show();
                                } else {
                                    $("#setMytoList").hide();
                                    $("#setMytoListProf").hide();
                                }
                                var tmp = "", _rs = expData, cc = [], _rowindex = 0;
                                function rd(pid, lv) {
                                    var dt, i = 0, _tid, _pid, tmp;
                                    for (; dt = _rs[i]; ++i) {
                                        _tid = dt.taskId; _pid = dt.nodePid;
                                        if (_pid === pid) {
                                            _rowindex++;
                                            var _cls = "", clrs = '', clre = '', pretasks = '', _url;

                                            dt["pretasks"] = pretasks; dt["cls"] = _cls;
                                            dt['clrs'] = clrs; dt['clre'] = clre;
                                            dt['padleft'] = 16 * lv;
                                            dt['icon'] = "cursor:pointer";
                                            dt["rowindex"] = _rowindex;
                                            cc.push(tplv.compileTpl(dt));
                                            rd(_tid, lv + 1);
                                        }
                                    }
                                }
                                rd(0, 0);
                                tmp = cc.join('');
                                if (tmp == "") tmp = "<tr><td colspan='3'>暂无内容</td></tr>";
                                $("#viewExp").find("tbody").html(tmp);
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
                        YH.dom.scrollintoContainer(lastobj, $("#exp_clist"), true, true, {});
                        if (lastobj.tagName.toLowerCase() == "li") $(lastobj).trigger("click");
                    }
                });
                //$("#manager").unbind("click").bind("click", function() {

                //});
                bindASearchableInput($("#manager"), $("#managerId"), $("#managerSysProfId"), projId, 0, true);
                $("#checkMe").click(function () {
                    if ($(this).attr("checked")) {
                        $(this).parent().parent().next().show();
                        $(this).val("1");
                    } else {
                        $(this).parent().parent().next().hide(); $(this).val("0");
                    }
                });
                var _cfirst = o.db.find("li[secdPlanId]:first");
                _cfirst[0] && _cfirst.trigger("click");
                setoffset_year = o.db.find("input[name='OffSetYear']");
                setoffset_month = o.db.find("input[name='OffSetMonth']");
                setoffset_day = o.db.find("input[name='OffSetDay']");
                isIgnorePerson = o.db.find("input[name='isIgnorePerson']");
                isIgnoreDate = o.db.find("input[name='isIgnoreDate']");
                isIgnorePassed = o.db.find("input[name='isIgnorePassed']");
                setoffset_year.onumberbox(); setoffset_month.onumberbox(); setoffset_day.onumberbox();
            },
            submit_cb: function (o) {
                //                if (_secdPlanId == -1) {
                //                    $.tmsg("m_loadexp", "请选择要导入的项目计划模板！");
                //                    return false;
                //                }
                if ($("#managerId").val() == -1) {
                    $.tmsg("m_loadexp", "请选择计划负责人！");
                    return false;
                }
                if ($("#takeTime").length > 0) {
                    if ($("#takeTime").val() == "") {
                        $.tmsg("m_loadexp", "请选择拿地时间！");
                        return false;
                    }
                }
                $("#newplan").validate({
                    submitHandler: function (form) {
                        o.fbox.mask("载入中...");
                        var ser = $("#newplan").serialize(), _str = '';
                        if (!isIgnorePerson.attr("checked")) {
                            _str = "&isIgnorePerson=1";
                        }
                        if (!isIgnoreDate.attr("checked")) {
                            _str += "&isIgnoreDate=1";
                        }
                        if (isIgnorePassed.length && !isIgnorePassed.attr("checked")) {
                            _str += "&isIgnorePassed=1";
                        }
                        $.ajax({
                            url: urls.iExpSave + "?" + ser + _str, data: { copySecPlanId: _secdPlanId, projId: projId, sourceSecPlanId: secdPlanId }, type: 'POST',
                            error: function () {
                                hiAlert('未知错误，请联系服务器管理员，或者刷新页面重试', '保存失败');
                            },
                            success: function (data) {
                                if (data.Success) {
                                    $.tmsg("m_loadexp", "操作成功！页面跳转中...", { infotype: 1 });
                                    var gourl = '/DesignManage/PayMultiPlanManage?projId=';
                                    window.location.href = gourl + projId;
                                } else {
                                    o.fbox.unmask();
                                    $.tmsg("m_loadexp", data.msgError, { infotype: 2 });
                                }
                            }
                        });
                    }
                });
                $("#newplan").submit();
                return false;
            },
            onDestroy: function (o) {
//                if (force) {
//                    newbox3(projId, true);
//                }
            }
        })
    }
    return {
        init: function (cfg) {
            __initLoad(cfg || {});
        },
        importExp: function (force, projID) { // 导入公司经验
            __importExp(force, projID, false);
        },
        importExpFromPlan: function (force, projID) { // 直接从计划导入
            __importExp(force, projID, true);
        }
    }
})();