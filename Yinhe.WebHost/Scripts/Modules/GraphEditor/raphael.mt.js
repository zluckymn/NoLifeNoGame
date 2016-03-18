(function(R) {

Array.prototype._swap1 = function(i, j){
	var temp = this[i];
	this[i] = this[j];
	this[j] = temp;
}
Array.prototype._quickSort1 = function(s, e){
	if (s == null) s = 0;
	if (e == null) e = this.length - 1;
	if (s >= e) return;
	this._swap1((s + e) >> 1, e);
	var index = s - 1, i;
	for (i = s; i <= e; ++i) {
		if (this[i] <= this[e]) this._swap1(i, ++index);
	}
	this._quickSort1(s, index - 1);
	this._quickSort1(index + 1, e);
}

R.loadXML = R.fn.loadXML = function(xml, flag) {
    var xmlDoc, f = flag || 0; // flag: load xml text default
    if (window.ActiveXObject) {
        var xmlV = ["MSXML2.DOMDocument.6.0", "MSXML2.DOMDocument.5.0", "MSXML2.DOMDocument.4.0", "MSXML2.DOMDocument.3.0", "MSXML2.DOMDocument", "Microsoft.XMLDOM"];
        for (var i = 0; i < xmlV.length; i++) {
            try {
                xmlDoc = new ActiveXObject(xmlV[i]);
                break;
            } catch (e) { }
        }
        if (xmlDoc.parseError.reason) return null;
        xmlDoc.async = false;
        f == 0 ? xmlDoc.loadXML(xml) : xmlDoc.load(xml);
    } else {
        if (document.implementation && document.implementation.createDocument) {
            xmlDoc = document.implementation.createDocument("", "", null);
            if (!f) {
                var parser = new DOMParser();
                xmlDoc = parser.parseFromString(xml, "text/xml");
            } else {
                xmlDoc.async = false;
                xmlDoc.load(xml);
            }
            if (xmlDoc.documentElement.nodeName == 'parsererror') return null;
        }
    }
    return xmlDoc;
};

var createShape = (function() {
    var tonum = MT.number;

    function getArr(x1, y1, x2, y2, size, ang) {
        var angle = ang != null && ang >= 0 ? ang : Raphael.angle(x1, y1, x2, y2),
            a45 = Raphael.rad(angle - 45),
            a45m = Raphael.rad(angle + 45);
        var x2a = x2 + Math.cos(a45) * size,
            y2a = y2 + Math.sin(a45) * size,
            x2b = x2 + Math.cos(a45m) * size,
            y2b = y2 + Math.sin(a45m) * size,
            result = ["M", x2a, y2a, "L", x2, y2, "L", x2b, y2b];
        return result;
    }

    function __createPath(vml, path, x, y, w, h, r) {
        var res = vml.path(path), a = res.attrs;
        res.X = a.x = x;
        res.Y = a.y = y;
        res.W = a.width = w;
        res.H = a.height = h;
        if (r) {
            res.R = a.r = r;
        }
        res.path = a.path = path;
        res.type = "path";
        return res;
    }
    function __theRhombus(vml, x, y, w, h) { // 棱形
        var hfw = parseFloat(tonum.toFixed(w/2*1, 1), 10),
            hfh = parseFloat(tonum.toFixed(h/2*1, 1), 10),
            path = ["M", x, y+hfh, "l", hfw, -hfh, hfw, hfh, -hfw, hfh, "z"];
        return __createPath(vml, path, x, y, w, h);
    }

    function __theRects(vml, x, y, w, h) { // 矩形内嵌矩形
        var path = ["M", x, y, "l", w, 0, 0, h, -w, 0, "z m", 10, 0, "l0", h, "m", w-20, 0, "l0", -h];
        return __createPath(vml, path, x, y, w, h);
    }

    function __theHexagon(vml, x, y, w, h, r) { // 六边形
        var path = ["M", x+r, y, "l", w-2*r, 0, r, Math.floor(h/2), -r, Math.floor(h/2), 2*r-w, 0, -r, Math.floor(-h/2), "z"];
        return __createPath(vml, path, x, y, w, h, r);
    }

    function __theTriangle(vml, x, y, w, h) {
        var hfw = parseFloat(tonum.toFixed(w/2*1, 1), 10),
            path = ["M", x, y+h, "l", hfw, -h, hfw, h, "z"];
        return __createPath(vml, path, x, y, w, h);
    }
    return {
        Hexagon: function(vml, x, y, w, h, r) {
            return __theHexagon(vml, x || 0, y || 0, w || 0, h || 0, r || 0);
        },
        Rhombus: function(vml, x, y, w, h) {
            return __theRhombus(vml, x || 0, y || 0, w || 0, h || 0);
        },
        Rects: function(vml, x, y, w, h) {
            return __theRects(vml, x || 0, y || 0, w || 0, h || 0);
        },
        Triangle: function(vml, x, y, w, h) {
            return __theTriangle(vml, x || 0, y || 0, w || 0, h || 0);
        },
        Arrow: function(o, from, to, type, fill, sw, ang) {
            var path = getArr(from.x, from.y, to.x, to.y, 8, ang), arr;
            if (type == 2) {
                path.push('Z');
            }
            arr = o.path(path).attr({"stroke": fill});
            if (type == 2) arr.attr({"fill": fill});
            if (sw) arr.attr({"stroke-width": sw});
            return arr;
        }
    };
})();

R.fn.hexagon = function(x, y, w, h, r) { // 六边形
    return createShape.Hexagon(this, x, y, w, h, r);
};
R.fn.rhombus = function(x, y, w, h) { // 棱形
    return createShape.Rhombus(this, x, y, w, h);
};
R.fn.rects = function(x, y, w, h) { // 矩形内嵌矩形
    return createShape.Rects(this, x, y, w, h);
};
R.fn.triangle = function(x, y, angle, rotate, h) { // 三角形
    return createShape.Triangle(this, x, y, angle, rotate, h);
};
R.fn.drawArr = function (from, to, type, fill, sw, ang) {
    return createShape.Arrow(this, from, to, type, fill, sw, ang);
};

var cedge = (function() {
    var _a = [];
    function _eeww(t, fbox, tbox, fc, tc) {
        var cd = t === 'ee',
            cd1 = cd ? (fbox.right < tbox.left) : (tbox.right < fbox.left),
            cd2 = cd ? (fbox.left > tbox.right) : (tbox.left > fbox.right),
            z = cd ? 1 : -1, tm, m;
        if (cd1 && tbox.bottom >= fc.y && tbox.top < fc.y) {
            tm = cd ? (tbox.left-fbox.right) : (fbox.left-tbox.right);
            tm = fc.x+tm/2*z; m = cd ? (tbox.top-24) : (tbox.bottom+24);
            _a = ['L', tm, fc.y, tm, m, tc.x+24*z, m, tc.x+24*z, tc.y];
        } else if (tc.y > fbox.top && tc.y < fbox.bottom && cd2) {
            tm = cd ? (fbox.left-tbox.right) : (tbox.left-fbox.right);
            tm = tc.x+tm/2*z; m = cd ? (fbox.bottom+24) : (fbox.top-24);
            _a = ['L', fc.x+24*z, fc.y, fc.x+24*z, m, tm, m, tm, tc.y];
        } else {
            tm = cd ? Math.max(fbox.right, tbox.right) : Math.min(fbox.left, tbox.left);
            _a = ['L', tm+24*z, fc.y, tm+24*z, tc.y];
        }
        return _a;
    }
    function _nnss(t, fbox, tbox, fc, tc) {
        var cd = t === 'nn',
            cd1 = cd ? (tbox.bottom < fbox.top) : (tbox.top > fbox.bottom),
            cd2 = cd ? (tbox.top > fbox.bottom) : (tbox.bottom < fbox.top),
            z = cd ? -1 : 1, tm, m;
        if (cd1 && tbox.right >= fc.x && tbox.left < fc.x) {
            tm = cd ? (fbox.top-tbox.bottom) : (tbox.top-fbox.bottom);
            tm = fc.y+tm/2*z; m = cd ? (tbox.left-24) : (tbox.right+24);
            _a = ['L', fc.x, tm, m, tm, m, tc.y+24*z, tc.x, tc.y+24*z];
        } else if (tc.x > fbox.left && tc.x < fbox.right && cd2) {
            tm = cd ? (tbox.top-fbox.bottom) : (fbox.top-tbox.bottom);
            tm = tc.y+tm/2*z; m = cd ? (fbox.right+24) : (fbox.left-24);
            _a = ['L', fc.x, fc.y+24*z, m, fc.y+24*z, m, tm, tc.x, tm];
        } else {
            tm = cd ? Math.min(fbox.top, tbox.top) : Math.max(fbox.bottom, tbox.bottom);
            _a = ['L', fc.x, tm+24*z, tc.x, tm+24*z];
        }
        return _a;
    }
    function _ns2w(t, fbox, tbox, fc, tc) {
        var cd = t === 'n',
            cd1 = cd ? (fc.y >= tc.y) : (fc.y <= tc.y),
            cd2 = cd ? (tc.y > fc.y) : (tc.y < fc.y),
            cd3 = cd ? (tbox.bottom < fbox.top) : (tbox.top > fbox.bottom),
            z = cd ? 1 : -1, tm, m;
        if (fc.x <= tc.x && cd1) {
            _a = ['L', fc.x, tc.y];
        } else if (cd2 && fbox.right <= tbox.left) {
            tm = (tbox.left-fbox.right)/2;
            _a = ['L', fc.x, fc.y-24*z, tc.x-tm, fc.y-24*z, tc.x-tm, tc.y];
        } else if (tbox.left < fc.x && cd3) {
            tm = cd ? (fbox.top-tbox.bottom) : (tbox.top-fbox.bottom); tm = tm/2;
            m = Math.min(fbox.left, tbox.left);
            _a = ['L', fc.x, fc.y-tm*z, m-24, fc.y-tm*z, m-24, tc.y];
        } else {
            tm = Math.min(fbox.left, tbox.left);
            _a = ['L', fc.x, fc.y-24*z, tm-24, fc.y-24*z, tm-24, tc.y];
        }
        return _a;
    }
    function _ns2e(t, fbox, tbox, fc, tc) {
        var cd = t === 'n',
            cd1 = cd ? (fc.y >= tc.y) : (fc.y <= tc.y),
            cd3 = cd ? (tbox.bottom < fbox.top) : (tbox.top > fbox.bottom),
            z = cd ? 1 : -1, tm, m;
        if (fc.x >= tc.x && cd1) {
            _a = ['L', fc.x, tc.y];
        } else if (tc.y < fc.y && tbox.right <= fbox.left) {
            tm = (fbox.left-tbox.right)/2;
            _a = ['L', fc.x, fc.y-24*z, tc.x+tm, fc.y-24*z, tc.x+tm, tc.y];
        } else if (tc.y > fc.y && tbox.right <= fbox.left) {
            tm = (fbox.left-tbox.right)/2;
            _a = ['L', fc.x, fc.y-24*z, tc.x+tm, fc.y-24*z, tc.x+tm, tc.y];
        } else if (tbox.right > fc.x && cd3) {
            tm = cd ? (fbox.top-tbox.bottom) : (tbox.top-fbox.bottom);
            tm = tm /2; m = cd ? tc.x : Math.max(fbox.right, tbox.right);
            _a = ['L', fc.x, fc.y-tm*z, m+24, fc.y-tm*z, m+24, tc.y];
        } else {
            tm = Math.max(fbox.right, tbox.right);
            _a = ['L', fc.x, fc.y-24*z, tm+24, fc.y-24*z, tm+24, tc.y];
        }
        return _a;
    }
    function _s2n(fbox, tbox, fc, tc) {
        if (tc.y > fc.y) { // 终点在起点的下方
            if (tc.x === fc.x) {
                _a = ['L'];
            } else {
                var c1 = (tc.y - fc.y)/2;
                _a = ['L', fc.x, fc.y+c1, tc.x, tc.y-c1];
            }
        } else if (tc.y <= fc.y) { // 在上方
            if (tbox.right < fbox.left) {
                var c1 = (fbox.left-tbox.right)/2;
                _a = ['L', fc.x, fc.y+24, c1+tbox.right, fc.y+24, c1+tbox.right, tc.y-24, tc.x, tc.y-24];
            } else if (tbox.left > fbox.right) {
                var c1 = (tbox.left-fbox.right)/2;
                _a = ['L', fc.x, fc.y+24, c1+fbox.right, fc.y+24, c1+fbox.right, tc.y-24, tc.x, tc.y-24];
            } else {
                if (tc.x <= fc.x) {
                    var c1 = Math.min(tbox.left, fbox.left);
                    _a = ['L', fc.x, fc.y+24, c1-24, fc.y+24, c1-24, tc.y-24, tc.x, tc.y-24];
                } else {
                    var c1 = Math.max(tbox.right, fbox.right);
                    _a = ['L', fc.x, fc.y+24, c1+24, fc.y+24, c1+24, tc.y-24, tc.x, tc.y-24];
                }
            }
        }
        return _a;
    }
    function _e2w(fbox, tbox, fc, tc) {
       if (tbox.left >= fbox.right) {
            if (tc.y === fc.y) {
                _a = ['L', fc.x, fc.y];
            } else {
                var c1 = (tbox.left-fbox.right)/2;
                _a = ['L', fc.x+c1, fc.y, fc.x+c1, tc.y];
            }
        } else if (tbox.top < fbox.bottom && fc.y <= tc.y) {
            var c1 = Math.max(fbox.bottom, tbox.bottom);
            _a = ['L', fc.x+24, fc.y, fc.x+24, c1+24, tc.x-24, c1+24, tc.x-24, tc.y];
        } else if (fc.y > tc.y && fbox.top < tbox.bottom) {
            var c1 = Math.min(fbox.top, tbox.top);
            _a = ['L', fc.x+24, fc.y, fc.x+24, c1-24, tc.x-24, c1-24, tc.x-24, tc.y];
        } else if (tbox.top >= fbox.bottom) {
            var c1 = (tbox.top - fbox.bottom)/2;
            _a = ['L', fc.x+24, fc.y, fc.x+24, fbox.bottom+c1, tc.x-24, fbox.bottom+c1, tc.x-24, tc.y];
        } else {
            var c1 = (fbox.top - tbox.bottom)/2;
            _a = ['L', fc.x+24, fc.y, fc.x+24, tbox.bottom+c1, tc.x-24, tbox.bottom+c1, tc.x-24, tc.y];
        }
        return _a;
    }
    return {
        ee: function(fbox, tbox, fc, tc) {
            return {'a':_eeww('ee', fbox, tbox, fc, tc), 'r':false};
        },
        ww: function(fbox, tbox, fc, tc) {
            return {'a':_eeww('ww', fbox, tbox, fc, tc), 'r':false};
        },
        nn: function(fbox, tbox, fc, tc) {
            return {'a':_nnss('nn', fbox, tbox, fc, tc), 'r':false};
        },
        ss: function(fbox, tbox, fc, tc) {
            return {'a':_nnss('ss', fbox, tbox, fc, tc), 'r':false};
        },
        nw: function(fbox, tbox, fc, tc) {
            return {'a':_ns2w('n', fbox, tbox, fc, tc), 'r':false};
        },
        wn: function(fbox, tbox, fc, tc) {
            return {'a':_ns2w('n', tbox, fbox, tc, fc), 'r':true};
        },
        sw: function(fbox, tbox, fc, tc) {
            return {'a':_ns2w('s', fbox, tbox, fc, tc), 'r':false};
        },
        ws: function(fbox, tbox, fc, tc) {
            return {'a':_ns2w('s', tbox, fbox, tc, fc), 'r':true};
        },
        ne: function(fbox, tbox, fc, tc) {
            return {'a':_ns2e('n', fbox, tbox, fc, tc), 'r':false};
        },
        en: function(fbox, tbox, fc, tc) {
            return {'a':_ns2e('n', tbox, fbox, tc, fc), 'r':true};
        },
        se: function(fbox, tbox, fc, tc) {
            return {'a':_ns2e('s', fbox, tbox, fc, tc), 'r':false};
        },
        es: function(fbox, tbox, fc, tc) {
            return {'a':_ns2e('s', tbox, fbox, tc, fc), 'r':true};
        },
        sn: function(fbox, tbox, fc, tc) {
            return {'a':_s2n(fbox, tbox, fc, tc), 'r':false};
        },
        ns: function(fbox, tbox, fc, tc) {
            return {'a':_s2n(tbox, fbox, tc, fc), 'r':true};
        },
        ew: function(fbox, tbox, fc, tc) {
            return {'a':_e2w(fbox, tbox, fc, tc), 'r':false};
        },
        we: function(fbox, tbox, fc, tc) {
            return {'a':_e2w(tbox, fbox, tc, fc), 'r':true};
        }
    }
})();

function pInt(v) {
    return parseInt(v, 10);
}

function pFloat(v) {
    return parseFloat(v, 10);
}

var doLine = (function() {
    var cache = {}, opt,
        host;

    function getJunctionXY(direct, bb) {
        var x = 0, y = 0;
        switch (direct) {
            case 'n':
                x = bb.x + bb.width/2; y = bb.y;
                break;
            case 's':
                x = bb.x + bb.width/2; y = bb.y + bb.height;
                break;
            case 'e':
                x = bb.x + bb.width; y = bb.y + bb.height/2;
                break;
            case 'w':
                x = bb.x; y = bb.y + bb.height/2;
                break;
        }
        return {x:x, y:y};
    }

    function __renderEdge(edge) { // render edge
        var _nid, from, to, fnd, tnd, f = null, t = null,
            fbox, tbox, fc, tc, _o, points, _ph = edge.path,
            oedge, _a;
        from = edge.from;
        to = edge.to;
        fnd = edge.fdir;
        tnd = edge.tdir;
        fc = edge.fCoor;
        tc = edge.tCoor;

        if (from != 'x') f = host.nodes[from]['node']; // from node
        if (to != 'x') t = host.nodes[to]['node']; // to node
        if (from != 'x') {
            _o = f.getBBox();
            fbox = {top:_o.y, right:_o.x+_o.width, bottom:_o.y+_o.height, left:_o.x, width:_o.width, height:_o.height}; // bound
        } else {
            fbox = {top:fc.y, right:fc.x+1, bottom:fc.y+1, left:fc.x, width:1, height:1};
        }
        if (to != 'x') {
            _o = t.getBBox();
            tbox = {top:_o.y, right:_o.x+_o.width, bottom:_o.y+_o.height, left:_o.x, width:_o.width, height:_o.height};
        } else {
            tbox = {top:tc.y, right:tc.x+1, bottom:tc.y+1, left:tc.x, width:1, height:1};
        }

        _a = cedge[fnd+tnd](fbox, tbox, fc, tc);
        if (_a.r) {
            points = ['M', tc.x, tc.y].concat(_a.a, [fc.x, fc.y]);
        } else {
            points = ['M', fc.x, fc.y].concat(_a.a, [tc.x, tc.y]);
        }

        //oedge = paper.path(points);
        return points;
    }

    return {
        init: function(cfg) {
            opt = cfg || {};
            host = opt.host;
            if (!host) {
                alert('error:未初始化canvas'); return false;
            }
            return true;
        },
        renderEdge: function(paper, edge) {
            return __renderEdge(paper, edge);
        },
        getJunctionXY: function(dir, bb) {
            return getJunctionXY(dir, bb);
        }
    };
})();

R.fn.doLine = doLine;

R.fn.shapeArgs = function(host, node) {
    var id,
        drawType, pid,
        tmpNode, attrs, style = {}, geo = {}, args, rotate = null, hLink;

    id = node.getAttribute('id');
    drawType = node.getAttribute('type');
    hLink = node.getAttribute('url');
    tmpNode = node.getElementsByTagName('rCell');
    attrs = tmpNode[0].attributes || [];
    MT.each(attrs, function(v, i) {
        if (!MT.isEmpty(v.value)) style[v.name] = v.value;
    });
    tmpNode = node.getElementsByTagName('rGeometry');
    attrs = tmpNode[0].attributes || [];
    MT.each(attrs, function(v, i) {
        geo[v.name] = pFloat(v.value);
    });

    args = this.shapeType(drawType, geo);
    if (geo.rotate) rotate = geo.rotate;
    return [
        id, drawType, args, style, rotate, hLink
    ];
};

R.fn.shapeType = function(drawType, geo) {
    var args = [];
    switch (drawType) {
        case "circle":
            args = [geo.x+geo.width/2, geo.y+geo.height/2, geo.width/2];
            break;
        case "rect":
            args = [geo.x, geo.y, geo.width, geo.height];
            break;
        case "ellipse":
            args = [geo.x, geo.y, geo.width/2, geo.height/2];
            break;
        case "rectc":
            args = [geo.x, geo.y, geo.width, geo.height, geo.width/2];
            break;
        case "rhombus":
            args = [geo.x, geo.y, geo.width, geo.height];
            break;
        case "triangle":
            args = [geo.x, geo.y, geo.width, geo.height];
            break;
        case "rects":
            args = [geo.x, geo.y, geo.width, geo.height];
            break;
        case "hexagon":
            args = [geo.x, geo.y, geo.width, geo.height, 8];
            break;
    }
    return args;
};

R.fn.lineArgs = function(host, line) {
    var id,
        drawType, pid,
        tmpNode, attrs, style = {}, geo = {}, args, ft = {};

    id = line.getAttribute('id');
    drawType = line.getAttribute('type');
    tmpNode = line.getElementsByTagName('rCell');
    attrs = tmpNode[0].attributes || [];
    MT.each(attrs, function(v, i) {
        if (!MT.isEmpty(v.value)) style[v.name] = v.value;
    });
    tmpNode = line.getElementsByTagName('rGeometry');
    attrs = tmpNode[0].attributes || [];
    MT.each(attrs, function(v, i) {
        geo[v.name] = pFloat(v.value);
    });

    tmpNode = line.getElementsByTagName('rFt');
    attrs = tmpNode[0].attributes || [];
    MT.each(attrs, function(v, i) {
        ft[v.name] = v.value;
    });

    return [
        id, {from: ft.from, to: ft.to}, {x: pFloat(geo.x1), y: pFloat(geo.y1)}, {x: pFloat(geo.x2), y: pFloat(geo.y2)}, {from: ft.fdir, to: ft.tdir}, pInt(ft.type), ft.arrPos, ft.arrType, style
    ];
};

R.fn.textArgs = function(host, txt) {
    var id,
        drawType, hLink,
        tmpNode, txtData, geo = {}, args, ft = {}, rotate = null;

    id = txt.getAttribute('id');
    drawType = txt.getAttribute('type');
    hLink = txt.getAttribute('url');
    tmpNode = txt.getElementsByTagName('rData');
    txtData = MT.JSON.parse(tmpNode[0].childNodes[0].nodeValue);
    if (txtData === null) {
        txtData = {"txts": []};
    }

    tmpNode = txt.getElementsByTagName('rGeometry');
    attrs = tmpNode[0].attributes || [];
    MT.each(attrs, function(v, i) {
        geo[v.name] = pFloat(v.value);
    });
    if (geo.rotate) rotate = geo.rotate;

    return [
        txtData, id, {x: geo.x, y: geo.y}, rotate, null, hLink,
    ];
};

})(Raphael);