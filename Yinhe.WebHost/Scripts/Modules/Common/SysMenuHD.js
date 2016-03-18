//恒大临时，菜单配置项
var MenuArr = new Array();

MenuArr[0] = [{ "name": "首页", "url": "/Project/ProjIndex" },
    { "name": "项目资料库", "url": "/Project/ProjIndex" },
    { "name": "标准化产品库", "url": "/Project/FileLibIndex?typeId=11" },
    { "name": "市场产品信息库", "url": "/Project/CaseLibIndex?typeId=29" },
    { "name": "合作伙伴信息库", "url": "/Supplier/Designsupplier" },
    { "name": "建筑法律法规库", "url": "/Project/FileLibIndex?typeId=23" },
    { "name": "各地建筑规范库", "url": "/Project/FileLibIndex?typeId=26" },
    { "name": "-", "url": "" },
    { "name": "-", "url": "" },
    { "name": "系统设置", "url": "/HumanResource/UserManage"}];

MenuArr[2] = [{ "name": "全国地图分布", "url": "/Project/ProjIndex" },
{ "name": "成果搜索", "url": "/Project/NodeSearch" },
{ "name": "节点管理", "url": "/Project/ProjManage" },
{ "name": "类型管理", "url": "/Project/ObjManage" },
{ "name": "技术指标项管理", "url": "/Project/IndexItem" },
{ "name": "文档搜索", "url": "/Project/FileSearch"}];
//{ "name": "技术指标列管理", "url": "/Project/IndexColumn"}

MenuArr[3] = [{ "name": "设计要求库", "url": "/Project/FileLibIndex?typeId=11" },
    { "name": "审图标准库", "url": "/Project/FileLibIndex?typeId=17" },
    { "name": "设计缺陷库", "url": "/Project/FileLibIndex?typeId=14" },
    { "name": "项目后评估", "url": "/Project/FileLibIndex?typeId=20"}];
    
MenuArr[5] = [{ "name": "", "url": ""}]
MenuArr[4] = [{ "name": "", "url": ""}]
MenuArr[6] = [{ "name": "", "url": ""}]
MenuArr[7] = [{ "name": "", "url": ""}]

MenuArr[10] = [{ "name": "用户管理", "url": "/HumanResource/UserManage" },
{ "name": "权限管理", "url": "/Project/PurviewManage" }
    ];
//var hasRightMenuCode = (isCheckRight==true) && (typeof ArrMenuRight != "undefined");
var hasRightMenuCode = typeof ArrMenuRight != "undefined";
function LoadSecondMenu(i, o) {

    $(".dropmenu").remove();
    i = i + 1;
    if (MenuArr[i]) {
        if (MenuArr[i].length > 1) {
            var t = $(o).offset().top + 34;
            var l = $(o).offset().left;
            var html = '<div onmouseout="HideQuickList(this);" class="dropmenu" style=" top: ' + t + 'px; left: ' + l + 'px;width:110px;"><ul>';
            for (var x = 0; x < MenuArr[i].length; x++) {
                var hasMenuRight = hasRightMenuCode == true ? (ArrMenuRight[i][x] == true) : true;
                if (MenuArr[i][x].url != "" && hasMenuRight == true) {
                    html += '<li><a href="' + MenuArr[i][x].url + '">' + MenuArr[i][x].name + '</a></li>';
                }
            }
            html += "</ul></div>";
            if (html.indexOf("<li>") == -1) {
                return false;
            }
            $(document.body).append(html);
        }
    }
}

function SetUpMenu() {
    var html = "";
    for (var x = 0; x < MenuArr[0].length; x++) {
        var hasMenuRight = hasRightMenuCode == true ? (ArrMenuRight[0][x] == true) : true;
        if (MenuArr[0][x].url != "" && hasMenuRight == true) {
            html += "<li><a href='" + MenuArr[0][x].url + "' onmouseover='LoadSecondMenu(" + x + ", this);'>" + MenuArr[0][x].name + "</a></li>";
        } else {
            html += "<li style='display:none;'><a href='" + MenuArr[0][x].url + "'>" + MenuArr[0][x].name + "</a></li>";
        }
    }
    $(".nav_lev1").html(html);
}

function SetMenu(index, cindex) {
    if (isNaN(cindex)) {
        cindex = findIndex(cindex, index);
        if (cindex == false) cindex = 0;
    }
    SetUpMenu();
    if (MenuArr[index]) {
        var html = "";
        for (var x = 0; x < MenuArr[index].length; x++) {
            var hasMenuRight = hasRightMenuCode == true ? (ArrMenuRight[index][x] == true) : true;
            if (MenuArr[index][x].url != "" && hasMenuRight == true) {
                html += "<li><a href='" + MenuArr[index][x].url + "'>" + MenuArr[index][x].name + "</a></li>";
            } else {
                html += "<li style='display:none;'><a href='" + MenuArr[index][x].url + "'>" + MenuArr[index][x].name + "</a></li>";
            }
        }
        $(".nav_lev2").html(html);
    }
    index--;
    $(".nav_lev1").find("li:eq(" + index + ")").addClass("this");
    if (cindex) { cindex--; $(".nav_lev2").find("li:eq(" + cindex + ")").addClass("this"); }
}

function findIndex(str, index) {
    if (MenuArr[index]) {
        for (var x = 0; x < MenuArr[index].length; x++) {
            if (MenuArr[index][x].name == str) {
                return x + 1;
            }
        }
        return false;
    } else { return false; }
}

function HideQuickList(obj) {
    var mX;
    var mY;
    var vDiv;
    var mDiv;
    vDiv = $(obj);
    mX = window.event.clientX + $(document.body).scrollLeft();
    mY = window.event.clientY + getScrollT();

    if ((mX < (parseInt($(vDiv).offset().left) + 2)) || (mX > (parseInt($(vDiv).offset().left) + $(vDiv).width())) || (mY < (parseInt($(vDiv).offset().top))) || (mY > parseInt($(vDiv).offset().top) + $(vDiv).height())) {
        $(vDiv).hide();
    }
}

//获取滚动条移动top信息
function getScrollT() {
    var t, l, w, h;
    if (document.documentElement && document.documentElement.scrollTop) {

        t = document.documentElement.scrollTop;

    } else if (document.body) {
        t = document.body.scrollTop;

    }
    return t;
}