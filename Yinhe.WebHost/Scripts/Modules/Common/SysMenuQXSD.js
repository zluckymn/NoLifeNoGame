//临时，菜单配置项
var MenuArr = new Array();
 
MenuArr[0] = [
    { "name": "市场动态", "url": "http://dcreport.kingold.com/SaleTop/SaleChart", "visible": false },
    { "name": "豪宅设备", "url": "/Equipment", "visible": false },
    { "name": "广州豪宅数据", "url": "/LuxuriousHouse", "visible": false },
    { "name": "销售排行榜", "url": "http://dcreport.kingold.com/SaleTop/index/", "visible": false },
    { "name": "市场报告", "url": "http://dcreport.kingold.com/Report/Index/4", "visible": false }
//    { "name": "系统设置", "url": "/Equipment/HighResidentialDevice", "visible": false}
];
 
// MenuArr[3] = [{ "name": "用户管理", "url": "/HumanResource/UserManage" },
//    { "name": "市调平台管理", "url": "/Equipment/HighResidentialDevice" }
////    { "name": "模块访问量统计", "url": "/Equipment/ModeViewCount" }
//    ];

 

var hasRightMenuCode = typeof ArrMenuRight != "undefined";

function SetUpMenu(index) {
    //MenuArr[0][index - 1].visible = true;
    var html = "<li class='nav_lev1_left'></li>"
               + "<li class='navhome'><a href='http://dcreport.kingold.com/'></a></li>";
    for (var x = 0; x < MenuArr[0].length; x++) {
        var hasMenuRight = hasRightMenuCode == true ? (ArrMenuRight[0][x] == true) : true;
        var nameArr = MenuArr[0][x].name.split(",")[0];
        if (MenuArr[0][x].url != "" && hasMenuRight == true) {
            html += "<li class='firstlevel'><a href='" + MenuArr[0][x].url + "'>" + nameArr + "</a></li>";
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
    $(".nav_lev1").find("li.firstlevel:eq(" + index + ")").addClass("this");
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