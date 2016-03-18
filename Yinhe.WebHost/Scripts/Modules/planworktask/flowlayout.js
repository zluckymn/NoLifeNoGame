var gLayout = {
    Lod: {},
    level: 0,
    paper: null,
    calPos: 'lb', // could be 'lt', 'lc', 'lb'
    ctOffset: null, // container offset
    urlList: {},
    tbWrap: '<table class="tball {tbCls}">{trs}</table>',
    trWrap: '<tr><td>{td1}</td><td style="width:{nodeDis}px;"></td><td>{td3}</td></tr>',
    itemWrap: '<div id="nd_{id}" class="{cls} {lvl}"><a href="{href}"{target}>{txt}</a></div>',
    renderTpl: function (str, o) {
        return str.replace(/\{\w+\}/g, function (_) {
            return o[_.replace(/\{|\}/g, "")] || ""
        });
    },
    doLod: function (data, pid, lvl) {
        var me = this,
lodname = 'lvl' + lvl, gpname = 'group' + pid,
lodItem = me.Lod[lodname],
item, cycleArr = [], hasItems = false;

        if (me.level < lvl) {
            me.level = lvl;
        }
        if (typeof lodItem == 'undefined') {
            lodItem = me.Lod[lodname] = {};
        }
        if (typeof lodItem[gpname] == 'undefined') {
            lodItem[gpname] = [];
        }

        while (data.length) {
            item = data.shift();
            if (item.pid == pid) {
                lodItem[gpname].push(item);
                hasItems = true;
                me.doLod(data.concat(cycleArr), item.nodeId, lvl + 1);
            } else {
                cycleArr.push(item);
            }
        }
        if (!hasItems) { // delete empty array.
            lodItem[gpname] = undefined;
            delete lodItem[gpname];
        }
    },
    createTable: function (lvl, group) {
        var me = this, i, item,
lod = me.Lod, items, out = [],
lvlItm = lod['lvl' + lvl], td1, td3, tr, tb = "", url;

        items = lvlItm['group' + group];
        if (items && items.length > 0) {
            for (i = 0; item = items[i]; ++i) {
                item.id = item.nodeId;
                item.cls = lvl == 0 ? '' : 'ctbox';
                url = typeof item.type != 'undefined' ? me.urlList['t' + item.type] : '';
                url = url || '';
                item.target = url != '' ? ' target=' + me.target_ : '';
                if (url == '') url = 'javascript:;';
                item.href = me.renderTpl(url, item);
                item.lvl = 'lvl' + lvl;
                td1 = me.renderTpl(me.itemWrap, item);
                td3 = me.createTable(lvl + 1, item.nodeId);
                tr = me.renderTpl(me.trWrap, { "td1": td1, "td3": td3, "nodeDis": me.nodeDis });
                out.push(tr);
            }
            tb = me.renderTpl(me.tbWrap, { "tbCls": 'tblvl' + lvl, "trs": out.join('') });
            out = [];
        }
        return tb;
    },
    initPaper: function (container, w, h) {
        var r = Raphael(container, w, h);
        return r;
    },
    cCache: {},
    doLine: function (lvl) {
        if (lvl > this.level - 1) return false;
        var me = this, i, j, len,
lod = me.Lod, items, child, lvlItm = lod['lvl' + lvl],
nid, to, gid, groups, gids, grp;
        for (grp in lvlItm) {
            items = lvlItm[grp];
            if (items && (len = items.length)) {
                for (i = 0; i < len; ++i) {
                    gid = items[i].nodeId;
                    nid = document.getElementById('nd_' + gid);
                    from = me.cCache['c' + gid] || (me.cCache['c' + gid] = { pos: $(nid).offset(), w: nid.offsetWidth, h: nid.offsetHeight });

                    groups = lod['lvl' + (lvl + 1)];
                    if (groups && (child = groups['group' + gid])) {
                        for (k = 0; k < child.length; k++) {
                            gids = child[k].nodeId;
                            nid = document.getElementById('nd_' + gids);
                            to = me.cCache['c' + gids] || (me.cCache['c' + gids] = { pos: $(nid).offset(), w: nid.offsetWidth, h: nid.offsetHeight });
                            me.makeLineData(lvl, from, to);
                        }
                    }
                }
            }
        }
        me.doLine(lvl + 1);
    },
    makeLineData: function (lvl, from, to) {
        var me = this, cwof = me.ctOffset, sx, sy, ex, ey, calPos = me.calPos,
spos = from.pos, sw = from.w, sh = from.h,
epos = to.pos, ew = to.w, eh = to.h;
        sx = spos.left - cwof.left + sw;
        sy = spos.top - cwof.top + (lvl === 0 ? Math.round(sh / 2) : sh);

        ex = epos.left - cwof.left;
        ey = 0;
        if (calPos === 'lb') {
            ey = eh;
        } else if (calPos === 'lc') {
            ey = Math.round(eh / 2);
        }
        ey += epos.top - cwof.top;
        me.curve(sx, sy, sx + 40, sy, ex - 40, ey, ex, ey);
    },
    curve: function (x, y, ax, ay, bx, by, zx, zy, color) {
        var path = [["M", x, y - 1], ["C", ax, ay - 1, bx, by - 1, zx - 1, zy - 1]],
r = this.paper, discattr = { fill: "#999", stroke: "none" },
curve = r.path(path).attr({ stroke: color || Raphael.getColor(), "stroke-width": 2, "stroke-linecap": "round" });
        r.circle(zx, zy - 1, 3).attr(discattr);
    },
    doLayout: function (data, container, opt) {
        var me = this,
tb, ctw, cth, container = document.getElementById(container);
        opt = opt || {};
        me.nodeDis = opt.nodeDis || 25;
        me.urlList = opt.urlList || {};
        me.target_ = opt.target || '_blank';
        me.doLod(data, 0, 0);
        tb = me.createTable(0, 0);
        container.innerHTML = tb;
        tb = null;
        me.ctOffset = $(container).offset();
        tb = $(container).find('table:first');
        ctw = me.level * (130 + me.nodeDis); // tb.outerWidth();
        if (tb[0]) {
            tb[0].style.width = ctw + 'px';
        }
        cth = tb.outerHeight();
        container.style.width = ctw + 'px';
        container.style.height = cth + 'px';
        me.paper = me.initPaper(container, ctw, cth);

        me.doLine(0);
        me.cCache = {};
    }
};