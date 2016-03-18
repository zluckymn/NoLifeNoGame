//临时，菜单配置项
var MenuArr = new Array();

MenuArr[0] = [
    { "name": "首页", displayName: "首页", order: 1, "url": "/PersonelWorkCenter/HomeIndex" },
    { "name": "产品系列", displayName: "产品系列", order: 2, "url": "/ProductDevelop/ProductSeriesIndex" },
    { "name": "项目资料管理", displayName: "项目库", order: 3, "url": "/ProjectManage/ProjLandIndex" },
    { "name": "专业库", displayName: "专业库", order: 4, "url": "/StandardResult/UnitIndex" },
    { "name": "工艺工法库", displayName: "工艺工法库", order: 5, "url": "/TecMethod/Index?libId=4" },
    { "name": "部品材料库", displayName: "部品材料库", order: 6, "url": "/Material/MaterialStorage" },
    
    { "name": "土地库", displayName: "土地库", order: 7, "url": "/LandManage/LandIndex" },
    { "name": "系统设置", displayName: "系统设置", order: 8, "url": "/HumanResource/UserManage" }
    ];
MenuArr[1] = [
{ "name": "首页", "url": "/DesignManage/NewHome_XC" }
    ];
MenuArr[2] = [
{ "name": "产品系列展示", "url": "/ProductDevelop/ProductSeriesIndex" },
{ "name": "产品系列管理", "url": "/ProductDevelop/ProductSeriesManage" }
    ];
MenuArr[3] = [
{ "name": "项目聚合", "url": "/ProjectManage/ProjLandIndex" },
//{ "name": "项目展示", "url": "/ProjectManage/ProjLandIndex" },
{ "name": "项目管理", "url": "/ProjectManage/ProjLandIndex?isEdit=1" }
    ];
MenuArr[4] = [
{ "name": "平面标准库", displayName: "平面库", "url": "/StandardResult/UnitIndex" },
{ "name": "立面标准库", displayName: "立面库", "url": "/StandardResult/FacadeIndex" },
{ "name": "室内标准库", displayName: "精装修库", "url": "/StandardResult/FineDecorationIndex" },
{ "name": "景观标准库", displayName: "景观库", "url": "/StandardResult/LandscapeIndex" },
{ "name": "专业库管理", "url": "/StandardResult/StandardResultManage" }
    ];
MenuArr[5] = [
{ "name": "工艺工法库展示", "url": "/TecMethod/Index?libId=4" },
{ "name": "工艺工法库管理", "url": "/TecMethod/Index/?isEdit=1&libId=4" }
    ];
MenuArr[6] = [
{ "name": "材料库", "url": "/Material/MaterialStorage" },
{ "name": "苗木库", "url": "/Material/MaterialSeedlings" },
{ "name": "材料库管理", "url": "/Material/MaterialStorage?isEdit=1" },
{ "name": "苗木库管理", "url": "/Material/MaterialSeedlings?isEdit=1" }
];

MenuArr[7] = [
{ "name": "土地展示", "url": "/LandManage/LandIndex" },
{ "name": "土地管理", "url": "/LandManage/LandIndex?isEdit=1" }
    ];
MenuArr[8] = [
{ "name": "用户管理", "url": "/HumanResource/UserManage" },
{ "name": "部门岗位", "url": "/HumanResource/OrgManage" },
{ "name": "通用岗位", "url": "/HumanResource/ComPostManage" },
{ "name": "角色权限", "url": "/SystemSettings/SystemSettingsPage" },
{ "name": "首页管理", "url": "/PersonelWorkCenter/HomeIndexManage" }
    ];

var hasRightMenuCode = typeof ArrMenuRight != "undefined";
function SetUpMenu(index) {
    //MenuArr[0][index - 1].visible = true;
    var nameArr = "", html = "", hasMenuRight = false, item = null, ligroup = [], newIndex = 0;
    for (var x = 0; x < MenuArr[0].length; x++) {
        item = MenuArr[0][x];
        hasMenuRight = hasRightMenuCode == true ? (ArrMenuRight[0][x] == true) : true;
        if (typeof item.displayName == "string") {
            nameArr = item.displayName;
        } else {
            nameArr = item.name.split(",")[0];
        }

        if (item.url != "" && hasMenuRight == true) {
            html = "<li><a hidefocus='true' href='" + item.url + "'>" + nameArr + "</a></li>";
        } else {
            html = "<li style='display:none;'><a hidefocus='true' href='" + item.url + "'>" + nameArr + "</a></li>";
        }
        ligroup.push({ html: html, newOrder: (typeof item.order === "number") ? parseInt(item.order) : 0, oldOrder: x });
    }
    ligroup = ligroup.sort(function (x, y) {
        return x.newOrder - y.newOrder;
    });
    html = "";
    for (x = 0, html = ""; x < ligroup.length; x++) {
        item = ligroup[x];
        html += item.html;
        if (item.oldOrder == (index - 1)) {
            newIndex = x;
        }
    }
    $(".nav_lev1").html(html);
    $(".nav_lev1").find("li:eq(" + newIndex + ")").addClass("this");
}

function SetMenu(index, cindex) {
    var nameArr = "", html = "", hasMenuRight = false, item = null,ligroup=[],newIndex;
    if (isNaN(index)) {
        index = findIndex(index);
        if (index == false) index = 0;
    }
    if (isNaN(cindex)) {
        cindex = findCIndex(cindex, index);
        if (cindex == false) cindex = 0;
    }
    
    SetUpMenu(index);
    if (MenuArr[index]) {
        for (var x = 0; x < MenuArr[index].length; x++) {
            item = MenuArr[index][x];
            if (typeof item.displayName == "string") {
                nameArr = item.displayName;
            } else {
                nameArr = item.name.split(",")[0];
            }
            var hasMenuRight = hasRightMenuCode == true ? (ArrMenuRight[index][x] == true) : true;
            
            if (item.url != "" && hasMenuRight == true) {
                html = "<li><a hidefocus='true' href='" + item.url + "'>" + nameArr + "</a></li>";
            } else {
                html = "<li style='display:none;'><a hidefocus='true' href='" + item.url + "'>" + nameArr + "</a></li>";
            }
            ligroup.push({ html: html, newOrder: (typeof item.order === "number") ? parseInt(item.order) : 0, oldOrder: x });
        }
        ligroup = ligroup.sort(function (x, y) {
            return x.newOrder - y.newOrder;
        });
        html = "";
        for (x = 0, html = ""; x < ligroup.length; x++) {
            item = ligroup[x];
            html += item.html;
            if (item.oldOrder == (cindex - 1)) {
                newIndex = x;
            }
        }
        $(".nav_lev2").html(html);
    }
    if (cindex) { cindex--; $(".nav_lev2").find("li:eq(" + newIndex + ")").addClass("this"); }
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
                        //MenuArr[0][x].name = str;
                        // MenuArr[0][x].visible = true;
                        return x + 1;
                    }
                }
                return false;
            }
        }
    }
}