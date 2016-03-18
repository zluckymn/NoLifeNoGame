(function (R) {
    function RenderGraph(cfg) {
        this.init(cfg);
    }

    RenderGraph.prototype = {
        dcache: {},
        nodes: {},
        ang: { 'n': 270, 's': 90, 'e': 0, 'w': 180 },
        init: function (cfg) {
            cfg = cfg || {};
            if (!cfg.paperCt) {
                throw new Error('无法获取画布容器，请检查html');
            }
            var me = this,
                paperct = MT.get(cfg.paperCt);
            me.paperct = paperct;
            me.callback = cfg.callback || MT.noop;
            me.container = MT.get(cfg.container);
            me.loadData(cfg.url);
        },
        initPaper: function (w, h) {
            var me = this,
                paperct = me.paperct;

            paperct.setSize(w, h);
            paperct.setStyle({ left: 0, top: 0 });
            me.paper = Raphael(paperct.tag, w, h);
        },
        setSubwayMapImg: function () { // 为画布添加图片背景。
            var me = this,
                oimg = me.dcache.bgImg;

            oimg && oimg.length && me.paperct.setStyle("background:url(" + oimg + ") no-repeat 0 0");
        },
        paperCfg: function (setting) {
            var me = this;
            MT.mixin(me.dcache, setting, true);
            me.initPaper(me.dcache.width, me.dcache.height);
            if (me.dcache.bgImg && me.dcache.bgImg != "") {
                me.setSubwayMapImg();
            }
        },
        renderShape: function (id, drawType, args, style, rotateDig, hLink) {
            var me = this,
                nowNode, paper = me.paper, dtp;
            dtp = drawType == 'rectc' ? 'rect' : drawType;
            if (MT.isFunction(paper[dtp])) {
                nowNode = paper[dtp].apply(paper, args);

                nowNode.attr(style || { fill: "#fff", stroke: "#000" });
                nowNode.id = id || MT.guid('shape_' + uuid);
                if (MT.isNumber(rotateDig)) nowNode.rotate(rotateDig);
                if (hLink && hLink != '') {
                    nowNode.attr({ href: hLink, target: "_blank" });
                }
                nowNode.isShape = true;
            }
            return nowNode;
        },
        renderLine: function (id, rels, fCoor, toCoor, dirs, type, arrPos, arrType, style) {
            var me = this, line, paper = me.paper, cfg;
            cfg = {
                from: rels.from, fCoor: fCoor, fdir: dirs ? dirs.from : null,
                to: rels.to, tCoor: toCoor, tdir: dirs ? dirs.to : null,
                path: null,
                type: type,
                arrPos: arrPos,
                arrType: arrType
            };
            if (type === 1) {
                line = paper.path(paper.doLine.renderEdge(cfg));
            } else {
                line = paper.path(['M', fCoor.x, fCoor.y, 'L', toCoor.x, toCoor.y]);
            }
            line.id = id;
            style && line.attr(style);
            if (arrPos && arrPos != "") {
                me.makeArrow(id, cfg, line, arrPos + '-' + arrType);
            }
            return line;
        },
        makeArrow: function (id, line, node, type) { // make line arrow
            type = type.split('-');
            var me = this,
                dir = type[0] || '', tp = type[1] || '',
                from, to,
                nfill = node.attr('stroke'), sw = node.attr('stroke-width') || 1,
                ang = null, ang1 = null;

            nfill = nfill == 'none' ? '#000' : nfill;

            if (type == "") return;
            switch (dir) {
                case '':
                    break;
                case 'f':
                    to = line.fCoor;
                    from = line.tCoor;
                    break;
                case 't':
                    from = line.fCoor;
                    to = line.tCoor;
                    break;
                case 'ft':
                    from = line.fCoor;
                    to = line.tCoor;
                    break;
            }

            if (line.type === 1) { // broken line
                ang = me.ang[line.fdir];
                ang1 = me.ang[dir == 'f' ? line.fdir : line.tdir];
            }
            me.paper.drawArr(from, to, tp, nfill, sw, ang1);
            if (dir == 'ft') me.paper.drawArr(to, from, tp, nfill, sw, ang);
        },
        doRenderText: function (txtData, id, xy, rotateDig, pid, hLink) {
            var me = this, st, text,
                bb = { x: 0, y: 0, width: 0, height: 0 },
                paper = me.paper;

            bb = MT.isObject(xy) ? xy : bb;
            st = st || paper.set();
            st.isText = true;
            st.id = id || st.id || MT.guid("st_" + uuid);
            me.tuneText(st, txtData.txts, bb);
            if (MT.isNumber(rotateDig)) nowNode.rotate(rotateDig);
            if (hLink && hLink != '') {
                st.attr({ href: hLink, target: "_blank" });
            }
            return st;
        },
        tuneText: function (st, texts, xy) {
            var me = this,
                fontSize = 12, setId = st.id,
                leading = 1.5,
                align = { 'left': 'start', 'center': 'middle', 'right': 'end' },
                obj, x = xy.x, y = xy.y, dy = 0,
                txt, styles, str, k, v, fill, txtAnchor, flag = false, sfsize,
                hw = xy.width / 2 || 0, hh = xy.height / 2 || 0, theText = "", dsize;

            for (var i = 0, ii = texts.length; i < ii; i++) {
                txt = texts[i];
                str = "";
                txtAnchor = "";
                fill = "";

                styles = MT.string.trim(txt.style).split(/\s*(?::|;)\s*/);
                sfsize = null;
                for (var j = 0, len = styles.length; j < len; ) {
                    k = MT.string.toCamelCase(styles[j++]);
                    v = styles[j++] || '';
                    flag = false;
                    if (k == 'color') {
                        fill = v; flag = true;
                    }
                    if (k == 'textAlign') {
                        k = "text-anchor";
                        v = align[v];
                        txtAnchor = v || "";
                    }
                    if (k == 'fontSize') sfsize = parseInt(v.replace('px', ''), 10);
                    if (!flag && k != '') str += k + ':' + v + ';';
                }
                sfsize = sfsize || fontSize;
                dsize = sfsize * leading;
                dy += i > 0 ? dsize : dsize / 2;
                obj = me.paper.text(x + hw, y + dy, txt.text);
                obj.attr({ "font-size": sfsize });
                str != "" && MT.dom.setStyle(obj.node, str);
                if (fill != '') obj.node.setAttribute("fill", fill);
                if (txtAnchor != '') obj.node.setAttribute("text-anchor", txtAnchor);
                obj.setId = setId;
                st.push(obj);
            }
            var bb = st.getBBox(), bw = bb.width, bn, nw, nanc;
            MT.each(st, function (nd, i) {
                bn = nd.getBBox();
                nanc = nd.node.getAttribute('text-anchor');
                switch (nanc) {
                    case 'start':
                        nd.attr({ x: x });
                        break;
                    case 'end':
                        nd.attr({ x: x + bw });
                        break;
                    default:
                        nd.attr({ x: x + bw / 2 });
                }
            });
        },
        loadData: function (url) {
            var me = this, paper;
            me.container.mask('数据加载中，请稍后...');
            $.ajax({
                url: url, cache: false,
                success: function (rs) {
                    if (rs) {

                        if (MT.isString(rs)) {
                            rs = R.loadXML(rs);
                         }
                        var canvs = rs.getElementsByTagName('canvas')[0],
                            setting = {}, nodes = {}, idx, idxs = [], count = 1, renderArg, pid,
                            node, nodeName;

                        MT.each(canvs.attributes || [], function (v, i) {
                            setting[v.name] = v.value;
                        });
                        me.paperCfg(setting);
                        paper = me.paper;
                        paper.doLine.init({ host: me }); // init line
                        node = canvs.nextSibling;
                        while (node) {
                            if (node.nodeType < 3) {
                                idx = node.getAttribute('idx') || (count++);
                                nodeName = node.nodeName.toLowerCase();
                                switch (nodeName) {
                                    case 'rshape':
                                        renderArg = paper.shapeArgs(me, node);
                                        break;
                                    case 'rtext':
                                        renderArg = paper.textArgs(me, node);
                                        pid = node.getAttribute('pid');
                                        break;
                                    case 'rline':
                                        renderArg = paper.lineArgs(me, node);
                                }
                                idxs.push(parseInt(idx, 10) || 0);
                                nodes['idx' + idx] = { nodeName: nodeName, args: renderArg, idx: idx, pid: pid };
                            }
                            node = node.nextSibling;
                        }
                        me.analysisNode(idxs, nodes);
                        idxs = [];
                        nodes = undefined;
                        delete nodes;
                    }
                    me.container.unmask();
                },
                error: function (e) {
                    me.container.unmask();
                    throw new Error(e.message);
                }
            });
        },
        analysisNode: function (idxs, nodes) {
            var me = this, i = 0, len, nodeObj, nd;
            idxs._quickSort1(); // quick sort
            len = idxs.length;
            for (; i < len; ++i) {
                nd = null;
                nodeObj = nodes['idx' + idxs[i]];
                switch (nodeObj.nodeName) {
                    case 'rshape':
                        nd = me.renderShape.apply(me, nodeObj.args || []);
                        break;
                    case 'rtext':
                        nd = me.doRenderText.apply(me, nodeObj.args || []);
                        break;
                    case 'rline':
                        nd = me.renderLine.apply(me, nodeObj.args || []);
                }
                if (nd && nd.id) {
                    me.nodes[nd.id] = nd;
                }
            }
            nd = null;
            nodes = undefined;
            delete nodes;
            me.callback && me.callback.call(me);
        }
    };

    MT.RenderGraph = RenderGraph;
})(Raphael);