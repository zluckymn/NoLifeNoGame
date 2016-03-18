//临时，菜单配置项
var MenuArr = new Array();

MenuArr[0] = [{ "name": "工作平台", "url": "/DesignManage/NewHome_ZHHY" },
    { "name": "设计管理平台", "url": "/DesignManage/EngIndex" },
    { "name": "设计资料库", "url": "/StandardResult/StandardResultLibrary?libId=1" },
    { "name": "部品材料库", "url": "/Material/MaterialStorage" },
    { "name": "外部项目库", "url": "/StandardResult/OutSideProjectIndex" },
      { "name": "设计单位管理", "url": "/Supplier/Designsupplier" },
    { "name": "设计知识库", "url": "/DesignManage/DesignKnowledge?libId=1"},
    { "name": "设计变更管理", "url": "" },
    { "name": "问答系统", "url": "/QuestionAnswer/Index"},
     { "name": "系统设置", "url": "/HumanResource/UserManage"}];
MenuArr[1] = [{ "name": "首页", "url": "/DesignManage/NewHome_ZHHY" },
    { "name": "工作函", "url": "/MailCenter/Index" },
//{ "name": "任务系统", "url": "/TaskManage/Index" },
    {"name": "流程中心", "url": "/WorkFlowInfoCenter/Index" },
    { "name": "设计变更", "url": "/DesignManage/DesignChangeIndex" },
    { "name": "周月计划", "url": "/MonthlyPlanManage/MonthlyPlan" },
//    { "name": "个人月计划", "url": "/MonthlyPlanManage/PersonalMonthlyPlan" },
    {"name": "月计划统计", "url": "/MonthlyPlanManage/MonthlyPlanSummary" }
//    {"name": "设计费审批", "url": "/DesignManage/DesignPay_PersonalFlowIndex" }
    ];
    MenuArr[2] = [{ "name": "全国项目分布", "url": "/DesignManage/EngIndex" }
    //{ "name": "土地分期", "url": "/DesignManage/EngManage" },

    ];

    MenuArr[3] = [
{ "name": "户型库", "url": "/StandardResult/StandardResultLibrary?libId=1" },
{ "name": "规划库", "url": "/StandardResult/StandardResultLibrary?libId=7" },
  { "name": "立面库", "url": "/StandardResult/StandardResultLibrary?libId=4" },
   { "name": "景观库", "url": "/StandardResult/StandardResultLibrary?libId=2" },
{ "name": "装修库", "url": "/StandardResult/StandardResultLibrary?libId=3" },
//{ "name": "示范区库", "url": "/StandardResult/StandardResultLibrary?libId=5" },
    {"name": "售楼处", "url": "/StandardResult/StandardResultLibrary?libId=8" }
    ];
//{ "name": "案例库", "url": "/StandardResult/CaseModelsManage"}];

MenuArr[4] = [{ "name": "材料库", "url": "/Material/MaterialStorage"}];
//    { "name": "苗木库", "url": "/Material/MaterialSeedlings"}
MenuArr[5] = [{ "name": "外部项目库", "url": "/StandardResult/OutSideProjectIndex" }
//,{ "name": "考察楼盘", "url": "/ProjectPeriphery/ProjectPeripherySummary"}
, { "name": "外部材料", "url": "/StandardResult/OutMaterial" }
    ];
MenuArr[6] = [{ "name": "设计单位库", "url": "/Supplier/Designsupplier" }
    ];

MenuArr[7] = [{ "name": "设计知识库", "url": "/DesignManage/DesignKnowledge"}];
//    { "name": "苗木库", "url": "/Material/MaterialSeedlings"}

MenuArr[9] = [{ "name": "问答系统", "url": "/QuestionAnswer/Index"}];
//    { "name": "苗木库", "url": "/Material/MaterialSeedlings"}

MenuArr[10] = [{ "name": "用户管理", "url": "/HumanResource/UserManage" },
    { "name": "部门岗位", "url": "/HumanResource/OrgManage" },
    { "name": "通用岗位", "url": "/HumanResource/ComPostManage" },
    { "name": "流程模板", "url": "/DesignManage/WorkFlowManage" },
     { "name": "角色权限", "url": "/SystemSettings/SystemSettingsPage" },
    { "name": "计划模板", "url": "/DesignManage/CorpExperience" },
     { "name": "系统业态设置", "url": "/DesignManage/PatternManage" },
     { "name": "地铁图", "url": "/DesignManage/SubwayMapList" },
     { "name": "在线模板", "url": "" },
    // { "name": "在线模板", "url": "/DesignManage/BookTaskTemplates" },
     { "name": "评价类型", "url": "/Supplier/SupplierEvaluatItem" },
     { "name": "流程效率统计", "url": "/DesignManage/WorkFlowInstanceStatic" },
     { "name": "信函效率统计", "url": "/MailCenter/MailStatic" },
     { "name": "设计费用模板", "url": "/DesignManage/DesignPayModel?type=1" },
     { "name": "流程审批人员", "url": "/DesignManage/ProjFlowPostUserManageIndex" }
     
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