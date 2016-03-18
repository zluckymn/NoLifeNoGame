var QZFL = {},
QZFF_M_img_ribr = [];
QZFL.event = {
    getEvent: function (evt) {
        var evt = window.event || evt,
        c, cnt;
        if (!evt && window.Event) {
            c = arguments.callee;
            cnt = 0;
            while (c) {
                if ((evt = c.arguments[0]) && typeof (evt.srcElement) != "undefined") {
                    break;
                } else if (cnt > 9) {
                    break;
                }
                c = c.caller; ++cnt;
            }
        }
        return evt;
    },
    getTarget: function (evt) {
        var e = QZFL.event.getEvent(evt);
        if (e) {
            return e.srcElement || e.target;
        }
        return null;
    }
};
QZFL.object = {
    getType: function (o) {
        return o === null ? 'null' : (o === undefined ? 'undefined' : Object.prototype.toString.call(o).slice(8, -1).toLowerCase());
    }
};
QZFL.media = {
    reduceImgByRule: function (ew, eh, opts, cb) {
        QZFF_M_img_ribr.push(QZFL.event.getTarget());
    },
    adjustImageSize: function (w, h, s, cb, ecb) {
        var op = {
            trueSrc: s,
            callback: function (o, type, ew, eh, p) {
                (QZFL.object.getType(cb) == "function") && cb(o, ew, eh, null, p.ow, p.oh, p);
            },
            errCallback: ecb
        };
        QZFL.media.reduceImage(0, w, h, op);
    },
    reduceImage: function (type, ew, eh, opts) {
        var rd = function (o, t, ew, eh, p, cb) {
            var rl, k;
            if (p.rate == 1) {
                p.direction[0] = (ew > eh ? 'height' : 'width');
                p.direction[1] = (ew > eh ? 'width' : 'height');
            }
            rl = (p.direction[t] == "width" ? ew : eh);
            t ? (((rl > p.shortSize) ? (rl = p.shortSize) : 1) && (p.k = p.shortSize / rl)) : (((rl > p.longSize) ? (rl = p.longSize) : 1) && (p.k = p.longSize / rl));
            o.setAttribute(p.direction[t], rl); (QZFL.object.getType(cb) == "function") && cb(o, t, ew, eh, p);
        };
        opts = opts || {};
        opts.img = (opts.img && (typeof (opts.img.nodeName) != 'undefined' || typeof (opts.img.nodeType) != 'undefined') ? opts.img : QZFL.event.getTarget());
        opts.img.onload = null;
        opts.trueSrc && (opts.img.src = opts.trueSrc);
        if (opts.img) {
            if (!(opts.direction && opts.rate && opts.longSize && opts.shortSize)) {
                r = QZFL.media.getImageInfo(function (o, p) {
                    if (!o || !p) {
                        return;
                    }
                    rd(opts.img, type, ew, eh, p, opts.callback)
                },
                opts);
            } else {
                rd(opts.img, type, ew, eh, opts, opts.callback)
            }
        }
    },
    getImageInfo: function (callback, opts) {
        var gif = function (img, cb, opts) {
            if (img) {
                var _w = opts.ow || img.width,
                _h = opts.oh || img.height,
                r, ls, ss, d;
                if (_w && _h) {
                    if (_w >= _h) {
                        ls = _w;
                        ss = _h;
                        d = ["width", "height"];
                    } else {
                        ls = _h;
                        ss = _w;
                        d = ["height", "width"];
                    }
                    r = {
                        direction: d,
                        rate: ls / ss,
                        longSize: ls,
                        shortSize: ss
                    };
                    r.ow = _w;
                    r.oh = _h;
                } (QZFL.object.getType(cb) == "function") && cb(img, r, opts);
            }
        };
        opts = opts || {};
        if (QZFL.object.getType(opts.trueSrc) == "string") {
            var _i = new Image();
            _i.onload = (function (ele, cb, p) {
                return function () {
                    gif(ele, cb, p);
                    ele = ele.onerror = ele.onload = null;
                };
            })(_i, callback, opts);
            _i.onerror = (function (ele, cb, p) {
                return function () {
                    if (typeof (p.errCallback) == 'function') {
                        p.errCallback();
                    }
                    ele = ele.onerror = ele.onload = null;
                };
            })(_i, callback, opts);
            _i.src = opts.trueSrc;
        } else if (opts.img && opts.img.nodeType == 1) {
            gif(opts.img, callback, opts);
        }
    }
};

//快速调整图片大小
function QuickAdjustImageSize(full_background, full_background_img) {

    var bg = $(full_background),
        bg_img = $(full_background_img),
        cw = bg.style.width,
        ch = bg.style.height,
        iw = bg_img.width,
        ih = bg_img.height;
    
    if (cw / ch > iw / ih) {
        var new_h = cw * ih / iw,
            imgTop = (ch - new_h) / 2;
        bg_img.style.width = cw + "px";
        bg_img.style.height = new_h + "px";
        bg_img.style.top = "";
        bg_img.style.left = "";
    } else {
        var new_w = ch * iw / ih,
            imgLeft = (cw - new_w) / 2;
        bg_img.style.width = new_w + "px";
        bg_img.style.height = ch + "px";
        bg_img.style.left = imgLeft + "px";
        bg_img.style.top = "";
    }
}