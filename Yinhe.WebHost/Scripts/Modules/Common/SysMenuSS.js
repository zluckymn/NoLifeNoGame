/*
//新菜单
//
*/
firstMenus = { "1": { "name": "首页", "url": "/PolicyDecision/Index", "code": "", "visible": false, "isShow": false },
    "2": { "name": "项目库", "url": "/DesignManage/LandIndex", "code": "PROJECTLIB_VIEW", "visible": false, "isShow": true },
    "3": { "name": "产品系列", "url": "/ProductDevelop/ProductSeriesIndex", "code": "PRODUCTDEVPLAT_VIEW", "visible": false, "isShow": true },
    "4": { "name": "外部项目库", "url": "/StandardResult/OutSideProjectIndex", "code": "OUTPROJECTLIB_VIEW", "visible": false, "isShow": true },
    "5": { "name": "部品材料库", "url": "/Material/MaterialStorage", "code": "MATERIALLIB_VIEW", "visible": false, "isShow": true },
    "6": { "name": "专业标准库", "url": "/StandardResult/StandardLibraryIndex", "code": "UNITLIB_VIEW,LANDSCAPELIB_VIEW,DECORATIONLIB_VIEW,FACADELIB_VIEW,CRAFTSLIB_VIEW,DEMAREALIB_VIEW", "visible": false, "isShow": true },
    "7": { "name": "项目资料库", "url": "/DesignManage/ProjDocumentIndex", "code": "SYSPROJECTDATALIB_VIEW", "visible": false, "isShow": true },
    "8": { "name": "系统设置", "url": "/HumanResource/UserManage", "code": "SYSTEMSETTING_VIEW", "visible": false, "isShow": true }
}

/*
//第一个key值代表属于哪个一级菜单
*/
secondMenus = {
    "3": {
        "1": { "name": "产品标准平台", "url": "/ProductDevelop/ProductSeriesIndex", "code": "PRODUCTDEVPLAT_VIEW", "isShow": true },
        "2": { "name": "产品维护", "url": "/ProductDevelop/ProductSeriesManage", "code": "PRODUCTWORTH_UPDATE,PRODUCTDEVPLAT_ADD,PRODUCTDEVPLAT_UPDATE,PRODUCTDEVPLAT_DEL,PRODUCTWORTH_UPDATE,PRODUCTCONFIGSTAND_UPDATE", "isShow": true }
    },
    "5": {
        "1": { "name": "类目管理", "url": "/Material/CatatorySetting", "code": "MATERIALCAT_VIEW", "isShow": true },
        "2": { "name": "基类管理", "url": "/Material/BaseCatSetting", "code": "MATERIALBASE_VIEW", "isShow": true },
        "3": { "name": "品牌管理", "url": "/Material/BrandSetting", "code": "MATERIALBRAND_VIEW", "isShow": true },
        "4": { "name": "供应商管理", "url": "/Material/MatSupplierList", "code": "MATERIALPRO_VIEW", "isShow": true },
        "5": { "name": "材料城市", "url": "/Material/MaterialCity", "code": "MATERIALCITY_VIEW", "isShow": true },
        "6": { "name": "应用项目", "url": "/Material/MaterialProject", "code": "MATERIALCITY_VIEW", "isShow": true }
    },
    "6": {
        "1": { "name": "户型库", "url": "/StandardResult/StandardResultLibrary?libId=1", "code": "UNITLIB_VIEW", "isShow": true },
        "2": { "name": "景观库", "url": "/StandardResult/StandardResultLibrary?libId=2", "code": "LANDSCAPELIB_VIEW", "isShow": true },
        "3": { "name": "室内库", "url": "/StandardResult/StandardResultLibrary?libId=3", "code": "DECORATIONLIB_VIEW", "isShow": true },
        "4": { "name": "立面库", "url": "/StandardResult/StandardResultLibrary?libId=4", "code": "FACADELIB_VIEW", "isShow": true },
        "5": { "name": "工艺工法库", "url": "/StandardResult/StandardResultLibrary?libId=6", "code": "CRAFTSLIB_VIEW", "isShow": true },
        //"6": { "name": "规划库", "url": "/StandardResult/StandardResultLibrary?libId=7", "code": "SYSTEMSETTING_VIEW", "isShow": false },
        "7": { "name": "示范区库", "url": "/StandardResult/StandardResultLibrary?libId=10", "code": "DEMAREALIB_VIEW", "isShow": true }
    },
    "7": {
        "1": { "name": "跨EPS项目资料汇总", "url": "/DesignManage/ProjEpsDocManage/", "code": "SYSPROJECTDATALIB_VIEW", "isShow": true }
    },
    "8": {
        "1": { "name": "用户管理", "url": "/HumanResource/UserManage", "code": "USERMANAGE_VIEW", "isShow": true },
        "2": { "name": "部门岗位管理", "url": "/HumanResource/OrgManage", "code": "ORGMANAGE_VIEW", "isShow": true },
        "3": { "name": "通用岗位管理", "url": "/HumanResource/ComPostManage", "code": "GENERALPOSITION_VIEW", "isShow": true },
        "4": { "name": "决策模板管理", "url": "/PolicyDecision/PolicyDecisionTemplate", "code": "DECISIONMANAGE_VIEW", "isShow": true },
        "5": { "name": "角色权限", "url": "/SystemSettings/SystemSettingsPage", "code": "ROLERIGHT_VIEW", "isShow": true },
        "6": { "name": "项目资料模板设置", "url": "/DesignManage/CorpExperience", "code": "PROJDATATEMPLATE_VIEW", "isShow": true },
        "7": { "name": "业态设置", "url": "/InformationBank/PropertyManage", "code": "PROERTYMANAGE_VIEW", "isShow": true },
        "8": { "name": "专业设置", "url": "/DesignManage/SysProfessionalManage", "code": "PROFESSIONALMANAGE_VIEW", "isShow": true },
        "9": { "name": "阶段设置", "url": "/DesignManage/SysStageManage", "code": "SYSSTAGEMANAGE_VIEW", "isShow": true },
        "10": { "name": "区域城市设置", "url": "/DesignManage/LandAreaCityManage", "code": "AREACITYMANAGE_VIEW", "isShow": true },
        "11": { "name": "文档类别设置", "url": "/DesignManage/SysFileCategory", "code": "SYSFILECATEGORY_VIEW", "isShow": true }
    }
}



var hasRightMenuCode = typeof menuCodes != "undefined", codeObjs; //是否有判断权限的menuCode
var hasShowProjLib = typeof isShowProjLib != "undefined";
var hasShowProj = typeof isShowProj != "undefined";

if (!hasShowProjLib) {
    var isShowProjLib = "";
}

if (!hasShowProj) {
    var isShowProj = "";
}

if (typeof menuCodes != "undefined") {
    codeObjs = $.parseJSON(menuCodes);
}


//展示一级目录
function showFirstMenu() {

    var _hasSysRight_SS = false; //用来标识用户是否有系统的访问权限
    var html = "";
    for (var k in firstMenus) {
        var menuItem = firstMenus[k];
        if (hasRightMenuCode && menuItem.isShow) {
            if (checkRight(menuItem)) {
                _hasSysRight_SS = true;
                html += "<li key='" + k + "'><a hidefocus='true' href='" + menuItem.url + "'>" + menuItem.name + "</a></li>";
            } else { html += "<li key='" + k + "'><a hidefocus='true' class='gray' >" + menuItem.name + "</a></li>"; }
        }
    }

    $(".nav_lev1").html(html);
    return _hasSysRight_SS
}



function SetMenu(first, second) {
    var _hasSysRight_SS = showFirstMenu();
    if (!_hasSysRight_SS) {
       window.location.href = "/RightPage.html";
    }
    if (first in secondMenus) {
        var tempMenus = secondMenus[first];
        var html = "";

        for (var k in tempMenus) {
            var menuItem = tempMenus[k];
            if (menuItem.isShow == true) {
                if (hasRightMenuCode) {
                    if (checkRight(menuItem)) {
                        html += "<li key='" + k + "'><a hidefocus='true' href='" + menuItem.url + "'>" + menuItem.name + "</a></li>";
                    } else {
                        //html += "<li key='" + k + "'><a hidefocus='true' class='gray' href='javascript:void(0)'>" + menuItem.name + "</a></li>";
                    }
                }
            }
        };
        $(".nav_lev2").html(html);
    }
    $(".nav_lev1").find("li[key=" + first + "]").addClass("this");
    if (second) {
        $(".nav_lev2").find("li[key=" + second + "]").addClass("this");
    }
}


//判断能否查看
function checkRight(menuItem) {
    var codes = menuItem.code.split(",");
    if (codes != "SYSPROJECTDATALIB_VIEW" && codes != "PROJECTLIB_VIEW") {
        for (var index in codes) {
            var code = codes[index];
            if (code in codeObjs) {
                return true;
            }
        }
    } else if (codes == "PROJECTLIB_VIEW") {//项目库查看权限
        return checkProjLib(isShowProj);
    }
    else if (codes == "SYSPROJECTDATALIB_VIEW") {//项目资料库权限
        return checkProjLib(isShowProjLib);
    }
    
    return false;
}

function checkProjLib(right) {
    if (right == 'True') {
        return true;
    } else {
        return false;
    }
}