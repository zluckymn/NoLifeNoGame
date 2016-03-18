var locater = null;

function ElemLocater(ge) {
    this.ge = ge;
    this.elemList = new Array();
    this.currentElem = null;

    var polyLineDefaultHeight = 10;

    this.polyLineList = new Array();
    //输入的一个coor应该是一个形如[{ "Ya" : "36.07029634608144","Za" : "120.36496639251709"}, {"Ya" : "36.07109417220052","Za" : "120.36492347717285"}, {"Ya" : "36.067070270998464","Za" : "120.36801338195801"}, {"Ya" : "36.07088604442791","Za" : "120.36406517028808"}], [{"Ya" : "36.08018188118015","Za" : "120.35831451416015"}, {"Ya" : "36.08011251333357","Za" : "120.36681175231933"}, {"Ya" : "36.07518723980868","Za" : "120.3658676147461"}, {"Ya" : "36.07671341388517","Za" : "120.35840034484863"}]
        //的JSON对象参数
    //不直接传入JSON对象的原因是为了和地图类有同一个接口
    this.addPolyline = function (coorList) {
      
        var LineString = ge.createLineString('');
        LineString.setExtrude(true);
        LineString.setAltitudeMode(ge.ALTITUDE_RELATIVE_TO_GROUND);

        for (var i = 0; i != coorList.length; i++) {
            var coor = coorList[i];
            LineString.getCoordinates().pushLatLngAlt(parseFloat(coor.Ya), parseFloat(coor.Za), polyLineDefaultHeight);
        }
        var coor = coorList[0]; //再次加入第一个点让首尾闭合
        LineString.getCoordinates().pushLatLngAlt(parseFloat(coor.Ya), parseFloat(coor.Za), polyLineDefaultHeight);

        var polyLine = ge.createPlacemark("");
        polyLine.setGeometry(LineString);

        if (polyLine.getStyleSelector() == null) {
            polyLine.setStyleSelector(ge.createStyle(''));
        }
        var lineStyle = polyLine.getStyleSelector().getLineStyle();
        lineStyle.setWidth(5);
        lineStyle.getColor().set("FF0000FF");  // aabbggrr format
        this.polyLineList.push(polyLine);
        ge.getFeatures().appendChild(polyLine);
        
    }

    locater = this;
    var thisObj = this;

    this.defaultElemPolygonOpt = {
        "vts": 4,               //顶点数
        "vht": 10,              //模型高度
        "latRadius": 10,        //维度方向长度
        "lngRadius": 10,        //经度方0长度
        "rotate": 0             //角度
    };
    this.dragedHandle = null;
    this.addElem = function (lat, lng) {
        var elem = new Elem(ge, lat, lng);
        elem.setElemPolygonOpt(this.defaultElemPolygonOpt);
        this.elemList.push(elem);
        elem.Index = this.elemList.length - 1;
        this.currentElem = elem;
        return elem;
    }
    var isPlaceMarkDragging = false, _glPlaceMark = null;
    google.earth.addEventListener(ge.getWindow(), 'mousedown', function (event) {
        isPlaceMarkDragging = false;
        if (event.getTarget().getType() == 'KmlPlacemark' &&
            event.getTarget().getGeometry().getType() == 'KmlPoint') {
            _glPlaceMark = event.getTarget();
            isPlaceMarkDragging = true;
        }
    });
    google.earth.addEventListener(ge.getGlobe(), 'mousemove', function (event) {
        if (_glPlaceMark && isPlaceMarkDragging) {
            event.preventDefault();
            var point = _glPlaceMark.getGeometry();

            point.setLatitude(event.getLatitude());
            point.setLongitude(event.getLongitude());
        }
    });
    google.earth.addEventListener(ge.getWindow(), 'mouseup', function (event) {
        var tag = _glPlaceMark;
        if (tag && isPlaceMarkDragging) {
            var pm = _getPlaceMarkInstance(tag);
            if (pm && pm.isPlaceMark) {
                if (isPlaceMarkDragging) {
                    isPlaceMarkDragging = false;
                    // if the placemark was dragged, prevent balloons from popping up
                    event.preventDefault();
                }
                placeLat = event.getLatitude();
                placeLng = event.getLongitude();
                try {
                    var pmark = pm.parentElem;
                    pmark.createCirclePolygon(pmark.getLat(), pmark.getLng(), pmark.getLatRadius(), pmark.getLngRadius(), pmark.getRotate(), pmark.getHeight(), pmark.getStep());
                } catch (e) {
                    alert(e);
                }
                locater.currentElem = pm.parentElem;
                if (locater.dragedHandle != null) {
                    locater.dragedHandle(tag.parentElem);
                }
            }
        }
    });

    
}

function _getPlaceMarkInstance(placemark) {
    var i = 0, pm;
    for (; pm = _gdict[i]; ++i) {
        if (pm.placemark == placemark) {
            return pm;
        }
    }
    return null;
}

function PlaceMarkQue(placemark, host) {
    this.placemark = placemark;
    this.parentElem = host;
    this.isPlaceMark = true;
}

var defaultElemName = "默认名称", _gdict = [];
 function Elem(ge, lat, lng) {

    this.placemark = ge.createPlacemark('');   
    
    var thisObj = this;

    var ge = ge;
    var placeLat = lat;
    var placeLng = lng;
    
    var latRadius = 10;
    var lngRadius = 10;
    var rotate = 0;
    var height = 10;
    var step = 4;

    this.name = defaultElemName;
    this.placemark.setName(this.name);

    this.elementId = null;

    var isFill = true;
    if (this.placemark.getStyleSelector() == null) {
        this.placemark.setStyleSelector(ge.createStyle(''));
    }
    this.placemark.getStyleSelector().getLabelStyle().setScale(1.5);

    _gdict.push(new PlaceMarkQue(this.placemark, this));
    //this.placemark.parentElem = this;
    //this.placemark.isPlaceMark = true;
    
    this._color = "9900ffff";

    var point = ge.createPoint('');
    point.setLatitude(lat);
    point.setLongitude(lng);
    this.placemark.setGeometry(point);

    ge.getFeatures().appendChild(this.placemark);

    this.reflushPolygon = function () {
        this.createCirclePolygon(this.placemark.getGeometry().getLatitude(), this.placemark.getGeometry().getLongitude(), latRadius, lngRadius, rotate, height, step);
    }
    this.polygonPlacemark = null;
    this.createCirclePolygon = function (centerLat, centerLng, latRadius, lngRadius, rotate, height, step) {
        console.log("输出数据:" + centerLat.toString() + "|" + centerLng.toString() + "|" + latRadius.toString() + "|" + lngRadius.toString() + "|" + rotate.toString() + "|" + height.toString() + "|" + step.toString());
        try {
            if (!rotate) rotate = 0;
            if (!height) height = 10;
            rotate = rotate % 360;
            function makeOval(centerLat, centerLng, latRadius, lngRadius) {
                var ring = ge.createLineString('');
                var steps = step;
                var pi2 = Math.PI * 2;
                rotate = rotate / 360 * pi2;
                ring.setExtrude(true);
                var rotateOffset = pi2 / 8;
                ring.setAltitudeMode(ge.ALTITUDE_RELATIVE_TO_GROUND);
                for (var i = 0; i < steps; i++) {
                    var lat = centerLat + latRadius * Math.cos(i / steps * pi2 + rotateOffset);
                    var lng = centerLng + lngRadius * Math.sin(i / steps * pi2 + rotateOffset);
                    var latTmp = lat - centerLat;
                    var lngTmp = lng - centerLng;
                    lat = latTmp * Math.cos(rotate) - lngTmp * Math.sin(rotate) + centerLat;
                    lng = latTmp * Math.sin(rotate) + lngTmp * Math.cos(rotate) + centerLng;
                    ring.getCoordinates().pushLatLngAlt(lat, lng, Math.abs(height));
                }

                var lat = centerLat + latRadius * Math.cos(0 / steps * pi2 + rotateOffset);
                var lng = centerLng + lngRadius * Math.sin(0 / steps * pi2 + rotateOffset);
                var latTmp = lat - centerLat;
                var lngTmp = lng - centerLng;
                lat = latTmp * Math.cos(rotate) - lngTmp * Math.sin(rotate) + centerLat;
                lng = latTmp * Math.sin(rotate) + lngTmp * Math.cos(rotate) + centerLng;

                ring.getCoordinates().pushLatLngAlt(lat, lng, Math.abs(height));

                return ring;
            }
            if (this.polygonPlacemark != null) { ge.getFeatures().removeChild(this.polygonPlacemark); }

            var geometry = makeOval(centerLat, centerLng, latRadius / 10000, lngRadius / 10000);

            //设置样式
            this.polygonPlacemark = ge.createPlacemark('');
            this.polygonPlacemark.setGeometry(geometry);

            this.polygonPlacemark.setStyleSelector(ge.createStyle(''));
            try {
                var polygonStyle = this.polygonPlacemark.getStyleSelector().getPolyStyle();
                polygonStyle.setFill(true);
            } catch (e) {
                alert(e);
            }

            var lineStyle = this.polygonPlacemark.getStyleSelector().getLineStyle();
            lineStyle.setWidth(5);
            lineStyle.getColor().set(this._color);  // aabbggrr format

            this.polygonPlacemark.setName(this.name);

            ge.getFeatures().appendChild(this.polygonPlacemark);
        } catch (e) {
            console.error("创建多边形区域时:" + e);
        }
    }
    this.OutputJSON = function () {
        alert("未完成！"); return;  //完成后删去
        var ans = new Object();
        
    }

    this.setLat = function (lat) { this.placemark.getGeometry().setLatitude(lat); this.reflushPolygon(); }
    this.setLng = function (lng) { this.placemark.getGeometry().setLongitude(lng); this.reflushPolygon(); }
    this.getLat = function () { return this.placemark.getGeometry().getLatitude(); }
    this.getLng = function () { return this.placemark.getGeometry().getLongitude(); }

    this.setLatRadius = function (_latRadius) { latRadius = _latRadius; this.reflushPolygon(); }
    this.setLngRadius = function (_lngRadius) { lngRadius = _lngRadius; this.reflushPolygon(); }
    this.setRotate = function (_rotate) { rotate = _rotate; this.reflushPolygon(); }
    this.setStep = function (_step) { step = _step; this.reflushPolygon(); }
    this.setHeight = function (_height) { height = _height; this.reflushPolygon(); }
    this.setColor = function (_color) {
        try {
            this._color = _color;
            this.polygonPlacemark.getStyleSelector().getLineStyle().getColor().set(_color);
        } catch (e) { alert("err:" + e); }
    }
    this.setFill = function (_fill) { this.polygonPlacemark.getStyleSelector().getPolyStyle().setFill(_fill); return this.polygonPlacemark.getStyleSelector().getPolyStyle().getFill(); }

    this.getLatRadius = function () { return latRadius; }
    this.getLngRadius = function () { return lngRadius; }
    this.getRotate = function () { return rotate; }
    this.getStep = function () { return step; }
    this.getHeight = function () { return height; }
    this.getColor = function () { return this.polygonPlacemark.getStyleSelector().getLineStyle().getColor().get(); }
    this.getFill = function () { return this.polygonPlacemark.getStyleSelector().getPolyStyle().getFill(); }

    this.setName = function (_name) { this.name = _name; this.placemark.setName(this.name); }

    this.setElemPolygonOpt = function (Opt) { 
        step = Opt.vts;
        height = Opt.vht;
        latRadius = Opt.latRadius;
        lngRadius = Opt.lngRadius;
        rotate = Opt.rotate;
        this.createCirclePolygon(placeLat, placeLng, latRadius, lngRadius, rotate, height, step);
    }
}