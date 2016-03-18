//临时，菜单配置项
var MenuArr = new Array();

MenuArr[0] = [{ "name": "首页,", "url": "/DesignManage/Main_HSCZ" },
    { "name": "产品系列", "url": "/ProductDevelop/ProductSeriesIndex" },
    { "name": "项目设计管理", "url": "/DesignManage/EngManage" },
    { "name": "部品材料库", "url": "/Material/MaterialStorage" },
    { "name": "设计单位管理", "url": "/Supplier/Designsupplier" },
    { "name": "专业标准库", "url": "/StandardResult/StandardLibraryIndex" },
     { "name": "系统设置,", "url": "/HumanResource/UserManage"}];

MenuArr[2] = [{ "name": "产品标准平台", "url": "/ProductDevelop/ProductSeriesIndex" },
    { "name": "产品维护", "url": "/ProductDevelop/ProductSeriesManage"}];

MenuArr[3] = [{ "name": "EPS项目分期结构管理", "url": "/DesignManage/EngManage"}];

MenuArr[4] = [{ "name": "部品材料库", "url": "/Material/MaterialStorage" }];


MenuArr[5] = [{ "name": "供应商库", "url": "/Supplier/Designsupplier" },
    { "name": "", "url": "/Supplier/SupplierEvaluatItem"}];

MenuArr[6] = [{ "name": "标准化设置", "url": "" },
    { "name": "户型库", "url": "/StandardResult/StandardResultLibrary?libId=1" },
    { "name": "景观库", "url": "/StandardResult/StandardResultLibrary?libId=5" },
    { "name": "室内库", "url": "/StandardResult/StandardResultLibrary?libId=2" },
    { "name": "立面库", "url": "/StandardResult/StandardResultLibrary?libId=3" }];


MenuArr[7] = [{ "name": "用户管理", "url": "/HumanResource/UserManage" },
    { "name": "部门岗位管理", "url": "/HumanResource/OrgManage" },
    { "name": "", "url": "" },
    { "name": "流程模板管理", "url": "/DesignManage/WorkFlowManage" },
    { "name": "角色权限", "url": "/SystemSettings/SystemSettingsPage"},
      { "name": "计划模板管理", "url": "/DesignManage/CorpExperience" },
       { "name": "系统业态设置", "url": "/DesignManage/PatternManage" },
        { "name": "公告设置", "url": "/DesignManage/NoticeManage" }
    ];


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