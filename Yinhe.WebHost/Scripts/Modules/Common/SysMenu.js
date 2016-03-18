//临时，菜单配置项
var MenuArr = new Array();

MenuArr[0] = [{ "name": "首页", "url": "/ProductDevelop/Index", "visible": false },
    { "name": "产品标准平台", "url": "/ProductDevelop/ProductSeriesIndex", "visible": false },
    { "name": "设计管理平台", "url": "/DesignManage/EngIndex", "visible": false },
    { "name": "产品模块标准", "url": "/StandardResult/StandardLibraryIndex", "visible": false },
    { "name": "部品材料库", "url": "/Material/MaterialStorage", "visible": false },
    { "name": "设计供应商管理", "url": "/Supplier/Designsupplier", "visible": false },
    { "name": "研发基础数据库", "url": "/DevDatabase/StandardManagementLibrary", "visible": false },
    { "name": "土地库", "url": "/ProductDevelop/LandCountryIndex", "visible": false },
    { "name": "客群库", "url": "/ProductDevelop/SegmentLibraryIndex", "visible": false },
     { "name": "系统设置", "url": "/HumanResource/UserManage", "visible": false}];

MenuArr[2] = [{ "name": "产品标准平台", "url": "/ProductDevelop/ProductSeriesIndex" },
    { "name": "产品维护", "url": "/ProductDevelop/ProductSeriesManage"}];

MenuArr[3] = [{ "name": "全国项目分布", "url": "/DesignManage/EngIndex" }
//    { "name": "土地分期", "url": "/DesignManage/EngManage" }
    ];

MenuArr[4] = [
//{ "name": "标准化设置", "url": "/StandardResult/Index" },
    { "name": "户型库", "url": "/StandardResult/StandardResultLibrary?libId=1" },
    { "name": "批量精装修库", "url": "/StandardResult/StandardResultLibrary?libId=2" },
    { "name": "立面库", "url": "/StandardResult/StandardResultLibrary?libId=3" },
    { "name": "示范区库", "url": "/StandardResult/StandardResultLibrary?libId=4" },
    { "name": "景观库", "url": "/StandardResult/StandardResultLibrary?libId=5" },
    { "name": "标准工艺工法", "url": "/StandardResult/StandardResultLibrary?libId=6" },
    { "name": "公共部位库", "url": "/StandardResult/StandardResultLibrary?libId=7" },
    { "name": "设备与技术库", "url": "/StandardResult/StandardResultLibrary?libId=8"}];

MenuArr[5] = [{ "name": "材料库", "url": "/Material/MaterialStorage" },




    { "name": "苗木库", "url": "/Material/MaterialSeedlings"}];

MenuArr[6] = [{ "name": "供应商库", "url": "/Supplier/Designsupplier" },
    { "name": "评价类型管理", "url": "/Supplier/SupplierEvaluatItem"}];

MenuArr[7] = [{ "name": "标准管理文件库", "url": "/DevDatabase/StandardManagementLibrary" },
    { "name": "离散设计要素库", "url": "/DevDatabase/DiscreteDesignLibrary" },
    { "name": "标杆案例库", "url": "/DevDatabase/CaseModelsIndex"}];

MenuArr[10] = [{ "name": "用户管理", "url": "/HumanResource/UserManage" },
    { "name": "部门岗位管理", "url": "/HumanResource/OrgManage" },
    { "name": "通用岗位管理", "url": "/HumanResource/ComPostManage" },
    { "name": "流程模板管理", "url": "/DesignManage/WorkFlowManage" },
    { "name": "角色权限", "url": "/SystemSettings/SystemSettingsPage" },
    { "name": "计划模板管理", "url": "/DesignManage/CorpExperience" },
    { "name": "地铁图编辑", "url": "/DesignManage/SubwayMapList" }

    ];

MenuArr[8] = [{ "name": "土地库首页", "url": "/ProductDevelop/LandCountryIndex" },
    { "name": "土地库管理", "url": "/ProductDevelop/LandLibraryManage"}];

MenuArr[9] = [{ "name": "客群库首页", "url": "/ProductDevelop/SegmentLibraryIndex" },
    { "name": "客群管理", "url": "/ProductDevelop/SegmentItemManage"}];

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