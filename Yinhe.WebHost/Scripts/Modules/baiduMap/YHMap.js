/*
*
*百度地图API应用封装
*调用例子:var map=new YHMap();
*         map.init("id",{city:"广州",location:"汇景新城"})
*使用时候，需要引入
*<script type="text/javascript" src="http://api.map.baidu.com/api?v=1.5&ak=2b866a6daac9014292432d81fe9b47e3"></script>
*
*
*
*/
var YH_Map = {};
var YHMapCity = [{ n: "从化", g: "113.592263,23.554693" },
                 { n: "深圳", g: "114.064493,22.549258" },
                 { n: "广州", g: "113.270855,23.13547" },
                 { n: "厦门", g: "118.142405,24.496071" },
                 { n: "福州", g: "119.3009,26.082956" },
                 { n: "上海", g: "121.47985,31.236812" },
                 { n: "南京", g: "118.798771,32.064091" },
                 { n: "北京", g: "116.419972,39.918431" },
                 { n: "重庆", g: "106.549607,29.572253" },
                 { n: "宁德", g: "119.555161,26.672541" },
                 { n: "廊坊", g: "116.683,39.553" },
                 { n: "唐山", g: "118.179,39.649" },
                 { n: "天津", g: "117.195,39.110" },
                 { n: "镇江", g: "119.425,32.206" },
                 { n: "苏州", g: "120.588,31.328" },
                 { n: "长沙", g: "112.943,28.262" },
                 { n: "成都", g: "104.068,30.593" },
                 { n: "武汉", g: "114.303,30.631" },
                 { n: "嘉兴", g: "120.757,30.767" },
                 { n: "合肥", g: "117.227,31.831" },
                 { n: "杭州", g: "120.156,30.309"}];
//对象创建入口
function YHMap() {

    this.init = YH_MapInit;

}

//默认初始化参数
var YH_MapInit = function (id, options) {
    var defaults = {
        city: "undefined", //城市名称 (必填)
        location: "undefined", //定位或搜索关键字 
        type: 0,    //地图类型,0为普通百度地图,1为百度卫星图,2为百度3D图
        isDrag: true, //是否可以拖拽地图,默认可以拖拽
        isScrooll: true, //是否可以用滚轮缩放地图,默认可以
        isControl: true, //是否自定义地图控件,默认为百度提供的
        overlay: "default", //取值有default、original、custom，默认default不添加标注,original采用百度提供的默认标注,custom自定义标注
        infoWindow: "default"   //取值有default、original、custom，默认default不添加信息窗口,original采用百度提供的默认信息窗口,custom自定义信息窗口

    };
    YH_Map.params = $.extend({}, defaults, options || {});
    YH_Map.id = id;
    YH_MapFactory();
}

function YH_MapFactory() {

    YH_MapCreate(YH_Map.params);
}
//判断是否存在城市列表,若存在获取对应城市的经纬度,否则返回flase
function YH_MapgetG(city) {
    if (typeof (city) != 'undefined') {
   
        for (var index in YHMapCity) {

            if (YHMapCity[index].n == city) {

                return YHMapCity[index].g;
                break;
            }
        }

    }

}
function YH_MapCreate(params) {
    var map;

    if (params.type == 0) {
        map = new BMap.Map(YH_Map.id);

    }

    else if (params.type == 1) {//百度卫星地图
        map = new BMap.Map(YH_Map.id, { mapType: BMAP_HYBRID_MAP });
    }
    else {
        map = new BMap.Map(YH_Map.id, { mapType: BMAP_PERSPECTIVE_MAP }); //3D地图
        map.setCurrentCity(params.city);
    }
    var point = YH_MapGetPoint(params.city);
    map.centerAndZoom(point, 15);
    if (params.isDrag == true)
        map.enableDragging();
    if (params.isScrooll == true)
        map.enableScrollWheelZoom();
    if (params.isControl == true)
        map.addControl(new BMap.NavigationControl());
    if (typeof (params.location) != 'undefined') {
        var local = new BMap.LocalSearch(map, {
            renderOptions: { map: map }
        });
        local.search(params.location);
    }
    YH_Map.map = map;

}
//经纬度或者地方名称创建point对象
function YH_MapGetPoint(city) {
    var gkey = YH_MapgetG(city);
    var gks = [];
    var point;
    if (gkey != 'undefined') {
        gks = gkey.split(",");
        //   alert(Number(gks[0]))
        //   alert(Number(gks[1]))
        point = new BMap.Point(Number(gks[0]), Number(gks[1]));  // 创建点坐标  
        

    }
    else
        point = new BMap.Point(116.395645, 39.929986); //不传名称默认显示北京的经纬度
    return point;

}
//添加默认标注物
function YH_MapAddMaker(point) {
    var marker = new BMap.Marker(point);
    YH_Map.map.addOverlay(marker);

}
// 添加图片标注,src为图片的路径
function YH_MapAddImgMarker(src, point, index) {
    var myIcon = new BMap.Icon(src, new BMap.Size(23, 25), {
        // 指定定位位置。   
        // 当标注显示在地图上时，其所指向的地理位置距离图标左上    
        // 角各偏移10像素和25像素。您可以看到在本例中该位置即是   
        // 图标中央下端的尖角位置。    
        offset: new BMap.Size(10, 25),
        // 设置图片偏移。   
        // 当您需要从一幅较大的图片中截取某部分作为标注图标时，您   
        // 需要指定大图的偏移位置，此做法与css sprites技术类似。    
        imageOffset: new BMap.Size(0, 0 - index * 25)   // 设置图片偏移    
    });
    // 创建标注对象并添加到地图   
    marker = new BMap.Marker(point, { icon: myIcon });
    //   marker.enableDragging(); //标注可拖拽
    map.addOverlay(marker);
}
//添加自定义的覆盖物
function YH_MapAddCustomMaker(div, point, callbackFun) {
    function SquareOverlay(div) {
        this._point = point;
    };
    // 继承API的BMap.Overlay    
    SquareOverlay.prototype = new BMap.Overlay();
    SquareOverlay.prototype.initialize = function (map) {
        // 保存map对象实例   
        this._map = map;
        // 将div添加到覆盖物容器中   
        map.getPanes().markerPane.appendChild(div);
        // 保存div实例   
        this._div = div;
        // 需要将div元素作为方法的返回值，当调用该覆盖物的show、   
        // hide方法，或者对覆盖物进行移除时，API都将操作此元素。   
        return div;
    }
    // 实现绘制方法   
    SquareOverlay.prototype.draw = function () {
        // 根据地理坐标转换为像素坐标，并设置给容器    
        var position = this._map.pointToOverlayPixel(this._point);
        this._div.style.left = position.x - this._length / 2 + "px";
        this._div.style.top = position.y - this._length / 2 + "px";
    }
    // 实现显示方法    
    SquareOverlay.prototype.show = function () {
        if (this._div) {
            this._div.style.display = "";
        }
    }
    // 实现隐藏方法  
    SquareOverlay.prototype.hide = function () {
        if (this._div) {
            this._div.style.display = "none";
        }
    }
    // 添加自定义方法   
    SquareOverlay.prototype.toggle = function () {
        if (this._div) {
            if (this._div.style.display == "") {
                this.hide();
            }
            else {
                this.show();
            }
        }
    }

    var mySquare = new SquareOverlay(div, point, callbackFun); //暂时不考虑回调函数
    map.addOverlay(mySquare);

}










