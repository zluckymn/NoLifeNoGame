//临时，菜单配置项
var MenuArr = new Array();

MenuArr[0] = [
    { "name": "我的桌面", "url": "/PersonelWorkCenter/WorkCenter", "visible": false },
    { "name": "产品系列", "url": "/ProductDevelop/ProductSeriesIndex", "visible": false },
    { "name": "项目资料库", "url": "/DesignManage/LandIndex", "visible": false },
    {"name": "设计变更管理", "url": "/DesignManage/DesignChangeIndex", "visible": false },
    { "name": "部品材料库", "url": "/Material/MaterialStorage", "visible": false },
    { "name": "构造库", "url": "/StandardResult/StandardResultLibrary?libId=5", "visible": false },
    { "name": "设计单位库", "url": "/Supplier/Designsupplier", "visible": false },
    //{ "name": "调研数据管理", "url": "/ProductDevelop/LandCountryIndex", "visible": false },
    //{ "name": "高端住宅设备", "url": "/Equipment", "visible": false },
    //{ "name": "广州豪宅销售情况", "url": "/LuxuriousHouse", "visible": false },
    { "name": "系统设置", "url": "/HumanResource/UserManage", "visible": false}];
 
MenuArr[2] = [{ "name": "产品系列首页", "url": "/ProductDevelop/ProductSeriesIndex" },
    { "name": "产品系列管理", "url": "/ProductDevelop/ProductSeriesManage" }
     ];

MenuArr[3] = [{ "name": "项目聚合", "url": "/DesignManage/LandIndex" },
  { "name": "项目管理", "url": "/DesignManage/EngManage" }
//  { "name": "项目经济技术指标管理", "url": "/DesignManage/EngManage" },
//  { "name": "团队和权限管理", "url": "/DesignManage/EngManage" },
//  { "name": "项目资料管理", "url": "/DesignManage/EngManage" },
//  { "name": "方案评审流程", "url": "/DesignManage/EngManage" }
    ];

MenuArr[6] = [
    //{ "name": "标准化设置", "url": "/StandardResult/Index" },
    //{ "name": "户型库", "url": "/StandardResult/StandardResultLibrary?libId=1" },
   // { "name": "景观库", "url": "/StandardResult/StandardResultLibrary?libId=2" },
   // { "name": "室内库", "url": "/StandardResult/StandardResultLibrary?libId=3" },
   // { "name": "立面库", "url": "/StandardResult/StandardResultLibrary?libId=4" },
    {"name": "构造库", "url": "/StandardResult/StandardResultLibrary?libId=5" },
    { "name": "构造库管理", "url": "/StandardResult/StandardResultLibrary?libId=5&isEdit=1" }
    ];

//MenuArr[5] = [{ "name": "材料库", "url": "/Material/MaterialStorage" },

// 
//    { "name": "苗木库", "url": "/Material/MaterialSeedlings"}];

MenuArr[7] = [{ "name": "供应商库", "url": "/Supplier/Designsupplier" },
              { "name": "供应商管理", "url": "/Supplier/Designsupplier?isEdit=1"}
             ];

MenuArr[5] = [{ "name": "材料库", "url": "/Material/MaterialStorage" },
    {"name":"材料库管理","url":"/Material/MaterialStorage?isEdit=1"}
    ];
//MenuArr[7] = [{ "name": "标准管理文件库", "url": "/DevDatabase/StandardManagementLibrary" },
//    { "name": "离散设计要素库", "url": "/DevDatabase/DiscreteDesignLibrary" },
//    { "name": "标杆案例库", "url": "/DevDatabase/CaseModelsIndex"}];
MenuArr[4] = [
        { "name": "设计变更聚合", "url": "/DesignManage/DesignChangeIndex" },
        {"name": "设计变更审批", "url": "/DesignManage/DesignChangePersonalIndex"}
    ];
MenuArr[8] = [{ "name": "用户管理", "url": "/HumanResource/UserManage" },
    { "name": "部门岗位管理", "url": "/HumanResource/OrgManage" },
    { "name": "通用岗位管理", "url": "/HumanResource/ComPostManage" },
    { "name": "流程中心", "url": "/DesignManage/WorkFlowManage" },
    { "name": "角色权限", "url": "/SystemSettings/SystemSettingsPage" },
    { "name": "管理缺陷类型和部位", "url": "/ProductDevelop/ImproveTypeAndPosition" },
    { "name": "管理材料采购方式", "url": "/Material/ProcurementMethodManage" },
    { "name": "首页管理", "url": "/PersonelWorkCenter/HomeIndexManage" },
    { "name": "市调平台管理", "url": "http://172.16.62.11:8010/Equipment/HighResidentialDevice" },
    { "name": "编制单位管理", "url": "/Supplier/EnterpriseManager" }
    ];

//MenuArr[8] = [{ "name": "土地库首页", "url": "/ProductDevelop/LandCountryIndex" },
//    { "name": "土地库管理", "url": "/ProductDevelop/LandLibraryManage"}];

//MenuArr[9] = [{ "name": "客群库首页", "url": "/ProductDevelop/SegmentLibraryIndex" },
//    { "name": "客群管理", "url": "/ProductDevelop/SegmentItemManage"}];

var hasRightMenuCode = typeof ArrMenuRight != "undefined";
function SetUpMenu(index) {
    //MenuArr[0][index - 1].visible = true;
    var html = "";
    for (var x = 0; x < MenuArr[0].length; x++) {
        var hasMenuRight = hasRightMenuCode == true ? (ArrMenuRight[0][x] == true) : true;
        var nameArr = MenuArr[0][x].name.split(",")[0];
        if (MenuArr[0][x].url != "" && hasMenuRight == true) {
            html += "<li><a href='" + MenuArr[0][x].url + "'>" + nameArr + "</a></li>";
        } else {
            html += "<li style='display:none;'><a href='" + MenuArr[0][x].url + "'>" + nameArr + "</a></li>";
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
                html += "<li><a href='" + MenuArr[index][x].url + "'>" + nameArr + "</a></li>";
            } else {
                html += "<li style='display:none;'><a href='" + MenuArr[index][x].url + "'>" + nameArr + "</a></li>";
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

function findRealIndex(firStStr, str) {
    var firstIndex = findIndex(firStStr);
    if (firstIndex) {
        for (var x = 0; x < MenuArr[firstIndex].length; x++) {
            var nameArr = MenuArr[firstIndex][x].name.split(",");
            if (MenuArr[firstIndex][x].name.indexOf(str) != -1) {
                if (MenuArr[firstIndex][x].name == str) {
                    // MenuArr[0][x].visible = true;
                    return x + 1;
                }
            }
        }
    }
    return false;
}

function modifySysManageUrl(sysIndex,secIndex) {
 }