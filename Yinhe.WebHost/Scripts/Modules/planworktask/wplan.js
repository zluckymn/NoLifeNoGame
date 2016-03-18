/*
* plan.js 计划编辑 更改状态  计划变更 专业阶段  人员设置
* 2011/3/8 Qingbao.zhao
*/
var a3Stat = {
    plan: {
        Unconfirmed: "未确认", // 0
        Processing: "进行中", // 1
        Completed: "已完成", // 2
        Revising: "修订中" // 3
    }
};
var a3plan = (function () {
    var urls = {
        planInfo: '/DesignManage/PlanEdit',
        savePlanInfo: '/Home/SavePlanInfo',
        changestatus: '/Home/ChangePlanStatus',
        hasCompleted: '/Home/HasIncompleteTask'
    };
    return {
        createPlan: function (t, pos, force, isJD) {
            function __smit(o, tt) {
                $("#planForm").validate({
                    submitHandler: function (form) {
                        var dt = $("#planForm").serialize(), _e = $("#endData").val(),
							_name = $("#name").val(), _manager = $("#manager").val(), _s = $("#startData").val();
                        o.fbox.mask("保存中，请稍候...");
                        $.ajax({
                            type: 'POST',
                            url: urls.savePlanInfo + "?r=" + Math.random(),
                            data: dt,
                            error: function () {
                                $.tmsg('cplan1', '未知错误，请联系服务器管理员，或者刷新页面重试', { infotype: 2 });
                            },
                            success: function (data) {
                                if (data.Success) {
                                    $.tmsg("cplan1", "操作成功！", { infotype: 1 });
                                    if (pos) {
                                        $("#editpname" + t).text(_name);
                                        $("#edituname" + t).html(_manager);
                                        $("#editsdate" + t).html(_s);
                                        $("#editedate" + t).html(_e);
                                        o.fbox.unmask();
                                        o.destroy();
                                        //window.location.href = '/DesignManage/MultiPlanManage?projId=' + projId;
                                        window.location.reload();
                                    } else {
                                        $.tmsg("m_loadexp", "操作成功！页面跳转中...", { infotype: 1 });
                                        window.location.reload();
                                        //window.location.href = '/DesignManage/MultiPlanManage?projId=' + projId;
                                        //o.fbox.unmask();
                                    }
                                } else {
                                    $.tmsg("cplan1", data.Message, { infotype: 2 });
                                    o.fbox.unmask();
                                }
                            }
                        });
                    }
                });
                $("#planForm").submit();
                return false;
            }
            var isTempContractPlan = "";

            if (typeof isContractPlan != "undefined") {
                isTempContractPlan = isContractPlan;
            }

            box(urls.planInfo + "?projId=" + projId + "&secdPlanId=" + t + "&isContractPlan=" + isTempContractPlan + "&_t=" + new Date().getTime(), { boxid: 'a3_plan', title: '计划基本信息', contentType: 'ajax', modal: true, fixable: false, submit_BtnName: "保存", cls: 'shadow-container', width: 400,
                onLoad: function (o) {
                    var _o1 = o.fbox.find("#managerIds"), _o2 = -1, inputfiled = _o1.prev();
                    bindASearchableInput(inputfiled, _o1, _o2, projId, 2, false, false);

                    //var _userid = $("#g_userId").val(); // 当前我的用户id
                    $("#startData").click(function () {
                        WdatePicker({ maxDate: $("#endData").val().replace("-", "/") });
                    });
                    $("#endData").click(function () {
                        WdatePicker({ minDate: $("#startData").val().replace("-", "/") });
                    });
                    $("#actualStartDate").click(function () {
                        WdatePicker();
                    });
                    if ($("#isNewPlan").val() == true) {
                        bindASearchableInput($("#manager"), $("#managerId"), $("#managerProfId"), projId, 0, true);
                    } else {
                        bindASearchableInput($("#manager"), $("#managerId"), $("#managerProfId"), projId, 0);
                        $("#managerProfId").parent().parent().hide();
                    }
                    //});
                    $("#checkMe").click(function () {
                        if ($(this).attr("checked")) {
                            $(this).parent().parent().next().show();
                        } else {
                            $(this).parent().parent().next().hide();
                        }
                    });
                },
                submit_cb: function (o) {
                    var ids = $("#managerIds").val();
                    if (ids == -1 || ids == "undefined" || ids == 0) {
                        $.tmsg("cplan1", "请选择负责计划人！");
                        return false;
                    }
                    __smit(o, false);
                    return false;
                },
                onDestroy: function (o) {
                    if (force) {
                        if (isJD) {
                            newbox3(projId, true);
                        } else {
                            newbox(projId, true);
                        }
                    }
                }
            });
        },
        changeStatus: function (obj, xd) {
            var secdPlanId = $(obj).attr("data-planid"), onowstat = $("#nowstats" + secdPlanId), nst = onowstat.attr("nst"), newstat = $(obj).attr("newstat"), tip = "";
            var tpl = '<div class="contain"><form id="planChangeForm1" method="post" action=""><div style="font-size:14px;font-weight:bold;margin:5px 8px" id="chgstitle"></div><table width="100%" style=" margin:8px"><tr><td align="left" valign="top"><font color="#FF3300">*</font>计划状态变更说明:</td><td><textarea name="reason" id="reason" class="textarea_01" title="计划状态变更说明" style="width:250px; height:100px"></textarea></td></tr></table></form></div>',
				strRS = '<table width="100%" bgcolor="#FFFFFF" style="margin-bottom:20px"><tr><td width="60" valign="top"><div class="picdivus"><img src="{imgSrc}" width="50" height="50" /></div></td><td valign="top"><div style="line-height:23px; background-color:#f2f2f2; padding-left:5px">发起人：<font color="#0187dc">{changeUser}</font>&nbsp;&nbsp;&nbsp;&nbsp;时间：<font color="#0187dc">{date}</font></div><div class="hb-main">{reason}</div></td></tr></table>', btn = "", _tp = "";
            $.ajax({
                url: urls.hasCompleted + "?_t=" + Math.random(), data: { planId: secdPlanId, status: newstat },
                success: function (data) {
                    data = data.Data;
                    if (data.success) {
                        var flag = false, batchFis = false;
                        if (data.hasIncompleteTask) { // 存在未完成
                            if (newstat == "Completed") {
                                tip = "该计划下尚有任务未完成！无法完成计划！";
                                flag = true;
                            } else {
                                tip = "该计划下尚有待分解的任务！确定强制启动计划吗？"; btn = "强制启动计划"; _tp = "启动";
                                batchFis = true;
                            }
                        } else {
                            if (newstat == "Completed") {
                                tip = "确定完成计划吗？"; btn = "确定完成计划"; _tp = "完成";
                            } else {
                                if (nst == "2") {
                                    tip = "确定重启计划吗？"; btn = "确定重启计划"; _tp = "重启";
                                } else {
                                    tip = "确定启动计划吗？"; btn = "确定启动计划"; _tp = "启动";
                                    batchFis = true;
                                }
                            }
                        }
                        if (flag) {
                            $.tmsg("m_stat", tip);
                            return false;
                        }
                        box(tpl, { boxid: 'changePlan1', title: btn, contentType: 'html', cls: 'shadow-container', width: 400, submit_BtnName: btn, modal: true,
                            onOpen: function (o) {
                                o.db.find("#chgstitle").html(tip);
                                var d = new Date(), _y = d.getFullYear(), _m = d.getMonth() + 1, _d = d.getDate();
                                o.db.find("textarea").val(_y + "年" + _m + "月" + _d + "日 " + _tp + "计划：\r\n");
                            },
                            submit_cb: function (o) {
                                $("#planChangeForm1").validate({
                                    submitHandler: function (form) {
                                        var dt = $("#planChangeForm1").serialize();
                                        o.fbox.mask("处理中，请稍候...");
                                        $.ajax({
                                            type: 'POST',
                                            url: urls.changestatus,
                                            data: dt + "&planId=" + secdPlanId + "&status=" + newstat,
                                            success: function (data) {
                                                o.fbox.unmask();
                                                if (data.Success) {
                                                    $.tmsg("m_cgplan", "操作成功！", { infotype: 1 });
                                                    onowstat.attr("nowstat", newstat).html(a3Stat.plan[newstat]);
                                                    $(obj).attr("newstat", "Completed").val("完成");
                                                    if (newstat == 'Completed') {
                                                        $(obj).val("修订").attr("newstat", "Processing");
                                                    }
                                                    // $("#cgst_factstart" + secdPlanId).text(data.plan.actualStartDate);
                                                    // $("#cgst_factend" + secdPlanId).text(data.plan.actualEndDate);

                                                } else {
                                                    $.tmsg("m_cgplan", data.Message, { infotype: 2 });
                                                }
                                                o.destroy();
                                            }
                                        });
                                    }
                                });
                                $("#planChangeForm1").submit();
                                return false;
                            }
                        });
                    } else {
                        $.tmsg("m_stat", data.Message, { infotype: 2 });
                    }
                }
            });
        }
    }
})();

var newbox = function (projId, force) {
    var tpl = '<div style="text-align:center;padding:50px 0 0 80px;height:100px;"><a href="javascript:;" id="conce" class="importemcreat"></a>　<a href="javascript:;" id="cplan" class="manualcreat"></a></div>', nocancel = force ? true : false;
    box(tpl, { boxid: "createP", no_titlebar: true, no_submit: true, modal: false, cls: 'shadow-container', no_cancel: nocancel, width: 600,
        onOpen: function (o) {
            $(window).resize(function () {
                o.__setPosition();
            });
            $("#cplan").click(function () {
                if ($("div.wk-datagrid").length == 0) o.fbox.css({ 'background': 'none', "border": 'none' });
                o.destroy();
                a3plan.createPlan(0, 0, force);
            });
            $("#conce").click(function () {
                o.destroy();
                a3plan_impexp.importExp(force, projId);
            });
        }
    });
};
var newbox3 = function(projId, force) {
    var tpl = '<div class="divcreatplan" style="padding:50px 0 100px 300px;"><a class="creatplan" id="conce" href="javascript:;"><img src="/Content/Images/zh-cn/prodesiman/linedpaperpencil32.png"><b></b></a><a class="creatplan" style="display:none;" id="cplan" href="javascript:;"><img src="/Content/Images/zh-cn/prodesiman/pencil32.png"><b>手动创建计划</b></a><a class="creatplan" id="cotherp" href="javascript:;" style="display:none;"><img src="/Content/Images/zh-cn/prodesiman/linedpaperplus32.png"><b>复制其他项目计划</b></a></div>', nocancel = force ? true : false;
    box(tpl, { boxid: "createP", no_titlebar: true, no_submit: true, modal: false, width: 700, no_cancel: nocancel,
        onOpen: function(o) {
            $(window).resize(function() {
                o.__setPosition();
            });
            o.fbox.css({ 'background': 'none', "border": 'none' });
            $("#cplan").click(function() {
                o.destroy();
                a3plan.createPlan(0, 0, force, true);
            });
            $("#conce").click(function() {
                o.destroy();
                //a3plan_impexp.importExp(force, projId, false);
                a3plan_impexp.importExpFromPlan(force, projId, true);
            });
            $("#cotherp").click(function() {
                o.destroy();
                a3plan_impexp.importExpFromPlan(force, projId, true);
            });
        }
    });
};