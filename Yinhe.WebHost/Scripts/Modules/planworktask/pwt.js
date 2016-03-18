/*
* pwt.js 工作任务公共  选人等
* 2011/3/8 Qingbao.zhao
*/
var a3Stat = {
    plan: {
        Unconfirmed: "未确认", // 0
        Processing: "进行中", // 1
        Completed: "已完成", // 2
        Revising: "修订中" // 3
    },
    work: {
}
};
var isSignal = false;
var _gtop = window, _gdoc;
while (_gtop.parent != _gtop)
    _gtop = _gtop.parent;
_gdoc = _gtop.document;
var SelTree = function (arg) {
    var val = $('input[name=radType][checked]').val();
    var url = "";
    var args = typeof arg != "undefined" ? ("?orgId=" + arg) : "";
    switch (val) {
        case "orgpost":
            //url = "/HumanResource/OrgPostTree" + args;
            $(_gdoc).find("#ifrm").SelectTree({
                startXml: "/Home/GetOrgAndPostTreeXML?lv=2",
                asynUrl: "/Home/GetOrgAndPostTreeXML",
                defaultShowItemLv: 2,
                _onClick: function (id, name, obj, node) {
                    var param = node.attr("param") || '';
                    if (param.split("_")[0] == 0) { var endStr = "org", id = param.split("_")[1]; } else { var endStr = "orgpost", id = param.split("_")[2]; }
                    GetUserList(name, id, endStr);
                }
            });

            break;
        case "compost":
            url = "/HumanResource/CommonPostTree";
            break;
        case "group":
            url = "/HumanResource/GroupTree?g=1";
            break;
        case "propost":
            url = "/HumanResource/GroupTree?g=1";
            break;
    }
    //_gdoc.getElementById('ifrm').src = url;
}
function showUserList(url, param, selid) {
    $.post(
        url,
        param,
        function (data) {
            //if (data == "") { return false; }
            //data = eval("(" + data + ")");
            $("#" + selid).html("");
            var str = "";

            $.each(data, function (i, item) {

                var isExist = false;
                //判断右边已添加的用户是否已经存在
                $("#selAdd option").each(function () {
                    if ($(this).val() == item.id) {
                        isExist = true;
                        return false;
                    }
                });
                //如果右边不存在该用户则添加
                if (!isExist) {
                    str += "<option value='" + item.id + "'>" + item.name + "</option>";
                }
            });
            if (str != "") $("#selUser").html(str);
            if ($("#msg_pwtselUser")[0]) $("#msg_pwtselUser").hide();
        }, "json"
   );
}
function GetUserList(name, id, type) {
    var url = "/Home/FindUsers?_t=" + new Date().getTime();
    var param = {
        id: id,
        type: type
    };
    showUserList(url, param, "selUser");
}
function GetGroupUsers(id) {
    var url = "/Home/FindUsers";
    var param = {
        id: id,
        type: "group"
    };
    showUserList(url, param, "selAdd");
}

function Addusername(id) {
    var url = "/Home/FindUsersNameById";
    var param = {
        id: id
    };

    $.post(
        url,
        param,
        function (data) {
            if (data == "") { return false; }
            //data = eval("(" + data + ")");
            $.each(data, function (i, item) {
                addtoUserList(id, item.name);
            });
        }, "json"
   );


}
function delbmByid(index, id) {
    //if (index != -1) { G(id).remove(index); }
    var o = $("#" + id).find("option:selected");
    if (o.length == 0) {
        alert("请选择要删除的项！");
    } else {
        o.remove();
    }
}
//用户搜索
function btnSearchUserFunc() {
    var val = $("#txtSearchUser").attr("value");
    //http://bts2.yinhooserv.com/browse/AIII-1954 暂时开放关键词为空的情况的搜索功能
    //if (val == "") { return false; }
    var url = "/Home/SearchUsers", _o = findObjByBoxid("pwtselUser");
    var param = {
        key: val
    };
    $.tmsg("msg_pwtselUser", "查询中...请稍候", { el: _o.fbox, time_out: 3000, infotype: 3 });
    showUserList(url, param, "selUser");

}
function txtSearchUserFunc() {//event

    if (event.keyCode == '13') {
        $("#btnSearchUser").click();
        $(this).focus();
    }

}
function btnTreeSearchFunc() {

    var val = $('input[name=radType][checked]').val();
    var key = $("#txttree").attr("value");
    if ($.trim(key) == "") { return false; }
    window.ifrm.location.href = "/HumanResource/SearchTree?type=" + val + "&key=" + escape(key);

}
function txtTreeKeyPressFunc() {
    if (event.keyCode == '13') {
        $("#btnTreeSearch").click();
        $(this).focus();
    }
}


/*
$('input[name=radType]').click(function() {
SelTree();
})*/



function adds() {



    if ($("#selUser option:selected").length > 0) {
        if (isSignal) {
            if ($("#selUser option:selected").length > 1) {

                showInfoBar("对不起，只能添加一条记录！");
                return;
            }

            if ($("#selAdd option").length > 0) {

                showInfoBar("对不起，已经添加了一条记录，不能继续添加！");
                return;
            }

        }

        $("#selUser option:selected").each(function () {
            $("#selAdd").append("<option value='" + $(this).val() + "'>" + $(this).text() + "</option>");
            $(this).remove();
            SetAddedUser($(this).val());
        })
    }
    else {
        showInfoBar("请选择要添加的记录！");
    }

}

function dels() {
    if ($("#selAdd option:selected").length > 0) {
        $("#selAdd option:selected").each(function () {
            $("#selUser").append("<option value='" + $(this).val() + "'>" + $(this).text() + "</option>");
            $(this).remove();
            SetDeledUser($(this).val());
        })
    }
    else {
        showInfoBar("请选择要删除的记录！");
    }
}

function SetAddedUser(id) {
    var addedUsers = $("#AddedUsers").attr("value");
    var deledUsers = $("#DeledUsers").attr("value");
    var tmp = "," + id + ",";
    if (deledUsers.indexOf(tmp) >= 0) {
        deledUsers = deledUsers.replace(tmp, ",");
        $("#DeledUsers").attr("value", deledUsers);
    } else {
        addedUsers += id + ",";
        $("#AddedUsers").attr("value", addedUsers);
    }



}
function SetDeledUser(id) {
    var addedUsers = $("#AddedUsers").attr("value");
    var deledUsers = $("#DeledUsers").attr("value");
    var tmp = "," + id + ",";


    if (addedUsers.indexOf(tmp) >= 0) {
        addedUsers = addedUsers.replace(tmp, ",");
        $("#AddedUsers").attr("value", addedUsers);
    } else {
        deledUsers += id + ",";
        $("#DeledUsers").attr("value", deledUsers);
    }

}

var a3_plan_selectUser = (function () {
    var opt = {
        multisel: true
    };
    function create() {
        var op = $.extend({}, { boxid: "pwtselUser", title: "选择人员", contentType: "ajax", cls: "shadow-container", width:400 }, opt);
        op.onLoad = function (o) {
            if (!op.multisel) {
                $("input[type='checkbox'][name='sel-userId']").each(function () {
                    var v = $(this).val(), zyid = $(this).attr("zyid");
                    $(this).parent().html('<input type="radio" name="sel-userId" zyid="' + zyid + '" value="' + v + '" />');
                });
            }
            $("#u_prof_usr").click(function () {
                hiConfirm("专业添加完成？", '确定', function (r) {
                    if (r) {
                        box("/Projects/Assignment/ProjManagers/?projId=" + opt.projId + "&type=" + opt.type + "&_t=" + new Date().getTime(), op);
                    }
                });
            });
            $("a[projprofid]").click(function () {
                var projprofid = $(this).attr("projprofid");
                SelectUsers.init({
                    rType: "cb",
                    prt: "selUser",
                    onOpen: function (o) {
                        SelTree();
                    },
                    callback: function (rs) {
                        save(rs, projprofid, o);
                    }
                });
            });
            o.db.find("input[name='btnSch']").click(function () {
                var keyw = $.trim(o.db.find("input[name='u_name']").val()), _zy = o.db.find("select[name='u_prof']").val();
                if (_zy == "-1" && keyw == "") {
                    //$.tmsg("n_cus", "请输入姓名关键字！", { pos: 't-t' });
                    return false;
                }
                var tags = o.db.find("tr[usrs='1']"), txt = "", lastObj, zyi = 0;
                tags.css("background-color", "");
                tags.each(function () {
                    txt = $(this).text();
                    zyi = $(this).find("input").attr("zyid");
                    if (((_zy == "-1" || _zy == zyi) && txt && txt.indexOf(keyw) != -1) || (_zy == zyi && txt == "")) {
                        $(this).css("background-color", "#B6EDF4");
                        lastObj = $(this);
                    }
                });
                if (lastObj) YH.dom.scrollintoContainer(lastObj, $("#n_container"), true, true);
            });
        }
        op.submit_cb = function (o) {
            var rs = {};
            o.db.find("input").each(function () {
                if ($(this).attr("checked")) {
                    rs[$(this).val()] = $(this).parent().next().text();
                    rs[$(this).val() + "_zy"] = $(this).attr("zyid");
                }
            });
            /*$("input[checked]").each(function() {
            rs[$(this).val()] = $(this).parent().next().text();
            rs[$(this).val() + "_zy"] = $(this).attr("zyid");
            alert(rs[$(this).val()]);
            });*/
            op.callback(rs);
            rs = null;
        }
        box("/Home/ProjManagers/?projId=" + opt.projId + "&type=" + opt.type + "&_t=" + new Date().getTime(), op);
    }
    function render(rs, projprofid) {
        var tmp = "", i = 0, len = rs.length, ck = opt.multisel ? "checkbox" : "radio", fckid = 0;
        for (; i < len; ++i) {
            if (i == 0) fckid = rs[i].id;
            tmp += '<tr usrs="1"><td><input type="' + ck + '" zyid="' + projprofid + '" name="sel-userId" value="' + rs[i].id + '" /></td><td class="fontblue">' + rs[i].name + '</td></tr>';
        }
        $("#choseptb" + projprofid).append(tmp);
        $("input[name='sel-userId']").each(function () {
            if (parseInt($(this).val(), 10) == fckid) {
                $(this).attr("checked", "checked");
                return false;
            }
        });
        tmp = null;
    }
    function save(rs, projprofid, o) {
        if ($.isEmptyObject(rs || {})) {
            $.tmsg("selUsermsg", "未选择任何人员！");
            return;
        }
        var ids = "";
        for (var key in rs) {
            ids += key + ",";
        }
        o && o.fbox.mask("保存中...");
        $.ajax({
            type: 'get', cache: false,
            url: '/Home/QuickAddManager?ids=' + ids + "&projProfId=" + projprofid + "&type=" + opt.type,
            success: function (dt) {
                if (dt.success) {
                    render(dt.items, projprofid);
                    o && o.fbox.unmask();
                    $.tmsg("selUser1", "添加成功！", { infotype: 1 });
                } else {
                    $.tmsg("selUser1", dt.msg, { infotype: 2 });
                }
            }
        });
    }
    return {
        init: function (config) {
            opt = $.extend(opt, config || {});
            create();
        }
    }
})();
String.prototype.Tpl = function (o) {
    return this.replace(/\{\w+\}/g, function (_) {
        return String(o[_.replace(/\{|\}/g, "")]) || "";
    });
};
(function ($) {
    $.fn.a3Load = function (url, params, fn) {
        var self = this;
        if (params) {
            if ($.isFunction(params)) {
                fn = params;
                params = null;
            }
        }
        $.ajax({
            url: url,
            data: params,
            success: function (rs) {
                $(self).html(rs);
                fn && fn.call(self);
            },
            dataType: 'html'
        });
    }
})(jQuery);


///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//系统下拉选人功能
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//初始化人员选择列表，将选人下拉框绑定到对应的InputBox上
//传入值：
//o             :对应的InputBox
//userIdObj     :保存UserId的Jquery对象
//profIdObj     :保存用户专业的Jquery对象
//projectId     :项目ID
//type          :对应角色类型
//hasProfSelect :特殊：当项目创建或者导入模板创建时，人员的专业不是在弹出框里选择，这时候给hasProfSelect传入true

var currentListIndex = 0;   //上下选择时当前所在Item是列表中第几个
var thisAObject;            //上下选择时当前所在Item对应的a对象
var currentListedInputBox;  //当前有下拉框的对象
var defaultValueOfCurrentListedInputBox;    //当前有下拉框的对象的默认值
var defaultValueOfuserIdObj, defaultValueOfprofIdObj;
var userIdObject, profIdObject;

var bindASearchableInput = function (o, userIdObj, profIdObj, projectId, type, hasProfSelect, needsearch) {
    userIdObject = userIdObj;
    profIdObject = profIdObj;

    $(o).unbind("focus").bind("focus", function () {
        showTaskPeopleList(o, userIdObj, profIdObj, projectId, type, hasProfSelect, needsearch);
    });
    $(o).unbind("keyup").bind("keyup", function () {
        addKeyboardControlSupport(o, userIdObj, profIdObj, projectId, type, hasProfSelect);
    });
    $(o).unbind("click").bind("click", function () {
        showTaskPeopleList(o, userIdObj, profIdObj, projectId, type, hasProfSelect, needsearch);
    });
}

function addKeyboardControlSupport(o, userIdObj, profIdObj, projectId, type, hasProfSelect) {
    var keycode = event.keyCode;
    //    var currentListIndex = $("#aDivToRecordCurrentListIndex").text();
    //    if (currentListIndex.length == 0) {
    //        $("body").append('<div id="aDivToRecordCurrentListIndex">0</div>')
    //        currentListIndex = $("#aDivToRecordCurrentListIndex").text();
    //    }
    //var keycode = window.event ? event.keyCode : event.which;

    switch (keycode) {
        case 38:
            { //上
                currentListIndex -= 1;
                setCurrentListIndexItemStateToBeOnMouseOver();
                ajustScrollPositionAccordingToCurrentListIndex();
            } break;
        case 40:
            { //下
                currentListIndex += 1;
                setCurrentListIndexItemStateToBeOnMouseOver();
                ajustScrollPositionAccordingToCurrentListIndex();
            } break;
        case 13:
            { //回车
                confirmTheItemSelection(thisAObject, o, userIdObj, profIdObj, /*hasProfSelect*/true);
            } break;
        default:
            {
                showSearchTaskPeopleList(o, userIdObj, profIdObj, projectId, type, /*hasProfSelect*/true);
            }
    }
}

//上下键选择的项目高亮
function setAnItemHoverStyle(anAObject) {
    $(anAObject).addClass("aui-list-item-link-hover");
}

//上下键失去选择的失去高亮
function removeHoverStyleOfAnItem(anAObject) {
    $(anAObject).removeClass("aui-list-item-link-hover");
}

//根据 currentListIndex 的值调整滚动条的位置
function ajustScrollPositionAccordingToCurrentListIndex() {
    $("div.aui-list").animate({ scrollTop: currentListIndex * 25 - 50 }, "fast");
}

//使得某一个 Item 处于鼠标停在上面的状态
function setCurrentListIndexItemStateToBeOnMouseOver() {
    if (currentListIndex > $("ul.aui-list-section").find("a").length - 1) {
        currentListIndex = currentListIndex - $("ul.aui-list-section").find("a").length;
    }
    if (currentListIndex < 0) {
        currentListIndex = $("ul.aui-list-section").find("a").length + currentListIndex;
    }
    removeHoverStyleOfAnItem(thisAObject);
    thisAObject = $("ul.aui-list-section").find("a").eq(currentListIndex);
    setAnItemHoverStyle(thisAObject);
    //$(currentListedInputBox).val(thisAObject.attr("name"));
}

//给相应的 Item 绑定鼠标停在上面的会切换到鼠标停在上面的状态的事件
function bindOnMouseOverItemStateToBeOnMouseOver(thisAObject) {
    $(thisAObject).unbind('mouseover').bind('mouseover', function () {
        currentListIndex = $(this).parent().index();
        setCurrentListIndexItemStateToBeOnMouseOver();
    });
}

function confirmTheItemSelection(thisAObject, o, userIdObj, profIdObj, hasProfSelect) {
    hideAllTaskPeopleList();

    //如果是在项目创建的地方，直接回写专业
    if (!hasProfSelect) {
        if ($(thisAObject).attr("sysProfId") == "null") {                               // 为什么居然是 == "null"
            SelectProfWithPerson($(thisAObject).attr("sysProfId"), o, $(thisAObject).text(), $(thisAObject).attr("id"), userIdObj, profIdObj);
        } else {
            var _sysProfName = $(thisAObject).attr("sysProfName") + '';
            if (_sysProfName != '') $.tmsg("confirmPeopleSelection", "已为该人员自动选择" + _sysProfName + "专业", { time_out: 1000, infotype: 1 });
            $(profIdObj).val($(thisAObject).attr("sysProfId"));
            $(o).val($(thisAObject).attr("name"));
            $(userIdObj).val($(thisAObject).attr("id"));
        }
        if ($("#setMytoList").attr("username") != $(thisAObject).attr("name")) { $("#setMytoList").show(); } else { $("#setMytoList").hide(); $("#setMytoListProf").hide(); }
    } else {
        SelectProfWithPerson($(thisAObject).attr("sysProfId"), o, $(thisAObject).text(), $(thisAObject).attr("id"), userIdObj, profIdObj);
    }
}

//搜索系统人员
function getSearchTaskPeopleList(o, userIdObj, profIdObj, hasProfSelect) {
    currentListIndex = 0;
    var urls = {
        getTaskPeople: '/Home/PlanRelatedPersonnel?r=' + Math.random(),  //取当前项目相关人员
        searchTaskPeople: '/Home/SearchUsers?r=' + Math.random()        //根据关键字查询系统用户
    }

    var searchKey = $(o).attr("value");
    var ListRom = $("ul.aui-list-section");
    ListRom.html("");

    $.ajax({
        type: "POST",
        url: urls.searchTaskPeople,
        data: "key=" + searchKey,
        success: function (msg) {
            var searchPeopleList = "";
            reSizePeopleList(msg.length);
            $(msg).each(function () {
                var indexOfKeywords = this.name.toLowerCase().indexOf(searchKey.toLowerCase());
                searchPeopleList = searchPeopleList + '<li><a class="aui-list-item-link" href="javascript:;" id="' + this.id
                                                    + '" sysProfId="' + this.sysProfId + '" name="' + this.name
                                                    + '" >';
                searchPeopleList = searchPeopleList + this.name.substring(0, indexOfKeywords);
                searchPeopleList = searchPeopleList + '<font color="#FF0000">'
                                                    + this.name.substring(indexOfKeywords, indexOfKeywords + searchKey.length) + '</font>';
                searchPeopleList = searchPeopleList + this.name.substring(indexOfKeywords + searchKey.length);
                searchPeopleList = searchPeopleList + '</a></li>';
            });
            if (searchPeopleList == "") {
                ListRom.html("未搜索到相关人员");
            } else {
                ListRom.html(searchPeopleList);
            }

            ListRom.find("a").each(function () {
                bindOnMouseOverItemStateToBeOnMouseOver(this);
                $(this).click(function () {
                    confirmTheItemSelection(this, o, userIdObj, profIdObj, /*hasProfSelect*/true);
                });
            });
        }
    });
    $("div#recordLastSearchInput:last").text(searchKey);
}

//获取当前项目已经处于人员列表的项目成员
function getTaskPeopleList(o, userIdObj, profIdObj, projectId, type) {
    currentListIndex = 0;
    var urls = {
        getTaskPeople: '/Home/PlanRelatedPersonnel?r=' + Math.random(), //取当前项目相关人员
        searchTaskPeople: '/Home/SearchUsers?r=' + Math.random() //根据关键字查询系统用户
    }

    $.getJSON(urls.getTaskPeople, { projId: projectId, type: type }, function (data) {
        data = data.Data;
        if (data.Success) {
            var taskPeopleList = "";
            //写入加载信息
            $("ul.aui-list-section").html("人员读取中...");

            reSizePeopleList(data.Items.length);
            $(data.Items).each(function () {
                taskPeopleList = taskPeopleList + '<li><a class="aui-list-item-link" href="javascript:;" name="' + this.name + '" id="' + this.id
                                                            + '" sysProfId="' + this.sysProfId + '" sysProfName="' + this.sysProfName + '">'
                                                            + this.name + '</a></li>';//（' + this.sysProfName + '）
            });
            if (taskPeopleList == "") {
                $("ul.aui-list-section").html("<div style='line-height:20px;color:#ff0000; padding-left:5px;'>暂无人员，请从系统中选择。</div>");
            } else {
                $("ul.aui-list-section").html(taskPeopleList);
                $("ul.aui-list-section").find("a").each(function () {
                    bindOnMouseOverItemStateToBeOnMouseOver(this);
                    $(this).click(function () {
                        confirmTheItemSelection(this, o, userIdObj, profIdObj, /*false*/true);
                    });
                });
            }
        } else {
            outputJSONLoadingErrorMessage(data);
        }
        $("div#recordLastSearchInput:last").text($(o).attr("value"));
    });
}

//根据给出的 userId 读取专业列表，并生成 options 返回
function returnOptionListOfProfFromJSONData(data) {
    var options = "";
    for (var x = 0; x < data.length; x++) {
        options += '<option';
        if (data[x].check) {
            options += ' selected="selected"';
        }
        options += ' value="' + data[x].id + '">' + data[x].name + '</option>';
    }
    return options;
}

//为从系统中选择的人员添加专业
//新增输入参数
//ProfId        :从系统中的人员得到的专业ID
function SelectProfWithPerson(ProfId, o, name, id, userIdObj, profIdObj) {
    $(o).val(name);
    $(userIdObj).val(id);
    $(profIdObj).val('');
    $(o).focus();
    $(".ajs-layer").hide();
    /*$.get("/Home/GetProfJson", { userId: id }, function (data) {
        box("<div style='padding:10px 5px; line-height:22px;color:#0187dc'>请为该人员选择一个专业：<br/><select id=SelectProfWithPersons style='width:260px'>" + returnOptionListOfProfFromJSONData(data) + "</select></div>", { title: "选择专业", contentType: 'html', cls: 'shadow-container',
            submit_cb: function () {
                $(o).val(name);
                $(userIdObj).val(id);
                $(profIdObj).val($("#SelectProfWithPersons").val());
                $(o).focus();
            },
            onOpen: function () {
                if (ProfId != null) {
                    $("#SelectProfWithPersons").val(ProfId);
                    if ($("#setMytoList").attr("username") != name) { $("#setMytoList").show(); } else { $("#setMytoList").hide(); $("#setMytoListProf").hide(); }
                }
            }
        });

    }, "json");*/

}

//初始化人员选择框HTML结构
function initTaskPeopleList(addself) {
    $(".ajs-layer").remove();
    var _gn = typeof _guserName != 'undefined' ? _guserName : '';
    var TaskPPList = $('<div class="ajs-layer" style="width: 200px; display:none;z-index:1000;background:#fff;border-bottom:#bbb 1px solid"><div class="aui-list" style="width: auto; display: block; white-space: nowrap; height:25px; overflow:auto; overflow-x:hidden;"><ul class="aui-list-section">人员读取中....</ul></div><div id="selectingListBtnBar" style="background-color:#efefef;border-left:#bbb 1px solid ;border-right:#bbb 1px solid ;text-align:center; padding:2px 0;"><input id="selectUserForProf" type="button" value="从系统中选择人员"/>' + (typeof addself != 'undefined' && addself ? ' <a href="javascript:;" id="selectSelf">' + _gn + '</a>' : '') + '</div></div>');
    $("body").append(TaskPPList);
}

//重画人员框的高度
function reSizePeopleList(i) {
    var h = 25;
    if (i < 5 && i > 0) { h = i * 25; }
    if (i > 4) { h = 100; }
    $("div.aui-list").animate({ height: h + "px" });
}

//检查人员选择框是否存在
function isTaskPeopleListExist(projectId) {
    return ($(".ajs-layer").length != 0);
}

//初始化输入值过往记录
function initADivToRecordLastSearchInput() {
    $("body").append('<div id="recordLastSearchInput" style="display:none;"></div>');
}

//检查是否存在输入过往记录
function isADivToRecordLastSearchInputExist() {
    return ($("div#recordLastSearchInput").length != 0);
}

//判断输入框是否为空
function isSearchKeyInputed(o) {
    return ($(o).attr("value") != "");
}

//判断输入框的值是否改变
function isSearchKeyChanged(o) {
    return ($(o).attr("value") != $("div#recordLastSearchInput:last").text());
}

function zindex(obj) {
    var topindex = 0;
    $("div").each(function () {
        if (parseInt($(this).css("z-index")) > topindex && parseInt($(this).css("z-index")) < 10000)
        { topindex = parseInt($(this).css("z-index")); }
    });

    $(obj).css("z-index", topindex + 2);
}

//显示人员选择列表
function showAllTaskPeopleList(o) {
    if ($(o).attr('canempty') != '1') currentListedInputBox = o;
    defaultValueOfCurrentListedInputBox = o.val();
    if (profIdObject) { defaultValueOfprofIdObj = profIdObject.val(); }
    if (userIdObject) { defaultValueOfuserIdObj = userIdObject.val(); }
    $(".ajs-layer").css("display", "inline");
    $(".ajs-layer").css("position", "absolute");
    $(".ajs-layer").css("left", $(o).offset().left);
    $(".ajs-layer").css("top", $(o).offset().top + $(o).height());
    //重置Div的Index值，保持其处于最顶部
    zindex($("div.ajs-layer")[0]);
}

//显示人员选择列表，根据坐标
function showAllTaskPeopleListXY(x, y) {

    $(".ajs-layer").css("display", "inline");
    $(".ajs-layer").css("position", "absolute");
    $(".ajs-layer").css("left", x);
    $(".ajs-layer").css("top", y);
    //重置Div的Index值，保持其处于最顶部
    zindex($("div.ajs-layer")[0]);
}

//隐藏人员选择列表
function hideAllTaskPeopleList() {
    $(".ajs-layer").css("display", "none");
}

//根据给出的JSON数据返回人员下拉列表
//function returnPeopleLiHTML(data, isCheckBoxRequired) {
//}

//显示人员选择窗体，并初始化系统选人框
function showTaskPeopleList(o, userIdObj, profIdObj, projectId, type, hasProfSelect, needsearch) {
    if (!isADivToRecordLastSearchInputExist()) {
        initADivToRecordLastSearchInput();
    }
    initTaskPeopleList(needsearch);
    getTaskPeopleList(o, userIdObj, profIdObj, projectId, type);
    showAllTaskPeopleList(o);
    $("#selectUserForProf").click(function () {
        hideAllTaskPeopleList();
        SelectUsers.init({
            rType: "cb",
            prt: "selUser",
            multiSel: false,
            single: true,
            onOpen: function (o) {
                if (needsearch) {
                    o.db.find("#txttree").val('深圳地产设计管理中心');
                    o.db.find("#btnTreeSearch").trigger('click');
                    var ifrm = o.db.find("#ifrm");
                    if (ifrm.length) {
                        ifrm = ifrm[0];
                        if (ifrm.attachEvent) {
                            ifrm.attachEvent("onload", function () {
                                var _db = $(ifrm.contentWindow.document.body);
                                _db.find("span:first").trigger('click');
                            });
                        } else {
                            ifrm.onload = function () {
                                var _db = $(ifrm.contentWindow.document.body);
                                _db.find("span:first").trigger('click');
                            };
                        }
                    }
                } else {
                    SelTree();
                }
            },
            callback: function (rs) {
                var ids = "";
                for (var key in rs) {

                    if (/*hasProfSelect == true*/1) {
                        $(o).val(rs[key]);
                        $(userIdObj).val(key);
                        if ($("#setMytoList").attr("username") != rs[key]) { $("#setMytoList").show(); } else { $("#setMytoList").hide(); $("#setMytoListProf").hide(); }
                    } else {
                        SelectProfWithPerson(null, o, rs[key], key, userIdObj, profIdObj);
                    }

                }
            }
        });
    });
    if (needsearch) {
        var btnself = $("#selectSelf");
        btnself.attr({ 'id': _guserId, 'name': _guserName, 'sysProfId': 'null' });
        btnself.click(function () {
            confirmTheItemSelection(this, o, userIdObj, profIdObj, /*false*/true);
        });
    }
}

//根据情况显示搜索用户列表框还是项目人员列表
function showSearchTaskPeopleList(o, userIdObj, profIdObj, projectId, type, hasProfSelect) {
    if (isSearchKeyChanged(o) && isSearchKeyInputed(o)) {
        getSearchTaskPeopleList(o, userIdObj, profIdObj, hasProfSelect);
    }
    if (isSearchKeyChanged(o) && !isSearchKeyInputed(o)) {
        getTaskPeopleList(o, userIdObj, profIdObj, projectId, type);
    }
}

//系统下拉选人单选事件
function getTaskPeopleListForOneClick(x, y, projectId, type, cAjax) {
    initTaskPeopleList();
    var urls = {
        getTaskPeople: '/Home/PlanRelatedPersonnel?r=' + Math.random(), //取当前项目相关人员
        searchTaskPeople: '/Home/SearchUsers?r=' + Math.random() //根据关键字查询系统用户
    }

    //显示弹窗
    showAllTaskPeopleListXY(x, y);
    //写入加载信息
    $("ul.aui-list-section").html("人员读取中...");

    //读取人员列表
    $.getJSON(urls.getTaskPeople, { projId: projectId, type: type }, function (data) {
        data = data.Data;
        if (data.Success) {
            var taskPeopleList = "";
            $("ul.aui-list-section").html("");
            reSizePeopleList(data.Items.length);
            $(data.Items).each(function () {
                taskPeopleList = taskPeopleList + '<li><a class="aui-list-item-link" href="javascript:;" name="' + this.name + '" id="' + this.id
                                                            + '" sysProfId="' + this.sysProfId + '">'
                                                            + this.name + '</a></li>'; //（' + this.sysProfName + '）
            });

            //置入数据
            $("ul.aui-list-section").html(taskPeopleList);
            $("ul.aui-list-section").find("a").each(function () {
                bindOnMouseOverItemStateToBeOnMouseOver(this);
                $(this).click(function () {
                    hideAllTaskPeopleList();
                    cAjax($(this).attr("id"), $(this).attr("sysProfId"), $(this).attr("name"));
                });
            });
            $("#selectUserForProf").click(function () {
                hideAllTaskPeopleList();
                SelectUsers.init({
                    rType: "cb",
                    prt: "selUser",
                    multiSel: false,
                    single: true,
                    onOpen: function (o) {
                        SelTree();
                    },
                    callback: function (rs) {
                        var ids = "";
                        for (var key in rs) {

                            // SelectProfWithPersonForOneClick(rs[key], key, cAjax);
                            cAjax(key, '', rs[key]);
                        }
                    }
                });
            });

        } else {
            outputJSONLoadingErrorMessage(data);
        }
    });
}

//为从系统中选择的人员添加专业(下拉单选)
//新增输入参数
//ProfId        :从系统中的人员得到的专业ID
function SelectProfWithPersonForOneClick(name, id, cAjax) {

    $.get("/Home/GetProfJson", { userId: id }, function (data) {
        box("<div style='padding:10px 5px;line-height:22px;color:#0187dc'>请为该人员选择一个专业：<br/><select id=SelectProfWithPersons style='width:260px'>" + returnOptionListOfProfFromJSONData(data) + "</select></div>", { title: "选择专业", contentType: 'html', cls: 'shadow-container',
            submit_cb: function () {
                cAjax(id, $("#SelectProfWithPersons").val(), name);
            }
        });

    }, "json");

}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//系统下拉选人功能
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//系统下拉多选选人功能（使用了部分单选列表的函数）
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//初始化人员多选选择列表，将选人下拉框绑定到对应的Object上
//传入值：
//o             :对应的Object
//projectId     :项目ID
//type          :对应角色类型

//主函数，展示一个多选的人员下拉框
function showAMultipleSelectingTaskPeopleList(o, projectId, type, callback) {
    initTaskPeopleList();
    getAMultipleSelectingTaskPeopleList(o, projectId, type, callback);
    showAllTaskPeopleList(o);
    bindSelectPersonFromSystemBtnFunction(o);
}

//绑定“从系统中选择人员”按钮的事件
function bindSelectPersonFromSystemBtnFunction(o) {
    $("#selectUserForProf").click(function () {
        hideAllTaskPeopleList();
        SelectUsers.init({
            rType: "cb",
            prt: "selUser",
            multiSel: false,
            single: true,
            onOpen: function (o) {
                SelTree();
            },
            callback: function (rs) {
                var ids = "";
                for (var key in rs) {
                    selectProfWithPersonForMultiSelectList(o, rs[key], key);
                }
            }
        });
    });
}


//将从系统中增加的人员添加到多选列表的头部
function appendAPersonToTaskPeopleList(o, userId, name, profId, profName) {
    var appendContent = "";
    appendContent = appendContent + '<li><a class="aui-list-item-link" href="javascript:;" name="' + name + '" id="' + userId
                                                            + '" sysProfId="' + profId + '">'
                                                            + '<input type=checkbox checked="checked" />'
                                                            + name + '（' + profName + '）</a></li>';
    $("ul.aui-list-section li:first-child").before(appendContent);
    reSizePeopleList($("ul.aui-list-section").find("input").length);
    showAllTaskPeopleList(o);
}

//多选下拉框为该人员选择专业
function selectProfWithPersonForMultiSelectList(o, name, id) {
    $.get("/Home/GetProfJson", { userId: id }, function (data) {
        box("<div style='padding:10px 5px;line-height:22px;color:#0187dc'>请为该人员选择一个专业：<br/><select id=SelectProfWithPersons style='width:260px'>" + returnOptionListOfProfFromJSONData(data) + "</select></div>", { title: "选择专业", contentType: 'html', cls: 'shadow-container',
            submit_cb: function () {
                appendAPersonToTaskPeopleList(o, id, name, $("#SelectProfWithPersons").val(), $("#SelectProfWithPersons").find("option[selected]").text());
            }
        });
    }, "json");

}

//点击下拉框的确定按钮后的操作
function confirmMultiplePeopleSelection(o, callback) {
    var selectedTaskPeople = "";
    var rd = {};

    hideAllTaskPeopleList();
    $(o).parent().parent().find("input[checked]").each(function () {
        var parentALable = $(this).parent();
        selectedTaskPeople = selectedTaskPeople + parentALable.attr("id") + "-" + parentALable.attr("sysProfId") + ",";
        rd[parentALable.attr("id")] = parentALable.attr("name");
    });
    selectedTaskPeople = selectedTaskPeople.substring(0, selectedTaskPeople.length - 1);

    postPeopleDataSelected(o, selectedTaskPeople, rd, callback);
}

//将选择并确定的数据传送给callback函数处理
function postPeopleDataSelected(o, ids, rd, callback) {
    if (ids == "") return false;
    callback(ids, rd);
}

//在多选下拉框末尾的适当位置增加一个“确定”按钮
function appendAnOKButtonToTaskPeopleList(projectId, callback) {
    $("#selectingListBtnBar").append('<input id="confirmMultiplePeopleSelectionbtn" type="button" value="确定" />');
    $("#confirmMultiplePeopleSelectionbtn").unbind("click").bind("click", function () {
        confirmMultiplePeopleSelection(this, callback);
    });
}

//切换被点击的a标签内的checkbox的选中状态
function switchAllCheckBoxState(o) {
    if ($(o).attr("checked") == "false") {
        $(o).attr("checked", "true");
        $(o).find("input").attr("checked", false);
    } else {
        $(o).attr("checked", "false");
        $(o).find("input").attr("checked", true);
    }
}

//a 标签内的 checkbox 被点击时选中状态的切换，解决a标签被点击的事件与checkbox被点击的事件相互抵消的问题
function bindCheckBoxClickFuncion(o) {
    if ($(o).parent().attr("checked") == "false") {
        $(o).attr("checked", false);
    } else {
        $(o).attr("checked", true);
    }
}

//获取数据，生成多选下拉框的选项列表
function getAMultipleSelectingTaskPeopleList(o, projectId, type, callback) {
    var urls = {
        getTaskPeople: '/Home/PlanRelatedPersonnel?r=' + Math.random(), //取当前项目相关人员
        searchTaskPeople: '/Home/SearchUsers?r=' + Math.random() //根据关键字查询系统用户
    }
    $.getJSON(urls.getTaskPeople, { projId: projectId, type: type }, function (data) {
        data = data.Data;
        if (data.Success) {
            var taskPeopleList = "";
            $("ul.aui-list-section").html("");

            reSizePeopleList(data.Items.length);
            $(data.Items).each(function () {
                taskPeopleList = taskPeopleList + '<li><a class="aui-list-item-link" href="javascript:;" name="' + this.name + '" id="' + this.id
                                                            + '" sysProfId="' + this.sysProfId + '">'
                                                            + '<input type=checkbox />'
                                                            + this.name + '（' + this.sysProfName + '）</a></li>';
            });
            $("ul.aui-list-section").append(taskPeopleList);
            $("ul.aui-list-section").find("a").each(function () {
                bindOnMouseOverItemStateToBeOnMouseOver(this);
                $(this).click(function () {
                    switchAllCheckBoxState(this);
                });
                $(this).find("input").each(function () {
                    $(this).click(function () {
                        bindCheckBoxClickFuncion(this);
                    });
                });
            });
            appendAnOKButtonToTaskPeopleList(projectId, callback);
        } else {
            outputJSONLoadingErrorMessage(data);
        }
    });
}

//当试图获取JSON的时候返回 data.success = false ，在下拉列表输出错误信息
function outputJSONLoadingErrorMessage(data) {
    $("ul.aui-list-section").html('<li>' + data.Message + '</li>');
}

//当鼠标点击下拉框以外的地区时隐藏下拉框，并将输入框内的值置于初始值
$(document.body).mousedown(function () {
    if ($(".ajs-layer").css("display") != "none") {
        var x = window.event.x;
        var y = window.event.y + document.documentElement.scrollTop;
        var div = $(".ajs-layer");
        var dof = div.offset();
        if (dof) {
            if (x < dof.left ||
        y < dof.top ||
        x > dof.left + div.width() ||
        y > dof.top + div.height()
        ) {
                div.fadeOut();
                if (currentListedInputBox) { currentListedInputBox.val(defaultValueOfCurrentListedInputBox); }
                if (userIdObject) { userIdObject.val(defaultValueOfuserIdObj); }
                if (profIdObject) { profIdObject.val(defaultValueOfprofIdObj); }
            }
        }

    }
});


///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//系统下拉多选选人功能
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////