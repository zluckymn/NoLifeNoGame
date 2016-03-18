//临时，菜单配置项
var MenuArr = new Array();

MenuArr[0] = [{ "name": "首页", "url": "/DesignManage/NewHome_ZHTZ", "visible": false },
    { "name": "设计管理平台", "url": "/DesignManage/EngIndex", "visible": false },
    { "name": "系统设置", "url": "/HumanResource/UserManage", "visible": false}];
 
MenuArr[2] = [{ "name": "全国项目分布", "url": "/DesignManage/EngIndex" },
    { "name": "项目计划报表", "url": "/DesignManage/ProjTaskMonth" },
    { "name": "合同与费用支付汇总", "url":"/DesignManage/ProjectPayMent"},
    { "name": "新增项目", "url": "/DesignManage/EngManage" }
    ];
 
MenuArr[3] = [{ "name": "用户管理", "url": "/HumanResource/UserManage" },
    { "name": "部门岗位管理", "url": "/HumanResource/OrgManage" },
    { "name": "通用岗位管理", "url": "/HumanResource/ComPostManage" },
    { "name": "流程模板管理", "url": "/DesignManage/WorkFlowManage" },
    { "name": "角色权限", "url": "/SystemSettings/SystemSettingsPage" },
    { "name": "计划模板管理", "url": "/DesignManage/CorpExperience" },
    { "name": "系统业态设置", "url": "/DesignManage/PatternManage" },
    { "name": "地铁图编辑", "url": "/DesignManage/SubwayMapList" },
      { "name": "在线模板编辑", "url": "/DesignManage/BookTaskTemplates" }      
    ];

var hasRightMenuCode = typeof ArrMenuRight != "undefined";
function SetUpMenu(index) {
    //MenuArr[0][index - 1].visible = true;
    var html = "";
    for (var x = 0; x < MenuArr[0].length; x++) {
        var hasMenuRight = hasRightMenuCode == true ? (ArrMenuRight[0][x] == true) : true;
        var nameArr = MenuArr[0][x].name.split(",")[0];

        if (MenuArr[0][x].url != "" && hasMenuRight == true) {
            html += "<li><a hidefocus='true' href='" + MenuArr[0][x].url + "'>" + nameArr + "</a></li>";
        } else {
            html += "<li style='display:none;'><a hidefocus='true' href='" + MenuArr[0][x].url + "'>" + nameArr + "</a></li>";
        }
    }
    $(".nav_lev1").html(html);
}

function SetMenu(index, cindex) {
    if (isNaN(cindex)) {
        cindex = findCIndex(cindex, index);
        if (cindex == false) cindex = 0;
    }
    if (isNaN(index)) {
        index = findIndex(index);
        if (index == false) index = 0;
    }

    SetUpMenu(index);

    if (MenuArr[index]) {
        var html = "";
        for (var x = 0; x < MenuArr[index].length; x++) {
            var nameArr = MenuArr[index][x].name.split(",")[0];
            var hasMenuRight = hasRightMenuCode == true ? (ArrMenuRight[index][x] == true) : true;
            if (MenuArr[index][x].url != "" && hasMenuRight == true) {
                html += "<li><a hidefocus='true' href='" + MenuArr[index][x].url + "'>" + nameArr + "</a></li>";
            } else {
                html += "<li style='display:none;'><a hidefocus='true' href='" + MenuArr[index][x].url + "'>" + nameArr + "</a></li>";
            }
        }
        $(".nav_lev2").html(html);
    } else {


        $("div.nav_lev2_box").hide();


    }
    index--;
    $(".nav_lev1").find("li:eq(" + index + ")").addClass("this");
    if (cindex) { cindex--; $(".nav_lev2").find("li:eq(" + cindex + ")").addClass("this"); }
}

function findCIndex(str, index) {
    if (MenuArr[index]) {
        for (var x = 0; x < MenuArr[index].length; x++) {
            if (MenuArr[index][x].name == str) {
                return x + 1;
            }
        }
        return false;
    } else { return false; }
}


function findIndex(str) {
    for (var x = 0; x < MenuArr[0].length; x++) {
        var nameArr = MenuArr[0][x].name.split(",");
        if (MenuArr[0][x].name.indexOf(str) != -1) {
            if (MenuArr[0][x].name == str) {
                // MenuArr[0][x].visible = true;
                return x + 1;

            } else {
                for (var y = 0; y < nameArr.length; y++) {
                    if (nameArr[y] == str) {
                        MenuArr[0][x].name = str;
                        // MenuArr[0][x].visible = true;
                        return x + 1;
                    }
                }
                return false;
            }
        }
    }
}